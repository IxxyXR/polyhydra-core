/*
* HE_Mesh  Frederik Vanhoutte - www.wblut.com
*
* https://github.com/wblut/HE_Mesh
* A Processing/Java library for creating and manipulating polygonal meshes.
*
* Public Domain: http://creativecommons.org/publicdomain/zero/1.0/
*/

using System.Collections.Generic;
using System.Linq;
using GK;
using Polyhydra.Core;
using UnityEngine;

	public static class WatermanPoly
	{
		public static PolyMesh Build(float R = 1.0f, int root = 2, int c = 0, bool mergeFaces=false)
		{
			Vector3[] centers = 
			{
				new Vector3(0, 0, 0),
				new Vector3(0.5f, 0.5f, 0.0f),
				new Vector3(1.0f / 3.0f, 1.0f / 3.0f, 2.0f / 3.0f),
				new Vector3(1.0f / 3.0f, 1.0f / 3.0f, 1.0f / 3.0f),
				new Vector3(0.5f, 0.5f, 0.5f),
				new Vector3(0.0f, 0.0f, 0.5f),
				new Vector3(1.0f, 0.0f, 0.0f)
			};
			
			if (root < 1)
			{
				return new PolyMesh();
			}

			if (R == 0)
			{
				return new PolyMesh();
			}

			Vector3 center = centers[c];
			float radius2;
			float radius;
			switch (c)
			{
				case 0:
					radius2 = 2 * root;
					radius = Mathf.Sqrt(radius2);
					break;
				case 1:
					radius = 2 + 4 * root;
					radius = 0.5f * Mathf.Sqrt(radius);
					break;
				case 2:
					radius = 6 * (root + 1);
					radius = Mathf.Sqrt(radius) / 3.0f;
					break;
				case 3:
					radius = 3 + 6 * root;
					radius = Mathf.Sqrt(radius) / 3.0f;
					break;
				case 4:
					radius = 3 + 8 * (root - 1);
					radius = 0.5f * Mathf.Sqrt(radius);
					break;
				case 5:
					radius = 1 + 4 * root;
					radius = 0.5f * Mathf.Sqrt(radius);
					break;
				case 6:
					radius = 1 + 2 * (root - 1);
					radius = Mathf.Sqrt(radius);
					break;

				default:
					radius = 2 * root;
					radius = Mathf.Sqrt(radius);
					break;
			}

			radius2 = (radius + Mathf.Epsilon) * (radius + Mathf.Epsilon);
			float scale = R / radius;
			int IR = (int) (radius + 1);

			var points = new List<Vector3>();
			float R2x, R2y, R2;
			for (int i = -IR; i <= IR; i++)
			{
				R2x = (i - center.x) * (i - center.x);
				if (R2x > radius2)
				{
					continue;
				}

				for (int j = -IR; j <= IR; j++)
				{
					R2y = R2x + (j - center.y) * (j - center.y);
					if (R2y > radius2)
					{
						continue;
					}

					for (int k = -IR; k <= IR; k++)
					{
						if ((i + j + k) % 2 == 0)
						{
							R2 = R2y + (k - center.z) * (k - center.z);
							if (R2 <= radius2 && R2 > radius2 - 400)
							{
								var scaledVector = new Vector3(i, j, k) * scale;
								points.Add(scaledVector);
							}
						}
					}
				}
			}
			
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
			
			var hull = new ConvexHullCalculator();
            hull.GenerateHull(points, false, ref verts, ref tris, ref normals);
            var faces = tris.Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 3)
                .Select(x => x.Select(v => v.Value));
            var poly = new PolyMesh(verts, faces);
            // poly.MergeCoplanarFaces(0.01f);
            return poly;
		}
	}
