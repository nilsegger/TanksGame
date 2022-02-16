using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWarBehaviour : MonoBehaviour
{

    public Material material;

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, material);
    }

    public void SetCenterPoint(Vector4 center) {
        material.SetVector("center", center); 
    }
    public void SetPoints(List<Vector4> points) {
        material.SetVectorArray("points", points);
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
