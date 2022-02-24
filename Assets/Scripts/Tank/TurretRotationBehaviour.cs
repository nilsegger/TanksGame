using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class TurretRotationBehaviour : NetworkBehaviour
{
    public Transform m_Turret;
    public float m_RotationSpeed = 30.0f;
    public float m_ServerRotationIncrease = 1.1f;

    private NetworkVariable<float> _serverRotation = new NetworkVariable<float>(); // this is the y rotation in euler
   
    [SerializeField]
    private NetworkServerOverrideFloat _networkServerOverride = new NetworkServerOverrideFloat(3.0f, 20.0f, 1.0f);
    
    private Quaternion _rotationDestination;

    private float _lastRotation = 0.0f;

    void Update()
    {
        if (!GameManagerBehaviour.GameBegun) return;

        if (NetworkManager.Singleton.IsServer)
        {
            m_Turret.rotation = Quaternion.RotateTowards(m_Turret.rotation, _rotationDestination, m_RotationSpeed * Time.deltaTime * m_ServerRotationIncrease);
            _serverRotation.Value = m_Turret.rotation.eulerAngles.y;
            return;
        } else if (NetworkManager.Singleton.IsClient && IsOwner)
        {
            bool rotateLeft = Input.GetKey("q");
            bool rotateRight = Input.GetKey("e");

            if (rotateLeft)
            {
                m_Turret.Rotate(Vector3.up, -m_RotationSpeed * Time.deltaTime);
            }

            if (rotateRight)
            {
                m_Turret.Rotate(Vector3.up, m_RotationSpeed * Time.deltaTime);
            }

            // if both left and right at the same time, the turret stays at the same place
            if (_lastRotation != m_Turret.rotation.eulerAngles.y)
            {
                _lastRotation = m_Turret.rotation.eulerAngles.y;
                ClientPushNewRotationServerRpc(m_Turret.rotation.eulerAngles.y);
            }

            Debug.DrawLine(m_Turret.position, m_Turret.position + ServerRotation() * Vector3.forward, Color.white);
        } else if (NetworkManager.Singleton.IsClient && !IsOwner)
        {
            m_Turret.rotation = Quaternion.RotateTowards(m_Turret.rotation, ServerRotation(), m_RotationSpeed * Time.deltaTime);
        }

        float updatedEulerAngle;
        var angleOffset = Mathf.Abs(_serverRotation.Value - m_Turret.transform.eulerAngles.y);
        if (_networkServerOverride.CheckForRequiredServerOverride(m_Turret.transform.rotation.eulerAngles.y, _serverRotation.Value, out updatedEulerAngle, angleOffset, Time.deltaTime))
        {
            m_Turret.transform.rotation = Quaternion.Euler(m_Turret.transform.rotation.eulerAngles.x, updatedEulerAngle, m_Turret.transform.rotation.eulerAngles.z);
        }

        if (_networkServerOverride.IsOverrideDistance(angleOffset))
        {
            Debug.DrawLine(m_Turret.position, m_Turret.position + ServerRotation() * Vector3.forward, Color.red);
        }
    }

    private Quaternion ServerRotation()
    {
        return Quaternion.Euler(0.0f, _serverRotation.Value, 0.0f);
    }

    [ServerRpc]
    private void ClientPushNewRotationServerRpc(float rotation)
    {
        if (!GameManagerBehaviour.GameBegun) return;
        _rotationDestination = Quaternion.Euler(0.0f, rotation, 0.0f);
    }
    
}
