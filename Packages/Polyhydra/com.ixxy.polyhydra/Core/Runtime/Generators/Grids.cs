using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
	public static class GridEnums
	{
		public enum GridShapes
		{
			Plane,
			// Torus,
			Cylinder,
			Cone,
			Sphere,
			Polar,
		}

		public enum GridTypes
		{
			// Regular
			Triangular = 0,
			Square = 1,
			Hexagonal = 2,

			// Archimedean, uniform or semiregular
			SnubTrihexagonal = 3,
			ElongatedTriangular = 4, // 3.3.3.4.4
			SnubSquare = 5, // 3.3.4.3.4
			Rhombitrihexagonal = 6, // 3.4.6.4
			Trihexagonal = 7, // 3.6.3.6
			TruncatedHexagonal = 8, // 3.12.12
			TruncatedTrihexagonal = 9, // 4.6.12
			TruncatedSquare = 10, // 4.8.8

			// Laves or Catalan
			TetrakisSquare = 34, // Dual of TruncatedSquare
			CairoPentagonal = 35, // Dual of SnubSquare
			Rhombille = 36, // Dual of Trihexagonal
			TriakisTriangular = 37, // Dual of TruncatedHexagonal
			DeltoidalTrihexagonal = 38, // Dual of Rhombitrihexagonal
			Kisrhombille = 39, // Dual of TruncatedTrihexagonal
			FloretPentagonal = 40, // Dual of SnubTrihexagonal
			PrismaticPentagonal = 41, // Dual of ElongatedTriangular

			// Durer
			Durer1 = 16,
			Durer2 = 17,

			// 2-Uniform
			DissectedRhombitrihexagonal = 18, // 3.3.3.3.3.3;3.3.4.3.4
			DissectedTruncatedHexagonal1  = 19, // 3.4.6.4;3.3.4.3.4
			DissectedTruncatedHexagonal2 = 20, // 3.4.6.4;3.3.3.4.4
			HexagonalTruncatedTriangular = 21, // 3.4.6.4;3.4.4.6 ??
			DemiregularHexagonal = 22, // 4.6.12;3.4.6.4
			DissectedTruncatedTrihexagonal1 = 12, // 3.3.3.3.3.3;3.3.4.12
			DemiregularSquare = 14, // 3.12.12;3.4.3.12

			DissectedHexagonal1 = 23, // 3.3.3.3.3.3;3.3.6.6
			DissectedHexagonal2 = 24, // 3.3.3.3.3.3;3.3.3.3.6: A
			DissectedHexagonal3 = 25, // 3.3.3.3.3.3;3.3.3.3.6: B ??
			AlternatingTrihexagonal = 26, // 3.3.6.6;3.3.3.3.6 ???
			DissectedRhombiHexagonal = 13, // 3.6.3.6;3.3.6.6
			AlternatingTrihexSquare = 27, // 3.4.4.6;3.6.3.6: A ???
			TrihexSquare = 15, // 3.4.4.6;3.6.3.6: B

			AlternatingTriSquare = 28, // 3.3.3.4.4;3.3.4.3.4: A
			SemiSnubTriSquare = 29, // 3.3.3.4.4;3.3.4.3.4: B
			TriSquareSquare1 = 30, // 4.4.4.4;3.3.3.4.4: A
			TriSquareSquare2 = 31, // 4.4.4.4;3.3.3.4.4: B
			TriTriSquare1 = 32, // 3.3.3.3.3.3;3.3.3.4.4: A
			TriTriSquare2 = 33, // 3.3.3.3.3.3;3.3.3.4.4: B
		}

	}

	public static class Grids
    {
	    public struct TileDef
	    {
		    public PolyMesh tile;
		    public GridEnums.GridShapes gridShape;
		    public bool offsetAlternateRows;
		    public bool alternateRows;
		    public List<List<List<Roles>>> roleSet;
		    public Vector3 xOffset;
		    public Vector3 yOffset;
		    public int xRepeats;
		    public int yRepeats;
	    }

        public static PolyMesh MakePolarGrid(int sides = 6, int divisions = 4)
		{

			var vertexPoints = new List<Vector3>();
			var faceIndices = new List<List<int>>();

			var faceRoles = new List<Roles>();

			float theta = Mathf.PI * 2 / sides;

			int start, end, inc;

			start = 0;
			end = sides;
			inc = 1;
			float radiusStep = 1f / divisions;

			vertexPoints.Add(Vector3.zero);

			for (float radius = radiusStep; radius < 1f + radiusStep; radius += radiusStep)
			{
				for (int i = start; i != end; i += inc)
				{
					float angle = theta * i + theta;
					vertexPoints.Add(new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius));
				}
			}

			for (int i = 0; i < sides; i++)
			{
				faceIndices.Add(new List<int>{0, (i + 1) % sides + 1, i + 1});
				if (sides % 2 == 0) // Even sides
				{
					faceRoles.Add((Roles)(i % 2) + 2);
				}
				else
				{
					int lastCellMod = (i == sides - 1 && sides % 3 == 1) ? 1 : 0;  //  Fudge the last cell to stop clashes in some cases
					faceRoles.Add((Roles)((i + lastCellMod) % 3) + 2);
				}
			}

			for (int d = 0; d < divisions - 1; d++)
			{
				for (int i = 0; i < sides; i++)
				{
					int rowStart = d * sides + 1;
					int nextRowStart = (d + 1) * sides + 1;
					faceIndices.Add(new List<int>
					{
						rowStart + i,
						rowStart + (i + 1) % sides,
						nextRowStart + (i + 1) % sides,
						nextRowStart + i
					});
					if (sides % 2 == 0) // Even sides
					{
						faceRoles.Add((Roles)((i + d) % 2) + 2);
					}
					else
					{
						int lastCellMod = (i == sides - 1 && sides % 3 == 1) ? 1 : 0;  //  Fudge the last cell to stop clashes in some cases
						faceRoles.Add((Roles)((i + d + lastCellMod + 1) % 3) + 2);
					}
				}
			}

			var vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count);
			return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles);
		}

        public static TileDef BuildTileDefFromFormat(string format)
        {
	        PolyMesh tile;

	        var lines = format.Split('\n');
	        var parts = lines[0].Split(' ');
	        int sides = Int32.Parse(parts[0]);
	        float angle = parts.Length > 1 ? float.Parse(parts[1]) : 0;
	        var roles = new List<Roles> { parts.Length > 2 ? (Roles)int.Parse(parts[2]) : Roles.Existing };
	        tile = Shapes.Polygon(sides);
	        tile = tile.Rotate(Vector3.up, angle);
	        var xOffsetLine = lines[1].Split(' ');
	        var xOffsetIndex0 = int.Parse(xOffsetLine[0]);
	        var xOffsetIndex1 = int.Parse(xOffsetLine[1]);
	        var yOffsetLine = lines[2].Split(' ');
	        var yOffsetIndex0 = int.Parse(yOffsetLine[0]);
	        var yOffsetIndex1 = int.Parse(yOffsetLine[1]);

	        foreach (var line in lines.Skip(3))
	        {
		        parts = line.Trim().Split(' ');
		        if (string.IsNullOrEmpty(line) || parts.Length < 1)
		        {
			        continue;
		        }
		        int parentFaceIndex = int.Parse(parts[0]);
		        int parentEdgeIndex = parts.Length > 1 ? int.Parse(parts[1]) : 0;
		        sides = parts.Length > 2 ? int.Parse(parts[2]) : 4;
		        tile.ExtendFace(parentFaceIndex, parentEdgeIndex, sides);
			    roles.Add(parts.Length > 3 ? (Roles)int.Parse(parts[3]) : Roles.ExistingAlt);
	        }

	        Vector3 xOffset = default, yOffset = default;
	        if (xOffsetIndex0 < tile.Vertices.Count && xOffsetIndex1 < tile.Vertices.Count)
	        {
		        xOffset = tile.Vertices[xOffsetIndex0].Position - tile.Vertices[xOffsetIndex1].Position;
	        }

	        if (yOffsetIndex0 < tile.Vertices.Count && yOffsetIndex1 < tile.Vertices.Count)
	        {
		        yOffset = tile.Vertices[yOffsetIndex0].Position - tile.Vertices[yOffsetIndex1].Position;
	        }

	        return new TileDef
	        {
		        tile = tile,
		        roleSet = new List<List<List<Roles>>>{ new() {roles}},
		        xOffset = xOffset,
		        yOffset = yOffset,
		        alternateRows = true,
		        offsetAlternateRows = false
	        };
        }

        public static PolyMesh BuildGridFromTileDef(TileDef tiledef)
        {
	        PolyMesh poly = new PolyMesh();
	        var newFaceRoles = new List<Roles>();
	        var xCentering = ((tiledef.xOffset * (tiledef.xRepeats - 1)) / 2f);
	        xCentering.y = 0;
	        var yCentering = ((tiledef.yOffset * (tiledef.yRepeats - 1)) / 2f);
	        yCentering.x = 0;

	        int roleCountY = tiledef.roleSet.Count;
	        int roleCountX = tiledef.roleSet[0].Count;
	        int tileCount = tiledef.tile.Faces.Count;
	        for (int y = 0; y < tiledef.yRepeats; y++)
	        {
		        var rowOffset = tiledef.offsetAlternateRows ? y % roleCountX : 0;
		        for (int x = 0; x < tiledef.xRepeats; x++)
		        {
			        var colOffset = tiledef.alternateRows && y % 2 == 0 ? 1 : 0;
			        // Vector3 tileOffset = (xOffset * x) + (yOffset * y - yCentering);
			        Vector3 tileOffset = (tiledef.xOffset * x - xCentering) + (tiledef.yOffset * y - yCentering);
			        if (tiledef.gridShape != GridEnums.GridShapes.Plane)
			        {
				        // I can't remember why we do this :-(
				        if (y % 2 == 0)
				        {
					        tileOffset.x -= tiledef.yOffset.x * Mathf.FloorToInt(y);
				        }
				        else
				        {
					        tileOffset.x -= tiledef.yOffset.x * Mathf.FloorToInt(y + 1);
				        }
			        }
			        poly.Append(tiledef.tile, tileOffset, forceDuplicate: true);

			        int rowRoleIndex = (y % roleCountY) % tiledef.roleSet.Count;
			        var rowRoles = tiledef.roleSet[rowRoleIndex];
			        int colRoleIndex = ((x + colOffset) % roleCountX) % rowRoles.Count;
			        var roles = rowRoles[colRoleIndex].GetRange(rowOffset, tileCount);
			        newFaceRoles.AddRange(roles);
		        }
	        }

	        poly.FaceRoles = newFaceRoles;

	        float width = tiledef.xRepeats * tiledef.xOffset.x;
	        float heightScale = (1f/width) * Mathf.PI;
	        float maxHeight = poly.Vertices.Max(x => x.Position.z) * 2f * heightScale;

	        switch (tiledef.gridShape)
	        {
		        case GridEnums.GridShapes.Polar:
		        case GridEnums.GridShapes.Cylinder:
		        case GridEnums.GridShapes.Cone:
		        case GridEnums.GridShapes.Sphere:
			        poly.Scale(new Vector3(1f/width, 1, 1));
			        poly = ShapeWrap(poly, tiledef.gridShape, heightScale, maxHeight);
			        break;
		        case GridEnums.GridShapes.Plane:
			        poly = poly.Weld(0.01f);
			        break;
	        }

	        poly.DebugVerts = tiledef.tile.DebugVerts;
	        return poly;
        }

        public static PolyMesh Build(GridEnums.GridTypes type, GridEnums.GridShapes gridShape, int xRepeats,
	        int yRepeats)
        {
	        var xOffset = Vector3.zero;
	        var yOffset = Vector3.zero;
	        PolyMesh tile = null;
	        bool offsetAlternateRows = true;
	        bool alternateRows = false;
	        List<List<List<Roles>>> roleSet = null;
	        string format = null;
	        TileDef tileDef = default;

	        switch (type)
	        {
		        case GridEnums.GridTypes.Triangular:
			        tile = Shapes.Polygon(3);
			        tile = tile.Rotate(Vector3.up, 30);
			        tile.ExtendFace(0, 0, 3);
			        xOffset = tile.Vertices[0].Position - tile.Vertices[2].Position;
			        yOffset = tile.Vertices[3].Position - tile.Vertices[0].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
					        { new List<Roles> { Roles.Existing, Roles.New } }
			        };
			        break;
		        case GridEnums.GridTypes.Square:
			        tile = Shapes.Polygon(4);
			        tile = tile.Rotate(Vector3.up, 45);
			        xOffset = tile.Vertices[0].Position - tile.Vertices[3].Position;
			        yOffset = tile.Vertices[0].Position - tile.Vertices[1].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles> { Roles.Existing, Roles.New },
					        new List<Roles> { Roles.New, Roles.Existing },
				        }
			        };
			        break;
		        case GridEnums.GridTypes.Hexagonal:
			        tile = Shapes.Polygon(6);
			        tile = tile.Rotate(Vector3.up, 30);
			        xOffset = tile.Vertices[1].Position - tile.Vertices[5].Position;
			        yOffset = tile.Vertices[1].Position - tile.Vertices[3].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles> { Roles.Existing, Roles.NewAlt, Roles.New },
					        new List<Roles> { Roles.New, Roles.Existing, Roles.NewAlt },
					        new List<Roles> { Roles.NewAlt, Roles.New, Roles.Existing },
				        }
			        };
			        break;
		        case GridEnums.GridTypes.SnubTrihexagonal:
			        tile = Shapes.Polygon(6);
			        tile = tile.Rotate(Vector3.up, -19);
			        tile.ExtendFace(0, 0, 3);
			        tile.ExtendFace(0, 1, 3);
			        tile.ExtendFace(0, 2, 3);
			        tile.ExtendFace(1, 2, 3);
			        tile.ExtendFace(1, 3, 3);
			        tile.ExtendFace(2, 2, 3);
			        tile.ExtendFace(2, 3, 3);
			        tile.ExtendFace(3, 2, 3);
			        xOffset = tile.Vertices[8].Position - tile.Vertices[10].Position;
			        yOffset = tile.Vertices[10].Position - tile.Vertices[4].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.New,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.New,
						        Roles.NewAlt
					        }
				        }
			        };
			        break;
		        case GridEnums.GridTypes.ElongatedTriangular:
			        tile = Shapes.Polygon(4);
			        tile = tile.Rotate(Vector3.up, -45);
			        tile.ExtendFace(0, 1, 3);
			        tile.ExtendFace(0, 3, 3);
			        xOffset = tile.Vertices[2].Position - tile.Vertices[3].Position;
			        yOffset = tile.Vertices[4].Position - tile.Vertices[2].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.Ignored,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.New,
						        Roles.NewAlt,
					        },
					        new List<Roles>
					        {
						        Roles.Existing,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.New,
						        Roles.NewAlt,
					        },
				        }
			        };
			        offsetAlternateRows = false;
			        break;
		        case GridEnums.GridTypes.SnubSquare:
			        tile = Shapes.Polygon(3);
			        tile = tile.Rotate(Vector3.up, 30);
			        tile.ExtendFace(0, 2, 4);
			        tile.ExtendFace(0, 1, 4);
			        tile.ExtendFace(1, 2, 3);
			        tile.ExtendFace(2, 0, 3);
			        tile.ExtendFace(2, 3, 3);
			        xOffset = tile.Vertices[5].Position - tile.Vertices[4].Position;
			        yOffset = tile.Vertices[5].Position - tile.Vertices[7].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.Ignored,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.Existing,
					        }
				        }
			        };
			        break;
		        case GridEnums.GridTypes.Rhombitrihexagonal:
			        tile = Shapes.Polygon(6);
			        tile = tile.Rotate(Vector3.up, 30);
			        tile.ExtendFace(0, 0, 4);
			        tile.ExtendFace(0, 1, 4);
			        tile.ExtendFace(0, 2, 4);
			        tile.ExtendFace(1, 0, 3);
			        tile.ExtendFace(2, 0, 3);
			        xOffset = tile.Vertices[11].Position - tile.Vertices[4].Position;
			        yOffset = tile.Vertices[9].Position - tile.Vertices[3].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.Ignored,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.Existing,
					        }
				        }
			        };
			        break;
		        case GridEnums.GridTypes.Trihexagonal:
			        tile = Shapes.Polygon(6);
			        tile = tile.Rotate(Vector3.up, 30);
			        tile.ExtendFace(0, 0, 3);
			        tile.ExtendFace(0, 1, 3);
			        xOffset = tile.Vertices[1].Position - tile.Vertices[4].Position;
			        yOffset = tile.Vertices[7].Position - tile.Vertices[2].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.New,
					        },
					        new List<Roles>
					        {
						        Roles.NewAlt,
						        Roles.Existing,
						        Roles.New,
					        },
				        }
			        };
			        offsetAlternateRows = false;
			        break;
		        case GridEnums.GridTypes.TruncatedHexagonal:
			        tile = Shapes.Polygon(12);
			        tile = tile.Rotate(Vector3.up, 45);
			        tile.ExtendFace(0, 7, 3);
			        tile.ExtendFace(0, 9, 3);
			        xOffset = tile.Vertices[4].Position - tile.Vertices[9].Position;
			        yOffset = tile.Vertices[2].Position - tile.Vertices[7].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.NewAlt,
					        }
				        }
			        };
			        break;
		        case GridEnums.GridTypes.TruncatedTrihexagonal:
			        tile = Shapes.Polygon(12);
			        tile = tile.Rotate(Vector3.up, 45);
			        tile.ExtendFace(0, 0, 4);
			        tile.ExtendFace(0, 2, 4);
			        tile.ExtendFace(0, 4, 4);
			        tile.ExtendFace(1, 4, 6);
			        tile.ExtendFace(2, 4, 6);
			        xOffset = tile.Vertices[16].Position - tile.Vertices[10].Position;
			        yOffset = tile.Vertices[15].Position - tile.Vertices[7].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.New,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.NewAlt,
					        }
				        }
			        };
			        break;
		        case GridEnums.GridTypes.TruncatedSquare:
			        tile = Shapes.Polygon(8);
			        tile = tile.Rotate(Vector3.up, -22.5f);
			        tile.ExtendFace(0, 1, 4);
			        xOffset = tile.Vertices[2].Position - tile.Vertices[8].Position;
			        yOffset = tile.Vertices[9].Position - tile.Vertices[4].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.NewAlt,
					        },
					        new List<Roles>
					        {
						        Roles.Existing,
						        Roles.New,
						        Roles.NewAlt,
					        },
				        }
			        };
			        offsetAlternateRows = false;
			        break;
		        case GridEnums.GridTypes.DissectedTruncatedTrihexagonal1:
			        tile = Shapes.Polygon(12);
			        tile = tile.Rotate(Vector3.up, 15);
			        tile.ExtendFace(0, 0, 3);
			        tile.ExtendFace(0, 1, 4);
			        tile.ExtendFace(0, 2, 3);
			        tile.ExtendFace(0, 3, 4);
			        tile.ExtendFace(0, 4, 3);
			        tile.ExtendFace(0, 5, 4);
			        tile.ExtendFace(0, 6, 3);
			        tile.ExtendFace(0, 8, 3);
			        tile.ExtendFace(0, 10, 3);

			        tile.ExtendFace(2, 0, 3);
			        tile.ExtendFace(2, 2, 3);

			        tile.ExtendFace(4, 0, 3);
			        tile.ExtendFace(4, 2, 3);

			        tile.ExtendFace(6, 0, 3);
			        tile.ExtendFace(6, 2, 3);

			        xOffset = tile.Vertices[20].Position - tile.Vertices[10].Position;
			        yOffset = tile.Vertices[17].Position - tile.Vertices[8].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.Ignored,
						        Roles.New,
						        Roles.Ignored,
						        Roles.New,
						        Roles.Ignored,
						        Roles.New,
						        Roles.New,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.NewAlt,
					        }
				        }
			        };
			        break;
		        case GridEnums.GridTypes.DissectedRhombiHexagonal:
			        tile = Shapes.Polygon(6);
			        tile = tile.Rotate(Vector3.up, 0);
			        tile.ExtendFace(0, 5, 3);
			        tile.ExtendFace(0, 0, 3);
			        xOffset = tile.Vertices[2].Position - tile.Vertices[5].Position;
			        yOffset = tile.Vertices[1].Position - tile.Vertices[3].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.New,
						        Roles.Existing,
						        Roles.Ignored,
					        },
					        new List<Roles>
					        {
						        Roles.NewAlt,
						        Roles.Existing,
						        Roles.Ignored,
					        },
				        }
			        };
			        offsetAlternateRows = false;
			        alternateRows = true;
			        break;
		        case GridEnums.GridTypes.DemiregularSquare:
			        tile = Shapes.Polygon(12);
			        tile = tile.Rotate(Vector3.up, 15);
			        tile.ExtendFace(0, 1, 3);
			        tile.ExtendFace(0, 0, 3);
			        tile.ExtendFace(1, 2, 4);
			        tile.ExtendFace(3, 0, 3);
			        tile.ExtendFace(3, 3, 3);
			        xOffset = tile.Vertices[5].Position - tile.Vertices[10].Position;
			        yOffset = tile.Vertices[2].Position - tile.Vertices[7].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.Existing,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.Ignored,
						        Roles.NewAlt,
						        Roles.NewAlt,
					        },
				        }
			        };
			        offsetAlternateRows = false;
			        alternateRows = true;
			        break;
		        case GridEnums.GridTypes.TrihexSquare:
			        tile = Shapes.Polygon(6);
			        tile = tile.Rotate(Vector3.up, 0);
			        tile.ExtendFace(0, 5, 3);
			        tile.ExtendFace(0, 0, 3);
			        tile.ExtendFace(0, 1, 4);
			        tile.ExtendFace(2, 0, 4);
			        xOffset = tile.Vertices[2].Position - tile.Vertices[5].Position;
			        yOffset = tile.Vertices[9].Position - tile.Vertices[3].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.NewAlt,
						        Roles.NewAlt,
						        Roles.Existing,
						        Roles.ExistingAlt,
					        }
				        }
			        };
			        break;

		        case GridEnums.GridTypes.TetrakisSquare:
			        tile = Shapes.Polygon(4);
			        tile = tile.Rotate(Vector3.up, 45);
			        tile = tile.Kis(new OpParams());
			        xOffset = tile.Vertices[0].Position - tile.Vertices[1].Position;
			        yOffset = tile.Vertices[1].Position - tile.Vertices[2].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.NewAlt,
						        Roles.ExistingAlt,
						        Roles.NewAlt,
					        }
				        }
			        };
			        break;
		        case GridEnums.GridTypes.CairoPentagonal:
			        var sqrt3 = Mathf.Sqrt(3);
			        float halfBaseLength = (sqrt3 - 1) / 2;
			        var points = new List<Vector3>
			        {
				        new (-halfBaseLength, 0, 0),
				        new (halfBaseLength, 0, 0),
				        new (0.5f + halfBaseLength,0, sqrt3 / 2),
				        new (0, 0, (sqrt3 / 2) + 0.5f),
				        new (-(0.5f + halfBaseLength), 0, sqrt3 / 2)
			        };
			        var oneTile = new PolyMesh(points);
			        tile = oneTile.Duplicate();
			        tile.Append(new PolyMesh(points.Select(p => new Vector3(p.x, 0, -p.z))));
			        oneTile.Transform(new Vector3(0, 0, -(halfBaseLength + (sqrt3 / 2) + 0.5f)));
			        tile.Append(oneTile.Rotate(Vector3.up, 90));
			        tile.Append(oneTile.Rotate(Vector3.up, -90));
			        xOffset = tile.Vertices[15].Position - tile.Vertices[11].Position;
			        yOffset = tile.Vertices[16].Position - tile.Vertices[8].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new()
				        {
					        new()
					        {
						        Roles.New,
						        Roles.NewAlt,
						        Roles.Existing,
						        Roles.ExistingAlt,
					        }
				        }
			        };
			        break;

		        case GridEnums.GridTypes.Rhombille:
			        // Create a rhombus by joining two triangles
			        var rhomb = Shapes.Polygon(3);
			        rhomb.Transform(Vector3.zero, new Vector3(0, 30, 0));
			        rhomb.ExtendFace(0, 0, 3);
			        var rhombusVerts = rhomb.Vertices.Select(v => v.Position).ToList();
			        rhomb = new PolyMesh(rhombusVerts);

			        // Move it so v0 is on the origin
			        rhomb.Transform(-rhomb.Vertices[0].Position);

			        // Create a tile by joining three rhombuses
			        tile = rhomb.Duplicate();
			        rhomb.Transform(Vector3.zero, new Vector3(0, 120, 0));
			        tile.Append(rhomb, forceDuplicate: true);
			        rhomb.Transform(Vector3.zero, new Vector3(0, 120, 0));
			        tile.Append(rhomb, forceDuplicate: true);

			        xOffset = tile.Vertices[2].Position - tile.Vertices[6].Position;
			        yOffset = tile.Vertices[10].Position - tile.Vertices[6].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new()
				        {
					        new()
					        {
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.New
					        }
				        }
			        };
			        offsetAlternateRows = false;
			        break;

		        case GridEnums.GridTypes.TriakisTriangular:
			        tile = Shapes.Polygon(6);
			        tile = tile.Stake(new OpParams(1f / 3));
			        tile = tile.SplitFaces(new OpParams(0));
			        xOffset = tile.Vertices[4].Position - tile.Vertices[2].Position;
			        yOffset = tile.Vertices[3].Position - tile.Vertices[1].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new()
				        {
					        new()
					        {
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.New,
					        }
				        }
			        };

			        break;

		        case GridEnums.GridTypes.DeltoidalTrihexagonal:
			        tile = Shapes.Polygon(6);
			        tile = tile.Ortho(new OpParams(0));
			        xOffset = tile.Vertices[4].Position - tile.Vertices[12].Position;
			        yOffset = tile.Vertices[4].Position - tile.Vertices[8].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new()
				        {
					        new()
					        {
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt
					        }
				        }
			        };
			        break;

		        case GridEnums.GridTypes.Kisrhombille:
			        tile = Shapes.Polygon(6);
			        tile = tile.Meta(new OpParams(0));
			        xOffset = tile.Vertices[5].Position - tile.Vertices[1].Position;
			        yOffset = tile.Vertices[3].Position - tile.Vertices[1].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new()
				        {
					        new()
					        {
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt
					        }
				        }
			        };
			        break;


		        case GridEnums.GridTypes.FloretPentagonal:
			        var floret  = Shapes.Polygon(6);
			        floret.ExtendFace(0, 4, 3);
			        var verts = floret.Vertices.Select(v => v.Position).ToList();
			        verts = new List<Vector3>
			        {
				        verts[0],
				        verts[1],
				        verts[2],
				        verts[6],
				        verts[5],
			        };
			        floret = Shapes.Polygon(5);
			        floret.SetVertexPositions(verts.ToList());
			        floret.Transform(new Vector3(0, 0, -floret.Vertices[3].Position.z / 2), scale: Vector3.one / 2);

			        tile = floret.Duplicate();
			        for (int i = 1; i < 6; i++)
			        {
				        floret.Transform(Vector3.zero, new Vector3(0, 60, 0));
				        tile.Append(floret);
			        }

			        xOffset = tile.Vertices[25].Position - tile.Vertices[5].Position;
			        yOffset = tile.Vertices[0].Position - tile.Vertices[20].Position;

			        roleSet = new List<List<List<Roles>>>
			        {
				        new()
				        {
					        new()
					        {
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt,
						        Roles.Existing,
						        Roles.ExistingAlt
					        }
				        }
			        };
			        break;

		        case GridEnums.GridTypes.PrismaticPentagonal:
			        halfBaseLength = (Mathf.Sqrt(3) - 1) / 2;
			        points = new List<Vector3>
			        {
				        new (-0.5f, 0, 0),
				        new (0.5f, 0, 0),
				        new (0.5f,0, halfBaseLength * 2),
				        new (0, 0, halfBaseLength * 3),
				        new (-0.5f, 0, halfBaseLength* 2)
			        };
			        var pentile = new PolyMesh(points);
			        tile = pentile.Duplicate();
			        tile.Append(new PolyMesh(points.Select(p => new Vector3(p.x, 0, -p.z))));
			        xOffset = tile.Vertices[0].Position - tile.Vertices[1].Position;
			        yOffset = tile.Vertices[8].Position - tile.Vertices[2].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new()
				        {
					        new()
					        {
						        Roles.NewAlt,
						        Roles.New,
					        },
					        new()
					        {
						        Roles.Existing,
						        Roles.ExistingAlt,
					        },
				        }
			        };
			        offsetAlternateRows = false;
			        break;

		        case GridEnums.GridTypes.Durer1:
			        tile = Shapes.Polygon(5);
			        tile = tile.Rotate(Vector3.up, 54);
			        tile.ExtendFace(0, 5, 5);
			        tile.AddKite(0, 3, 1, 1);
			        xOffset = tile.Vertices[1].Position - tile.Vertices[3].Position;
			        yOffset = tile.Vertices[7].Position - tile.Vertices[2].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.NewAlt,
					        }
				        }
			        };
			        break;

		        case GridEnums.GridTypes.Durer2:
			        tile = Shapes.Polygon(5);
			        tile = tile.Rotate(Vector3.up, 54);
			        tile.ExtendFace(0, 5, 5);
			        tile.AddKite(0, 3, 1, 1);
			        tile.AddRhombus(0, 2, 72);
			        xOffset = tile.Vertices[1].Position - tile.Vertices[3].Position;
			        yOffset = tile.Vertices[6].Position - tile.Vertices[2].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new List<List<Roles>>
				        {
					        new List<Roles>
					        {
						        Roles.ExistingAlt,
						        Roles.New,
						        Roles.NewAlt,
						        Roles.NewAlt,
					        }
				        }
			        };
			        break;

		        case GridEnums.GridTypes.DissectedRhombitrihexagonal:
			        tile = Shapes.Polygon(6);
			        tile = tile.Kis(new OpParams());

			        tile.ExtendFace(4, 1, 4);
			        tile.ExtendFace(5, 1, 4);
			        tile.ExtendFace(0, 1, 4);

			        tile.ExtendFace(6, 0, 3);
			        tile.ExtendFace(7, 0, 3);

			        xOffset = tile.Vertices[10].Position - tile.Vertices[1].Position;
			        yOffset = tile.Vertices[7].Position - tile.Vertices[1].Position;
			        roleSet = new List<List<List<Roles>>>
			        {
				        new()
				        {
					        new()
					        {
						        Roles.ExistingAlt,
						        Roles.ExistingAlt,
						        Roles.ExistingAlt,
						        Roles.ExistingAlt,
						        Roles.ExistingAlt,
						        Roles.ExistingAlt,

						        Roles.Existing,
						        Roles.Existing,
						        Roles.Existing,

						        Roles.NewAlt,
						        Roles.NewAlt,
					        }
				        }
			        };
			        break;

		        case GridEnums.GridTypes.DissectedTruncatedHexagonal1:
			        format = @"6 0 0
