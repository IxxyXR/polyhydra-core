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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Polyhydra.core.csg.core;
using Polyhydra.core.csg.util;

namespace Polyhydra.Core
{

    public class CsgOperations
    {
        private const float COPLANAR_EPS = 0.001f;


        /// <summary>
        ///   Perform the subtract on CsgObjects.  The implementation follows the paper:
        ///   http://vis.cs.brown.edu/results/videos/bib/pdf/Laidlaw-1986-CSG.pdf
        /// </summary>
        public static List<CsgPolygon> CsgSubtract(CsgContext ctx, CsgObject leftObj, CsgObject rightObj)
        {
            SplitObject(ctx, leftObj, rightObj);
            SplitObject(ctx, rightObj, leftObj);
            SplitObject(ctx, leftObj, rightObj);
            ClassifyPolygons(leftObj, rightObj);
            ClassifyPolygons(rightObj, leftObj);

            FaceProperties facePropertiesForNewFaces = leftObj.polygons[0].faceProperties;
            List<CsgPolygon> polys = SelectPolygons(leftObj, false, null, PolygonStatus.OUTSIDE, PolygonStatus.OPPOSITE);
            polys.AddRange(SelectPolygons(rightObj, true, facePropertiesForNewFaces, PolygonStatus.INSIDE));

            return polys;
        }

        /// <summary>
        ///   Perform union on CsgObjects
        /// </summary>
        public static List<CsgPolygon> CsgUnion(CsgContext ctx, CsgObject leftObj, CsgObject rightObj)
        {
            SplitObject(ctx, leftObj, rightObj);
            SplitObject(ctx, rightObj, leftObj);
            SplitObject(ctx, leftObj, rightObj);
            ClassifyPolygons(leftObj, rightObj);
            ClassifyPolygons(rightObj, leftObj);

            FaceProperties facePropertiesForNewFaces = leftObj.polygons[0].faceProperties;
            List<CsgPolygon> polys = SelectPolygons(leftObj, false, null, PolygonStatus.OUTSIDE, PolygonStatus.SAME);
            polys.AddRange(SelectPolygons(rightObj, true, facePropertiesForNewFaces, PolygonStatus.OUTSIDE));

            return polys;
        }

        /// <summary>
        ///   Perform intersection on CsgObjects
        /// </summary>
        public static List<CsgPolygon> CsgIntersect(CsgContext ctx, CsgObject leftObj, CsgObject rightObj)
        {
            SplitObject(ctx, leftObj, rightObj);
            SplitObject(ctx, rightObj, leftObj);
            SplitObject(ctx, leftObj, rightObj);
            ClassifyPolygons(leftObj, rightObj);
            ClassifyPolygons(rightObj, leftObj);

            FaceProperties facePropertiesForNewFaces = leftObj.polygons[0].faceProperties;
            List<CsgPolygon> polys = SelectPolygons(leftObj, false, null, PolygonStatus.INSIDE, PolygonStatus.SAME);
            polys.AddRange(SelectPolygons(rightObj, true, facePropertiesForNewFaces, PolygonStatus.INSIDE));

            return polys;

        }

        /// <summary>
        ///   Select all of the polygons in the object with any of the given statuses.
        /// </summary>
        private static List<CsgPolygon> SelectPolygons(CsgObject obj, bool invert, FaceProperties? overwriteFaceProperties, params PolygonStatus[] status)
        {
            HashSet<PolygonStatus> selectedStatus = new HashSet<PolygonStatus>(status);
            List<CsgPolygon> polys = new List<CsgPolygon>();

            foreach (CsgPolygon poly in obj.polygons)
            {
                if (selectedStatus.Contains(poly.status))
                {
                    CsgPolygon polyToAdd = poly;
                    if (invert)
                    {
                        polyToAdd = poly.Invert();
                    }
                    if (overwriteFaceProperties.HasValue)
                    {
                        polyToAdd.faceProperties = overwriteFaceProperties.Value;
                    }
                    polys.Add(polyToAdd);
                }
            }

            return polys;
        }

