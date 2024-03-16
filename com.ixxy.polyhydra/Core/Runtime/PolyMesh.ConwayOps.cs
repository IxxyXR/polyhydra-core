using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {

        public PolyMesh Dual()
        {
            var newFaceTags = new List<HashSet<string>>();
            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            // Create vertices from faces
            var vertexPoints = new List<Vector3>(Faces.Count);
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var f = Faces[faceIndex];
                vertexPoints.Add(f.Centroid);
                vertexRoles.Add(Roles.New);
            }

            // Create sublist of non-boundary vertices
            var naked = new Dictionary<Guid, bool>(Vertices.Count); // vertices (name, boundary?)
            // boundary halfedges (name, index of point in new mesh)
            var hlookup = new Dictionary<(Guid, Guid)?, int>(Halfedges.Count);

            for (var i = 0; i < Halfedges.Count; i++)
            {
                var he = Halfedges[i];
                if (!naked.ContainsKey(he.Vertex.Name))
                {
                    // if not in dict, add (boundary == true)
                    naked.Add(he.Vertex.Name, he.Pair == null);
                }
                else if (he.Pair == null)
                {
                    // if in dict and belongs to boundary halfedge, set true
                    naked[he.Vertex.Name] = true;
                }

                if (he.Pair == null)
                {
                    // if boundary halfedge, add mid-point to vertices and add to lookup
                    hlookup.Add(he.Name, vertexPoints.Count);
                    vertexPoints.Add(he.Midpoint);
                    vertexRoles.Add(Roles.New);
                }
            }

            // List new faces by their vertex indices
            // (i.e. old vertices by their face indices)
            var flookup = new Dictionary<string, int>();

            for (int i = 0; i < Faces.Count; i++)
            {
                flookup.Add(Faces[i].Name, i);
            }

            var faceIndices = new List<List<int>>(Vertices.Count);

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                var vertex = Vertices[vertexIndex];
                var fIndex = new List<int>();

                var vertexFaces = vertex.GetVertexFaces();
                if (vertexFaces.Count < 3) continue;
                for (var faceIndex = 0; faceIndex < vertexFaces.Count; faceIndex++)
                {
                    Face f = vertexFaces[faceIndex];
                    if (flookup.ContainsKey(f.Name))
                    {
                        fIndex.Add(flookup[f.Name]);
                    }
                }

                if (naked.ContainsKey(vertex.Name) && naked[vertex.Name])
                {
                    // Handle boundary vertices...
                    var h = vertex.Halfedges;
                    if (h.Count > 0)
                    {
                        // Add points on naked edges and the naked vertex
                        fIndex.Add(hlookup[h.Last().Name]);
                        fIndex.Add(vertexPoints.Count);
                        fIndex.Add(hlookup[h.First().Next.Name]);
                        vertexPoints.Add(vertex.Position);
                        vertexRoles.Add(Roles.New);
                    }
                }

                if (fIndex.Count >= 3)
                {
                    faceIndices.Add(fIndex);
                    try
                    {
                        faceRoles.Add(VertexRoles[vertexIndex]);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(
                            $"Dual op failed to set face role based on existing vertex role. " +
                            $"Faces.Count: {Faces.Count} Verts: {Vertices.Count} " +
                            $"old VertexRoles.Count: {VertexRoles.Count} i: {vertexIndex}. Error: {e.Message}");
                    }
                    var vertexFaceIndices = vertexFaces.Select(f => Faces.IndexOf(f));
                    var existingTagSets =
                        vertexFaceIndices.Select(fi => FaceTags[fi]);
                    var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                    {
                        rs.UnionWith(i);
                        return rs;
                    });
                    newFaceTags.Add(newFaceTagSet);
                }
            }

            // If we're ended up with an invalid number of roles then just set them all to 'New'
            if (faceRoles.Count != faceIndices.Count)
                faceRoles = Enumerable.Repeat(Roles.New, faceIndices.Count).ToList();
            if (vertexRoles.Count != vertexPoints.Count)
                vertexRoles = Enumerable.Repeat(Roles.New, vertexPoints.Count).ToList();

            return new PolyMesh(vertexPoints, faceIndices.ToArray(), faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh AddDual(float scale)
        {
            var oldPoly = Duplicate();
            var newPoly = Dual();
            oldPoly.ScalePolyhedra();
            newPoly.ScalePolyhedra(scale);
            oldPoly.Append(newPoly);
            return oldPoly;
        }

        public PolyMesh Zip(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            // Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
            var vertexPoints = new List<Vector3>(); // vertices as points
            var newInnerVerts = new Dictionary<(Guid, Guid)?, int>();
            int count = 0;

            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            // faces to faces
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var prevFaceTagSet = FaceTags[faceIndex];
                var centroid = face.Centroid;
                foreach (var edge in face.GetHalfedges())
                {
                    vertexPoints.Add(Vector3.Lerp(edge.Midpoint, centroid, o.GetValueA(this, faceIndex)));
                    vertexRoles.Add(Roles.New);
                    newInnerVerts.Add(edge.Name, count++);
                }

                faceIndices.Add(face.GetHalfedges().Select(edge => newInnerVerts[edge.Name]));
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            // vertices to faces
            for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
            {
                var vertex = Vertices[vertIndex];
                var halfedges = vertex.Halfedges;
                if (halfedges.Count < 3) continue;
                var newVertexFace = new List<int>();
                foreach (var edge in halfedges)
                {
                    newVertexFace.Add(newInnerVerts[edge.Name]);
                    if (edge.Pair != null)
                    {
                        newVertexFace.Add(newInnerVerts[edge.Pair.Name]);
                    }
                    else
                    {
                        vertexPoints.Add(edge.Midpoint);
                        vertexRoles.Add(Roles.NewAlt);
                        newVertexFace.Add(vertexPoints.Count - 1);
                    }
                }

                faceIndices.Add(newVertexFace);
                faceRoles.Add(Roles.New);

                var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
                var existingTagSets =
                    vertexFaceIndices.Select(fi => FaceTags[fi]);
                var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                {
                    rs.UnionWith(i);
                    return rs;
                });
                newFaceTags.Add(newFaceTagSet);
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        /// <summary>
        /// Conway's ambo operator
        /// </summary>
        /// <returns>the ambo as a new mesh</returns>
        public PolyMesh AlternatingAmbo()
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            // Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
            var vertexPoints = new List<Vector3>(); // vertices as points
            var hlookup = new Dictionary<(Guid, Guid)?, int>();
            int count = 0;

            foreach (var edge in Halfedges)
            {
                // if halfedge's pair is already in the table, give it the same index
                if (edge.Pair != null && hlookup.ContainsKey(edge.Pair.Name))
                {
                    hlookup.Add(edge.Name, hlookup[edge.Pair.Name]);
                }
                else
                {
                    // otherwise create a new vertex and increment the index
                    hlookup.Add(edge.Name, count++);
                    vertexPoints.Add(edge.Midpoint);
                    vertexRoles.Add(Roles.New);
                }
            }

            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            // faces to faces
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                faceIndices.Add(face.GetHalfedges().Select(edge => hlookup[edge.Name]));
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            // vertices to faces
            for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
            {
                var vertex = Vertices[vertIndex];
                var halfedges = vertex.Halfedges;
                if (halfedges.Count == 0) continue; // no halfedges (naked vertex, ignore)
                var newHalfedges = halfedges.Select(edge => hlookup[edge.Name]); // halfedge indices for vertex-loop
                if (halfedges[0].Next.Pair == null)
                {
                    // Handle boundary vertex, add itself and missing boundary halfedge
                    newHalfedges = newHalfedges.Concat(new[] {vertexPoints.Count, hlookup[halfedges[0].Next.Name]});
                    vertexPoints.Add(vertex.Position);
                    vertexRoles.Add(Roles.NewAlt);
                }

                if (newHalfedges.Count() >= 3)
                {
                    faceIndices.Add(newHalfedges);
                    faceRoles.Add(Roles.New);
                    var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f)).Where(x=>x!=-1);  // No idea why IndexOf is failing to match
                    var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi]);
                    var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                    {
                        rs.UnionWith(i);
                        return rs;
                    });
                    newFaceTags.Add(newFaceTagSet);
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

                public PolyMesh Ambo()
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            // Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
            var vertexPoints = new List<Vector3>(); // vertices as points
            var hlookup = new Dictionary<(Guid, Guid)?, int>();
            int count = 0;

            foreach (var edge in Halfedges)
            {
                // if halfedge's pair is already in the table, give it the same index
                if (edge.Pair != null && hlookup.ContainsKey(edge.Pair.Name))
                {
                    hlookup.Add(edge.Name, hlookup[edge.Pair.Name]);
                }
                else
                {
                    // otherwise create a new vertex and increment the index
                    hlookup.Add(edge.Name, count++);
                    vertexPoints.Add(edge.Midpoint);
                    vertexRoles.Add(Roles.New);
                }
            }

            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            // faces to faces
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                faceIndices.Add(face.GetHalfedges().Select(edge => hlookup[edge.Name]));
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            // vertices to faces
            for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
            {
                var vertex = Vertices[vertIndex];
                var halfedges = vertex.Halfedges;
                if (halfedges.Count == 0) continue; // no halfedges (naked vertex, ignore)
                var newHalfedges = halfedges.Select(edge => hlookup[edge.Name]); // halfedge indices for vertex-loop
                if (halfedges[0].Next.Pair == null)
                {
                    // Handle boundary vertex, add itself and missing boundary halfedge
                    newHalfedges = newHalfedges.Concat(new[] {vertexPoints.Count, hlookup[halfedges[0].Next.Name]});
                    vertexPoints.Add(vertex.Position);
                    vertexRoles.Add(Roles.NewAlt);
                }

                if (newHalfedges.Count() >= 3)
                {
                    faceIndices.Add(newHalfedges);
                    faceRoles.Add(Roles.New);
                    var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f)).Where(x=>x!=-1);  // No idea why IndexOf is failing to match
                    var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi]);
                    var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                    {
                        rs.UnionWith(i);
                        return rs;
                    });
                    newFaceTags.Add(newFaceTagSet);
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Girih(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            // Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
            var edgeVertexPoints = new List<Vector3>(); // vertices as points
            var midpointVertexPoints = new List<Vector3>(); // vertices as points
            var edgeIndices = new Dictionary<(Guid, Guid)?, int>();
            var midpointIndices = new Dictionary<(Guid, Guid)?, int>();
            int count = 0;

            foreach (var edge in Halfedges)
            {
                // if halfedge's pair is already in the table, give it the same index
                if (edge.Pair != null && edgeIndices.ContainsKey(edge.Pair.Name))
                {
                    edgeIndices.Add(edge.Name, edgeIndices[edge.Pair.Name]);
                }
                else
                {
                    // otherwise create a new vertex and increment the index
                    edgeIndices.Add(edge.Name, count++);
                    edgeVertexPoints.Add(edge.Midpoint);
                    vertexRoles.Add(Roles.New);
                }
            }

            for (var i = 0; i < Halfedges.Count; i++)
            {
                var edge = Halfedges[i];
                float param1 = o.GetValueA(this, Faces.IndexOf(edge.Face));

                midpointIndices.Add(edge.Name, edgeVertexPoints.Count + i);
                var pos = Vector3.Lerp(edge.Midpoint, edge.Next.Midpoint, 0.5f);
                midpointVertexPoints.Add(Vector3.LerpUnclamped(pos, edge.Vertex.Position, param1));
                vertexRoles.Add(Roles.New);
            }

            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            // faces to faces
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                var thisFaceIndices = face.GetHalfedges().Select(edge => edgeIndices[edge.Name]);
                var thisMidpointIndices = face.GetHalfedges().Select(edge => midpointIndices[edge.Name]);
                faceIndices.Add(thisFaceIndices.Zip(thisMidpointIndices, (a, b) => new[] { a, b })
                    .SelectMany(pair => pair).ToList());
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            // vertices to faces
            for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
            {
                var vertex = Vertices[vertIndex];
                var halfedges = vertex.Halfedges;
                if (halfedges.Count == 0) continue; // no halfedges (naked vertex, ignore)
                var edgeHalfedges = halfedges.Select(edge => edgeIndices[edge.Name]);
                var midpointHalfedges = halfedges.Select(edge => midpointIndices[edge.Name]);
            // if (halfedges[0].Next.Pair == null)
            // {
            //     // Handle boundary vertex, add itself and missing boundary halfedge
            //     edgeHalfedges = edgeHalfedges.Concat(new[]
            //         { edgeVertexPoints.Count, edgeIndices[halfedges[0].Next.Name] });
            //     edgeVertexPoints.Add(vertex.Position);
            //     vertexRoles.Add(Roles.NewAlt);
            // }

                if (edgeHalfedges.Count() >= 3)
                {
                    var newHalfEdges = midpointHalfedges
                        .Zip(edgeHalfedges, (a, b) => new[] { a, b })
                        .SelectMany(pair => pair)
                        .ToList();
                    faceIndices.Add(newHalfEdges);
                    faceRoles.Add(Roles.New);
                    var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f)).Where(x=>x!=-1);  // No idea why IndexOf is failing to match
                    var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi]);
                    var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                    {
                        rs.UnionWith(i);
                        return rs;
                    });
                    newFaceTags.Add(newFaceTagSet);
                }
            }

            return new PolyMesh(edgeVertexPoints.Concat(midpointVertexPoints), faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Truncate(OpParams o)
        {
            int GetVertID(Vertex v)
            {
                return Vertices.FindIndex(a => a == v);
            }

            var newFaceTags = new List<HashSet<string>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            var vertexPoints = new List<Vector3>(); // vertices as points
            var ignoredVerts = new Dictionary<Guid, int>();
            var newVerts = new Dictionary<Vector3, int>();

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                float amount = o.GetValueA(this, vertexIndex);
                var v = Vertices[vertexIndex];
                foreach (var edge in v.Halfedges)
                {
                    Vector3 pos = edge.PointAlongEdge(amount);
                    vertexPoints.Add(pos);
                    vertexRoles.Add(Roles.New);
                    newVerts[pos] = vertexPoints.Count - 1;

                    if (edge.Pair == null)
                    {
                        Vector3 pos2 = edge.PointAlongEdge(1 - amount);
                        vertexPoints.Add(pos2);
                        vertexRoles.Add(Roles.New);
                        newVerts[pos2] = vertexPoints.Count - 1;
                    }
                }
                vertexPoints.Add(v.Position);
                vertexRoles.Add(Roles.Ignored);
                ignoredVerts[v.Name] = vertexPoints.Count - 1;
            }

            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices

            // faces to faces
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float amount = o.GetValueA(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                var centerFace = new List<int>();
                var faceEdges = face.GetHalfedges();
                for (var i = 0; i < faceEdges.Count; i++)
                {
                    var edge = faceEdges[i];
                    Vector3 pos1 = edge.PointAlongEdge(amount);
                    var nextEdge = faceEdges[ActualMod((i + 1), faceEdges.Count)].Pair;
                    Vector3 pos2;
                    if (nextEdge != null)
                    {
                        pos2 = nextEdge.PointAlongEdge(amount);
                    }
                    else
                    {
                        pos2 = faceEdges[ActualMod((i + 1), faceEdges.Count)].PointAlongEdge(1 - amount);
                    }

                    if (IncludeVertex(GetVertID(edge.Vertex), o.filter))
                    {
                        try {centerFace.Add(newVerts[pos1]);} catch (Exception e){}
                        try {centerFace.Add(newVerts[pos2]);} catch (Exception e){}
                    }
                    else
                    {
                        centerFace.Add(ignoredVerts[edge.Vertex.Name]);
                    }
                }

                if (centerFace.Count >= 3)
                {
                    faceIndices.Add(centerFace);
                    faceRoles.Add(Roles.Existing);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            // vertices to faces
            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                float amount = o.GetValueA(this, vertexIndex);

                var vertex = Vertices[vertexIndex];
                if (!IncludeVertex(GetVertID(vertex), o.filter)) continue;
                bool boundary = false;
                var edges = vertex.Halfedges;

                var vertexFace = new List<int>();
                for (var i = 0; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    Vector3 pos;
                    if (edge.Pair != null)
                    {
                        pos = edge.PointAlongEdge(amount);
                    }
                    else
                    {
                        // It's a reverse edge
                        boundary = true;
                        break;
                        // pos = edge.PointAlongEdge(1 - amount);
                    }

                    vertexFace.Add(newVerts[pos]);
                }

                if (vertexFace.Count >= 3 && !boundary)
                {
                    faceIndices.Add(vertexFace);
                    faceRoles.Add(Roles.New);

                    var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
                    var existingTagSets =
                        vertexFaceIndices.Select(fi => FaceTags[fi]);
                    var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                    {
                        rs.UnionWith(i);
                        return rs;
                    });
                    newFaceTags.Add(newFaceTagSet);
                }
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            return poly;
        }

        public PolyMesh Bevel(OpParams o)
        {
            return Bevel(o, false);
        }

        public PolyMesh Bevel(OpParams o, bool useExtraParam, float extraParam = 0)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            var newVertexPoints = new List<Vector3>();
            var newInnerVertsL = new Dictionary<(Guid, Guid)?, int>();
            var newInnerVertsR = new Dictionary<(Guid, Guid)?, int>();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var centroid = face.Centroid;
                float offset = o.GetValueB(this, faceIndex);

                foreach (var edge in face.GetHalfedges())
                {
                    float amountP, amountQ;

                    amountP = o.GetValueA(this, faceIndex);
                    amountQ = useExtraParam ? extraParam : amountP;

                    var edgePointL = edge.PointAlongEdge(amountP);
                    var innerPointL = Vector3.LerpUnclamped(edgePointL, centroid, amountQ);
                    innerPointL += face.Normal * offset;
                    newVertexPoints.Add(innerPointL);
                    vertexRoles.Add(Roles.New);
                    newInnerVertsL.Add(edge.Name, newVertexPoints.Count - 1);

                    var edgePointR = edge.PointAlongEdge(1 - amountP);
                    var innerPointR = Vector3.LerpUnclamped(edgePointR, centroid, amountQ);
                    innerPointR += face.Normal * offset;
                    newVertexPoints.Add(innerPointR);
                    vertexRoles.Add(Roles.New);
                    newInnerVertsR.Add(edge.Name, newVertexPoints.Count - 1);
                }
            }

            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices

            // faces to faces
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                var newFace = new List<int>();
                foreach (var edge in face.GetHalfedges())
                {
                    newFace.Add(newInnerVertsR[edge.Name]);
                    newFace.Add(newInnerVertsL[edge.Name]);
                }

                faceIndices.Add(newFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
            {
                bool skip = false;
                var edges = Vertices[vertIndex].Halfedges;
                var list = new List<int>();
                foreach (var edge in edges)
                {
                    if (edge.Pair == null)
                    {
                        skip = true;
                        break;
                    }

                    list.Add(newInnerVertsL[edge.Name]);
                    list.Add(newInnerVertsR[edge.Pair.Name]);
                }

                if (skip) continue;
                // list.Reverse();
                faceIndices.Add(list);
                faceRoles.Add(Roles.New);

                var vertexFaceIndices = Vertices[vertIndex].GetVertexFaces().Select(f => Faces.IndexOf(f));
                var existingTagSets =
                    vertexFaceIndices.Select(fi => FaceTags[fi]);
                var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                {
                    rs.UnionWith(i);
                    return rs;
                });
                newFaceTags.Add(newFaceTagSet);
            }

            var edgeFlags = new HashSet<(Guid, Guid)?>();
            foreach (var edge in Halfedges)
            {
                if (edge.Pair == null) continue;
                if (edgeFlags.Contains(edge.PairedName)) continue;
                var list = new List<int>
                {
                    newInnerVertsL[edge.Name],
                    newInnerVertsR[edge.Name],
                    newInnerVertsL[edge.Pair.Name],
                    newInnerVertsR[edge.Pair.Name],
                };
                faceIndices.Add(list);
                faceRoles.Add(Roles.NewAlt);
                edgeFlags.Add(edge.PairedName);

                var newFaceTagSet = new HashSet<string>();
                newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                newFaceTagSet.UnionWith(
                    FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                newFaceTags.Add(newFaceTagSet);
            }

            return new PolyMesh(newVertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Ortho(OpParams o, bool catmullClarke = false)
        {

            Vector3 calcEdgePos(Halfedge e)
            {
                Vector3 pos;
                float count = 0;

                pos = e.Midpoint;
                count++;

                pos += e.Face.Centroid;
                count++;

                if (e.Pair != null)
                {
                    pos += e.Pair.Face.Centroid;
                    count++;
                }

                pos /= count;
                return pos;
            }

            var newFaceTags = new List<HashSet<string>>();

            var existingVerts = new Dictionary<string, int>();
            var newEdgeVertsLookup = new Dictionary<string, int>();
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            // Loop through old faces
            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var prevFaceTagSet = FaceTags[faceIndex];
                var oldFace = Faces[faceIndex];

                float param1 = o.GetValueA(this, faceIndex);

                Vector3 facePos;
                if (catmullClarke)
                {
                    // Catmull-Clarke style
                    facePos = oldFace.Centroid;
                }
                else
                {
                    // Explicit offset
                    facePos = oldFace.Centroid + oldFace.Normal * param1;
                }

                vertexPoints.Add(facePos);
                vertexRoles.Add(Roles.New);
                int centroidIndex = vertexPoints.Count - 1;

                // Loop through each vertex on old face and create a new face for each
                for (int j = 0; j < oldFace.GetHalfedges().Count; j++)
                {
                    int seedVertexIndex;
                    int edgeIndex;
                    int prevEdgeIndex;

                    string keyName;

                    var thisFaceIndices = new List<int>();
                    var edges = oldFace.GetHalfedges();

                    var seedVertex = edges[j].Vertex;
                    keyName = seedVertex.Name.ToString();
                    if (existingVerts.ContainsKey(keyName))
                    {
                        seedVertexIndex = existingVerts[keyName];
                    }
                    else
                    {
                        Vector3 seedVertexPos;
                        if (catmullClarke)
                        {
                            // Catmull-Clarke style

                            var Q = Vector3.zero;
                            var sharedFaces = seedVertex.GetVertexFaces();

                            foreach (var face in sharedFaces)
                            {
                                Q += face.Centroid;
                            }
                            Q /= (float)sharedFaces.Count;

                            var R = Vector3.zero;
                            var sharedEdges = seedVertex.Halfedges;
                            foreach (var edge in sharedEdges)
                            {
                                R += edge.Midpoint;
                            }
                            R /= sharedEdges.Count;

                            float n = sharedFaces.Count;

                            seedVertexPos = (Q + (R * 2) + (seedVertex.Position * (n - 3)))/n;
                            seedVertexPos = Vector3.LerpUnclamped(seedVertex.Position, seedVertexPos, param1);
                        }
                        else
                        {
                            // Explicit offset
                            seedVertexPos = seedVertex.Position - seedVertex.Normal * param1;
                        }
                        vertexPoints.Add(seedVertexPos);
                        vertexRoles.Add(Roles.Existing);
                        seedVertexIndex = vertexPoints.Count - 1;
                        existingVerts[keyName] = seedVertexIndex;
                    }

                    Vector3 edgePos;

                    if (catmullClarke)
                    {
                        edgePos = calcEdgePos(edges[j]);
                        edgePos = Vector3.LerpUnclamped(edges[j].Midpoint, edgePos, param1);
                    }
                    else
                    {
                        // Maintain a fixed point
                        edgePos = edges[j].Midpoint;
                    }

                    keyName = edges[j].PairedName.ToString();
                    if (newEdgeVertsLookup.ContainsKey(keyName))
                    {
                        edgeIndex = newEdgeVertsLookup[keyName];
                    }
                    else
                    {
                        vertexPoints.Add(edgePos);
                        vertexRoles.Add(Roles.NewAlt);
                        edgeIndex = vertexPoints.Count - 1;
                        newEdgeVertsLookup[keyName] = edgeIndex;
                    }

                    Vector3 prevMidpointVertex;
                    if (catmullClarke)
                    {
                        prevMidpointVertex = calcEdgePos(edges[j].Next);
                        prevMidpointVertex = Vector3.LerpUnclamped(edges[j].Next.Midpoint, prevMidpointVertex, param1);
                    }
                    else
                    {
                        prevMidpointVertex = edges[j].Next.Midpoint;
                    }
                    keyName = edges[j].Next.PairedName.ToString();

                    if (newEdgeVertsLookup.ContainsKey(keyName))
                    {
                        prevEdgeIndex = newEdgeVertsLookup[keyName];
                    }
                    else
                    {
                        vertexPoints.Add(prevMidpointVertex);
                        vertexRoles.Add(Roles.NewAlt);
                        prevEdgeIndex = vertexPoints.Count - 1;
                        newEdgeVertsLookup[keyName] = prevEdgeIndex;
                    }

                    thisFaceIndices.Add(centroidIndex);
                    thisFaceIndices.Add(edgeIndex);
                    thisFaceIndices.Add(seedVertexIndex);
                    thisFaceIndices.Add(prevEdgeIndex);

                    faceIndices.Add(thisFaceIndices);
                    // Alternate roles but only for faces with an even number of sides
                    if (j % 2 == 0 || (j < Faces.Count && Faces[j].Sides % 2 != 0))
                    {
                        faceRoles.Add(Roles.New);
                    }
                    else
                    {
                        faceRoles.Add(Roles.NewAlt);
                    }

                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            return poly;
        }

        public PolyMesh Expand(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var newVertices = new Dictionary<(Guid, Guid)?, int>();
            var edgeFaceFlags = new Dictionary<(Guid, Guid)?, bool>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            int vertexIndex = 0;

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float ratio = o.GetValueA(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];

                var edge = face.Halfedge;
                var centroid = face.Centroid;

                // Create a new face for each existing face
                var newInsetFace = new int[face.Sides];

                for (int i = 0; i < face.Sides; i++)
                {
                    var vertex = edge.Vertex.Position;
                    var newVertex = Vector3.LerpUnclamped(vertex, centroid, ratio);
                    vertexPoints.Add(newVertex);
                    vertexRoles.Add(Roles.Existing);
                    newInsetFace[i] = vertexIndex;
                    newVertices[edge.Name] = vertexIndex++;
                    edge = edge.Next;
                }

                faceIndices.Add(newInsetFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            // Add edge faces
            foreach (var edge in Halfedges)
            {
                if (!edgeFaceFlags.ContainsKey(edge.PairedName))
                {
                    if (edge.Pair != null)
                    {
                        var edgeFace = new[]
                        {
                            newVertices[edge.Name],
                            newVertices[edge.Prev.Name],
                            newVertices[edge.Pair.Name],
                            newVertices[edge.Pair.Prev.Name],
                        };
                        faceIndices.Add(edgeFace);
                        faceRoles.Add(Roles.New);

                        var newFaceTagSet = new HashSet<string>();
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                        newFaceTags.Add(newFaceTagSet);
                    }

                    edgeFaceFlags[edge.PairedName] = true;
                }
            }

            for (var idx = 0; idx < Vertices.Count; idx++)
            {
                var vertex = Vertices[idx];
                var vertexFace = new List<int>();
                for (var j = 0; j < vertex.Halfedges.Count; j++)
                {
                    var edge = vertex.Halfedges[j];
                    vertexFace.Add(newVertices[edge.Name]);
                }

                if (vertexFace.Count >= 3)
                {
                    faceIndices.Add(vertexFace.ToArray());
                    faceRoles.Add(Roles.NewAlt);

                    var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
                    var existingTagSets =
                        vertexFaceIndices.Select(fi => FaceTags[fi]);
                    var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                    {
                        rs.UnionWith(i);
                        return rs;
                    });
                    newFaceTags.Add(newFaceTagSet);
                }
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            return poly;
        }

        public PolyMesh Chamfer(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newVertices = new Dictionary<(Guid, Guid)?, int>();
            var edgeFaceFlags = new Dictionary<(Guid, Guid)?, bool>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vertexPoints[i]] = i;
            }

            int vertexIndex = existingVertices.Count;

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float ratio = o.GetValueA(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];

                var edge = face.Halfedge;
                var centroid = face.Centroid;

                // Create a new face for each existing face
                var newInsetFace = new int[face.Sides];

                for (int i = 0; i < face.Sides; i++)
                {
                    var vertex = edge.Vertex.Position;
                    var newVertex = Vector3.LerpUnclamped(vertex, centroid, ratio);
                    vertexPoints.Add(newVertex);
                    vertexRoles.Add(Roles.New);
                    newInsetFace[i] = vertexIndex;
                    newVertices[edge.Name] = vertexIndex++;
                    edge = edge.Next;
                }

                faceIndices.Add(newInsetFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            // Add edge faces
            foreach (var edge in Halfedges)
            {
                var newFaceTagSet = new HashSet<string>();
                if (!edgeFaceFlags.ContainsKey(edge.PairedName))
                {
                    edgeFaceFlags[edge.PairedName] = true;
                    if (edge.Pair != null)
                    {
                        var edgeFace = new[]
                        {
                            existingVertices[edge.Vertex.Position],
                            newVertices[edge.Name],
                            newVertices[edge.Prev.Name],
                            existingVertices[edge.Pair.Vertex.Position],
                            newVertices[edge.Pair.Name],
                            newVertices[edge.Pair.Prev.Name],
                        };
                        faceIndices.Add(edgeFace);
                        faceRoles.Add(Roles.New);
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                        newFaceTags.Add(newFaceTagSet);
                    }
                }
            }

            // Planarize new edge faces
            // TODO not perfect - we need an iterative algorithm
            edgeFaceFlags = new Dictionary<(Guid, Guid)?, bool>();
            foreach (var edge in Halfedges)
            {
                if (edge.Pair == null) continue;

                if (!edgeFaceFlags.ContainsKey(edge.PairedName))
                {
                    edgeFaceFlags[edge.PairedName] = true;

                    float distance;

                    var plane = new Plane();
                    plane.Set3Points(
                        vertexPoints[newVertices[edge.Name]],
                        vertexPoints[newVertices[edge.Prev.Name]],
                        vertexPoints[newVertices[edge.Pair.Name]]
                    );

                    var ray1 = new Ray(edge.Vertex.Position, edge.Vertex.Normal);
                    plane.Raycast(ray1, out distance);
                    vertexPoints[existingVertices[edge.Vertex.Position]] = ray1.GetPoint(distance);

                    var ray2 = new Ray(edge.Pair.Vertex.Position, edge.Pair.Vertex.Normal);
                    plane.Raycast(ray2, out distance);
                    vertexPoints[existingVertices[edge.Pair.Vertex.Position]] = ray2.GetPoint(distance);
                }
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            return poly;
        }

        public PolyMesh Join(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newCentroidVertices = new Dictionary<string, int>();
            var rhombusFlags = new Dictionary<(Guid, Guid)?, bool>(); // Track if we've created a face for joined edges

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
                existingVertices[vertexPoints[i]] = i;
                vertexRoles.Add(Roles.New);
            }

            int vertexIndex = vertexPoints.Count();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                // This doesn't really work
                // Join shouldn't really accept an offset in any case
                float offset = o.GetValueA(this, faceIndex) * (faceIndex%2==0 ? 1 : -1);
                var face = Faces[faceIndex];
                vertexPoints.Add(face.Centroid + face.Normal * offset);
                newCentroidVertices[face.Name] = vertexIndex++;
                vertexRoles.Add(Roles.New);
            }

            for (var i = 0; i < Halfedges.Count; i++)
            {
                var edge = Halfedges[i];
                if (!rhombusFlags.ContainsKey(edge.PairedName))
                {
                    if (edge.Pair != null)
                    {
                        var rhombus = new[]
                        {
                            newCentroidVertices[edge.Pair.Face.Name],
                            existingVertices[edge.Vertex.Position],
                            newCentroidVertices[edge.Face.Name],
                            existingVertices[edge.Prev.Vertex.Position]
                        };
                        faceIndices.Add(rhombus);
                        faceRoles.Add(i % 2 == 0 ? Roles.New : Roles.NewAlt);

                        var newFaceTagSet = new HashSet<string>();
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                        newFaceTags.Add(newFaceTagSet);
                    }

                    rhombusFlags[edge.PairedName] = true;
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Needle(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newCentroidVertices = new Dictionary<string, int>();
            var rhombusFlags = new Dictionary<(Guid, Guid)?, bool>(); // Track if we've created a face for joined edges

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            int lastVertIndex = 0;
            for (var i = 0; i < Vertices.Count; i++)
            {
                var vert = Vertices[i];
                vertexPoints.Add(vert.Position);
                existingVertices[vert.Position] = lastVertIndex;
                vertexRoles.Add(Roles.Existing);
                lastVertIndex++;
            }

            int vertexIndex = vertexPoints.Count();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float offset = o.GetValueA(this, faceIndex);
                var face = Faces[faceIndex];
                vertexPoints.Add(face.Centroid + face.Normal * (offset + 1));
                newCentroidVertices[face.Name] = vertexIndex++;
                vertexRoles.Add(Roles.New);
            }

            for (var i = 0; i < Halfedges.Count; i++)
            {
                var edge = Halfedges[i];
                var pair = edge.Pair;
                if (!rhombusFlags.ContainsKey(edge.PairedName))
                {
                    if (pair != null)
                    {
                        var tri1 = new[]
                        {
                            newCentroidVertices[pair.Face.Name],
                            existingVertices[edge.Vertex.Position],
                            newCentroidVertices[edge.Face.Name],
                        };
                        faceIndices.Add(tri1);
                        faceRoles.Add( Roles.New);

                        var newFaceTagSet1 = new HashSet<string>();
                        newFaceTagSet1.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                        newFaceTagSet1.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                        newFaceTags.Add(newFaceTagSet1);

                        var tri2 = new[]
                        {
                            newCentroidVertices[pair.Face.Name],
                            newCentroidVertices[edge.Face.Name],
                            existingVertices[pair.Vertex.Position],
                        };
                        faceIndices.Add(tri2);
                        faceRoles.Add(Roles.NewAlt);

                        var newFaceTagSet2 = new HashSet<string>();
                        newFaceTagSet2.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                        newFaceTagSet2.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                        newFaceTags.Add(newFaceTagSet2);
                    }
                    rhombusFlags[edge.PairedName] = true;

                }
            }
            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);

        }

        public PolyMesh Kis(OpParams o, List<int> selectedFaces = null, bool scalebyArea = false)
        {
            var newFaceTags = new List<HashSet<string>>();
            // vertices and faces to vertices
            var vertexRoles = Enumerable.Repeat(Roles.Existing, Vertices.Count).ToList();
            List<Vector3> vertexPoints = Vertices.Select(v => v.Position).ToList();

            var faceRoles = new List<Roles>();

            // vertex lookup
            var vlookup = new Dictionary<Guid, int>();
            int n = Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                vlookup.Add(Vertices[i].Name, i);
            }

            // create new tri-faces (like a fan)
            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var prevFaceTagSet = FaceTags[faceIndex];
                bool includeFace = selectedFaces == null || selectedFaces.Contains(faceIndex);
                includeFace &= IncludeFace(faceIndex, o.filter);
                if (includeFace)
                {
                    var face = Faces[faceIndex];
                    float offset = o.GetValueA(this, faceIndex);
                    var newVertPos = face.Centroid + face.Normal * offset;
                    if (scalebyArea) newVertPos *= face.GetArea();
                    vertexPoints.Add(newVertPos);
                    vertexRoles.Add(Roles.New);


                    var list = Faces[faceIndex].GetHalfedges();
                    for (var edgeIndex = 0; edgeIndex < list.Count; edgeIndex++)
                    {
                        var edge = list[edgeIndex];
                        // Create new face from edge start, edge end and centroid
                        faceIndices.Add(
                            new[] {vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], vertexPoints.Count - 1}
                        );
                        // Alternate roles but only for faces with an even number of sides
                        if (edgeIndex % 2 == 0 || Faces[faceIndex].Sides % 2 != 0)
                        {
                            faceRoles.Add(Roles.New);
                        }
                        else
                        {
                            faceRoles.Add(Roles.NewAlt);
                        }

                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }
                }
                else
                {
                    faceIndices.Add(ListFacesByVertexIndices()[faceIndex]);
                    faceRoles.Add(Roles.Ignored);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Gyro(OpParams o)
        {
            // Happy accidents - skip n new faces - offset just the centroid?

            var newFaceTags = new List<HashSet<string>>();

            var existingVerts = new Dictionary<string, int>();
            var newVerts = new Dictionary<string, int>();
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            // Loop through old faces
            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float ratio = o.GetValueA(this, faceIndex);
                float offset = o.GetValueB(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var oldFace = Faces[faceIndex];
                vertexPoints.Add(oldFace.Centroid + oldFace.Normal * offset);
                vertexRoles.Add(Roles.New);
                int centroidIndex = vertexPoints.Count - 1;

                // Loop through each vertex on old face and create a new face for each
                for (int j = 0; j < oldFace.GetHalfedges().Count; j++)
                {
                    int seedVertexIndex;
                    int OneThirdIndex;
                    int PairOneThirdIndex;
                    int PrevThirdIndex;

                    string keyName;

                    var thisFaceIndices = new List<int>();
                    var edges = oldFace.GetHalfedges();

                    var seedVertex = edges[j].Vertex;
                    keyName = seedVertex.Name.ToString();
                    if (existingVerts.ContainsKey(keyName))
                    {
                        seedVertexIndex = existingVerts[keyName];
                    }
                    else
                    {
                        vertexPoints.Add(seedVertex.Position - seedVertex.Normal * offset);
                        vertexRoles.Add(Roles.Existing);
                        seedVertexIndex = vertexPoints.Count - 1;
                        existingVerts[keyName] = seedVertexIndex;
                    }

                    var OneThirdVertex = edges[j].PointAlongEdge(ratio);
                    keyName = edges[j].Name.ToString();
                    if (newVerts.ContainsKey(keyName))
                    {
                        OneThirdIndex = newVerts[keyName];
                    }
                    else
                    {
                        vertexPoints.Add(OneThirdVertex);
                        vertexRoles.Add(Roles.NewAlt);
                        OneThirdIndex = vertexPoints.Count - 1;
                        newVerts[keyName] = OneThirdIndex;
                    }

                    Vector3 PrevThirdVertex;
                    if (edges[j].Next.Pair != null)
                    {
                        PrevThirdVertex = edges[j].Next.Pair.PointAlongEdge(ratio);
                        keyName = edges[j].Next.Pair.Name.ToString();
                    }
                    else
                    {
                        PrevThirdVertex = edges[j].Next.PointAlongEdge(1 - ratio);
                        keyName = edges[j].Next.Name + "-Pair";
                    }

                    if (newVerts.ContainsKey(keyName))
                    {
                        PrevThirdIndex = newVerts[keyName];
                    }
                    else
                    {
                        vertexPoints.Add(PrevThirdVertex);
                        vertexRoles.Add(Roles.NewAlt);
                        PrevThirdIndex = vertexPoints.Count - 1;
                        newVerts[keyName] = PrevThirdIndex;
                    }

                    Vector3 PairOneThird;
                    if (edges[j].Pair != null)
                    {
                        PairOneThird = edges[j].Pair.PointAlongEdge(ratio);
                        keyName = edges[j].Pair.Name.ToString();
                    }
                    else
                    {
                        PairOneThird = edges[j].PointAlongEdge(1 - ratio);
                        keyName = edges[j].Name + "-Pair";
                    }

                    if (newVerts.ContainsKey(keyName))
                    {
                        PairOneThirdIndex = newVerts[keyName];
                    }
                    else
                    {
                        vertexPoints.Add(PairOneThird);
                        vertexRoles.Add(Roles.NewAlt);
                        PairOneThirdIndex = vertexPoints.Count - 1;
                        newVerts[keyName] = PairOneThirdIndex;
                    }

                    thisFaceIndices.Add(centroidIndex);
                    thisFaceIndices.Add(PairOneThirdIndex);
                    thisFaceIndices.Add(OneThirdIndex);
                    thisFaceIndices.Add(seedVertexIndex);
                    thisFaceIndices.Add(PrevThirdIndex);

                    faceIndices.Add(thisFaceIndices);
                    // Alternate roles but only for faces with an even number of sides
                    if (j % 2 == 0 || (j < Faces.Count && Faces[j].Sides % 2 != 0))
                    {
                        faceRoles.Add(Roles.New);
                    }
                    else
                    {
                        faceRoles.Add(Roles.NewAlt);
                    }

                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            return poly;
        }

        public PolyMesh Subdivide(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            for (int vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                float offset = o.GetValueA(this, vertexIndex);
                var x = Vertices[vertexIndex];
                vertexPoints.Add(x.Position + x.Normal * offset);
            }

            //Vertices.Select(x => ).ToList(); // Existing vertices
            var vertexRoles = Enumerable.Repeat(Roles.Existing, vertexPoints.Count).ToList();

            var faceRoles = new List<Roles>();

            // Create new vertices, one at the midpoint of each edge

            var newVertices = new Dictionary<(Guid, Guid)?, int>();
            int currentVertexIndex = vertexPoints.Count;

            foreach (var edge in Halfedges)
            {
                vertexPoints.Add(edge.Midpoint);
                vertexRoles.Add(Roles.New);
                newVertices[edge.PairedName] = currentVertexIndex++;
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var prevFaceTagSet = FaceTags[faceIndex];
                // Create a new face for each existing face
                var newFace = new int[face.Sides];
                var edge = face.Halfedge;

                for (int i = 0; i < face.Sides; i++)
                {
                    newFace[i] = newVertices[edge.PairedName];
                    edge = edge.Next;
                }

                faceIndices.Add(newFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            // Create new faces for each vertex
            for (int idx = 0; idx < Vertices.Count; idx++)
            {
                var vertex = Vertices[idx];
                var adjacentFaces = vertex.GetVertexFaces();
                if (adjacentFaces.Count < 3) continue;

                for (var faceIndex = 0; faceIndex < adjacentFaces.Count; faceIndex++)
                {
                    Face face = adjacentFaces[faceIndex];
                    var edge = face.GetHalfedges().Find(x => x.Vertex == vertex);
                    int currVertex = newVertices[edge.PairedName];
                    int prevVertex = newVertices[edge.Next.PairedName];
                    var triangle = new[] {idx, prevVertex, currVertex};
                    faceIndices.Add(triangle);
                    faceRoles.Add(Roles.New);

                    var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
                    var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi]);
                    var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                    {
                        rs.UnionWith(i);
                        return rs;
                    });
                    newFaceTags.Add(newFaceTagSet);
                }
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            return poly;
        }

        public PolyMesh Loft(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();
            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newVertices = new Dictionary<(Guid, Guid)?, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vertexPoints[i]] = i;
            }

            int vertexIndex = vertexPoints.Count();

            // Create new vertices
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var prevFaceTags = FaceTags[faceIndex];

                if (IncludeFace(faceIndex, o.filter))
                {
                    float ratio = o.GetValueA(this, faceIndex);
                    float offset = o.GetValueB(this, faceIndex);
                    var prevExtrovertTags = FaceTags[faceIndex];
                    var offsetVector = face.Normal * offset;

                    var edge = face.Halfedge;
                    var centroid = face.Centroid;

                    // Create a new face for each existing face
                    var newInsetFace = new int[face.Sides];
                    int newV;
                    int prevNewV = -1;

                    for (int i = 0; i < face.Sides; i++)
                    {
                        var vertex = edge.Vertex.Position;
                        var newVertex = Vector3.LerpUnclamped(vertex, centroid, ratio);
                        newVertex += offsetVector;
                        vertexPoints.Add(newVertex);
                        vertexRoles.Add(Roles.New);
                        newInsetFace[i] = vertexIndex;
                        newVertices[edge.Name] = vertexIndex++;

                        // Generate new faces
                        newV = newVertices[edge.Name];
                        if (i > 0)
                        {
                            var newEdgeFace = new[]
                            {
                                newV,
                                prevNewV,
                                existingVertices[edge.Prev.Vertex.Position],
                                existingVertices[edge.Vertex.Position]
                            };
                            faceIndices.Add(newEdgeFace);
                            // Alternate roles but only for faces with an even number of sides
                            if (i % 2 == 0 || face.Sides % 2 != 0)
                            {
                                faceRoles.Add(Roles.New);
                            }
                            else
                            {
                                faceRoles.Add(Roles.NewAlt);
                            }

                            newFaceTags.Add(new HashSet<string>(prevExtrovertTags));
                        }

                        prevNewV = newV;
                        edge = edge.Next;
                    }

                    // Add the final missing new edge face

                    var lastEdge = face.Halfedge.Prev;
                    var finalFace = new[]
                    {
                        existingVertices[lastEdge.Vertex.Position],
                        existingVertices[lastEdge.Next.Vertex.Position],
                        newVertices[lastEdge.Next.Name],
                        newVertices[lastEdge.Name]
                    };
                    faceIndices.Add(finalFace);
                    faceRoles.Add(Roles.New);
                    newFaceTags.Add(new HashSet<string>(prevExtrovertTags));

                    // Inner face
                    faceIndices.Add(newInsetFace);
                    faceRoles.Add(Roles.Existing);
                    newFaceTags.Add(new HashSet<string>(prevFaceTags));
                }
                else
                {
                    faceIndices.Add(
                        face.GetHalfedges().Select(
                            x => existingVertices[x.Vertex.Position]
                        ).ToArray());
                    faceRoles.Add(Roles.Ignored);
                    newFaceTags.Add(new HashSet<string>(prevFaceTags));
                }
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            return poly;
        }

        public PolyMesh Quinto(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
            var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                var pos = Vertices[vertexIndex].Position;
                vertexPoints.Add(pos);
                vertexRoles.Add(Roles.Existing);
                existingVertices[pos] = vertexIndex;
            }

            int currentVertexIndex = vertexPoints.Count();

            // Create new edge vertices
            foreach (var edge in Halfedges)
            {
                var pos = edge.Midpoint;
                float offset = o.GetValueB(this, Faces.IndexOf(edge.Face));
                vertexPoints.Add(pos - edge.Face.Normal * offset);
                vertexRoles.Add(Roles.New);
                newEdgeVertices[edge.PairedName] = currentVertexIndex++;
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float ratio = o.GetValueA(this, faceIndex);
                float offset = o.GetValueB(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                var edge = face.Halfedge;
                var centroid = face.Centroid;

                // Create a new face for each existing face
                var newInsetFace = new int[face.Sides];
                int prevNewEdgeVertex = -1;
                int prevNewInnerVertex = -1;

                for (int i = 0; i < face.Sides; i++)
                {
                    var newEdgeVertex = vertexPoints[newEdgeVertices[edge.PairedName]];
                    var newInnerVertex = Vector3.LerpUnclamped(newEdgeVertex, centroid, ratio);

                    vertexPoints.Add(newInnerVertex + face.Normal * offset);
                    vertexRoles.Add(Roles.NewAlt);
                    newInsetFace[i] = currentVertexIndex;
                    newInnerVertices[edge.Name] = currentVertexIndex++;


                    // Generate new faces
                    if (i > 0)
                    {
                        var newEdgeFace = new[]
                        {
                            prevNewInnerVertex,
                            prevNewEdgeVertex,
                            existingVertices[edge.Prev.Vertex.Position],
                            newEdgeVertices[edge.PairedName],
                            newInnerVertices[edge.Name]
                        };
                        faceIndices.Add(newEdgeFace);
                        // Alternate roles but only for faces with an even number of sides
                        if (i % 2 == 0 || face.Sides % 2 != 0)
                        {
                            faceRoles.Add(Roles.New);
                        }
                        else
                        {
                            faceRoles.Add(Roles.NewAlt);
                        }

                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }

                    prevNewEdgeVertex = newEdgeVertices[edge.PairedName];
                    prevNewInnerVertex = newInnerVertices[edge.Name];
                    edge = edge.Next;
                }

                faceIndices.Add(newInsetFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                // Add the final missing new edge face
                var finalFace = new[]
                {
                    prevNewInnerVertex,
                    prevNewEdgeVertex,
                    existingVertices[edge.Prev.Vertex.Position],
                    newEdgeVertices[edge.PairedName],
                    newInnerVertices[edge.Name]
                };
                faceIndices.Add(finalFace);
                // Alternate roles for final face
                if (face.Sides % 2 == 0 || face.Sides % 2 != 0)
                {
                    faceRoles.Add(Roles.New);
                }
                else
                {
                    faceRoles.Add(Roles.NewAlt);
                }

                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            return poly;
        }

        public PolyMesh JoinedLace(OpParams o)
        {
            return _Lace(o, true, false);
        }

        public PolyMesh OppositeLace(OpParams o)
        {
            return _Lace(o, false, true);
        }

        public PolyMesh Lace(OpParams o)
        {
            return _Lace(o, false, false);
        }

        private PolyMesh _Lace(OpParams o, bool joined, bool opposite)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();
            var rhombusFlags = new Dictionary<(Guid, Guid)?, bool>(); // Track if we've created a face for joined edges

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                var pos = Vertices[i].Position;
                vertexPoints.Add(pos);
                vertexRoles.Add(Roles.Existing);
                existingVertices[pos] = i;
            }

            int vertexIndex = vertexPoints.Count();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float ratio = o.GetValueA(this, faceIndex);
                float offset = o.GetValueB(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                var offsetVector = face.Normal * offset;
                if (joined || opposite || IncludeFace(faceIndex, o.filter))
                {
                    var edge = face.Halfedge;
                    var centroid = face.Centroid;
                    var innerFace = new int[face.Sides];

                    for (int i = 0; i < face.Sides; i++)
                    {
                        var newVertex = Vector3.LerpUnclamped(
                            edge.Midpoint,
                            centroid,
                            ratio
                        );

                        // Build face at center of each original face
                        vertexPoints.Add(newVertex + offsetVector);
                        vertexRoles.Add(Roles.New);
                        newInnerVertices[edge.Name] = vertexIndex;
                        innerFace[i] = vertexIndex++;

                        edge = edge.Next;
                    }

                    edge = face.Halfedge;

                    for (int i = 0; i < face.Sides; i++)
                    {
                        var largeTriangle = new[]
                        {
                            newInnerVertices[edge.Next.Name],
                            newInnerVertices[edge.Name],
                            existingVertices[edge.Vertex.Position]
                        };
                        faceIndices.Add(largeTriangle);
                        faceRoles.Add(Roles.NewAlt);
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                        if (!joined && !opposite)
                        {
                            var smallTriangle = new []
                            {
                                existingVertices[edge.Prev.Vertex.Position],
                                existingVertices[edge.Vertex.Position],
                                newInnerVertices[edge.Name]
                            };
                            faceIndices.Add(smallTriangle);
                            faceRoles.Add(Roles.New);
                            newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                        }

                        edge = edge.Next;
                    }

                    faceIndices.Add(innerFace);
                    faceRoles.Add(Roles.Existing);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
                else
                {
                    faceIndices.Add(
                        face.GetHalfedges().Select(
                            x => existingVertices[x.Vertex.Position]
                        ).ToArray());
                    faceRoles.Add(Roles.Ignored);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            // Create Rhombus faces
            // TODO Make planar

            if (joined)
            {
                foreach (var edge in Halfedges)
                {
                    if (!rhombusFlags.ContainsKey(edge.PairedName))
                    {
                        if (edge.Pair != null)
                        {
                            var rhombus = new[]
                            {
                                existingVertices[edge.Prev.Vertex.Position],
                                newInnerVertices[edge.Pair.Name],
                                existingVertices[edge.Vertex.Position],
                                newInnerVertices[edge.Name]
                            };
                            faceIndices.Add(rhombus);
                            faceRoles.Add(Roles.New);

                            var newFaceTagSet = new HashSet<string>();
                            newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                            newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                            newFaceTags.Add(newFaceTagSet);
                        }

                        rhombusFlags[edge.PairedName] = true;
                    }
                }
            }

            if (opposite)
            {
                foreach (var edge in Halfedges)
                {
                    if (!rhombusFlags.ContainsKey(edge.PairedName))
                    {
                        if (edge.Pair != null)
                        {
                            var tri1 = new[]
                            {
                                existingVertices[edge.Prev.Vertex.Position],
                                newInnerVertices[edge.Pair.Name],
                                newInnerVertices[edge.Name]
                            };
                            faceIndices.Add(tri1);
                            faceRoles.Add(Roles.New);

                            var newFaceTagSet1 = new HashSet<string>();
                            newFaceTagSet1.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                            newFaceTagSet1.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                            newFaceTags.Add(newFaceTagSet1);

                            var tri2 = new[]
                            {
                                newInnerVertices[edge.Pair.Name],
                                existingVertices[edge.Vertex.Position],
                                newInnerVertices[edge.Name]
                            };
                            faceIndices.Add(tri2);
                            faceRoles.Add(Roles.New);

                            var newFaceTagSet2 = new HashSet<string>();
                            newFaceTagSet2.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                            newFaceTagSet2.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                            newFaceTags.Add(newFaceTagSet2);
                        }

                        rhombusFlags[edge.PairedName] = true;
                    }
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Stake(OpParams o, bool join = false)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();
            var newCentroidVertices = new Dictionary<string, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vertexPoints[i]] = i;
            }

            int vertexIndex = vertexPoints.Count();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float ratio = o.GetValueA(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                if (join || IncludeFace(faceIndex, o.filter))
                {
                    var edge = face.Halfedge;
                    var centroid = face.Centroid;

                    vertexPoints.Add(centroid);
                    newCentroidVertices[face.Name] = vertexIndex++;
                    vertexRoles.Add(Roles.New);

                    // Generate the quads and triangles on this face
                    for (int i = 0; i < face.Sides; i++)
                    {
                        var newVertex = Vector3.LerpUnclamped(
                            edge.Midpoint,
                            centroid,
                            ratio
                        );

                        vertexPoints.Add(newVertex);
                        vertexRoles.Add(Roles.NewAlt);
                        newInnerVertices[edge.Name] = vertexIndex++;

                        edge = edge.Next;
                    }

                    edge = face.Halfedge;

                    for (int i = 0; i < face.Sides; i++)
                    {
                        if (!join)
                        {
                            var triangle = new[]
                            {
                                newInnerVertices[edge.Name],
                                existingVertices[edge.Prev.Vertex.Position],
                                existingVertices[edge.Vertex.Position]
                            };
                            faceIndices.Add(triangle);
                            // Alternate roles but only for faces with an even number of sides
                            if (i % 2 == 0 || face.Sides % 2 != 0)
                            {
                                faceRoles.Add(Roles.New);
                            }
                            else
                            {
                                faceRoles.Add(Roles.NewAlt);
                            }

                            newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                        }

                        var quad = new[]
                        {
                            existingVertices[edge.Vertex.Position],
                            newInnerVertices[edge.Next.Name],
                            newCentroidVertices[face.Name],
                            newInnerVertices[edge.Name],
                        };
                        faceIndices.Add(quad);
                        // Alternate roles but only for faces with an even number of sides
                        if (i % 2 == 0 || face.Sides % 2 != 0)
                        {
                            faceRoles.Add(Roles.Existing);
                        }
                        else
                        {
                            faceRoles.Add(Roles.ExistingAlt);
                        }

                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                        edge = edge.Next;
                    }
                }
                else
                {
                    faceIndices.Add(
                        face.GetHalfedges().Select(
                            x => existingVertices[x.Vertex.Position]
                        ).ToArray()
                    );
                    faceRoles.Add(Roles.Ignored);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            if (join)
            {
                var edgeFlags = new HashSet<(Guid, Guid)?>();
                foreach (var edge in Halfedges)
                {
                    if (edge.Pair == null) continue;

                    if (!edgeFlags.Contains(edge.PairedName))
                    {
                        var quad = new[]
                        {
                            existingVertices[edge.Vertex.Position],
                            newInnerVertices[edge.Name],
                            existingVertices[edge.Pair.Vertex.Position],
                            newInnerVertices[edge.Pair.Name],
                        };
                        faceIndices.Add(quad);
                        faceRoles.Add(Roles.New);
                        edgeFlags.Add(edge.PairedName);

                        var newFaceTagSet = new HashSet<string>();
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                        newFaceTags.Add(newFaceTagSet);
                    }
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh JoinKisKis(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();
            var newCentroidVertices = new Dictionary<string, int>();
            var rhombusFlags = new Dictionary<(Guid, Guid)?, bool>(); // Track if we've created a face for joined edges

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                var vert = Vertices[i];
                vertexPoints.Add(vert.Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vert.Position] = i;
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float offset = o.GetValueB(this, faceIndex);
                var face = Faces[faceIndex];
                var centroid = face.Centroid;
                vertexPoints.Add(centroid + face.Normal * offset);
                vertexRoles.Add(Roles.Existing);
                newCentroidVertices[face.Name] = vertexPoints.Count - 1;
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var ratio = o.GetValueA(this, faceIndex);
                var offset = o.GetValueB(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                var centroid = face.Centroid;
                var edges = face.GetHalfedges();

                for (int i = 0; i < edges.Count; i++)
                {
                    var edge = edges[i];

                    var newVertex = Vector3.LerpUnclamped(
                        edge.Midpoint,
                        centroid,
                        ratio
                    );
                    vertexPoints.Add(newVertex + face.Normal * offset);
                    vertexRoles.Add(Roles.New);
                    newInnerVertices[edge.Name] = vertexPoints.Count - 1;

                    var triangle1 = new[]
                    {
                        newCentroidVertices[edge.Face.Name],
                        newInnerVertices[edge.Name],
                        existingVertices[edge.Vertex.Position]
                    };
                    faceIndices.Add(triangle1);
                    faceRoles.Add(Roles.New);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    var triangle2 = new[]
                    {
                        newInnerVertices[edge.Name],
                        newCentroidVertices[edge.Face.Name],
                        existingVertices[edge.Prev.Vertex.Position]
                    };
                    faceIndices.Add(triangle2);
                    faceRoles.Add(Roles.NewAlt);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            // Create Rhombus faces
            // TODO Make planar

            foreach (var edge in Halfedges)
            {
                if (!rhombusFlags.ContainsKey(edge.PairedName))
                {
                    if (edge.Pair != null)
                    {
                        var rhombus = new[]
                        {
                            existingVertices[edge.Prev.Vertex.Position],
                            newInnerVertices[edge.Pair.Name],
                            existingVertices[edge.Vertex.Position],
                            newInnerVertices[edge.Name]
                        };
                        faceIndices.Add(rhombus);
                        faceRoles.Add(Roles.Existing);

                        var newFaceTagSet = new HashSet<string>();
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Face)]);
                        newFaceTagSet.UnionWith(FaceTags[Faces.IndexOf(edge.Pair.Face)]);
                        newFaceTags.Add(newFaceTagSet);
                    }

                    rhombusFlags[edge.PairedName] = true;
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Medial(OpParams o)
        {
            return _Medial(o);
        }

        public PolyMesh EdgeMedial(OpParams o)
        {
            return _Medial(o, true);
        }

        private PolyMesh _Medial(OpParams o, bool edgeMedial = false)
        {
            int subdivisions = ((int) o.GetValueA(this, 0)) + 1;
            subdivisions = subdivisions < 1 ? 1 : subdivisions;
            subdivisions = subdivisions > 64 ? 64 : subdivisions;

            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newEdgeVertices = new Dictionary<(Guid, Guid)?, int[]>();
            var prevFaceTagSets = new Dictionary<string, HashSet<string>>();
            var newCentroidVertices = new Dictionary<string, int>();
            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                var vert = Vertices[i];
                vertexPoints.Add(vert.Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vert.Position] = i;
            }

            int vertexIndex = vertexPoints.Count;

            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                if (!newCentroidVertices.ContainsKey(face.Name))
                {
                    float offset = o.GetValueB(this, faceIndex);
                    vertexPoints.Add(face.Centroid + face.Normal * offset);
                    newCentroidVertices[face.Name] = vertexIndex++;

                    vertexRoles.Add(Roles.New);
                    prevFaceTagSets[face.Name] = FaceTags[faceIndex];
                }

                var firstEdge = face.Halfedge;
                Halfedge edge = null;
                while (edge != firstEdge)
                {
                    if (edge == null) edge = firstEdge;

                    bool hasPair = edge.Pair!=null && newEdgeVertices.ContainsKey(edge.Pair.Name);
                    if (!newEdgeVertices.ContainsKey(edge.Name) && !hasPair)
                    {
                        newEdgeVertices[edge.Name] = new int[subdivisions];
                        for (int i = 0; i < subdivisions; i++)
                        {
                            vertexPoints.Add(edge.PointAlongEdge((1f / (subdivisions + 1)) * (i + 1)));
                            vertexRoles.Add(Roles.NewAlt);
                            newEdgeVertices[edge.Name][i] = vertexIndex++;
                        }
                    }

                    int[] currNewVerts = null;
                    bool flip = false;
                    if (newEdgeVertices.ContainsKey(edge.Name))
                    {
                        currNewVerts = newEdgeVertices[edge.Name];
                    }
                    else if (edge.Pair != null && newEdgeVertices.ContainsKey(edge.Pair.Name))
                    {
                        currNewVerts = newEdgeVertices[edge.Pair.Name];
                        flip = true;
                    }

                    int centroidIndex = newCentroidVertices[face.Name];
                    var prevFaceTagSet = prevFaceTagSets[face.Name];

                    if (!edgeMedial)
                    {
                        // Two triangular faces
                        var triangle1 = flip ?
                            new[]
                            {
                                centroidIndex,
                                currNewVerts.Last(),
                                existingVertices[edge.Vertex.Position]
                            } :
                            new[] {
                                centroidIndex,
                                currNewVerts[0],
                                existingVertices[edge.Vertex.Position]
                            };
                        faceIndices.Add(triangle1);
                        faceRoles.Add(Roles.Existing);
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                        var triangle2 = flip ?
                            new[] {
                                centroidIndex,
                                existingVertices[edge.Prev.Vertex.Position],
                                currNewVerts.First(),
                            } :
                            new[] {
                                centroidIndex,
                                existingVertices[edge.Prev.Vertex.Position],
                                currNewVerts.Last(),
                            };
                        faceIndices.Add(triangle2);
                        faceRoles.Add(Roles.Existing);
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }

                    // Create new triangular faces at edges
                    for (int j = 0; j < subdivisions - 1; j++)
                    {
                        int edgeVertIndex;
                        int edgeNextVertIndex;

                        edgeVertIndex = j;
                        edgeNextVertIndex = j + 1;

                        int[] edgeTriangle = flip ?
                            new[] {
                                centroidIndex,
                                currNewVerts[edgeVertIndex],
                                currNewVerts[edgeNextVertIndex],
                            }
                            : new[]
                            {
                                centroidIndex,
                                currNewVerts[edgeNextVertIndex],
                                currNewVerts[edgeVertIndex],
                            };

                        faceIndices.Add(edgeTriangle);
                        faceRoles.Add(j % 2 == 0 ? Roles.New : Roles.NewAlt);
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }

                    edge = edge.Next;
                }
            }

            if (edgeMedial)
            {
                for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
                {
                    var face = Faces[faceIndex];
                    var firstEdge = face.Halfedge;
                    Halfedge edge = null;
                    while (edge != firstEdge)
                    {
                        if (edge == null) edge = firstEdge;

                        int[] currNewVerts = null;
                        bool flip = false;
                        if (newEdgeVertices.ContainsKey(edge.Name))
                        {
                            currNewVerts = newEdgeVertices[edge.Name];
                        }
                        else if (newEdgeVertices.ContainsKey(edge.Pair.Name))
                        {
                            currNewVerts = newEdgeVertices[edge.Pair.Name];
                            flip = true;
                        }

                        int[] nextNewVerts = null;
                        bool flipNext = false;
                        if (newEdgeVertices.ContainsKey(edge.Next.Name))
                        {
                            nextNewVerts = newEdgeVertices[edge.Next.Name];
                        }
                        else if (newEdgeVertices.ContainsKey(edge.Next.Pair.Name))
                        {
                            nextNewVerts = newEdgeVertices[edge.Next.Pair.Name];
                            flipNext = true;
                        }

                        int centroidIndex = newCentroidVertices[face.Name];
                        var prevFaceTagSet = prevFaceTagSets[face.Name];

                        // One quadrilateral face
                        int[] quad;

                        quad = new[]
                        {
                            centroidIndex,
                            flip ? currNewVerts.Last() : currNewVerts.First(),
                            existingVertices[edge.Vertex.Position],
                            flipNext ? nextNewVerts.First() : nextNewVerts.Last(),
                        };

                        faceIndices.Add(quad);
                        faceRoles.Add(Roles.Existing);
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                        edge = edge.Next;
                    }
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Propeller(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newEdgeVertices = new Dictionary<string, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vertexPoints[i]] = i;
            }

            int vertexIndex = vertexPoints.Count();

            // Create new edge vertices
            for (var i = 0; i < Halfedges.Count; i++)
            {
                var edge = Halfedges[i];
                float ratio = 1 - o.GetValueA(this, Faces.IndexOf(edge.Face));
                vertexPoints.Add(edge.PointAlongEdge(ratio));
                newEdgeVertices[edge.Name.ToString()] = vertexIndex++;
                vertexRoles.Add(Roles.New);

                if (edge.Pair != null)
                {
                    vertexPoints.Add(edge.Pair.PointAlongEdge(ratio));
                    newEdgeVertices[edge.Pair.Name.ToString()] = vertexIndex++;
                }
                else
                {
                    vertexPoints.Add(edge.PointAlongEdge(1 - ratio));
                    newEdgeVertices[edge.Name.ToString() + "-Pair"] = vertexIndex++;
                }

                vertexRoles.Add(Roles.New);
            }

            // Create quadrilateral faces and one central face on each face
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var prevFaceTagSet = FaceTags[faceIndex];

                var face = Faces[faceIndex];
                var edge = face.Halfedge;
                var centralFace = new int[face.Sides];

                for (int i = 0; i < face.Sides; i++)
                {
                    string edgePairName;
                    if (edge.Pair != null)
                    {
                        edgePairName = edge.Pair.Name.ToString();
                    }
                    else
                    {
                        edgePairName = edge.Name + "-Pair";
                    }

                    string edgeNextPairName;
                    if (edge.Next.Pair != null)
                    {
                        edgeNextPairName = edge.Next.Pair.Name.ToString();
                    }
                    else
                    {
                        edgeNextPairName = edge.Next.Name.ToString() + "-Pair";
                    }

                    var quad = new[]
                    {
                        newEdgeVertices[edge.Next.Name.ToString()],
                        newEdgeVertices[edgeNextPairName],
                        newEdgeVertices[edgePairName],
                        existingVertices[edge.Vertex.Position],
                    };
                    faceIndices.Add(quad);
                    // Alternate roles but only for faces with an even number of sides
                    if (i % 2 == 0 || face.Sides % 2 != 0)
                    {
                        faceRoles.Add(Roles.New);
                    }
                    else
                    {
                        faceRoles.Add(Roles.NewAlt);
                    }

                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    centralFace[i] = newEdgeVertices[edgePairName];
                    edge = edge.Next;
                }

                faceIndices.Add(centralFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Whirl(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newEdgeVertices = new Dictionary<string, int>();
            var newInnerVertices = new Dictionary<string, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vertexPoints[i]] = i;
            }

            int vertexIndex = vertexPoints.Count();

            for (var i = 0; i < Halfedges.Count; i++)
            {
                var edge = Halfedges[i];
                // Choosing a face index is a bit arbitrary but what can we do?
                float ratio = o.GetValueA(this, Faces.IndexOf(edge.Face));
                vertexPoints.Add(edge.PointAlongEdge(ratio));
                vertexRoles.Add(Roles.New);
                newEdgeVertices[edge.Name.ToString()] = vertexIndex++;
                if (edge.Pair != null)
                {
                    vertexPoints.Add(edge.Pair.PointAlongEdge(ratio));
                    newEdgeVertices[edge.Pair.Name.ToString()] = vertexIndex++;
                }
                else
                {
                    vertexPoints.Add(edge.PointAlongEdge(1 - ratio));
                    newEdgeVertices[edge.Name.ToString() + "-Pair"] = vertexIndex++;
                }

                vertexRoles.Add(Roles.New);
            }

            foreach (var face in Faces)
            {
                var edges = face.GetHalfedges();
                for (var i = 0; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    var direction = (face.Centroid - edge.Midpoint) * 2;
                    var pointOnEdge = vertexPoints[newEdgeVertices[edge.Name.ToString()]];
                    float ratio = o.GetValueA(this, Faces.IndexOf(edge.Face));
                    vertexPoints.Add(Vector3.LerpUnclamped(pointOnEdge, pointOnEdge + direction, ratio));
                    vertexRoles.Add(Roles.NewAlt);
                    newInnerVertices[edge.Name.ToString()] = vertexIndex++;
                }
            }

            // Generate hexagonal faces and central face
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var prevFaceTagSet = FaceTags[faceIndex];
                var centralFace = new int[face.Sides];
                var edge = face.Halfedge;

                for (var i = 0; i < face.Sides; i++)
                {
                    string edgeNextPairName;
                    if (edge.Next.Pair != null)
                    {
                        edgeNextPairName = edge.Next.Pair.Name.ToString();
                    }
                    else
                    {
                        edgeNextPairName = edge.Next.Name + "-Pair";
                    }

                    var hexagon = new[]
                    {
                        existingVertices[edge.Vertex.Position],
                        newEdgeVertices[edgeNextPairName],
                        newEdgeVertices[edge.Next.Name.ToString()],
                        newInnerVertices[edge.Next.Name.ToString()],
                        newInnerVertices[edge.Name.ToString()],
                        newEdgeVertices[edge.Name.ToString()],
                    };
                    faceIndices.Add(hexagon);

                    // Alternate roles but only for faces with an even number of sides
                    if (i % 2 == 0 || face.Sides % 2 != 0)
                    {
                        faceRoles.Add(Roles.New);
                    }
                    else
                    {
                        faceRoles.Add(Roles.NewAlt);
                    }

                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    centralFace[i] = newInnerVertices[edge.Name.ToString()];
                    edge = edge.Next;
                }

                faceIndices.Add(centralFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Volute(OpParams o)
        {
            return Whirl(o).Dual();
        }

        public PolyMesh Meta(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
            var newCenterVertices = new Dictionary<string, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                var vert = Vertices[vertexIndex];
                float offset2 = o.GetValueB(this, vertexIndex);
                vertexPoints.Add(vert.Position + vert.Normal * offset2);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vert.Position] = vertexIndex;
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float offset = o.GetValueA(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];

                var face = Faces[faceIndex];
                var centroid = face.Centroid;

                vertexPoints.Add(centroid + face.Normal * offset);
                vertexRoles.Add(Roles.Existing);
                newCenterVertices[face.Name] = vertexPoints.Count - 1;

                var edges = face.GetHalfedges();

                for (int j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j];

                    if (!newEdgeVertices.ContainsKey(edge.PairedName))
                    {
                        vertexPoints.Add(edge.Midpoint);
                        vertexRoles.Add(Roles.New);
                        newEdgeVertices[edge.PairedName] = vertexPoints.Count - 1;
                    }
                }

                for (int j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j];

                    var edgeFace1 = new List<int>
                    {
                        existingVertices[edge.Vertex.Position],
                        newCenterVertices[face.Name],
                        newEdgeVertices[edge.PairedName],
                    };
                    faceIndices.Add(edgeFace1);
                    faceRoles.Add(Roles.New);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    var edgeFace2 = new List<int>
                    {
                        newEdgeVertices[edge.PairedName],
                        newCenterVertices[face.Name],
                        existingVertices[edge.Prev.Vertex.Position],
                    };
                    faceIndices.Add(edgeFace2);
                    faceRoles.Add(Roles.NewAlt);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Cross(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
            var newInnerVertices = new Dictionary<(Guid, Guid)?, int>();
            var newCentroidVertices = new Dictionary<string, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vertexPoints[i]] = i;
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float amount = o.GetValueA(this, faceIndex);
                amount = amount * 0.5f + 0.5f;

                var face = Faces[faceIndex];
                var prevFaceTagSet = FaceTags[faceIndex];
                var centroid = face.Centroid;

                vertexPoints.Add(centroid);
                vertexRoles.Add(Roles.Existing);
                newCentroidVertices[face.Name] = vertexPoints.Count - 1;

                var edges = face.GetHalfedges();
                for (int j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j];

                    vertexPoints.Add(centroid - (centroid - edge.Vertex.Position) * amount);
                    vertexRoles.Add(Roles.NewAlt);
                    newInnerVertices[edge.Name] = vertexPoints.Count - 1;

                    if (!newEdgeVertices.ContainsKey(edge.PairedName))
                    {
                        vertexPoints.Add(edge.Midpoint);
                        vertexRoles.Add(Roles.New);
                        newEdgeVertices[edge.PairedName] = vertexPoints.Count - 1;
                    }
                }

                for (int j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j];

                    var innerFace = new List<int>
                    {
                        newCentroidVertices[face.Name],
                        newInnerVertices[edge.Prev.Name],
                        newEdgeVertices[edge.PairedName],
                        newInnerVertices[edge.Name],
                    };
                    faceIndices.Add(innerFace);
                    faceRoles.Add((j % 2) == 1 && (edges.Count % 2) == 0 ? Roles.ExistingAlt : Roles.Existing);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    var edgeFace1 = new List<int>
                    {
                        existingVertices[edge.Vertex.Position],
                        newInnerVertices[edge.Name],
                        newEdgeVertices[edge.PairedName],
                    };
                    faceIndices.Add(edgeFace1);
                    faceRoles.Add(Roles.New);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    var edgeFace2 = new List<int>
                    {
                        newEdgeVertices[edge.PairedName],
                        newInnerVertices[edge.Prev.Name],
                        existingVertices[edge.Prev.Vertex.Position],
                    };
                    faceIndices.Add(edgeFace2);
                    faceRoles.Add(Roles.NewAlt);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Squall(OpParams o, bool join = true)
        {
            var newFaceTags = new List<HashSet<string>>();

            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
            var newInnerVertices = new Dictionary<string, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            if (!join)
            {
                for (var i = 0; i < Vertices.Count; i++)
                {
                    vertexPoints.Add(Vertices[i].Position);
                    vertexRoles.Add(Roles.Existing);
                    existingVertices[vertexPoints[i]] = i;
                }
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var prevFaceTagSet = FaceTags[faceIndex];
                var centroid = face.Centroid;

                var edges = face.GetHalfedges();
                if (edges.Count<3) continue;

                for (int j = 0; j < edges.Count; j++)
                {
                    float amount = o.GetValueA(this, faceIndex);
                    var edge = edges[j];

                    vertexPoints.Add(Vector3.LerpUnclamped(centroid, edge.Vertex.Position, amount / 2f));
                    vertexRoles.Add(Roles.NewAlt);
                    newInnerVertices[face.Name + edge.Vertex.Name] = vertexPoints.Count - 1;

                    if (!newEdgeVertices.ContainsKey(edge.PairedName))
                    {
                        vertexPoints.Add(edge.Midpoint);
                        vertexRoles.Add(Roles.New);
                        newEdgeVertices[edge.PairedName] = vertexPoints.Count - 1;
                    }
                }

                for (int j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j];

                    var innerFace = new List<int>
                    {
                        newInnerVertices[face.Name + edge.Vertex.Name],
                        newInnerVertices[face.Name + edge.Prev.Vertex.Name],
                        newEdgeVertices[edge.PairedName],
                    };
                    faceIndices.Add(innerFace);
                    faceRoles.Add(Roles.New);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    if (!join)
                    {
                        var vertexFace = new List<int>
                        {
                            existingVertices[edge.Vertex.Position],
                            newEdgeVertices[edge.Next.PairedName],
                            newInnerVertices[face.Name + edge.Vertex.Name],
                            newEdgeVertices[edge.PairedName],
                        };
                        faceIndices.Add(vertexFace);
                        faceRoles.Add(Roles.NewAlt);
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }
                }

                var existingFace = new List<int>();
                for (int j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j];
                    existingFace.Add(newInnerVertices[face.Name + edge.Vertex.Name]);
                }

                faceIndices.Add(existingFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            if (join)
            {
                for (var vertIndex = 0; vertIndex < Vertices.Count; vertIndex++)
                {
                    var vertex = Vertices[vertIndex];
                    var vertexFace = new List<int>();
                    var edges = vertex.Halfedges;
                    if (edges.Count<3) continue;

                    for (int j = 0; j < edges.Count; j++)
                    {
                        var edge = vertex.Halfedges[j];
                        vertexFace.Add(newInnerVertices[edge.Face.Name + vertex.Name]);
                        vertexFace.Add(newEdgeVertices[edge.PairedName]);
                    }

                    faceIndices.Add(vertexFace);
                    faceRoles.Add(Roles.NewAlt);

                    var vertexFaceIndices = vertex.GetVertexFaces().Select(f => Faces.IndexOf(f));
                    var existingTagSets = vertexFaceIndices.Select(fi => FaceTags[fi]);
                    var newFaceTagSet = existingTagSets.Aggregate(new HashSet<string>(), (rs, i) =>
                    {
                        rs.UnionWith(i);
                        return rs;
                    });
                    newFaceTags.Add(newFaceTagSet);
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Ortho3(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();

            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newEdgeVertices = new Dictionary<string, int>();
            var newNakedEdgeVertices = new Dictionary<(Guid, Guid)?, int>();
            var newInnerVertices = new Dictionary<string, int>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var i = 0; i < Vertices.Count; i++)
            {
                vertexPoints.Add(Vertices[i].Position);
                vertexRoles.Add(Roles.Existing);
                existingVertices[vertexPoints[i]] = i;
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var centroid = face.Centroid;
                var edges = face.GetHalfedges();

                for (int j = 0; j < edges.Count; j++)
                {
                    float amount = o.GetValueA(this, faceIndex);
                    float edgeAmount = o.GetValueB(this, faceIndex) / 2f;
                    float offset = 0; // Sigh. Need more params
                    var edge = edges[j];


                    var offsetCentroid = offset > 0 ? centroid + (face.Normal * offset) : centroid;
                    var newVert = Vector3.LerpUnclamped(offsetCentroid, edge.Vertex.Position, amount);
                    vertexPoints.Add(newVert);
                    vertexRoles.Add(Roles.NewAlt);
                    newInnerVertices[face.Name + edge.Vertex.Name] = vertexPoints.Count - 1;

                    if (!newEdgeVertices.ContainsKey(face.Name + edge.Vertex.Name))
                    {
                        vertexPoints.Add(edge.PointAlongEdge(edgeAmount));
                        vertexRoles.Add(Roles.New);
                        newEdgeVertices[face.Name + edge.Vertex.Name] = vertexPoints.Count - 1;
                    }

                    if (edge.Pair == null)
                    {
                        if (!newNakedEdgeVertices.ContainsKey(edge.Name))
                        {
                            vertexPoints.Add(edge.PointAlongEdge(1f - edgeAmount));
                            vertexRoles.Add(Roles.New);
                            newNakedEdgeVertices[edge.Name] = vertexPoints.Count - 1;
                        }
                    }
                    else
                    {
                        if (!newEdgeVertices.ContainsKey(face.Name + edge.Vertex.Name))
                        {
                            vertexPoints.Add(edge.TwoThirdsPoint);
                            vertexRoles.Add(Roles.New);
                            newEdgeVertices[face.Name + edge.Pair.Vertex.Name] = vertexPoints.Count - 1;
                        }
                    }
                }
            }

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var prevFaceTagSet = FaceTags[faceIndex];

                var edges = face.GetHalfedges();
                if (edges.Count<3) continue;

                for (int j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j];


                    List<int> innerFace;
                    if (edge.Pair == null)
                    {
                        innerFace = new List<int>
                        {
                            newInnerVertices[face.Name + edge.Vertex.Name],
                            newInnerVertices[face.Name + edge.Prev.Vertex.Name],
                            newNakedEdgeVertices[edge.Name],
                            newEdgeVertices[face.Name + edge.Vertex.Name],
                        };
                        faceIndices.Add(innerFace);
                        faceRoles.Add(Roles.New);
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }
                    else
                    {
                        innerFace = new List<int>
                        {
                            newInnerVertices[face.Name + edge.Vertex.Name],
                            newInnerVertices[face.Name + edge.Prev.Vertex.Name],
                            newEdgeVertices[edge.Pair.Face.Name + edge.Pair.Vertex.Name],
                            newEdgeVertices[face.Name + edge.Vertex.Name],
                        };
                        faceIndices.Add(innerFace);
                        faceRoles.Add(Roles.New);
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }

                    List<int> vertexFace;
                    if (edge.Next.Pair == null)
                    {
                        vertexFace = new List<int>
                        {
                            existingVertices[edge.Vertex.Position],
                            newNakedEdgeVertices[edge.Next.Name],
                            newInnerVertices[face.Name + edge.Vertex.Name],
                            newEdgeVertices[face.Name + edge.Vertex.Name],
                        };
                    }
                    else
                    {
                        vertexFace = new List<int>
                        {
                            existingVertices[edge.Vertex.Position],
                            newEdgeVertices[face.Name + edge.Vertex.Name],
                            newInnerVertices[face.Name + edge.Vertex.Name],
                            newEdgeVertices[edge.Next.Pair.Face.Name + edge.Next.Pair.Vertex.Name],
                        };
                    }

                    faceIndices.Add(vertexFace);
                    faceRoles.Add(Roles.NewAlt);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }

                var existingFace = new List<int>();
                for (int j = 0; j < edges.Count; j++)
                {
                    var edge = edges[j];
                    existingFace.Add(newInnerVertices[face.Name + edge.Vertex.Name]);
                }

                faceIndices.Add(existingFace);
                faceRoles.Add(Roles.Existing);
                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh Yank(OpParams o)
        {
            var result = Kis(o);
            result = result.Dual();
            result = result.Kis(o);
            result = result.Dual();
            return result;
        }

        public PolyMesh Exalt(OpParams o)
        {
            // TODO return a correct VertexRole array
            // I suspect the last vertices map to the original shape verts
            var result = Dual();
            result = result.Kis(o);
            result = result.Dual();
            result = result.Kis(o);
            return result;
        }

        public PolyMesh Gable(OpParams o, int edgeOffset=0)
        {
            var newFaceTags = new List<HashSet<string>>();
            // vertices and faces to vertices
            var vertexRoles = Enumerable.Repeat(Roles.Existing, Vertices.Count).ToList();
            List<Vector3> vertexPoints = Vertices.Select(v => v.Position).ToList();

            var faceRoles = new List<Roles>();

            // vertex lookup
            var vlookup = new Dictionary<Guid, int>();
            int n = Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                vlookup.Add(Vertices[i].Name, i);
            }

            // create new tri-faces (like a fan)
            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var prevFaceTagSet = FaceTags[faceIndex];
                bool includeFace = IncludeFace(faceIndex, o.filter);
                var face = Faces[faceIndex];
                if (includeFace && face.Sides % 2 == 0)  // Only even sided faces
                {
                    var edges = face.GetHalfedges();
                    int initialEdgeIndex = edgeOffset;
                    var initialEdge = edges[initialEdgeIndex];
                    int oppositeEdgeIndex = (edgeOffset + (face.Sides / 2) % face.Sides);
                    var oppositeEdge = edges[oppositeEdgeIndex];

                    float amount = o.GetValueA(this, faceIndex);
                    float offset = o.GetValueB(this, faceIndex);
                    var normalOffset = face.Normal * offset;
                    var offsetCentroid = face.Centroid + normalOffset;
                    var initialNewVertPos = Vector3.LerpUnclamped(offsetCentroid, initialEdge.Midpoint + normalOffset, amount);
                    var oppositeNewVertPos = Vector3.LerpUnclamped(offsetCentroid, oppositeEdge.Midpoint + normalOffset, amount);
                    vertexPoints.Add(initialNewVertPos);
                    vertexRoles.Add(Roles.New);
                    int initialVertIndex = vertexPoints.Count - 1;
                    vertexPoints.Add(oppositeNewVertPos);
                    vertexRoles.Add(Roles.New);
                    int oppositeVertIndex = vertexPoints.Count - 1;

                    faceIndices.Add(
                        new[]
                        {
                            vlookup[initialEdge.Prev.Vertex.Name],
                            vlookup[initialEdge.Vertex.Name],
                            initialVertIndex
                        }
                    );
                    faceRoles.Add(Roles.NewAlt);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    faceIndices.Add(
                        new[]
                        {
                            vlookup[oppositeEdge.Prev.Vertex.Name],
                            vlookup[oppositeEdge.Vertex.Name],
                            oppositeVertIndex
                        }
                    );
                    faceRoles.Add(Roles.NewAlt);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    var newFace1 = new List<int>();
                    for (var edgeIndex = initialEdgeIndex; edgeIndex != oppositeEdgeIndex; edgeIndex=(edgeIndex+1)%edges.Count)
                    {
                        var edge = edges[edgeIndex];
                        newFace1.Add(vlookup[edge.Vertex.Name]);
                    }
                    newFace1.Add(oppositeVertIndex);
                    newFace1.Add(initialVertIndex);
                    faceIndices.Add(newFace1);
                    faceRoles.Add(Roles.New);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    var newFace2 = new List<int>();
                    for (var edgeIndex = oppositeEdgeIndex; edgeIndex != initialEdgeIndex; edgeIndex=(edgeIndex+1)%edges.Count)
                    {
                        var edge = edges[edgeIndex];
                        newFace2.Add(vlookup[edge.Vertex.Name]);
                    }
                    newFace2.Add(initialVertIndex);
                    newFace2.Add(oppositeVertIndex);
                    faceIndices.Add(newFace2);
                    faceRoles.Add(Roles.New);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
                else
                {
                    faceIndices.Add(ListFacesByVertexIndices()[faceIndex]);
                    faceRoles.Add(Roles.Ignored);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }

        public PolyMesh SplitFaces(OpParams o)
        {
            var newFaceTags = new List<HashSet<string>>();
            // vertices and faces to vertices
            var vertexRoles = Enumerable.Repeat(Roles.Existing, Vertices.Count).ToList();
            List<Vector3> vertexPoints = Vertices.Select(v => v.Position).ToList();

            var faceRoles = new List<Roles>();

            // vertex lookup
            var vlookup = new Dictionary<Guid, int>();
            int n = Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                vlookup.Add(Vertices[i].Name, i);
            }

            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                int vertexOffset = Mathf.FloorToInt(Mathf.Lerp(0, face.Sides, o.GetValueA(this, faceIndex) % 0.5f));
                var edges = face.GetHalfedges();
                int oppositeVertexIndex = (vertexOffset + (face.Sides / 2) % face.Sides);
                var prevFaceTagSet = FaceTags[faceIndex];
                bool includeFace = IncludeFace(faceIndex, o.filter);
                if (includeFace && face.Sides % 2 == 0 )  // Only even sided faces > 4
                {
                    var newFace1 = new List<int>();
                    for (var edgeIndex = vertexOffset; edgeIndex != oppositeVertexIndex; edgeIndex=(edgeIndex+1)%face.Sides)
                    {
                        var edge = edges[edgeIndex];
                        newFace1.Add(vlookup[edge.Vertex.Name]);
                    }
                    newFace1.Add(vlookup[edges[oppositeVertexIndex].Vertex.Name]);
                    faceIndices.Add(newFace1);
                    faceRoles.Add(Roles.New);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                    var newFace2 = new List<int>();
                    for (var edgeIndex = oppositeVertexIndex; edgeIndex != vertexOffset; edgeIndex=(edgeIndex+1)%face.Sides)
                    {
                        var edge = edges[edgeIndex];
                        newFace2.Add(vlookup[edge.Vertex.Name]);
                    }
                    newFace2.Add(vlookup[edges[vertexOffset].Vertex.Name]);
                    faceIndices.Add(newFace2);
                    faceRoles.Add(Roles.NewAlt);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
                else
                {
                    faceIndices.Add(ListFacesByVertexIndices()[faceIndex]);
                    faceRoles.Add(Roles.Ignored);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
        }
    }
}