12 17
15 10
0 0 4 1
0 1 4 1
0 2 4 1
0 3 4 1
0 4 4 1
0 5 4 1

1 0 3 5
2 0 3 4
3 0 3 5
4 0 3 4
5 0 3 5
6 0 3 4

5 3 3 0
6 3 3 0";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.DissectedTruncatedHexagonal2:
			        format = @"6 0 1
10 17
9 14

0 0 4 4
0 1 4 5
0 2 4 4
0 3 4 5
0 4 4 4
0 5 4 5

1 0 3 1
2 0 3 1
3 0 3 1
4 0 3 1
5 0 3 1
6 0 3 1

12 2 3 0
11 2 3 0";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.HexagonalTruncatedTriangular:
			        format = @"6 30 1
11 32
27 14
0 0 4 0
0 1 4 0
0 2 4 0
0 3 4 0
0 4 4 0
0 5 4 0
1 0 3 1
2 0 3 1
3 0 3 1
4 0 3 1
5 0 3 1
6 0 3 1
1 3 4 4
2 3 4 4
12 2 6 0
6 3 4 1
7 2 6 0";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.DemiregularHexagonal:
			        format = @"12 15 1
4 26
3 21
0 8 4 0
0 10 4 0
0 12 4 0
0 14 4 0
0 9 6 4
0 11 6 4
0 13 6 4
1 3 3 1
2 3 3 5
6 3 4 0
5 3 4 0";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

