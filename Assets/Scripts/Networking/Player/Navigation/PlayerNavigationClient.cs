using System;
using Networking.Player.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayerNavigationClient : PlayerNavigationCommon
{
    private PlayerNavigationServer _server;
    
    public Camera m_PlayerCamera;
    public float m_TouchBlock = 0.5f; // Blocks the next touch for 0.5f after lifting finger, since somehow they get registered even though it was an ui click.
    private float _touchBlockCooldown = 0.0f;
    
    public GameObject m_DestinationMarker;
    private GameObject _destinationMarkerInstance;

    private NavMeshPath _path;

    [HideInInspector]
    public NetworkVariable<Vector3> agentDestination = new NetworkVariable<Vector3>();
    [HideInInspector]
    public NetworkVariable<Vector3> serverPosition = new NetworkVariable<Vector3>();
    private NetworkServerOverridePosition _serverPositionOverride = new NetworkServerOverridePosition();
    
    [HideInInspector]
    public NetworkVariable<float> serverRotation = new NetworkVariable<float>();
    private NetworkServerOverrideDegrees _serverRotationOverride = new NetworkServerOverrideDegrees();
    
    private float _clientLastRotation = 0.0f; // used to check whetever client needs to push new rotation to server

    protected override void Start()
    {
        base.Start();

        _server = GetComponent<PlayerNavigationServer>();
        
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
        if (!NetworkManager.Singleton.IsClient)
        {
            return;
        } 
        
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        }
        else
        {
            // only activate for clients which are not owned
            _serverPositionOverride.Activate("spawn", true);
            _serverRotationOverride.Activate("spawn", true);
        }
    }

    

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            NetworkManager.Singleton.SceneManager.OnLoad -= OnSceneLoad;
        }
    }

    // Only called for owner
    private void OnSceneLoad(ulong clientid, string scenename, LoadSceneMode loadscenemode, AsyncOperation asyncoperation)
    {
        if (!scenename.ToLower().Contains("lobby"))
        {
            ActivatePlayer();
        }
    }
    
    private void ActivatePlayer()
    {
        m_PlayerCamera.gameObject.SetActive(true);
    }
    
    [ClientRpc]
    public void SetSpawnPositionClientRpc(Vector3 position, Vector3 forward)
    {
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(forward);
    }

    private void HandleTouch(Vector3 pos)
    {
        var ray = m_PlayerCamera.ScreenPointToRay(pos);
        var hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit))
        {
            ClientSetLocalNavDestination(hit.point);
        }
    }
    
    private void CheckInput()
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
    
    protected override Vector3 NextPathPoint()
    {
        if (IsOwner && _path != null && _path.corners.Length >= 2) return _path.corners[1];
        else if (!IsOwner && agentDestination.Value != Vector3.zero) return agentDestination.Value;
        else return transform.position;
    }

    public void Halt()
    {
        CalculateNewPath(_agent.transform.position);
    }

    private void Update()
    {
        if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsClient) return;
        
        if (IsOwner)
        {
            CheckInput();
        }
        
        FollowPath();

        if (IsOwner)
        {
            if (Math.Abs(_clientLastRotation - transform.rotation.eulerAngles.y) > 0.01)
            {
                _clientLastRotation = transform.rotation.eulerAngles.y;
                _server.ClientPushRotationTargetServerRpc(transform.rotation.eulerAngles.y);
            }
        }
        
        ClientCheckServerOverrides();
    }

    private void ClientCheckServerOverrides()
    {
        if(IsOwner && !IsMoving())
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
        
        var lagOffset = (transform.position - serverPosition.Value).magnitude;
        if(_serverPositionOverride.CheckForRequiredServerOverride(transform.position, serverPosition.Value, out var result, lagOffset))
        {
            transform.position = result;
        }
        Debug.DrawLine(transform.position, serverPosition.Value, _serverPositionOverride.IsOverrideDistance(lagOffset) ? Color.red : Color.white);

        var eulerY = transform.rotation.eulerAngles.y;
        var rotationOffset = Mathf.Abs(eulerY - serverRotation.Value);
        if (_serverRotationOverride.CheckForRequiredServerOverride(eulerY, serverRotation.Value, out float updatedRotation, rotationOffset))
        {
            transform.rotation = Quaternion.Euler(0.0f, updatedRotation, 0.0f);
        }
        Debug.DrawLine(serverPosition.Value, serverPosition.Value + (Quaternion.Euler(0.0f, serverRotation.Value, 0.0f) * Vector3.forward * 2.0f), _serverRotationOverride.IsOverrideDistance(rotationOffset) ? Color.red : Color.white);
    }
    
    private void ClientSetLocalNavDestination(Vector3 destination)
    {
        CalculateNewPath(destination);
        
        if(_destinationMarkerInstance == null) _destinationMarkerInstance = Instantiate(m_DestinationMarker);
        _destinationMarkerInstance.transform.position = destination;
        _destinationMarkerInstance.SetActive(true);
    }
    
    private void CalculateNewPath(Vector3 destination)
    {
        Assert.IsTrue(IsOwner);
        _path ??= new NavMeshPath();
        _agent.CalculatePath(destination, _path);
        _server.ClientPushNewNavDestinationServerRpc(NextPathPoint());
    }

    private void OnDrawGizmos()
    {
        if (NetworkManager == null || !IsClient) return;
        
        Gizmos.DrawWireSphere(serverPosition.Value, 1);
            
        if (IsOwner && _path != null && _path.corners.Length > 1)
        {
            for (var i = 0; i < _path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(_path.corners[i], _path.corners[i + 1]);
            }
        } else if (!IsOwner)
        {
            Gizmos.DrawLine(transform.position, agentDestination.Value);
        }
    }
}
