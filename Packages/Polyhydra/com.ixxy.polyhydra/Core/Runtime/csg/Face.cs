﻿// Copyright 2020 The Blocks Authors
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
using System.Collections.ObjectModel;
using Polyhydra.core.csg.render;
using UnityEngine;

namespace Polyhydra.core.csg.core
{
    /// <summary>
    ///   A polygonal face of a MMesh.  Vertices must be specified in clockwise
    ///   order.  Edges may not cross.  Each Vertex must have a normal relative
    ///   to this face.
    /// </summary>
    public class Face
    {
        // The id of this face (unique within the mesh)
        private readonly int _id;
        // The ordered collection of vertex ids that comprise the face, in clockwise order.
        private readonly ReadOnlyCollection<int> _vertexIds;
        // We support two different triangulations as the renderer uses triangulation one way, and the modelling tools
        // another.  The renderer needs indices into its vertex buffer, and therefore wants triangulation to return triangle
        // indices relative to the vertices in the face - it will apply an offset to those, and add them to the mesh's
        // triangle array.  More straightforwardly, modeling operations need to look up the relevant vertices from the
        // MMesh, and hence require the vertex id.

        // This triangulation is a list of triangles whose indices are vertex ids.  This is used by mesh validation among
        // others.  This representation is used to allow easy lookup of vertex data from the MMesh.
        private List<Triangle> _modelTriangulation;

        // This triangulation is a list of triangles which index into this face's _vertexIds collection. This representation
        // is required for adding triangles to a Unity Mesh, but isn't as helpful for general modelling operations.
        private List<Triangle> _renderTriangulation;

        // The face normal.  Prior to face being added to a mesh via committing a GeometryOperation, this may not be set
        // (which is represented by having a value of Vector3.zero)
        private Vector3 _normal;

        // The properties of the face - primarily the material.
        private FaceProperties _properties;

        // Read-only getters.
        public int id { get { return _id; } }
        public ReadOnlyCollection<int> vertexIds { get { return _vertexIds; } }
        public Vector3 normal { get { return _normal; } }
        public FaceProperties properties { get { return _properties; } }

        // Cached vertex data, used to optimize construction of full mesh data for rendering.
        private List<Vector3> cachedMeshSpacePositions = null;
        private List<Vector3> cachedRenderNormals = null;

        /// <summary>
        /// Constructs a face with no normal.  This constructor should only be used when it is certain that the normal will
        /// be calculated later (ie, by Mesh.CommitOperation)
        /// </summary>
        /// <param name="id">Face id</param>
        /// <param name="vertexIds">Vertex ids for face in clockwise winding order.</param>
        /// <param name="properties">Face properties</param>
        private Face(int id, ReadOnlyCollection<int> vertexIds, FaceProperties properties)
        {
            _id = id;
            _vertexIds = vertexIds;
            _normal = Vector3.zero;

            _properties = properties;
            _modelTriangulation = null;
            _renderTriangulation = null;

            // Allocating capacity here to avoid needing to do it on every fetch - capacity is retained after Clear().
            cachedMeshSpacePositions = new List<Vector3>(vertexIds.Count);
            cachedRenderNormals = new List<Vector3>(vertexIds.Count);
        }

        // Constructs a face with an unset normal - the normal will be calculated
        public static Face FaceWithPendingNormal(int id, ReadOnlyCollection<int> vertexIds, FaceProperties properties)
        {
            return new Face(id, vertexIds, properties);
        }

        // Constructs a face with an unset normal - the normal will be calculated
        public static Face FaceWithPendingNormal(int id, List<Vertex> vertices, FaceProperties properties)
        {
            List<int> indices = new List<int>(vertices.Count);
            for (int i = 0; i < vertices.Count; i++)
            {
                indices.Add(vertices[i].id);
            }
            return new Face(id, indices.AsReadOnly(), properties);
        }

        /// <summary>
        /// Constructs a face with the supplied normal.  This constructor should only be used when a face is being created with
        /// the same normal as a preexisting face - otherwise one of the normal calculating constructors should be used.
        /// </summary>
        /// <param name="id">Face id</param>
        /// <param name="vertexIds">Vertex ids for face in clockwise winding order.</param>
        /// <param name="normal">Normal from another face this face should match</param>
        /// <param name="properties">Face properties</param>
        public Face(int id, ReadOnlyCollection<int> vertexIds, Vector3 normal, FaceProperties properties)
        {
            _id = id;
            _vertexIds = vertexIds;
            _normal = normal;

            _properties = properties;
            _modelTriangulation = null;
            _renderTriangulation = null;

            // Allocating capacity here to avoid needing to do it on every fetch - capacity is retained after Clear().
            cachedMeshSpacePositions = new List<Vector3>(vertexIds.Count);
            cachedRenderNormals = new List<Vector3>(vertexIds.Count);
        }

        /// <summary>
        /// Constructs a face, calculating its normal.
        /// </summary>
        /// <param name="id">Face id</param>
        /// <param name="vertexIds">Vertex ids for face in clockwise winding order.</param>
        /// <param name="verticesById">Dictionary of vertex ids to vertex data.</param>
        /// <param name="properties">Face properties</param>
        public Face(int id, ReadOnlyCollection<int> vertexIds, Dictionary<int, Vertex> verticesById, FaceProperties properties)
        {
            _id = id;
            _vertexIds = vertexIds;
            _normal = MeshMath.CalculateNormal(vertexIds, verticesById);
            _properties = properties;
            _modelTriangulation = null;
            _renderTriangulation = null;
            // Allocating capacity here to avoid needing to do it on every fetch - capacity is retained after Clear().
            cachedMeshSpacePositions = new List<Vector3>(vertexIds.Count);
            cachedRenderNormals = new List<Vector3>(vertexIds.Count);
        }

        /// <summary>
        /// Private constructor - exists to support Clone().
        /// </summary>
        private Face(int id, ReadOnlyCollection<int> vertexIds, Vector3 normal, FaceProperties properties,
          List<Triangle> modelTriangulation, List<Triangle> renderTriangulation,
          List<Vector3> cachedMeshSpacePositions, List<Vector3> cachedRenderNormals)
        {
            _id = id;
            _vertexIds = vertexIds;
            _normal = normal;
            _properties = properties;
            _modelTriangulation = modelTriangulation;
            _renderTriangulation = renderTriangulation;

            this.cachedMeshSpacePositions = cachedMeshSpacePositions;
            this.cachedRenderNormals = cachedRenderNormals;
        }

        /// <summary>
        /// Recalculate the normal for this face, using the supplied vertex data.
        /// </summary>
        public void RecalculateNormal(Dictionary<int, Vertex> verticesById)
        {
            _normal = MeshMath.CalculateNormal(vertexIds, verticesById);
            cachedRenderNormals.Clear();
            for (int i = 0; i < vertexIds.Count; i++)
            {
                cachedRenderNormals.Add(_normal);
            }
        }

        /// <summary>
        /// Clears the vertex cache (because one of them has been modified).  Cache will be recalculated next time the
        /// vertices are accessed.
        /// </summary>
        public void InvalidateVertexCache()
        {
            cachedMeshSpacePositions.Clear();
        }


        public Face Clone()
        {
            // Properties is a value object, so no need to clone.  But we still need to
            // make a new Face, so that the properties aren't shared.
            return new Face(
              _id,
              _vertexIds,
              _normal,
              _properties,
              _modelTriangulation,
              _renderTriangulation,
              new List<Vector3>(cachedMeshSpacePositions),
              new List<Vector3>(cachedRenderNormals));
        }
    }
}