// 		        case GridEnums.GridTypes.TruncatedTrihexagonal1:
// 		        	format = @"12 15 1
// 3 16
// 2 12
// 0 8 4 0
// 0 9 3 0
// 0 10 4 0
// 0 11 3 0
// 1 0 3 1
// 2 0 3 1
// 3 0 3 1
// 4 0 3 1
// 7 2 3 0
// 8 2 3 0
// 0 0 4 0
// 0 1 3 0
// 11 0 3 1
// 13 2 3 0
// 9 0 3 1";
			        // tileDef = BuildTileDefFromFormat(format);
			        // break;

		        // case GridEnums.GridTypes.DemiregularSquare:

		        case GridEnums.GridTypes.DissectedHexagonal1:
			        format = @"6 0 1
11 10
11 12
0 5 6 0
0 0 3 0
0 2 3 0
0 4 3 0
1 0 3 1
1 2 3 1
1 4 3 1
";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.DissectedHexagonal2:
			        format = @"6 0 1
1 12
1 10
0 3 3 0
0 4 3 0
0 5 3 0
0 6 3 0
1 0 3 4
2 0 3 4
3 0 3 4
5 0 3 5
6 0 3 5
7 0 3 5
";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.DissectedHexagonal3:
			        format = @"6 0 1
13 11
7 15
0 0 3 0
0 1 3 0
0 2 3 0
0 3 3 0
0 4 3 0
0 5 3 0
1 0 3 1
2 0 3 1
3 0 3 1
4 0 3 1
5 0 3 1
6 0 3 1
7 0 3 4
8 0 3 4
9 0 3 4
10 0 3 4
11 0 3 4
12 0 3 4
7 2 3 0
12 2 3 0";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.AlternatingTrihexagonal:
			        format = @"6 30 1
