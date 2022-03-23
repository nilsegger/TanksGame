using UnityEngine;

namespace Networking.Player.Look
{
    [CreateAssetMenu(fileName = "PlayerLookData", menuName = "ScriptableObjects/PlayerLookData", order = 1)]
    public class PlayerLookData : ScriptableObject 
    {
        public float RotationSpeed = 30.0f;
    }
}
