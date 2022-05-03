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
			K_3_3_3_3_3_3,
			K_4_4_4_4,
			K_6_6_6,
			K_3_3_3_3_6,
			K_3_3_3_4_4,
			K_3_3_4_3_4,
			K_3_4_6_4,
			K_3_6_3_6,
			K_3_12_12,
			K_4_6_12,
			K_4_8_8,
			K_3_3_4_12__3_3_3_3_3_3,
			K_3_3_6_6__3_6_3_6,
			K_3_4_3_12__3_12_12,
			K_3_4_4_6__3_6_3_6,
			Durer1,
			Durer2
		}
		
	}

	public static class Grids
    {
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
        
		public static PolyMesh Build(GridEnums.GridTypes type, GridEnums.GridShapes gridShape, int xRepeats, int yRepeats)
		{
            Vector3 xOffset, yOffset;
			PolyMesh tile, poly;
			bool offsetAlternateRows = true;
			bool alternateRows = false;

			List<List<List<Roles>>> roleSet = null;
			
			switch (type)
			{
				case GridEnums.GridTypes.K_3_3_3_3_3_3:
					tile = Shapes.Polygon(3);
					tile = tile.Rotate(Vector3.up, 30);
					tile.ExtendFace(0, 0, 3);
					xOffset = tile.Vertices[0].Position - tile.Vertices[2].Position;
					yOffset = tile.Vertices[3].Position - tile.Vertices[0].Position;
                    roleSet = new List<List<List<Roles>>>
                    {new List<List<Roles>>
                        {new List<Roles> { Roles.Existing, Roles.New}}
                    };
					break;
				case GridEnums.GridTypes.K_4_4_4_4:
					tile = Shapes.Polygon(4);
					tile = tile.Rotate(Vector3.up, 45);
					xOffset = tile.Vertices[0].Position - tile.Vertices[3].Position;
					yOffset = tile.Vertices[0].Position - tile.Vertices[1].Position;
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> { Roles.Existing, Roles.New},
						new List<Roles> { Roles.New, Roles.Existing},
					}};
					break;
				case GridEnums.GridTypes.K_6_6_6:
					tile = Shapes.Polygon(6);
					tile = tile.Rotate(Vector3.up, 30);
					xOffset = tile.Vertices[1].Position - tile.Vertices[5].Position;
					yOffset = tile.Vertices[1].Position - tile.Vertices[3].Position;	
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> { Roles.Existing, Roles.NewAlt, Roles.New},
						new List<Roles> { Roles.New, Roles.Existing, Roles.NewAlt},
						new List<Roles> { Roles.NewAlt, Roles.New, Roles.Existing},
					}};
					break;
				case GridEnums.GridTypes.K_3_3_3_3_6:
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
					{new List<List<Roles>> {
						new List<Roles> {
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
						}}};
					break;
				case GridEnums.GridTypes.K_3_3_3_4_4:
					tile = Shapes.Polygon(4);
					tile = tile.Rotate(Vector3.up, -45);
					tile.ExtendFace(0, 1, 3);
					tile.ExtendFace(0, 3, 3);
					xOffset = tile.Vertices[2].Position - tile.Vertices[3].Position;
					yOffset = tile.Vertices[4].Position - tile.Vertices[2].Position;
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.Ignored,
							Roles.New,
							Roles.NewAlt,
							Roles.New,
							Roles.NewAlt,
						},
						new List<Roles> {
							Roles.Existing,
							Roles.New,
							Roles.NewAlt,
							Roles.New,
							Roles.NewAlt,
						},
					}};
					offsetAlternateRows = false;
					break;
				case GridEnums.GridTypes.K_3_3_4_3_4:
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
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.New,
							Roles.Ignored,
							Roles.NewAlt,
							Roles.NewAlt,
							Roles.Existing,
						}}};
					break;
				case GridEnums.GridTypes.K_3_4_6_4:
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
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.New,
							Roles.Ignored,
							Roles.New,
							Roles.NewAlt,
							Roles.Existing,
						}}};
					break;
				case GridEnums.GridTypes.K_3_6_3_6:
					tile = Shapes.Polygon(6);
					tile = tile.Rotate(Vector3.up, 30);
					tile.ExtendFace(0, 0, 3);
					tile.ExtendFace(0, 1, 3);
					xOffset = tile.Vertices[1].Position - tile.Vertices[4].Position;
					yOffset = tile.Vertices[7].Position - tile.Vertices[2].Position;	
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.Existing,
							Roles.New,
						},
						new List<Roles> {
							Roles.NewAlt,
							Roles.Existing,
							Roles.New,
						},
					}};
					offsetAlternateRows = false;
					break;
				case GridEnums.GridTypes.K_3_12_12:
					tile = Shapes.Polygon(12);
					tile = tile.Rotate(Vector3.up, 45);
					tile.ExtendFace(0, 7, 3);
					tile.ExtendFace(0, 9, 3);
					xOffset = tile.Vertices[4].Position - tile.Vertices[9].Position;
					yOffset = tile.Vertices[2].Position - tile.Vertices[7].Position;
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.New,
							Roles.NewAlt,
						}}};
					break;
				case GridEnums.GridTypes.K_4_6_12:
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
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.New,
							Roles.New,
							Roles.New,
							Roles.NewAlt,
							Roles.NewAlt,
						}}};
					break;
				case GridEnums.GridTypes.K_4_8_8:
					tile = Shapes.Polygon(8);
					tile = tile.Rotate(Vector3.up, -22.5f);
					tile.ExtendFace(0, 1, 4);
					xOffset = tile.Vertices[2].Position - tile.Vertices[8].Position;
					yOffset = tile.Vertices[9].Position - tile.Vertices[4].Position;
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.New,
							Roles.NewAlt,
						},
						new List<Roles> {
							Roles.Existing,
							Roles.New,
							Roles.NewAlt,
						},
					}};
					offsetAlternateRows = false;
					break;
				case GridEnums.GridTypes.K_3_3_4_12__3_3_3_3_3_3:
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
					{new List<List<Roles>> {
						new List<Roles> {
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
						}}};
					break;
				case GridEnums.GridTypes.K_3_3_6_6__3_6_3_6:
					tile = Shapes.Polygon(6);
					tile = tile.Rotate(Vector3.up, 0);
					tile.ExtendFace(0, 5, 3);
					tile.ExtendFace(0, 0, 3);
					xOffset = tile.Vertices[2].Position - tile.Vertices[5].Position;
					yOffset = tile.Vertices[1].Position - tile.Vertices[3].Position;
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.New,
							Roles.Existing,
							Roles.Ignored,
						},
						new List<Roles> {
							Roles.NewAlt,
							Roles.Existing,
							Roles.Ignored,
						},
					}};
					offsetAlternateRows = false;
					alternateRows = true;
					break;
				case GridEnums.GridTypes.K_3_4_3_12__3_12_12:
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
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.Existing,
							Roles.NewAlt,
							Roles.NewAlt,
							Roles.Ignored,
							Roles.NewAlt,
							Roles.NewAlt,
						},
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.NewAlt,
							Roles.NewAlt,
							Roles.Ignored,
							Roles.NewAlt,
							Roles.NewAlt,
						},
					}};
					offsetAlternateRows = false;
					alternateRows = true;
					break;
				case GridEnums.GridTypes.K_3_4_4_6__3_6_3_6:
					tile = Shapes.Polygon(6);
					tile = tile.Rotate(Vector3.up, 0);
					tile.ExtendFace(0, 5, 3);
					tile.ExtendFace(0, 0, 3);
					tile.ExtendFace(0, 1, 4);
					tile.ExtendFace(2, 0, 4);
					xOffset = tile.Vertices[2].Position - tile.Vertices[5].Position;
					yOffset = tile.Vertices[9].Position - tile.Vertices[3].Position;
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.NewAlt,
							Roles.NewAlt,
							Roles.Existing,
							Roles.ExistingAlt,
						}}};
					break;
				case GridEnums.GridTypes.Durer1:
					tile = Shapes.Polygon(5);
					tile = tile.Rotate(Vector3.up, 54);
					tile.ExtendFace(0, 5, 5);
					tile.AddKite(0, 3, 1, 1);
					xOffset = tile.Vertices[1].Position - tile.Vertices[3].Position;
					yOffset = tile.Vertices[7].Position - tile.Vertices[2].Position;
					roleSet = new List<List<List<Roles>>>
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.New,
							Roles.NewAlt,
						}}};
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
					{new List<List<Roles>> {
						new List<Roles> {
							Roles.ExistingAlt,
							Roles.New,
							Roles.NewAlt,
							Roles.NewAlt,
						}}};
					break;
                default:
					xOffset = yOffset = Vector3.zero;
					tile = new PolyMesh();
					break;
			}
			
			poly = new PolyMesh();
			var newFaceRoles = new List<Roles>();
            var xCentering = ((xOffset * (xRepeats - 1)) / 2f);
            xCentering.y = 0;
			var yCentering = ((yOffset * (yRepeats - 1)) / 2f);
            yCentering.x = 0;

			int roleCountY = roleSet.Count;
			int roleCountX = roleSet[0].Count;
			int tileCount = tile.Faces.Count;
			for (int y=0; y<yRepeats; y++)
			{
				var rowOffset = offsetAlternateRows ? y % roleCountX : 0;
				for (int x=0; x<xRepeats; x++)
                {
					var colOffset = alternateRows && y%2==0 ? 1 : 0;
                    // Vector3 tileOffset = (xOffset * x) + (yOffset * y - yCentering);
                    Vector3 tileOffset = (xOffset * x - xCentering) + (yOffset * y - yCentering);
                    if (y % 2 == 0)
                    {
                        tileOffset.x -= yOffset.x * Mathf.FloorToInt(y);
                    }
                    else
                    {
                        tileOffset.x -= yOffset.x * Mathf.FloorToInt(y+1);
                    }
                    poly.Append(tile, tileOffset);
					newFaceRoles.AddRange(roleSet[y%roleCountY][(x+colOffset)%roleCountX].GetRange(rowOffset, tileCount));
				}
			}

			poly.FaceRoles = newFaceRoles;

			float width = xRepeats * xOffset.x;
			float heightScale = (1f/width) * Mathf.PI;
			float maxHeight = poly.Vertices.Max(x => x.Position.z) * 2f * heightScale;
			
			switch (gridShape)
			{
				case GridEnums.GridShapes.Polar:
				case GridEnums.GridShapes.Cylinder:
				case GridEnums.GridShapes.Cone:
				case GridEnums.GridShapes.Sphere:
					poly.Scale(new Vector3(1f/width, 1, 1));
					poly = ShapeWrap(poly, gridShape, heightScale, maxHeight);
					break;
				case GridEnums.GridShapes.Plane:
					poly = poly.Weld(0.01f);
					break;
			}
            return poly;
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