using System.Collections;
using Networking.Player.Look;
using Unity.Netcode;
using UnityEngine;

namespace Networking.Player.Shoot
{
    public class PlayerShootServer : PlayerShootCommon
    {

        public PlayerShootClient client;

        public PlayerNavigationServer serverNavigation;
        public PlayerLookServer turretRotationServer;
    
        void Update()
        {
            if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsServer) return;

            if (_cooldown > 0.0f) _cooldown -= Time.deltaTime;

            DisplayHitMarker();
        }

        private void ServerSpawnShell(float spawnTime)
        {
            var shell = NetworkPool.Singleton.GetNetworkObject(m_ShellPrefab, m_ShellSpawnPosition.transform.position, m_ShellSpawnPosition.transform.rotation);
            shell.SpawnWithOwnership(OwnerClientId);
        
            var shellBehaviour = shell.gameObject.GetComponent<NetworkedShellBehaviour>();
            shellBehaviour.SetSpawnTime(spawnTime);
        }
    
        private IEnumerator WaitToUnlockMovement(float waitTime)
        {
            if (waitTime > 0.0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
        
            serverNavigation.UnlockMovement();
            turretRotationServer.UnlockMovement();
        }

        [ServerRpc]
        public void ShootServerRpc(double atTime, Vector3 position, float localRotationY)
        {
            if (!GameManagerBehaviour.GameBegun) return;
            if (_cooldown > 0.0f) return;
            var waitTime = atTime - NetworkManager.ServerTime.Time;
            SetShootAnimation((float) waitTime);
            ServerSpawnShell((float) atTime);
        
            serverNavigation.LockMovement();
            serverNavigation.UpdateDestinationForShot(position, maxPositionCorrectionOnShootStop);
        
            turretRotationServer.LockMovement();
            turretRotationServer.UpdateDestinationForShot(localRotationY, maxAngleCorrectionOnShootStop);

            client.StartShootAnimationClientRpc((float)atTime);
            StartCoroutine(WaitToUnlockMovement((float) waitTime));

            _cooldown = m_ShootCooldown;
        }
    }
}
