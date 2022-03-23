using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Networking.Player.Look
{
    public abstract class PlayerLookCommon : NetworkBehaviour
    {
        public Transform m_Turret;

        public PlayerLookData data;
   
        protected bool _lockedMovement = false;
   
        public void LockMovement()
        {
            _lockedMovement = true;
        }

        public void UnlockMovement()
        {
            _lockedMovement = false;
        }

        private void OnValidate()
        {
            Assert.IsNotNull(gameObject.GetComponent<PlayerLookClient>());     
            Assert.IsNotNull(gameObject.GetComponent<PlayerLookServer>());     
        }
    }
}
