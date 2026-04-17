using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        // ===== Halfedge Operator Application =====
        //
        // Applies a Conway-style operator defined by a compact notation string such as
        // "E-E", "F-F!", "E-E,E-F", "ve0-ve0,ve1-ve1", etc.
        //
        // Each atom "A-B" means: for every face, connect every point of class A to its
        // corresponding point(s) of class B. The resulting edge set is reconstructed into
        // a valid halfedge mesh by a CCW-sort / DCEL algorithm.
        //
        // Point classes:
        //   V   — original vertices (shared)
        //   E   — edge midpoints (shared by 2 faces)
        //   F   — face centroid (face-local)
        //   ve  — midpoints between V and E, 2 per edge; ve0 = same-edge pair, ve1 = corner pair (shared)
        //   vf  — midpoints between V and F, 1 per vertex per face (face-local)
        //   fe  — midpoints between E and F, 1 per edge per face (face-local)
        //   F!  — adjacent face centroid, reached via halfedge twin (shared via cache)
        //   vf! — vf points in the adjacent face, 2 per edge (face-local new vertices)
        //   fe! — fe points in the adjacent face, 1 per edge (face-local new vertices)
        //
        // Reference: halfedge_operator_plan.md

        // -------------------------------------------------------------------------
        // Output vertex used during operator application
        // -------------------------------------------------------------------------

        private class OVertex
        {
            private static int _idCounter;
            public readonly int Id;
            public string PointClass;
            public Vector3 Position;
            public Vector3 Normal;

            public OVertex(string pointClass, Vector3 pos, Vector3 normal)
            {
                Id = ++_idCounter;
                PointClass = pointClass;
                Position = pos;
                Normal = normal;
            }
        }

        private enum OperatorAtomFamily
        {
            Vertex,
            Edge,
            Face,
            VertexEdge,
            EdgeFace,
            VertexFace,
        }

        private class OEdge
        {
            public OVertex A;
            public OVertex B;
            public string Atom;
            public OperatorAtomFamily Family;
            public string SourceEdgeKey;
        }

        private struct OperatorConnection
        {
            public OVertex A;
            public OVertex B;
            public int SourceEdgeIndex;
        }

        private class ReconstructedFaceInfo
        {
            public List<OVertex> Vertices;
            public List<string> BoundaryAtoms;
            public List<OperatorAtomFamily> BoundaryFamilies;
            public string Signature;
        }

        // -------------------------------------------------------------------------
        // Cache that maps canonical string keys to shared OVertex objects
        // -------------------------------------------------------------------------

        private class OperatorVertexCache
        {
            private readonly Dictionary<string, OVertex> _cache = new Dictionary<string, OVertex>();

            public OVertex GetOrCreate(string key, string pointClass, Vector3 pos, Vector3 normal)
            {
                if (!_cache.TryGetValue(key, out var v))
                {
                    v = new OVertex(pointClass, pos, normal);
                    _cache[key] = v;
                }
                return v;
            }
        }

        // -------------------------------------------------------------------------
        // Public entry point
        // -------------------------------------------------------------------------

        /// <summary>
        /// Applies the operator described by <paramref name="operatorNotation"/> and returns the
        /// resulting mesh. The notation is a comma-separated list of atoms, e.g. "E-E,E-F",
        /// "F-F!" (dual), or "ve0-ve0,ve1-ve1".
        /// <para><paramref name="t"/> controls midpoint placement (0.5 = exact midpoint).</para>
        /// </summary>
        public PolyMesh ApplyHalfedgeOperator(string operatorNotation, float t = 0.5f)
        {
            var atoms = ParseHalfedgeOperatorString(operatorNotation);

            var classesNeeded = new HashSet<string>();
            foreach (var (a, b) in atoms) { classesNeeded.Add(a); classesNeeded.Add(b); }

            var cache    = new OperatorVertexCache();
            var edgeSeen = new HashSet<(int, int)>();
            var edges    = new List<OEdge>();

            foreach (var face in Faces)
            {
                var halfedges = face.GetHalfedges();
                int n = halfedges.Count;
                var sourceEdgeKeys = new string[n];
                for (int i = 0; i < n; i++)
                    sourceEdgeKeys[i] = GetSourceEdgeKey(halfedges[i]);

                var ptsSingle = new Dictionary<string, OVertex>();
                var ptsArray  = new Dictionary<string, OVertex[]>();

                BuildFacePoints(face, halfedges, n, classesNeeded, cache, t, ptsSingle, ptsArray);

                foreach (var (classA, classB) in atoms)
                    AddAtomConnections(classA, classB, sourceEdgeKeys, ptsSingle, ptsArray, n, edges, edgeSeen);
            }

            return BuildMeshFromEdges(edges);
        }

        // -------------------------------------------------------------------------
        // Parsing
        // -------------------------------------------------------------------------

        private static List<(string, string)> ParseHalfedgeOperatorString(string op)
        {
            return op.Split(',').Select(atom =>
            {
                int dash = atom.IndexOf('-');
                if (dash < 0)
                    throw new ArgumentException($"Invalid atom '{atom.Trim()}' in operator '{op}': missing '-'");
                var a = NormalizeClass(atom.Substring(0, dash).Trim());
                var b = NormalizeClass(atom.Substring(dash + 1).Trim());
                return (a, b);
            }).ToList();
        }

        // Normalise class names: move a leading '!' to a trailing '!'.
        // Allows "F-!F" and "F-F!" to mean the same thing.
        private static string NormalizeClass(string c)
        {
            return c.StartsWith("!") ? c.Substring(1) + "!" : c;
        }

        // -------------------------------------------------------------------------
        // Per-face point creation
        //
        // Indexing convention (matching the operator plan):
        //   V[i]      = halfedges[i].Prev.Vertex          (origin of halfedge i)
        //   E[i]      = midpoint of halfedges[i]           (edge from V[i] to V[(i+1)%n])
        //   ve[2i]    = lerp(V[i],       E[i], t)          (near V[i])
        //   ve[2i+1]  = lerp(V[(i+1)%n], E[i], t)          (near V[(i+1)%n])
        //   vf[i]     = lerp(V[i],   F, t)
        //   fe[i]     = lerp(F,      E[i], t)
        //   F![i]     = centroid of face across edge i
        //   fe![i]    = lerp(F![i],  E[i], t)
        //   vf![2i]   = lerp(V[i],       F![i], t)
        //   vf![2i+1] = lerp(V[(i+1)%n], F![i], t)
        // -------------------------------------------------------------------------

        private void BuildFacePoints(
            Face face, List<Halfedge> halfedges, int n,
            HashSet<string> classesNeeded,
            OperatorVertexCache cache, float t,
            Dictionary<string, OVertex> ptsSingle,
            Dictionary<string, OVertex[]> ptsArray)
        {
            var fn = face.Normal;

            bool needV     = classesNeeded.Contains("V");
            bool needE     = classesNeeded.Contains("E");
            bool needF     = classesNeeded.Contains("F");
            bool needVe    = classesNeeded.Contains("ve") || classesNeeded.Contains("ve0") || classesNeeded.Contains("ve1");
            bool needVf    = classesNeeded.Contains("vf");
            bool needFe    = classesNeeded.Contains("fe");
            bool needFAdj  = classesNeeded.Contains("F!");
            bool needFeAdj = classesNeeded.Contains("fe!");
            bool needVfAdj = classesNeeded.Contains("vf!");

            // --- V ---
            OVertex[] V = null;
            if (needV || needVe || needVf || needVfAdj)
            {
                V = new OVertex[n];
                for (int i = 0; i < n; i++)
                {
                    var vert = halfedges[i].Prev.Vertex;
                    V[i] = cache.GetOrCreate($"V_{vert.Name}", "V", vert.Position, vert.Normal);
                }
                ptsArray["V"] = V;
            }

            // --- E ---
            OVertex[] E = null;
            if (needE || needVe || needFe || needFeAdj)
            {
                E = ComputeEArray(face, halfedges, n, fn, cache);
                ptsArray["E"] = E;
            }

            // --- F ---
            OVertex F = null;
            if (needF || needVf || needFe)
            {
                F = cache.GetOrCreate($"F_{face.Name}", "F", face.Centroid, fn);
                ptsSingle["F"] = F;
            }

            // --- ve / ve0 / ve1  (all share the same backing array) ---
            if (needVe)
            {
                if (E == null) { E = ComputeEArray(face, halfedges, n, fn, cache); ptsArray["E"] = E; }
                var ve = new OVertex[2 * n];
                for (int i = 0; i < n; i++)
                {
                    var h = halfedges[i];
                    var eNorm = E[i].Normal;
                    // Keys include the specific vertex name so that the adjacent face
                    // (which traverses this edge in the opposite direction) resolves to
                    // the same OVertex objects rather than swapping the two ve points.
                    ve[2*i]   = cache.GetOrCreate($"ve_{h.Prev.Vertex.Name}_{MakeKey(h.PairedName)}", "ve",
                        Vector3.Lerp(h.Prev.Vertex.Position, E[i].Position, t), eNorm);
                    ve[2*i+1] = cache.GetOrCreate($"ve_{h.Vertex.Name}_{MakeKey(h.PairedName)}", "ve",
                        Vector3.Lerp(h.Vertex.Position, E[i].Position, t), eNorm);
                }
                ptsArray["ve"]   = ve;
                ptsArray["ve0"] = ve;
                ptsArray["ve1"] = ve;
            }

            // --- vf ---
            // Keyed by (face, vertex) so that vf![2i]/vf![2i+1] in adjacent faces resolve
            // to the same OVertex objects as the corresponding vf[k] computed from that face.
            if (needVf)
            {
                if (F == null) { F = cache.GetOrCreate($"F_{face.Name}", "F", face.Centroid, fn); ptsSingle["F"] = F; }
                var vf = new OVertex[n];
                for (int i = 0; i < n; i++)
                {
                    var vert = halfedges[i].Prev.Vertex;
                    vf[i] = cache.GetOrCreate(
                        $"vf_{face.Name}_{vert.Name}",
                        "vf",
                        Vector3.Lerp(vert.Position, F.Position, t),
                        Vector3.Lerp(vert.Normal, fn, t).normalized);
                }
                ptsArray["vf"] = vf;
            }

            // --- fe ---
            // Keyed by (face, edge) so that fe![i] in adjacent faces resolve to the same
            // OVertex objects as the corresponding fe[j] computed from that adjacent face.
            if (needFe)
            {
                if (F == null) { F = cache.GetOrCreate($"F_{face.Name}", "F", face.Centroid, fn); ptsSingle["F"] = F; }
                if (E == null) { E = ComputeEArray(face, halfedges, n, fn, cache); ptsArray["E"] = E; }
                var fe = new OVertex[n];
                for (int i = 0; i < n; i++)
                    fe[i] = cache.GetOrCreate(
                        $"fe_{face.Name}_{MakeKey(halfedges[i].PairedName)}",
                        "fe",
                        Vector3.Lerp(F.Position, E[i].Position, t),
                        Vector3.Lerp(E[i].Normal, fn, t).normalized);
                ptsArray["fe"] = fe;
            }

            // --- F! ---
            OVertex[] FAdjacent = null;
            if (needFAdj || needVfAdj || needFeAdj)
            {
                FAdjacent = new OVertex[n];
                for (int i = 0; i < n; i++)
                {
                    var pair = halfedges[i].Pair;
                    if (pair == null)
                    {
                        FAdjacent[i] = cache.GetOrCreate($"F_{face.Name}", "F!", face.Centroid, fn);
                    }
                    else
                    {
                        var adj = pair.Face;
                        FAdjacent[i] = cache.GetOrCreate($"F_{adj.Name}", "F!", adj.Centroid, adj.Normal);
                    }
                }
                ptsArray["F!"] = FAdjacent;
            }

            // --- fe! ---
            // fe![i] = the fe point in the adjacent face on the shared edge i.
            // Uses the same cache key as the adjacent face's fe[j] for that edge,
            // so they resolve to the same OVertex regardless of processing order.
            if (needFeAdj)
            {
                if (E == null) { E = ComputeEArray(face, halfedges, n, fn, cache); ptsArray["E"] = E; }
                var feAdj = new OVertex[n];
                for (int i = 0; i < n; i++)
                {
                    var adjFaceName = halfedges[i].Pair?.Face.Name ?? face.Name;
                    feAdj[i] = cache.GetOrCreate(
                        $"fe_{adjFaceName}_{MakeKey(halfedges[i].PairedName)}",
                        "fe!",
                        Vector3.Lerp(FAdjacent[i].Position, E[i].Position, t),
                        Vector3.Lerp(E[i].Normal, FAdjacent[i].Normal, t).normalized);
                }
                ptsArray["fe!"] = feAdj;
            }

            // --- vf! ---
            // vf![2i]   = vf point near V[i]       in the adjacent face.
            // vf![2i+1] = vf point near V[(i+1)%n] in the adjacent face.
            // Uses the same cache key as the adjacent face's vf[k] for those vertices.
            if (needVfAdj)
            {
                var vfAdj = new OVertex[2 * n];
                for (int i = 0; i < n; i++)
                {
                    var adjFaceName = halfedges[i].Pair?.Face.Name ?? face.Name;
                    var vOrigin = halfedges[i].Prev.Vertex;
                    var vDest   = halfedges[i].Vertex;
                    vfAdj[2*i]   = cache.GetOrCreate(
                        $"vf_{adjFaceName}_{vOrigin.Name}",
                        "vf!",
                        Vector3.Lerp(vOrigin.Position, FAdjacent[i].Position, t),
                        Vector3.Lerp(vOrigin.Normal,   FAdjacent[i].Normal,   t).normalized);
                    vfAdj[2*i+1] = cache.GetOrCreate(
                        $"vf_{adjFaceName}_{vDest.Name}",
                        "vf!",
                        Vector3.Lerp(vDest.Position, FAdjacent[i].Position, t),
                        Vector3.Lerp(vDest.Normal,   FAdjacent[i].Normal,   t).normalized);
                }
                ptsArray["vf!"] = vfAdj;
            }
        }

        // Shared helper: build E[] for a face via cache
        private OVertex[] ComputeEArray(Face face, List<Halfedge> halfedges, int n, Vector3 fn, OperatorVertexCache cache)
        {
            var E = new OVertex[n];
            for (int i = 0; i < n; i++)
            {
                var h = halfedges[i];
                var pairNormal = h.Pair?.Face.Normal ?? fn;
                var eNormal = ((fn + pairNormal) * 0.5f).normalized;
                E[i] = cache.GetOrCreate($"E_{MakeKey(h.PairedName)}", "E", h.Midpoint, eNormal);
            }
            return E;
        }

        // -------------------------------------------------------------------------
        // Connection rules
        // -------------------------------------------------------------------------

        private static void AddAtomConnections(
            string classA, string classB,
            string[] sourceEdgeKeys,
            Dictionary<string, OVertex> ptsSingle, Dictionary<string, OVertex[]> ptsArray,
            int n, List<OEdge> edges, HashSet<(int, int)> edgeSeen)
        {
            var tmp = new List<OperatorConnection>();
            if (!TryConnections(classA, classB, ptsSingle, ptsArray, n, tmp))
            {
                if (!TryConnections(classB, classA, ptsSingle, ptsArray, n, tmp))
                    throw new ArgumentException($"Unknown atom: '{classA}-{classB}'");
            }

            var atom = $"{classA}-{classB}";
            var family = GetAtomFamily(classA, classB);
            foreach (var connection in tmp)
            {
                var a = connection.A;
                var b = connection.B;
                if (a.Id == b.Id) continue; // skip degenerate self-loops (naked-edge fallback)
                int lo = Math.Min(a.Id, b.Id), hi = Math.Max(a.Id, b.Id);
                if (edgeSeen.Add((lo, hi)))
                {
                    edges.Add(new OEdge
                    {
                        A = a,
                        B = b,
                        Atom = atom,
                        Family = family,
                        SourceEdgeKey = connection.SourceEdgeIndex >= 0 && connection.SourceEdgeIndex < sourceEdgeKeys.Length
                            ? sourceEdgeKeys[connection.SourceEdgeIndex]
                            : null
                    });
                }
            }
        }

        private static string GetSourceEdgeKey(Halfedge halfedge)
        {
            var a = halfedge.Prev.Vertex.Name;
            var b = halfedge.Vertex.Name;
            return a.CompareTo(b) <= 0 ? $"{a}_{b}" : $"{b}_{a}";
        }

        private static OperatorAtomFamily GetAtomFamily(string classA, string classB)
        {
            var familyA = GetPointFamily(classA);
            var familyB = GetPointFamily(classB);
            if (familyA == familyB)
                return familyA;

            if ((familyA == OperatorAtomFamily.Vertex && familyB == OperatorAtomFamily.Edge) ||
                (familyA == OperatorAtomFamily.Edge && familyB == OperatorAtomFamily.Vertex))
                return OperatorAtomFamily.VertexEdge;

            if ((familyA == OperatorAtomFamily.Edge && familyB == OperatorAtomFamily.Face) ||
                (familyA == OperatorAtomFamily.Face && familyB == OperatorAtomFamily.Edge))
                return OperatorAtomFamily.EdgeFace;

            return OperatorAtomFamily.VertexFace;
        }

        private static OperatorAtomFamily GetPointFamily(string pointClass)
        {
            switch (pointClass)
            {
                case "V":
                    return OperatorAtomFamily.Vertex;

                case "E":
                case "ve":
                case "ve0":
                case "ve1":
                    return OperatorAtomFamily.Edge;

                case "F":
                case "F!":
                case "vf":
                case "vf!":
                case "fe":
                case "fe!":
                    return OperatorAtomFamily.Face;

                default:
                    throw new ArgumentException($"Unknown point class '{pointClass}'");
            }
        }

        private static bool TryConnections(
            string a, string b,
            Dictionary<string, OVertex> ptsSingle, Dictionary<string, OVertex[]> ptsArray,
            int n, List<OperatorConnection> result)
        {
            OVertex[] Arr(string key) => ptsArray.TryGetValue(key, out var v) ? v : null;
            OVertex Single(string key) => ptsSingle.TryGetValue(key, out var v) ? v : null;
            void Add(OVertex from, OVertex to, int sourceEdgeIndex = -1)
            {
                result.Add(new OperatorConnection
                {
                    A = from,
                    B = to,
                    SourceEdgeIndex = sourceEdgeIndex
                });
            }

            switch ($"{a}-{b}")
            {
                case "E-E":
                { var A = Arr("E"); for (int i = 0; i < n; i++) Add(A[i], A[(i+1)%n], i); return true; }

                case "E-F":
                { var A = Arr("E"); var B = Single("F"); for (int i = 0; i < n; i++) Add(A[i], B, i); return true; }

                case "E-V":
                { var A = Arr("E"); var B = Arr("V");
                  for (int i = 0; i < n; i++) { Add(A[i], B[i], i); Add(A[i], B[(i+1)%n], i); }
                  return true; }

                case "E-ve": case "E-ve0": case "E-ve1":
                { var A = Arr("E"); var B = Arr("ve");
                  for (int i = 0; i < n; i++) { Add(A[i], B[2*i], i); Add(A[i], B[2*i+1], i); }
                  return true; }

                case "E-vf":
                { var A = Arr("E"); var B = Arr("vf");
                  for (int i = 0; i < n; i++) { Add(A[i], B[i], i); Add(A[i], B[(i+1)%n], i); }
                  return true; }

                case "E-fe":
                { var A = Arr("E"); var B = Arr("fe"); for (int i = 0; i < n; i++) Add(A[i], B[i], i); return true; }

                case "F-F!":
                { var A = Single("F"); var B = Arr("F!"); for (int i = 0; i < n; i++) Add(A, B[i], i); return true; }

                case "F-V":
                { var A = Single("F"); var B = Arr("V"); for (int i = 0; i < n; i++) Add(A, B[i]); return true; }

                case "F-ve": case "F-ve0": case "F-ve1":
                { var A = Single("F"); var B = Arr("ve"); for (int j = 0; j < 2*n; j++) Add(A, B[j]); return true; }

                case "F-vf":
                { var A = Single("F"); var B = Arr("vf"); for (int i = 0; i < n; i++) Add(A, B[i]); return true; }

                case "F-fe":
                { var A = Single("F"); var B = Arr("fe"); for (int i = 0; i < n; i++) Add(A, B[i], i); return true; }

                case "V-V":
                { var A = Arr("V"); for (int i = 0; i < n; i++) Add(A[i], A[(i+1)%n], i); return true; }

                case "V-ve": case "V-ve0": case "V-ve1":
                { var A = Arr("V"); var B = Arr("ve");
                  for (int i = 0; i < n; i++) { Add(A[i], B[ActualMod(2*i-1, 2*n)], ActualMod(i - 1, n)); Add(A[i], B[2*i], i); }
                  return true; }

                case "V-vf":
                { var A = Arr("V"); var B = Arr("vf"); for (int i = 0; i < n; i++) Add(A[i], B[i]); return true; }

                case "fe-V":
                { var A = Arr("fe"); var B = Arr("V");
                  for (int i = 0; i < n; i++) { Add(A[i], B[i], i); Add(A[i], B[(i+1)%n], i); }
                  return true; }

                case "ve0-ve0":
                { var A = Arr("ve0"); for (int i = 0; i < n; i++) Add(A[2*i], A[2*i+1], i); return true; }

                case "ve1-ve1":
                { var A = Arr("ve1"); for (int i = 0; i < n; i++) Add(A[2*i+1], A[(2*i+2)%(2*n)], i); return true; }

                case "ve-vf": case "ve0-vf": case "ve1-vf":
                { var A = Arr("ve"); var B = Arr("vf");
                  for (int i = 0; i < n; i++) { Add(A[2*i], B[i], i); Add(A[2*i+1], B[(i+1)%n], i); }
                  return true; }

                case "fe-ve": case "fe-ve0": case "fe-ve1":
                { var A = Arr("fe"); var B = Arr("ve");
                  for (int i = 0; i < n; i++) { Add(A[i], B[2*i], i); Add(A[i], B[2*i+1], i); }
                  return true; }

                case "vf-vf":
                { var A = Arr("vf"); for (int i = 0; i < n; i++) Add(A[i], A[(i+1)%n], i); return true; }

                case "vf-vf!":
                // Only the same-vertex connection: vf[i] — vf![2i] (near V[i] in adjacent face).
                // The symmetric connection vf[i+1] — vf_adj[i+1] is added by the adjacent face's
                // own processing of its pair halfedge, so no cross-connection is needed here.
                // Adding vf[i] — vf![2i+1] (the cross-connection) causes near-collinearity with
                // E[i] edges at vf[i], which breaks the CCW sort and produces wrong face loops.
                { var A = Arr("vf"); var B = Arr("vf!");
                  for (int i = 0; i < n; i++) Add(A[i], B[2*i], i);
                  return true; }

                case "fe-vf":
                { var A = Arr("fe"); var B = Arr("vf");
                  for (int i = 0; i < n; i++) { Add(A[i], B[i], i); Add(A[i], B[(i+1)%n], i); }
                  return true; }

                case "fe-fe":
                // Cyclic rule: fe[i] — fe[(i+1)%n], forming a loop around the face.
                // (The plan's _fe_fe_connections diagonal rule was incorrect — the operator
                // is described as "fe cycle" and the cyclic rule matches Zip = fe-fe,fe-fe!.)
                { var A = Arr("fe");
                  for (int i = 0; i < n; i++) Add(A[i], A[(i+1)%n], i);
                  return true; }

                case "fe-fe!":
                { var A = Arr("fe"); var B = Arr("fe!"); for (int i = 0; i < n; i++) Add(A[i], B[i], i); return true; }

                default:
                    return false;
            }
        }

        // -------------------------------------------------------------------------
        // Build halfedge mesh from an undirected edge set
        //
        // Algorithm (from halfedge_operator_plan.md):
        //   Phase 1 — create halfedge pairs
        //   Phase 2 — sort outgoing halfedges CCW at each vertex (using vertex normal)
        //   Phase 3 — set next pointers:  twin[out[j]].next = out[(j+1)%k]
        //   Phase 4 — walk unvisited halfedges to extract face loops
        // -------------------------------------------------------------------------

        private PolyMesh BuildMeshFromEdges(List<OEdge> edges)
        {
            if (edges.Count == 0)
                throw new ArgumentException("Operator produced no edges");

            int nEdges = edges.Count;
            int nHE    = 2 * nEdges;

            // heOrigin[h] = origin vertex of halfedge h
            // twin of halfedge 2i is 2i+1 and vice-versa
            var heOrigin = new OVertex[nHE];
            var heNext   = new int[nHE];

            for (int i = 0; i < nEdges; i++)
            {
                var u = edges[i].A;
                var v = edges[i].B;
                heOrigin[2*i]   = u;
                heOrigin[2*i+1] = v;
            }

            for (int i = 0; i < nHE; i++) heNext[i] = -1;

            // Group outgoing halfedges per origin vertex
            var outgoing = new Dictionary<OVertex, List<int>>();
            for (int h = 0; h < nHE; h++)
            {
                var orig = heOrigin[h];
                if (!outgoing.TryGetValue(orig, out var lst))
                {
                    lst = new List<int>();
                    outgoing[orig] = lst;
                }
                lst.Add(h);
            }

            // Sort CCW and set next pointers
            foreach (var (vert, outList) in outgoing)
            {
                int k = outList.Count;
                if (k == 1)
                {
                    // Degree-1: degenerate self-loop, face extractor will discard it
                    heNext[Twin(outList[0])] = outList[0];
                    continue;
                }

                SortCCW(outList, vert, heOrigin);
                TryRewriteTwoBundleVfStarOrder(vert, outList, edges);

                for (int j = 0; j < k; j++)
                    heNext[Twin(outList[j])] = outList[(j + 1) % k];
            }
            // Extract face loops
            var visited = new bool[nHE];
            var faceInfos = new List<ReconstructedFaceInfo>();
            var positiveOrientationFaceIndices = new List<int>();

            for (int hStart = 0; hStart < nHE; hStart++)
            {
                if (visited[hStart] || heNext[hStart] < 0) continue;

                var loop = new List<OVertex>();
                var boundaryAtoms = new List<string>();
                var boundaryFamilies = new List<OperatorAtomFamily>();
                int h = hStart;
                while (!visited[h])
                {
                    visited[h] = true;
                    var edge = edges[h / 2];
                    loop.Add(heOrigin[h]);
                    boundaryAtoms.Add(edge.Atom);
                    boundaryFamilies.Add(edge.Family);
                    h = heNext[h];
                    if (h < 0) break;
                }

                if (loop.Count >= 3)
                {
                    var centroid = Vector3.zero;
                    foreach (var v in loop) centroid += v.Position;
                    centroid /= loop.Count;

                    var faceNormal = Vector3.zero;
                    for (int k = 0; k < loop.Count; k++)
                    {
                        var curr = loop[k].Position - centroid;
                        var next = loop[(k + 1) % loop.Count].Position - centroid;
                        faceNormal += Vector3.Cross(curr, next);
                    }

                    var avgVertNormal = Vector3.zero;
                    foreach (var v in loop) avgVertNormal += v.Normal;

                    float dot = Vector3.Dot(faceNormal, avgVertNormal);
                    if (dot < 0f)
                    {
                        loop.Reverse();
                        boundaryAtoms.Reverse();
                        boundaryFamilies.Reverse();
                        RotateLeft(boundaryAtoms);
                        RotateLeft(boundaryFamilies);
                    }

                    int faceIndex = faceInfos.Count;
                    faceInfos.Add(new ReconstructedFaceInfo
                    {
                        Vertices = loop,
                        BoundaryAtoms = boundaryAtoms,
                        BoundaryFamilies = boundaryFamilies,
                        Signature = BuildAtomSignature(boundaryAtoms)
                    });
                    if (dot >= 0f)
                        positiveOrientationFaceIndices.Add(faceIndex);
                }
            }

            PruneUniqueLongestPositiveOrientationLoop(faceInfos, positiveOrientationFaceIndices);

            // Assemble PolyMesh
            var allVerts  = new List<OVertex>();
            var vertIdxMap = new Dictionary<OVertex, int>();

            foreach (var faceInfo in faceInfos)
                foreach (var v in faceInfo.Vertices)
                    if (!vertIdxMap.ContainsKey(v))
                    {
                        vertIdxMap[v] = allVerts.Count;
                        allVerts.Add(v);
                    }

            var positions   = allVerts.Select(v => v.Position).ToList();
            var faceIdxs    = faceInfos.Select(f => (IEnumerable<int>)f.Vertices.Select(v => vertIdxMap[v]).ToList()).ToList();
            var faceRoles   = AssignFaceRoles(faceInfos);
            var vertexRoles = Enumerable.Repeat(Roles.New, allVerts.Count).ToList();

            return new PolyMesh(positions, faceIdxs, faceRoles, vertexRoles);
        }


        private static void PruneUniqueLongestPositiveOrientationLoop(
            List<ReconstructedFaceInfo> faceInfos,
            List<int> positiveOrientationFaceIndices)
        {
            if (positiveOrientationFaceIndices.Count == 0)
                return;

            var candidates = positiveOrientationFaceIndices
                .Distinct()
                .Where(index => index >= 0 && index < faceInfos.Count)
                .Select(index => new { Index = index, Length = faceInfos[index].Vertices.Count })
                .ToList();

            if (candidates.Count == 0)
                return;

            int maxLength = candidates.Max(candidate => candidate.Length);
            var longestFaces = candidates.Where(candidate => candidate.Length == maxLength).ToList();
            if (longestFaces.Count != 1)
                return;

            int secondLongest = candidates
                .Where(candidate => candidate.Length < maxLength)
                .Select(candidate => candidate.Length)
                .DefaultIfEmpty(0)
                .Max();

            if (maxLength <= secondLongest)
                return;

            faceInfos.RemoveAt(longestFaces[0].Index);
        }

        private static void RotateLeft<T>(List<T> items)
        {
            if (items.Count <= 1)
                return;

            var first = items[0];
            for (int i = 0; i < items.Count - 1; i++)
                items[i] = items[i + 1];
            items[items.Count - 1] = first;
        }

        private static List<Roles> AssignFaceRoles(List<ReconstructedFaceInfo> faceInfos)
        {
            var signatureRoleMap = BuildSignatureRoleMap(faceInfos);
            return faceInfos.Select(faceInfo => signatureRoleMap[faceInfo.Signature]).ToList();
        }

        private static Dictionary<string, Roles> BuildSignatureRoleMap(List<ReconstructedFaceInfo> faceInfos)
        {
            var availableRoles = new List<Roles>
            {
                Roles.Existing,
                Roles.New,
                Roles.NewAlt,
                Roles.ExistingAlt
            };

            var groupedSignatures = faceInfos
                .GroupBy(faceInfo => faceInfo.Signature)
                .Select(group => new
                {
                    Signature = group.Key,
                    Count = group.Count(),
                    Families = group.First().BoundaryFamilies,
                    PreferredRole = ClassifyFaceRole(group.First().BoundaryFamilies)
                })
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Signature, StringComparer.Ordinal)
                .ToList();

            var roleAssignments = new Dictionary<string, Roles>();
            foreach (var group in groupedSignatures)
            {
                if (availableRoles.Contains(group.PreferredRole))
                {
                    roleAssignments[group.Signature] = group.PreferredRole;
                    availableRoles.Remove(group.PreferredRole);
                    continue;
                }

                if (availableRoles.Count > 0)
                {
                    roleAssignments[group.Signature] = availableRoles[0];
                    availableRoles.RemoveAt(0);
                    continue;
                }

                roleAssignments[group.Signature] = group.PreferredRole;
            }

            return roleAssignments;
        }

        private static string BuildAtomSignature(List<string> boundaryAtoms)
        {
            if (boundaryAtoms.Count == 0)
                return string.Empty;

            var forward = CanonicalizeCyclicSequence(boundaryAtoms);
            var reversed = CanonicalizeCyclicSequence(boundaryAtoms.AsEnumerable().Reverse().ToList());
            return string.CompareOrdinal(forward, reversed) <= 0 ? forward : reversed;
        }

        private static string CanonicalizeCyclicSequence(List<string> atoms)
        {
            var best = string.Empty;
            bool initialized = false;
            for (int start = 0; start < atoms.Count; start++)
            {
                var rotated = new string[atoms.Count];
                for (int i = 0; i < atoms.Count; i++)
                    rotated[i] = atoms[(start + i) % atoms.Count];

                var candidate = string.Join("|", rotated);
                if (!initialized || string.CompareOrdinal(candidate, best) < 0)
                {
                    best = candidate;
                    initialized = true;
                }
            }

            return best;
        }

        private static Roles ClassifyFaceRole(List<OperatorAtomFamily> boundaryFamilies)
        {
            var counts = new Dictionary<OperatorAtomFamily, int>();
            foreach (var family in boundaryFamilies)
            {
                if (!counts.ContainsKey(family))
                    counts[family] = 0;
                counts[family]++;
            }

            var dominantFamily = counts
                .OrderByDescending(x => x.Value)
                .ThenBy(x => (int)x.Key)
                .First()
                .Key;

            int vertexScore = 0;
            int edgeScore = 0;
            int faceScore = 0;

            foreach (var family in boundaryFamilies)
            {
                switch (family)
                {
                    case OperatorAtomFamily.Vertex:
                        vertexScore += 2;
                        break;

                    case OperatorAtomFamily.Edge:
                        edgeScore += 2;
                        break;

                    case OperatorAtomFamily.Face:
                        faceScore += 2;
                        break;

                    case OperatorAtomFamily.VertexEdge:
                        vertexScore++;
                        edgeScore++;
                        break;

                    case OperatorAtomFamily.EdgeFace:
                        edgeScore++;
                        faceScore++;
                        break;

                    case OperatorAtomFamily.VertexFace:
                        vertexScore++;
                        faceScore++;
                        break;
                }
            }

            if (IsStrictlyLargest(faceScore, edgeScore, vertexScore))
                return Roles.Existing;

            if (IsStrictlyLargest(edgeScore, faceScore, vertexScore))
                return Roles.NewAlt;

            if (IsStrictlyLargest(vertexScore, faceScore, edgeScore))
                return Roles.New;

            return MapAtomFamilyToRole(dominantFamily);
        }

        private static bool IsStrictlyLargest(int candidate, int otherA, int otherB)
        {
            return candidate > otherA && candidate > otherB;
        }

        private static Roles MapAtomFamilyToRole(OperatorAtomFamily family)
        {
            switch (family)
            {
                case OperatorAtomFamily.Face:
                    return Roles.Existing;

                case OperatorAtomFamily.Edge:
                    return Roles.NewAlt;

                case OperatorAtomFamily.Vertex:
                    return Roles.New;

                case OperatorAtomFamily.VertexEdge:
                    return Roles.New;

                case OperatorAtomFamily.EdgeFace:
                case OperatorAtomFamily.VertexFace:
                default:
                    return Roles.ExistingAlt;
            }
        }

        // Twin of halfedge h: swap the LSB (2i <-> 2i+1)
        private static int Twin(int h) => h ^ 1;

        // Sort outgoing halfedge indices CCW around vert using vert.Normal as the axis
        private static void SortCCW(List<int> outList, OVertex vert, OVertex[] heOrigin)
        {
            var normal = vert.Normal.normalized;
            var center = vert.Position;

            if (normal.sqrMagnitude < 1e-10f)
                normal = Vector3.up;

            // Reference direction: projection of the first destination onto the tangent plane
            var firstDest = heOrigin[Twin(outList[0])].Position - center;
            var refDir    = firstDest - Vector3.Dot(firstDest, normal) * normal;
            if (refDir.sqrMagnitude < 1e-10f)
            {
                var arb = Mathf.Abs(normal.x) < 0.9f ? Vector3.right : Vector3.up;
                refDir = Vector3.Cross(normal, arb);
            }
            refDir = refDir.normalized;
            var perpDir = Vector3.Cross(normal, refDir); // 90° CCW from refDir

            outList.Sort((ha, hb) =>
            {
                var da = heOrigin[Twin(ha)].Position - center;
                da -= Vector3.Dot(da, normal) * normal;
                float angleA = Mathf.Atan2(Vector3.Dot(da, perpDir), Vector3.Dot(da, refDir));
                float radiusA = da.magnitude;

                var db = heOrigin[Twin(hb)].Position - center;
                db -= Vector3.Dot(db, normal) * normal;
                float angleB = Mathf.Atan2(Vector3.Dot(db, perpDir), Vector3.Dot(db, refDir));
                float radiusB = db.magnitude;

                int angleCompare = angleA.CompareTo(angleB);
                if (angleCompare != 0)
                    return angleCompare;

                int radiusCompare = radiusA.CompareTo(radiusB);
                if (radiusCompare != 0)
                    return radiusCompare;

                return ha.CompareTo(hb);
            });
        }

        private static bool TryRewriteTwoBundleVfStarOrder(OVertex vert, List<int> outList, List<OEdge> edges)
        {
            if (vert.PointClass != "vf" || outList.Count != 4)
                return false;

            var bundles = outList
                .Select((halfedge, index) => new
                {
                    Halfedge = halfedge,
                    Index = index,
                    Edge = edges[halfedge / 2]
                })
                .Where(item => item.Edge.SourceEdgeKey != null)
                .GroupBy(item => item.Edge.SourceEdgeKey, StringComparer.Ordinal)
                .Select(group => new
                {
                    SourceEdgeKey = group.Key,
                    Items = group.OrderBy(item => item.Index).ToList()
                })
                .OrderBy(group => group.Items.Min(item => item.Index))
                .ToList();

            if (bundles.Count != 2 || bundles.Any(group => group.Items.Count != 2))
                return false;

            var rewritten = new List<int>(4);
            foreach (var bundle in bundles)
            {
                var far = bundle.Items.SingleOrDefault(item => item.Edge.Atom == "vf-vf");
                var near = bundle.Items.SingleOrDefault(item => item.Edge.Atom == "fe-vf");
                if (far == null || near == null)
                    return false;

                rewritten.Add(near.Halfedge);
                rewritten.Add(far.Halfedge);
            }

            int temp = rewritten[2];
            rewritten[2] = rewritten[3];
            rewritten[3] = temp;

            bool changed = false;
            for (int i = 0; i < outList.Count; i++)
            {
                if (outList[i] != rewritten[i])
                {
                    changed = true;
                    break;
                }
            }

            if (!changed)
                return false;

            outList.Clear();
            outList.AddRange(rewritten);
            return true;
        }

    }
}
