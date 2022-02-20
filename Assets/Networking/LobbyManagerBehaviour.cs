using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
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
    public LobbyUIBehaviour ui;
    
    public GameObject lobby;

    private bool _gameStarted = false;
    private Dictionary<Vector3, ulong> _spawnPointsToClientid;

    private List<ulong> _approvedClients;

    private static LobbyManagerBehaviour _instance;
    
    private void Start()
    {
        Assert.IsTrue(_instance == null);
        _instance = this;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        
        ui.AddOnClickPlayListener(OnPlayClick);
        ui.AddOnClickDisconnectListener(OnDisconnectClick);
        AddSpawnPoints();
    }

    private void AddSpawnPoints()
    {
        _spawnPointsToClientid = new Dictionary<Vector3, ulong>();
        foreach (Transform childTransform  in lobby.transform)
        {
            if (childTransform.gameObject.tag == "SpawnPoint")
            {
                _spawnPointsToClientid.Add(childTransform.position, 0);
                childTransform.gameObject.SetActive(false);
            }
        }
    }
    
    private void OnPlayClick()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer ||
            NetworkManager.Singleton.IsHost) return;
        
        NetworkManager.Singleton.StartClient();
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientConnectResult_CustomMessage), ReceiveServerToClientConnectResult_CustomMessage);
        NetworkManager.Singleton.SceneManager.OnLoad += ClientOnSceneLoadEvent;
        ui.SetPlayButtonVisibility(false);
        ui.SetDisconnectButtonVisibility(false);
        ui.SetNetworkStatusText("Connecting...");
    }

    private void OnDisconnectClick()
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return;
        
        NetworkManager.Singleton.Shutdown();
        
        /*
        ui.SetPlayButtonVisibility(false);
        ui.SetDisconnectButtonVisibility(false);
        ui.SetNetworkStatusText("Disconnecting...");
        */
        
        ui.SetDisconnectButtonVisibility(false); 
        ui.SetPlayButtonVisibility(true); 
        ui.SetNetworkStatusText("Disconnected");
                    
        NetworkManager.Singleton.SceneManager.OnLoad -= ClientOnSceneLoadEvent;
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(nameof(ReceiveServerToClientConnectResult_CustomMessage));
    }

    private void ClientOnSceneLoadEvent(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        // TODO show loading screen (careful async operation can be null)
    }

    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadEventComplete;
        
        _approvedClients = new List<ulong>();
        UpdateMissingPlayersCount();
        
        ui.SetNetworkStatusText("SERVER");
        ui.SetPlayButtonVisibility(false);
        ui.SetDisconnectButtonVisibility(false);
    } 
    
    private void OnClientConnect(ulong cliendId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                UpdateMissingPlayersCount();
                if (IsLobbyReadyForGame())
                {
                    _gameStarted = true;
                    NetworkManager.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
                }
            } else if (NetworkManager.Singleton.IsClient)
            {
               ui.SetDisconnectButtonVisibility(true); 
               ui.SetPlayButtonVisibility(false); 
               ui.SetNetworkStatusText("Connected as client");
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
            UpdateMissingPlayersCount();
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
                    _instance.ui.SetNetworkStatusText("Server is full.");
                    _instance.ui.SetPlayButtonVisibility(true);
                    _instance.ui.SetDisconnectButtonVisibility(false);
                    break;
                }
                case ConnectStatus.GameStarted:
                {
                    _instance.ui.SetNetworkStatusText("Game has already begun.");
                    _instance.ui.SetPlayButtonVisibility(true);
                    _instance.ui.SetDisconnectButtonVisibility(false);
                    break;
                }
                default:
                {
                    _instance.ui.SetNetworkStatusText(status.ToString());
                    break;
                }
            }
        }
    }

    
    
    private void OnSceneLoadEventComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        // TODO begin countdown to start game
       Debug.Log("All clients have loaded the scene " + sceneName);
       var camera = GameObject.FindWithTag("ServerCamera");
       camera.SetActive(true);
    }

    private bool IsLobbyReadyForGame()
    {
        return _approvedClients.Count == playersPerTeam * 2;
    }

    private void UpdateMissingPlayersCount()
    {
        if (!_gameStarted)
        {
            ui.missingPlayersCount.Value = (playersPerTeam * 2) - _approvedClients.Count;
        }
    }
    
    
}

