using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
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
            serverCamera.gameObject.SetActive(true);
            FindSpawnPosition();
        } 
    }

    public override void OnNetworkSpawn()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnAllClientsConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnAllClientsConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong @ulong)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 1)
        {
            Debug.Log("All clients have disconnected.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
            var behaviour = prefab.GetComponent<PlayerNavigationClient>();
            behaviour.SetSpawnPositionClientRpc(spawn.position, spawn.forward);
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
