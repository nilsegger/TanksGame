using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TurretRotationBehaviour : NetworkBehaviour 
{
    public float m_RotationSpeed = 30.0f;

    private NetworkVariable<Quaternion> _serverRotation;
    private NetworkServerOverrideRotation _networkServerOverride;
    private Quaternion _destination;

    void Start()
    {
        _serverRotation.OnValueChanged += OnServerRotationChanged;
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManagerBehaviour.GameBegun) return;

        if (NetworkManager.Singleton.IsServer)
        {
            
            _serverRotation.Value = transform.rotation;
        } else if (NetworkManager.Singleton.IsClient && IsOwner)
        {
            bool rotateLeft = Input.GetKey("q");
            bool rotateRight = Input.GetKey("e");

            if (rotateLeft)
            {
                transform.Rotate(Vector3.up, -m_RotationSpeed * Time.deltaTime);
            }

            if (rotateRight)
            {
                transform.Rotate(Vector3.up, m_RotationSpeed * Time.deltaTime);
            }

            // if both left and right at the same time, the turret stays at the same place
            if ((rotateLeft || rotateRight) && !(rotateLeft && rotateRight))
            {
                ClientPushNewRotationServerRpc(transform.rotation);
            }
        }
        
    }

    [ServerRpc]
    private void ClientPushNewRotationServerRpc(Quaternion rotation)
    {
        if (!GameManagerBehaviour.GameBegun) return;
        _destination = rotation;
    }

    private void OnServerRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        
    }
}
