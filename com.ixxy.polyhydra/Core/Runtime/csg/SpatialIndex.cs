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

using System.Collections.Generic;
using Polyhydra.core.csg.util;
using UnityEngine;

namespace Polyhydra.core.csg.core
{
    /// <summary>
    ///   Holder for calculated information about a Face.
    /// </summary>
    public struct FaceInfo
    {
        internal Bounds bounds;
        internal Plane plane;
        internal Vector3 baryCenter;
        internal List<Vector3> border;
    }

    /// <summary>
    ///   Holder for calculated information about an edge.
    /// </summary>
    public struct EdgeInfo
    {
        internal Bounds bounds;
        internal float length;
        internal Vector3 edgeStart;
        internal Vector3 edgeVector;
    }

    /// <summary>
    ///   Holder for an object along with a distance.  Makes it easy to determine distance once for
    ///   a set of candidates and then sort on that.
    /// </summary>
    public struct DistancePair<T>
    {
        public float distance;
        public T value;

        internal DistancePair(float distance, T value)
        {
            this.distance = distance;
            this.value = value;
        }
    }

    /// <summary>
    ///   Comparator for sorting DistancePairs.
    /// </summary>
    internal class DistancePairComparer<T> : IComparer<DistancePair<T>>
    {
        public int Compare(DistancePair<T> left, DistancePair<T> right)
        {
            return left.distance.CompareTo(right.distance);
        }
    }

    public class SpatialIndex
    {
        public const int MAX_INTERSECT_RESULTS = 100000;

        public CollisionSystem<int> meshes { get; private set; }
        private CollisionSystem<FaceKey> faces;
        private CollisionSystem<EdgeKey> edges;
        private CollisionSystem<VertexKey> vertices;
        private CollisionSystem<int> meshBounds;
        private Dictionary<FaceKey, FaceInfo> faceInfo;
        private Dictionary<EdgeKey, EdgeInfo> edgeInfo;

        public SpatialIndex(Bounds bounds)
        {
            Setup(bounds);
        }

        private void Setup(Bounds bounds)
        {
            meshes = new OctreeImpl<int>(bounds);
            faces = new OctreeImpl<FaceKey>(bounds);
            edges = new OctreeImpl<EdgeKey>(bounds);
            vertices = new OctreeImpl<VertexKey>(bounds);
            meshBounds = new OctreeImpl<int>(bounds);

            faceInfo = new Dictionary<FaceKey, FaceInfo>();
            edgeInfo = new Dictionary<EdgeKey, EdgeInfo>();
        }


        /// <summary>
        ///   Resets the entire state of the Spatial Index, using the given bounds.
        /// </summary>
        public void Reset(Bounds bounds)
        {
            Setup(bounds);
        }
    }
}
