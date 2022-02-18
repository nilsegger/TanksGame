using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Unity.Collections;
using UnityEngine;

using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.Assertions;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class LobbyManagerBehaviour : NetworkBehaviour
{
    
    public enum ConnectStatus
    {
        Undefined,
        Success,                  //client successfully connected. This may also be a successful reconnect.
        ServerFull,               //can't join, server is already at capacity.
        UserRequestedDisconnect,  //Intentional Disconnect triggered by the user.
        GenericDisconnect,        //server disconnected, but no specific reason given.
    }

    public int playersPerTeam = 1;
    public LobbyUIBehaviour ui;

    private List<ulong> _approvedClients;

    private static LobbyManagerBehaviour _instance;
    
    private void Start()
    {
        Assert.IsTrue(_instance == null);
        _instance = this;
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        
        ui.AddOnClickPlayListener(OnPlayClick);
    }
    
    void OnPlayClick()
    {
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.NetworkConfig.ClientConnectionBufferTimeout = 3;
            NetworkManager.Singleton.StartClient();
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(nameof(ReceiveServerToClientConnectResult_CustomMessage), ReceiveServerToClientConnectResult_CustomMessage);
            ui.SetConnectedType("Client", false);
        }
    }

    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        _approvedClients = new List<ulong>();
        ui.SetConnectedType("Server", false);
        UpdateMissingPlayersCount();
    } 

    private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        
        if (_approvedClients.Count >= playersPerTeam * 2)
        {
            SendServerToClientConnectResult(clientId, ConnectStatus.ServerFull);
            StartCoroutine(WaitToDisconnect(clientId));
            return;
        }

        _approvedClients.Add(clientId);
        
        //Your logic here
        bool approve = true;
        bool createPlayerObject = true;

        // The prefab hash. Use null to use the default player prefab
        // If using this hash, replace "MyPrefabHashGenerator" with the name of a prefab added to the NetworkPrefabs field of your NetworkManager object in the scene
        //ulong? prefabHash = NetworkSpawnManager.GetPrefabHashFromGenerator("MyPrefabHashGenerator");
    
        //If approve is true, the connection gets added. If it's false. The client gets disconnected
        callback(createPlayerObject, null, approve, new Vector3(Random.Range(-10.0f, 10.0f), 0.0f, Random.Range(-5.0f, 5.0f)), Quaternion.Euler(0.0f, 180.0f, 0.0f));
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
        _instance.ui.SetConnectedStatus(status, true);
    }

    private void OnClientConnect(ulong cliendId)
    {
        if (!NetworkManager.IsServer) return;
        
        UpdateMissingPlayersCount();
        if (LobbyReadyForGame())
        {
            ui.StartGameBeginCountdownClientRpc(NetworkManager.Singleton.ServerTime.Time + 3.0);
            NetworkManager.SceneManager.LoadScene("SampleScene", LoadSceneMode.Additive);
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadEventComplete;
        }
    }

    private void OnSceneLoadEventComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
       Debug.Log("All clients have loaded the scene " + sceneName); 
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            _approvedClients.Remove(clientId);
            UpdateMissingPlayersCount();
        } 
    }

    private bool LobbyReadyForGame()
    {
        return _approvedClients.Count == playersPerTeam * 2;
    }

    private void UpdateMissingPlayersCount()
    {
        ui.missingPlayersCount.Value = (playersPerTeam * 2) - _approvedClients.Count;
    }
    
    
}

