using System;
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
    public GameObject m_Shell;
    public float ShootCooldownS = 3.0f;
    public Transform m_Turret;
    public float m_ShellForwardOffset = 1.5f;
    public float m_IgnorePlayerShootRequestBeforeCooldownAtS = 1.0f; // All shoots request which happen before cooldown has hit 1s will be ignored

    private NavMeshAgent _agent;
    private bool _playerRequestedToShoot;
    private bool _gameHasStarted = false;
    private float _shootCooldown = 0.0f;

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
    private void ClientRequestsShootServerRpc()
    {
        if (_shootCooldown <= m_IgnorePlayerShootRequestBeforeCooldownAtS)
        {
            _playerRequestedToShoot = true;
        }
        
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
        if (IsOwner && _gameHasStarted && NetworkManager.Singleton.IsClient)
        {
            if (Input.GetMouseButton(0))
            {
                Ray ray = m_PlayerCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(ray, out hit))
                {
                    SetNavAgentDestinationServerRpc(hit.point);
                }
            }

            if (Input.GetKey("space"))
            {
                ClientRequestsShootServerRpc();
            }
        } 
    }

    private void CheckToSpawnShell()
    {
        _shootCooldown -= Time.fixedDeltaTime;
        if (!_playerRequestedToShoot || _shootCooldown > 0.0f) return;
        
        NetworkObject shell = NetworkPool.Singleton.GetNetworkObject(m_Shell, m_Turret.position + m_Turret.forward * m_ShellForwardOffset, m_Turret.rotation);
        shell.Spawn(true);
        _shootCooldown = ShootCooldownS;
        _playerRequestedToShoot = false;
    }

    private void FixedUpdate()
    {
        if (_gameHasStarted && NetworkManager.Singleton.IsServer)
        {
            CheckToSpawnShell();
        }
    }
}
