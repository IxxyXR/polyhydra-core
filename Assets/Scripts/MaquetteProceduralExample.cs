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

    public bool Initialize(Material material)
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
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
            // Centre the geometry above the plinth without moving the example's
            // transform, which also anchors its label and plinth.
            if (poly.Vertices.Count > 0)
            {
                var bounds = new Bounds(poly.Vertices[0].Position, Vector3.zero);
                foreach (var vertex in poly.Vertices) bounds.Encapsulate(vertex.Position);
                var offset = new Vector3(-bounds.center.x, .08f - bounds.min.y, -bounds.center.z);
                foreach (var vertex in poly.Vertices) vertex.Position += offset;
            }
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
