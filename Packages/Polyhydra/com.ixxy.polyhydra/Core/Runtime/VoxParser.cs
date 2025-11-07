using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Polyhydra.Core
{
    /// <summary>
    /// Parser for MagicaVoxel .vox file format
    /// Converts voxel data to mesh vertices and faces with optional face culling
    /// </summary>
    public class VoxParser
    {
        private struct Voxel
        {
            public byte X;
            public byte Y;
            public byte Z;
            public byte ColorIndex;

            public Voxel(byte x, byte y, byte z, byte colorIndex)
            {
                X = x;
                Y = y;
                Z = z;
                ColorIndex = colorIndex;
            }
        }

        private class VoxModel
        {
            public int SizeX;
            public int SizeY;
            public int SizeZ;
            public List<Voxel> Voxels = new List<Voxel>();
        }

        // Default MagicaVoxel palette (256 colors)
        private static readonly Color32[] DefaultPalette = new Color32[]
        {
            new Color32(255, 255, 255, 255), new Color32(255, 255, 204, 255), new Color32(255, 255, 153, 255), new Color32(255, 255, 102, 255),
            new Color32(255, 255, 51, 255), new Color32(255, 255, 0, 255), new Color32(255, 204, 255, 255), new Color32(255, 204, 204, 255),
            new Color32(255, 204, 153, 255), new Color32(255, 204, 102, 255), new Color32(255, 204, 51, 255), new Color32(255, 204, 0, 255),
            new Color32(255, 153, 255, 255), new Color32(255, 153, 204, 255), new Color32(255, 153, 153, 255), new Color32(255, 153, 102, 255),
            new Color32(255, 153, 51, 255), new Color32(255, 153, 0, 255), new Color32(255, 102, 255, 255), new Color32(255, 102, 204, 255),
            new Color32(255, 102, 153, 255), new Color32(255, 102, 102, 255), new Color32(255, 102, 51, 255), new Color32(255, 102, 0, 255),
            new Color32(255, 51, 255, 255), new Color32(255, 51, 204, 255), new Color32(255, 51, 153, 255), new Color32(255, 51, 102, 255),
            new Color32(255, 51, 51, 255), new Color32(255, 51, 0, 255), new Color32(255, 0, 255, 255), new Color32(255, 0, 204, 255),
            new Color32(255, 0, 153, 255), new Color32(255, 0, 102, 255), new Color32(255, 0, 51, 255), new Color32(255, 0, 0, 255),
            new Color32(204, 255, 255, 255), new Color32(204, 255, 204, 255), new Color32(204, 255, 153, 255), new Color32(204, 255, 102, 255),
            new Color32(204, 255, 51, 255), new Color32(204, 255, 0, 255), new Color32(204, 204, 255, 255), new Color32(204, 204, 204, 255),
            new Color32(204, 204, 153, 255), new Color32(204, 204, 102, 255), new Color32(204, 204, 51, 255), new Color32(204, 204, 0, 255),
            new Color32(204, 153, 255, 255), new Color32(204, 153, 204, 255), new Color32(204, 153, 153, 255), new Color32(204, 153, 102, 255),
            new Color32(204, 153, 51, 255), new Color32(204, 153, 0, 255), new Color32(204, 102, 255, 255), new Color32(204, 102, 204, 255),
            new Color32(204, 102, 153, 255), new Color32(204, 102, 102, 255), new Color32(204, 102, 51, 255), new Color32(204, 102, 0, 255),
            new Color32(204, 51, 255, 255), new Color32(204, 51, 204, 255), new Color32(204, 51, 153, 255), new Color32(204, 51, 102, 255),
            new Color32(204, 51, 51, 255), new Color32(204, 51, 0, 255), new Color32(204, 0, 255, 255), new Color32(204, 0, 204, 255),
            new Color32(204, 0, 153, 255), new Color32(204, 0, 102, 255), new Color32(204, 0, 51, 255), new Color32(204, 0, 0, 255),
            new Color32(153, 255, 255, 255), new Color32(153, 255, 204, 255), new Color32(153, 255, 153, 255), new Color32(153, 255, 102, 255),
            new Color32(153, 255, 51, 255), new Color32(153, 255, 0, 255), new Color32(153, 204, 255, 255), new Color32(153, 204, 204, 255),
            new Color32(153, 204, 153, 255), new Color32(153, 204, 102, 255), new Color32(153, 204, 51, 255), new Color32(153, 204, 0, 255),
            new Color32(153, 153, 255, 255), new Color32(153, 153, 204, 255), new Color32(153, 153, 153, 255), new Color32(153, 153, 102, 255),
            new Color32(153, 153, 51, 255), new Color32(153, 153, 0, 255), new Color32(153, 102, 255, 255), new Color32(153, 102, 204, 255),
            new Color32(153, 102, 153, 255), new Color32(153, 102, 102, 255), new Color32(153, 102, 51, 255), new Color32(153, 102, 0, 255),
            new Color32(153, 51, 255, 255), new Color32(153, 51, 204, 255), new Color32(153, 51, 153, 255), new Color32(153, 51, 102, 255),
            new Color32(153, 51, 51, 255), new Color32(153, 51, 0, 255), new Color32(153, 0, 255, 255), new Color32(153, 0, 204, 255),
            new Color32(153, 0, 153, 255), new Color32(153, 0, 102, 255), new Color32(153, 0, 51, 255), new Color32(153, 0, 0, 255),
            new Color32(102, 255, 255, 255), new Color32(102, 255, 204, 255), new Color32(102, 255, 153, 255), new Color32(102, 255, 102, 255),
            new Color32(102, 255, 51, 255), new Color32(102, 255, 0, 255), new Color32(102, 204, 255, 255), new Color32(102, 204, 204, 255),
            new Color32(102, 204, 153, 255), new Color32(102, 204, 102, 255), new Color32(102, 204, 51, 255), new Color32(102, 204, 0, 255),
            new Color32(102, 153, 255, 255), new Color32(102, 153, 204, 255), new Color32(102, 153, 153, 255), new Color32(102, 153, 102, 255),
            new Color32(102, 153, 51, 255), new Color32(102, 153, 0, 255), new Color32(102, 102, 255, 255), new Color32(102, 102, 204, 255),
            new Color32(102, 102, 153, 255), new Color32(102, 102, 102, 255), new Color32(102, 102, 51, 255), new Color32(102, 102, 0, 255),
            new Color32(102, 51, 255, 255), new Color32(102, 51, 204, 255), new Color32(102, 51, 153, 255), new Color32(102, 51, 102, 255),
            new Color32(102, 51, 51, 255), new Color32(102, 51, 0, 255), new Color32(102, 0, 255, 255), new Color32(102, 0, 204, 255),
            new Color32(102, 0, 153, 255), new Color32(102, 0, 102, 255), new Color32(102, 0, 51, 255), new Color32(102, 0, 0, 255),
            new Color32(51, 255, 255, 255), new Color32(51, 255, 204, 255), new Color32(51, 255, 153, 255), new Color32(51, 255, 102, 255),
            new Color32(51, 255, 51, 255), new Color32(51, 255, 0, 255), new Color32(51, 204, 255, 255), new Color32(51, 204, 204, 255),
            new Color32(51, 204, 153, 255), new Color32(51, 204, 102, 255), new Color32(51, 204, 51, 255), new Color32(51, 204, 0, 255),
            new Color32(51, 153, 255, 255), new Color32(51, 153, 204, 255), new Color32(51, 153, 153, 255), new Color32(51, 153, 102, 255),
            new Color32(51, 153, 51, 255), new Color32(51, 153, 0, 255), new Color32(51, 102, 255, 255), new Color32(51, 102, 204, 255),
            new Color32(51, 102, 153, 255), new Color32(51, 102, 102, 255), new Color32(51, 102, 51, 255), new Color32(51, 102, 0, 255),
            new Color32(51, 51, 255, 255), new Color32(51, 51, 204, 255), new Color32(51, 51, 153, 255), new Color32(51, 51, 102, 255),
            new Color32(51, 51, 51, 255), new Color32(51, 51, 0, 255), new Color32(51, 0, 255, 255), new Color32(51, 0, 204, 255),
            new Color32(51, 0, 153, 255), new Color32(51, 0, 102, 255), new Color32(51, 0, 51, 255), new Color32(51, 0, 0, 255),
            new Color32(0, 255, 255, 255), new Color32(0, 255, 204, 255), new Color32(0, 255, 153, 255), new Color32(0, 255, 102, 255),
            new Color32(0, 255, 51, 255), new Color32(0, 255, 0, 255), new Color32(0, 204, 255, 255), new Color32(0, 204, 204, 255),
            new Color32(0, 204, 153, 255), new Color32(0, 204, 102, 255), new Color32(0, 204, 51, 255), new Color32(0, 204, 0, 255),
            new Color32(0, 153, 255, 255), new Color32(0, 153, 204, 255), new Color32(0, 153, 153, 255), new Color32(0, 153, 102, 255),
            new Color32(0, 153, 51, 255), new Color32(0, 153, 0, 255), new Color32(0, 102, 255, 255), new Color32(0, 102, 204, 255),
            new Color32(0, 102, 153, 255), new Color32(0, 102, 102, 255), new Color32(0, 102, 51, 255), new Color32(0, 102, 0, 255),
            new Color32(0, 51, 255, 255), new Color32(0, 51, 204, 255), new Color32(0, 51, 153, 255), new Color32(0, 51, 102, 255),
            new Color32(0, 51, 51, 255), new Color32(0, 51, 0, 255), new Color32(0, 0, 255, 255), new Color32(0, 0, 204, 255),
            new Color32(0, 0, 153, 255), new Color32(0, 0, 102, 255), new Color32(0, 0, 51, 255), new Color32(0, 0, 0, 255),
            new Color32(238, 0, 0, 255), new Color32(221, 0, 0, 255), new Color32(187, 0, 0, 255), new Color32(170, 0, 0, 255),
            new Color32(136, 0, 0, 255), new Color32(119, 0, 0, 255), new Color32(85, 0, 0, 255), new Color32(68, 0, 0, 255),
            new Color32(34, 0, 0, 255), new Color32(17, 0, 0, 255), new Color32(0, 238, 0, 255), new Color32(0, 221, 0, 255),
            new Color32(0, 187, 0, 255), new Color32(0, 170, 0, 255), new Color32(0, 136, 0, 255), new Color32(0, 119, 0, 255),
            new Color32(0, 85, 0, 255), new Color32(0, 68, 0, 255), new Color32(0, 34, 0, 255), new Color32(0, 17, 0, 255),
            new Color32(0, 0, 238, 255), new Color32(0, 0, 221, 255), new Color32(0, 0, 187, 255), new Color32(0, 0, 170, 255),
            new Color32(0, 0, 136, 255), new Color32(0, 0, 119, 255), new Color32(0, 0, 85, 255), new Color32(0, 0, 68, 255),
            new Color32(0, 0, 34, 255), new Color32(0, 0, 17, 255), new Color32(238, 238, 238, 255), new Color32(221, 221, 221, 255),
            new Color32(187, 187, 187, 255), new Color32(170, 170, 170, 255), new Color32(136, 136, 136, 255), new Color32(119, 119, 119, 255),
            new Color32(85, 85, 85, 255), new Color32(68, 68, 68, 255), new Color32(34, 34, 34, 255), new Color32(17, 17, 17, 255)
        };

        public bool CullInternalFaces { get; set; } = true;

        /// <summary>
        /// Parse a .vox file and convert to mesh data
        /// </summary>
        public bool Parse(byte[] fileData, out List<Vector3> vertices, out List<IEnumerable<int>> faceIndices, out List<Color> vertexColors)
        {
            vertices = new List<Vector3>();
            faceIndices = new List<IEnumerable<int>>();
            vertexColors = new List<Color>();

            try
            {
                using (var stream = new MemoryStream(fileData))
                using (var reader = new BinaryReader(stream))
                {
                    // Read header
                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (magic != "VOX ")
                    {
                        Debug.LogError("Invalid .vox file: missing VOX magic header");
                        return false;
                    }

                    int version = reader.ReadInt32();

                    // Read MAIN chunk
                    string mainId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (mainId != "MAIN")
                    {
                        Debug.LogError("Invalid .vox file: missing MAIN chunk");
                        return false;
                    }

                    int mainContentSize = reader.ReadInt32();
                    int mainChildrenSize = reader.ReadInt32();

                    // Parse child chunks
                    Color32[] palette = DefaultPalette;
                    List<VoxModel> models = new List<VoxModel>();
                    VoxModel currentModel = null;

                    long endPosition = stream.Position + mainChildrenSize;
                    while (stream.Position < endPosition)
                    {
                        string chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                        int contentSize = reader.ReadInt32();
                        int childrenSize = reader.ReadInt32();
                        long chunkEndPos = stream.Position + contentSize + childrenSize;

                        switch (chunkId)
                        {
                            case "SIZE":
                                currentModel = new VoxModel();
                                currentModel.SizeX = reader.ReadInt32();
                                currentModel.SizeY = reader.ReadInt32();
                                currentModel.SizeZ = reader.ReadInt32();
                                models.Add(currentModel);
                                break;

                            case "XYZI":
                                if (currentModel != null)
                                {
                                    int numVoxels = reader.ReadInt32();
                                    for (int i = 0; i < numVoxels; i++)
                                    {
                                        byte x = reader.ReadByte();
                                        byte y = reader.ReadByte();
                                        byte z = reader.ReadByte();
                                        byte colorIndex = reader.ReadByte();
                                        currentModel.Voxels.Add(new Voxel(x, y, z, colorIndex));
                                    }
                                }
                                break;

                            case "RGBA":
                                palette = new Color32[256];
                                for (int i = 0; i < 256; i++)
                                {
                                    byte r = reader.ReadByte();
                                    byte g = reader.ReadByte();
                                    byte b = reader.ReadByte();
                                    byte a = reader.ReadByte();
                                    palette[i] = new Color32(r, g, b, a);
                                }
                                break;
                        }

                        // Skip to end of chunk
                        stream.Position = chunkEndPos;
                    }

                    // Convert voxels to mesh
                    if (models.Count > 0)
                    {
                        ConvertVoxelsToMesh(models[0], palette, vertices, faceIndices, vertexColors);
                        return true;
                    }
                    else
                    {
                        Debug.LogError("No models found in .vox file");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing .vox file: {e.Message}");
                return false;
            }
        }

        private void ConvertVoxelsToMesh(VoxModel model, Color32[] palette,
            List<Vector3> vertices, List<IEnumerable<int>> faceIndices, List<Color> vertexColors)
        {
            // Create a 3D grid to track occupied voxels for culling
            HashSet<(int, int, int)> voxelGrid = new HashSet<(int, int, int)>();
            Dictionary<(int, int, int), byte> voxelColors = new Dictionary<(int, int, int), byte>();

            foreach (var voxel in model.Voxels)
            {
                voxelGrid.Add((voxel.X, voxel.Y, voxel.Z));
                voxelColors[(voxel.X, voxel.Y, voxel.Z)] = voxel.ColorIndex;
            }

            // Vertex deduplication - map positions to indices for manifold topology
            Dictionary<Vector3, int> vertexLookup = new Dictionary<Vector3, int>();

            // Define cube face normals (6 faces per cube)
            Vector3Int[] faceDirections = new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),   // Right
                new Vector3Int(-1, 0, 0),  // Left
                new Vector3Int(0, 1, 0),   // Top
                new Vector3Int(0, -1, 0),  // Bottom
                new Vector3Int(0, 0, 1),   // Front
                new Vector3Int(0, 0, -1)   // Back
            };

            // Face vertices relative to cube origin (0,0,0 to 1,1,1)
            Vector3[][] faceVertices = new Vector3[][]
            {
                // Right (+X)
                new Vector3[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) },
                // Left (-X)
                new Vector3[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) },
                // Top (+Y)
                new Vector3[] { new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(0, 1, 1) },
                // Bottom (-Y)
                new Vector3[] { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 0, 0) },
                // Front (+Z)
                new Vector3[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 0, 1) },
                // Back (-Z)
                new Vector3[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 0) }
            };

            // Generate faces for each voxel
            foreach (var voxel in model.Voxels)
            {
                Vector3 voxelPos = new Vector3(voxel.X, voxel.Y, voxel.Z);
                Color32 color = palette[voxel.ColorIndex - 1]; // Color indices are 1-based

                for (int faceIdx = 0; faceIdx < 6; faceIdx++)
                {
                    Vector3Int dir = faceDirections[faceIdx];
                    int neighborX = voxel.X + dir.x;
                    int neighborY = voxel.Y + dir.y;
                    int neighborZ = voxel.Z + dir.z;

                    // Check if this face should be generated
                    bool hasNeighbor = voxelGrid.Contains((neighborX, neighborY, neighborZ));
                    bool shouldGenerateFace = !CullInternalFaces || !hasNeighbor;

                    if (shouldGenerateFace)
                    {
                        List<int> face = new List<int>();

                        // Add face vertices with deduplication for manifold topology
                        int[] faceVertexIndices = new int[4];
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 vertexPos = voxelPos + faceVertices[faceIdx][i];

                            // Check if vertex already exists
                            if (!vertexLookup.TryGetValue(vertexPos, out int vertexIndex))
                            {
                                // New vertex - add it
                                vertexIndex = vertices.Count;
                                vertices.Add(vertexPos);
                                vertexColors.Add(color);
                                vertexLookup[vertexPos] = vertexIndex;
                            }

                            faceVertexIndices[i] = vertexIndex;
                        }

                        // Reverse winding order (like ParseOff does)
                        for (int i = 3; i >= 0; i--)
                        {
                            face.Add(faceVertexIndices[i]);
                        }

                        faceIndices.Add(face);
                    }
                }
            }
        }
    }
}
