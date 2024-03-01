#if false
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class HalfEdgeMesh : MonoBehaviour
{
    public List<Vertex> Vertices;
    public List<HalfEdge> HalfEdges;
    public List<Face> Faces;

    public struct Vertex
    {
        public Vector3 position;
        public HalfEdge halfEdge;
    }

    public struct HalfEdge
    {
        public int next;
        public int prev;
        public int vertex;
        public int face;
    }

    public struct Face
    {
        public HalfEdge halfEdge;
    }

    [BurstCompile]
    struct CreateHalfEdgesJob : IJob
    {
        public NativeArray<Vector3> vertexPositions;
        public NativeArray<int> faceIndices;
        public NativeList<Vertex> vertices;
        public NativeList<HalfEdge> halfEdges;
        public NativeList<Face> faces;

        public void Execute()
        {
            int vertexCount = vertexPositions.Length;
            vertices.ResizeUninitialized(vertexCount);

            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = new Vertex { position = vertexPositions[i] };
            }

            int edgeCount = faceIndices.Length;
            halfEdges.ResizeUninitialized(edgeCount);

            for (int i = 0; i < edgeCount; i++)
            {
                int current = faceIndices[i];
                int nextIndex = (i + 1) % edgeCount;
                int next = faceIndices[nextIndex];

                halfEdges[i] = new HalfEdge
                {
                    next = nextIndex,
                    prev = (i - 1 + edgeCount) % edgeCount,
                    vertex = current,
                    face = 0
                };

                Vertex updatedVertex = vertices[current];
                updatedVertex.halfEdge = halfEdges[i];
                vertices[current] = updatedVertex;
            }

            faces[0] = new Face { halfEdge = halfEdges[0] };
        }
    }

    public void Create(List<Vector3> vertexPositions, List<int> faceIndices)
    {
        Vertices = new List<Vertex>(vertexPositions.Count);
        Faces = new List<Face>(1);
        HalfEdges = new List<HalfEdge>(faceIndices.Count);

        Faces.Add(new Face());

        using (NativeList<Vertex> nativeVertices = new NativeList<Vertex>(Allocator.TempJob))
        using (NativeList<Vector3> nativeVertexPositions = new NativeList<Vector3>(vertexPositions, Allocator.TempJob))
        using (NativeList<HalfEdge> nativeHalfEdges = new NativeList<HalfEdge>(Allocator.TempJob))
        using (NativeList<Face> nativeFaces = new NativeList<Face>(Faces, Allocator.TempJob))
        using (NativeList<int> nativeFaceIndices = new NativeList<int>(faceIndices, Allocator.TempJob))
        {
            CreateHalfEdgesJob job = new CreateHalfEdgesJob
            {
                vertices = nativeVertices,
                vertexPositions = nativeVertexPositions,
                halfEdges = nativeHalfEdges,
                faces = nativeFaces,
                faceIndices = nativeFaceIndices
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            Vertices.Clear();
            Vertices.AddRange(nativeVertices);

            HalfEdges.Clear();
            HalfEdges.AddRange(nativeHalfEdges);

            Faces.Clear();
            Faces.AddRange(nativeFaces);
        }
    }

    [BurstCompile]
    struct CreateCubeMeshJob : IJob {
        public NativeArray<int> cubeIndices;
        public NativeList<int> meshTriangles;

        public void Execute() {
            for (int i = 0; i < cubeIndices.Length; i += 4) {
                meshTriangles.Add(cubeIndices[i]);
                meshTriangles.Add(cubeIndices[i + 1]);
                meshTriangles.Add(cubeIndices[i + 2]);

                meshTriangles.Add(cubeIndices[i]);
                meshTriangles.Add(cubeIndices[i + 2]);
                meshTriangles.Add(cubeIndices[i + 3]);
            }
        }
    }

            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            Vertices.Clear();
            Vertices.AddRange(nativeVertices);

            HalfEdges.Clear();
            HalfEdges.AddRange(nativeHalfEdges);

            Faces.Clear();
            Faces.AddRange(nativeFaces);
        }
    }

    [BurstCompile]
    struct CreateCubeMeshJob : IJob {
        public NativeArray<int> cubeIndices;
        public NativeList<int> meshTriangles;

        public void Execute() {
            for (int i = 0; i < cubeIndices.Length; i += 4) {
                meshTriangles.Add(cubeIndices[i]);
                meshTriangles.Add(cubeIndices[i + 1]);
                meshTriangles.Add(cubeIndices[i + 2]);

                meshTriangles.Add(cubeIndices[i]);
                meshTriangles.Add(cubeIndices[i + 2]);
                meshTriangles.Add(cubeIndices[i + 3]);
            }
        }
    }

    public Mesh CreateCubeMesh() {
        List<Vector3> cubeVertices = new List<Vector3> {
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f)
        };

        List<int> cubeIndices = new List<int> {
            0, 1, 2, 3, // Front
            4, 5, 1, 0, // Bottom
            5, 6, 2, 1, // Right
            6, 7, 3, 2, // Top
            7, 4, 0, 3, // Left
            7, 6, 5, 4  // Back
        };

        HalfEdgeMesh halfEdgeMesh = new HalfEdgeMesh();
        halfEdgeMesh.Create(cubeVertices, cubeIndices);

        Mesh mesh = new Mesh();

        using (NativeArray<int> nativeCubeIndices = new NativeArray<int>(cubeIndices.ToArray(), Allocator.TempJob))
        using (NativeList<Vector3> nativeMeshVertices = new NativeList<Vector3>(halfEdgeMesh.Vertices.Select(v => v.position).ToArray(), Allocator.TempJob))
        using (NativeList<int> nativeMeshTriangles = new NativeList<int>(Allocator.TempJob)) {
            CreateCubeMeshJob job = new CreateCubeMeshJob {
                cubeIndices = nativeCubeIndices,
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