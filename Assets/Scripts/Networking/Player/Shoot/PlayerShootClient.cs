using System.Collections;
using Networking.Player.Look;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Player.Shoot
{
    public class PlayerShootClient : PlayerShootCommon 
    {
        public Button m_ShootButton;
        private bool _shootButtonPressed = false;

        private PlayerShootServer _server;

        private PlayerNavigationClient _clientNavigation;
        private PlayerLookClient _turretRotationClient;
    
        // Start is called before the first frame update
        void Start()
        {
            _server = GetComponent<PlayerShootServer>();
            _clientNavigation = GetComponent<PlayerNavigationClient>();
            _turretRotationClient = GetComponent<PlayerLookClient>();
            
            m_ShootButton.onClick.AddListener(() => _shootButtonPressed = true) ;
        }

        void Update()
        {
            if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsClient) return;

            if (IsOwner)
            {
                if ((Input.GetKey("space") || _shootButtonPressed) && _cooldown <= 0.0f)
                {
                    _cooldown = data.shootCooldown;
                    _shootButtonPressed = false;
                    var shootShellAt = NetworkManager.LocalTime.Time + data.shootWarmUp;
                    SetShootAnimation(data.shootWarmUp); 
                    _server.ShootServerRpc(shootShellAt, transform.position, m_Turret.localRotation.eulerAngles.y);

                    _clientNavigation.Halt();
                    _clientNavigation.LockMovement();
                
                    _turretRotationClient.LockMovement();

                    StartCoroutine(WaitToUnlockMovement((float)(shootShellAt - NetworkManager.LocalTime.Time)));
                }
                else
                {
                    _cooldown -= Time.deltaTime;
                }
            } 

            DisplayHitMarker();
        }
   
        private IEnumerator WaitToUnlockMovement(float waitTime)
        {
            if (waitTime > 0.0f)
            {
                yield return new WaitForSeconds(waitTime);
            }
        
            _clientNavigation.UnlockMovement();
            _turretRotationClient.UnlockMovement();
        }

        [ClientRpc]
        public void StartShootAnimationClientRpc(float finishAtTime)
        {
            if (!IsOwner)
            {
                SetShootAnimation(finishAtTime - (float) NetworkManager.ServerTime.Time);
            }
        }

    }
}