        // Section 7:  Classify all polygons in the object.
        private static void ClassifyPolygons(CsgObject obj, CsgObject wrt)
        {
            // Set up adjacency information.
            foreach (CsgPolygon poly in obj.polygons)
            {
                for (int i = 0; i < poly.vertices.Count; i++)
                {
                    int j = (i + 1) % poly.vertices.Count;
                    poly.vertices[i].neighbors.Add(poly.vertices[j]);
                    poly.vertices[j].neighbors.Add(poly.vertices[i]);
                }
            }

            // Classify polys.
            foreach (CsgPolygon poly in obj.polygons)
            {
                if (HasUnknown(poly) || AllBoundary(poly))
                {
                    ClassifyPolygonUsingRaycast(poly, wrt);
                    if (poly.status == PolygonStatus.INSIDE || poly.status == PolygonStatus.OUTSIDE)
                    {
                        VertexStatus newStatus = poly.status == PolygonStatus.INSIDE ? VertexStatus.INSIDE : VertexStatus.OUTSIDE;
                        foreach (CsgVertex vertex in poly.vertices)
                        {
                            PropagateVertexStatus(vertex, newStatus);
                        }
                    }
                }
                else
                {
                    // Use the status of the first vertex that is inside or outside.
                    foreach (CsgVertex vertex in poly.vertices)
                    {
                        if (vertex.status == VertexStatus.INSIDE)
                        {
                            poly.status = PolygonStatus.INSIDE;
                            break;
                        }
                        if (vertex.status == VertexStatus.OUTSIDE)
                        {
                            poly.status = PolygonStatus.OUTSIDE;
                            break;
                        }
                    }
                    AssertOrThrow.True(poly.status != PolygonStatus.UNKNOWN, "Should have classified polygon.");
                }
            }
        }

        // Fig 8.1: Propagate vertex status.
        private static void PropagateVertexStatus(CsgVertex vertex, VertexStatus newStatus)
        {
            if (vertex.status == VertexStatus.UNKNOWN)
            {
                vertex.status = newStatus;
                foreach (CsgVertex neighbor in vertex.neighbors)
                {
                    PropagateVertexStatus(neighbor, newStatus);
                }
            }
        }

        // Fig 7.2: Classify a given polygon by raycasting from its barycenter into the faces of the other object.
        // Public for testing.
        public static void ClassifyPolygonUsingRaycast(CsgPolygon poly, CsgObject wrt)
        {
            Vector3 rayStart = poly.baryCenter;
            Vector3 rayNormal = poly.plane.normal;
            CsgPolygon closest = null;
            float closestPolyDist = float.MaxValue;

            bool done;
            int count = 0;
            do
            {
                done = true;  // Done unless we hit a special case.
                foreach (CsgPolygon otherPoly in wrt.polygons)
                {
                    float dot = Vector3.Dot(rayNormal, otherPoly.plane.normal);
                    bool perp = Mathf.Abs(dot) < CsgMath.EPSILON;
                    bool onOtherPlane = Mathf.Abs(otherPoly.plane.GetDistanceToPoint(rayStart)) < CsgMath.EPSILON;
                    Vector3 projectedToOtherPlane = Vector3.zero;
                    float signedDist = -1f;
                    if (!perp)
                    {
                        CsgMath.RayPlaneIntersection(out projectedToOtherPlane, rayStart, rayNormal, otherPoly.plane);
                        float dist = Vector3.Distance(projectedToOtherPlane, rayStart);
                        signedDist = dist * Mathf.Sign(Vector3.Dot(rayNormal, (projectedToOtherPlane - rayStart)));
                    }

                    if (perp && onOtherPlane)
                    {
                        done = false;
                        break;
                    }
                    else if (perp && !onOtherPlane)
                    {
                        // no intersection
                    }
                    else if (!perp && onOtherPlane)
                    {
                        int isInside = CsgMath.IsInside(otherPoly, projectedToOtherPlane);
                        if (isInside >= 0)
                        {
                            closestPolyDist = 0;
                            closest = otherPoly;
                            break;
                        }
                    }
                    else if (!perp && signedDist > 0)
                    {
                        if (signedDist < closestPolyDist)
                        {
                            int isInside = CsgMath.IsInside(otherPoly, projectedToOtherPlane);
                            if (isInside > 0)
                            {
                                closest = otherPoly;
                                closestPolyDist = signedDist;
                            }
                            else if (isInside == 0)
                            {
                                // On segment, perturb and try again.
                                done = false;
                                break;
                            }
                        }
                    }
                }
                if (!done)
                {
                    // Perturb the normal and try again.
                    rayNormal += new Vector3(
                      UnityEngine.Random.Range(-0.1f, 0.1f),
                      UnityEngine.Random.Range(-0.1f, 0.1f),
                      UnityEngine.Random.Range(-0.1f, 0.1f));
                    rayNormal = rayNormal.normalized;
                }
                count++;
            } while (!done && count < 5);

            if (closest == null)
            {
                // Didn't hit any polys, we are outside.
                poly.status = PolygonStatus.OUTSIDE;
            }
            else
            {
                float dot = Vector3.Dot(poly.plane.normal, closest.plane.normal);
                if (Mathf.Abs(closestPolyDist) < CsgMath.EPSILON)
                {
                    poly.status = dot < 0 ? PolygonStatus.OPPOSITE : PolygonStatus.SAME;
                }
                else
                {
                    poly.status = dot < 0 ? PolygonStatus.OUTSIDE : PolygonStatus.INSIDE;
                }
            }
        }

