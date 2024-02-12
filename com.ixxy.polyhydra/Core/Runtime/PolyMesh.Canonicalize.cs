using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Polyhydra.Core
{

    // Double Precision Vector3
    public struct Vector3D
    {
        public double X, Y, Z;

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3D Cross(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(
                v1.Y * v2.Z - v1.Z * v2.Y,
                v1.Z * v2.X - v1.X * v2.Z,
                v1.X * v2.Y - v1.Y * v2.X);
        }

        public static double Dot(Vector3D v1, Vector3D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        // Normalize the vector to unit length
        public Vector3D Normalize()
        {
            double len = Length;
            if (len == 0)
                return new Vector3D(0, 0, 0);
            return new Vector3D(X / len, Y / len, Z / len);
        }

        public double LengthSquared => X * X + Y * Y + Z * Z;

        public double Length => Math.Sqrt(LengthSquared);

        public static Vector3D Zero => new Vector3D(0, 0, 0);

        public static Vector3D operator *(Vector3D v, double scalar)
        {
            return new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vector3D operator *(double scalar, Vector3D v)
        {
            return new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        // Vector addition
        public static Vector3D operator +(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        // Vector subtraction
        public static Vector3D operator -(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        // Unary negation
        public static Vector3D operator -(Vector3D v)
        {
            return new Vector3D(-v.X, -v.Y, -v.Z);
        }

        // Scalar division
        public static Vector3D operator /(Vector3D v, double scalar)
        {
            return new Vector3D(v.X / scalar, v.Y / scalar, v.Z / scalar);
        }

        public static Vector3D Orthogonal(Vector3D v1, Vector3D v2, Vector3D v3)
        {
            Vector3D d1 = v2 - v1;
            Vector3D d2 = v3 - v2;
            return Cross(d1, d2);
        }

        // Find the point where the line v1...v2 is tangent to a sphere at the origin
        public static Vector3D TangentPoint(Vector3D v1, Vector3D v2)
        {
            Vector3D d = v2 - v1;
            return v1 - (Dot(d, v1) / d.LengthSquared) * d;
        }

        // Calculate the distance of line v1...v2 from the origin
        public static double EdgeDist(Vector3D v1, Vector3D v2)
        {
            return TangentPoint(v1, v2).Length;
        }

        // reflect 3vec in unit sphere, spherical reciprocal
        public Vector3D Reciprocal()
        {
            return (1.0 / this.LengthSquared) * this;
        }
    }

    public partial class PolyMesh
    {
        public void SetVertexPositions(List<Vector3> newPositions)
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Position = newPositions[i];
            }
        }

        private static double MAX_VERTEX_CHANGE = 1.0;

        /**
		 * A port of the "reciprocalN" function written by George Hart.
		 *
		 * @param poly The polyhedron to apply this canonicalization to.
		 * @return A list of the new vertices of the dual polyhedron.
		 */
        private static List<Vector3> ReciprocalVertices(PolyMesh poly)
        {
            var newVertices = new List<Vector3>();

            foreach (var face in poly.Faces)
            {
                // Initialize values which will be updated in the loop below
                var centroid = face.Centroid;
                var normalSum = new Vector3();
                double avgEdgeDistance = 0.0;

                // Retrieve the indices of the vertices defining this face
                var faceVertices = face.GetVertices();

                // Keep track of the "previous" two vertices in CCW order
                var lastlastVertex = faceVertices[^2];
                var lastVertex = faceVertices[^1];

                for (var i = 0; i < faceVertices.Count; i++)
                {
                    var vertex = faceVertices[i];
                    // Compute the normal of the plane defined by this vertex and
                    // the previous two
                    var v1 = lastlastVertex.Position;
                    v1 -= lastVertex.Position;
                    var v2 = vertex.Position;
                    v2 -= lastVertex.Position;
                    var normal = Vector3.Cross(v1, v2);
                    normalSum += normal;

                    // Compute distance from edge to origin
                    avgEdgeDistance += PointLineDist(new Vector3(), lastlastVertex.Position, lastVertex.Position);

                    // Update the previous vertices for the next iteration
                    lastlastVertex = lastVertex;
                    lastVertex = vertex;
                }

                normalSum = normalSum.normalized;
                avgEdgeDistance /= faceVertices.Count;

                var resultingVector = new Vector3();
                resultingVector = Vector3.Dot(centroid, normalSum) * normalSum;
                resultingVector *= Mathf.Pow(1.0f / resultingVector.magnitude, 2);
                resultingVector *= (1.0f + (float) avgEdgeDistance) / 2.0f;
                newVertices.Add(resultingVector);
            }

            return newVertices;
        }

        private static double PointLineDist(Vector3 lineDir, Vector3 linePnt, Vector3 pnt)
        {
            lineDir.Normalize(); //this needs to be a unit vector
            var v = pnt - linePnt;
            var d = Vector3.Dot(v, lineDir);
            return (linePnt + lineDir * d).magnitude;
        }

        /**
		 * Modifies a polyhedron's vertices such that faces are closer to planar.
		 * The more iterations, the closer the faces are to planar. If a vertex
		 * moves by an unexpectedly large amount, or if the new vertex position
		 * has an NaN component, the algorithm automatically terminates.
		 *
		 * @param poly          The polyhedron whose faces to planarize.
		 * @param numIterations The number of iterations to planarize for.
		 */
        public static void Planarize(PolyMesh poly, int numIterations)
        {
            var dual = poly.Dual();

            for (int i = 0; i < numIterations; i++)
            {
                var newDualPositions = ReciprocalVertices(poly);
                dual.SetVertexPositions(newDualPositions);
                var newPositions = ReciprocalVertices(dual);

                double maxChange = 0.0;

                for (int j = 0; j < poly.Vertices.Count; j++)
                {
                    var newPos = poly.Vertices[j].Position;
                    var diff = newPos - poly.Vertices[j].Position;
                    maxChange = Math.Max(maxChange, diff.magnitude);
                }

                // Check if an error occurred in computation. If so, terminate
                // immediately. This likely occurs when faces are already planar.
                if (Double.IsNaN(newPositions[0].x) || Double.IsNaN(newPositions[0].y) ||
                    Double.IsNaN(newPositions[0].z))
                {
                    break;
                }

                // Check if the position changed by a significant amount so as to
                // be erroneous. If so, terminate immediately
                if (maxChange > MAX_VERTEX_CHANGE)
                {
                    break;
                }

                poly.SetVertexPositions(newPositions);
            }
        }

        /**
		 * Modifies a polyhedron's vertices such that faces are closer to planar.
		 * When no vertex moves more than the given threshold, the algorithm
		 * terminates.
		 *
		 * @param poly      The polyhedron to canonicalize.
		 * @param threshold The threshold of vertex movement after an iteration.
		 * @return The number of iterations that were executed.
		 */
        public static int Planarize(PolyMesh poly, double threshold)
        {
            return _Canonicalize(poly, threshold, true);
        }

        /**
		 * A port of the "reciprocalC" function written by George Hart. Reflects
		 * the centers of faces across the unit sphere.
		 *
		 * @param poly The polyhedron whose centers to invert.
		 * @return The list of inverted face centers.
		 */
        private static List<Vector3> ReciprocalCenters(PolyMesh poly)
        {
            var faceCenters = new List<Vector3>();

            for (var i = 0; i < poly.Faces.Count; i++)
            {
                var newCenter = poly.Faces[i].Centroid;
                newCenter *= 1.0f / Mathf.Pow(newCenter.magnitude, 2);
                faceCenters.Add(newCenter);
            }

            return faceCenters;
        }

        /**
		 * Canonicalizes a polyhedron by adjusting its vertices iteratively.
		 *
		 * @param poly          The polyhedron whose vertices to adjust.
		 * @param numIterations The number of iterations to adjust for.
		 */
        public static void Adjust(PolyMesh poly, int numIterations, bool OldMethod = false)
        {
            PolyMesh dual = new PolyMesh();
            if (OldMethod)
            {
                dual = poly.Dual();
            }

            for (int i = 0; i < numIterations; i++)
            {
                if (OldMethod)
                {
                    var newDualPositions = ReciprocalCenters(poly);
                    dual.SetVertexPositions(newDualPositions);
                    var newPositions = ReciprocalCenters(dual);
                    poly.SetVertexPositions(newPositions);
                }
                else
                {
                    for (var j = 0; j < poly.Vertices.Count; j++)
                    {
                        var vert = poly.Vertices[j];
                        var edges = vert.Halfedges;
                        // I got quite interesting results skipping edges
                        // But it was more generally useful to not do so
                        // Might investigate further at some point.
                        // nb Checking for edge boundaries is probably better than just doing >=3 
                        // if (edges.Count >= 3)
                        // {
                        var adjoiningFaces = edges.Select(e => e.Face);
                        var sum = Vector3.zero;
                        foreach (var face in adjoiningFaces)
                        {
                            sum += face.Centroid;
                        }

                        var newCenter = sum / edges.Count;
                        newCenter *= 1.0f / Mathf.Pow(newCenter.magnitude, 2);
                        poly.Vertices[j].Position = newCenter;
                        // }
                    }
                }
            }
        }

        /**
		 * Canonicalizes a polyhedron by adjusting its vertices iteratively. When
		 * no vertex moves more than the given threshold, the algorithm terminates.
		 *
		 * @param poly      The polyhedron whose vertices to adjust.
		 * @param threshold The threshold of vertex movement after an iteration.
		 * @return The number of iterations that were executed.
		 */
        public static int Adjust(PolyMesh poly, double threshold)
        {
            return _Canonicalize(poly, threshold, false);
        }

        /**
		 * A helper method for threshold-based termination in both planarizing and
		 * adjusting. If a vertex moves by an unexpectedly large amount, or if the
		 * new vertex position has an NaN component, the algorithm automatically
		 * terminates.
		 *
		 * @param poly      The polyhedron to canonicalize.
		 * @param threshold The threshold of vertex movement after an iteration.
		 * @param planarize True if we are planarizing, false if we are adjusting.
		 * @return The number of iterations that were executed.
		 */
        private static int _Canonicalize(PolyMesh poly, double threshold, bool planarize)
        {
            var dual = poly.Dual();
            var currentPositions = poly.Vertices.Select(x => x.Position).ToList();

            int iterations = 0;

            while (true)
            {
                var newDualPositions = planarize ? ReciprocalVertices(poly) : ReciprocalCenters(poly);
                dual.SetVertexPositions(newDualPositions);
                var newPositions = planarize ? ReciprocalVertices(dual) : ReciprocalCenters(dual);

                double maxChange = 0.0;
                for (int i = 0; i < currentPositions.Count; i++)
                {
                    var newPos = poly.Vertices[i].Position;
                    var diff = newPos - currentPositions[i];
                    maxChange = Math.Max(maxChange, diff.magnitude);
                }

                // Check if an error occurred in computation. If so, terminate
                // immediately
                if (Double.IsNaN(newPositions[0].x) || Double.IsNaN(newPositions[0].y) ||
                    Double.IsNaN(newPositions[0].z))
                {
                    break;
                }

                // Check if the position changed by a significant amount so as to
                // be erroneous. If so, terminate immediately
                if (planarize && maxChange > MAX_VERTEX_CHANGE)
                {
                    break;
                }

                poly.SetVertexPositions(newPositions);

                if (maxChange < threshold)
                {
                    break;
                }

                currentPositions = poly.Vertices.Select(x => x.Position).ToList();
                iterations++;
            }

            return iterations;
        }

        /**
		* Canonicalizes this polyhedron for the given number of iterations.
		* See util.Canonicalize for more details. Performs "adjust" followed
		* by "planarize".
		*
		* @param iterationsAdjust    The number of iterations to "adjust" for.
		* @param iterationsPlanarize The number of iterations to "planarize" for.
		* @return The canonicalized version of this polyhedron.
		*/
        public PolyMesh Canonicalize(int iterationsAdjust, int iterationsPlanarize)
        {
            var previousFaceRoles = FaceRoles;
            var canonicalized = Duplicate();
            if (iterationsAdjust > 0) Adjust(canonicalized, iterationsAdjust);
            if (iterationsPlanarize > 0) Planarize(canonicalized, iterationsPlanarize);
            canonicalized.FaceRoles = previousFaceRoles;
            return canonicalized;
        }

        public List<Vector3D> ReciprocalN(List<Vector3D> verts, List<int>[] faceIndices)
        {
            var result = new List<Vector3D>();
            foreach (var f in faceIndices)
            {
                Vector3D centroid = Vector3D.Zero; // Running sum of vertex coords
                Vector3D normalV = Vector3D.Zero; // Running sum of normal vectors
                double avgEdgeDist = 0; // Running sum for avg edge distance

                var v1 = verts[f[^2]];
                var v2 = verts[f[^1]];

                foreach (var vertexIndex in f)
                {
                    var v3 = verts[vertexIndex];
                    centroid += v3;
                    normalV += Vector3D.Orthogonal(v1, v2, v3);
                    avgEdgeDist += Vector3D.EdgeDist(v1, v2);
                    v1 = v2;
                    v2 = v3;
                }

                centroid = (1.0 / f.Count) * centroid;
                normalV = normalV.Normalize();
                avgEdgeDist /= f.Count;
                Vector3D tmp = (Vector3D.Dot(centroid, normalV) * normalV).Reciprocal();
                result.Add(
                    (tmp * (1.0 + avgEdgeDist) / 2.0)
                );

            }
            return result;
        }

        public List<Vector3D> GetVertexPositionsDouble()
        {
            return Vertices.Select(v =>
                new Vector3D(v.Position.x, v.Position.y, v.Position.z))
                .ToList();
        }


        // Hacky Canonicalization Algorithm
        // Using center of gravity of vertices for each face to planarize faces
        // get the spherical reciprocals of face centers
        public PolyMesh Kanonicalize(int iterations = 1)
        {
            var poly = Duplicate();
            if (iterations == 0) return poly;

            poly.Recenter(); // Necessary

            var polyV = poly.GetVertexPositionsDouble();
            var polyI = poly.ListFacesByVertexIndices();

            var dpoly = poly.Dual();
            var dpolyV = dpoly.GetVertexPositionsDouble();
            var dpolyI = dpoly.ListFacesByVertexIndices();

            // iteratively reciprocate face normals
            for (int count = 0; count < iterations; count++)
            {
                dpolyV = ReciprocalN(polyV, polyI);
                polyV = ReciprocalN(dpolyV, dpolyI);
            }

            poly.SetVertexPositions(
                polyV.Select(
                    v => new Vector3((float)v.X, (float)v.Y, (float)v.Z)
                ).ToList()
            );

            return poly;
        }

        public static PolyMesh AdjustXYZ(PolyMesh poly, int nIterations = 1)
        {
            poly = poly.Duplicate();
            var dpoly = poly.Dual();
            for (int count = 0; count < nIterations; count++)
            {
                // Reciprocate face centers
                dpoly.SetVertexPositions(ReciprocalC(poly));
                poly.SetVertexPositions(ReciprocalC(dpoly));
            }
            return poly;
        }

        private static List<Vector3> ReciprocalC(PolyMesh poly)
        {
            var centers = poly.Faces.Select(v => v.Centroid).ToList();
            for (int i = 0; i < centers.Count; i++) {
                float dotProduct = Vector3.Dot(centers[i], centers[i]);
                if (dotProduct != 0)
                {
                    centers[i] = (1f / dotProduct) * centers[i];
                }
            }
            return centers;
        }


        // Reimplementation. Currently not working
        // Switch to using double precision and follow similar approach to the new Kanonicalize?
        public PolyMesh CanonicalizeNew(int iterations, int _)
        {
            Vector3 _TangentPoint (Vector3 v1, Vector3 v2)
            {
                var d = v2 - v1;
                return v1 - (Vector3.Dot(d, v1) / d.sqrMagnitude) * d;
            }

            Dictionary<Guid, Vector3> _Tangentify(Dictionary<Guid, Vector3> vertices, MeshHalfedgeList edges)
            {
                // hack to improve convergence
                var STABILITY_FACTOR = 0.5f;
                var newVs = new Dictionary<Guid, Vector3>(vertices); // copy vertices
                foreach (var e in edges)
                {
                    // The point closest to origin
                    var t = _TangentPoint(e.Vertex.Position, e.Pair.Vertex.Position);
                    // Adjustment from sphere
                    var c = STABILITY_FACTOR / 2f * (1f - Mathf.Sqrt(Vector3.Dot(t,t))) * t;
                    vertices[e.Vertex.Name] += c;
                    vertices[e.Pair.Vertex.Name] += c;
                }
                return newVs;
            }

            Dictionary<Guid, Vector3> _Recenter(Dictionary<Guid, Vector3> vertices, MeshHalfedgeList edges)
            {
                // Centers of edges
                var edgecenters = edges.Select(e => e.Midpoint);
                Vector3 polycenter = new Vector3(
                    edgecenters.Average(v => v.x),
                    edgecenters.Average(v => v.y),
                    edgecenters.Average(v => v.z)
                );

                // Subtract off any deviation from center
                return vertices.ToDictionary(pair => pair.Key, pair => pair.Value - polycenter);
            }

            Dictionary<Guid, Vector3> _Rescale(Dictionary<Guid, Vector3> vertices)
            {
                var maxExtent = vertices.Values.Select(v => v.magnitude).Max();
                var s = 1f / maxExtent;
                return vertices.ToDictionary(pair => pair.Key, pair => pair.Value * s);
            }

            Dictionary<Guid, Vector3> _Planarize(Dictionary<Guid, Vector3> vertices, MeshFaceList faces)
            {
                var STABILITY_FACTOR = 0f; // Hack to improve convergence
                var newVs = new Dictionary<Guid, Vector3>(vertices); // copy vertices
                foreach (var f in faces)
                {
                    var normal = f.Normal;
                    var centroid = f.Centroid;

                    if (Vector3.Dot(normal, centroid) < 0)
                    { // correct sign if needed
                        normal = -normal;
                    }
                    foreach (var v in f.GetVertices())
                    {
                        // Project (vertex - centroid) onto normal, subtract off this component
                        newVs[v.Name] += normal * Vector3.Dot(
                            normal * STABILITY_FACTOR,
                            centroid - v.Position
                        );
                    }
                }
                return newVs;
            }

            var poly = Duplicate();
            poly.Recenter(); // Necessary

            var faces = poly.Faces;
            var edges = poly.Halfedges;
            var newVs = poly.Vertices.ToDictionary(pair => pair.Name, pair => pair.Position);

            if (iterations < 1) iterations = 1;
            for (var i = 0; i <= iterations; i++)
            {
                var oldVs = new Dictionary<Guid, Vector3>(newVs); // Copy vertices
                newVs = _Tangentify(newVs, edges);
                newVs = _Recenter(newVs, edges);
                newVs = _Planarize(newVs, faces);
                float maxChange = newVs.Values.Zip(oldVs.Values, (a, b) => Vector3.Distance(a, b)).Max();
                if (maxChange < 1e-7f) break;
            }

            foreach (var v in poly.Vertices)
            {
                v.Position = newVs[v.Name];
            }
            poly.Recenter();
            return poly;
        }

        /**
		 * Canonicalizes this polyhedron until the change in position does not
		 * exceed the given threshold. That is, the algorithm terminates when no vertex
		 * moves more than the threshold after one iteration.
		 *
		 * @param thresholdAdjust    The threshold for change in one "adjust"
		 *                           iteration.
		 * @param thresholdPlanarize The threshold for change in one "planarize"
		 *                           iteration.
		 * @return The canonicalized version of this polyhedron.
		 */
        public void Canonicalize(double thresholdAdjust, double thresholdPlanarize)
        {
            var previousFaceRoles = FaceRoles.ToList();
            var previousVertexRoles = VertexRoles.ToList();
            PolyMesh canonicalized = Duplicate();
            canonicalized.Recenter(); // Necessary

            if (thresholdAdjust > 0) Adjust(this, thresholdAdjust);
            if (thresholdPlanarize > 0) Planarize(this, thresholdPlanarize);
            FaceRoles = previousFaceRoles;
            VertexRoles = previousVertexRoles;
        }

        // Performs Laplacian smoothing on the mesh with the given number of iteration
        // https://en.wikipedia.org/wiki/Laplacian_smoothing
        private void Relax(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                var newPositions = new Vector3[Vertices.Count];
                for (var j = 0; j < Vertices.Count; j++)
                {
                    var neighbours = Vertices[j].Neighbours;
                    foreach (var neighbour in neighbours)
                    {
                        newPositions[j] += neighbour.Position;
                    }
                    newPositions[j] /= neighbours.Count;
                }
                for (int k = 0; k < Vertices.Count; k++)
                {
                    Vertices[k].Position = newPositions[k];
                }
            }
        }

        public void Planarize2(int iterations, float rate=0.5f)
        {
            for (int i = 0; i < iterations; i++)
            {
                var faceIndices = ListFacesByVertexIndices();
                var newVertices = Vertices.Select(v => v.Position).ToList();
                for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
                {
                    var face = Faces[faceIndex];
                    var centroid = face.Centroid;
                    Vector3 normal = face.Normal;
                    // Project each vertex onto the best-fit plane
                    for (int j = 0; j < face.Sides; j++)
                    {
                        int currentIndex = faceIndices[faceIndex][j];
                        Vector3 currentPosition = Vertices[currentIndex].Position;
                        Vector3 toCentroid = currentPosition - centroid;
                        float distanceToPlane = Vector3.Dot(toCentroid, normal);
                        Vector3 projectedPosition = currentPosition - normal * distanceToPlane;
                        newVertices[currentIndex] = Vector3.Lerp(newVertices[currentIndex], projectedPosition, rate);
                    }
                }

                for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
                {
                    var v = Vertices[vertexIndex];
                    v.Position = newVertices[vertexIndex];
                }
            }
        }

    }
}