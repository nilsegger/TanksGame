using Unity.Netcode;
using Networking.Player.Navigation;
using UnityEngine;

public class PlayerNavigationServer : PlayerNavigationCommon
{
    public PlayerNavigationClient client;
    public float m_ServerCorrectionRotationSpeed = 15.0f;
    private Quaternion _ownerRotationTarget = Quaternion.identity;
    
    // When shooting, server will still be allowed to update position for this amount of time
    public float m_LockedMovementUpdateBufferTimeS = 0.1f;
    private float _lockedMovementBufferCountdown = 0.0f;
    
    protected override Vector3 NextPathPoint()
    {
        if (client.agentDestination.Value != Vector3.zero) return client.agentDestination.Value;
        return _agent.transform.position;
    }

    void Update()
    {
        if (!GameManagerBehaviour.GameBegun || !NetworkManager.Singleton.IsServer) return;

        if (_lockedMovement && _lockedMovementBufferCountdown <= 0.0f) return;

        client.serverPosition.Value = transform.position;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, _ownerRotationTarget,
            m_ServerCorrectionRotationSpeed * Time.deltaTime);
        
        client.serverRotation.Value = transform.rotation.eulerAngles.y;
        
        FollowPath();

        if (_lockedMovementBufferCountdown > 0.0f) _lockedMovementBufferCountdown -= Time.deltaTime;
    }

    [ServerRpc]
    public void ClientPushNewNavDestinationServerRpc(Vector3 destination)
    {
        if (GameManagerBehaviour.GameBegun && !_lockedMovement)
        {
            client.agentDestination.Value = destination;
        }
    }
    
    [ServerRpc]
    public  void ClientPushRotationTargetServerRpc(float yRotation)
    {
        if (!GameManagerBehaviour.GameBegun) return;
        _ownerRotationTarget = Quaternion.Euler(0.0f, yRotation, 0.0f);
    }
   
    public void UpdateDestinationForShot(Vector3 position, float maxCorrectionMagnitude)
    {
        var forward = position - transform.position;
        if (forward.magnitude <= maxCorrectionMagnitude)
        {
            client.agentDestination.Value = position;
        }
        else
        {
            forward.Normalize();
            var correctedPosition = transform.position + forward  * maxCorrectionMagnitude;
            client.agentDestination.Value = correctedPosition;
        }

        _lockedMovementBufferCountdown = m_LockedMovementUpdateBufferTimeS;
    }

    
}
