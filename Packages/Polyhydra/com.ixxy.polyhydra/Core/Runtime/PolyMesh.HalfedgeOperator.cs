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
        // "E-E", "F-F!", "E-E,E-F", "ve_e-ve_e,ve_c-ve_c", etc.
        //
        // Each atom "A-B" means: for every face, connect every point of class A to its
        // corresponding point(s) of class B. The resulting edge set is reconstructed into
        // a valid halfedge mesh by a CCW-sort / DCEL algorithm.
        //
        // Point classes:
        //   V   — original vertices (shared)
        //   E   — edge midpoints (shared by 2 faces)
        //   F   — face centroid (face-local)
        //   ve  — midpoints between V and E, 2 per edge; ve_e = same-edge pair, ve_c = corner pair (shared)
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
            public Vector3 Position;
            public Vector3 Normal;

            public OVertex(Vector3 pos, Vector3 normal)
            {
                Id = ++_idCounter;
                Position = pos;
                Normal = normal;
            }
        }

        // -------------------------------------------------------------------------
        // Cache that maps canonical string keys to shared OVertex objects
        // -------------------------------------------------------------------------

        private class OperatorVertexCache
        {
            private readonly Dictionary<string, OVertex> _cache = new Dictionary<string, OVertex>();

            public OVertex GetOrCreate(string key, Vector3 pos, Vector3 normal)
            {
                if (!_cache.TryGetValue(key, out var v))
                {
                    v = new OVertex(pos, normal);
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
        /// "F-F!" (dual), or "ve_e-ve_e,ve_c-ve_c".
        /// <para><paramref name="t"/> controls midpoint placement (0.5 = exact midpoint).</para>
        /// </summary>
        public PolyMesh ApplyHalfedgeOperator(string operatorNotation, float t = 0.5f)
        {
            var atoms = ParseHalfedgeOperatorString(operatorNotation);

            var classesNeeded = new HashSet<string>();
            foreach (var (a, b) in atoms) { classesNeeded.Add(a); classesNeeded.Add(b); }

            var cache    = new OperatorVertexCache();
            var edgeSeen = new HashSet<(int, int)>();
            var edges    = new List<(OVertex, OVertex)>();

            foreach (var face in Faces)
            {
                var halfedges = face.GetHalfedges();
                int n = halfedges.Count;

                var ptsSingle = new Dictionary<string, OVertex>();
                var ptsArray  = new Dictionary<string, OVertex[]>();

                BuildFacePoints(face, halfedges, n, classesNeeded, cache, t, ptsSingle, ptsArray);

                foreach (var (classA, classB) in atoms)
                    AddAtomConnections(classA, classB, ptsSingle, ptsArray, n, edges, edgeSeen);
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
        //   fe[i]     = lerp(E[i],   F, t)
        //   F![i]     = centroid of face across edge i
        //   fe![i]    = lerp(E[i],   F![i], t)
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
            bool needVe    = classesNeeded.Contains("ve") || classesNeeded.Contains("ve_e") || classesNeeded.Contains("ve_c");
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
                    V[i] = cache.GetOrCreate($"V_{vert.Name}", vert.Position, vert.Normal);
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
                F = cache.GetOrCreate($"F_{face.Name}", face.Centroid, fn);
                ptsSingle["F"] = F;
            }

            // --- ve / ve_e / ve_c  (all share the same backing array) ---
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
                    ve[2*i]   = cache.GetOrCreate($"ve_{h.Prev.Vertex.Name}_{MakeKey(h.PairedName)}",
                        Vector3.Lerp(h.Prev.Vertex.Position, E[i].Position, t), eNorm);
                    ve[2*i+1] = cache.GetOrCreate($"ve_{h.Vertex.Name}_{MakeKey(h.PairedName)}",
                        Vector3.Lerp(h.Vertex.Position, E[i].Position, t), eNorm);
                }
                ptsArray["ve"]   = ve;
                ptsArray["ve_e"] = ve;
                ptsArray["ve_c"] = ve;
            }

            // --- vf ---
            // Keyed by (face, vertex) so that vf![2i]/vf![2i+1] in adjacent faces resolve
            // to the same OVertex objects as the corresponding vf[k] computed from that face.
            if (needVf)
            {
                if (F == null) { F = cache.GetOrCreate($"F_{face.Name}", face.Centroid, fn); ptsSingle["F"] = F; }
                var vf = new OVertex[n];
                for (int i = 0; i < n; i++)
                {
                    var vert = halfedges[i].Prev.Vertex;
                    vf[i] = cache.GetOrCreate(
                        $"vf_{face.Name}_{vert.Name}",
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
                if (F == null) { F = cache.GetOrCreate($"F_{face.Name}", face.Centroid, fn); ptsSingle["F"] = F; }
                if (E == null) { E = ComputeEArray(face, halfedges, n, fn, cache); ptsArray["E"] = E; }
                var fe = new OVertex[n];
                for (int i = 0; i < n; i++)
                    fe[i] = cache.GetOrCreate(
                        $"fe_{face.Name}_{MakeKey(halfedges[i].PairedName)}",
                        Vector3.Lerp(E[i].Position, F.Position, t),
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
                        FAdjacent[i] = cache.GetOrCreate($"F_{face.Name}", face.Centroid, fn);
                    }
                    else
                    {
                        var adj = pair.Face;
                        FAdjacent[i] = cache.GetOrCreate($"F_{adj.Name}", adj.Centroid, adj.Normal);
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
                        Vector3.Lerp(E[i].Position, FAdjacent[i].Position, t),
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
                        Vector3.Lerp(vOrigin.Position, FAdjacent[i].Position, t),
                        Vector3.Lerp(vOrigin.Normal,   FAdjacent[i].Normal,   t).normalized);
                    vfAdj[2*i+1] = cache.GetOrCreate(
                        $"vf_{adjFaceName}_{vDest.Name}",
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
                E[i] = cache.GetOrCreate($"E_{MakeKey(h.PairedName)}", h.Midpoint, eNormal);
            }
            return E;
        }

        // -------------------------------------------------------------------------
        // Connection rules
        // -------------------------------------------------------------------------

        private static void AddAtomConnections(
            string classA, string classB,
            Dictionary<string, OVertex> ptsSingle, Dictionary<string, OVertex[]> ptsArray,
            int n, List<(OVertex, OVertex)> edges, HashSet<(int, int)> edgeSeen)
        {
            var tmp = new List<(OVertex, OVertex)>();
            if (!TryConnections(classA, classB, ptsSingle, ptsArray, n, tmp))
            {
                if (!TryConnections(classB, classA, ptsSingle, ptsArray, n, tmp))
                    throw new ArgumentException($"Unknown atom: '{classA}-{classB}'");
            }

            foreach (var (a, b) in tmp)
            {
                if (a.Id == b.Id) continue; // skip degenerate self-loops (naked-edge fallback)
                int lo = Math.Min(a.Id, b.Id), hi = Math.Max(a.Id, b.Id);
                if (edgeSeen.Add((lo, hi)))
                    edges.Add((a, b));
            }
        }

        private static bool TryConnections(
            string a, string b,
            Dictionary<string, OVertex> ptsSingle, Dictionary<string, OVertex[]> ptsArray,
            int n, List<(OVertex, OVertex)> result)
        {
            OVertex[] Arr(string key) => ptsArray.TryGetValue(key, out var v) ? v : null;
            OVertex Single(string key) => ptsSingle.TryGetValue(key, out var v) ? v : null;

            switch ($"{a}-{b}")
            {
                case "E-E":
                { var A = Arr("E"); for (int i = 0; i < n; i++) result.Add((A[i], A[(i+1)%n])); return true; }

                case "E-F":
                { var A = Arr("E"); var B = Single("F"); for (int i = 0; i < n; i++) result.Add((A[i], B)); return true; }

                case "E-V":
                { var A = Arr("E"); var B = Arr("V");
                  for (int i = 0; i < n; i++) { result.Add((A[i], B[i])); result.Add((A[i], B[(i+1)%n])); }
                  return true; }

                case "E-ve": case "E-ve_e": case "E-ve_c":
                { var A = Arr("E"); var B = Arr("ve");
                  for (int i = 0; i < n; i++) { result.Add((A[i], B[2*i])); result.Add((A[i], B[2*i+1])); }
                  return true; }

                case "E-vf":
                { var A = Arr("E"); var B = Arr("vf");
                  for (int i = 0; i < n; i++) { result.Add((A[i], B[i])); result.Add((A[i], B[(i+1)%n])); }
                  return true; }

                case "E-fe":
                { var A = Arr("E"); var B = Arr("fe"); for (int i = 0; i < n; i++) result.Add((A[i], B[i])); return true; }

                case "F-F!":
                { var A = Single("F"); var B = Arr("F!"); for (int i = 0; i < n; i++) result.Add((A, B[i])); return true; }

                case "F-V":
                { var A = Single("F"); var B = Arr("V"); for (int i = 0; i < n; i++) result.Add((A, B[i])); return true; }

                case "F-ve": case "F-ve_e": case "F-ve_c":
                { var A = Single("F"); var B = Arr("ve"); for (int j = 0; j < 2*n; j++) result.Add((A, B[j])); return true; }

                case "F-vf":
                { var A = Single("F"); var B = Arr("vf"); for (int i = 0; i < n; i++) result.Add((A, B[i])); return true; }

                case "F-fe":
                { var A = Single("F"); var B = Arr("fe"); for (int i = 0; i < n; i++) result.Add((A, B[i])); return true; }

                case "V-V":
                { var A = Arr("V"); for (int i = 0; i < n; i++) result.Add((A[i], A[(i+1)%n])); return true; }

                case "V-ve": case "V-ve_e": case "V-ve_c":
                { var A = Arr("V"); var B = Arr("ve");
                  for (int i = 0; i < n; i++) { result.Add((A[i], B[ActualMod(2*i-1, 2*n)])); result.Add((A[i], B[2*i])); }
                  return true; }

                case "V-vf":
                { var A = Arr("V"); var B = Arr("vf"); for (int i = 0; i < n; i++) result.Add((A[i], B[i])); return true; }

                case "fe-V":
                { var A = Arr("fe"); var B = Arr("V");
                  for (int i = 0; i < n; i++) { result.Add((A[i], B[i])); result.Add((A[i], B[(i+1)%n])); }
                  return true; }

                case "ve_e-ve_e":
                { var A = Arr("ve_e"); for (int i = 0; i < n; i++) result.Add((A[2*i], A[2*i+1])); return true; }

                case "ve_c-ve_c":
                { var A = Arr("ve_c"); for (int i = 0; i < n; i++) result.Add((A[2*i+1], A[(2*i+2)%(2*n)])); return true; }

                case "ve-vf": case "ve_e-vf": case "ve_c-vf":
                { var A = Arr("ve"); var B = Arr("vf");
                  for (int i = 0; i < n; i++) { result.Add((A[2*i], B[i])); result.Add((A[2*i+1], B[(i+1)%n])); }
                  return true; }

                case "fe-ve": case "fe-ve_e": case "fe-ve_c":
                { var A = Arr("fe"); var B = Arr("ve");
                  for (int i = 0; i < n; i++) { result.Add((A[i], B[2*i])); result.Add((A[i], B[2*i+1])); }
                  return true; }

                case "vf-vf":
                { var A = Arr("vf"); for (int i = 0; i < n; i++) result.Add((A[i], A[(i+1)%n])); return true; }

                case "vf-vf!":
                // Only the same-vertex connection: vf[i] — vf![2i] (near V[i] in adjacent face).
                // The symmetric connection vf[i+1] — vf_adj[i+1] is added by the adjacent face's
                // own processing of its pair halfedge, so no cross-connection is needed here.
                // Adding vf[i] — vf![2i+1] (the cross-connection) causes near-collinearity with
                // E[i] edges at vf[i], which breaks the CCW sort and produces wrong face loops.
                { var A = Arr("vf"); var B = Arr("vf!");
                  for (int i = 0; i < n; i++) result.Add((A[i], B[2*i]));
                  return true; }

                case "fe-vf":
                { var A = Arr("fe"); var B = Arr("vf");
                  for (int i = 0; i < n; i++) { result.Add((A[i], B[i])); result.Add((A[i], B[(i+1)%n])); }
                  return true; }

                case "fe-fe":
                // Cyclic rule: fe[i] — fe[(i+1)%n], forming a loop around the face.
                // (The plan's _fe_fe_connections diagonal rule was incorrect — the operator
                // is described as "fe cycle" and the cyclic rule matches Zip = fe-fe,fe-fe!.)
                { var A = Arr("fe");
                  for (int i = 0; i < n; i++) result.Add((A[i], A[(i+1)%n]));
                  return true; }

                case "fe-fe!":
                { var A = Arr("fe"); var B = Arr("fe!"); for (int i = 0; i < n; i++) result.Add((A[i], B[i])); return true; }

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

        private PolyMesh BuildMeshFromEdges(List<(OVertex, OVertex)> edges)
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
                var (u, v) = edges[i];
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

                for (int j = 0; j < k; j++)
                    heNext[Twin(outList[j])] = outList[(j + 1) % k];
            }

            // Extract face loops
            var visited   = new bool[nHE];
            var faceLists = new List<List<OVertex>>();

            for (int hStart = 0; hStart < nHE; hStart++)
            {
                if (visited[hStart] || heNext[hStart] < 0) continue;

                var loop = new List<OVertex>();
                int h = hStart;
                while (!visited[h])
                {
                    visited[h] = true;
                    loop.Add(heOrigin[h]);
                    h = heNext[h];
                    if (h < 0) break;
                }

                if (loop.Count >= 3)
                    faceLists.Add(loop);
            }

            // Assemble PolyMesh
            var allVerts  = new List<OVertex>();
            var vertIdxMap = new Dictionary<OVertex, int>();

            foreach (var fv in faceLists)
                foreach (var v in fv)
                    if (!vertIdxMap.ContainsKey(v))
                    {
                        vertIdxMap[v] = allVerts.Count;
                        allVerts.Add(v);
                    }

            var positions   = allVerts.Select(v => v.Position).ToList();
            var faceIdxs    = faceLists.Select(fv => (IEnumerable<int>)fv.Select(v => vertIdxMap[v]).ToList()).ToList();
            var faceRoles   = Enumerable.Repeat(Roles.New, faceLists.Count).ToList();
            var vertexRoles = Enumerable.Repeat(Roles.New, allVerts.Count).ToList();

            return new PolyMesh(positions, faceIdxs, faceRoles, vertexRoles);
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

                var db = heOrigin[Twin(hb)].Position - center;
                db -= Vector3.Dot(db, normal) * normal;
                float angleB = Mathf.Atan2(Vector3.Dot(db, perpDir), Vector3.Dot(db, refDir));

                return angleA.CompareTo(angleB);
            });
        }
    }
}
