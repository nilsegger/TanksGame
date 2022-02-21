using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerBehaviour : NetworkBehaviour
{
    
    public GameBasicUIBehaviour m_GameUi;
    public GameObject m_Level;

    public float gameStartCountdownDurationS = 5.0f;

    private Dictionary<Transform, ulong> _spawnPositionsToClientId;
    // Start is called before the first frame update

    private double _gameStartServerTime;
    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnAllClientsConnected;
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSingleLoadComplete;
            FindSpawnPosition();
        } else if (NetworkManager.Singleton.IsClient)
        {
            
        }
    }

    private void FindSpawnPosition()
    {
        _spawnPositionsToClientId = new Dictionary<Transform, ulong>();
        foreach (Transform childTransform  in m_Level.transform)
        {
            if (childTransform.gameObject.CompareTag("SpawnPoint"))
            {
                _spawnPositionsToClientId.Add(childTransform, 0);
                // childTransform.gameObject.SetActive(false);
            }
        }
    }

    // Set new spawn position for player
    private void OnSingleLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        foreach (var entry in _spawnPositionsToClientId.Where(entry => entry.Value == 0))
        {
            _spawnPositionsToClientId[entry.Key] = clientId;
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.position = entry.Key.position;
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.rotation = entry.Key.rotation;
            break;
        }
    }
    
    private void OnAllClientsConnected(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log("All Clients have loaded the new scene");
        double beginCountdownAt = NetworkManager.ServerTime.Time + 3.0f;
        _gameStartServerTime = beginCountdownAt + gameStartCountdownDurationS;
        m_GameUi.SetGameCountdownClientRpc(beginCountdownAt, gameStartCountdownDurationS); // Start a countdown of 5 seconds in 3 seconds
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var tankBehaviour = client.Value.PlayerObject.GetComponent<NetworkedTankBehaviour>();
            tankBehaviour.ServerStartGameInTime(_gameStartServerTime);
            tankBehaviour.StartGameAtTimeClientRpc(_gameStartServerTime);
        }
    }

}
