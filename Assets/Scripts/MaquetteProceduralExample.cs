using System;
using Polyhydra.Core;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Runtime-only editable recipe; rebuilding preserves the example's GameObjects.</summary>
public abstract class MaquetteProceduralExample : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh mesh;
    private bool needsRebuild;
    public PolyMesh Poly { get; private set; }
    public Transform ModelTransform => meshFilter.transform;

    public bool Initialize(Material material)
    {
        var model = new GameObject("Model");
        model.transform.SetParent(transform, false);
        meshFilter = model.AddComponent<MeshFilter>();
        model.AddComponent<MeshRenderer>().sharedMaterial = material;
        return Rebuild();
    }

    protected void OnValidate() => needsRebuild = true;

    protected void Update()
    {
        if (needsRebuild && meshFilter != null) Rebuild();
    }

    protected abstract PolyMesh Build();

    private bool Rebuild()
    {
        needsRebuild = false;
        try
        {
            var poly = Build();
            var data = poly.BuildMeshData(useSmoothing: true);
            if (mesh == null)
            {
                mesh = new Mesh { name = gameObject.name, indexFormat = IndexFormat.UInt32 };
                meshFilter.sharedMesh = mesh;
            }
            mesh.Clear();
            mesh.SetVertices(data.meshVertices);
            mesh.SetNormals(data.meshNormals);
            mesh.SetTriangles(data.meshTriangles, 0);
            mesh.SetUVs(0, data.meshUVs);
            mesh.RecalculateBounds();
            meshFilter.transform.localPosition = new Vector3(-mesh.bounds.center.x,
                .08f - mesh.bounds.min.y, -mesh.bounds.center.z);
            Poly = poly;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MaquetteProceduralExamples] {gameObject.name}: {exception}", this);
            return false;
        }
    }

    protected void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }
}
