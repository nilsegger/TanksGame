using Unity.Netcode;
using UnityEngine;

namespace Networking.Player.Look
{
    public abstract class PlayerLookCommon : NetworkBehaviour
    {
        public Transform m_Turret;
        public float m_RotationSpeed = 30.0f;
   
        protected bool _lockedMovement = false;
   
        public void LockMovement()
        {
            _lockedMovement = true;
        }

        public void UnlockMovement()
        {
            _lockedMovement = false;
        }
    
    }
}
