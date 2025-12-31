using System.Collections.Generic;
using UnityEngine;

public class ModuleData : MonoBehaviour
{
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    public Dictionary<string,Dictionary<string, object>> prototype;

    public Mesh mesh
    {
        get => _mesh;
        set
        {
            _mesh = value;
            SetMesh(_mesh);
        }
    }

    public void SetMesh(Mesh mesh)
    {
        if(_meshFilter!=null)     
        {
            _meshFilter.mesh = mesh;
        }
    }

    public void SetMaterial(Material material)
    {
        _meshRenderer.material = material;
    }
    
    void Awake()
    {
        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
        
    }
}
