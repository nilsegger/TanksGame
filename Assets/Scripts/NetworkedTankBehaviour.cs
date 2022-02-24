using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class NetworkedTankBehaviour : NetworkBehaviour 
{

    public Camera m_PlayerCamera;
    public GameObject m_DestinationMarker;
    public float m_ServerNavMeshAccelerationIncrease = 1.2f;
    
    [SerializeField]
    private NetworkServerOverridePosition networkServerOverride = new NetworkServerOverridePosition(3.0f, 3.0f, 1.0f);

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
        
        var lagOffset = (transform.position - _serverPosition.Value).magnitude;
        Vector3 result;
        if(networkServerOverride.CheckForRequiredServerOverride(transform.position, _serverPosition.Value, out result, lagOffset, Time.deltaTime))
        {
            transform.position = result;
        }

        if (networkServerOverride.IsOverrideDistance(lagOffset))
        {
            Debug.DrawLine(transform.position, _serverPosition.Value, Color.red);
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
