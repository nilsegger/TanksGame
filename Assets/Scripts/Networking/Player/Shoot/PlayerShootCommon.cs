using System.Collections;
using Networking.Player.Look;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Networking.Player.Shoot
{
    public abstract class PlayerShootCommon : NetworkBehaviour
    {

        public Transform m_ShellSpawnPosition;
        public Transform m_Turret;
        public Animator turretAnimator;

        public PlayerShootData data;

        protected float _cooldown = 0.0f;
   
        protected void DisplayHitMarker()
        {
            var ray = new Ray(m_ShellSpawnPosition.position, m_ShellSpawnPosition.forward);
            if (!Physics.Raycast(ray, out var hit, data.maxRangeHitRaycast))
            {
                return;
            }

            if (!hit.collider.gameObject.CompareTag("HitboxPlayer")) return;
            
            var behaviour = hit.collider.gameObject.GetComponent<HitboxBehaviour>();
            behaviour.SetTargeted();
        }

        protected void SetShootAnimation(float durationS)
        {
            turretAnimator.SetTrigger("Shoot");
            var animationSpeedup = data.shootAnimation.length / durationS;
            turretAnimator.speed = animationSpeedup;
        }
        
        private void OnValidate()
        {
            Assert.IsNotNull(gameObject.GetComponent<PlayerShootClient>());     
            Assert.IsNotNull(gameObject.GetComponent<PlayerShootServer>());     
        }
    }
}
