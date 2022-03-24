using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

using Unity.Netcode;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class LobbyManagerBehaviour : NetworkBehaviour
{
    
    public enum ConnectStatus
    {
        Undefined,
        Success,                  //client successfully connected. This may also be a successful reconnect.
        ServerFull,               //can't join, server is already at capacity.
        GameStarted,              // Game has already started and is not accepting new players 
        UserRequestedDisconnect,  //Intentional Disconnect triggered by the user.
        GenericDisconnect,        //server disconnected, but no specific reason given.
    }

    public int playersPerTeam = 1;
    public LobbyUIBehaviour lobbyUi;
    public GameObject lobby;

    public int m_WaitTime = 5 * 60;

    private float _waitTime = 0.0f;

    private bool _gameStarted = false;
    private Dictionary<Vector3, ulong> _spawnPointsToClientid;

    private List<ulong> _approvedClients;

    private static LobbyManagerBehaviour _instance;
    
    private NetworkVariable<int> missingPlayersCount = new NetworkVariable<int>(0);
    
    private void Start()
    {
        Assert.IsTrue(_instance == null);
        _instance = this;
        
        lobbyUi.AddOnClickPlayListener(OnPlayClick);
        lobbyUi.AddOnClickDisconnectListener(OnDisconnectClick);
        AddSpawnPoints();
        
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
                
        lobbyUi.m_StartServer.onClick.AddListener(() =>
        {
            lobbyUi.SetNetworkStatusText("Starting server...");
            lobbyUi.SetStartServerBtnVisibility(false);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (lobbyUi.ServerPort(out var port))
            {
                transport.SetConnectionData(lobbyUi.ServerIp(), (ushort) port);
                NetworkManager.Singleton.StartServer();
            } else Debug.Log("error with port");
        });
        
        lobbyUi.m_StartHost.onClick.AddListener(() =>
        {
            _approvedClients = new List<ulong>();
            NetworkManager.Singleton.StartHost();
        });
        
        #if UNITY_SERVER
            CheckToStartServer();
            PlayFabMultiplayerAgentAPI.OnShutDownCallback += () => Application.Quit();
        #endif
    }

    private void CheckToStartServer()
    {
        string ip = "";
        int port = 0;
        
        string[] args = System.Environment.GetCommandLineArgs ();
        for (int i = 0; i < args.Length; i++) {
            if (args [i] == "-bindIp") {
                ip = args [i + 1];
            }
            if (args [i] == "-bindPort") {
                if (!int.TryParse(args[i + 1], out port))
                {
                    Console.Error.WriteLine("Failed to convert " + args[i + 1] + " to port.");
                    Application.Quit();
                }
            }
        }

        if (ip.Equals("") || port == 0)
        {
            Console.Error.WriteLine("Please initialize -bindIp and -bindPort");
            Application.Quit();
        }
        
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);

#if UNITY_SERVER
        PlayFabMultiplayerAgentAPI.Start();
#endif
        NetworkManager.Singleton.StartServer();
    }

    private void AddSpawnPoints()
    {
        _spawnPointsToClientid = new Dictionary<Vector3, ulong>();
        foreach (Transform childTransform  in lobby.transform)
        {
            if (childTransform.gameObject.CompareTag("SpawnPoint"))
            {
                _spawnPointsToClientid.Add(childTransform.position, 0);
                childTransform.gameObject.SetActive(false);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        missingPlayersCount.OnValueChanged += UpdateMissingPlayerCountUI;
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientConnectResult_CustomMessage), ReceiveServerToClientConnectResult_CustomMessage);
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        missingPlayersCount.OnValueChanged -= UpdateMissingPlayerCountUI;
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(ReceiveServerToClientConnectResult_CustomMessage));
    }

    private void UpdateMissingPlayerCountUI(int old, int newV)
    {
        lobbyUi.SetMissingPlayersCount(newV);
    }

    private void OnPlayClick()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer ||
            NetworkManager.Singleton.IsHost) return;

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (lobbyUi.ServerPort(out var port))
        {
            transport.SetConnectionData(lobbyUi.ServerIp(), (ushort) port);
        }
        else
        {
            // TODO obviously this should have separate text, but code go brrr
            lobbyUi.SetNetworkStatusText("Invalid port");
            return;
        }

        if (PlayfabPersistenceData.IsUsingPlayFab)
        {
            transport.SetConnectionData(PlayfabPersistenceData.ServerDetails.IPV4Address, (ushort) PlayfabPersistenceData.ServerDetails.Ports[0].Num);
        }

        NetworkManager.Singleton.StartClient();
        lobbyUi.SetPlayButtonVisibility(false);
        lobbyUi.SetDisconnectButtonVisibility(false);
        lobbyUi.SetStartServerBtnVisibility(false);
        lobbyUi.SetNetworkStatusText("Connecting...");
        StartCoroutine(WaitToCheckIfConnected());
    }

    IEnumerator WaitToCheckIfConnected()
    {
        yield return new WaitForSeconds(5);
        
        if (!NetworkManager.Singleton.IsConnectedClient)
        {
           lobbyUi.SetDisconnectButtonVisibility(false); 
           lobbyUi.SetPlayButtonVisibility(true); 
           lobbyUi.SetNetworkStatusText("Connection was not successful");
           
           // NetworkManager.Singleton.Shutdown(); 
        }
    }

    private void OnDisconnectClick()
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return;
        
        lobbyUi.SetDisconnectButtonVisibility(false); 
        lobbyUi.SetPlayButtonVisibility(false); 
        lobbyUi.SetNetworkStatusText("Disconnecting...");
        
        NetworkManager.Singleton.Shutdown();
    }

    private void OnServerStarted()
    {
        if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            _approvedClients = new List<ulong>();
            lobbyUi.SetNetworkStatusText("SERVER");
            lobbyUi.SetPlayButtonVisibility(false);
            lobbyUi.SetDisconnectButtonVisibility(false);
            lobbyUi.SetStartGameVisibility(true);
            lobbyUi.SetHostBtnVisibility(false);
            
            #if UNITY_SERVER
                    PlayFabMultiplayerAgentAPI.ReadyForPlayers();
            #endif
                    
            lobbyUi.m_ServerStartGame.onClick.AddListener(() =>
            {
                NetworkManager.SceneManager.LoadScene("DesertMap", LoadSceneMode.Single);
            });
            
        } else if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.SceneManager.LoadScene("DesertMap", LoadSceneMode.Single);
        }
    } 
    
    private void OnClientConnect(ulong cliendId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (IsLobbyReadyForGame())
            {
                _gameStarted = true;
                NetworkManager.SceneManager.LoadScene("DesertMap", LoadSceneMode.Single);
            }
        } else if (NetworkManager.Singleton.IsClient)
        {
           lobbyUi.SetDisconnectButtonVisibility(true); 
           lobbyUi.SetPlayButtonVisibility(false); 
           lobbyUi.SetNetworkStatusText("Connected as client");
        }
    }
    
    private void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Player disconnected");
            if (_approvedClients.Remove(clientId))
            {
                foreach(KeyValuePair<Vector3, ulong> entry in _spawnPointsToClientid)
                {
                    if (entry.Value == clientId)
                    {
                        _spawnPointsToClientid[entry.Key] = 0;
                        break;
                    }
                } 
            }
            if (_gameStarted && _approvedClients.Count == 0)
            {
                Debug.Log("All Players have disconnected.");
                NetworkManager.Singleton.Shutdown();
                
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            } 
        } else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Client disconnect log on client side");
            lobbyUi.SetNetworkStatusText("Disconnected.");
            lobbyUi.SetPlayButtonVisibility(true);
            lobbyUi.SetDisconnectButtonVisibility(false);
            /*
            #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
            #endif
            */
        }
    }

    private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        
        if (_approvedClients.Count >= playersPerTeam * 2)
        {
            SendServerToClientConnectResult(clientId, ConnectStatus.ServerFull);
            StartCoroutine(WaitToDisconnect(clientId));
            return;
        }
        
        if (_gameStarted) /* TODO check if it is a player which is reconnecting */
        {
            SendServerToClientConnectResult(clientId, ConnectStatus.GameStarted);
            StartCoroutine(WaitToDisconnect(clientId));
            return;
        }

        _approvedClients.Add(clientId);

        Vector3 spawnPoint = Vector3.negativeInfinity;
        
        foreach(KeyValuePair<Vector3, ulong> entry in _spawnPointsToClientid)
        {
            if (entry.Value == 0)
            {
                spawnPoint = entry.Key; 
                _spawnPointsToClientid[entry.Key] = clientId;
                break;
            }
        }
        
        Assert.AreNotEqual(spawnPoint, Vector3.negativeInfinity);

        callback(true /* create player object */, null, true /* approve */, spawnPoint,  Quaternion.Euler(0.0f, 180.0f, 0.0f));
        
        
        missingPlayersCount.Value = (playersPerTeam * 2) - _approvedClients.Count;
    }
    
    private IEnumerator WaitToDisconnect(ulong clientId)
    {
        yield return new WaitForSeconds(0.5f);
        NetworkManager.Singleton.DisconnectClient(clientId);
    }
    
    private void SendServerToClientConnectResult(ulong clientID, ConnectStatus status)
    {
        var writer = new FastBufferWriter(sizeof(ConnectStatus), Allocator.Temp);
        writer.WriteValueSafe(status);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(nameof(ReceiveServerToClientConnectResult_CustomMessage), clientID, writer);
    }
    
    public static void ReceiveServerToClientConnectResult_CustomMessage(ulong clientID, FastBufferReader reader)
    {
        reader.ReadValueSafe(out ConnectStatus status);
        Debug.Log("ReceiveServerToClientConnectResult_CustomMessage: " + status);
        if (status != ConnectStatus.Success)
        {
            switch (status)
            {
                case ConnectStatus.ServerFull:
                {
                    _instance.lobbyUi.SetNetworkStatusText("Server is full.");
                    _instance.lobbyUi.SetPlayButtonVisibility(true);
                    _instance.lobbyUi.SetDisconnectButtonVisibility(false);
                    break;
                }
                case ConnectStatus.GameStarted:
                {
                    _instance.lobbyUi.SetNetworkStatusText("Game has already begun.");
                    _instance.lobbyUi.SetPlayButtonVisibility(true);
                    _instance.lobbyUi.SetDisconnectButtonVisibility(false);
                    break;
                }
                default:
                {
                    _instance.lobbyUi.SetNetworkStatusText(status.ToString());
                    break;
                }
            }
        }
    }

    private bool IsLobbyReadyForGame()
    {
        return _approvedClients.Count == playersPerTeam * 2;
    }

    private void Update()
    {
        _waitTime += Time.deltaTime;
        if (_waitTime >= m_WaitTime)
        {
            #if UNITY_EDITOR
                            UnityEditor.EditorApplication.isPlaying = false;
            #else
                            Application.Quit();
            #endif
        }
    }
}

