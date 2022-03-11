using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankShaderBehaviour : MonoBehaviour
{

    public float fadeDistance = .5f;
    public float visibilityRange = 15.0f;
    private List<Material> _shaderMaterials = new List<Material>();

    private void Start()
    {
        FindChildRenderer(gameObject.transform); 
    }

    private void FindChildRenderer(Transform t)
    {
        foreach (Transform c in t)
        {
            var r = c.gameObject.GetComponent<Renderer>();
            if(r != null) {
                foreach (var m in r.materials)
                {
                 _shaderMaterials.Add(m);
                }
            }
            
            FindChildRenderer(c);
        } 
    }

    private void PushDetectingSources(Material m, List<Vector4> positions)
    {
        m.SetFloat("fade_distance", fadeDistance);
        m.SetFloat("visibility_range", visibilityRange);
        m.SetInt("detecting_sources_count", positions.Count);
        m.SetVectorArray("detecting_sources_position", positions); 
    }

    // Update is called once per frame
    void Update()
    {
        if (_shaderMaterials == null) return;

        var positions = new List<Vector4>() {new Vector4(0.0f, 0.0f, 0.0f, 0.0f)};

        foreach (var m in _shaderMaterials)
        {
            PushDetectingSources(m, positions);
        }
    }
}
