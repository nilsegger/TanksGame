using System;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class NetworkedTankBehaviour : NetworkBehaviour 
{

    public Camera m_PlayerCamera;
    
    public GameObject m_DestinationMarker;
    private GameObject _destinationMarkerInstance;
    
    private NavMeshAgent _agent;
    private NetworkVariable<Vector3> _navDestination;
    private NetworkVariable<Vector3> _serverPosition;
    
    private NetworkServerOverridePosition movingServerOverridePosition = new NetworkServerOverridePosition(3.0f, 3.0f, 1.0f);
    private NetworkServerOverridePosition stoppedServerOverridePosition = new NetworkServerOverridePosition(1.5f, 0.1f, 0.5f);
    
    public float m_ServerCorrectionRotationSpeed = 15.0f;
    private NetworkVariable<float> _serverRotation = new NetworkVariable<float>();
    private Quaternion _ownerRotationTarget = Quaternion.identity;
    private float _clientLastRotation = 0.0f; // used to check whetever client needs to push new rotation to server
    private NetworkServerOverrideFloat _movingRotationOverride = new NetworkServerOverrideFloat(3.0f, 10.0f, 1.0f);
    private NetworkServerOverrideFloat _stoppedRotationOverride = new NetworkServerOverrideFloat(1.0f, 0.5f, 1.0f);

    private bool _lockedMovement = false;
    void Start()
    {
        NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        _agent = gameObject.GetComponent<NavMeshAgent>();
        PrepareWayMarker();
    }

    public override void OnNetworkSpawn()
    {
        if (_agent == null)
        {
            _agent = gameObject.GetComponent<NavMeshAgent>();
        }

        _navDestination.OnValueChanged += OnClientChangedNavDestination;
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
                ActivatePlayerCamera();
            }
        }
    }

    private void PrepareWayMarker()
    {
        _destinationMarkerInstance = Instantiate(m_DestinationMarker);
        _destinationMarkerInstance.SetActive(false);
        DontDestroyOnLoad(_destinationMarkerInstance);
    }
    
    private void ActivatePlayerCamera()
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
            if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject() && !_lockedMovement)
            {
                HandleTouch(Input.mousePosition);
            }

            if (Input.touchCount > 0 && !_lockedMovement)
            {
                HandleTouch(Input.GetTouch(0).position);
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

        var positionOverrideManager = movingServerOverridePosition;
        var rotationOverrideManager = _movingRotationOverride;
        if(_agent.velocity.magnitude == 0.0f)
        {
            positionOverrideManager = stoppedServerOverridePosition;
            rotationOverrideManager = _stoppedRotationOverride;
        }
        
        var lagOffset = (transform.position - _serverPosition.Value).magnitude;
        if(positionOverrideManager.CheckForRequiredServerOverride(transform.position, _serverPosition.Value, out var result, lagOffset, Time.deltaTime))
        {
            transform.position = result;
        }

        if (positionOverrideManager.IsOverrideDistance(lagOffset))
        {
            Debug.DrawLine(transform.position, _serverPosition.Value, Color.red);
        }
        
        var rotationOffset = Mathf.Abs(transform.rotation.eulerAngles.y - _serverRotation.Value);
        if (rotationOverrideManager.CheckForRequiredServerOverride(transform.rotation.eulerAngles.y,
                _serverRotation.Value, out float updatedRotation, rotationOffset, Time.deltaTime))
        {
            transform.rotation = Quaternion.Euler(0.0f, updatedRotation, 0.0f);
        }
        
        Debug.DrawLine(_serverPosition.Value, _serverPosition.Value + (Quaternion.Euler(0.0f, _serverRotation.Value, 0.0f) * Vector3.forward * 2.0f), rotationOverrideManager.IsOverrideDistance(rotationOffset) ? Color.red : Color.white);
        
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
