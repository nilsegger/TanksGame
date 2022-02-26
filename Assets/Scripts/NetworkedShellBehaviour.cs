using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class NetworkedShellBehaviour : NetworkBehaviour 
{

    public float m_Speed = 1.0f;

    public Renderer m_Renderer;
    private Rigidbody _body;

    private NetworkVariable<float> _spawnTime = new NetworkVariable<float>(0.0f);
    private NetworkVariable<Vector3> _serverPosition = new NetworkVariable<Vector3>(Vector3.zero);

    private bool _activatedSinceLastNetworkSpawn = false;

    private void Start()
    {
        _body = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        if (_body != null)
        {
            _body.velocity = new Vector3(0f, 0f, 0f);
            _body.angularVelocity = new Vector3(0f, 0f, 0f);
        }

        if (m_Renderer != null)
        {
            m_Renderer.enabled = false;
        }
        
        _activatedSinceLastNetworkSpawn = false;
        
        _spawnTime.OnValueChanged += OnSpawnTimeChanged;

        if (NetworkManager.Singleton.IsServer && _body != null)
        {
            _body.isKinematic = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        _spawnTime.OnValueChanged -= OnSpawnTimeChanged;
    }

    private IEnumerator ActivateInS(float waitTime)
    {
        if (_activatedSinceLastNetworkSpawn) yield break;
        
        if (waitTime > 0.0f)
        {
            yield return new WaitForSeconds(waitTime);
        }
        
        m_Renderer.enabled = true;
        _activatedSinceLastNetworkSpawn = true;
    }

    public void SetSpawnTime(float time)
    {
        _spawnTime.Value = time;
        StartCoroutine(ActivateInS((float)(time - NetworkManager.ServerTime.Time)));
    }

    private void OnSpawnTimeChanged(float oldTime, float newTime)
    {
        
        if (NetworkManager.Singleton.IsServer) return;
        if (_activatedSinceLastNetworkSpawn) return;
        
        var waitTime = newTime - NetworkManager.ServerTime.Time;
        StartCoroutine(ActivateInS((float) waitTime));
    }

    private void Update()
    {
        Debug.DrawLine(transform.position, _serverPosition.Value, Color.blue);
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
    }
    */

    private void FixedUpdate()
    {
        if (!NetworkManager.Singleton.IsServer || !GameManagerBehaviour.GameBegun || !_activatedSinceLastNetworkSpawn) return;
        _body.MovePosition(transform.position + (transform.forward * m_Speed * Time.fixedDeltaTime));
        _serverPosition.Value = _body.transform.position;
        Debug.Log("Hello World!");
    }
}
