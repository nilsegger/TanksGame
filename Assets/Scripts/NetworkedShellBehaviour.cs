using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkedShellBehaviour : NetworkBehaviour 
{

    public float m_Speed = 1.0f;

    public Renderer m_Renderer;
    private Rigidbody _body;

    private NetworkVariable<float> _spawnTime = new NetworkVariable<float>(0.0f);

    private bool _activatedSinceLastNetworkSpawn = false;

    private void Start()
    {
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true; // dont be affected by gravity etc while waiting for shell to become active
        
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
    }

    private void OnSpawnTimeChanged(float oldTime, float newTime)
    {
        
        if (NetworkManager.Singleton.IsServer) return;
        if (_activatedSinceLastNetworkSpawn) return;
        
        var waitTime = newTime - NetworkManager.ServerTime.Time;
        StartCoroutine(ActivateInS((float) waitTime));
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
    }

    private void FixedUpdate()
    {
        
    }
    */
}
