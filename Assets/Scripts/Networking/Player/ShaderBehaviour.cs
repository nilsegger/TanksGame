using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class ShaderBehaviour : NetworkBehaviour 
{

    public float visibilityRange = 15.0f;
    public float fadeDistance = .5f;
    public float visibilityDegreesRange = 120.0f;
    public float fadeDegreesDistance = 10.0f;
    public Transform eyes;
    
    private List<Material> _shaderMaterials = new List<Material>();

    private static Dictionary<ulong, Transform> _networkIdToTransform = new Dictionary<ulong, Transform>();

    private void Start()
    {
        FindChildRenderer(gameObject.transform); 
    }

    public override void OnNetworkSpawn()
    {
        _networkIdToTransform[NetworkObjectId] = eyes;
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

    private void PushDetectingSources(Material m, List<Vector4> positions, List<Vector4> forward)
    {
        Assert.IsFalse(positions.Count > 10, "Maximum of 10 detectors or shader max count has to be changed");
        m.SetFloat("fade_distance", fadeDistance);
        m.SetFloat("fade_degrees_distance", fadeDegreesDistance);
        m.SetFloat("visibility_range", visibilityRange);
        m.SetFloat("visibility_degrees_range", visibilityDegreesRange);
        m.SetInt("detecting_sources_count", positions.Count);
        m.SetVectorArray("detecting_sources_position", positions); 
        m.SetVectorArray("detecting_sources_forward", forward); 
    }

    void Update()
    {
        if (NetworkManager == null || !NetworkManager.Singleton.IsClient || !GameManagerBehaviour.GameBegun || IsOwner) return;

        var positions = new List<Vector4>();
        var forwards = new List<Vector4>();

        foreach (var entry in _networkIdToTransform)
        {
            if (entry.Key != NetworkObjectId)
            {
                positions.Add(entry.Value.position);    
                forwards.Add(entry.Value.forward);
            }
        }
        
        foreach (var m in _shaderMaterials)
        {
            PushDetectingSources(m, positions, forwards);
        }
    }
}
