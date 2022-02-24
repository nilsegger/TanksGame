using System.Numerics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

[System.Serializable]
public struct NetworkPlayerSettings
{
    public float resetPositionAfterMismatchTime;
    public float serverOverridePositionAfterMaxDistance;
    public float interpolationDuration;

    public NetworkPlayerSettings(float resetPositionAfterMismatchTime, float serverOverridePositionAfterMaxDistance, float interpolationDuration)
    {
        this.resetPositionAfterMismatchTime = resetPositionAfterMismatchTime;
        this.serverOverridePositionAfterMaxDistance = serverOverridePositionAfterMaxDistance;
        this.interpolationDuration = interpolationDuration;
        this._offsetCounter = 0.0f;
        this._interpolatingTimer = 0.0f;
        this._interpolatingStart = Vector3.zero;
    }

    private float _offsetCounter;
    private float _interpolatingTimer;
    private Vector3 _interpolatingStart;
    
    public void StartCountingMismatch(float time)
    {
        _offsetCounter += time;
    }

    public bool IsOverrideDistance(float distance)
    {
        return distance >= serverOverridePositionAfterMaxDistance;
    }

    public bool ShouldOverridePosition()
    {
        return _offsetCounter >= this.resetPositionAfterMismatchTime;
    }

    public void Reset()
    {
        _offsetCounter = 0.0f;
    }

    public bool IsInterpolating()
    {
        return _interpolatingTimer != 0.0f && _interpolatingTimer < interpolationDuration;
    }
    
    public Vector3 StartInterpolate(Vector3 position, Vector3 target, float time)
    {
        _interpolatingTimer = 0.0f;
        _interpolatingStart = position;
        return Interpolate(target, time);
    }

    public Vector3 Interpolate(Vector3 target, float time)
    {
        _interpolatingTimer += time;
        return Vector3.Lerp(_interpolatingStart, target, 1.0f / interpolationDuration * _interpolatingTimer);
    }
}

public class NetworkedTankBehaviour : NetworkBehaviour 
{

    public Camera m_PlayerCamera;
    public GameObject m_DestinationMarker;
    public float m_ServerNavMeshAccelerationIncrease = 1.2f;
    public NetworkPlayerSettings networkPlayerSettings = new NetworkPlayerSettings(3.0f, 3.0f, 1.0f);

    private NavMeshAgent _agent;
    private GameObject _destinationMarkerInstance;

    private NetworkVariable<Vector3> _navDestination;
    private NetworkVariable<Vector3> _serverPosition;

    

    void Start()
    {
        NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        _agent = gameObject.GetComponent<NavMeshAgent>();
        PrepareWayMarker();
        _navDestination.OnValueChanged += OnClientChangedNavDestination;

       UpgradeServerNavSpeed(); 
    }

    private void UpgradeServerNavSpeed()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        _agent.acceleration *= m_ServerNavMeshAccelerationIncrease;
        _agent.angularSpeed *= m_ServerNavMeshAccelerationIncrease;
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

    private void DoClientUpdate()
    {
        if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsClient) return;

        if (IsOwner)
        {
            if (Input.GetMouseButton(0))
            {
                Ray ray = m_PlayerCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(ray, out hit))
                {
                    ClientSetLocalNavDestination(hit.point);
                }
            }

            if (Input.GetKey("s"))
            {
                ClientSetLocalNavDestination(transform.position);
            }
        }
        
        CheckForRequiredServerOverride();
    }

    private void CheckForRequiredServerOverride()
    {
        if (networkPlayerSettings.IsInterpolating())
        {
            transform.position = networkPlayerSettings.Interpolate(_serverPosition.Value, Time.deltaTime);
            return;
        }
        
        var lagOffset = transform.position - _serverPosition.Value;
        if (networkPlayerSettings.IsOverrideDistance(lagOffset.magnitude))
        {
            networkPlayerSettings.StartCountingMismatch(Time.deltaTime);
            if (networkPlayerSettings.ShouldOverridePosition())
            {
                transform.position =
                    networkPlayerSettings.StartInterpolate(transform.position, _serverPosition.Value, Time.deltaTime);
                // transform.position = _serverPosition.Value;
                networkPlayerSettings.Reset();
            }
            
            Debug.DrawLine(transform.position, _serverPosition.Value, Color.red);
        }
        else
        {
            networkPlayerSettings.Reset();
        }
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
        if (GameManagerBehaviour.GameBegun)
        {
            _navDestination.Value = destination;
        }
    }

    private void DoServerUpdate()
    {
        if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsServer) return;

        _serverPosition.Value = transform.position;
    }

    private void OnClientChangedNavDestination(Vector3 oldValue, Vector3 newValue)
    {
        if (!IsOwner)
        {
            _agent.SetDestination(newValue);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(_serverPosition.Value, 1);
    }
    
}
