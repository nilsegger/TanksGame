using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class NetworkedTankBehaviour : NetworkBehaviour 
{
    /* private struct PlayerInputs : INetworkSerializable
    {
        public Vector3 destination;
        public bool requestsToShoot;
        public bool rotateTurretLeft;
        public bool rotateTurretRight;
        public bool rotateTankLeft;
        public bool rotateTankRight;

        public void Reset()
        {
            destination = Vector3.zero;
            requestsToShoot = false;
            rotateTurretLeft = false;
            rotateTurretRight = false;
            rotateTankLeft = false;
            rotateTankRight = false;
        }

        public bool ContainsAction()
        {
            return destination != Vector3.zero || requestsToShoot || rotateTurretLeft || rotateTurretRight;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref destination);
            serializer.SerializeValue(ref requestsToShoot);
            serializer.SerializeValue(ref rotateTurretLeft);
            serializer.SerializeValue(ref rotateTurretRight);
            serializer.SerializeValue(ref rotateTurretLeft);
            serializer.SerializeValue(ref rotateTurretRight);
        }

        public void MergeNewer(PlayerInputs other)
        {
            if (other.destination != Vector3.zero) destination = other.destination;
            requestsToShoot |= other.requestsToShoot;
            rotateTurretLeft |= other.rotateTurretLeft;
            rotateTurretRight |= other.rotateTurretRight;
        }
    } */
    // Start is called before the first frame update

    public Camera m_PlayerCamera;
    public GameObject m_Shell;
    public GameObject m_DestinationMarker;
    public float ShootCooldownS = 3.0f;
    public Transform m_Turret;
    public float m_TurretRotateSpeed = 25.0f;
    public float m_TankRotateSpeed = 15.0f;
    public float m_ShellForwardOffset = 2.0f;
    public float m_IgnorePlayerShootRequestBeforeCooldownAtS = 1.0f; // All shoots request which happen before cooldown has hit 1s will be ignored

    private NavMeshAgent _agent;
    private bool _gameHasStarted = false;
    private float _shootCooldown = 0.0f;
    private GameObject _destinationMarkerInstance;

    private PlayerInput clientLocalInput = new PlayerInput();
    private Queue<PlayerInput> clientServerInputsQueue = new Queue<PlayerInput>();

    void Start()
    {
        NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        _agent = gameObject.GetComponent<NavMeshAgent>();
        PrepareWayMarker();        
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

    [ServerRpc]
    private void ReceiveClientInputServerRpc(PlayerInput input)
    {
        clientServerInputsQueue.Enqueue(input);
    }

    private IEnumerator WaitToStartGame(double time)
    {
        if (time > 0.0f)
        {
            yield return new WaitForSeconds((float) time);
        }

        _gameHasStarted = true;
        GetComponent<NetworkTransform>().Interpolate = true;
    }

    [ClientRpc]
    public void StartGameAtTimeClientRpc(double time)
    {
        var waitTime = time - NetworkManager.ServerTime.Time;
        StartCoroutine(WaitToStartGame(waitTime));
    }
    
    public void ServerStartGameInTime(double time)
    {
        var waitTime = time - NetworkManager.ServerTime.Time;
        StartCoroutine(WaitToStartGame(waitTime));
    }

    // Update is called once per frame
    void Update()
    {
        DoClientUpdate();
        DoServerUpdate();
    }

    private void DoClientUpdate()
    {
        if (!IsOwner || !_gameHasStarted || !NetworkManager.Singleton.IsClient) return;
        
        clientLocalInput.Reset();
                
        if (Input.GetMouseButton(0))
        {
            Ray ray = m_PlayerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit))
            {
                ClientSetLocalNavDestination(hit.point);
            }
        }

        clientLocalInput.requestsToShoot = Input.GetKey("space");
        clientLocalInput.rotateTurretLeft = Input.GetKey("q");
        clientLocalInput.rotateTurretRight = Input.GetKey("e");
        clientLocalInput.rotateTankLeft = Input.GetKey("a");
        clientLocalInput.rotateTankRight = Input.GetKey("d");

        if (Input.GetKey("s"))
        {
            ClientSetLocalNavDestination(transform.position);
        }

        if (clientLocalInput.ContainsAction())
        {
            ReceiveClientInputServerRpc(clientLocalInput);
        }
        
        DoClientPreMovement();
    }
    
    private void ClientSetLocalNavDestination(Vector3 destination)
    {
        clientLocalInput.destination = destination;
        _destinationMarkerInstance.transform.position = destination;
        _destinationMarkerInstance.SetActive(true);
    }

    private void DoClientPreMovement()
    {
        // if(clientLocalInput.destination != Vector3.zero) _agent.SetDestination(clientLocalInput.destination);
        NavigatePlayer(clientLocalInput); 
        // CheckToSpawnShell(input);
        RotateTurret(clientLocalInput);
        RotateTank(clientLocalInput);
    }

    private void DoServerUpdate()
    {
        if (!_gameHasStarted || !NetworkManager.Singleton.IsServer || clientServerInputsQueue.Count == 0) return;

        while (clientServerInputsQueue.Count > 0)
        {
            var input = clientServerInputsQueue.Dequeue();
            NavigatePlayer(input); 
            CheckToSpawnShell(input);
            RotateTurret(input);
            RotateTank(input);
        }
    }

    private void NavigatePlayer(PlayerInput input)
    {
        if (input.destination == Vector3.zero) return;
        _agent.SetDestination(input.destination);
        _destinationMarkerInstance.transform.position = input.destination;
        _destinationMarkerInstance.SetActive(true);
    }

    private void CheckToSpawnShell(PlayerInput input)
    {
        _shootCooldown -= Time.fixedDeltaTime;

        if (_shootCooldown > m_IgnorePlayerShootRequestBeforeCooldownAtS)
        {
            input.requestsToShoot = false;
            return;
        }
        
        if (!input.requestsToShoot || _shootCooldown > 0.0f) return;
        
        NetworkObject shell = NetworkPool.Singleton.GetNetworkObject(m_Shell, m_Turret.position + m_Turret.forward * m_ShellForwardOffset, m_Turret.rotation);
        shell.Spawn(true);
        _shootCooldown = ShootCooldownS;
        input.requestsToShoot = false;
    }

    private void RotateTurret(PlayerInput input)
    {
        if (input.rotateTurretLeft)
        {
            m_Turret.Rotate(Vector3.up,  -m_TurretRotateSpeed * Time.deltaTime);
        }

        if (input.rotateTurretRight)
        {
            m_Turret.Rotate(Vector3.up,  m_TurretRotateSpeed * Time.deltaTime);
        }
    }
    
    private void RotateTank(PlayerInput input)
        {
            if (input.rotateTankLeft)
            {
                transform.Rotate(Vector3.up,  -m_TankRotateSpeed* Time.deltaTime);
            }
    
            if (input.rotateTankRight)
            {
                transform.Rotate(Vector3.up,  m_TankRotateSpeed* Time.deltaTime);
            }
        }
    
}
