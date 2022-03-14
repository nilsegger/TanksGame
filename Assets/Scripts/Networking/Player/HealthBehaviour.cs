using System;
using Unity.Netcode;
using UnityEngine;

public enum TankCollisionBox
{
    BODY,
    TURRET,
    CANISTER
}

public class HealthBehaviour : NetworkBehaviour 
{
    // Start is called before the first frame update
    public Transform healthBar;
    private Vector3 _scale;

    private NetworkVariable<float> _health = new NetworkVariable<float>(100.0f);

    private void Start()
    {
        _scale = healthBar.localScale;
    }

    public override void OnNetworkSpawn()
    {
        _health.OnValueChanged += OnHealthChange;
    }

    public override void OnNetworkDespawn()
    {
        _health.OnValueChanged -= OnHealthChange;
    }

    private void OnHealthChange(float old, float newVal)
    {
        healthBar.localScale = new Vector3(_scale.x / 100.0f * _health.Value, _scale.y, _scale.z); 
    }

    public void OnCollision(TankCollisionBox part)
    {
        if (!NetworkManager.Singleton.IsServer || !GameManagerBehaviour.GameBegun) return;
        switch (part)
        {
           case TankCollisionBox.BODY:
               _health.Value -= 15.0f;
               break;
           case TankCollisionBox.TURRET:
               _health.Value -= 5.0f;
               break;
           case TankCollisionBox.CANISTER:
               _health.Value -= 25.0f;
               break;
        }
    }

}
