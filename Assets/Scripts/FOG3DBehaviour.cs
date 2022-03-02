using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[ExecuteInEditMode]
public class FOG3DBehaviour : MonoBehaviour
{

    public Vector3[] m_Corners = new Vector3[4]; // Must be in clockwise order
    public List<Vector3> m_VisibleArea;

    public float m_GridSize; 

    private Mesh _mesh;
    
    private Vector3[] _visibleAreaBoundingBox = new Vector3[4];

    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector3[] _normals;
    
    void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        
        CreateShape();
        UpdateMesh();
    }

    private void OnValidate()
    {
        Assert.IsNotNull(m_Corners);
        Assert.IsNotNull(m_VisibleArea);
    }

    // Calculcate bounding box for visible area, ignores y position
    // TODO maybe check that point only satisfies one corner
    private void CalcVisibleAreaBoundingBox()
    {
        _visibleAreaBoundingBox[0] = m_VisibleArea[0];
        _visibleAreaBoundingBox[1] = m_VisibleArea[0];
        _visibleAreaBoundingBox[2] = m_VisibleArea[0];
        _visibleAreaBoundingBox[3] = m_VisibleArea[0];

        foreach (var point in m_VisibleArea)
        {
            // top left
            _visibleAreaBoundingBox[0].x = Mathf.Min(_visibleAreaBoundingBox[0].x, point.x);
            _visibleAreaBoundingBox[0].z = Mathf.Max(_visibleAreaBoundingBox[0].z, point.z);
            
            // top right
            _visibleAreaBoundingBox[1].x = Mathf.Max(_visibleAreaBoundingBox[1].x, point.x);
            _visibleAreaBoundingBox[1].z = Mathf.Max(_visibleAreaBoundingBox[1].z, point.z);
            
            // bot right
            _visibleAreaBoundingBox[2].x = Mathf.Max(_visibleAreaBoundingBox[2].x, point.x);
            _visibleAreaBoundingBox[2].z = Mathf.Min(_visibleAreaBoundingBox[2].z, point.z);
            
            // bot right
            _visibleAreaBoundingBox[3].x = Mathf.Min(_visibleAreaBoundingBox[3].x, point.x);
            _visibleAreaBoundingBox[3].z = Mathf.Min(_visibleAreaBoundingBox[3].z, point.z);
        }
        
    }

    private void CreateShape()
    {
        _vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
            
            new Vector3(1, 0, 1),
            new Vector3(1, 0, 2)
        };

        _triangles = new int[]
        {
            0, 1, 2,
            1, 4, 3
        };

        _normals = new Vector3[]
        {
            new Vector3(0, 1.0f, 0),
            new Vector3(0, 1.0f, 0),
            new Vector3(0, 1.0f, 0),
            
            new Vector3(0, 1.0f, 0),
            new Vector3(0, 1.0f, 0)
        };
    }

    private void UpdateMesh()
    {
        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        _mesh.normals = _normals;
    }

    private void DebugDrawPoint(Vector3 point, float r)
    {
        Debug.DrawLine(point + Vector3.left * r, point + Vector3.right * r, Color.blue);
        Debug.DrawLine(point + Vector3.forward * r, point + Vector3.back * r, Color.blue);
    }

    void Update()
    {
        
        CalcVisibleAreaBoundingBox(); 
        CreateShape();
        UpdateMesh();
        
        Debug.DrawLine(_visibleAreaBoundingBox[0], _visibleAreaBoundingBox[1], Color.green);
        Debug.DrawLine(_visibleAreaBoundingBox[1], _visibleAreaBoundingBox[2], Color.blue);
        Debug.DrawLine(_visibleAreaBoundingBox[2], _visibleAreaBoundingBox[3], Color.red);
        Debug.DrawLine(_visibleAreaBoundingBox[3], _visibleAreaBoundingBox[0], Color.cyan);

        foreach (var point in m_VisibleArea)
        {
           DebugDrawPoint(point, 0.2f); 
        }

        foreach (var point in m_Corners)
        {
           DebugDrawPoint(point, 0.2f); 
        }
    }
    
}
