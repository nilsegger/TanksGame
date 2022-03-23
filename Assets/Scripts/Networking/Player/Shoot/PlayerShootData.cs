using UnityEngine;

namespace Networking.Player.Shoot
{
    [CreateAssetMenu(fileName = "PlayerShootData", menuName = "ScriptableObjects/PlayerShootData", order = 1)]
    public class PlayerShootData : ScriptableObject 
    {
        public float maxRangeHitRaycast = 22.0f;
        public GameObject shellPrefab;
        public AnimationClip shootAnimation;
        public float maxAngleCorrectionOnShootStop = 10.0f;
        public float maxPositionCorrectionOnShootStop = 1.0f;
        public float shootCooldown = 3.0f;
        public float shootWarmUp = 1.0f;
    }
}
