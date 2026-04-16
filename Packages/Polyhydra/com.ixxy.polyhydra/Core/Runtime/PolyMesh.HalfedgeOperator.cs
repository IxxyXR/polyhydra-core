using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        private const string HalfedgeOperatorBaselineDebugPrefix = "[HOP0_BASE_20260415A]";
        private const string HalfedgeOperatorMultiplicityDebugPrefix = "[HOP0_MULTI_20260415B]";
        private const string HalfedgeOperatorOrientationDebugPrefix = "[HOP0_ORIENT_20260415C]";
        private const string HalfedgeOperatorSortDebugPrefix = "[HOP0_SORT_20260415D]";
        private const string HalfedgeOperatorOrderDebugPrefix = "[HOP0_ORDER_20260415E]";
        private const string HalfedgeOperatorFeOrderDebugPrefix = "[HOP0_FEORDER_20260415F]";
        private const string HalfedgeOperatorBundleDebugPrefix = "[HOP0_BUNDLE_20260415G]";
        private const string HalfedgeOperatorSampleDebugPrefix = "[HOP0_SAMPLE_20260415H]";
        private const string HalfedgeOperatorFaceSampleDebugPrefix = "[HOP0_FACESRC_20260416A]";
        private const string HalfedgeOperatorChoiceDebugPrefix = "[HOP0_CHOICE_20260416C]";
        private const string HalfedgeOperatorSourceEdgeDebugPrefix = "[HOP0_SRCEDGE_20260416E]";
        private const string HalfedgeOperatorVfTransitionDebugPrefix = "[HOP0_VFTRANS_20260416G]";
        private const string HalfedgeOperatorVfClassDebugPrefix = "[HOP0_VFCLASS_20260416I]";
        private const string HalfedgeOperatorVfOrderRewriteDebugPrefix = "[HOP0_VFORDER_20260416J]";
        private const string HalfedgeOperatorFeTransitionDebugPrefix = "[HOP0_FETRANS_20260416K]";
        private const string HalfedgeOperatorVfIncomingFeDebugPrefix = "[HOP0_VFFEIN_20260416L]";
        private const string HalfedgeOperatorEdgeCoverageDebugPrefix = "[HOP0_EDGECOV_20260416M]";
        private const string HalfedgeOperatorRejectDebugPrefix = "[HOP0_REJECT_20260416O]";
        private const string HalfedgeOperatorFePlacementDebugPrefix = "[HOP0_FEPOS_20260416V]";
        private static string _activeHalfedgeOperatorNotation;
        private static int _remainingDetailedOrderLogs;
        private static int _remainingFeOrderLogs;

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
            public string SourceFaceName;
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
            public List<int> BoundaryHalfedges;
            public List<string> BoundaryAtoms;
            public List<OperatorAtomFamily> BoundaryFamilies;
            public List<string> BoundarySourceFaces;
            public List<string> BoundarySourceEdges;
            public List<string> BoundaryStepDescriptions;
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
            _activeHalfedgeOperatorNotation = operatorNotation;
            _remainingDetailedOrderLogs = string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal) ? 8 : 0;
            _remainingFeOrderLogs = string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal) ? 8 : 0;
            var atoms = ParseHalfedgeOperatorString(operatorNotation);

            var classesNeeded = new HashSet<string>();
            foreach (var (a, b) in atoms) { classesNeeded.Add(a); classesNeeded.Add(b); }

            var cache    = new OperatorVertexCache();
            var edgeSeen = new HashSet<(int, int)>();
            var edges    = new List<OEdge>();
            var attemptedEdgeAtoms = new Dictionary<(int, int), List<string>>();

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
                    AddAtomConnections(classA, classB, face.Name, sourceEdgeKeys, ptsSingle, ptsArray, n, edges, edgeSeen, attemptedEdgeAtoms);
            }

            LogAttemptedEdgeMultiplicity(operatorNotation, attemptedEdgeAtoms);

            try
            {
                return BuildMeshFromEdges(edges, operatorNotation);
            }
            finally
            {
                _activeHalfedgeOperatorNotation = null;
                _remainingDetailedOrderLogs = 0;
                _remainingFeOrderLogs = 0;
            }
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
            string sourceFaceName,
            string[] sourceEdgeKeys,
            Dictionary<string, OVertex> ptsSingle, Dictionary<string, OVertex[]> ptsArray,
            int n, List<OEdge> edges, HashSet<(int, int)> edgeSeen,
            Dictionary<(int, int), List<string>> attemptedEdgeAtoms)
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
                var key = (lo, hi);
                if (!attemptedEdgeAtoms.TryGetValue(key, out var attemptedAtoms))
                {
                    attemptedAtoms = new List<string>();
                    attemptedEdgeAtoms[key] = attemptedAtoms;
                }
                attemptedAtoms.Add(atom);
                if (edgeSeen.Add((lo, hi)))
                {
                    edges.Add(new OEdge
                    {
                        A = a,
                        B = b,
                        Atom = atom,
                        Family = family,
                        SourceFaceName = sourceFaceName,
                        SourceEdgeKey = connection.SourceEdgeIndex >= 0 && connection.SourceEdgeIndex < sourceEdgeKeys.Length
                            ? sourceEdgeKeys[connection.SourceEdgeIndex]
                            : null
                    });
                }
            }
        }

        private static void LogAttemptedEdgeMultiplicity(string operatorNotation, Dictionary<(int, int), List<string>> attemptedEdgeAtoms)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var duplicates = attemptedEdgeAtoms
                .Where(entry => entry.Value.Count > 1)
                .Select(entry => $"{entry.Value.Count}x{string.Join("/", entry.Value.OrderBy(atom => atom, StringComparer.Ordinal))}")
                .GroupBy(text => text)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Take(12)
                .Select(group => $"{group.Count()}x{group.Key}");

            Debug.Log($"{HalfedgeOperatorMultiplicityDebugPrefix} duplicate-pairs=[{string.Join(" | ", duplicates)}]");
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

        private PolyMesh BuildMeshFromEdges(List<OEdge> edges, string operatorNotation)
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
            var hePrev = new int[nHE];
            for (int i = 0; i < nHE; i++) hePrev[i] = -1;

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

            var orderedOutgoing = new Dictionary<OVertex, List<int>>();
            int rewrittenTwoBundleVfStars = 0;

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

                SortCCW(outList, vert, heOrigin, edges);
                if (TryRewriteTwoBundleVfStarOrder(vert, outList, edges))
                    rewrittenTwoBundleVfStars++;
                orderedOutgoing[vert] = outList.ToList();

                for (int j = 0; j < k; j++)
                {
                    heNext[Twin(outList[j])] = outList[(j + 1) % k];
                    hePrev[Twin(outList[j])] = outList[(j - 1 + k) % k];
                }

            }

            LogOrientationComparison(operatorNotation, edges, heOrigin, heNext, hePrev);
            LogBundleRuleComparison(operatorNotation, edges, heOrigin, heNext, orderedOutgoing);
            LogVfStarTransitionSummary(operatorNotation, edges, heNext, orderedOutgoing);
            LogVfStarTransitionClasses(operatorNotation, edges, heNext, orderedOutgoing);
            LogVfIncomingFeClasses(operatorNotation, edges, heNext, orderedOutgoing);
            LogFeStarTransitionClasses(operatorNotation, edges, heNext, orderedOutgoing);
            if (string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                Debug.Log($"{HalfedgeOperatorVfOrderRewriteDebugPrefix} rewritten-two-bundle-vf-stars={rewrittenTwoBundleVfStars}");

            // Extract face loops
            var visited = new bool[nHE];
            var faceInfos = new List<ReconstructedFaceInfo>();
            var keptLoopSignatures = new List<string>();
            var rejectedLoopSignatures = new List<string>();
            var positiveOrientationFaceIndices = new List<int>();

            for (int hStart = 0; hStart < nHE; hStart++)
            {
                if (visited[hStart] || heNext[hStart] < 0) continue;

                var loop = new List<OVertex>();
                var boundaryHalfedges = new List<int>();
                var boundaryAtoms = new List<string>();
                var boundaryFamilies = new List<OperatorAtomFamily>();
                var boundarySourceFaces = new List<string>();
                var boundarySourceEdges = new List<string>();
                var boundaryStepDescriptions = new List<string>();
                int h = hStart;
                while (!visited[h])
                {
                    visited[h] = true;
                    var origin = heOrigin[h];
                    var dest = heOrigin[Twin(h)];
                    var edge = edges[h / 2];
                    loop.Add(origin);
                    boundaryHalfedges.Add(h);
                    boundaryAtoms.Add(edge.Atom);
                    boundaryFamilies.Add(edge.Family);
                    boundarySourceFaces.Add(edge.SourceFaceName);
                    boundarySourceEdges.Add(edge.SourceEdgeKey);
                    boundaryStepDescriptions.Add($"{origin.PointClass}:{origin.Id}-{edge.Atom}:{edge.SourceEdgeKey}->{dest.PointClass}:{dest.Id}");
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
                    string pointClassSequence = CanonicalizePointClassSequence(loop);

                    if (dot < 0f)
                    {
                        loop.Reverse();
                        boundaryHalfedges.Reverse();
                        boundaryAtoms.Reverse();
                        boundaryFamilies.Reverse();
                        RotateLeft(boundaryHalfedges);
                        RotateLeft(boundaryAtoms);
                        RotateLeft(boundaryFamilies);
                        RotateLeft(boundarySourceFaces);
                        RotateLeft(boundarySourceEdges);
                        RotateLeft(boundaryStepDescriptions);
                    }

                    if (IsAdjacentDuplicateEdgeTriangle(loop))
                    {
                        rejectedLoopSignatures.Add(pointClassSequence);
                    }
                    else
                    {
                        int faceIndex = faceInfos.Count;
                        faceInfos.Add(new ReconstructedFaceInfo
                        {
                            Vertices = loop,
                            BoundaryHalfedges = boundaryHalfedges,
                            BoundaryAtoms = boundaryAtoms,
                            BoundaryFamilies = boundaryFamilies,
                            BoundarySourceFaces = boundarySourceFaces,
                            BoundarySourceEdges = boundarySourceEdges,
                            BoundaryStepDescriptions = boundaryStepDescriptions,
                            Signature = BuildAtomSignature(boundaryAtoms)
                        });
                        if (dot >= 0f)
                            positiveOrientationFaceIndices.Add(faceIndex);
                        keptLoopSignatures.Add(pointClassSequence);
                    }
                }
            }

            PruneUniqueLongestPositiveOrientationLoop(operatorNotation, faceInfos, keptLoopSignatures, rejectedLoopSignatures, positiveOrientationFaceIndices);
            LogBaselineLoopSummary(operatorNotation, faceInfos, edges, heOrigin, heNext, orderedOutgoing);
            LogEdgeTriangleCoverage(operatorNotation, edges, faceInfos);
            LogRejectedLoopSummary(operatorNotation, keptLoopSignatures, rejectedLoopSignatures);

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

            LogConstructorRejectedFaces(operatorNotation, positions, faceInfos, faceIdxs, faceRoles);
            var result = new PolyMesh(positions, faceIdxs, faceRoles, vertexRoles);
            LogFinalAcceptedFaceSummary(operatorNotation, faceInfos, result);
            LogTriangleGeometrySamples(operatorNotation, faceInfos, result);
            return result;
        }

        private static void LogBaselineLoopSummary(
            string operatorNotation,
            List<ReconstructedFaceInfo> faceInfos,
            List<OEdge> edges,
            OVertex[] heOrigin,
            int[] heNext,
            Dictionary<OVertex, List<int>> orderedOutgoing)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var loopSummary = faceInfos
                .GroupBy(faceInfo => CanonicalizePointClassSequence(faceInfo.Vertices))
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Take(12)
                .Select(group => $"{group.Count()}x{group.Key}");

            Debug.Log($"{HalfedgeOperatorBaselineDebugPrefix} loops=[{string.Join(" | ", loopSummary)}]");

            var representative = faceInfos
                .GroupBy(faceInfo => CanonicalizePointClassSequence(faceInfo.Vertices))
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new
                {
                    Sequence = group.Key,
                    Sample = group.First()
                })
                .Take(6)
                .Select(entry => $"{entry.Sequence} => {DescribeLoop(entry.Sample)}");

            Debug.Log($"{HalfedgeOperatorSampleDebugPrefix} samples=[{string.Join(" || ", representative)}]");

            var faceRepresentative = faceInfos
                .GroupBy(faceInfo => CanonicalizePointClassSequence(faceInfo.Vertices))
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new
                {
                    Sequence = group.Key,
                    Sample = group.First()
                })
                .Take(4)
                .Select(entry => $"{entry.Sequence} => {DescribeLoopFaces(entry.Sample)}");

            Debug.Log($"{HalfedgeOperatorFaceSampleDebugPrefix} samples=[{string.Join(" || ", faceRepresentative)}]");

            var sourceEdgeRepresentative = faceInfos
                .GroupBy(faceInfo => CanonicalizePointClassSequence(faceInfo.Vertices))
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new
                {
                    Sequence = group.Key,
                    Sample = group.First()
                })
                .Take(4)
                .Select(entry => $"{entry.Sequence} => {DescribeLoopSourceEdges(entry.Sample)}");

            Debug.Log($"{HalfedgeOperatorSourceEdgeDebugPrefix} samples=[{string.Join(" || ", sourceEdgeRepresentative)}]");

            var longBadLoop = faceInfos
                .Where(faceInfo => faceInfo.Vertices.Count >= 8 && faceInfo.Vertices.Any(vertex => vertex.PointClass == "fe"))
                .OrderByDescending(faceInfo => faceInfo.Vertices.Count)
                .FirstOrDefault();
            if (longBadLoop != null)
                Debug.Log($"{HalfedgeOperatorChoiceDebugPrefix} trace=[{DescribeLoopChoices(longBadLoop, edges, heOrigin, heNext, orderedOutgoing)}]");
        }

        private static void LogOrientationComparison(
            string operatorNotation,
            List<OEdge> edges,
            OVertex[] heOrigin,
            int[] heNext,
            int[] hePrev)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var nextSummary = SummarizeLoopSequences(edges, heOrigin, heNext);
            var prevSummary = SummarizeLoopSequences(edges, heOrigin, hePrev);
            Debug.Log($"{HalfedgeOperatorOrientationDebugPrefix} next=[{nextSummary}] prev=[{prevSummary}]");
        }

        private static void LogBundleRuleComparison(
            string operatorNotation,
            List<OEdge> edges,
            OVertex[] heOrigin,
            int[] heNext,
            Dictionary<OVertex, List<int>> orderedOutgoing)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var sameBundle = (int[])heNext.Clone();
            var laneNext = (int[])heNext.Clone();
            var farOnly = (int[])heNext.Clone();

            foreach (var (vert, outList) in orderedOutgoing)
            {
                if (vert.PointClass != "vf" || outList.Count != 4)
                    continue;

                if (!TryGetTwoBundleVfOrder(vert, outList, heOrigin, out var near0, out var far0, out var near1, out var far1))
                    continue;

                sameBundle[Twin(near0)] = far0;
                sameBundle[Twin(far0)] = near0;
                sameBundle[Twin(near1)] = far1;
                sameBundle[Twin(far1)] = near1;

                laneNext[Twin(near0)] = near1;
                laneNext[Twin(far0)] = far1;
                laneNext[Twin(near1)] = near0;
                laneNext[Twin(far1)] = far0;

                farOnly[Twin(far0)] = far1;
                farOnly[Twin(far1)] = far0;
            }

            var sameBundleSummary = SummarizeLoopSequences(edges, heOrigin, sameBundle);
            var laneNextSummary = SummarizeLoopSequences(edges, heOrigin, laneNext);
            var farOnlySummary = SummarizeLoopSequences(edges, heOrigin, farOnly);
            Debug.Log($"{HalfedgeOperatorBundleDebugPrefix} same=[{sameBundleSummary}] lane=[{laneNextSummary}] far=[{farOnlySummary}]");
        }


        private static bool TryGetTwoBundleVfOrder(
            OVertex vert,
            List<int> outList,
            OVertex[] heOrigin,
            out int near0,
            out int far0,
            out int near1,
            out int far1)
        {
            near0 = far0 = near1 = far1 = -1;

            var normal = vert.Normal.normalized;
            var center = vert.Position;
            if (normal.sqrMagnitude < 1e-10f)
                normal = Vector3.up;

            var firstDest = heOrigin[Twin(outList[0])].Position - center;
            var refDir = firstDest - Vector3.Dot(firstDest, normal) * normal;
            if (refDir.sqrMagnitude < 1e-10f)
            {
                var arb = Mathf.Abs(normal.x) < 0.9f ? Vector3.right : Vector3.up;
                refDir = Vector3.Cross(normal, arb);
            }
            refDir = refDir.normalized;
            var perpDir = Vector3.Cross(normal, refDir);

            var infos = outList.Select(halfedge =>
            {
                var delta = heOrigin[Twin(halfedge)].Position - center;
                delta -= Vector3.Dot(delta, normal) * normal;
                return new
                {
                    Halfedge = halfedge,
                    Angle = Mathf.Atan2(Vector3.Dot(delta, perpDir), Vector3.Dot(delta, refDir)),
                    Radius = delta.magnitude
                };
            }).OrderBy(info => info.Angle).ThenBy(info => info.Radius).ThenBy(info => info.Halfedge).ToList();

            const float angleEpsilon = 1e-4f;
            if (infos.Count != 4)
                return false;
            if (Mathf.Abs(infos[1].Angle - infos[0].Angle) > angleEpsilon)
                return false;
            if (Mathf.Abs(infos[3].Angle - infos[2].Angle) > angleEpsilon)
                return false;
            if (Mathf.Abs(infos[2].Angle - infos[1].Angle) <= angleEpsilon)
                return false;

            near0 = infos[0].Halfedge;
            far0 = infos[1].Halfedge;
            near1 = infos[2].Halfedge;
            far1 = infos[3].Halfedge;
            return true;
        }

        private static string SummarizeLoopSequences(List<OEdge> edges, OVertex[] heOrigin, int[] successor)
        {
            var visited = new bool[successor.Length];
            var summaries = new List<string>();

            for (int hStart = 0; hStart < successor.Length; hStart++)
            {
                if (visited[hStart] || successor[hStart] < 0)
                    continue;

                var loop = new List<OVertex>();
                int h = hStart;
                while (!visited[h])
                {
                    visited[h] = true;
                    loop.Add(heOrigin[h]);
                    h = successor[h];
                    if (h < 0)
                        break;
                }

                if (loop.Count < 3)
                    continue;

                var centroid = Vector3.zero;
                foreach (var vertex in loop)
                    centroid += vertex.Position;
                centroid /= loop.Count;

                var faceNormal = Vector3.zero;
                for (int i = 0; i < loop.Count; i++)
                {
                    var curr = loop[i].Position - centroid;
                    var next = loop[(i + 1) % loop.Count].Position - centroid;
                    faceNormal += Vector3.Cross(curr, next);
                }

                var avgVertNormal = Vector3.zero;
                foreach (var vertex in loop)
                    avgVertNormal += vertex.Normal;

                if (Vector3.Dot(faceNormal, avgVertNormal) < 0f)
                    loop.Reverse();

                summaries.Add(CanonicalizePointClassSequence(loop));
            }

            return string.Join(" | ", summaries
                .GroupBy(sequence => sequence)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Take(8)
                .Select(group => $"{group.Count()}x{group.Key}"));
        }

        private static string CanonicalizePointClassSequence(List<OVertex> loop)
        {
            if (loop.Count == 0)
                return string.Empty;

            var pointClasses = loop.Select(vertex => vertex.PointClass).ToList();
            var forward = CanonicalizeCyclicSequence(pointClasses);
            var reversed = CanonicalizeCyclicSequence(pointClasses.AsEnumerable().Reverse().ToList());
            return string.CompareOrdinal(forward, reversed) <= 0 ? forward : reversed;
        }

        private static bool IsAdjacentDuplicateEdgeTriangle(List<OVertex> loop)
        {
            if (loop.Count != 3)
                return false;

            int vfCount = loop.Count(vertex => vertex.PointClass == "vf");
            int feBangCount = loop.Count(vertex => vertex.PointClass == "fe!");
            return vfCount == 2 && feBangCount == 1;
        }

        private static void PruneUniqueLongestPositiveOrientationLoop(
            string operatorNotation,
            List<ReconstructedFaceInfo> faceInfos,
            List<string> keptLoopSignatures,
            List<string> rejectedLoopSignatures,
            List<int> positiveOrientationFaceIndices)
        {
            if (positiveOrientationFaceIndices.Count == 0)
                return;

            var candidates = positiveOrientationFaceIndices
                .Distinct()
                .Where(index => index >= 0 && index < faceInfos.Count)
                .Select(index => new { index, length = faceInfos[index].Vertices.Count })
                .ToList();

            if (candidates.Count == 0)
                return;

            int maxLength = candidates.Max(candidate => candidate.length);
            var longestFaces = candidates
                .Where(candidate => candidate.length == maxLength)
                .ToList();

            if (longestFaces.Count != 1)
                return;

            int secondLongest = candidates
                .Where(candidate => candidate.length < maxLength)
                .Select(candidate => candidate.length)
                .DefaultIfEmpty(0)
                .Max();

            if (maxLength <= secondLongest)
                return;

            int exteriorIndex = longestFaces[0].index;
            string rejectedSignature = CanonicalizePointClassSequence(faceInfos[exteriorIndex].Vertices);
            faceInfos.RemoveAt(exteriorIndex);
            int keptIndex = keptLoopSignatures.IndexOf(rejectedSignature);
            if (keptIndex >= 0)
                keptLoopSignatures.RemoveAt(keptIndex);
            rejectedLoopSignatures.Add(rejectedSignature);

            if (string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                Debug.Log($"[HOP0_OUTER_20260416P] positive={candidates.Count} dropped-length={maxLength} next-length={secondLongest} remaining={faceInfos.Count}");
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

        private static string DescribeLoop(ReconstructedFaceInfo faceInfo)
        {
            return string.Join(" | ", faceInfo.BoundaryStepDescriptions);
        }

        private static string DescribeLoopFaces(ReconstructedFaceInfo faceInfo)
        {
            return string.Join(" | ", faceInfo.BoundaryStepDescriptions);
        }

        private static string DescribeLoopSourceEdges(ReconstructedFaceInfo faceInfo)
        {
            var parts = new List<string>(faceInfo.BoundaryHalfedges.Count);
            for (int i = 0; i < faceInfo.BoundaryHalfedges.Count; i++)
            {
                var sourceEdge = faceInfo.BoundarySourceEdges[i] ?? "?";
                parts.Add($"{faceInfo.BoundaryAtoms[i]}:{sourceEdge}");
            }

            return string.Join(" | ", parts);
        }

        private static string DescribeLoopChoices(
            ReconstructedFaceInfo faceInfo,
            List<OEdge> edges,
            OVertex[] heOrigin,
            int[] heNext,
            Dictionary<OVertex, List<int>> orderedOutgoing)
        {
            var parts = new List<string>();
            for (int i = 0; i < faceInfo.BoundaryHalfedges.Count; i++)
            {
                int h = faceInfo.BoundaryHalfedges[i];
                int next = heNext[h];
                var origin = heOrigin[h];
                var chosenEdge = edges[next / 2];
                var chosen = $"{chosenEdge.Atom}:{chosenEdge.SourceEdgeKey}";
                string options = "?";
                if (orderedOutgoing.TryGetValue(heOrigin[Twin(h)], out var outList))
                {
                    options = string.Join(",",
                        outList.Select(outHe =>
                        {
                            var edge = edges[outHe / 2];
                            var dest = heOrigin[Twin(outHe)];
                            return $"{edge.Atom}:{edge.SourceEdgeKey}->{dest.PointClass}:{dest.Id}";
                        }));
                }

                var edgeKey = edges[h / 2].SourceEdgeKey ?? "?";
                parts.Add($"{origin.PointClass}:{origin.Id}-{edges[h / 2].Atom}:{edgeKey}->next={chosen}@{heOrigin[Twin(h)].PointClass}:{heOrigin[Twin(h)].Id} opts=[{options}]");
            }

            return string.Join(" || ", parts);
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

        private static void LogVfStarTransitionSummary(
            string operatorNotation,
            List<OEdge> edges,
            int[] heNext,
            Dictionary<OVertex, List<int>> orderedOutgoing)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var groups = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var (vert, outList) in orderedOutgoing)
            {
                if (vert.PointClass != "vf" || outList.Count != 4)
                    continue;

                var bundleCounts = outList
                    .Select(h => edges[h / 2].SourceEdgeKey)
                    .Where(key => key != null)
                    .GroupBy(key => key)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

                if (bundleCounts.Count != 2 || bundleCounts.Any(pair => pair.Value != 2))
                    continue;

                foreach (int outgoing in outList)
                {
                    int incoming = Twin(outgoing);
                    int chosen = heNext[incoming];
                    if (chosen < 0)
                        continue;

                    var incomingEdge = edges[incoming / 2];
                    var chosenEdge = edges[chosen / 2];
                    if (incomingEdge.Atom != "vf-vf")
                        continue;
                    string key =
                        $"{incomingEdge.Atom}:{incomingEdge.SourceEdgeKey}->{chosenEdge.Atom}:{chosenEdge.SourceEdgeKey}";

                    groups.TryGetValue(key, out int count);
                    groups[key] = count + 1;
                }
            }

            if (groups.Count == 0)
                return;

            var summary = groups
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(16)
                .Select(pair => $"{pair.Value}x{pair.Key}");

            Debug.Log($"{HalfedgeOperatorVfTransitionDebugPrefix} transitions=[{string.Join(" | ", summary)}]");
        }

        private static void LogVfStarTransitionClasses(
            string operatorNotation,
            List<OEdge> edges,
            int[] heNext,
            Dictionary<OVertex, List<int>> orderedOutgoing)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var classes = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var (vert, outList) in orderedOutgoing)
            {
                if (vert.PointClass != "vf" || outList.Count != 4)
                    continue;

                var bundleCounts = outList
                    .Select(h => edges[h / 2].SourceEdgeKey)
                    .Where(key => key != null)
                    .GroupBy(key => key)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

                if (bundleCounts.Count != 2 || bundleCounts.Any(pair => pair.Value != 2))
                    continue;

                foreach (int outgoing in outList)
                {
                    int incoming = Twin(outgoing);
                    int chosen = heNext[incoming];
                    if (chosen < 0)
                        continue;

                    var incomingEdge = edges[incoming / 2];
                    if (incomingEdge.Atom != "vf-vf")
                        continue;

                    var chosenEdge = edges[chosen / 2];
                    string relation = string.Equals(incomingEdge.SourceEdgeKey, chosenEdge.SourceEdgeKey, StringComparison.Ordinal)
                        ? "same"
                        : "cross";
                    string key = $"vf-vf->{chosenEdge.Atom}:{relation}";

                    classes.TryGetValue(key, out int count);
                    classes[key] = count + 1;
                }
            }

            if (classes.Count == 0)
                return;

            var summary = classes
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Value}x{pair.Key}");

            Debug.Log($"{HalfedgeOperatorVfClassDebugPrefix} classes=[{string.Join(" | ", summary)}]");
        }

        private static void LogFeStarTransitionClasses(
            string operatorNotation,
            List<OEdge> edges,
            int[] heNext,
            Dictionary<OVertex, List<int>> orderedOutgoing)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var classes = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var (vert, outList) in orderedOutgoing)
            {
                if (vert.PointClass != "fe" && vert.PointClass != "fe!")
                    continue;

                foreach (int outgoing in outList)
                {
                    int incoming = Twin(outgoing);
                    int chosen = heNext[incoming];
                    if (chosen < 0)
                        continue;

                    var incomingEdge = edges[incoming / 2];
                    if (incomingEdge.Atom != "fe-vf")
                        continue;

                    var chosenEdge = edges[chosen / 2];
                    string relation = string.Equals(incomingEdge.SourceEdgeKey, chosenEdge.SourceEdgeKey, StringComparison.Ordinal)
                        ? "same"
                        : "cross";
                    string key = $"{vert.PointClass}:fe-vf->{chosenEdge.Atom}:{relation}";

                    classes.TryGetValue(key, out int count);
                    classes[key] = count + 1;
                }
            }

            if (classes.Count == 0)
                return;

            var summary = classes
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Value}x{pair.Key}");

            Debug.Log($"{HalfedgeOperatorFeTransitionDebugPrefix} classes=[{string.Join(" | ", summary)}]");
        }

        private static void LogVfIncomingFeClasses(
            string operatorNotation,
            List<OEdge> edges,
            int[] heNext,
            Dictionary<OVertex, List<int>> orderedOutgoing)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var classes = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var (vert, outList) in orderedOutgoing)
            {
                if (vert.PointClass != "vf" || outList.Count != 4)
                    continue;

                var bundleCounts = outList
                    .Select(h => edges[h / 2].SourceEdgeKey)
                    .Where(key => key != null)
                    .GroupBy(key => key)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

                if (bundleCounts.Count != 2 || bundleCounts.Any(pair => pair.Value != 2))
                    continue;

                foreach (int outgoing in outList)
                {
                    int incoming = Twin(outgoing);
                    int chosen = heNext[incoming];
                    if (chosen < 0)
                        continue;

                    var incomingEdge = edges[incoming / 2];
                    if (incomingEdge.Atom != "fe-vf")
                        continue;

                    var chosenEdge = edges[chosen / 2];
                    string relation = string.Equals(incomingEdge.SourceEdgeKey, chosenEdge.SourceEdgeKey, StringComparison.Ordinal)
                        ? "same"
                        : "cross";
                    string key = $"fe-vf->{chosenEdge.Atom}:{relation}";

                    classes.TryGetValue(key, out int count);
                    classes[key] = count + 1;
                }
            }

            if (classes.Count == 0)
                return;

            var summary = classes
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Value}x{pair.Key}");

            Debug.Log($"{HalfedgeOperatorVfIncomingFeDebugPrefix} classes=[{string.Join(" | ", summary)}]");
        }

        private static void LogEdgeTriangleCoverage(
            string operatorNotation,
            List<OEdge> edges,
            List<ReconstructedFaceInfo> faceInfos)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var allSourceEdges = edges
                .Select(edge => edge.SourceEdgeKey)
                .Where(key => !string.IsNullOrEmpty(key))
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            var triangleLoops = faceInfos
                .Where(faceInfo =>
                {
                    string pointClassSequence = CanonicalizePointClassSequence(faceInfo.Vertices);
                    return pointClassSequence == "fe|vf|vf" || pointClassSequence == "fe!|vf|vf";
                })
                .ToList();

            var triangleSourceEdges = triangleLoops
                .SelectMany(faceInfo => faceInfo.BoundarySourceEdges)
                .Where(key => !string.IsNullOrEmpty(key))
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            var triangleSummary = triangleLoops
                .GroupBy(faceInfo => CanonicalizePointClassSequence(faceInfo.Vertices), StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Count()}x{group.Key}");

            Debug.Log(
                $"{HalfedgeOperatorEdgeCoverageDebugPrefix} triangles=[{string.Join(" | ", triangleSummary)}] " +
                $"unique-triangle-source-edges={triangleSourceEdges.Count} total-source-edges={allSourceEdges.Count}");
        }

        private static void LogRejectedLoopSummary(
            string operatorNotation,
            List<string> keptLoopSignatures,
            List<string> rejectedLoopSignatures)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var keptSummary = keptLoopSignatures
                .GroupBy(signature => signature, StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Take(8)
                .Select(group => $"{group.Count()}x{group.Key}");

            var rejectedSummary = rejectedLoopSignatures
                .GroupBy(signature => signature, StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Take(8)
                .Select(group => $"{group.Count()}x{group.Key}");

            Debug.Log(
                $"{HalfedgeOperatorRejectDebugPrefix} kept=[{string.Join(" | ", keptSummary)}] " +
                $"rejected=[{string.Join(" | ", rejectedSummary)}]");
        }

        private static void LogFinalAcceptedFaceSummary(
            string operatorNotation,
            List<ReconstructedFaceInfo> extractedFaceInfos,
            PolyMesh result)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var extractedSummary = extractedFaceInfos
                .GroupBy(faceInfo => faceInfo.Vertices.Count)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => $"{group.Count()}x{group.Key}");

            var acceptedSummary = result.Faces
                .GroupBy(face => face.Sides)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => $"{group.Count()}x{group.Key}");

            Debug.Log(
                $"[HOP0_FINAL_20260416Q] extracted={extractedFaceInfos.Count} " +
                $"extractedSides=[{string.Join(" | ", extractedSummary)}] " +
                $"accepted={result.Faces.Count} acceptedSides=[{string.Join(" | ", acceptedSummary)}]");

            var geomSummary = result.Faces
                .Select((face, index) => new
                {
                    face.Sides,
                    Area = face.GetArea(),
                    Role = index < result.FaceRoles.Count ? result.FaceRoles[index] : Roles.New
                })
                .GroupBy(item => item.Sides)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    float minArea = group.Min(item => item.Area);
                    float maxArea = group.Max(item => item.Area);
                    float avgArea = group.Average(item => item.Area);
                    string roles = string.Join(",",
                        group.GroupBy(item => item.Role)
                            .OrderByDescending(roleGroup => roleGroup.Count())
                            .ThenBy(roleGroup => roleGroup.Key.ToString(), StringComparer.Ordinal)
                            .Select(roleGroup => $"{roleGroup.Key}x{roleGroup.Count()}"));
                    return $"{group.Key}s[min={minArea:F6},avg={avgArea:F6},max={maxArea:F6};roles={roles}]";
                });

            Debug.Log($"[HOP0_FINALGEOM_20260416T] {string.Join(" | ", geomSummary)}");
        }

        private static void LogTriangleGeometrySamples(
            string operatorNotation,
            List<ReconstructedFaceInfo> extractedFaceInfos,
            PolyMesh result)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            string FormatPoint(Vector3 point) => $"({point.x:F4},{point.y:F4},{point.z:F4})";

            float ExtractedArea(ReconstructedFaceInfo faceInfo)
            {
                if (faceInfo.Vertices.Count != 3)
                    return 0f;

                var a = faceInfo.Vertices[0].Position;
                var b = faceInfo.Vertices[1].Position;
                var c = faceInfo.Vertices[2].Position;
                return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            }

            var extractedSamples = extractedFaceInfos
                .Where(faceInfo => faceInfo.Vertices.Count == 3)
                .Take(3)
                .Select(faceInfo =>
                    $"{CanonicalizePointClassSequence(faceInfo.Vertices)}:area={ExtractedArea(faceInfo):F6}:" +
                    $"{string.Join(" | ", faceInfo.Vertices.Select(vertex => $"{vertex.PointClass}{FormatPoint(vertex.Position)}"))}");

            var acceptedSamples = result.Faces
                .Where(face => face.Sides == 3)
                .Take(3)
                .Select(face =>
                    $"area={face.GetArea():F6}:{string.Join(" | ", face.GetVertices().Select(vertex => FormatPoint(vertex.Position)))}");

            int extractedTriangleCount = extractedFaceInfos.Count(faceInfo => faceInfo.Vertices.Count == 3);
            int extractedZeroAreaTriangleCount = extractedFaceInfos.Count(faceInfo => faceInfo.Vertices.Count == 3 && ExtractedArea(faceInfo) <= 1e-6f);
            int acceptedTriangleCount = result.Faces.Count(face => face.Sides == 3);
            int acceptedZeroAreaTriangleCount = result.Faces.Count(face => face.Sides == 3 && face.GetArea() <= 1e-6f);

            Debug.Log(
                $"[HOP0_TRIGEOM_20260416U] extracted=[{string.Join(" || ", extractedSamples)}] " +
                $"accepted=[{string.Join(" || ", acceptedSamples)}]");
            Debug.Log(
                $"{HalfedgeOperatorFePlacementDebugPrefix} extractedTriangles={extractedTriangleCount} " +
                $"extractedZeroArea={extractedZeroAreaTriangleCount} acceptedTriangles={acceptedTriangleCount} " +
                $"acceptedZeroArea={acceptedZeroAreaTriangleCount}");
        }

        private static void LogConstructorRejectedFaces(
            string operatorNotation,
            List<Vector3> positions,
            List<ReconstructedFaceInfo> extractedFaceInfos,
            List<IEnumerable<int>> faceIdxs,
            List<Roles> faceRoles)
        {
            if (!string.Equals(operatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            var probe = new PolyMesh();
            foreach (var position in positions)
                probe.Vertices.Add(new Vertex(position));

            var rejected = new List<string>();
            var rejectedSamples = new List<string>();
            for (int i = 0; i < extractedFaceInfos.Count; i++)
            {
                var indices = faceIdxs[i].ToList();
                bool accepted = probe.Faces.Add(indices.Select(index => probe.Vertices[index]));

                if (!accepted)
                {
                    indices.Reverse();
                    accepted = probe.Faces.Add(indices.Select(index => probe.Vertices[index]));
                }

                if (accepted)
                {
                    probe.FaceRoles.Add(faceRoles[i]);
                    probe.FaceTags.Add(new HashSet<string>());
                    continue;
                }

                rejected.Add($"{CanonicalizePointClassSequence(extractedFaceInfos[i].Vertices)}:{extractedFaceInfos[i].Vertices.Count}");
                if (rejectedSamples.Count < 6)
                {
                    rejectedSamples.Add(
                        $"{CanonicalizePointClassSequence(extractedFaceInfos[i].Vertices)} => " +
                        $"{string.Join(" | ", extractedFaceInfos[i].BoundaryStepDescriptions)}");
                }
            }

            if (rejected.Count == 0)
                return;

            string summary = string.Join(" | ", rejected
                .GroupBy(item => item, StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Count()}x{group.Key}"));

            Debug.Log($"[HOP0_DROP_20260416R] rejected=[{summary}]");
            Debug.Log($"[HOP0_DROPSAMPLE_20260416S] samples=[{string.Join(" || ", rejectedSamples)}]");
        }

        // Sort outgoing halfedge indices CCW around vert using vert.Normal as the axis
        private static void SortCCW(List<int> outList, OVertex vert, OVertex[] heOrigin, List<OEdge> edges)
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

            var angleInfos = new List<(int Halfedge, float Angle, float Radius)>(outList.Count);
            foreach (var halfedge in outList)
            {
                var delta = heOrigin[Twin(halfedge)].Position - center;
                delta -= Vector3.Dot(delta, normal) * normal;
                float angle = Mathf.Atan2(Vector3.Dot(delta, perpDir), Vector3.Dot(delta, refDir));
                angleInfos.Add((halfedge, angle, delta.magnitude));
            }

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

            LogSortAmbiguity(vert, angleInfos, edges, heOrigin);
            LogDetailedOrder(vert, angleInfos, edges, heOrigin);
            LogFeVertexOrder(vert, angleInfos, edges, heOrigin);
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

        private static void LogSortAmbiguity(
            OVertex vert,
            List<(int Halfedge, float Angle, float Radius)> angleInfos,
            List<OEdge> edges,
            OVertex[] heOrigin)
        {
            if (!string.Equals(_activeHalfedgeOperatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;

            const float angleEpsilon = 1e-4f;
            var bundled = angleInfos
                .OrderBy(info => info.Angle)
                .ToList();

            var ambiguousGroups = new List<string>();
            int index = 0;
            while (index < bundled.Count)
            {
                int end = index + 1;
                while (end < bundled.Count && Mathf.Abs(bundled[end].Angle - bundled[index].Angle) <= angleEpsilon)
                    end++;

                if (end - index > 1)
                {
                    var groupText = string.Join(",",
                        bundled.GetRange(index, end - index)
                            .Select(info => $"{edges[info.Halfedge / 2].Atom}:{edges[info.Halfedge / 2].SourceEdgeKey}:{heOrigin[Twin(info.Halfedge)].PointClass}:{info.Radius:F4}"));
                    ambiguousGroups.Add(groupText);
                }

                index = end;
            }

            if (ambiguousGroups.Count == 0)
                return;

            Debug.Log($"{HalfedgeOperatorSortDebugPrefix} v={vert.Id} groups=[{string.Join(" | ", ambiguousGroups)}]");
        }

        private static void LogDetailedOrder(
            OVertex vert,
            List<(int Halfedge, float Angle, float Radius)> angleInfos,
            List<OEdge> edges,
            OVertex[] heOrigin)
        {
            if (!string.Equals(_activeHalfedgeOperatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;
            if (_remainingDetailedOrderLogs <= 0)
                return;

            const float angleEpsilon = 1e-4f;
            var ordered = angleInfos
                .OrderBy(info => info.Angle)
                .ThenBy(info => info.Radius)
                .ThenBy(info => info.Halfedge)
                .ToList();

            bool hasAmbiguity = false;
            for (int i = 1; i < ordered.Count; i++)
            {
                if (Mathf.Abs(ordered[i].Angle - ordered[i - 1].Angle) <= angleEpsilon)
                {
                    hasAmbiguity = true;
                    break;
                }
            }

            if (!hasAmbiguity)
                return;

            _remainingDetailedOrderLogs--;

            var orderText = string.Join(" | ", ordered.Select(info =>
                $"{edges[info.Halfedge / 2].Atom}:{edges[info.Halfedge / 2].SourceEdgeKey}:{heOrigin[Twin(info.Halfedge)].PointClass}:a={info.Angle:F4}:r={info.Radius:F4}:h={info.Halfedge}"));
            Debug.Log($"{HalfedgeOperatorOrderDebugPrefix} v={vert.Id} order=[{orderText}]");
        }

        private static void LogFeVertexOrder(
            OVertex vert,
            List<(int Halfedge, float Angle, float Radius)> angleInfos,
            List<OEdge> edges,
            OVertex[] heOrigin)
        {
            if (!string.Equals(_activeHalfedgeOperatorNotation, "vf-vf,fe-vf,fe-fe!", StringComparison.Ordinal))
                return;
            if (_remainingFeOrderLogs <= 0)
                return;
            if (vert.PointClass != "fe" && vert.PointClass != "fe!")
                return;

            _remainingFeOrderLogs--;

            var ordered = angleInfos
                .OrderBy(info => info.Angle)
                .ThenBy(info => info.Radius)
                .ThenBy(info => info.Halfedge)
                .ToList();

            var orderText = string.Join(" | ", ordered.Select(info =>
                $"{edges[info.Halfedge / 2].Atom}:{edges[info.Halfedge / 2].SourceEdgeKey}:{heOrigin[Twin(info.Halfedge)].PointClass}:a={info.Angle:F4}:r={info.Radius:F4}:h={info.Halfedge}"));
            Debug.Log($"{HalfedgeOperatorFeOrderDebugPrefix} v={vert.Id}:{vert.PointClass} order=[{orderText}]");
        }
    }
}
