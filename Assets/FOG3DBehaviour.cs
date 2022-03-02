using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class FOG3DBehaviour : MonoBehaviour
{

    public MeshFilter m_Filter;
    private Mesh _mesh;
    // Start is called before the first frame update
    void Start()
    {
        Assert.IsNotNull(m_Filter);
        
        _mesh = m_Filter.mesh;
        
        Assert.IsNotNull(_mesh);

        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3[] updated = new Vector3[_mesh.vertexCount];
        for (int i = 0; i < _mesh.vertexCount; i++)
        {
            updated[i] = _mesh.vertices[i] + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
        }

        _mesh.vertices = updated;
    }
}
