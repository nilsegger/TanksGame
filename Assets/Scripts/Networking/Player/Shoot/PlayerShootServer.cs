using System;
using System.Collections;
using Networking.Player.Look;
using Unity.Netcode;
using UnityEngine;

namespace Networking.Player.Shoot
{
    public class PlayerShootServer : PlayerShootCommon
    {

        private PlayerShootClient _client;

        private PlayerNavigationServer _serverNavigation;
        private PlayerLookServer _turretRotationServer;

        private void Start()
        {
            _client = GetComponent<PlayerShootClient>();
            _serverNavigation = GetComponent<PlayerNavigationServer>();
            _turretRotationServer = GetComponent<PlayerLookServer>();
        }

        void Update()
        {
            if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsServer) return;

            if (_cooldown > 0.0f) _cooldown -= Time.deltaTime;

            DisplayHitMarker();
        }

        private void ServerSpawnShell(float spawnTime)
        {
            var shell = NetworkPool.Singleton.GetNetworkObject(data.shellPrefab, m_ShellSpawnPosition.transform.position, m_ShellSpawnPosition.transform.rotation);
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
        
            _serverNavigation.UnlockMovement();
            _turretRotationServer.UnlockMovement();
        }

        [ServerRpc]
        public void ShootServerRpc(double atTime, Vector3 position, float localRotationY)
        {
            if (!GameManagerBehaviour.GameBegun) return;
            if (_cooldown > 0.0f) return;
            var waitTime = atTime - NetworkManager.ServerTime.Time;
            SetShootAnimation((float) waitTime);
            ServerSpawnShell((float) atTime);
        
            _serverNavigation.LockMovement();
            _serverNavigation.UpdateDestinationForShot(position, data.maxPositionCorrectionOnShootStop);
        
            _turretRotationServer.LockMovement();
            _turretRotationServer.UpdateDestinationForShot(localRotationY, data.maxAngleCorrectionOnShootStop);

            _client.StartShootAnimationClientRpc((float)atTime);
            StartCoroutine(WaitToUnlockMovement((float) waitTime));

            _cooldown = data.shootCooldown;
        }
    }
}
