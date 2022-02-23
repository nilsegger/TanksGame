using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class GameManagerBehaviour : NetworkBehaviour
{
    
    public GameBasicUIBehaviour m_GameUi;
    public GameObject m_Level;
    public Camera serverCamera;

    public float gameStartCountdownDurationS = 5.0f;

    private Queue<Transform> _spawnPositions;

    private double _gameStartServerTime;

    public static bool GameBegun = false;
    
    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnAllClientsConnected;
            serverCamera.gameObject.SetActive(true);
            FindSpawnPosition();
        } else if (NetworkManager.Singleton.IsClient)
        {
            
        }
    }

    private void FindSpawnPosition()
    {
        _spawnPositions = new Queue<Transform>();
        foreach (Transform childTransform  in m_Level.transform)
        {
            if (childTransform.gameObject.CompareTag("SpawnPoint"))
            {
                _spawnPositions.Enqueue(childTransform);
                childTransform.gameObject.SetActive(false);
            }
        }
    }
    
    private void OnAllClientsConnected(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log("All Clients have loaded the new scene");
        
        Assert.IsTrue(clientsTimedOut.Count == 0);

        foreach (var clientId in clientsCompleted)
        {
            var spawn = _spawnPositions.Dequeue();
            var prefab = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            prefab.transform.position = spawn.position;
            prefab.transform.rotation = spawn.rotation;
            var behaviour = prefab.GetComponent<NetworkedTankBehaviour>();
            behaviour.ServerOverridePositionClientRpc(spawn.position, spawn.rotation);
        } 
        
        double beginCountdownAt = NetworkManager.ServerTime.Time + 3.0f;
        m_GameUi.SetGameCountdownClientRpc(beginCountdownAt, gameStartCountdownDurationS); // Start a countdown of 5 seconds in 3 seconds
        StartGameAtTimeClientRpc(beginCountdownAt + gameStartCountdownDurationS);
        StartGameAtTime(beginCountdownAt + gameStartCountdownDurationS);
    }
    
    private static IEnumerator WaitToStartGame(double time)
    {
        if (time > 0.0f)
        {
            yield return new WaitForSeconds((float) time);
        }
        GameBegun = true;
    }
    
    private void StartGameAtTime(double time)
    {
        var waitTime = time - NetworkManager.ServerTime.Time;
        StartCoroutine(WaitToStartGame(waitTime));
    }
    
    [ClientRpc]
    private void StartGameAtTimeClientRpc(double time)
    {
        var waitTime = time - NetworkManager.ServerTime.Time;
        StartCoroutine(WaitToStartGame(waitTime));
    }

}
