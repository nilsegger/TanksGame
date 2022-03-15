using System;
using Unity.Netcode;
using UnityEngine;

public class LookBehaviour : NetworkBehaviour
{
    public Transform m_Turret;
    public float m_RotationSpeed = 30.0f;

    public ButtonPressBehaviour m_RotateLeftButton;
    public ButtonPressBehaviour m_RotateRightButton;

    private NetworkVariable<float> _serverRotation = new NetworkVariable<float>(); // this is the local y rotation in euler
   
    private NetworkServerOverrideDegrees _serverRotationOverride = new NetworkServerOverrideDegrees();
    
    private Quaternion _rotationDestination;

    private bool _lockedMovement = false;

    private void Start()
    {
        _serverRotationOverride.AddSetting("moving", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 2.0f, MaxAllowedDelta = 15.0f});
        _serverRotationOverride.AddSetting("spawn", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 0.0f, MaxAllowedDelta = 0f});
        _serverRotationOverride.AddSetting("client", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 1.0f, MaxAllowedDelta = 5.0f});
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsClient && !IsOwner)
        {
            _serverRotationOverride.Activate("spawn", true);
        }
    }

    void Update()
    {
        if (!GameManagerBehaviour.GameBegun) return;

        if (NetworkManager.Singleton.IsServer)
        {
            m_Turret.localRotation = Quaternion.RotateTowards(m_Turret.localRotation, _rotationDestination, m_RotationSpeed * Time.deltaTime);
            _serverRotation.Value = m_Turret.localRotation.eulerAngles.y;
            return;
        } else if (NetworkManager.Singleton.IsClient && IsOwner)
        {
            bool rotateLeft = (Input.GetKey("q") || m_RotateLeftButton.isPressed) && !_lockedMovement;
            bool rotateRight = (Input.GetKey("e") || m_RotateRightButton.isPressed) && !_lockedMovement;

            if (rotateLeft)
            {
                m_Turret.Rotate(Vector3.up, -m_RotationSpeed * Time.deltaTime);
            }

            if (rotateRight)
            {
                m_Turret.Rotate(Vector3.up, m_RotationSpeed * Time.deltaTime);
            }

            if ((rotateLeft || rotateRight) && !(rotateLeft && rotateRight))
            {
                ClientPushNewRotationServerRpc(m_Turret.localRotation.eulerAngles.y);
            }

            Debug.DrawLine(m_Turret.position, m_Turret.position + ServerGlobalRotation() * Vector3.forward, Color.white);
        } else if (NetworkManager.Singleton.IsClient && !IsOwner)
        {
            m_Turret.localRotation = Quaternion.RotateTowards(m_Turret.localRotation, ServerRotation(), m_RotationSpeed * Time.deltaTime);
        }
        
        _serverRotationOverride.Activate(IsOwner ? "moving" : "client");

        var angleOffset = Mathf.Abs(_serverRotation.Value - m_Turret.localRotation.eulerAngles.y);
        if (_serverRotationOverride.CheckForRequiredServerOverride(m_Turret.transform.localRotation.eulerAngles.y, _serverRotation.Value, out var updatedEulerAngle, angleOffset, Time.deltaTime))
        {
            m_Turret.transform.localRotation= Quaternion.Euler(m_Turret.transform.localRotation.eulerAngles.x, updatedEulerAngle, m_Turret.transform.localRotation.eulerAngles.z);
        }

        if (_serverRotationOverride.IsOverrideDistance(angleOffset))
        {
            Debug.DrawLine(m_Turret.position, m_Turret.position + ServerGlobalRotation() * Vector3.forward, Color.red);
        }
    }

    private Quaternion ServerRotation()
    {
        return Quaternion.Euler(0.0f, _serverRotation.Value, 0.0f);
    }

    // This isnt 100% correct since the parent of m_Turret will have a different position / rotation on the server
    private Quaternion ServerGlobalRotation()
    {
        return Quaternion.Euler(m_Turret.transform.parent.rotation.eulerAngles + ServerRotation().eulerAngles);
    }

    [ServerRpc]
    private void ClientPushNewRotationServerRpc(float rotation)
    {
        if (!GameManagerBehaviour.GameBegun || _lockedMovement) return;
        _rotationDestination = Quaternion.Euler(0.0f, rotation, 0.0f);
    }

    public void UpdateDestinationForShot(float localRotationY, float maxCorrection)
    {
        if (Mathf.Abs(m_Turret.transform.localRotation.eulerAngles.y - localRotationY) <= maxCorrection)
        {
            _rotationDestination = Quaternion.Euler(0.0f, localRotationY, 0.0f);
        }
        else
        {
            var angle = m_Turret.transform.localRotation.eulerAngles.y + Mathf.Sign(localRotationY - m_Turret.transform.localRotation.eulerAngles.y) * maxCorrection;
            _rotationDestination = Quaternion.Euler(0.0f, angle, 0.0f);
        }
    }
    
    public void LockMovement()
    {
        _lockedMovement = true;
    }

    public void UnlockMovement()
    {
        _lockedMovement = false;
    }
    
}
