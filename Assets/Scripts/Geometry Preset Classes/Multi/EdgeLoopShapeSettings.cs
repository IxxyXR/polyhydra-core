using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

public enum EdgeLoopMode
{
    VertexWalk,   // follow edges, pick straightest exit at each vertex
    FaceCrossing  // jump across each face via opposite edge midpoints
}

[CreateAssetMenu(fileName = "EdgeLoopShapeSettings", menuName = "PolyhydraMulti/EdgeLoopShapeSettings", order = 1)]
public class EdgeLoopShapeSettings : BaseSettings
{
    [Header("Source")]
    public BaseSettings SourceSettings;
    public bool IncludeSource = false;

    [Header("Loop Tracing")]
    public EdgeLoopMode LoopMode = EdgeLoopMode.VertexWalk;

    [Tooltip("VertexWalk: minimum dot product between incoming and outgoing edge directions " +
             "(0 = allow 90° turns, -1 = greedy). " +
             "FaceCrossing: minimum dot product of the 'opposite' direction check across a face.")]
    [Range(-1f, 1f)] public float MinContinuationDot = -0.1f;

    [Tooltip("FaceCrossing only: when two exit edges are equally good, prefer clockwise turns.")]
    public bool PreferClockwise = true;

    [Tooltip("Discard loops shorter than this many edges.")]
    [Min(1)] public int MinLoopEdges = 3;

    [Header("Cross Section")]
    public PolylineSweepMode Mode = PolylineSweepMode.Tube;
    [Range(3, 32)] public int Sides = 4;
    [Min(0.001f)] public float Radius = 0.05f;
    public Vector3 UpHint = Vector3.up;
    public bool CapEnds = false;

    public override PolyMesh BuildBaseShape()
    {
        var sourcePoly = SourceSettings.BuildBaseShape();
        sourcePoly = SourceSettings.ApplyModifiers(sourcePoly);

        var finalPoly = IncludeSource ? sourcePoly.Duplicate() : new PolyMesh();

        var loops = LoopMode == EdgeLoopMode.VertexWalk
            ? ExtractVertexWalkLoops(sourcePoly)
            : ExtractFaceCrossingLoops(sourcePoly);

        foreach (var (points, closed) in loops)
        {
            if (points.Count < MinLoopEdges) continue;
            finalPoly.Append(PolylineSweep.Build(points, closed, Mode, Sides, Radius, UpHint, CapEnds));
        }

        finalPoly = ApplyModifiers(finalPoly);
        return finalPoly;
    }

    // -------------------------------------------------------------------------
    // VertexWalk — follow edges, pick straightest continuation at each vertex.
    //
    // Algorithm:
    //  1. Pre-compute nextMap: for every directed edge (u→v), which vertex w
    //     is the straightest continuation? This is deterministic and only done once.
    //  2. A directed state (u,v) is a "chain start" if nothing leads into it —
    //     i.e. no other (w→u) has next = v.  Trace all chain starts first.
    //  3. Any remaining unvisited states are part of closed cycles; trace those.
    //
    // Each directed state is visited exactly once, so no edge is emitted twice.
    // -------------------------------------------------------------------------

    private List<(List<Vector3> points, bool closed)> ExtractVertexWalkLoops(PolyMesh poly)
    {
        // Step 1: build the deterministic "next" map
        var nextMap = new Dictionary<(Guid, Guid), Vertex>();
        foreach (var he in poly.Halfedges)
        {
            var u = he.Prev.Vertex;
            var v = he.Vertex;
            var w = FindStraightest(v, u, (v.Position - u.Position).normalized);
            if (w != null)
                nextMap[(u.Name, v.Name)] = w;
        }

        // Step 2: find which directed states have a predecessor
        var hasPred = new HashSet<(Guid, Guid)>();
        foreach (var kvp in nextMap)
            hasPred.Add((kvp.Key.Item2, kvp.Value.Name)); // (v,w) has pred u

        var visited = new HashSet<(Guid, Guid)>();
        var result  = new List<(List<Vector3>, bool)>();

        // Pass 1: open-chain starts (no predecessor → never a mid-point)
        foreach (var he in poly.Halfedges)
        {
            var u = he.Prev.Vertex;
            var v = he.Vertex;
            if (hasPred.Contains((u.Name, v.Name))) continue;
            if (visited.Contains((u.Name, v.Name))) continue;
            result.Add(TraceFromState(u, v, nextMap, visited));
        }

        // Pass 2: remaining unvisited states are closed cycles
        foreach (var he in poly.Halfedges)
        {
            var u = he.Prev.Vertex;
            var v = he.Vertex;
            if (visited.Contains((u.Name, v.Name))) continue;
            result.Add(TraceFromState(u, v, nextMap, visited));
        }

        return result;
    }

