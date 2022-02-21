using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class NetworkedTankBehaviour : NetworkBehaviour 
{
    // Start is called before the first frame update

    public Camera m_PlayerCamera;
    private NavMeshAgent _agent;

    private bool _gameHasStarted = false;
    
    void Start()
    {
        NetworkManager.Singleton.SceneManager.OnLoad += OnSceneLoad;
        _agent = gameObject.GetComponent<NavMeshAgent>();
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

    private void ActivatePlayerCamera()
    {
        m_PlayerCamera.gameObject.SetActive(true);
    }

    [ServerRpc]
    private void SetNavAgentDestinationServerRpc(Vector3 destination)
    {
        _agent.SetDestination(destination);
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
        if (_gameHasStarted && NetworkManager.Singleton.IsClient && IsOwner && Input.GetMouseButton(0))
        {
            Ray ray = m_PlayerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast (ray, out hit))
            {
                SetNavAgentDestinationServerRpc(hit.point);
            }
        }
    }
}
