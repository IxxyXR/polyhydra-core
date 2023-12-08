/*
 * Tactile-JS
 * Copyright 2018 Craig S. Kaplan, csk@uwaterloo.ca
 *
 * Distributed under the terms of the 3-clause BSD license.  See the
 * file "LICENSE" for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core.IsohedralGrids
{
    public class IsohedralTiling
    {

        private TilingTypeConfig ttd;
        private List<double> parameters;
        public List<Vector2> verts;
        private List<float[]> edges { get; set; }
        private List<bool> reversals { get; set; }


        int numTypes = 81;

        public static readonly int[] tilingTypes = new int[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 20,
            21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
            31, 32, 33, 34, 36, 37, 38, 39, 40,
            41, 42, 43, 44, 45, 46, 47, 49, 50,
            51, 52, 53, 54, 55, 56, 57, 58, 59,
            61, 62, 64, 66, 67, 68, 69,
            71, 72, 73, 74, 76, 77, 78, 79,
            81, 82, 83, 84, 85, 86, 88,
            90, 91, 93
        };

        private float[][] M_orients =
        {
            new[] {1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f}, // IDENTITY
            new[] {-1.0f, 0.0f, 1.0f, 0.0f, -1.0f, 0.0f}, // ROT
            new[] {-1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f}, // FLIP
            new[] {1.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f} // ROFL
        };

        private float[][] TSPI_U =
        {
            new[] {0.5f, 0.0f, 0.0f, 0.0f, 0.5f, 0.0f},
            new[] {-0.5f, 0.0f, 1.0f, 0.0f, 0.5f, 0.0f}
        };

        private float[][] TSPI_S =
        {
            new[] {0.5f, 0.0f, 0.0f, 0.0f, 0.5f, 0.0f},
            new[] {-0.5f, 0.0f, 1.0f, 0.0f, -0.5f, 0.0f}
        };

        private List<double[]> aspects;
        private Vector2 t1;
        private Vector2 t2;
        private int tiling_type;

        private Vector2 makePoint(double[] coeffs, int offs, double[] par)
        {
            var ret = Vector2.zero;

            for (int i = 0; i < par.Length; ++i)
            {
                ret.x += (float) (coeffs[offs + i] * par[i]);
                ret.y += (float) (coeffs[offs + par.Length + i] * par[i]);
            }

            return ret;
        }

        private double[] makeMatrix(double[] coeffs, int offs, double[] par)
        {
            var ret = new double[6];

            int counter = 0;

            for (int row = 0; row < 2; ++row)
            {
                for (int col = 0; col < 3; ++col)
                {
                    double val = 0.0;
                    for (int idx = 0; idx < par.Length; ++idx)
                    {
                        val += coeffs[offs + idx] * par[idx];
                    }

                    ret[counter] = val;
                    counter++;
                    offs += par.Length;
                }
            }

            return ret;
        }

        public static Vector2 Multiply(float[] A, Vector2 B)
        {
            // Matrix * Point
            return new Vector2(
                x: A[0] * B.x + A[1] * B.y + A[2],
                y: A[3] * B.x + A[4] * B.y + A[5]
            );
        }

        public static float[] Multiply(float[] A, float[] B)
        {
            // Matrix * Matrix
            return new float[]
            {
                A[0] * B[0] + A[1] * B[3],
                A[0] * B[1] + A[1] * B[4],
                A[0] * B[2] + A[1] * B[5] + A[2],

                A[3] * B[0] + A[4] * B[3],
                A[3] * B[1] + A[4] * B[4],
                A[3] * B[2] + A[4] * B[5] + A[5]
            };
        }

        public float[] matchSeg(Vector2 p, Vector2 q)
        {
            return new[]
            {
                q.x - p.x,
                p.y - q.y,
                p.x,
                q.y - p.y,
                q.x - p.x,
                p.y
            };
        }

        public IsohedralTiling(int tp)
        {
            Reset(tp);
        }

        public void Reset(int tp)
        {
            if (!tilingTypes.Contains(tp))
            {
                Debug.LogWarning("Invalid tiling type");
            }

            tiling_type = tp;
            ttd = IsohedralTilingHelpers.tiling_types[tp];
            parameters = new List<double>(ttd.default_params);
            parameters.Add(1.0);
            Recompute();
        }

        public void Recompute()
        {
            var ntv = numVertices();
            var np = numParameters();
            var na = numAspects();

            // Recompute tiling vertex locations.
            verts = new List<Vector2>();
            for (int idx = 0; idx < ntv; ++idx)
            {
                verts.Add(
                    makePoint(
                        ttd.vertex_coeffs,
                        idx * (2 * (np + 1)),
                        parameters.ToArray()
                    )
                );
            }

            // Recompute edge transforms and reversals from orientation information.
            reversals = new List<bool>();
            edges = new List<float[]>();
            for (int idx = 0; idx < ntv; ++idx)
            {
                var fl = ttd.edge_orientations[2 * idx];
                var ro = ttd.edge_orientations[2 * idx + 1];
                reversals.Add(fl != ro);
                float[] orientation = new float[] { };
                if (ro == false && fl == false)
                {
                    orientation = M_orients[0];
                }
                else if (ro == true && fl == false)
                {
                    orientation = M_orients[1];
                }
                else if (ro == false && fl == true)
                {
                    orientation = M_orients[2];
                }
                else if (ro == true && fl == true)
                {
                    orientation = M_orients[3];
                }

                edges.Add(
                    Multiply(
                        matchSeg(verts[idx], verts[(idx + 1) % ntv]),
                        orientation
                    )
                );
            }

            // Recompute aspect xforms.
            var param = parameters.ToArray();
            aspects = new List<double[]>();
            for (int idx = 0; idx < na; ++idx)
            {
                aspects.Add(
                    makeMatrix(ttd.aspect_coeffs, 6 * (np + 1) * idx, param)
                );
            }

            // Recompute translation vectors.
            t1 = makePoint(ttd.translation_coeffs, 0, param);
            t2 = makePoint(ttd.translation_coeffs, 2 * (np + 1), param);
        }

        public int numParameters()
        {
            return ttd.num_params;
        }

        public string tilingName()
        {
            return ttd.tiling_name;
        }

        public void setParameters(List<double> arr)
        {
            if (arr.Count == parameters.Count - 1)
            {
                parameters = new List<double>(arr);
                parameters.Add(1.0);
                Recompute();
            }
        }

        public List<double> getParameters()
        {
            return parameters.Take(parameters.Count - 1).ToList();
        }

        public int numEdgeShapes()
        {
            return ttd.num_edge_shapes;
        }

        public EdgeShape getEdgeShape(int idx)
        {
            return ttd.edge_shapes[idx];
        }

        public struct Tile
        {
            public float[] T;
            public int id;
            public EdgeShape shape;
            public bool rev;
            public bool second;
            public float t1;
            public float t2;
            public int aspect;

            public Tile(
                float[] T,
                int id = 0,
                EdgeShape shape = EdgeShape.J,
                bool rev = false,
                bool second = false,
                float t1 = 0,
                float t2 = 0,
                int aspect = 0)
            {
                this.T = T;
                this.t1 = t1;
                this.t2 = t2;
                this.id = id;
                this.shape = shape;
                this.rev = rev;
                this.second = second;
                this.aspect = aspect;
            }
        }

        public IEnumerable<Tile> shape()
        {

            for (int idx = 0; idx < numVertices(); ++idx)
            {
                var an_id = ttd.edge_shape_ids[idx];

                yield return new Tile(
                    T: edges[idx],
                    id: an_id,
                    shape: ttd.edge_shapes[an_id],
                    rev: reversals[idx]
                );
            }
        }

        public IEnumerable<Tile> parts()
        {
            for (int idx = 0; idx < numVertices(); ++idx)
            {
                var an_id = ttd.edge_shape_ids[idx];
                var shp = ttd.edge_shapes[an_id];

                if (shp == EdgeShape.J || shp == EdgeShape.I)
                {
                    yield return new Tile(
                        T: edges[idx],
                        id: an_id,
                        shape: shp,
                        rev: reversals[idx],
                        second: false
                    );
                }
                else
                {
                    (int, int) indices = reversals[idx] ? (1, 0) : (0, 1);
                    var Ms = (shp == EdgeShape.U) ? TSPI_U : TSPI_S;

                    yield return new Tile(
                        T: Multiply(edges[idx], Ms[indices.Item1]),
                        id: an_id,
                        shape: shp,
                        rev: false,
                        second: false
                    );

                    yield return new Tile(
                        T: Multiply(edges[idx], Ms[indices.Item2]),
                        id: an_id,
                        shape: shp,
                        rev: true,
                        second: true
                    );
                }
            }
        }

        public int numVertices()
        {
            return ttd.num_vertices;
        }

        // public Vector2 getVertex(int idx) {
        //     return {...verts[idx]};
        // }

        // public List<Vector2> vertices() {
        //     return verts.Select(v => ({...v}));
        // }

        public int numAspects()
        {
            return ttd.num_aspects;
        }

        // public void getAspectTransform(idx) {
        //     return [...aspects[idx]];
        // }

        public IEnumerable<IEnumerable<IEnumerable<Tile>>> fillRegionBounds(float xmin, float ymin, float xmax,
            float ymax)
        {
            return fillRegionQuad(
                new Vector2(xmin, ymin),
                new Vector2(xmax, ymin),
                new Vector2(xmax, ymax),
                new Vector2(xmin, ymax)
            );
        }

        public IEnumerable<IEnumerable<IEnumerable<Tile>>> fillRegionQuad(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
        {

            Vector2 bc(double[] M, Vector2 p)
            {
                return new Vector2(
                    (float) (M[0] * p.x + M[1] * p.y),
                    (float) (M[2] * p.x + M[3] * p.y)
                );
            }

            ;

            Vector2 sampleAtHeight(Vector2 P, Vector2 Q, float y)
            {
                float t = (y - P.y) / (Q.y - P.y);
                return new Vector2((1.0f - t) * P.x + t * Q.x, y);
            }

            IEnumerable<Tile> doFill(Vector2 AA, Vector2 BB, Vector2 CC, Vector2 DD, bool do_top)
            {
                float x1 = AA.x;
                float dx1 = (DD.x - AA.x) / (DD.y - AA.y);
                float x2 = BB.x;
                float dx2 = (CC.x - BB.x) / (CC.y - BB.y);
                float ymin = AA.y;
                float ymax = CC.y;

                if (do_top)
                {
                    ymax += 1.0f;
                }

                double y = Math.Floor(ymin);
                while (y < ymax)
                {
                    float yi = Mathf.Floor((float) y);
                    var xx = Math.Floor(x1);
                    while (xx < x2 + 1e-7)
                    {

                        float xi = Mathf.Floor((float) xx);

                        for (int asp = 0; asp < ttd.num_aspects; ++asp)
                        {
                            double[] M = new double[aspects[asp].Length];
                            aspects[asp].CopyTo(M, 0);
                            M[2] += xi * t1.x + yi * t2.x;
                            M[5] += xi * t1.y + yi * t2.y;

                            yield return new Tile(
                                T: M.Select(x => (float) x).ToArray(),
                                t1: xi,
                                t2: yi,
                                aspect: asp
                            );
                        }

                        xx += 1.0;
                    }

                    x1 += dx1;
                    x2 += dx2;
                    y += 1.0;
                }
            }

            IEnumerable<IEnumerable<Tile>> fillFixX(Vector2 AA, Vector2 BB, Vector2 CC, Vector2 DD, bool do_top)
            {
                if (AA.x > BB.x)
                {
                    yield return doFill(BB, AA, DD, CC, do_top);
                }
                else
                {
                    yield return doFill(AA, BB, CC, DD, do_top);
                }
            }

            IEnumerable<IEnumerable<Tile>> fillFixY(Vector2 AA, Vector2 BB, Vector2 CC, Vector2 DD, bool do_top)
            {
                if (AA.y > CC.y)
                {
                    yield return doFill(CC, DD, AA, BB, do_top);
                }
                else
                {
                    yield return doFill(AA, BB, CC, DD, do_top);
                }
            }

            var det = 1.0 / (t1.x * t2.y - t2.x * t1.y);
            var Mbc = new[] {t2.y * det, -t2.x * det, -t1.y * det, t1.x * det};
            var pts = new[] {bc(Mbc, A), bc(Mbc, B), bc(Mbc, C), bc(Mbc, D)};

            if (det < 0.0)
            {
                (pts[1], pts[3]) = (pts[3], pts[1]);
            }

            if (Math.Abs(pts[0].y - pts[1].y) < 1e-7)
            {
                yield return fillFixY(pts[0], pts[1], pts[2], pts[3], true);
            }
            else if (Math.Abs(pts[1].y - pts[2].y) < 1e-7)
            {
                yield return fillFixY(pts[1], pts[2], pts[3], pts[0], true);
            }
            else
            {
                int lowest = 0;
                for (int idx = 1; idx < 4; ++idx)
                {
                    if (pts[idx].y < pts[lowest].y)
                    {
                        lowest = idx;
                    }
                }

                var bottom = pts[lowest];
                var left = pts[(lowest + 1) % 4];
                var top = pts[(lowest + 2) % 4];
                var right = pts[(lowest + 3) % 4];

                if (left.x > right.x)
                {
                    (left, right) = (right, left);
                }

                if (left.y < right.y)
                {
                    var r1 = sampleAtHeight(bottom, right, left.y);
                    var l2 = sampleAtHeight(left, top, right.y);
                    yield return fillFixX(bottom, bottom, r1, left, false);
                    yield return fillFixX(left, r1, right, l2, false);
                    yield return fillFixX(l2, right, top, top, true);
                }
                else
                {
                    var l1 = sampleAtHeight(bottom, left, right.y);
                    var r2 = sampleAtHeight(right, top, left.y);
                    yield return fillFixX(bottom, bottom, right, l1, false);
                    yield return fillFixX(l1, right, r2, left, false);
                    yield return fillFixX(left, r2, top, top, true);
                }
            }
        }

        public int getColour(int a, int b, int asp)
        {
            var clrg = ttd.colouring;
            var nc = clrg[18];

            var mt1 = a % nc;
            if (mt1 < 0)
            {
                mt1 += nc;
            }

            var mt2 = b % nc;
            if (mt2 < 0)
            {
                mt2 += nc;
            }

            var col = clrg[asp];

            for (int idx = 0; idx < mt1; ++idx)
            {
                col = clrg[12 + col];
            }

            for (int idx = 0; idx < mt2; ++idx)
            {
                col = clrg[15 + col];
            }

            return col;
        }
    }
}