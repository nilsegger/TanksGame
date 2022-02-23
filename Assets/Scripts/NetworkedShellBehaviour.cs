using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkedShellBehaviour : NetworkBehaviour 
{

    public float m_DestroyAfterS = 10.0f;
    public float m_Speed = 1.0f;
    public GameObject m_ExplosionPrefab;

    private float _lifeTimeCounter = 0.0f;
    private Rigidbody _body;

    private void Start()
    {
        _body = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        _lifeTimeCounter = 0.0f;
        if (_body != null)
        {
            _body.velocity = new Vector3(0f, 0f, 0f);
            _body.angularVelocity = new Vector3(0f, 0f, 0f);
        }
    }

    private void MoveForward()
    {
        // var forward = new Vector3(_body.transform.forward.x, 0.0f, _body.transform.forward.z); // fix for when bullet drop too quickly
       _body.MovePosition(_body.transform.position + (_body.transform.forward * (m_Speed * Time.deltaTime)));

        /*Ray ray = new Ray(_body.transform.position, Vector3.down);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Distance to floor: " + (hit.point - _body.transform.position).magnitude);
        }j
        */
    }

    private void DoSafeDespawn()
    {
        var networkObject = GetComponent<NetworkObject>();
        if(networkObject.IsSpawned) networkObject.Despawn();
    }

    private void OnCollisionEnter(Collision collision)
    {
        var explosion = NetworkPool.Singleton.GetNetworkObject(m_ExplosionPrefab, collision.contacts[0].point, Quaternion.identity);
        explosion.GetComponent<NetworkObject>().Spawn();
        DoSafeDespawn(); 
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {

            MoveForward(); 
            
            _lifeTimeCounter += Time.fixedDeltaTime;

            if (_lifeTimeCounter >= m_DestroyAfterS)
            {
                DoSafeDespawn(); 
            }
        }
    }
}
