using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkedShellBehaviour : NetworkBehaviour 
{

    public float m_DestroyAfterS = 10.0f;
    public float m_Speed = 1.0f;

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
        var forward = new Vector3(_body.transform.forward.x, 0.0f, _body.transform.forward.z);
       _body.MovePosition(_body.transform.position + (forward * (m_Speed * Time.fixedDeltaTime)));

        /*Ray ray = new Ray(_body.transform.position, Vector3.down);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Distance to floor: " + (hit.point - _body.transform.position).magnitude);
        }
        */
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggering Shell with: " + other.name);
        GetComponent<NetworkObject>().Despawn();
    }


    private void FixedUpdate()
    {
        if (IsOwner)
        {

            MoveForward(); 
            
            _lifeTimeCounter += Time.fixedDeltaTime;

            if (_lifeTimeCounter >= m_DestroyAfterS)
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
