using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitboxBehaviour : MonoBehaviour
{
    public bool singleHit = false;
    public TankCollisionBox type;
    public HealthBehaviour healthBehaviour;

    public GameObject cube;
    private Material material;

    private bool _isTargeted = false;
    public float fadeValue = 0.4f;
    public float fadeTime = 0.1f;

    private float _fadeCountdown = 0.0f;

    private bool _hit = false;

    private void Start()
    {
        material = cube.GetComponent<Renderer>().material;
    }

    public void SetTargeted()
    {
        _isTargeted = true;
        _fadeCountdown = fadeTime;
    }

    private void UpdateMaterial()
    {
        cube.SetActive(true);
        material.SetFloat("fade", Mathf.Lerp(0.0f, fadeValue, 1.0f / fadeTime * _fadeCountdown));
    }
    
    private void Update()
    {
        if (!_isTargeted) return;
        UpdateMaterial();
        _fadeCountdown -= Time.deltaTime;
            
        if (_fadeCountdown < 0.0f)
        {
            _isTargeted = false;
            _fadeCountdown = 0.0f;
            cube.SetActive(false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (singleHit && _hit) return;
        healthBehaviour.OnCollision(type);
        _hit = true;
    }
}
