using System.Collections;
using Networking.Player.Look;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Player.Shoot
{
    public abstract class PlayerShootCommon : NetworkBehaviour
    {

        public GameObject m_ShellPrefab;
        public GameObject m_ShellSpawnPosition;

        public float m_MaxRangeHitRaycast = 22.0f;

        public Animator m_TurretAnimator;
        public GameObject m_Turret;
        public AnimationClip m_ShootAnimation;

        public float maxAngleCorrectionOnShootStop = 10.0f;
        public float maxPositionCorrectionOnShootStop = 1.0f;

        public float m_ShootCooldown = 3.0f;
        public float m_ShootWarmUp = 1.0f;

        protected float _cooldown = 0.0f;
   
        protected void DisplayHitMarker()
        {
            var ray = new Ray(m_ShellSpawnPosition.transform.position, m_ShellSpawnPosition.transform.forward);
            if (!Physics.Raycast(ray, out var hit, m_MaxRangeHitRaycast))
            {
                return;
            }

            if (!hit.collider.gameObject.CompareTag("HitboxPlayer")) return;
            
            var behaviour = hit.collider.gameObject.GetComponent<HitboxBehaviour>();
            behaviour.SetTargeted();
        }

        protected void SetShootAnimation(float durationS)
        {
            m_TurretAnimator.SetTrigger("Shoot");
            var animationSpeedup = m_ShootAnimation.length / durationS;
            m_TurretAnimator.speed = animationSpeedup;
        }
    }
}
