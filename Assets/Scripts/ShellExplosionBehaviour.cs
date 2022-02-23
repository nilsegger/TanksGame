using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShellExplosionBehaviour : NetworkBehaviour
{

    public ParticleSystem m_Particles;
    public float _DestroyAfterS = 5.0f;

    private float _lifeTimer = 0.0f;

    public override void OnNetworkSpawn()
    {
        m_Particles.Clear();
        m_Particles.Play();
        _lifeTimer = 0.0f;
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            _lifeTimer += Time.fixedDeltaTime;
            if (_lifeTimer >= _DestroyAfterS)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
