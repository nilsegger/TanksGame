using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkedShellBehaviour : NetworkBehaviour 
{

    public float m_Speed = 1.0f;

    public Renderer m_Renderer;

    public GameObject m_ClientHitRegister;
    public GameObject m_HitRegister;
    
    private Rigidbody _body;
    private BoxCollider _hitbox;

    private NetworkVariable<float> _spawnTime = new NetworkVariable<float>(0.0f);
    private NetworkVariable<Vector3> _serverPosition = new NetworkVariable<Vector3>(Vector3.zero);

    private NetworkServerOverridePosition _positionOverride = new NetworkServerOverridePosition(1.0f, 1.5f, 0.5f);

    private bool _activatedSinceLastNetworkSpawn = false;
    private bool _hitRegisteredSinceNetworkSpawn = false;

    private bool _clientRegisteredHit = false;

    private GameObject _clientHitRegisterEffect;

    private void Start()
    {
        _body = GetComponent<Rigidbody>();
        _hitbox = GetComponent<BoxCollider>();
    }

    public override void OnNetworkSpawn()
    {
        ResetRigidBody(); 

        if (m_Renderer != null)
        {
            m_Renderer.enabled = false;
        }

        _hitRegisteredSinceNetworkSpawn = false;
        _clientRegisteredHit = false;
        _clientHitRegisterEffect = null;
        _activatedSinceLastNetworkSpawn = false;
        _spawnTime.OnValueChanged += OnSpawnTimeChanged;
    }

    private void ResetRigidBody()
    {
        if (_body != null)
        {
            _body.isKinematic = true;
            _body.velocity = new Vector3(0f, 0f, 0f);
            _body.angularVelocity = new Vector3(0f, 0f, 0f);
        }

        if (_hitbox != null)
        {
            _hitbox.enabled = false;
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
        _hitbox.enabled = true;
        _body.isKinematic = false;
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
        if (_activatedSinceLastNetworkSpawn)
        {
            if (_positionOverride.ShouldOverride())
            {
                Debug.DrawLine(transform.position, _serverPosition.Value, Color.red);
            }
            else
            {
                Debug.DrawLine(transform.position, _serverPosition.Value, Color.blue);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ResetRigidBody();
        if (NetworkManager.Singleton.IsServer)
        {
            _hitRegisteredSinceNetworkSpawn = true;
            OnCollisionRegisteredClientRpc(collision.transform.position, collision.transform.rotation);
            StartCoroutine(WaitToDespawn(3.0f));
        }
        else
        {
            _clientRegisteredHit = true;
            _clientHitRegisterEffect = Instantiate(m_ClientHitRegister, collision.transform.position, collision.transform.rotation);
        }
    }

    private IEnumerator WaitToDespawn(float timeToWait)
    {
        yield return new WaitForSeconds(timeToWait);
        GetComponent<NetworkObject>().Despawn();
    } 

    [ClientRpc]
    private void OnCollisionRegisteredClientRpc(Vector3 position, Quaternion rotation)
    {
        ResetRigidBody();
        _hitRegisteredSinceNetworkSpawn = true;
        m_Renderer.enabled = false;
        Instantiate(m_HitRegister, position, rotation);
        if (_clientHitRegisterEffect != null)
        {
            Destroy(_clientHitRegisterEffect);
        }
    }

    private void FixedUpdate()
    {
        if (!GameManagerBehaviour.GameBegun || !_activatedSinceLastNetworkSpawn || _hitRegisteredSinceNetworkSpawn) return;
        
        if (NetworkManager.Singleton.IsServer)
        {
            _body.MovePosition(transform.position + (transform.forward * m_Speed * Time.fixedDeltaTime));
            _serverPosition.Value = _body.transform.position;
        } else if (NetworkManager.Singleton.IsClient)
        {
            if (!_clientRegisteredHit)
            {
                _body.MovePosition(transform.position + (transform.forward * m_Speed * Time.fixedDeltaTime));
            }

            var distance = (_body.transform.position - _serverPosition.Value).magnitude;
            if(_positionOverride.CheckForRequiredServerOverride(_body.transform.position, _serverPosition.Value, out var updated, distance, Time.deltaTime))
            {
                _body.transform.position = updated;
            }
        }
    }
}
