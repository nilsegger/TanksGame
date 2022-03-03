using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class TankShootBehaviour : NetworkBehaviour
{

    public GameObject m_ShellPrefab;
    public GameObject m_ShellSpawnPosition;
    public GameObject m_HitMarker;

    public Animator m_TurretAnimator;
    public GameObject m_Turret;
    public AnimationClip m_ShootAnimation;

    public Button m_ShootButton;
    private bool _shootButtonPressed = false;

    public float maxAngleCorrectionOnShootStop = 10.0f;
    public float maxPositionCorrectionOnShootStop = 1.0f;

    public float m_ShootCooldown = 3.0f;
    public float m_ShootWarmUp = 1.0f;

    private float _cooldown = 0.0f;

    private GameObject _hitMarkerInstance;

    private NetworkedTankBehaviour _tankMovementBehaviour;
    private TurretRotationBehaviour _turretRotationBehaviour;
    
    // Start is called before the first frame update
    void Start()
    {
        _tankMovementBehaviour = GetComponent<NetworkedTankBehaviour>();
        Assert.IsNotNull(_tankMovementBehaviour);

        _turretRotationBehaviour = GetComponent<TurretRotationBehaviour>();
        Assert.IsNotNull(_turretRotationBehaviour);
        
        m_ShootButton.onClick.AddListener(() => _shootButtonPressed = true) ;
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManagerBehaviour.GameBegun) return;

        if (IsOwner && NetworkManager.Singleton.IsClient)
        {
            if ((Input.GetKey("space") || _shootButtonPressed) && _cooldown <= 0.0f)
            {
                _cooldown = m_ShootCooldown;
                _shootButtonPressed = false;
                var shootShellAt = NetworkManager.LocalTime.Time + m_ShootWarmUp;
                SetShootAnimation(m_ShootWarmUp); 
                ShootServerRpc(shootShellAt, transform.position, m_Turret.transform.localRotation.eulerAngles.y);
                
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

        DisplayHitMarker();
    }

    private void DisplayHitMarker()
    {
        if (!NetworkManager.Singleton.IsServer && !IsOwner) return;
        
        Ray ray = new Ray(m_ShellSpawnPosition.transform.position, m_ShellSpawnPosition.transform.forward);
        if (!Physics.Raycast(ray, out var hit)) return;
        
        if (_hitMarkerInstance == null)
        {
            _hitMarkerInstance = Instantiate(m_HitMarker, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else
        {
            _hitMarkerInstance.transform.position = hit.point;
            _hitMarkerInstance.transform.rotation = Quaternion.LookRotation(hit.normal);
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
    private void ShootServerRpc(double atTime, Vector3 position, float localRotationY)
    {
        if (!GameManagerBehaviour.GameBegun) return;
        if (_cooldown > 0.0f) return;
        var waitTime = atTime - NetworkManager.ServerTime.Time;
        SetShootAnimation((float) waitTime);
        ServerSpawnShell((float) atTime);
        
        _tankMovementBehaviour.LockMovement();
        _tankMovementBehaviour.UpdateDestinationForShot(position, maxPositionCorrectionOnShootStop);
        
        _turretRotationBehaviour.LockMovement();
        _turretRotationBehaviour.UpdateDestinationForShot(localRotationY, maxAngleCorrectionOnShootStop);

        StartCoroutine(WaitToUnlockMovement((float) waitTime));

        _cooldown = m_ShootCooldown;
    }
}