        private static bool HasUnknown(CsgPolygon poly)
        {
            foreach (CsgVertex vertex in poly.vertices)
            {
                if (vertex.status == VertexStatus.UNKNOWN)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool AllBoundary(CsgPolygon poly)
        {
            foreach (CsgVertex vertex in poly.vertices)
            {
                if (vertex.status != VertexStatus.BOUNDARY)
                {
                    return false;
                }
            }
            return true;
        }

        // Public for testing.
        public static void SplitObject(CsgContext ctx, CsgObject toSplit, CsgObject splitBy)
        {
            bool splitPoly;
            int count = 0;
            HashSet<CsgPolygon> alreadySplit = new HashSet<CsgPolygon>();
            do
            {
                splitPoly = false;
                // Temporary guard to prevent infinite loops while there are bugs.
                // TODO(bug) figure out why csg creates so many rejected splits.
                count++;
                if (count > 100)
                {
                    // This usually occurs when csg keeps trying to do the same invalid split over and over.
                    // If the algorithm has reached this point, it usually means that the two meshes are
                    // split enough to perform a pretty good looking csg subtraction. More investigation
                    // should be done on bug and we may be able to remove this guard.
                    return;
                }
                foreach (CsgPolygon toSplitPoly in toSplit.polygons)
                {
                    if (alreadySplit.Contains(toSplitPoly))
                    {
                        continue;
                    }
                    alreadySplit.Add(toSplitPoly);
                    if (toSplitPoly.bounds.Intersects(splitBy.bounds))
                    {
                        foreach (CsgPolygon splitByPoly in splitBy.polygons)
                        {
                            if (toSplitPoly.bounds.Intersects(splitByPoly.bounds)
                                && !Coplanar(toSplitPoly.plane, splitByPoly.plane))
                            {
                                splitPoly = PolygonSplitter.SplitPolys(ctx, toSplit, toSplitPoly, splitByPoly);
                                if (splitPoly)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (splitPoly)
                    {
                        break;
                    }
                }
            } while (splitPoly);
        }

        private static bool Coplanar(Plane plane1, Plane plane2)
        {
            return Mathf.Abs(plane1.distance - plane2.distance) < COPLANAR_EPS
              && Vector3.Distance(plane1.normal, plane2.normal) < COPLANAR_EPS;
        }
    }
}