2 4
3 6
0 1 3 0
0 6 3 4
1 2 3 4
2 3 3 0";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        // case GridEnums.GridTypes.DissectedRhombiHexagonal:

		        case GridEnums.GridTypes.AlternatingTrihexSquare:
					format = @"6 0 0
2 5
3 8
0 6 3 1
0 5 3 1
0 1 4 4
1 0 4 5";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

// 				case GridEnums.GridTypes.TrihexSquare:
// 					format = @"6 0 0
// 2 5
// 3 9
// 0 6 3 1
// 0 5 3 1
// 0 1 4 4
// 1 0 4 5";
// 					tileDef = BuildTileDefFromFormat(format);
// 					break;

		        case GridEnums.GridTypes.AlternatingTriSquare:
			        format = @"4 45 1
15 14
19 8
0 1 4 0
0 2 3 0
0 0 3 0
1 0 3 1
1 2 3 1
1 3 3 1
0 3 3 0
4 0 3 5
2 0 4 5
3 0 3 4
10 2 4 5
11 3 4 4
11 0 3 1
12 0 3 1
11 2 3 0
12 2 3 0
4 2 4 5";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.SemiSnubTriSquare:
			        format = @"4 45 1
3 6
2 15
0 1 4 0
0 2 3 0
0 0 3 0
1 0 3 1
1 2 3 1
1 3 3 1
6 0 4 4
7 2 4 5
8 0 3 4
6 2 3 5
10 0 3 0
";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.TriSquareSquare1:
			        format = @"4 45 1
