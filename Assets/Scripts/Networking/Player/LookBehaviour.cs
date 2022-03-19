using System;
using Unity.Netcode;
using UnityEngine;

public class LookBehaviour : NetworkBehaviour
{
    public Transform m_YRotationTransform;
    public float m_RotationSpeed = 30.0f;
    
    public Transform m_PitchTransform;
    public float m_PitchSpeed = 10.0f;
    public float m_MaxPitch = 30.0f;

    public ButtonPressBehaviour m_RotateLeftButton;
    public ButtonPressBehaviour m_RotateRightButton;

    private NetworkVariable<float> _serverRotation = new NetworkVariable<float>(); // this is the local y rotation in euler
    private NetworkVariable<float> _serverPitch = new NetworkVariable<float>(); // local x rotation (euler)
   
    private NetworkServerOverrideDegrees _serverRotationOverride = new NetworkServerOverrideDegrees();
    private NetworkServerOverrideDegrees _serverPitchOverride = new NetworkServerOverrideDegrees();
    
    private Quaternion _rotationDestination;
    private Quaternion _pitchDestination;

    private bool _lockedMovement = false;

    private void Start()
    {
        _serverRotationOverride.AddSetting("moving", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 2.0f, MaxAllowedDelta = 15.0f});
        _serverRotationOverride.AddSetting("spawn", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 0.0f, MaxAllowedDelta = 0f});
        _serverRotationOverride.AddSetting("client", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 1.0f, MaxAllowedDelta = 5.0f});
        
        _serverPitchOverride.AddSetting("moving", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 2.0f, MaxAllowedDelta = 5.0f});
        _serverPitchOverride.AddSetting("spawn", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = 0.0f, MaxAllowedDelta = 0f});
        _serverPitchOverride.AddSetting("client", new NetworkServerOverrideSettings {InterpolationDuration = 1.0f, ResetPositionAfterMismatchTime = .5f, MaxAllowedDelta = 1.0f});
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsClient && !IsOwner)
        {
            _serverRotationOverride.Activate("spawn", true);
        }
    }
    
    float ClampAngle(float angle, float from, float to)
    {
        // accepts e.g. -80, 80
        if (angle < 0f) angle = 360 + angle;
        if (angle > 180f) return Mathf.Max(angle, 360+from);
        return Mathf.Min(angle, to);
    }

    void Update()
    {
        if (!GameManagerBehaviour.GameBegun) return;

        if (NetworkManager.Singleton.IsServer)
        {
            m_YRotationTransform.localRotation = Quaternion.RotateTowards(m_YRotationTransform.localRotation, _rotationDestination, m_RotationSpeed * Time.deltaTime);
            _serverRotation.Value = m_YRotationTransform.localRotation.eulerAngles.y;
            
            m_PitchTransform.localRotation = Quaternion.RotateTowards(m_PitchTransform.localRotation, _pitchDestination, m_PitchSpeed * Time.deltaTime);
            m_PitchTransform.localRotation = Quaternion.Euler(ClampAngle(m_PitchTransform.localRotation.eulerAngles.x, -m_MaxPitch, m_MaxPitch), 0.0f, 0.0f);
            _serverPitch.Value = m_PitchTransform.localRotation.eulerAngles.x;
            return;
        } else if (NetworkManager.Singleton.IsClient && IsOwner)
        {
            bool rotateLeft = (Input.GetKey("q") || m_RotateLeftButton.isPressed) && !_lockedMovement;
            bool rotateRight = (Input.GetKey("e") || m_RotateRightButton.isPressed) && !_lockedMovement;
            bool pitchUp = (Input.GetKey("w")) && !_lockedMovement;
            bool pitchDown = (Input.GetKey("s")) && !_lockedMovement;

            if (rotateLeft)
            {
                m_YRotationTransform.Rotate(Vector3.up, -m_RotationSpeed * Time.deltaTime);
            }

            if (rotateRight)
            {
                m_YRotationTransform.Rotate(Vector3.up, m_RotationSpeed * Time.deltaTime);
            }

            if (pitchUp)
            {
                m_PitchTransform.Rotate(Vector3.right, -m_PitchSpeed * Time.deltaTime);
            }

            if (pitchDown)
            {
                m_PitchTransform.Rotate(Vector3.right, m_PitchSpeed * Time.deltaTime);
            }


            if (((rotateLeft || rotateRight) && !(rotateLeft && rotateRight)) || ((pitchUp || pitchDown) && !(pitchUp && pitchDown)))
            {
                m_PitchTransform.localRotation = Quaternion.Euler(ClampAngle(m_PitchTransform.localRotation.eulerAngles.x, -m_MaxPitch, m_MaxPitch), 0.0f, 0.0f);
                ClientPushNewRotationServerRpc(m_YRotationTransform.localRotation.eulerAngles.y, m_PitchTransform.localRotation.eulerAngles.x);
            }

        } else if (NetworkManager.Singleton.IsClient && !IsOwner)
        {
            m_YRotationTransform.localRotation = Quaternion.RotateTowards(m_YRotationTransform.localRotation, ServerRotation(), m_RotationSpeed * Time.deltaTime);
        }
        
        CheckRotationOverride();
        CheckPitchOverride();
    }

    private void CheckRotationOverride()
    {
        _serverRotationOverride.Activate(IsOwner ? "moving" : "client");

        var angleOffset = Mathf.Abs(_serverRotation.Value - m_YRotationTransform.localRotation.eulerAngles.y);
        if (_serverRotationOverride.CheckForRequiredServerOverride(m_YRotationTransform.transform.localRotation.eulerAngles.y, _serverRotation.Value, out var updatedEulerAngle, angleOffset, Time.deltaTime))
        {
            m_YRotationTransform.transform.localRotation= Quaternion.Euler(m_YRotationTransform.transform.localRotation.eulerAngles.x, updatedEulerAngle, m_YRotationTransform.transform.localRotation.eulerAngles.z);
        }

        Debug.DrawLine(m_YRotationTransform.position, m_YRotationTransform.position + ServerGlobalRotation() * Vector3.forward, _serverRotationOverride.IsOverrideDistance(angleOffset) ? Color.red : Color.cyan);
    }
    
    private void CheckPitchOverride()
    {
        _serverPitchOverride.Activate(IsOwner ? "moving" : "client");

        var angleOffset = Mathf.Abs(_serverPitch.Value - m_PitchTransform.localRotation.eulerAngles.x);
        if (_serverPitchOverride.CheckForRequiredServerOverride(m_PitchTransform.transform.localRotation.eulerAngles.x, _serverPitch.Value, out var updatedEulerAngle, angleOffset, Time.deltaTime))
        {
            m_PitchTransform.transform.localRotation = Quaternion.Euler(updatedEulerAngle, m_PitchTransform.transform.localRotation.eulerAngles.y, m_PitchTransform.transform.localRotation.eulerAngles.z);
        }

        Debug.DrawLine(m_PitchTransform.position, m_PitchTransform.position + ServerGlobalPitch() * Vector3.forward, _serverPitchOverride.IsOverrideDistance(angleOffset) ? Color.red : Color.blue);
    }

    private Quaternion ServerRotation()
    {
        return Quaternion.Euler(0.0f, _serverRotation.Value, 0.0f);
    }

    // This isnt 100% correct since the parent of m_Turret will have a different position / rotation on the server
    private Quaternion ServerGlobalRotation()
    {
        return Quaternion.Euler(m_YRotationTransform.transform.parent.rotation.eulerAngles + ServerRotation().eulerAngles);
    }
    
    private Quaternion ServerPitch()
     {
         return Quaternion.Euler(_serverPitch.Value, 0.0f, 0.0f);
     }
    
    private Quaternion ServerGlobalPitch()
    {
        return Quaternion.Euler(m_PitchTransform.rotation.eulerAngles + ServerPitch().eulerAngles);
    }

    [ServerRpc]
    private void ClientPushNewRotationServerRpc(float rotation, float pitch)
    {
        if (!GameManagerBehaviour.GameBegun || _lockedMovement) return;
        _rotationDestination = Quaternion.Euler(0.0f, rotation, 0.0f);
        _pitchDestination = Quaternion.Euler(pitch, 0.0f, 0.0f);        
    }

    public void UpdateDestinationForShot(float localRotationY, float maxRefactorCorrection)
    {
        if (Mathf.Abs(m_YRotationTransform.transform.localRotation.eulerAngles.y - localRotationY) <= maxRefactorCorrection)
        {
            _rotationDestination = Quaternion.Euler(0.0f, localRotationY, 0.0f);
        }
        else
        {
            var angle = m_YRotationTransform.transform.localRotation.eulerAngles.y + Mathf.Sign(localRotationY - m_YRotationTransform.transform.localRotation.eulerAngles.y) * maxRefactorCorrection;
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