    private static (List<Vector3> points, bool closed) TraceFromState(
        Vertex startU, Vertex startV,
        Dictionary<(Guid, Guid), Vertex> nextMap,
        HashSet<(Guid, Guid)> visited)
    {
        var verts = new List<Vertex> { startU, startV };
        visited.Add((startU.Name, startV.Name));

        var u = startU;
        var v = startV;

        while (nextMap.TryGetValue((u.Name, v.Name), out var w))
        {
            var nextState = (v.Name, w.Name);
            if (visited.Contains(nextState)) break; // merges into already-traced chain

            if (ReferenceEquals(w, startU))
            {
                // Closed back to the trace's first vertex
                visited.Add(nextState);
                return (verts.Select(x => x.Position).ToList(), true);
            }

            visited.Add(nextState);
            verts.Add(w);
            u = v;
            v = w;
        }

        return (verts.Select(x => x.Position).ToList(), false);
    }

    private Vertex FindStraightest(Vertex current, Vertex cameFrom, Vector3 inDir)
    {
        Vertex best    = null;
        float  bestDot = MinContinuationDot - float.Epsilon;

        foreach (var neighbor in current.Neighbours)
        {
            if (ReferenceEquals(neighbor, cameFrom)) continue;

            var   outDir = (neighbor.Position - current.Position).normalized;
            float dot    = Vector3.Dot(inDir, outDir);

            if (dot > bestDot)
            {
                bestDot = dot;
                best    = neighbor;
            }
        }

        return best;
    }

    // -------------------------------------------------------------------------
    // FaceCrossing — jump across each face via the most-opposite edge midpoint
    // -------------------------------------------------------------------------

    private List<(List<Vector3> points, bool closed)> ExtractFaceCrossingLoops(PolyMesh poly)
    {
        var seenLoops = new HashSet<string>();
        var result    = new List<(List<Vector3>, bool)>();

        foreach (var startHe in poly.Halfedges)
        {
            if (startHe.Pair == null) continue;

            var (loop, closed) = TraceFaceCrossingLoop(startHe, poly.Halfedges.Count);
            if (loop.Count < 2) continue;

            var key = string.Join(",",
                loop.Select(he => he.PairedName?.ToString() ?? he.GetHashCode().ToString())
                    .OrderBy(s => s));

            if (!seenLoops.Add(key)) continue;

            result.Add((loop.Select(he => he.Midpoint).ToList(), closed));
        }

        return result;
    }

    private (List<Halfedge> loop, bool closed) TraceFaceCrossingLoop(Halfedge start, int maxSteps)
    {
        var loop = new List<Halfedge>();
        var he   = start;

        for (int step = 0; step < maxSteps; step++)
        {
            loop.Add(he);

            var opposite = FindOpposite(he);
            if (opposite == null) return (loop, false);

            var crossed = opposite.Pair;
            if (crossed == null) return (loop, false);

            if (ReferenceEquals(crossed, start)) return (loop, true);

            he = crossed;
        }

        return (loop, false);
    }

    private Halfedge FindOpposite(Halfedge he)
    {
        var centroid   = he.Face.Centroid;
        var toCurrent  = (he.Midpoint - centroid).normalized;
        var faceNormal = he.Face.Normal;

        Halfedge best          = null;
        float    bestPrimary   = MinContinuationDot - float.Epsilon;
        float    bestSecondary = PreferClockwise ? float.MaxValue : float.MinValue;

        foreach (var candidate in he.Face.GetHalfedges())
        {
            if (ReferenceEquals(candidate, he)) continue;

            var   toCandidate = (candidate.Midpoint - centroid).normalized;
            float primary     = Vector3.Dot(-toCurrent, toCandidate);

            if (primary < MinContinuationDot) continue;

            float secondary = Vector3.Dot(Vector3.Cross(toCurrent, toCandidate), faceNormal);

            bool betterPrimary   = primary > bestPrimary + 0.001f;
            bool equalPrimary    = !betterPrimary && primary >= bestPrimary - 0.001f;
            bool betterSecondary = PreferClockwise ? secondary < bestSecondary
                                                   : secondary > bestSecondary;

            if (betterPrimary || (equalPrimary && betterSecondary))
            {
                bestPrimary   = primary;
                bestSecondary = secondary;
                best          = candidate;
            }
        }

        return best;
    }

    public override Mesh BuildAll(AppearanceSettings appearanceSettings)
    {
        var finalPoly = BuildBaseShape();
        var meshData  = finalPoly.BuildMeshData(
            colorMethod: GetColorMethod(appearanceSettings),
            colors: CalculateColorList(appearanceSettings)
        );
        return finalPoly.BuildUnityMesh(meshData);
    }

    public override void AttachAction(Action settingsChanged, PolyhydraGenerator generator)
    {
        OnSettingsChanged += settingsChanged;
        SourceSettings.AttachAction(settingsChanged, generator);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        SourceSettings.DetachAction(settingsChanged);
    }
}
