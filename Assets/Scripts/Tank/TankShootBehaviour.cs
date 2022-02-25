using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class TankShootBehaviour : NetworkBehaviour
{

    public GameObject m_ShellPrefab;
    public GameObject m_ShellSpawnPosition;

    public Animator m_TurretAnimator;
    public GameObject m_Turret;
    public AnimationClip m_ShootAnimation;

    public float maxAngleCorrectionOnShootStop = 10.0f;
    public float maxPositionCorrectionOnShootStop = 1.0f;

    public float m_ShootCooldown = 3.0f;
    public float m_ShootWarmUp = 1.0f;

    private float _cooldown = 0.0f;

    private NetworkedTankBehaviour _tankMovementBehaviour;
    private TurretRotationBehaviour _turretRotationBehaviour;
    
    // Start is called before the first frame update
    void Start()
    {
        _tankMovementBehaviour = GetComponent<NetworkedTankBehaviour>();
        Assert.IsNotNull(_tankMovementBehaviour);

        _turretRotationBehaviour = GetComponent<TurretRotationBehaviour>();
        Assert.IsNotNull(_turretRotationBehaviour);
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
                ShootServerRpc(shootShellAt, transform.position, m_Turret.transform.rotation.eulerAngles.y);
                
                _tankMovementBehaviour.LockMovement();
                _tankMovementBehaviour.HaltAtPosition();
                _turretRotationBehaviour.LockMovement();

                StartCoroutine(WaitToUnlockMovement((float)(shootShellAt - NetworkManager.LocalTime.Time)));
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

    private IEnumerator WaitToUnlockMovement(float waitTime)
    {
        if (waitTime > 0.0f)
        {
            yield return new WaitForSeconds(waitTime);
        }
        
        _tankMovementBehaviour.UnlockMovement(); 
        _turretRotationBehaviour.UnlockMovement();
    }

    [ServerRpc]
    private void ShootServerRpc(double atTime, Vector3 position, float rotationY)
    {
        if (!GameManagerBehaviour.GameBegun) return;
        if (_cooldown > 0.0f) return;
        var waitTime = atTime - NetworkManager.ServerTime.Time;
        SetShootAnimation((float) waitTime);
        ServerSpawnShell((float) atTime);
        
        _tankMovementBehaviour.LockMovement();
        _tankMovementBehaviour.UpdateDestinationForShot(position, maxPositionCorrectionOnShootStop);
        
        _turretRotationBehaviour.LockMovement();
        _turretRotationBehaviour.UpdateDestinationForShot(rotationY, maxAngleCorrectionOnShootStop);

        StartCoroutine(WaitToUnlockMovement((float) waitTime));

        _cooldown = m_ShootCooldown;
    }
}
