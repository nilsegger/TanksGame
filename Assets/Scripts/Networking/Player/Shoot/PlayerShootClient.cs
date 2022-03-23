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

        public PlayerShootServer server;

        public PlayerNavigationClient clientNavigation;
        public PlayerLookClient turretRotationClient;
    
        // Start is called before the first frame update
        void Start()
        {
            m_ShootButton.onClick.AddListener(() => _shootButtonPressed = true) ;
        }

        void Update()
        {
            if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsClient) return;

            if (IsOwner)
            {
                if ((Input.GetKey("space") || _shootButtonPressed) && _cooldown <= 0.0f)
                {
                    _cooldown = m_ShootCooldown;
                    _shootButtonPressed = false;
                    var shootShellAt = NetworkManager.LocalTime.Time + m_ShootWarmUp;
                    SetShootAnimation(m_ShootWarmUp); 
                    server.ShootServerRpc(shootShellAt, transform.position, m_Turret.transform.localRotation.eulerAngles.y);

                    clientNavigation.Halt();
                    clientNavigation.LockMovement();
                
                    turretRotationClient.LockMovement();

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
        
            clientNavigation.UnlockMovement();
            turretRotationClient.UnlockMovement();
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
