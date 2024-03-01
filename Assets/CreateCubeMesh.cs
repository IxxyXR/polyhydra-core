#if false
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CreateCubeMesh
{
    public Mesh Create()
    {
        List<Vector3> cubeVertices = new List<Vector3>
        {
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f)
        };

        List<int> cubeIndices = new List<int>
        {
            0, 1, 2, 3, // Front
            4, 5, 1, 0, // Bottom
            5, 6, 2, 1, // Right
            6, 7, 3, 2, // Top
            7, 4, 0, 3, // Left
            7, 6, 5, 4 // Back
        };

        HalfEdgeMesh halfEdgeMesh = new HalfEdgeMesh();

        Mesh mesh = new Mesh();

        using (NativeArray<Vector3> nativeCubeVertices =
               new NativeArray<Vector3>(cubeVertices.ToArray(), Allocator.TempJob))
        using (NativeArray<int> nativeCubeIndices = new NativeArray<int>(cubeIndices.ToArray(), Allocator.TempJob))
        using (NativeList<Vector3> nativeMeshVertices = new NativeList<Vector3>(Allocator.TempJob))
        using (NativeList<int> nativeMeshTriangles = new NativeList<int>(Allocator.TempJob))
        {
            halfEdgeMesh.Create(cubeVertices, cubeIndices);
            nativeMeshVertices.AddRange(halfEdgeMesh.Vertices.Select(v => v.position).ToArray());

            HalfEdgeMesh.CreateCubeMeshJob job = new HalfEdgeMesh.CreateCubeMeshJob
            {
                cubeVertices = nativeCubeVertices,
                cubeIndices = nativeCubeIndices,
                meshVertices = nativeMeshVertices,
                meshTriangles = nativeMeshTriangles
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            mesh.SetVertices(nativeMeshVertices);
            mesh.SetTriangles(nativeMeshTriangles, 0);
            mesh.RecalculateNormals();
        }

        return mesh;
    }
}
#endif
