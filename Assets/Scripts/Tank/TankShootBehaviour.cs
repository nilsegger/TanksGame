using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TankShootBehaviour : NetworkBehaviour
{

    public GameObject m_ShellPrefab;

    public Animator m_TurretAnimator;

    public float m_ShootCooldown = 3.0f;

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
                Debug.Log("Playing animation");
                m_TurretAnimator.SetTrigger("Shoot");
            }
            else
            {
                _cooldown -= Time.deltaTime;
            }

        }
    }

    [ServerRpc]
    private void ShootServerRpc(float atTime)
    {
        
    }
}
