using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TankShootBehaviour : NetworkBehaviour
{

    public GameObject m_ShellPrefab;
    public GameObject m_ShellSpawnPosition;

    public Animator m_TurretAnimator;
    public AnimationClip m_ShootAnimation;

    public float m_ShootCooldown = 3.0f;
    public float m_ShootWarmUp = 1.0f;

    private float _cooldown = 0.0f;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManagerBehaviour.GameBegun) return;

        if (IsOwner && NetworkManager.Singleton.IsClient)
        {
            if (Input.GetKey("space") && _cooldown <= 0.0f)
            {
                _cooldown = m_ShootCooldown;
                var shootShellAt = NetworkManager.LocalTime.Time + m_ShootWarmUp;
                SetShootAnimation(m_ShootWarmUp); 
                ShootServerRpc(shootShellAt);
            }
            else
            {
                _cooldown -= Time.deltaTime;
            }
        } else if (NetworkManager.Singleton.IsServer)
        {
            if (_cooldown > 0.0f) _cooldown -= Time.deltaTime;
        }
    }

    private void SetShootAnimation(float durationS)
    {
        m_TurretAnimator.SetTrigger("Shoot");
        var animationSpeedup = m_ShootAnimation.length / durationS;
        m_TurretAnimator.speed = animationSpeedup;
    }

    private void ServerSpawnShell(float spawnTime)
    {
        var shell = NetworkPool.Singleton.GetNetworkObject(m_ShellPrefab, m_ShellSpawnPosition.transform.position, m_ShellSpawnPosition.transform.rotation);
        shell.SpawnWithOwnership(OwnerClientId);
        
        var shellBehaviour = shell.gameObject.GetComponent<NetworkedShellBehaviour>();
        shellBehaviour.SetSpawnTime(spawnTime);
    }

    [ServerRpc]
    private void ShootServerRpc(double atTime)
    {
        if (!GameManagerBehaviour.GameBegun) return;
        if (_cooldown > 0.0f) return;
        var waitTime = atTime - NetworkManager.ServerTime.Time;
        SetShootAnimation((float) waitTime);
        ServerSpawnShell((float) atTime);

        _cooldown = m_ShootCooldown;
    }
}
