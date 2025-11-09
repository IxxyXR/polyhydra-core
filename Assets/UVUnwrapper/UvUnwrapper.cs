 using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unwrapper
{
    public class UvUnwrapper
    {
        private static readonly float[] RotateDegrees = { 5, 15, 20, 25, 30, 35, 40, 45 };
        public List<List<Vector2>> FaceUvs { get; private set; } = new();
        public List<Rect> ChartRects { get; private set; } = new();

        public List<(List<int> faces, List<List<Vector2>> uvs)> Charts { get; private set; } = new();
        public List<int> ChartSourcePartitions { get; private set; } = new();

        private readonly Dictionary<int, List<int>> partitions = new();
        private readonly List<Vector2> chartSizes = new();
        private readonly List<Vector2> scaledChartSizes = new();

        // Configuration
        public bool SegmentByNormal = true;
        public float SegmentDotProductThreshold = 0.0f; // 90 degrees
        public float TexelSizePerUnit = 1.0f;
        public bool SegmentPreferMorePieces = true;
        public bool EnableRotation = true;

        private UVMesh mesh;
        private float resultTextureSize = 0;

        public void SetMesh(UVMesh mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }
            if (mesh.vertices == null || mesh.vertices.Count == 0)
                throw new ArgumentException("Mesh must have vertices", nameof(mesh));
            if (mesh.faces == null || mesh.faces.Length == 0)
                throw new ArgumentException("Mesh must have faces", nameof(mesh));
            this.mesh = mesh;
        }

        public float GetTextureSize() => resultTextureSize;

        private static void BuildEdgeToFaceMap(List<List<int>> faces, Dictionary<(int, int), int> edgeToFaceMap)
        {
            edgeToFaceMap.Clear();
            for (int index = 0; index < faces.Count; index++)
            {
                var face = faces[index];
                for (int i = 0; i < 3; i++)
                {
                    int j = (i + 1) % 3;
                    //var edge = (Math.Min(face[i], face[j]),
                    //          Math.Max(face[i], face[j]));
                    //edgeToFaceMap[edge] = index;
                    var edge = (face[i], face[j]);
                    edgeToFaceMap[edge] = index;
                }
            }
        }

        private void BuildEdgeToFaceMap(List<int> group, Dictionary<(int, int), int> edgeToFaceMap)
        {
            edgeToFaceMap.Clear();
            foreach (var index in group)
            {
                var face = mesh.faces[index];
                for (int i = 0; i < 3; i++)
                {
                    int j = (i + 1) % 3;
                    //var edge = (Math.Min(face[i], face[j]),
                    //          Math.Max(face[i], face[j]));
                    //edgeToFaceMap[edge] = index;
                    var edge = (face[i], face[j]);
                    edgeToFaceMap[edge] = index;
                }
            }
        }

        private void SplitPartitionToIslands(List<int> group, List<List<int>> islands)
        {
            var edgeToFaceMap = new Dictionary<(int, int), int>();
            BuildEdgeToFaceMap(group, edgeToFaceMap);
            //bool segmentByNormal = !mesh.faceNormals.IsNullOrEmpty() && this.segmentByNormal;
            bool segmentByNormal = (mesh.faceNormals != null && mesh.faceNormals.Count > 0) && this.SegmentByNormal;

            var processedFaces = new HashSet<int>();
            var waitFaces = new Queue<int>();

            foreach (var indexInGroup in group)
            {
                if (processedFaces.Contains(indexInGroup))
                    continue;

                waitFaces.Enqueue(indexInGroup);
                var island = new List<int>();

                while (waitFaces.Count > 0)
                {
                    int index = waitFaces.Dequeue();
                    if (processedFaces.Contains(index))
                        continue;

                    var face = mesh.faces[index];
                    for (int i = 0; i < 3; i++)
                    {
                        int j = (i + 1) % 3;
                        var oppositeEdge = (face[j], face[i]);
                        if (!edgeToFaceMap.TryGetValue(oppositeEdge, out int oppositeFaceIndex))
                            continue;

                        if (segmentByNormal)
                        {
                            var dot = Vector3.Dot(mesh.faceNormals[oppositeFaceIndex],
                                mesh.faceNormals[SegmentPreferMorePieces ? indexInGroup : index]);
                            if (dot < SegmentDotProductThreshold)
                                continue;
                        }

                        waitFaces.Enqueue(oppositeFaceIndex);
                    }

                    island.Add(index);
                    processedFaces.Add(index);
                }

                if (island.Count > 0)
                    islands.Add(island);
            }
        }

        private float CalculateFaceArea(List<int> face)
        {
            var v1 = mesh.vertices[face[0]];
            var v2 = mesh.vertices[face[1]];
            var v3 = mesh.vertices[face[2]];

            return CalculateTriangleArea(
                new Vector3(v1.x, v1.y, v1.z),
                new Vector3(v2.x, v2.y, v2.z),
                new Vector3(v3.x, v3.y, v3.z)
            );
        }

        private static float CalculateTriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
        }

        private static float CalculateTriangleArea2D(Vector2 a, Vector2 b, Vector2 c)
        {
            return CalculateTriangleArea(
                new Vector3(a.x, a.y, 0),
                new Vector3(b.x, b.y, 0),
                new Vector3(c.x, c.y, 0)
            );
        }

        private static void CalculateFaceTextureBoundingBox(List<List<Vector2>> faceTextureCoords,
            out float left, out float top, out float right, out float bottom)
        {
            left = top = right = bottom = 0;
            bool first = true;

            foreach (var item in faceTextureCoords)
            {
                for (int i = 0; i < 3; i++)
                {
                    var x = item[i].x;
                    var y = item[i].y;

                    if (first)
                    {
                        left = right = x;
                        top = bottom = y;
                        first = false;
                    }
                    else
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                    }
                }
            }
        }

        private void CalculateSizeAndRemoveInvalidCharts()
        {
            var validCharts = new List<(List<int>, List<List<Vector2>>)>();
            var validPartitions = new List<int>();
            chartSizes.Clear();
            scaledChartSizes.Clear();

            for (int chartIndex = 0; chartIndex < Charts.Count; chartIndex++)
            {
                var chart = Charts[chartIndex];
                CalculateFaceTextureBoundingBox(chart.uvs,
                    out float left, out float top, out float right, out float bottom);

                var size = new Vector2(right - left, bottom - top);
                if (size.x <= 0 || float.IsNaN(size.x) || float.IsInfinity(size.x) ||
                    size.y <= 0 || float.IsNaN(size.y) || float.IsInfinity(size.y))
                    continue;

                float surfaceArea = chart.faces.Sum(faceIndex => CalculateFaceArea(mesh.faces[faceIndex]));
                float uvArea = 0;

                // Normalize UVs
                foreach (var faceUv in chart.uvs)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var vector2 = faceUv[i];
                        vector2.x -= left;
                        vector2.y -= top;
                        faceUv[i] = vector2;
                    }

                    uvArea += CalculateTriangleArea2D(
                        new Vector2(faceUv[0].x, faceUv[0].y),
                        new Vector2(faceUv[1].x, faceUv[1].y),
                        new Vector2(faceUv[2].x, faceUv[2].y)
                    );
                }

                if (EnableRotation)
                {
                    var center = new Vector2(size.x * 0.5f, size.y * 0.5f);
                    float minRectArea = size.x * size.y;
                    float minRectLeft = 0;
                    float minRectTop = 0;
                    bool rotated = false;

                    foreach (float degrees in RotateDegrees)
                    {
                        float radians = degrees * Mathf.Deg2Rad;
                        var rotatedUvs = new List<List<Vector2>>();

                        foreach (var faceUv in chart.uvs)
                        {
                            var rotatedCoords = new Vector2[3].ToList();
                            for (int i = 0; i < 3; i++)
                            {
                                var point = new Vector2(faceUv[i].x, faceUv[i].y) - center;
                                var rotatedP = new Vector2(
                                    point.x * (float)Math.Cos(radians) - point.y * (float)Math.Sin(radians),
                                    point.x * (float)Math.Sin(radians) + point.y * (float)Math.Cos(radians)
                                );
                                rotatedCoords[i] = new Vector2(rotatedP.x, rotatedP.y);
                            }
                            rotatedUvs.Add(rotatedCoords);
                        }

                        CalculateFaceTextureBoundingBox(rotatedUvs,
                            out float rotLeft, out float rotTop,
                            out float rotRight, out float rotBottom);

                        var newSize = new Vector2(rotRight - rotLeft, rotBottom - rotTop);
                        float newRectArea = newSize.x * newSize.y;

                        if (newRectArea < minRectArea)
                        {
                            minRectArea = newRectArea;
                            size = newSize;
                            minRectLeft = rotLeft;
                            minRectTop = rotTop;
                            rotated = true;
                            chart.uvs = rotatedUvs;
                        }
                    }

                    if (rotated)
                    {
                        foreach (var faceUv in chart.uvs)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                var vector2 = faceUv[i];
                                vector2.x -= minRectLeft;
                                vector2.y -= minRectTop;
                                faceUv[i] = vector2;
                            }
                        }
                    }
                }

                float ratioOfSurfaceAreaAndUvArea = uvArea > 0 ? surfaceArea / uvArea : 1.0f;
                float scale = ratioOfSurfaceAreaAndUvArea * TexelSizePerUnit;

                chartSizes.Add(size);
                scaledChartSizes.Add(new Vector2(size.x * scale, size.y * scale));
                validCharts.Add(chart);
                validPartitions.Add(ChartSourcePartitions[chartIndex]);
            }

            Charts = validCharts;
            ChartSourcePartitions = validPartitions;
        }

        private void PackCharts()
        {
            var chartPacker = new ChartPacker();
            chartPacker.SetCharts(scaledChartSizes);
            resultTextureSize = chartPacker.Pack();

            ChartRects = new List<Rect>(chartSizes.Count);
            var packedResult = chartPacker.GetResults();

            for (int i = 0; i < Charts.Count; i++)
            {
                var chartSize = chartSizes[i];
                var (_, uvs) = Charts[i];

                if (i >= packedResult.Count)
                {
                    foreach (var faceUv in uvs)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            var vector2 = faceUv[j];
                            vector2.x = 0;
                            vector2.y = 0;
                            faceUv[j] = vector2;
                        }
                    }
                    continue;
                }

                var (position, size, flipped) = packedResult[i];

                ChartRects.Add(new Rect (
                    position.x,
                    position.y,
                    flipped ? size.y : size.x,
                    flipped ? size.x : size.y
                ));

                if (flipped)
                {
                    foreach (var faceUv in uvs)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            var vector2 = faceUv[j];
                            float temp = vector2.x;
                            vector2.x = vector2.y;
                            vector2.y = temp;
                            faceUv[j] = vector2;
                        }
                    }
                }

                foreach (var faceUv in uvs)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var vector2 = faceUv[j];
                        vector2.x /= chartSize.x;
                        vector2.y /= chartSize.y;
                        vector2.x *= size.x;
                        vector2.y *= size.y;
                        vector2.x += position.x;
                        vector2.y += position.y;
                        faceUv[j] = vector2;
                    }
                }
            }
        }

        // Fixed FinalizeUv method
        private void FinalizeUv()
        {
            Debug.Log("Finalizing UVs...");
            Debug.Log($"Charts count: {Charts.Count}");

            // Make sure faceUvs is properly initialized
            if (FaceUvs.Count != mesh.faces.Length)
            {
                FaceUvs = new();
                for (int i = 0; i < mesh.faces.Length; i++)
                {
                    FaceUvs.Add(new List<Vector2>
                    {
                        new Vector2(),
                        new Vector2(),
                        new Vector2()
                    });
                }
            }

            foreach (var (faces, uvs) in Charts)
            {
                Debug.Log($"Processing chart with {faces.Count} faces");
                for (int i = 0; i < faces.Count; i++)
                {
                    int globalFaceIndex = faces[i];
                    if (globalFaceIndex < 0 || globalFaceIndex >= FaceUvs.Count)
                    {
                        Debug.Log($"Warning: Invalid face index {globalFaceIndex}");
                        continue;
                    }

                    var sourceUv = uvs[i];
                    var destUv = FaceUvs[globalFaceIndex];

                    for (int j = 0; j < 3; j++)
                    {
                        var vector2 = destUv[j];
                        vector2.x = sourceUv[j].x;
                        vector2.y = sourceUv[j].y;
                        destUv[j] = vector2;
                    }
                }
            }

            Debug.Log($"Finalized {FaceUvs.Count} face UVs");
        }

        private void Partition()
        {
            partitions.Clear();
            if (mesh.facePartitions.Count == 0)
            {
                partitions[0] = Enumerable.Range(0, mesh.faces.Length).ToList();
            }
            else
            {
                for (int i = 0; i < mesh.faces.Length; i++)
                {
                    int partition = mesh.facePartitions[i];
                    if (!partitions.ContainsKey(partition))
                        partitions[partition] = new();
                    partitions[partition].Add(i);
                }
            }
        }

        private static float DistanceBetweenVertices(Vector3 first, Vector3 second)
        {
            float dx = first.x - second.x;
            float dy = first.y - second.y;
            float dz = first.z - second.z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private bool FixHolesExceptTheLongestRing(List<Vector3> vertices, List<List<int>> faces, out int remainingHoleNum)
        {
            Debug.Log($"\nFixHolesExceptTheLongestRing: Processing {faces.Count} faces");
            remainingHoleNum = 0;

            // Build edge to face map
            var edgeToFaceMap = new Dictionary<(int, int), int>();
            BuildEdgeToFaceMap(faces, edgeToFaceMap);
            Debug.Log($"Built edge map with {edgeToFaceMap.Count} edges");

            // Find boundary edges (edges with only one adjacent face)
            var boundaryEdges = new Dictionary<int, List<int>>();
            foreach (var face in faces)
            {
                for (int i = 0; i < 3; i++)
                {
                    int j = (i + 1) % 3;
                    var edge = (face[j], face[i]);  // Note: reversed for boundary check
                    if (!edgeToFaceMap.ContainsKey(edge))
                    {
                        // This is a boundary edge
                        if (!boundaryEdges.ContainsKey(face[i]))
                            boundaryEdges[face[i]] = new();
                        boundaryEdges[face[i]].Add(face[j]);
                    }
                }
            }

            Debug.Log($"Found {boundaryEdges.Count} vertices on boundaries");
            if (boundaryEdges.Count == 0)
            {
                Debug.Log("No boundary edges found - mesh is closed");
                remainingHoleNum = 0;
                return true;
            }

            // Find boundary loops
            var boundaryLoops = new List<List<int>>();
            var usedEdges = new HashSet<(int, int)>();

            while (boundaryEdges.Count > 0)
            {
                var startVertex = boundaryEdges.Keys.First();
                var currentLoop = new List<int> { startVertex };
                var currentVertex = startVertex;

                Debug.Log($"Starting new boundary loop from vertex {startVertex}");

                while (true)
                {
                    if (!boundaryEdges.TryGetValue(currentVertex, out var nextVertices))
                    {
                        Debug.Log($"Failed to find next vertex from {currentVertex}");
                        return false;
                    }

                    // Find an unused edge
                    int nextVertex = -1;
                    foreach (var candidate in nextVertices)
                    {
                        if (!usedEdges.Contains((currentVertex, candidate)))
                        {
                            nextVertex = candidate;
                            break;
                        }
                    }

                    if (nextVertex == -1)
                    {
                        Debug.Log($"No unused edges found from vertex {currentVertex}");
                        break;
                    }

                    usedEdges.Add((currentVertex, nextVertex));

                    if (nextVertex == startVertex)
                    {
                        Debug.Log("Loop closed successfully");
                        break;
                    }

                    currentLoop.Add(nextVertex);
                    currentVertex = nextVertex;

                    // Safety check for infinite loops
                    if (currentLoop.Count > vertices.Count)
                    {
                        Debug.Log("Safety limit reached - possible infinite loop");
                        return false;
                    }
                }

                if (currentLoop.Count >= 3)
                {
                    Debug.Log($"Found valid boundary loop with {currentLoop.Count} vertices");
                    boundaryLoops.Add(currentLoop);
                }
                else
                {
                    Debug.Log($"Discarding invalid loop with only {currentLoop.Count} vertices");
                }

                // Remove used vertices from boundary edges
                foreach (var vertex in currentLoop)
                {
                    boundaryEdges.Remove(vertex);
                }
            }

            Debug.Log($"Found {boundaryLoops.Count} boundary loops");

            if (boundaryLoops.Count == 0)
            {
                Debug.Log("No valid boundary loops found");
                return false;
            }

            // Sort loops by perimeter length
            boundaryLoops.Sort((a, b) =>
            {
                float lengthA = CalculateLoopLength(a, vertices);
                float lengthB = CalculateLoopLength(b, vertices);
                return lengthB.CompareTo(lengthA);  // Descending order
            });

            // Triangulate all but the longest loop
            for (int i = 1; i < boundaryLoops.Count; i++)
            {
                Debug.Log($"Triangulating hole {i} with {boundaryLoops[i].Count} vertices");
                Triangulator.Triangulate(vertices, faces, boundaryLoops[i]);
            }

            remainingHoleNum = 1; // Keep the longest loop
            return true;
        }

        private static float CalculateLoopLength(List<int> loop, List<Vector3> vertices)
        {
            float length = 0;
            for (int i = 0; i < loop.Count; i++)
            {
                int j = (i + 1) % loop.Count;
                length += DistanceBetweenVertices(vertices[loop[i]], vertices[loop[j]]);
            }
            return length;
        }

        // Helper method to visualize the mesh connectivity
        public void PrintMeshConnectivity(List<List<int>> faces)
        {
            var vertexFaces = new Dictionary<int, List<int>>();

            for (int faceIndex = 0; faceIndex < faces.Count; faceIndex++)
            {
                var face = faces[faceIndex];
                for (int i = 0; i < 3; i++)
                {
                    if (!vertexFaces.ContainsKey(face[i]))
                        vertexFaces[face[i]] = new();
                    vertexFaces[face[i]].Add(faceIndex);
                }
            }

            Debug.Log("\nMesh Connectivity:");
            foreach (var kvp in vertexFaces.OrderBy(x => x.Key))
            {
                Debug.Log($"Vertex {kvp.Key} is connected to faces: {string.Join(", ", kvp.Value)}");
            }
        }

        private void MakeSeamAndCut(List<Vector3> vertices,
            List<List<int>> faces,
            Dictionary<int, int> localToGlobalFacesMap,
            out List<int> firstGroup,
            out List<int> secondGroup)
        {
            firstGroup = new();
            secondGroup = new();

            // Find top triangle (max Y)
            float maxY = float.MinValue;
            int chosenIndex = -1;

            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                for (int j = 0; j < 3; j++)
                {
                    float y = vertices[face[j]].y;
                    if (y > maxY)
                    {
                        maxY = y;
                        chosenIndex = i;
                    }
                }
            }

            if (chosenIndex == -1)
                return;

            var edgeToFaceMap = new Dictionary<(int, int), int>();
            BuildEdgeToFaceMap(faces, edgeToFaceMap);

            var processedFaces = new HashSet<int>();
            var waitFaces = new Queue<int>();
            waitFaces.Enqueue(chosenIndex);

            while (waitFaces.Count > 0)
            {
                int index = waitFaces.Dequeue();
                if (processedFaces.Contains(index))
                    continue;

                var face = faces[index];
                for (int i = 0; i < 3; i++)
                {
                    int j = (i + 1) % 3;
                    var oppositeEdge = (face[j], face[i]);
                    if (edgeToFaceMap.TryGetValue(oppositeEdge, out int oppositeFaceIndex))
                    {
                        waitFaces.Enqueue(oppositeFaceIndex);
                    }
                }

                processedFaces.Add(index);
                firstGroup.Add(localToGlobalFacesMap[index]);

                if (firstGroup.Count * 2 >= faces.Count)
                    break;
            }

            for (int index = 0; index < faces.Count; index++)
            {
                if (!processedFaces.Contains(index))
                {
                    secondGroup.Add(localToGlobalFacesMap[index]);
                }
            }
        }

        private void UnwrapSingleIsland(List<int> group, int sourcePartition, bool skipCheckHoles = false)
        {
            if (group.Count == 0)
            {
                Debug.Log("Empty group, skipping");
                return;
            }

            Debug.Log($"UnwrapSingleIsland: Processing group of {group.Count} faces");

            // Create local mesh
            var localVertices = new List<Vector3>();
            var localFaces = new List<List<int>>();
            var globalToLocalVerticesMap = new Dictionary<int, int>();
            var localToGlobalFacesMap = new Dictionary<int, int>();

            // Build local mesh
            for (int i = 0; i < group.Count; i++)
            {
                var globalFace = mesh.faces[group[i]];
                var localFace = new int[3].ToList();

                for (int j = 0; j < 3; j++)
                {
                    int globalVertexIndex = globalFace[j];
                    if (!globalToLocalVerticesMap.TryGetValue(globalVertexIndex, out int localIndex))
                    {
                        localVertices.Add(mesh.vertices[globalVertexIndex]);
                        localIndex = localVertices.Count - 1;
                        globalToLocalVerticesMap[globalVertexIndex] = localIndex;
                    }
                    localFace[j] = localIndex;
                }

                localFaces.Add(localFace);
                localToGlobalFacesMap[localFaces.Count - 1] = group[i];
            }

            Debug.Log($"Created local mesh with {localVertices.Count} vertices and {localFaces.Count} faces");

            int faceNumBeforeFix = localFaces.Count;
            if (!skipCheckHoles)
            {
                if (!FixHolesExceptTheLongestRing(localVertices, localFaces, out int remainingHoleNum))
                {
                    Debug.Log("Failed to fix holes");
                    return;
                }

                Debug.Log($"Fixed holes. Remaining holes: {remainingHoleNum}");

                if (remainingHoleNum == 1)
                {
                    Debug.Log("One hole remains, parametrizing group");
                    ParametrizeSingleGroup(localVertices, localFaces, localToGlobalFacesMap,
                        faceNumBeforeFix, sourcePartition);
                    return;
                }

                if (remainingHoleNum == 0)
                {
                    Debug.Log("No holes remain, making seam and cut");
                    MakeSeamAndCut(localVertices, localFaces, localToGlobalFacesMap,
                        out var firstGroup, out var secondGroup);

                    if (firstGroup.Count == 0 || secondGroup.Count == 0)
                    {
                        Debug.Log("Invalid seam cut results");
                        return;
                    }

                    Debug.Log($"Cut into groups of {firstGroup.Count} and {secondGroup.Count} faces");
                    UnwrapSingleIsland(firstGroup, sourcePartition, true);
                    UnwrapSingleIsland(secondGroup, sourcePartition, true);
                    return;
                }
            }
            else
            {
                Debug.Log("Skipping hole check, parametrizing group directly");
                ParametrizeSingleGroup(localVertices, localFaces, localToGlobalFacesMap,
                    faceNumBeforeFix, sourcePartition);
            }
        }

        private void ParametrizeSingleGroup(
            List<Vector3> vertices,
            List<List<int>> faces,
            Dictionary<int, int> localToGlobalFacesMap,
            int faceNumToChart,
            int sourcePartition)
        {
            var localVertexUvs = new List<Vector2>();
            if (!Parametrizer.Parametrize(vertices, faces, localVertexUvs))
                return;

            var chartFaces = new List<int>();
            var chartUvs = new List<List<Vector2>>();

            for (int i = 0; i < faceNumToChart; i++)
            {
                var localFace = faces[i];
                var globalFaceIndex = localToGlobalFacesMap[i];
                var faceUv = new Vector2[3].ToList();

                for (int j = 0; j < 3; j++)
                {
                    var localVertexIndex = localFace[j];
                    var vertexUv = localVertexUvs[localVertexIndex];
                    faceUv[j] = vertexUv;
                }

                chartFaces.Add(globalFaceIndex);
                chartUvs.Add(faceUv);
            }

            if (chartFaces.Count > 0)
            {
                Charts.Add((chartFaces, chartUvs));
                ChartSourcePartitions.Add(sourcePartition);
            }
        }

        public void Unwrap()
        {
            if (mesh == null)
                throw new InvalidOperationException("Mesh must be set before unwrapping");
            if (mesh.faces == null)
                throw new InvalidOperationException("Mesh faces collection is null");

            Partition();

            // Initialize faceUvs with properly constructed FaceTextureCoords
            FaceUvs = new List<List<Vector2>>(mesh.faces.Length);
            for (int i = 0; i < mesh.faces.Length; i++)
            {
                FaceUvs.Add(new List<Vector2>
                {
                    new Vector2(),
                    new Vector2(),
                    new Vector2()
                });
            }

            foreach (var group in partitions)
            {
                var islands = new List<List<int>>();
                SplitPartitionToIslands(group.Value, islands);
                foreach (var island in islands)
                {
                    UnwrapSingleIsland(island, group.Key);
                }
            }

            CalculateSizeAndRemoveInvalidCharts();
            PackCharts();
            FinalizeUv();
        }
    }
}