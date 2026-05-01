using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

public enum EdgeExtractionMode
{
    AllEdges,    // one segment per edge — wireframe effect
    ChainedEdges // greedy chains through degree-2 vertices — longer continuous paths
}

[CreateAssetMenu(fileName = "EdgePolylineShapeSettings", menuName = "PolyhydraMulti/EdgePolylineShapeSettings", order = 1)]
public class EdgePolylineShapeSettings : BaseSettings
{
    [Header("Source")]
    public BaseSettings SourceSettings;
    public bool IncludeSource = false;

    [Header("Extraction")]
    public EdgeExtractionMode ExtractionMode = EdgeExtractionMode.AllEdges;

    [Header("Cross Section")]
    public PolylineSweepMode Mode = PolylineSweepMode.Tube;
    [Range(3, 32)] public int Sides = 4;
    [Min(0.001f)] public float Radius = 0.05f;
    public Vector3 UpHint = Vector3.up;
    public bool CapEnds = true;

    public override PolyMesh BuildBaseShape()
    {
        var sourcePoly = SourceSettings.BuildBaseShape();
        sourcePoly = SourceSettings.ApplyModifiers(sourcePoly);

        var finalPoly = IncludeSource ? sourcePoly.Duplicate() : new PolyMesh();

        var chains = ExtractionMode == EdgeExtractionMode.AllEdges
            ? ExtractAllEdges(sourcePoly)
            : ExtractChainedEdges(sourcePoly);

        foreach (var (points, closed) in chains)
        {
            if (points.Count < 2) continue;
            finalPoly.Append(PolylineSweep.Build(points, closed, Mode, Sides, Radius, UpHint, CapEnds));
        }

        finalPoly = ApplyModifiers(finalPoly);
        return finalPoly;
    }

    // -------------------------------------------------------------------------
    // All edges — one 2-point polyline per edge
    // -------------------------------------------------------------------------

    private static List<(List<Vector3>, bool)> ExtractAllEdges(PolyMesh poly)
    {
        var result = new List<(List<Vector3>, bool)>();
        var visited = new HashSet<(Guid, Guid)>();
        foreach (var he in poly.Halfedges)
        {
            var key = he.PairedName;
            if (key == null || !visited.Add(key.Value)) continue;
            result.Add((new List<Vector3> { he.Prev.Vertex.Position, he.Vertex.Position }, false));
        }
        return result;
    }

    // -------------------------------------------------------------------------
    // Chained edges — walks through degree-2 vertices to form longer paths.
    // Closed rings are detected and flagged so PolylineSweep seals them.
    // -------------------------------------------------------------------------

    private static List<(List<Vector3>, bool)> ExtractChainedEdges(PolyMesh poly)
    {
        var visited = new HashSet<(Guid, Guid)>();
        var edges   = new List<(Vertex a, Vertex b)>();
        var adj     = new Dictionary<Vertex, List<Vertex>>();

        foreach (var he in poly.Halfedges)
        {
            var key = he.PairedName;
            if (key == null || !visited.Add(key.Value)) continue;
            var a = he.Prev.Vertex;
            var b = he.Vertex;
            edges.Add((a, b));
            if (!adj.ContainsKey(a)) adj[a] = new List<Vertex>();
            if (!adj.ContainsKey(b)) adj[b] = new List<Vertex>();
            adj[a].Add(b);
            adj[b].Add(a);
        }

        var usedEdges = new HashSet<(Vertex, Vertex)>();
        var chains    = new List<(List<Vector3>, bool)>();

        // Process junction/endpoint edges first so chains start cleanly at corners
        var sorted = edges.OrderBy(e =>
            adj[e.a].Count == 2 && adj[e.b].Count == 2 ? 1 : 0);

        foreach (var (a, b) in sorted)
        {
            if (usedEdges.Contains((a, b))) continue;
            usedEdges.Add((a, b));
            usedEdges.Add((b, a));

            var fwd = Grow(b, a, adj, usedEdges);
            var bwd = Grow(a, b, adj, usedEdges);
            bwd.Reverse();

            var chain = new List<Vertex>(bwd);
            chain.Add(a);
            chain.Add(b);
            chain.AddRange(fwd);

            // Detect closed ring: last vertex walked back to the first
            bool isClosed = chain.Count > 2 && ReferenceEquals(chain[0], chain[chain.Count - 1]);
            if (isClosed) chain.RemoveAt(chain.Count - 1);

            chains.Add((chain.Select(v => v.Position).ToList(), isClosed));
        }

        return chains;
    }

    private static List<Vertex> Grow(
        Vertex current, Vertex prev,
        Dictionary<Vertex, List<Vertex>> adj,
        HashSet<(Vertex, Vertex)> used)
    {
        var result = new List<Vertex>();
        while (true)
        {
            if (!adj.TryGetValue(current, out var neighbors) || neighbors.Count != 2) break;

            Vertex next = null;
            foreach (var n in neighbors)
            {
                if (ReferenceEquals(n, prev) || used.Contains((current, n))) continue;
                next = n;
                break;
            }
            if (next == null) break;

            used.Add((current, next));
            used.Add((next, current));
            result.Add(next);
            prev    = current;
            current = next;
        }
        return result;
    }

    public override Mesh BuildAll(AppearanceSettings appearanceSettings)
    {
        var finalPoly = BuildBaseShape();
        var meshData = finalPoly.BuildMeshData(
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
