using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxBehaviour : MonoBehaviour
{
    public bool singleHit = false;
    public TankCollisionBox type;
    public HealthBehaviour healthBehaviour;

    private bool _hit = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (singleHit && _hit) return;
        healthBehaviour.OnCollision(type);
        _hit = true;
    }
}
