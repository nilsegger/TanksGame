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
    
    private NavMeshAgent _agent;
    private NetworkVariable<Vector3> _navDestination;
    private NetworkVariable<Vector3> _serverPosition;
    
    private NetworkServerOverridePosition _serverPositionOverride = new NetworkServerOverridePosition();
    
    public float m_ServerCorrectionRotationSpeed = 15.0f;
    private NetworkVariable<float> _serverRotation = new NetworkVariable<float>();
    private Quaternion _ownerRotationTarget = Quaternion.identity;
    private float _clientLastRotation = 0.0f; // used to check whetever client needs to push new rotation to server
    
    private NetworkServerOverrideFloat _serverRotationOverride = new NetworkServerOverrideFloat();

    private bool _lockedMovement = false;
    void Start()
    {
        NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        _agent = gameObject.GetComponent<NavMeshAgent>();
        PrepareWayMarker();
        
        _serverPositionOverride.AddSetting("moving", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 3.0f, MaxAllowedDelta = 3.0f});
        _serverPositionOverride.AddSetting("stopped", new NetworkServerOverrideSettings {InterpolationDuration = .5f, ResetPositionAfterMismatchTime = 1.0f, MaxAllowedDelta = 0f});
        _serverPositionOverride.AddSetting("spawn", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 0.0f, MaxAllowedDelta = 0f});
        
        _serverRotationOverride.AddSetting("moving", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 2.0f, MaxAllowedDelta = 15.0f});
        _serverRotationOverride.AddSetting("stopped", new NetworkServerOverrideSettings {InterpolationDuration = .5f, ResetPositionAfterMismatchTime = 1.0f, MaxAllowedDelta = 0f});
        _serverRotationOverride.AddSetting("spawn", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 0.0f, MaxAllowedDelta = 0f});
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
                Debug.Log("Hello");
                ActivatePlayer();
            }
        }
    }

    private void PrepareWayMarker()
    {
        _destinationMarkerInstance = Instantiate(m_DestinationMarker);
        _destinationMarkerInstance.SetActive(false);
        DontDestroyOnLoad(_destinationMarkerInstance);
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

            if (Math.Abs(_clientLastRotation - transform.rotation.eulerAngles.y) > 0.01)
            {
                _clientLastRotation = transform.rotation.eulerAngles.y;
                ClientPushRotationTargetServerRpc(transform.rotation.eulerAngles.y);
            }
        }

        
        if(_agent.velocity.magnitude == 0.0f)
        {
            _serverPositionOverride.Activate("stopped");
            _serverRotationOverride.Activate("stopped");
        }
        else
        {
            _serverPositionOverride.Activate("moving");
            _serverRotationOverride.Activate("moving");
        }
        
        var lagOffset = (transform.position - _serverPosition.Value).magnitude;
        if(_serverPositionOverride.CheckForRequiredServerOverride(transform.position, _serverPosition.Value, out var result, lagOffset, Time.deltaTime))
        {
            transform.position = result;
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
        }
        
        Debug.DrawLine(_serverPosition.Value, _serverPosition.Value + (Quaternion.Euler(0.0f, _serverRotation.Value, 0.0f) * Vector3.forward * 2.0f), _serverRotationOverride.IsOverrideDistance(rotationOffset) ? Color.red : Color.white);
        
    }
    
    private void ClientSetLocalNavDestination(Vector3 destination)
    {
        ClientPushNewNavDestinationServerRpc(destination);
        _agent.SetDestination(destination);
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

    private void DoServerUpdate()
    {
        if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsServer) return;

        _serverPosition.Value = transform.position;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, _ownerRotationTarget,
            m_ServerCorrectionRotationSpeed * Time.deltaTime);
        _serverRotation.Value = transform.rotation.eulerAngles.y;
    }

    private void OnClientChangedNavDestination(Vector3 oldValue, Vector3 newValue)
    {
        if (!IsOwner && newValue != Vector3.zero) // Sacrificing position because what are the chances that someone clicks Vector3.zero
        {
            _agent.SetDestination(newValue);
        }
    }

    public void HaltAtPosition()
    {
        _agent.SetDestination(transform.position);
    }
    
    public void UpdateDestinationForShot(Vector3 position, float maxCorrectionMagnitude)
    {
        var forward = position - transform.position;
        if (forward.magnitude <= maxCorrectionMagnitude)
        {
            _agent.SetDestination(position);
            _navDestination.Value = position;
        }
        else
        {
            forward.Normalize();
            var correctedPosition = transform.position + forward  * maxCorrectionMagnitude;
            _agent.SetDestination(correctedPosition);
            _navDestination.Value = correctedPosition;
        }
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
        if (NetworkManager != null)
        {
            Gizmos.DrawWireSphere(_serverPosition.Value, 1);
        }
    }

    [ServerRpc]
    private void ClientPushRotationTargetServerRpc(float yRotation)
    {
        if (!GameManagerBehaviour.GameBegun) return;
        _ownerRotationTarget = Quaternion.Euler(0.0f, yRotation, 0.0f);
    }
    
    
}
