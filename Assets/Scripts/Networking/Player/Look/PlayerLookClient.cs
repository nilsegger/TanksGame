using Unity.Netcode;
using UnityEngine;

namespace Networking.Player.Look
{
    public class PlayerLookClient : PlayerLookCommon 
    {
        public ButtonPressBehaviour m_RotateLeftButton;
        public ButtonPressBehaviour m_RotateRightButton;

        public PlayerLookServer server;

        // Used to display server position in debug
        public PlayerNavigationClient NavigationClient;
    
        [HideInInspector]
        public NetworkVariable<float> serverRotation = new NetworkVariable<float>(); // this is the local y rotation in euler
       
        private NetworkServerOverrideDegrees _serverRotationOverride = new NetworkServerOverrideDegrees();
    
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
            if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsClient) return;
    
            if(IsOwner)
            {
                var rotateLeft = (Input.GetKey("q") || m_RotateLeftButton.isPressed) && !_lockedMovement;
                var rotateRight = (Input.GetKey("e") || m_RotateRightButton.isPressed) && !_lockedMovement;
    
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
                    server.ReceiveClientRotationServerRpc(m_Turret.localRotation.eulerAngles.y);
                }
            } else
            {
                m_Turret.localRotation = Quaternion.RotateTowards(m_Turret.localRotation, ServerRotation(), m_RotationSpeed * Time.deltaTime);
            }
            
            _serverRotationOverride.Activate(IsOwner ? "moving" : "client");
    
            var angleOffset = Mathf.Abs(serverRotation.Value - m_Turret.localRotation.eulerAngles.y);
            if (_serverRotationOverride.CheckForRequiredServerOverride(m_Turret.transform.localRotation.eulerAngles.y, serverRotation.Value, out var updatedEulerAngle, angleOffset))
            {
                m_Turret.transform.localRotation= Quaternion.Euler(m_Turret.transform.localRotation.eulerAngles.x, updatedEulerAngle, m_Turret.transform.localRotation.eulerAngles.z);
            }
    
            Debug.DrawLine(m_Turret.position, m_Turret.position + ServerGlobalRotation() * Vector3.forward, _serverRotationOverride.IsOverrideDistance(angleOffset) ? Color.red : Color.white);
        }
    
        private Quaternion ServerRotation()
        {
            return Quaternion.Euler(0.0f, serverRotation.Value, 0.0f);
        }
    
        private Quaternion ServerGlobalRotation()
        {
            return Quaternion.Euler(new Vector3(0.0f, NavigationClient.serverRotation.Value, 0.0f) + ServerRotation().eulerAngles);
        }
    }
}
