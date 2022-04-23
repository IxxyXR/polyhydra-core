using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Polyhydra.Core {
    /// <summary>
    /// A collection of mesh faces
    /// </summary>
    public class MeshFaceList : KeyedCollection<string, Face> {
        
        private PolyMesh _mPolyMesh;

        public MeshFaceList(PolyMesh polyMesh) {
            _mPolyMesh = polyMesh;
        }

        protected override string GetKeyForItem(Face face) {
            return face.Name;
        }

        public Boolean Add(IEnumerable<Vertex> vertices)
        {
            return _AddOrInsert(vertices, false);
        }
        
        public Boolean Insert(int index, IEnumerable<Vertex> vertices)
        {
            return _AddOrInsert(vertices, true, index);
        }

        /// <summary>
        /// Add a new face by its vertices. Will not allow the mesh to become non-manifold (e.g. by duplicating an existing halfedge).
        /// </summary>
        /// <param name="vertices">the vertices which define the face, given in anticlockwise order</param>
        /// <returns>true on success, false on failure</returns>
        private Boolean _AddOrInsert(IEnumerable<Vertex> vertices, bool insert, int index=-1) {

            Vertex[] array = vertices.ToArray();

            if (array.Length < 3) {
                Debug.LogError("Too few vertices");
                return false;
            }

            int n = array.Length;
            Halfedge[] newEdges = new Halfedge[n]; // temporary container for new halfedges

            // create new halfedges (it is only possible for each to reference their vertex at this point)
            for (int i = 0; i < n; i++) {
                newEdges[i] = new Halfedge(array[i], null, null, null);
            }

            Face newFace = new Face(newEdges[0]); // create new face

            // link halfedges to face, next and prev
            // stop if a similar halfedge is found in the mesh (avoid duplicates)
            for (int i = 0; i < n; i++) {
                newEdges[i].Face = newFace;
                newEdges[i].Next = newEdges[(i + 1) % n];
                newEdges[i].Prev = newEdges[(i + n - 1) % n];
                if (_mPolyMesh.Halfedges.Contains(newEdges[i].Name)) {
                    return false;
                }
            }

            // Add halfedges to mesh
            for (int j = 0; j < n; j++) {
                array[j].Halfedge = array[j].Halfedge ?? newEdges[j];
                try
                {
                    _mPolyMesh.Halfedges.Add(newEdges[j]);
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning(e.Message);
                    return false;
                }
                // TODO Can we do something like the following
                // to remove the need to run MatchPairs after adding faces?
                // var rname = (newEdges[j].Prev.Vertex.Name, newEdges[j].Vertex.Name);
                // newEdges[j].Pair = _mPolyMesh.Halfedges.Contains(rname) ? _mPolyMesh.Halfedges[rname] : null;
            }

            // add face to mesh
            if (insert)
            {
                Insert(index, newFace);
            }
            else
            {
                Add(newFace);
            }

            return true;
        }

        /// <summary>
        /// Remove a face from the mesh, also removing its halfedges. (Replaces IList.Remove(T item).)
        /// Returns the edges of the hole.
        /// </summary>
        /// <param name="item">a reference to the face which is to be removed</param>
        public new List<Halfedge> Remove(Face item) {
            var edges = new List<Halfedge>();
            var hole = new List<Halfedge>();
            Halfedge edge = item.Halfedge;
            do {
                edges.Add(edge);
                _mPolyMesh.Halfedges.Remove(edge);
                edge = edge.Next;
            } while (edge != item.Halfedge);

            for (var edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++)
            {
                Halfedge e = edges[edgeIndex];
                if (e.Pair != null)
                {
                    e.Pair.Pair = null;
                    hole.Add(e.Pair);
                }

                // if halfedge's vertex references halfedge, point it to another
                if (e.Vertex.Halfedge == e)
                {
                    if (e.Pair != null)
                        e.Vertex.Halfedge = e.Pair.Prev;
                    else if (e.Next.Pair != null)
                        e.Vertex.Halfedge = e.Next.Pair;
                    else
                    {
                        // if all else fails, try searching through all of the halfedges for one which points to this vertex
                        try
                        {
                            e.Vertex.Halfedge = _mPolyMesh.Halfedges.First(i => i.Vertex == e.Vertex);
                        }
                        catch (InvalidOperationException)
                        {
                            // nothing found, remove the vertex
                            _mPolyMesh.Vertices.Remove(e.Vertex);
                        }
                    }
                }
            }

            base.Remove(item);
            return hole;
        }

        /// <summary>
        /// Reduce an n-gon mesh face to triangles.
        /// </summary>
        /// <param name="index">index of the mesh face to be triangulated</param>
        /// <param name="quads">if true, quad-faces will not be touched</param>
        /// <returns>the number of new tri-faces created</returns>
        ///
        public int Triangulate(int index, bool quads) {
            
            // fan method
            Face face = this[index];
            Vertex start = face.Halfedge.Vertex;
            List<Vertex> end = face.GetVertices();
            if (end.Count <= 3) return 0;
            if (end.Count <= 4 && !quads) return 0;


            int count = 0;
            for (int i = 2; i < end.Count - 1; i++) {
                Face f_new;
                Halfedge he_new, he_new_pair;
                face.Split(start, end[i], out f_new, out he_new, out he_new_pair);
                _mPolyMesh.Faces.Add(f_new);
                _mPolyMesh.Halfedges.Add(he_new);
                _mPolyMesh.Halfedges.Add(he_new_pair);
                count++;
            }

            return count;
        }
    }
}