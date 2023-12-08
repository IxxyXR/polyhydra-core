/*
 * Tactile-JS
 * Copyright 2018 Craig S. Kaplan, csk@uwaterloo.ca
 *
 * Distributed under the terms of the 3-clause BSD license.  See the
 * file "LICENSE" for more information.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core.IsohedralGrids
{
    public class IsohedralPoly
    {

        public int TilingType { get; private set; }
        public string TilingName { get; private set; }
        public int NumParams { get; private set; }
        private List<List<Vector2>> shapes;
        private IsohedralTiling tiling;

        public IsohedralPoly(int tilingType)
        {
            TilingType = tilingType;
            tiling = new IsohedralTiling(TilingType);
            TilingName = tiling.tilingName();
        }

        public PolyMesh MakePoly(
            List<double> tilingParameters,
            List<Vector2> NewVerts,
            Vector2 Size
        )
        {

            var verts = new List<Vector3>();
            var faces = new List<List<int>>();
            var faceRoles = new List<Roles>();

            List<double> ps = tiling.getParameters();
            if (tiling.numParameters() > 0)
            {
                for (int i = 0; i < ps.Count; i++)
                {
                    ps[i] = tilingParameters[i];
                }
            }

            tiling.setParameters(ps);

            // Make some edge shapes.  Note that here, we sidestep the
            // potential complexity of using .shape() vs. .parts() by checking
            // ahead of time what the intrinsic edge shape is and building
            // Bezier control points that have all necessary symmetries.

            var edges = new List<List<Vector2>>();


            var currentVert = 0;
            for (int i = 0; i < tiling.numEdgeShapes(); ++i)
            {
                var ej = new List<Vector2>();
                EdgeShape shp = tiling.getEdgeShape(i);

                if (shp == EdgeShape.I || currentVert >= NewVerts.Count)
                {
                    // I Edges must look the same after both rotation and reflection
                }
                else
                {
                    var newVert = new Vector2(NewVerts[currentVert].x + 0.5f, NewVerts[currentVert].y);
                    currentVert++;
                    if (shp == EdgeShape.J)
                    {
                        // J Edges can be a path of any shape
                        if (NewVerts.Count > 0)
                        {
                            ej.Add(newVert);
                        }
                    }
                    else if (shp == EdgeShape.S)
                    {
                        // S Edges must look the same after a 180° rotation
                        if (NewVerts.Count > 0)
                        {
                            ej.Add(newVert);
                            ej.Add(new Vector2(1.0f - ej[0].x, -ej[0].y));
                        }
                    }
                    else if (shp == EdgeShape.U)
                    {
                        // U Edges must look the same after reflecting across their length
                        if (NewVerts.Count > 0)
                        {
                            ej.Add(newVert);
                            ej.Add(new Vector2(1.0f - ej[0].x, ej[0].y));
                        }
                    }
                }

                edges.Add(ej);
            }

            IEnumerable<IEnumerable<IEnumerable<IsohedralTiling.Tile>>> tiles = tiling.fillRegionBounds(
                -Size.x / 2f,
                -Size.y / 2f,
                Size.x / 2f,
                Size.y / 2f
            );

            NumParams = tiling.numParameters();
            TilingName = tiling.tilingName();

            Roles[] availableRoles =
            {
                Roles.New,
                Roles.NewAlt,
                Roles.Ignored,
                Roles.Existing,
                Roles.ExistingAlt,
            };

            foreach (IEnumerable<IEnumerable<IsohedralTiling.Tile>> tileGroupGroup in tiles)
            {
                foreach (IEnumerable<IsohedralTiling.Tile> tileGroup in tileGroupGroup)
                {
                    foreach (IsohedralTiling.Tile tile in tileGroup)
                    {
                        var T = tile.T;
                        var colour = tiling.getColour(Mathf.FloorToInt(tile.t1), Mathf.FloorToInt(tile.t2),
                            tile.aspect);
                        var role = availableRoles[colour];
                        var face = new List<int>();
                        int initialVert = verts.Count;
                        var shape = tiling.shape().ToList();
                        bool start = true;
                        for (var jj = 0; jj < shape.Count; jj++)
                        {
                            IsohedralTiling.Tile si = shape[jj];
                            float[] S = IsohedralTiling.Multiply(T, si.T);
                            List<Vector2> seg = new[] {IsohedralTiling.Multiply(S, new Vector2(0, 0))}.ToList();
                            if (si.shape != EdgeShape.I)
                            {
                                var ej = edges[si.id];
                                for (var j = 0; j < ej.Count; j++)
                                {
                                    seg.Add(IsohedralTiling.Multiply(S, ej[j]));
                                }
                            }

                            seg.Add(IsohedralTiling.Multiply(S, new Vector2(1.0f, 0.0f)));

                            if (si.rev)
                            {
                                seg.Reverse();
                            }

                            if (start)
                            {
                                start = false;
                                verts.Add(new Vector3(seg[0].x, 0, seg[0].y));
                                face.Add(verts.Count - 1);
                            }

                            for (var s = 1; s < seg.Count; s++)
                            {
                                var currentSeg = seg[s];
                                if (verts.Last() !=
                                    new Vector3(currentSeg.x, 0, currentSeg.y)) // Don't add duplicate verts

                                {
                                    verts.Add(new Vector3(currentSeg.x, 0, currentSeg.y));
                                    face.Add(verts.Count - 1);
                                }
                            }
                        }

                        // Hacky way to fix normals
                        int normalSkip = face.Count / 3;
                        var normal = Vector3.Cross(verts[face[normalSkip]] - verts[face[0]],
                            verts[face[normalSkip * 2]] - verts[face[0]]);
                        if (normal.y < 0)
                        {
                            face.Reverse();
                        }

                        face = face.Take(face.Count - 1).ToList();
                        faces.Add(face);
                        faceRoles.Add(role);
                    }
                }
            }

            var vertexRoles = Enumerable.Repeat(Roles.Existing, verts.Count).ToList();

            return new PolyMesh(verts, faces, faceRoles, vertexRoles);

        }



        public List<double> GetDefaultTilingParameters()
        {
            var defaultParameters = new List<double>();
            List<double> ps = tiling.getParameters();

            for (int i = 0; i < tiling.numParameters(); i++)
            {
                if (i < tiling.numParameters())
                {
                    defaultParameters.Add(ps[i]);
                }
                else
                {
                    defaultParameters.Add(0);
                }
            }

            return defaultParameters;
        }
    }
}