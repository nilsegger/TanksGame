using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class NavigationBehaviour : NetworkBehaviour 
{

    public Camera m_PlayerCamera;
    public float m_TouchBlock = 0.5f; // Blocks the next touch for 0.5f after lifting finger, since somehow they get registered even though it was an ui click.
    private float _touchBlockCooldown = 0.0f;
    
    public GameObject m_DestinationMarker;
    private GameObject _destinationMarkerInstance;

    private NavMeshPath _path;
    private NavMeshAgent _agent;
    
    public float rotationSpeed = 45.0f;
    public float movementSpeed = 3.0f;
    public AnimationCurve turnCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public float m_LockedMovementUpdateBufferTimeS = 0.1f;
    private float _lockedMovementBufferCountdown = 0.0f;
    
    
    private NetworkVariable<Vector3> _navDestination;
    private NetworkVariable<Vector3> _serverPosition;
    
    private NetworkServerOverridePosition _serverPositionOverride = new NetworkServerOverridePosition();
    
    public float m_ServerCorrectionRotationSpeed = 15.0f;
    private NetworkVariable<float> _serverRotation = new NetworkVariable<float>();
    private Quaternion _ownerRotationTarget = Quaternion.identity;
    private float _clientLastRotation = 0.0f; // used to check whetever client needs to push new rotation to server
    
    
    private NetworkServerOverrideDegrees _serverRotationOverride = new NetworkServerOverrideDegrees();

    private bool _lockedMovement = false;
    void Start()
    {
        NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        _agent = gameObject.GetComponent<NavMeshAgent>();
        
        _serverPositionOverride.AddSetting("moving", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 3.0f, MaxAllowedDelta = 3.0f});
        _serverPositionOverride.AddSetting("stopped", new NetworkServerOverrideSettings {InterpolationDuration = .5f, ResetPositionAfterMismatchTime = 1.0f, MaxAllowedDelta = 0.1f});
        _serverPositionOverride.AddSetting("spawn", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 0.0f, MaxAllowedDelta = 0f});
        _serverPositionOverride.AddSetting("client", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 1.0f, MaxAllowedDelta = 1.0f});
        
        _serverRotationOverride.AddSetting("moving", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 2.0f, MaxAllowedDelta = 15.0f});
        _serverRotationOverride.AddSetting("stopped", new NetworkServerOverrideSettings {InterpolationDuration = .5f, ResetPositionAfterMismatchTime = 1.0f, MaxAllowedDelta = 0.5f});
        _serverRotationOverride.AddSetting("spawn", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 0.0f, MaxAllowedDelta = 0f});
        _serverRotationOverride.AddSetting("client", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 1.0f, MaxAllowedDelta = 10.0f});
    }

    public override void OnNetworkSpawn()
    {
        if (_agent == null)
        {
            _agent = gameObject.GetComponent<NavMeshAgent>();
        }

        _navDestination.OnValueChanged += OnClientChangedNavDestination;

        if (NetworkManager.Singleton.IsClient && !IsOwner)
        {
            _serverPositionOverride.Activate("spawn", true);
            _serverRotationOverride.Activate("spawn", true);
        }
    }

    public override void OnNetworkDespawn()
    {
        _navDestination.OnValueChanged -= OnClientChangedNavDestination;
    }

    private void OnSceneLoad(ulong clientid, string scenename, LoadSceneMode loadscenemode, AsyncOperation asyncoperation)
    {
        if (IsOwner)
        {
            if (!scenename.ToLower().Contains("lobby"))
            {
                ActivatePlayer();
            }
        }
    }
    
    private void ActivatePlayer()
    {
        m_PlayerCamera.gameObject.SetActive(true);
    }

    void Update()
    {
        DoClientUpdate();
        DoServerUpdate();
    }

    [ClientRpc]
    public void ServerOverridePositionClientRpc(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    private void HandleTouch(Vector3 pos)
    {
        Ray ray = m_PlayerCamera.ScreenPointToRay(pos);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit))
        {
            ClientSetLocalNavDestination(hit.point);
        }
    }

    private void DoClientUpdate()
    {
        if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsClient) return;

        if (IsOwner)
        {

            bool uiTouch = EventSystem.current.IsPointerOverGameObject() ||
                           (Input.touchCount > 0 &&
                            EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId));
            
            if (Input.GetMouseButton(0) && !uiTouch && !_lockedMovement)
            {
                HandleTouch(Input.mousePosition);
            }

            if (Input.touchCount > 0 && !uiTouch && _touchBlockCooldown <= 0.0f && !_lockedMovement)
            {
                HandleTouch(Input.GetTouch(0).position);
            } else if (uiTouch)
            {
                _touchBlockCooldown = m_TouchBlock;
            }

            if (_touchBlockCooldown > 0.0f)
            {
                _touchBlockCooldown -= Time.deltaTime;
            }

            if (Input.GetKey("s") && !_lockedMovement)
            {
                ClientSetLocalNavDestination(transform.position);
            }

            

        }
        
        FollowPath();

        if (IsOwner)
        {
            if (Math.Abs(_clientLastRotation - transform.rotation.eulerAngles.y) > 0.01)
            {
                _clientLastRotation = transform.rotation.eulerAngles.y;
                ClientPushRotationTargetServerRpc(transform.rotation.eulerAngles.y);
            }
        }
        
        ClientCheckServerOverrides();
    }

    private void ClientCheckServerOverrides()
    {
        if(IsOwner && !isMoving())
        {
            _serverPositionOverride.Activate("stopped");
            _serverRotationOverride.Activate("stopped");
        }
        else if(IsOwner)
        {
            _serverPositionOverride.Activate("moving");
            _serverRotationOverride.Activate("moving");
        }
        else
        {
            _serverPositionOverride.Activate("client");
            _serverRotationOverride.Activate("client");
        }
        
        var lagOffset = (transform.position - _serverPosition.Value).magnitude;
        if(_serverPositionOverride.CheckForRequiredServerOverride(transform.position, _serverPosition.Value, out var result, lagOffset, Time.deltaTime))
        {
            transform.position = result;
            Debug.Log("Overriding Position");
        }

        if (_serverPositionOverride.IsOverrideDistance(lagOffset))
        {
            Debug.DrawLine(transform.position, _serverPosition.Value, Color.red);
        }
        
        var rotationOffset = Mathf.Abs(transform.rotation.eulerAngles.y - _serverRotation.Value);
        if (_serverRotationOverride.CheckForRequiredServerOverride(transform.rotation.eulerAngles.y,
                _serverRotation.Value, out float updatedRotation, rotationOffset, Time.deltaTime))
        {
            transform.rotation = Quaternion.Euler(0.0f, updatedRotation, 0.0f);
            Debug.Log("Overriding Rotation");
        }
        
        Debug.DrawLine(_serverPosition.Value, _serverPosition.Value + (Quaternion.Euler(0.0f, _serverRotation.Value, 0.0f) * Vector3.forward * 2.0f), _serverRotationOverride.IsOverrideDistance(rotationOffset) ? Color.red : Color.white);
    }
    
    private void ClientSetLocalNavDestination(Vector3 destination)
    {
        ClientPushNewNavDestinationServerRpc(destination);
        
        CalculateNewPath(destination);
        
        if(_destinationMarkerInstance == null) _destinationMarkerInstance = Instantiate(m_DestinationMarker);
        _destinationMarkerInstance.transform.position = destination;
        _destinationMarkerInstance.SetActive(true);
    }

    [ServerRpc]
    private void ClientPushNewNavDestinationServerRpc(Vector3 destination)
    {
        if (GameManagerBehaviour.GameBegun && !_lockedMovement)
        {
            _navDestination.Value = destination;
        }
    }
    
    [ServerRpc]
    private void ClientPushRotationTargetServerRpc(float yRotation)
    {
        if (!GameManagerBehaviour.GameBegun) return;
        _ownerRotationTarget = Quaternion.Euler(0.0f, yRotation, 0.0f);
    }

    private void DoServerUpdate()
    {
        if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsServer) return;

        if (_lockedMovement && _lockedMovementBufferCountdown <= 0.0f) return;

        _serverPosition.Value = transform.position;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, _ownerRotationTarget,
            m_ServerCorrectionRotationSpeed * Time.deltaTime);
        _serverRotation.Value = transform.rotation.eulerAngles.y;
        
        FollowPath();

        if (_lockedMovementBufferCountdown > 0.0f) _lockedMovementBufferCountdown -= Time.deltaTime;
    }

    private void CalculateNewPath(Vector3 destination)
    {
        _path ??= new NavMeshPath();
        _agent.CalculatePath(destination, _path);
    }


    private void OnClientChangedNavDestination(Vector3 oldValue, Vector3 newValue)
    {
        if (!IsOwner && newValue != Vector3.zero) // Sacrificing position because what are the chances that someone clicks Vector3.zero
        {
           CalculateNewPath(newValue); 
        }
    }

    private bool isMoving()
    {
        if (_path != null && _path.corners.Length > 1)
        {
            var toCorner = _path.corners[1] - transform.position;
            return toCorner.sqrMagnitude > 0.01;
        }
        
        return false;
    }
    
    private void FollowPath()
    {
        if (_path != null && _path.corners.Length > 1)
        {
            
            if (!isMoving())
            {
                if (_path.corners.Length > 2)
                {
                    CalculateNewPath(_path.corners[_path.corners.Length - 1]);
                }
                else
                {
                    _path = null;
                    return;
                }
            }

            var toCorner = _path.corners[1] - transform.position;
            RotateTowardsPath(toCorner, out float slowDown);
            
            var relativeSpeed = toCorner.normalized * movementSpeed * Time.deltaTime * slowDown;
            // this clamps the forward movement vector to point if toCorner is already less
            if (toCorner.sqrMagnitude < relativeSpeed.sqrMagnitude) relativeSpeed = toCorner;
            _agent.Move(relativeSpeed); 
        }
    }
    
        // returns true if player is allowed to drive
    private void RotateTowardsPath(Vector3 toCorner, out float slowDown)
    {
        float angle = Vector3.Angle(transform.forward, toCorner);
        if (angle > 1.0f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toCorner, Vector3.up), rotationSpeed * Time.deltaTime);
        }
        slowDown = turnCurve.Evaluate(1.0f / 180.0f * (180.0f - angle));
    }

    public void HaltAtPosition()
    {
        if(_path != null) _path.ClearCorners();
    }
    
    public void UpdateDestinationForShot(Vector3 position, float maxCorrectionMagnitude)
    {
        var forward = position - transform.position;
        if (forward.magnitude <= maxCorrectionMagnitude)
        {
            CalculateNewPath(position);
            _navDestination.Value = position;
        }
        else
        {
            forward.Normalize();
            var correctedPosition = transform.position + forward  * maxCorrectionMagnitude;
            CalculateNewPath(correctedPosition);
            _navDestination.Value = correctedPosition;
        }

        _lockedMovementBufferCountdown = m_LockedMovementUpdateBufferTimeS;
    }

    public void LockMovement()
    {
        _lockedMovement = true;
    }

    public void UnlockMovement()
    {
        _lockedMovement = false;
    }

    private void OnDrawGizmos()
    {
        if (NetworkManager == null) return;
        
        Gizmos.DrawWireSphere(_serverPosition.Value, 1);
            
        if (_path != null && _path.corners.Length > 1)
        {
            // Gizmos.DrawLine(transform.position, _path.corners[0]);
            for (int i = 0; i < _path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(_path.corners[i], _path.corners[i + 1]);
            }
        }
    }
}
