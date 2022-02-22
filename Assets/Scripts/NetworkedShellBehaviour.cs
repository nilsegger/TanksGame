using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkedShellBehaviour : NetworkBehaviour 
{

    public float m_DestroyAfterS = 10.0f;

    private float _lifeTimeCounter = 0.0f;

    public override void OnNetworkSpawn()
    {
        _lifeTimeCounter = 0.0f;
    }
    
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            _lifeTimeCounter += Time.fixedDeltaTime;

            if (_lifeTimeCounter >= m_DestroyAfterS)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