4 5
6 5
0 2 4 0
0 0 3 5
1 3 3 4
";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.TriSquareSquare2:
			        format = @"4 45 1
4 5
7 8
0 2 4 0
0 0 4 0
1 3 3 5
2 3 3 4
";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.TriTriSquare1:
			        format = @"4 45 0
4 6
4 5
0 2 3 1
0 0 3 2
1 2 3 2
2 0 3 1
";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        case GridEnums.GridTypes.TriTriSquare2:
			        format = @"4 45 0
5 7
5 9
0 2 3 1
0 0 3 2
1 2 3 2
2 0 3 1
3 0 3 1
5 0 3 2";
			        tileDef = BuildTileDefFromFormat(format);
			        break;

		        default:
			        xOffset = yOffset = Vector3.zero;
			        tile = new PolyMesh();
			        break;
	        }

	        if (string.IsNullOrEmpty(format))
	        {
		        // Not created via a format string
		        // Create a tiledef based on values set above
		        tileDef = new TileDef
		        {
			        tile = tile,
			        gridShape = gridShape,
			        xOffset = xOffset,
			        yOffset = yOffset,
			        xRepeats = xRepeats,
			        yRepeats = yRepeats,
			        roleSet = roleSet,
			        offsetAlternateRows = offsetAlternateRows,
			        alternateRows = alternateRows
		        };
	        }
	        else
	        {
		        // Created via a format string
		        // Tiledef has been generated and offsets and roles have already been set
		        // Just fill in the params that are passed to this method
		        tileDef.gridShape = gridShape;
		        tileDef.xRepeats = xRepeats;
		        tileDef.yRepeats = yRepeats;
	        }

	        return BuildGridFromTileDef(tileDef);
        }

        public static PolyMesh ShapeWrap(PolyMesh grid, GridEnums.GridShapes gridShape, float heightScale, float maxHeight)
		{

			// Cylinder
			for (var i = 0; i < grid.Vertices.Count; i++)
			{
				var vert = grid.Vertices[i];
				var newPos = vert.Position;
				newPos = new Vector3(
					Mathf.Cos(newPos.x * Mathf.PI * 2) / 2f,
					newPos.z * heightScale,
					Mathf.Sin(newPos.x * Mathf.PI * 2) / 2f
				);
				vert.Position = newPos;
			}
			// Weld cylinder edges.
			// Other shapes might not work with welding tips etc
			grid = grid.Weld(0.01f);

			// Change cylinder profile for cone etc
			if (gridShape != GridEnums.GridShapes.Plane)
			{
				var heightRange = (maxHeight / 2f);

				for (var i = 0; i < grid.Vertices.Count; i++)
				{
					var vert = grid.Vertices[i];
					var newPos = vert.Position;
					if (gridShape != GridEnums.GridShapes.Cylinder)
					{
						float pinch = 1f;
						var y = 1 - Mathf.InverseLerp(0, heightRange, newPos.y);
						switch (gridShape)
						{
							case GridEnums.GridShapes.Polar:
							case GridEnums.GridShapes.Cone:
								pinch = 1 - y;
								break;
							case GridEnums.GridShapes.Sphere:
								pinch = Mathf.Sqrt(1f - Mathf.Pow(-1 + 2 * y, 2));
								y -= 0.5f;
								break;
						}

						newPos = new Vector3(
							newPos.x * pinch,
							y,
							newPos.z * pinch
						);
					}

					if (gridShape == GridEnums.GridShapes.Polar)
					{
						newPos.y = 0;
					}
					vert.Position = newPos;
				}
			}

            return grid;
		}
    }



}