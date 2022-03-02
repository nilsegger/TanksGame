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

    // TODO implement
    public float m_GridSize;
    public float m_Height;

    private Mesh _mesh;
    private List<Vector3> _vertices = new List<Vector3>();
    private List<int> _triangles = new List<int>();
    private List<Vector3> _normals = new List<Vector3>();
    
    private Vector3[] _visibleAreaBoundingBox = new Vector3[4];
    void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        
        CalcVisibleAreaBoundingBox();
        CreateBoundingBoxSurface();
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

    private void CreateBoundingBoxSurface()
    {
       _vertices.Clear(); 
       _triangles.Clear();
       _normals.Clear();

       _vertices.Add(m_Corners[0]);
       _vertices.Add(m_Corners[1]);
       _vertices.Add(m_Corners[2]);
       _vertices.Add(m_Corners[3]);
       
       _vertices.Add(_visibleAreaBoundingBox[0]);
       _vertices.Add(_visibleAreaBoundingBox[1]);
       _vertices.Add(_visibleAreaBoundingBox[2]);
       _vertices.Add(_visibleAreaBoundingBox[3]);
       
       for (int i = 0; i < 3; i++)
       {
           _triangles.Add(i + 5);
           _triangles.Add(i + 4);
           _triangles.Add(i);
       }
       
       _triangles.Add(3);
       _triangles.Add(4);
       _triangles.Add(7);

       for (int i = 0; i < _vertices.Count; i++)
       {
           _normals.Add(Vector3.up);
       }
    }

    private void UpdateMesh()
    {
        _mesh.Clear();
        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();
        _mesh.normals = _normals.ToArray();
    }

    private void DebugDrawPoint(Vector3 point, float r)
    {
        Debug.DrawLine(point + Vector3.left * r, point + Vector3.right * r, Color.blue);
        Debug.DrawLine(point + Vector3.forward * r, point + Vector3.back * r, Color.blue);
    }

    void Update()
    {
        
        CalcVisibleAreaBoundingBox(); 
        CreateBoundingBoxSurface();
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
