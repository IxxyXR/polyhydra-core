// Copyright 2020 The Blocks Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Polyhydra.core.csg.util;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Polyhydra.core.csg.core
{
    /// <summary>
    ///   Structure for holding a pair of edges and the separation between the edges.
    /// </summary>
    public struct EdgePair
    {
        internal float separation;
        internal EdgeInfo fromEdge;
        internal EdgeInfo toEdge;
    }

    /// <summary>
    ///   Structure for holding a pair of faces and the separation between the faces.
    /// </summary>
    public struct FacePair
    {
        internal float separation;
        internal float angle;
        internal FaceKey fromFaceKey;
        internal FaceKey toFaceKey;
        internal Vector3 fromFaceModelSpaceCenter;
        internal Vector3 toFaceModelSpaceCenter;
    }

    /// <summary>
    ///   Structure for holding a vertex and a face and the separation between them.
    /// </summary>
    public struct FaceVertexPair
    {
        internal float separation;
        internal VertexKey vertexKey;
        internal FaceKey faceKey;
    }

    /// <summary>
    ///   Math associated with meshes, faces and vertices.
    /// </summary>
    public class MeshMath
    {
        /// <summary>
        ///   Calculates a normal for a clockwise list of coplanar vertices.
        ///   This code is directly copied beneath to avoid an expensive Select.
        /// </summary>
        /// <param name="coplanarVertices">A clockwise list of Vertex objects.</param>
        /// <returns>The normal for this list of vertices.</returns>
        public static Vector3 CalculateNormal(List<Vertex> vertices)
        {
            if (vertices.Count == 0) return Vector3.zero;
            // This uses Newell's method, which is proven to generate correct normal for any polygon.
            Vector3 normal = Vector3.zero;
            int count = vertices.Count;
            Vector3 thisPos = vertices[0].loc;
            Vector3 nextPos;
            for (int i = 0, next = 1; i < count; i++, next++)
            {
                // Note: this is cheaper than computing "next % count" at each iteration.
                next = (next == count) ? 0 : next;
                nextPos = vertices[next].loc;
                normal.x += (thisPos.y - nextPos.y) * (thisPos.z + nextPos.z);
                normal.y += (thisPos.z - nextPos.z) * (thisPos.x + nextPos.x);
                normal.z += (thisPos.x - nextPos.x) * (thisPos.y + nextPos.y);
                thisPos = nextPos;
            }
            return Math3d.Normalize(normal);
        }

        /// <summary>
        ///   Calculates a normal for a clockwise list of coplanar vertices.
        ///   This code is directly copied from above to avoid an expensive Select.
        /// </summary>
        /// <param name="coplanarVertices">A clockwise list of Vertex objects.</param>
        /// <returns>The normal for this list of vertices.</returns>
        public static Vector3 CalculateNormal(List<Vector3> vertices)
        {
            if (vertices.Count == 0) return Vector3.zero;
            // This uses Newell's method, which is proven to generate correct normal for any polygon.
            Vector3 normal = Vector3.zero;
            int count = vertices.Count;
            Vector3 thisPos = vertices[0];
            Vector3 nextPos;
            for (int i = 0, next = 1; i < count; i++, next++)
            {
                // Note: this is cheaper than computing "next % count" at each iteration.
                next = (next == count) ? 0 : next;
                nextPos = vertices[next];
                normal.x += (thisPos.y - nextPos.y) * (thisPos.z + nextPos.z);
                normal.y += (thisPos.z - nextPos.z) * (thisPos.x + nextPos.x);
                normal.z += (thisPos.x - nextPos.x) * (thisPos.y + nextPos.y);
                thisPos = nextPos;
            }
            return Math3d.Normalize(normal);
        }

        /// <summary>
        /// Calculates a normal from a clockwise wound array of vertices,
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="verticesById"></param>
        /// <returns></returns>
        public static Vector3 CalculateNormal(ReadOnlyCollection<int> vertices, Dictionary<int, Vertex> verticesById)
        {
            if (vertices.Count == 0) return Vector3.zero;
            // This uses Newell's method, which is proven to generate correct normal for any polygon.
            Vector3 normal = Vector3.zero;
            int count = vertices.Count;
            Vector3 thisPos = verticesById[vertices[0]].loc;
            Vector3 nextPos;
            for (int i = 0, next = 1; i < count; i++, next++)
            {
                // Note: this is cheaper than computing "next % count" at each iteration.
                next = (next == count) ? 0 : next;
                nextPos = verticesById[vertices[next]].loc;
                normal.x += (thisPos.y - nextPos.y) * (thisPos.z + nextPos.z);
                normal.y += (thisPos.z - nextPos.z) * (thisPos.x + nextPos.x);
                normal.z += (thisPos.x - nextPos.x) * (thisPos.y + nextPos.y);
                thisPos = nextPos;
            }
            return Math3d.Normalize(normal);
        }

        /// <summary>
        ///   Calculates a normal for a clockwise list of coplanar vertices.
        ///   This code is directly copied from above to avoid an expensive Select.
        /// </summary>
        /// <param name="coplanarVertices">A clockwise list of Vertex objects.</param>
        /// <returns>The normal for this list of vertices.</returns>
        public static List<Vector3> CalculateNormals(List<Vector3> vertices, List<List<int>> indices)
        {
            List<Vector3> outList = new List<Vector3>();
            if (vertices.Count == 0) return outList;

            for (int faceIndex = 0; faceIndex < indices.Count; faceIndex++)
            {
                // This uses Newell's method, which is proven to generate correct normal for any polygon.
                Vector3 normal = Vector3.zero;
                int count = indices[faceIndex].Count;
                Vector3 thisPos = vertices[indices[faceIndex][0]];
                Vector3 nextPos;
                for (int i = 0, next = 1; i < count; i++, next++)
                {
                    // Note: this is cheaper than computing "next % count" at each iteration.
                    next = (next == count) ? 0 : next;
                    nextPos = vertices[indices[faceIndex][next]];
                    normal.x += (thisPos.y - nextPos.y) * (thisPos.z + nextPos.z);
                    normal.y += (thisPos.z - nextPos.z) * (thisPos.x + nextPos.x);
                    normal.z += (thisPos.x - nextPos.x) * (thisPos.y + nextPos.y);
                    thisPos = nextPos;
                }
                outList.Add(Math3d.Normalize(normal));
            }
            return outList;
        }

        /// <summary>
        ///   Calculates a normal for a clockwise list of coplanar vertices.
        ///   This code is directly copied from above to avoid an expensive Select.
        /// </summary>
        /// <param name="coplanarVertices">A clockwise list of Vertex objects.</param>
        /// <returns>The normal for this list of vertices.</returns>
        public static List<Vector3> CalculateNormals(List<Vertex> vertices, List<List<int>> indices)
        {
            List<Vector3> outList = new List<Vector3>();
            if (vertices.Count == 0) return outList;

            for (int faceIndex = 0; faceIndex < indices.Count; faceIndex++)
            {
                // This uses Newell's method, which is proven to generate correct normal for any polygon.
                Vector3 normal = Vector3.zero;
                int count = indices[faceIndex].Count;
                Vector3 thisPos = vertices[indices[faceIndex][0]].loc;
                Vector3 nextPos;
                for (int i = 0, next = 1; i < count; i++, next++)
                {
                    // Note: this is cheaper than computing "next % count" at each iteration.
                    next = (next == count) ? 0 : next;
                    nextPos = vertices[indices[faceIndex][next]].loc;
                    normal.x += (thisPos.y - nextPos.y) * (thisPos.z + nextPos.z);
                    normal.y += (thisPos.z - nextPos.z) * (thisPos.x + nextPos.x);
                    normal.z += (thisPos.x - nextPos.x) * (thisPos.y + nextPos.y);
                    thisPos = nextPos;
                }
                outList.Add(Math3d.Normalize(normal));
            }
            return outList;
        }

        /// <summary>
        ///   Calculates a normal for a clockwise list of coplanar vertices.
        ///   This code is directly copied from above to avoid an expensive Select.
        /// </summary>
        /// <param name="coplanarVertices">A clockwise list of Vertex objects.</param>
        /// <returns>The normal for this list of vertices.</returns>
        public static List<Vector3> CalculateNormals(Dictionary<int, Vertex> vertices, List<List<int>> indices)
        {
            List<Vector3> outList = new List<Vector3>();
            if (vertices.Count == 0) return outList;

            for (int faceIndex = 0; faceIndex < indices.Count; faceIndex++)
            {
                // This uses Newell's method, which is proven to generate correct normal for any polygon.
                Vector3 normal = Vector3.zero;
                int count = indices[faceIndex].Count;
                Vector3 thisPos = vertices[indices[faceIndex][0]].loc;
                Vector3 nextPos;
                for (int i = 0, next = 1; i < count; i++, next++)
                {
                    // Note: this is cheaper than computing "next % count" at each iteration.
                    next = (next == count) ? 0 : next;
                    nextPos = vertices[indices[faceIndex][next]].loc;
                    normal.x += (thisPos.y - nextPos.y) * (thisPos.z + nextPos.z);
                    normal.y += (thisPos.z - nextPos.z) * (thisPos.x + nextPos.x);
                    normal.z += (thisPos.x - nextPos.x) * (thisPos.y + nextPos.y);
                    thisPos = nextPos;
                }
                outList.Add(normal.normalized);
            }
            return outList;
        }

        /// <summary>
        ///   Calculates a normal for three vertices given in clockwise order.
        /// </summary>
        /// <param name="v1">First vertex.</param>
        /// <param name="v2">Second vertex.</param>
        /// <param name="v3">Third vertex.</param>
        /// <returns>The normal for the given vertices.</returns>
        public static Vector3 CalculateNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            // Note: we scale the vectors by 1000 before calculating the cross product because the vectors might be really
            // tiny, so the cross product and normalization might run into floating point errors causing the result
            // to be zero (bug). Pre-scaling the vectors by 1000 is mathematically equivalent, as we're normalizing
            // anyway.
            return Vector3.Cross((v1 - v2) * 1000f, (v1 - v3) * 1000f).normalized;
        }

        /// <summary>
        /// Tests whether a point p is on a coplanar convex face.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="faceNormal">The normal to the face (this is a flat face, so one normal)</param>
        /// <param name="faceVertices">The coplanar vertices of the face.</param>
        /// <param name="faceClosenessThreshold">How close we must be to the face.</param>
        /// <param name="vertexDistanceThreshold">How far we must be from the vertex.</param>
        /// <returns>True if we are close, false if we're not close.</returns>
        public static bool IsCloseToFaceInterior(Vector3 point, Vector3 faceNormal,
            List<Vector3> faceVertices, float faceClosenessThreshold, float vertexDistanceThreshold)
        {
            // Don't accept points that are too close to vertices as being 'close to a face'.
            foreach (Vector3 vertex in faceVertices)
            {
                if (Vector3.Distance(vertex, point) < vertexDistanceThreshold)
                {
                    return false;
                }
            }

            // Short-circuit where we know a point is too far from a face.
            if (Vector3.Distance(Vector3.Project(point, faceNormal), Vector3.Project(faceVertices[0], faceNormal))
              > faceClosenessThreshold)
            {
                return false;
            }

            Vector3 prev = faceVertices[faceVertices.Count - 1];
            for (int i = 0; i < faceVertices.Count; i++)
            {
                Vector3 vertex = faceVertices[i];
                Vector3 edge = vertex - prev;
                Vector3 normal = Vector3.Cross(faceNormal, edge);
                float min = float.MaxValue;
                float max = float.MinValue;
                foreach (Vector3 faceVertex in faceVertices)
                {
                    float dot = Vector3.Dot(faceVertex, normal);
                    if (dot < min)
                    {
                        min = dot;
                    }
                    if (dot > max)
                    {
                        max = dot;
                    }
                }
                float pointDot = Vector3.Dot(point, normal);
                if (pointDot < min || pointDot > max)
                {
                    return false;
                }
                prev = vertex;
            }
            return true;
        }

        public static Vector3 CalculateGeometricCenter(List<Vector3> coplanarVertices)
        {
            List<Vector3> cornerVertices = FindCornerVertices(coplanarVertices);
            return cornerVertices.Aggregate(Vector3.zero, (sum, vec) => sum + vec) / cornerVertices.Count;
        }

        /// <summary>
        ///   Takes a set of bounds and returns a bounds that encapsulates all of them.
        /// </summary>
        /// <param name="bounds">The bounds to be encapsulated.</param>
        /// <returns>A bounds that encapsulates all the passed bounds.</returns>
        public static Bounds FindEncapsulatingBounds(IEnumerable<Bounds> bounds)
        {
            // You have to start off with a bounds to encapsulate other bounds together. The first iteration of the for loop
            // will encapsulate the first bounds again but it won't change the outcome of encapsulating bounds.
            Bounds encapsulatingBounds = bounds.First();

            foreach (Bounds bound in bounds)
            {
                encapsulatingBounds.Encapsulate(bound);
            }

            return encapsulatingBounds;
        }

        /// <summary>
        ///   Finds the edge bisectors in clockwise order around a face.
        /// </summary>
        /// <param name="coplanarVertices">Clockwise vertices representing a face.</param>
        /// <returns>Clockwise positions of edge bisector points.</returns>
        public static List<Vector3> CalculateEdgeBisectors(IEnumerable<Vector3> coplanarVertices)
        {
            List<Vector3> edgeBisectors = new List<Vector3>();

            // Find all the edge centers by iterating through the vertices which are stored clockwise.
            for (int index = 0; index < coplanarVertices.Count(); index++)
            {
                Vector3 v1 = coplanarVertices.ElementAt(index);
                Vector3 v2 = (index + 1 == coplanarVertices.Count()) ? coplanarVertices.ElementAt(0) :
                  coplanarVertices.ElementAt(index + 1);

                // Add the edge bisectors to verticesAndEdgeBisectors.
                edgeBisectors.Add((v1 + v2) / 2.0f);
            }

            return edgeBisectors;
        }

        /// <summary>
        ///   Takes a list of coplanarVertices representing a face and removes any extraneous colinear vertices along the
        ///   edges returning only corner vertices.
        /// </summary>
        /// <param name="coplanarVertices">The list of clockwise coplanar vertices representing the face.</param>
        /// <returns>A list of clockwise coplanar vertices where no 3 vertices are colinear.</returns>
        public static List<Vector3> FindCornerVertices(List<Vector3> coplanarVertices)
        {
            // Start by populating the cornerVertices with all coplanarVertices. We will remove colinear ones as we iterate.
            int numElements = coplanarVertices.Count;

            if (numElements < 3)
            {
                // Nothing to do here, return a copy.
                return new List<Vector3>(coplanarVertices);
            }

            // Iterate through the coplanarVertices checking every Every vertex needs to be the middle vertex once. If the
            // colinear check returns true we remove the middle vertex from the set of cornerVertices.
            // Given that we are building a list, we need to ensure we preserve its order.
            List<Vector3> cornerVertices = new List<Vector3>(numElements);
            Vector3 previous = coplanarVertices[numElements - 1];
            Vector3 current = coplanarVertices[0];
            Vector3 next;
            for (int i = 0; i < numElements; i++)
            {
                next = coplanarVertices[(i + 1) % numElements];

                if (!Math3d.AreColinear(previous, current, next))
                {
                    cornerVertices.Add(current);
                }

                previous = current;
                current = next;
            }

            return cornerVertices;
        }

        /// <summary>
        ///   Finds which edge in a face a position is closest to by checking how far the position is from every edge in
        ///   the face.
        /// </summary>
        /// <param name="position">The position we want to find the closest edge to.</param>
        /// <param name="coplanarVertices">The clockwise vertices representing the face.</param>
        /// <returns>The two vertexIds that make the closest edge.</returns>
        public static KeyValuePair<Vector3, Vector3> FindClosestEdgeInFace(Vector3 position,
          IEnumerable<Vector3> coplanarVertices)
        {
            float closestDistance = Mathf.Infinity;
            KeyValuePair<Vector3, Vector3> closestEdge = new KeyValuePair<Vector3, Vector3>();

            // Check each edge. Every vertex in vertexIds should be the first vertex on one edge to iterate through every
            // edge we can iterate through the vertices which are stored in Face.vertexId in clockwise order and make this
            // vertex the first and the subsequent vertex the second one for the edge. When we hit the end of the list the
            // second vertex is the first one in the list.
            for (int index = 0; index < coplanarVertices.Count(); index++)
            {
                // Find the vertices.
                Vector3 v1 = coplanarVertices.ElementAt(index);
                Vector3 v2 = (index + 1 == coplanarVertices.Count()) ?
                  coplanarVertices.ElementAt(0) : coplanarVertices.ElementAt(index + 1);

                // Find the distance from the position to the edge.
                float currentDistance = DistanceFromEdge(position, v1, v2);

                if (currentDistance < closestDistance)
                {
                    closestDistance = currentDistance;
                    closestEdge = new KeyValuePair<Vector3, Vector3>(v1, v2);
                }
            }

            return closestEdge;
        }


        /// <summary>
        ///   Sorts all pairs of edges by increasing separation, where each edge belongs to a different face represented
        ///   by a list of clockwise coplanar vertices.
        /// </summary>
        /// <param name="fromFaceVertices">The coplanar vertices of the first face.</param>
        /// <param name="toFaceVertices">The coplanar vertices of the second face.</param>
        /// <returns>
        ///   A sorted list of edgePair structs which contains each edge and the average distance between them.
        /// </returns>
        public static IEnumerable<EdgePair> FindClosestEdgePairs(IEnumerable<Vector3> fromFaceVertices,
          IEnumerable<Vector3> toFaceVertices)
        {
            List<EdgePair> edgePairs = new List<EdgePair>();

            // Check each edge in fromFaceVertices against each edge in toFaceVertices.
            for (int toIndex = 0; toIndex < toFaceVertices.Count(); toIndex++)
            {
                Vector3 toV1 = toFaceVertices.ElementAt(toIndex);
                Vector3 toV2 = toFaceVertices.ElementAt((toIndex + 1) % toFaceVertices.Count());

                for (int fromIndex = 0; fromIndex < fromFaceVertices.Count(); fromIndex++)
                {
                    Vector3 fromV1 = fromFaceVertices.ElementAt(fromIndex);
                    Vector3 fromV2 = fromFaceVertices.ElementAt((fromIndex + 1) % fromFaceVertices.Count());

                    float separation;
                    bool edgesOverlap = CompareEdges(fromV1, fromV2, toV1, toV2, out separation);

                    if (edgesOverlap)
                    {
                        // Create the EdgePair and add it to the list.
                        EdgeInfo fromEdge = new EdgeInfo();
                        fromEdge.edgeStart = fromV1;
                        fromEdge.edgeVector = fromV2 - fromV1;

                        EdgeInfo toEdge = new EdgeInfo();
                        toEdge.edgeStart = toV1;
                        toEdge.edgeVector = toV2 - toV1;

                        EdgePair edgePair = new EdgePair();
                        edgePair.separation = separation;
                        edgePair.fromEdge = fromEdge;
                        edgePair.toEdge = toEdge;

                        edgePairs.Add(edgePair);
                    }
                }
            }

            // Return edgePairs sorted in ascending order of separation.
            return edgePairs.OrderBy(pair => pair.separation);
        }

        /// <summary>
        ///   Finds the pair of edges with the smallest separation, where each edge belongs to a different face
        ///   represented by a list of clockwise coplanar vertices.
        /// </summary>
        /// <param name="fromFaceVertices">The coplanar vertices of the first face.</param>
        /// <param name="toFaceVertices">The coplanar vertices of the second face.</param>
        /// <param name="closestEdgePair">The closest edge pair.</param>
        /// <returns>
        ///   Whether a closest edge pair was found.
        /// </returns>
        public static bool MaybeFindClosestEdgePair(IEnumerable<Vector3> fromFaceVertices,
          IEnumerable<Vector3> toFaceVertices, out EdgePair closestEdgePair)
        {
            closestEdgePair = new EdgePair();
            float closestSeparation = Mathf.Infinity;

            // Check each edge in fromFaceVertices against each edge in toFaceVertices.
            for (int toIndex = 0; toIndex < toFaceVertices.Count(); toIndex++)
            {
                Vector3 toV1 = toFaceVertices.ElementAt(toIndex);
                Vector3 toV2 = toFaceVertices.ElementAt((toIndex + 1) % toFaceVertices.Count());

                for (int fromIndex = 0; fromIndex < fromFaceVertices.Count(); fromIndex++)
                {
                    Vector3 fromV1 = fromFaceVertices.ElementAt(fromIndex);
                    Vector3 fromV2 = fromFaceVertices.ElementAt((fromIndex + 1) % fromFaceVertices.Count());

                    float separation;
                    bool edgesOverlap = CompareEdges(fromV1, fromV2, toV1, toV2, out separation);

                    if (edgesOverlap && separation < closestSeparation)
                    {
                        closestSeparation = separation;
                        // Create the EdgePair and add it to the list.
                        EdgeInfo fromEdge = new EdgeInfo();
                        fromEdge.edgeStart = fromV1;
                        fromEdge.edgeVector = fromV2 - fromV1;

                        EdgeInfo toEdge = new EdgeInfo();
                        toEdge.edgeStart = toV1;
                        toEdge.edgeVector = toV2 - toV1;

                        closestEdgePair.separation = separation;
                        closestEdgePair.fromEdge = fromEdge;
                        closestEdgePair.toEdge = toEdge; ;
                    }
                }
            }

            // Return edgePairs sorted in ascending order of separation.
            return closestSeparation != Mathf.Infinity;
        }

        /// <summary>
        /// Compares two edges and determines how far apart they are. The separation between edges is calculated by
        /// finding the average distance from each vertex in the first edge at a right angle to the second edge. We only
        /// compare edges if they overlap or if at least one vertex of either edge is inside the other edge.
        ///
        /// See bug for a diagram.
        /// </summary>
        /// <param name="a1">First vertex of an edge a.</param>
        /// <param name="a2">Second vertex of an edge a.</param>
        /// <param name="b1">First vertex of an edge b.</param>
        /// <param name="b2">Second vertex of an edge b.</param>
        /// <param name="separation">How far apart the edges are.</param>
        /// <returns>Whether the edges overlapped.</returns>
        public static bool CompareEdges(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out float separation)
        {
            float distanceA2 = DistanceFromEdge(a2, b1, b2);
            bool a2InsideB = InsideEdge(a2, b1, b2);

            float distanceA1 = DistanceFromEdge(a1, b1, b2);
            bool a1InsideB = InsideEdge(a1, b1, b2);

            // Find the separation which is the sum of the projections of a1 and a2 at 90 degree angles onto edge b.
            separation = (distanceA1 + distanceA2) / 2.0f;

            // Check if either a1 or a2 was inside edge b. If they were we already know the edges are comparable.
            if (a1InsideB || a2InsideB)
            {
                return true;
            }

            // We already know the separation but we still don't know if the edges are actually comparable because a1 and a2
            // were not inside edge b. But we can still compare the edges and use the separation we already calculated if
            // b1 or b2 are inside edge a.
            float distanceB1 = DistanceFromEdge(b1, a1, a2);
            bool b1InsideEdge = InsideEdge(b1, a1, a2);
            float distanceB2 = DistanceFromEdge(b2, a1, a2);
            bool b2InsideEdge = InsideEdge(b2, a1, a2);

            return b1InsideEdge || b2InsideEdge;
        }

        /// <summary>
        /// Finds the perpendicular distance from a position/vertex a to an edge b represented by two vertices b1 and b2.
        /// Determines whether a is inside edge b. A vertex is inside an edge if the triangle formed by all three vertices
        /// doesn't have obtuse angles at the corners defined by the edge's vertices.
        /// </summary>
        /// <param name="a">The position we are trying to find the distance to the edge for.</param>
        /// <param name="b1">First vertex for the edge.</param>
        /// <param name="b2">Second vertex for the edge.</param>
        /// <param name="distance">Perpendicular distance from the position to the edge.</param>
        /// <returns>Whether a is inside edge b.</returns>
        public static float DistanceFromEdge(Vector3 a, Vector3 b1, Vector3 b2)
        {
            // Find the angles of the corners of b2 in triangle ab2b1.
            float thetaAB2B1 = Vector3.Angle(a - b2, b1 - b2) * Mathf.Deg2Rad;

            // Calculate the distance between a and edge b such that the projection of a onto edge b forms a right angle
            // with edge b.
            return Vector3.Distance(a, b2) * Mathf.Sin(Mathf.Min(Mathf.PI - thetaAB2B1, thetaAB2B1)); ;
        }

        /// <summary>
        /// Checks if a given vertex a is inside an edge b. A vertex is inside an edge if the triangle formed by all three
        /// vertices doesn't have obtuse angles at the corners defined by the edge's vertices.
        /// </summary>
        /// <param name="a">The position we are trying to check is inside an edge.</param>
        /// <param name="b1">First vertex for the edge.</param>
        /// <param name="b2">Second vertex for the edge.</param>
        /// <returns>Whether a is inside edge b.</returns>
        public static bool InsideEdge(Vector3 a, Vector3 b1, Vector3 b2)
        {
            // Find the angles of the corners of b1 and b2 in triangle ab2b1.
            float thetaAB1B2 = Vector3.Angle(a - b1, b2 - b1) * Mathf.Deg2Rad;
            float thetaAB2B1 = Vector3.Angle(a - b2, b1 - b2) * Mathf.Deg2Rad;

            // Check that b1 and b2 aren't obtuse angles in triangle a2b2b1.
            return thetaAB1B2 < (Mathf.PI / 2.0f) && thetaAB2B1 < (Mathf.PI / 2.0f);
        }

        /// <summary>
        ///   Finds which edge from a set of coplanarVertices is closest to a given edge.
        /// </summary>
        /// <param name="coplanarVertices">A set of vertices representing edges in clockwise order.</param>
        /// <param name="toEdge">The edge being compared to.</param>
        /// <returns>A vector representation of the closestEdge.</returns>
        public static Vector3 ClosestEdgeToEdge(IEnumerable<Vector3> coplanarVertices, EdgeInfo toEdge)
        {
            // We just want the edge endpoints.
            Vector3 eV1 = toEdge.edgeStart;
            Vector3 eV2 = eV1 + toEdge.edgeVector;

            float closestDistance = Mathf.Infinity;
            Vector3 closestEdge = Vector3.zero;

            // Check each edge. To iterate through the edges we can iterate through the clockwise vertices in coplanar
            // vertices allowing each vertex to be the first vertex in an edge.
            for (int index = 0; index < coplanarVertices.Count(); index++)
            {
                // Find the vertices.
                Vector3 v1 = coplanarVertices.ElementAt(index);
                Vector3 v2 = coplanarVertices.ElementAt((index + 1) % coplanarVertices.Count());

                Vector3 edge = v2 - v1;

                float d1 = DistanceFromEdge(v1, eV1, eV2);
                float d2 = DistanceFromEdge(v2, eV1, eV2);

                float currentDistance = (d1 + d2) / 2.0f;

                if (currentDistance < closestDistance)
                {
                    closestDistance = currentDistance;
                    closestEdge = edge;
                }
            }

            return closestEdge;
        }

        /// <summary>
        ///   Checks to see if N vertices from a set of vertices are on a given mesh.
        /// </summary>
        /// <param name="meshId">The id for the mesh.</param>
        /// <param name="vertexKeys">The set of vertices.</param>
        /// <param name="minSetSize">The minimum number of vertices on the same mesh required to return true.</param>
        /// <returns>True if N vertices from the set are on the mesh.</returns>
        public static bool MultipleNearbyVerticesOnSameMesh(int meshId, IEnumerable<VertexKey> vertexKeys,
          int minSetSize)
        {
            ushort vertexCountOnSameMesh = 0;

            foreach (VertexKey vertexKey in vertexKeys)
            {
                if (meshId == vertexKey.meshId)
                {
                    vertexCountOnSameMesh++;
                    if (vertexCountOnSameMesh >= minSetSize)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   Checks to see if N faces from a set of faces are on the same mesh.
        /// </summary>
        /// <param name="faceKeys">The faces, as a List<DistancePair> for efficiency.</param>
        /// <param name="minSetSize">The minimum number of faces on the same mesh required to return true.</param>
        /// <param name="nearestMeshId">The mesh with the most nearby faces on it.</param>
        /// <returns>True if N faces from the set are on a mesh.</returns>
        public static bool TryFindingNearestMeshGivenNearbyFaces(List<DistancePair<FaceKey>> faces, int minSetSize,
          out int nearestMeshId)
        {
            Dictionary<int, int> faceCountByMeshId = new Dictionary<int, int>();
            int currentMaxCount = 0;
            nearestMeshId = -1;

            foreach (DistancePair<FaceKey> faceKeyPair in faces)
            {
                int meshId = faceKeyPair.value.meshId;

                // Set the current face count for this mesh to 1, or increment it.
                int currentFaceCount = 0;
                faceCountByMeshId.TryGetValue(meshId, out currentFaceCount);
                currentFaceCount++;
                faceCountByMeshId[meshId] = currentFaceCount;

                // Update the current max count if the current mesh has more references.
                if (currentFaceCount > currentMaxCount)
                {
                    currentMaxCount = currentFaceCount;
                    nearestMeshId = meshId;
                }
            }

            return currentMaxCount >= minSetSize;
        }


        /// <summary>
        ///   Compares two faces. Finds the physical separation between the faces as the average distance of each vertex
        ///   in the fromFace to the plane created by the toFace. Also finds the angle between the normals of the face
        ///   which defines flushness. Two faces are defined as flush if they have an angle of 180 degrees between their
        ///   normals.
        /// </summary>
        /// <param name="fromFaceKey">The key of the face we are comparing the difference from, to the other face.</param>
        /// <param name="fromFaceNormal">The normal of the fromFace.</param>
        /// <param name="fromFaceVertices">The coplanar vertices that make up the fromFace.</param>
        /// <param name="toFaceKey">The key of the face we are comparing the difference to, from the other face.</param>
        /// <param name="toFacePlane">The plane defined by the toFace.</param>
        /// <param name="facePair">
        ///   A FairPair containing both faces, their separation and their angle from being flush.
        /// </param>
        /// <returns>
        ///   Whether the faces were comparable. Faces are only comparable if Plane.Raycast() returns true. This happens
        ///   when the face normals have an angle > 90f. Or when they face each other.
        /// </returns>
        public static bool CompareFaces(FaceKey fromFaceKey, Vector3 fromFaceNormal,
          List<Vector3> fromFaceVertices, FaceKey toFaceKey, FaceInfo toFaceInfo, out FacePair facePair)
        {
            Vector3 fromFaceCenter = MeshMath.CalculateGeometricCenter(fromFaceVertices);

            Ray normalRay = new Ray(fromFaceCenter, fromFaceNormal);
            // The length of the Raycast before it enters the plane.
            float intersectionLength;

            // The angle is calculated as the degrees from flush the normals are. Essentially the number of degrees the faces
            // would have to be rotated to be flush. Faces are flush if there is 180 degrees between their normals.
            facePair.angle = Vector3.Angle(fromFaceNormal, toFaceInfo.plane.normal);

            bool normalsPointTowardEachOther = toFaceInfo.plane.Raycast(normalRay, out intersectionLength);
            float estimatedFaceSeparation = Vector3.Distance(fromFaceCenter, toFaceInfo.baryCenter);

            facePair.separation = (intersectionLength + estimatedFaceSeparation) / 2.0f;
            facePair.fromFaceKey = fromFaceKey;
            facePair.toFaceKey = toFaceKey;
            facePair.toFaceModelSpaceCenter = toFaceInfo.baryCenter;
            facePair.fromFaceModelSpaceCenter = fromFaceCenter;

            return normalsPointTowardEachOther;
        }

        /// <summary>
        ///   Finds the average distance of a set of vertices from a plane. We use the signed difference from point to
        ///   plane so that faces that intersect each other are considered closest.
        /// </summary>
        /// <param name="plane">The plane the vertices are separated from.</param>
        /// <param name="vertices">The vertices whose distance from the plane is being calculated.</param>
        /// <returns>The average distance for all vertices.</returns>
        public static float AverageDistanceFromPlane(Plane plane, IEnumerable<Vector3> vertices)
        {
            // Avoid a divide by zero error.
            if (vertices.Count() == 0)
                return 0;

            float sum = 0;
            foreach (Vector3 vertex in vertices)
            {
                // We want the distance from point to plane so we take the inverse.
                sum += -plane.GetDistanceToPoint(vertex);
            }

            return (sum / vertices.Count());
        }


        /// <summary>
        ///   Compares a face and vertex. Calculates the distance from the faces center to the vertex.
        /// </summary>
        /// <param name="faceKey">The key of the face being compared.</param>
        /// <param name="faceCenter">The center of the face being compared.</param>
        /// <param name="vertexKey">The key of the vertex being compared.</param>
        /// <param name="vertex">The position of the vertex being compared.</param>
        /// <returns>A FaceVertexPair with the face, the vertex and their separation.</returns>
        public static FaceVertexPair CompareFaceAndVertex(FaceKey faceKey, Vector3 faceCenter, VertexKey vertexKey,
          Vector3 vertex)
        {
            FaceVertexPair faceVertexPair = new FaceVertexPair();
            // Find the separation between the two as the distance from the vertex to the faces center.
            faceVertexPair.separation = Vector3.Distance(vertex, faceCenter);
            faceVertexPair.vertexKey = vertexKey;
            faceVertexPair.faceKey = faceKey;

            return faceVertexPair;
        }

        /// <summary>
        ///   Finds which edge in a face most represents the other edges. It does this by choosing an edge that is
        ///   perpendicular to the greatest number of other edges.
        /// </summary>
        /// <param name="coplanarFaceVertices">The coplanar vertices representing the face.</param>
        /// <returns>The most representative edge.</returns>
        public static EdgeInfo FindMostRepresentativeEdge(List<Vector3> coplanarFaceVertices)
        {
            // Start by cleaning out any colinear vertices (subedges).
            IEnumerable<Vector3> cornerVertices = FindCornerVertices(coplanarFaceVertices);
            // Create a dictionary that will hold an edge and the number of edges perpendicular to this edge.
            Dictionary<EdgeInfo, int> similarEdges = new Dictionary<EdgeInfo, int>();

            // Iterate through every edge.
            for (int index = 0; index < cornerVertices.Count(); index++)
            {
                // Find the vertices.
                Vector3 v1 = cornerVertices.ElementAt(index);
                Vector3 v2 = cornerVertices.ElementAt((index + 1) % cornerVertices.Count()); ;

                // Find the current edge.
                Vector3 currentEdge = (v2 - v1);

                bool foundSimilarEdge = false;

                // Check if the current edge is perpendicular to one of the edges in similarEdges. Two vectors are
                // perpendicular if their dot product is 0.
                foreach (KeyValuePair<EdgeInfo, int> edges in similarEdges)
                {
                    if (Mathf.Abs(Vector3.Dot(edges.Key.edgeVector, currentEdge.normalized) - 0) < Math3d.EPSILON)
                    {
                        // If they are similar, update the edgeCount and break to avoid iterating through the dictionary.
                        similarEdges[edges.Key] = similarEdges[edges.Key] + 1;
                        foundSimilarEdge = true;
                        break;
                    }
                }

                // If we there was no similar edge, create a new edge entry in similarEdges.
                if (!foundSimilarEdge)
                {
                    EdgeInfo currentEdgeInfo = new EdgeInfo();
                    currentEdgeInfo.edgeVector = currentEdge;
                    currentEdgeInfo.edgeStart = v1;
                    similarEdges[currentEdgeInfo] = 1;
                }
            }

            return similarEdges.OrderByDescending(pair => pair.Value).First().Key;
        }
    }
}
