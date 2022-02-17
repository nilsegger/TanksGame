using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Netcode;

public class RoomApprovalManagerBehaviour : NetworkBehaviour
{

    public int playersPerTeam = 1;

    private int _connectedPlayers = 0;
    
    private void Start() 
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnServerStarted()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
    } 

    private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        Debug.Log("Approval");
        
        //Your logic here
        bool approve = true;
        bool createPlayerObject = true;

        if (_connectedPlayers >= playersPerTeam * 2)
        {
            approve = false;
            createPlayerObject = false;
        }

        _connectedPlayers++;

        // The prefab hash. Use null to use the default player prefab
        // If using this hash, replace "MyPrefabHashGenerator" with the name of a prefab added to the NetworkPrefabs field of your NetworkManager object in the scene
        //ulong? prefabHash = NetworkSpawnManager.GetPrefabHashFromGenerator("MyPrefabHashGenerator");
    
        //If approve is true, the connection gets added. If it's false. The client gets disconnected
        callback(createPlayerObject, null, approve, Vector3.zero, Quaternion.identity);
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.Log("hLleo discconet world");
        _connectedPlayers--;
    }
}

