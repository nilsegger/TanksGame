using UnityEngine;
using Unity.Netcode;

namespace Networking.Player.Look
{
    public class PlayerLookServer : PlayerLookCommon 
    {
        private Quaternion _clientRotation;
        public PlayerLookClient client;
    
        void Update()
        {
            if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsServer || _lockedMovement) return;
    
            m_Turret.localRotation = Quaternion.RotateTowards(m_Turret.localRotation, _clientRotation, m_RotationSpeed * Time.deltaTime);
            client.serverRotation.Value = m_Turret.localRotation.eulerAngles.y;
        }
   
        [ServerRpc]
        public void ReceiveClientRotationServerRpc(float rotation)
        {
            _clientRotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
    
        public void UpdateDestinationForShot(float localRotationY, float maxCorrection)
        {
            _clientRotation = Quaternion.Euler(0.0f, localRotationY, 0.0f);
            var destination = Quaternion.RotateTowards(m_Turret.localRotation, _clientRotation, maxCorrection);
            m_Turret.localRotation = destination;
        }
    }
}
