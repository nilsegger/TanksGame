using UnityEngine;
using UnityEngine.Assertions;

public class HeightmapCameraBehaviour : MonoBehaviour
{

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
    }

    private void OnDestroy()
    {
        _texture.Release();
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination);
        m_FOGBehaviour.SetHeightmap(_texture);
    }
}
