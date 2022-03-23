using UnityEngine;

namespace Networking.Player.Navigation
{
    [CreateAssetMenu(fileName = "PlayerNavigationData", menuName = "ScriptableObjects/PlayerNavigationData", order = 1)]
    public class PlayerNavigationData : ScriptableObject 
    {
        public float rotationSpeed = 45.0f;
        public float movementSpeed = 3.0f;
        public AnimationCurve turnCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }
}
