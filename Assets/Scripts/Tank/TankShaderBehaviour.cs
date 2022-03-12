using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TankShaderBehaviour : NetworkBehaviour 
{

    public float fadeDistance = .5f;
    public float visibilityRange = 15.0f;
    private List<Material> _shaderMaterials = new List<Material>();

    private static Dictionary<ulong, Transform> _networkIdToTransform = new Dictionary<ulong, Transform>();

    private void Start()
    {
        FindChildRenderer(gameObject.transform); 
    }

    public override void OnNetworkSpawn()
    {
        _networkIdToTransform[NetworkObjectId] = gameObject.transform;
    }

    public override void OnNetworkDespawn()
    {
        _networkIdToTransform.Remove(NetworkObjectId);
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

    void Update()
    {
        if (NetworkManager == null || !NetworkManager.Singleton.IsClient || !GameManagerBehaviour.GameBegun || IsOwner) return;

        var positions = new List<Vector4>();

        foreach (var entry in _networkIdToTransform)
        {
            if (entry.Key != NetworkObjectId)
            {
                positions.Add(entry.Value.position);    
            }
        }
        
        foreach (var m in _shaderMaterials)
        {
            PushDetectingSources(m, positions);
        }
    }
}
