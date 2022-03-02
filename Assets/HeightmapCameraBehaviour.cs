using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Assertions;

public class HeightmapCameraBehaviour : MonoBehaviour
{

    public Material m_Material;
    public FogOfWarBehaviour m_FOGBehaviour; 
    
    private Camera _camera;
    private RenderTexture _texture;
    
    void Start()
    {
        _texture = new RenderTexture(Screen.width, Screen.height, 0);
        _texture.Create();
        
        _camera = GetComponent<Camera>();
        _camera.targetTexture = _texture;
        
        Assert.IsNotNull(_camera);

        _camera.depthTextureMode = DepthTextureMode.DepthNormals;
    }

    private void OnDestroy()
    {
        _texture.Release();
    }

    // Update is called once per frame
    void Update()
    {
        m_FOGBehaviour.SetHeightmap(_texture);
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, m_Material);
    }
}
