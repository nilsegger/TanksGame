using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FogOfWarBehaviour : MonoBehaviour 
{

    public Material m_Material;
    public int m_Casts = 20; // this needs to match the shader
    public float m_AngleRange = 5.0f;
    public float m_Range = 5.0f;
    public Transform m_Transform;

    public bool m_DrawDebugRayHits = false;

    private Camera _camera;
    private Vector3 _eyesPosition;
    private List<Vector4> _sightPoints;

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(source, destination, m_Material);
    }

    public void SetCenterPoint(Vector4 center) {
        m_Material.SetVector("center", center); 
    }
    public void SetPoints(List<Vector4> points) {
        m_Material.SetVectorArray("points", points);
    }
    
    private void SetDebugDrawRayHits() {
        m_Material.SetInt("debug", m_DrawDebugRayHits ? 1 : 0);
    }
    
    private void SetEyesPosition()
    {
        _eyesPosition = m_Transform.position; // - _turret.forward * 1.0f; 
    }
    
    private void RaycastSight()
    {
        for(int i = 0; i < m_Casts; i++) {
            Vector3 rotatedForward = Quaternion.Euler(0, (-m_AngleRange/ 2.0f) + i * (m_AngleRange/ (float)m_Casts), 0) * m_Transform.forward;
            RaycastHit hit = new RaycastHit();
            Vector3 result;
            if (Physics.Raycast (_eyesPosition, rotatedForward, out hit, m_Range))
            {
                result = hit.point;
            } else
            {
                result = _eyesPosition + rotatedForward * m_Range;
            }
            Vector3 screenPoint = _camera.WorldToScreenPoint(result);
            _sightPoints[i] = new Vector4(1.0f / _camera.pixelWidth * screenPoint.x, 1.0f / _camera.pixelHeight * screenPoint.y, 0, 0);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _camera = gameObject.GetComponent<Camera>();
        _sightPoints = new List<Vector4>(new Vector4[m_Casts]);
        SetEyesPosition();    
    }

    // Update is called once per frame
    void Update()
    {
        RaycastSight();
        SetEyesPosition();    
        
        Vector3 screenCenterPoint = _camera.WorldToScreenPoint(_eyesPosition);
        SetCenterPoint(new Vector4(1.0f / _camera.pixelWidth * screenCenterPoint.x , 1.0f / _camera.pixelHeight * screenCenterPoint.y, 0, 0));
        SetPoints(_sightPoints);   
        SetDebugDrawRayHits();
    }
}