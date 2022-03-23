using Networking.Player.Look;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Networking.Player.Navigation
{
    public abstract class PlayerNavigationCommon : NetworkBehaviour 
    {
        protected NavMeshAgent _agent;

        public PlayerNavigationData playerNavigationData; 
    
        protected bool _lockedMovement = false;
        
        protected virtual void Start()
        {
            _agent = gameObject.GetComponent<NavMeshAgent>();
        }

        public override void OnNetworkSpawn()
        {
            if (_agent == null)
            {
                _agent = gameObject.GetComponent<NavMeshAgent>();
            }
        }

        protected abstract Vector3 NextPathPoint();
  
        protected bool IsMoving()
        {
            var toCorner = NextPathPoint() - _agent.transform.position;
            return toCorner.sqrMagnitude > 0.01;
        }
        
        protected void FollowPath()
        {
            var toCorner = NextPathPoint() - _agent.transform.position;
            RotateTowardsPath(toCorner, out float slowDown);

            var relativeSpeed = toCorner.normalized * playerNavigationData.movementSpeed * Time.deltaTime * slowDown;
            // this clamps the forward movement vector to point if toCorner is already less
            if (toCorner.sqrMagnitude < relativeSpeed.sqrMagnitude) relativeSpeed = toCorner;
            _agent.Move(relativeSpeed);
        }
    
        // returns true if player is allowed to drive
        private void RotateTowardsPath(Vector3 toCorner, out float slowDown)
        {
            float angle = Vector3.Angle(transform.forward, toCorner);
            if (angle > 1.0f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toCorner, Vector3.up), playerNavigationData.rotationSpeed * Time.deltaTime);
            }
            slowDown = playerNavigationData.turnCurve.Evaluate(1.0f / 180.0f * (180.0f - angle));
        }

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
            Assert.IsNotNull(gameObject.GetComponent<PlayerNavigationClient>());     
            Assert.IsNotNull(gameObject.GetComponent<PlayerNavigationServer>());     
        }
    }
}
