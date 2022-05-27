using System;
using System.Collections.Generic;
using System.Linq;
using GK;
using UnityEngine;

namespace Polyhydra.Core
{
    public partial class PolyMesh
    {
        public bool IncludeFace(int faceIndex, Filter filter = null)
        {
            return filter == null || filter.eval(new FilterParams(this, faceIndex));
        }

        public bool IncludeVertex(int vertexIndex, Filter filter = null)
        {
            return filter == null || filter.eval(new FilterParams(this, vertexIndex));
        }

        public PolyMesh AddMirrored(OpParams o, Vector3 axis)
        {
            float amount = o.GetValueA(this, 0);
            var original = FaceRemove(o);
            var mirror = original.Duplicate();
            mirror.Mirror(axis, amount);
            mirror = mirror.FaceRemove(o, true);
            mirror.Halfedges.Flip();
            original.Append(mirror);
            original.Recenter();
            return original;
        }

        public void Mirror(Vector3 axis, float offset)
        {
            Vector3 offsetVector = offset * axis;
            for (var i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Position -= offsetVector;
            }

            for (var i = 0; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                v.Position = Vector3.Reflect(v.Position, axis);
            }
        }

        public PolyMesh AddCopy(Vector3 axis, float amount, Filter filter, string tags = "")
        {
            amount /= 2.0f;
            var original = Duplicate(axis * -amount, 1.0f);
            var copy = Duplicate(axis * amount, 1.0f);
            copy = copy.FaceRemove(filter, true);
            original.Append(copy);
            return original;
        }

        public PolyMesh Stack(Vector3 axis, float offset, float scale, Filter filter = default, float limit = 0.1f,
            string tags = "")
        {
            scale = Mathf.Abs(scale);
            scale = Mathf.Clamp(scale, 0.0001f, 0.99f);
            Vector3 offsetVector = axis * offset;

            var original = Duplicate();
            var copy = FaceRemove(filter, true);

            int copies = 0;
            while (scale > limit && copies < 64) // TODO make copies configurable
            {
                original.Append(copy.Duplicate(offsetVector, scale));
                scale *= scale;
                offsetVector += axis * offset;
                offset *= Mathf.Sqrt(scale); // Not sure why but sqrt *looks* right.
                copies++;
            }

            return original;
        }

        public void Scale(Vector3 scale)
        {
            if (Vertices.Count > 0)
            {
                for (var i = 0; i < Vertices.Count; i++)
                {
                    var vert = Vertices[i].Position;
                    Vertices[i].Position = new Vector3(
                        vert.x * scale.x,
                        vert.y * scale.y,
                        vert.z * scale.z
                    );
                }
            }
        }

        public void Transform(Vector3 pos, Vector3? rot = null, Vector3? scale = null)
        {
            var matrix = Matrix4x4.TRS(
                pos,
                Quaternion.Euler(rot ?? Vector3.zero),
                scale ?? Vector3.one
            );
            Transform(matrix);
        }

        public void Transform(Matrix4x4 matrix)
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                v.Position = matrix.MultiplyPoint3x4(v.Position);
            }
        }

        public PolyMesh Rotate(Vector3 axis, float amount)
        {
            var copy = Duplicate();
            for (var i = 0; i < copy.Vertices.Count; i++)
            {
                var v = copy.Vertices[i];
                v.Position = Quaternion.AngleAxis(amount, axis) * v.Position;
            }

            return copy;
        }

        public Vector3 GetCentroid()
        {
            if (Vertices.Count == 0) return Vector3.zero;

            return new Vector3(
                Vertices.Average(x => x.Position.x),
                Vertices.Average(x => x.Position.y),
                Vertices.Average(x => x.Position.z)
            );
        }

        public void Recenter()
        {
            Vector3 newCenter = GetCentroid();
            for (var i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Position -= newCenter;
            }
        }

        public void Morph(PolyMesh target, float amount, bool reverseVertexOrder)
        {
            int minVertCount = Mathf.Min(Vertices.Count, target.Vertices.Count);
            for (var i = 0; i < minVertCount; i++)
            {
                var targetPos = target.Vertices[reverseVertexOrder ? minVertCount - i - 1 : i].Position;
                Vertices[i].Position = Vector3.LerpUnclamped(Vertices[i].Position, targetPos, amount);
            }
        }

        public PolyMesh SitLevel(float faceFactor = 0)
        {
            int faceIndex = Mathf.FloorToInt(Faces.Count * faceFactor);
            faceIndex = Mathf.Clamp(faceIndex, 0, 1);
            var vertexPoints = new List<Vector3>();
            var faceIndices = ListFacesByVertexIndices();

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                var rot = Quaternion.LookRotation(Faces[faceIndex].Normal);
                var rotForwardToDown = Quaternion.FromToRotation(Vector3.down, Vector3.forward);
                vertexPoints.Add(Quaternion.Inverse(rot * rotForwardToDown) * Vertices[vertexIndex].Position);
            }

            var conway = new PolyMesh(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
            return conway;
        }

        public PolyMesh Stretch(float amount)
        {
            var vertexPoints = new List<Vector3>();
            var faceIndices = ListFacesByVertexIndices();

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                var vertex = Vertices[vertexIndex];
                float y;
                if (vertex.Position.y < 0.1)
                {
                    y = vertex.Position.y - amount;
                }
                else if (vertex.Position.y > -0.1)
                {
                    y = vertex.Position.y + amount;
                }
                else
                {
                    y = vertex.Position.y;
                }


                var newPos = new Vector3(vertex.Position.x, y, vertex.Position.z);
                vertexPoints.Add(newPos);
            }

            var conway = new PolyMesh(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
            return conway;
        }

        public PolyMesh FaceSlide(OpParams o)
        {
            var poly = Duplicate();
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = poly.Faces[faceIndex];
                if (!IncludeFace(faceIndex, o.filter)) continue;
                //var amount = amount * (float) (randomize ? random.NextDouble() : 1);
                var faceVerts = face.GetVertices();
                
                var (tangentLeft, tangentUp) = face.GetTangents();
                
                for (var vertexIndex = 0; vertexIndex < faceVerts.Count; vertexIndex++)
                {
                    float amount = o.GetValueA(this, vertexIndex);
                    float direction = o.GetValueB(this, vertexIndex);
                    var vertexPos = faceVerts[vertexIndex].Position;
                    
                    Vector3 tangent = Vector3.SlerpUnclamped(tangentUp, tangentLeft, direction);

                    var vector = tangent * amount;
                    var newPos = vertexPos + vector;
                    faceVerts[vertexIndex].Position = newPos;
                }
            }

            return poly;
        }

        public PolyMesh VertexScale(OpParams o)
        {
            var vertexPoints = new List<Vector3>();
            var faceIndices = ListFacesByVertexIndices();

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                float scale = o.GetValueA(this, vertexIndex);
                var vertex = Vertices[vertexIndex];
                var includeVertex = IncludeVertex(vertexIndex, o.filter);
                vertexPoints.Add(includeVertex ? vertex.Position * scale : vertex.Position);
            }

            return new PolyMesh(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
        }

        public PolyMesh VertexStellateSplit(OpParams o)
        {

            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float scale = o.GetValueA(this, faceIndex);
                var face = Faces[faceIndex];
                var faceCentroid = face.Centroid;
                var includeFace = IncludeFace(faceIndex, o.filter);
                int c = vertexPoints.Count;

                vertexPoints.AddRange(face.GetVertices()
                    .Select((v, i) =>
                        i % 2 == 0 && includeFace
                            ? Vector3.LerpUnclamped(faceCentroid, v.Position, scale)
                            : v.Position));
                var faceVerts = new List<int>();
                for (int ii = 0; ii < face.GetVertices().Count; ii++)
                {
                    faceVerts.Add(c + ii);
                }

                faceIndices.Add(faceVerts);
                faceRoles.Add(includeFace ? FaceRoles[faceIndex] : Roles.Ignored);
                var vertexRole = includeFace ? Roles.Existing : Roles.Ignored;
                vertexRoles.AddRange(Enumerable.Repeat(vertexRole, faceVerts.Count));
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, FaceTags);
        }

        public void VertexStellate(OpParams o)
        {
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                var includeFace = IncludeFace(faceIndex, o.filter);
                if (!includeFace) continue;
                var verts = face.GetVertices();
                float scale = o.GetValueA(this, faceIndex);
                var faceCentroid = face.Centroid;
                for (var vertexIndex = 0; vertexIndex < verts.Count; vertexIndex += 2)
                {
                    var vert = verts[vertexIndex];
                    vert.Position = Vector3.LerpUnclamped(faceCentroid, vert.Position, scale);
                }
            }
        }

        public PolyMesh VertexOffset(OpParams o)
        {
            var poly = Duplicate();
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = poly.Faces[faceIndex];
                if (!IncludeFace(faceIndex, o.filter)) continue;
                var faceCentroid = face.Centroid;
                var faceVerts = face.GetVertices();
                for (var vertexIndex = 0; vertexIndex < faceVerts.Count; vertexIndex++)
                {
                    float scale = o.GetValueA(this, vertexIndex);
                    var vertexPos = faceVerts[vertexIndex].Position;
                    var newPos = vertexPos + (vertexPos - faceCentroid) * scale;
                    faceVerts[vertexIndex].Position = newPos;
                }
            }

            return poly;
        }

        public PolyMesh VertexRotate(OpParams o)
        {
            var poly = Duplicate();
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float amount = o.GetValueA(this, faceIndex);
                var face = poly.Faces[faceIndex];
                if (!IncludeFace(faceIndex, o.filter)) continue;
                var faceCentroid = face.Centroid;
                var direction = face.Normal;
                var _angle = (360f / face.Sides) * amount;
                var faceVerts = face.GetVertices();
                for (var vertexIndex = 0; vertexIndex < faceVerts.Count; vertexIndex++)
                {
                    var vertexPos = faceVerts[vertexIndex].Position;
                    var rot = Quaternion.AngleAxis(_angle, direction);
                    var newPos = faceCentroid + rot * (vertexPos - faceCentroid);
                    faceVerts[vertexIndex].Position = newPos;
                }
            }

            return poly;
        }

        public PolyMesh FaceScale(OpParams o)
        {
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();

            var vertexRoles = new List<Roles>();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float scale = o.GetValueA(this, faceIndex);
                var face = Faces[faceIndex];
                var faceCentroid = face.Centroid;
                var includeFace = IncludeFace(faceIndex, o.filter);
                int c = vertexPoints.Count;

                vertexPoints.AddRange(face.GetVertices()
                    .Select(v =>
                        includeFace ? Vector3.LerpUnclamped(faceCentroid, v.Position, scale) : v.Position));
                var faceVerts = new List<int>();
                for (int i = 0; i < face.GetVertices().Count; i++)
                {
                    faceVerts.Add(c + i);
                }

                faceIndices.Add(faceVerts);
                var vertexRole = includeFace ? Roles.Existing : Roles.Ignored;
                vertexRoles.AddRange(Enumerable.Repeat(vertexRole, faceVerts.Count));
            }

            return new PolyMesh(vertexPoints, faceIndices, FaceRoles, vertexRoles, FaceTags);
        }

        public PolyMesh FaceOffset(OpParams o)
        {
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();
            
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float amount = o.GetValueA(this, faceIndex);
                var face = Faces[faceIndex];

                var includeFace = IncludeFace(faceIndex, o.filter);

                int c = vertexPoints.Count;
                var faceVertices = new List<int>();

                var offset = face.Normal * o.GetValueA(this, faceIndex);
 
                vertexPoints.AddRange(face.GetVertices().Select(
                    v => includeFace ? v.Position + offset : v.Position
                ));
                faceVertices = new List<int>();
                for (int ii = 0; ii < face.GetVertices().Count; ii++)
                {
                    faceVertices.Add(c + ii);
                }

                faceIndices.Add(faceVertices);
                faceRoles.Add(includeFace ? FaceRoles[faceIndex] : Roles.Ignored);
                var vertexRole = includeFace ? Roles.Existing : Roles.Ignored;
                vertexRoles.AddRange(Enumerable.Repeat(vertexRole, faceVertices.Count));
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, FaceTags);
        }

        public PolyMesh FaceRotate(OpParams o, int axis = 0)
        {
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();

            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();


            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float amount = o.GetValueA(this, faceIndex);
                var face = Faces[faceIndex];
                var _angle = (360f / face.Sides) * amount;

                var includeFace = IncludeFace(faceIndex, o.filter);

                int c = vertexPoints.Count;
                var faceVertices = new List<int>();
                
                var pivot = face.Centroid;
                
                Vector3 direction = face.Normal;
                switch (axis)
                {
                    case 0:
                        direction = Vector3.Cross(face.Normal, Vector3.up);
                        break;
                    case 1:
                        direction = Vector3.Cross(face.Normal, Vector3.forward);
                        break;
                    case 2:
                        break;
                }

                var rot = Quaternion.AngleAxis(_angle, direction);

                vertexPoints.AddRange(
                    face.GetVertices().Select(
                        v => includeFace ? pivot + rot * (v.Position - pivot) : v.Position
                    )
                );
                faceVertices = new List<int>();
                for (int ii = 0; ii < face.GetVertices().Count; ii++)
                {
                    faceVertices.Add(c + ii);
                }

                faceIndices.Add(faceVertices);
                faceRoles.Add(includeFace ? FaceRoles[faceIndex] : Roles.Ignored);
                var vertexRole = includeFace ? Roles.Existing : Roles.Ignored;
                vertexRoles.AddRange(Enumerable.Repeat(vertexRole, faceVertices.Count));
            }

            return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, FaceTags);
        }

        public PolyMesh VertexRemove(OpParams o, bool invertLogic)
        {
            var allFaceIndices = new List<List<int>>();
            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();

            int vertexCount = 0;

            var faces = ListFacesByVertexIndices();
            for (var i = 0; i < faces.Length; i++)
            {
                var oldFaceIndices = faces[i];
                var newFaceIndices = new List<int>();
                for (var idx = 0; idx < oldFaceIndices.Count; idx++)
                {
                    var vertexIndex = oldFaceIndices[idx];
                    bool keep = IncludeVertex(vertexIndex, o.filter);
                    keep = invertLogic ? !keep : keep;
                    if (!keep)
                    {
                        newFaceIndices.Add(vertexIndex);
                        vertexCount++;
                    }
                }

                if (newFaceIndices.Count > 2)
                {
                    allFaceIndices.Add(newFaceIndices);
                }
            }

            faceRoles.AddRange(Enumerable.Repeat(Roles.Existing, allFaceIndices.Count));
            vertexRoles.AddRange(Enumerable.Repeat(Roles.Existing, vertexCount));
            return new PolyMesh(Vertices.Select(x => x.Position), allFaceIndices, faceRoles, vertexRoles, FaceTags);
        }

        public PolyMesh Collapse(bool invertLogic, Filter filter = null)
        {
            var poly = VertexRemove(new OpParams(filter), invertLogic);
            poly.FillHoles();
            return poly;
        }

        public PolyMesh Layer(OpParams o, int layers)
        {
            var poly = Duplicate();
            var layer = Duplicate();
            for (int i = 0; i <= layers; i++)
            {
                var newLayer = layer.Duplicate();
                newLayer = newLayer.FaceScale(o);
                newLayer = newLayer.Offset(o);
                poly.Append(newLayer);
                layer = newLayer;
            }

            return poly;
        }

        public PolyMesh FaceRemove(OpParams o, bool invertLogic = false)
        {
            return _FaceRemove(o, invertLogic);
        }

        public PolyMesh FaceRemove(Filter filter, bool invertLogic = false)
        {
            return _FaceRemove(new OpParams { filter = filter}, invertLogic);
        }
        
        public PolyMesh FaceRemove(bool invertLogic, List<int> faceIndices)
        {
            var filter = new Filter(x => faceIndices.Contains(x.index));
            return _FaceRemove(new OpParams(filter), invertLogic);
        }

        public PolyMesh _FaceRemove(OpParams o, bool invertLogic = false)
        {
            var faceRoles = new List<Roles>();
            var vertexRoles = new List<Roles>();
            var facesToRemove = new List<Face>();
            var newPoly = Duplicate();
            var faceIndices = ListFacesByVertexIndices();
            var existingFaceRoles = new Dictionary<Vector3, Roles>();
            var existingVertexRoles = new Dictionary<Vector3, Roles>();

            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                bool removeFace;
                removeFace = IncludeFace(faceIndex, o.filter);
                removeFace = invertLogic ? !removeFace : removeFace;
                if (removeFace)
                {
                    facesToRemove.Add(newPoly.Faces[faceIndex]);
                }
                else
                {
                    existingFaceRoles[face.Centroid] = FaceRoles[faceIndex];
                    var verts = face.GetVertices();
                    for (var vertIndex = 0; vertIndex < verts.Count; vertIndex++)
                    {
                        var vert = verts[vertIndex];
                        existingVertexRoles[vert.Position] = VertexRoles[faceIndices[faceIndex][vertIndex]];
                    }
                }
            }

            var newFaceTags = new List<HashSet<string>>();

            for (var i = 0; i < facesToRemove.Count; i++)
            {
                var face = facesToRemove[i];
                newPoly.Faces.Remove(face);
            }

            newPoly.CullUnusedVertices();

            for (var faceIndex = 0; faceIndex < newPoly.Faces.Count; faceIndex++)
            {
                var face = newPoly.Faces[faceIndex];
                faceRoles.Add(existingFaceRoles[face.Centroid]);
                newFaceTags.Add(FaceTags[faceIndex]);
            }

            newPoly.FaceRoles = faceRoles;

            for (var i = 0; i < newPoly.Vertices.Count; i++)
            {
                var vert = newPoly.Vertices[i];
                vertexRoles.Add(existingVertexRoles[vert.Position]);
            }

            newPoly.VertexRoles = vertexRoles;
            newPoly.FaceTags = newFaceTags;
            return newPoly;
        }

        public void RemoveFace(int faceIndex)
        {
            var face = Faces[faceIndex];
            Faces.Remove(face);
            FaceRoles.Remove(FaceRoles[faceIndex]);
            FaceTags.Remove(FaceTags[faceIndex]);
            // TODO Vertex Roles
        }

        public (PolyMesh newPoly1, PolyMesh newPoly2) Split(OpParams o)
        {

            // Essentially the same code as _FaceRemove but we keep both polys
            // Both methods could probably be combined into a single one
            // but it didn't seem worth the extra complexity
            // Of course - you could just do:
            // var poly1 = _FaceRemove(o);
            // var poly2 = _FaceRemove(o, true);
            // return (poly1, poly2);
            // But that's slower - especially on complex polys.

            var existingFaceIndices = ListFacesByVertexIndices();
            var existingFaceRoles = new Dictionary<Vector3, Roles>();
            var existingVertexRoles = new Dictionary<Vector3, Roles>();

            var newPoly1 = Duplicate();
            var faceList1 = new List<Face>();
            var faceRoles1 = new List<Roles>();
            var vertexRoles1 = new List<Roles>();
            var newFaceTags1 = new List<HashSet<string>>();

            var newPoly2 = Duplicate();
            var faceList2 = new List<Face>();
            var faceRoles2 = new List<Roles>();
            var vertexRoles2 = new List<Roles>();
            var newFaceTags2 = new List<HashSet<string>>();

            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                existingFaceRoles[face.Centroid] = FaceRoles[faceIndex];
                var verts = face.GetVertices();
                for (var vertIndex = 0; vertIndex < verts.Count; vertIndex++)
                {
                    var vert = verts[vertIndex];
                    existingVertexRoles[vert.Position] = VertexRoles[existingFaceIndices[faceIndex][vertIndex]];
                }

                if (IncludeFace(faceIndex, o.filter))
                {
                    faceList1.Add(newPoly1.Faces[faceIndex]);
                }
                else
                {
                    faceList2.Add(newPoly2.Faces[faceIndex]);
                }
            }

            foreach (var t in faceList1) newPoly1.Faces.Remove(t);
            newPoly1.CullUnusedVertices();

            foreach (var t in faceList2) newPoly2.Faces.Remove(t);
            newPoly2.CullUnusedVertices();

            for (var faceIndex = 0; faceIndex < newPoly1.Faces.Count; faceIndex++)
            {
                faceRoles1.Add(existingFaceRoles[newPoly1.Faces[faceIndex].Centroid]);
                newFaceTags1.Add(FaceTags[faceIndex]);
            }

            newPoly1.FaceRoles = faceRoles1;

            for (var faceIndex = 0; faceIndex < newPoly2.Faces.Count; faceIndex++)
            {
                faceRoles2.Add(existingFaceRoles[newPoly2.Faces[faceIndex].Centroid]);
                newFaceTags2.Add(FaceTags[faceIndex]);
            }

            newPoly2.FaceRoles = faceRoles2;

            for (var i = 0; i < newPoly1.Vertices.Count; i++)
            {
                var vert = newPoly1.Vertices[i];
                vertexRoles1.Add(existingVertexRoles[vert.Position]);
            }

            newPoly1.VertexRoles = vertexRoles1;
            newPoly1.FaceTags = newFaceTags1;

            for (var i = 0; i < newPoly2.Vertices.Count; i++)
            {
                var vert = newPoly2.Vertices[i];
                vertexRoles2.Add(existingVertexRoles[vert.Position]);
            }

            newPoly2.VertexRoles = vertexRoles2;
            newPoly2.FaceTags = newFaceTags2;

            return (newPoly1, newPoly2);
        }

        /// <summary>
        /// Offsets a mesh by moving each vertex by the specified distance along its normal vector.
        /// </summary>
        /// <param name="offset">Offset distance</param>
        /// <returns>The offset mesh</returns>
        public PolyMesh Offset(float offset)
        {
            var offsetList = Enumerable.Range(0, Vertices.Count).Select(i => offset).ToList();
            return Offset(offsetList);
        }

        public PolyMesh Offset(OpParams o)
        {
            // This expects that faces are already split and don't share vertices

            var offsetList = new List<float>();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                float offset = o.GetValueA(this, faceIndex);
                var vertexOffset = IncludeFace(faceIndex, o.filter) ? offset : 0;
                for (var i = 0; i < Faces[faceIndex].GetVertices().Count; i++)
                {
                    offsetList.Add(vertexOffset);
                }
            }

            return Offset(offsetList);
        }

        public PolyMesh FaceMerge(OpParams o)
        {
            // TODO Breaks if the poly already has holes.
            var newPoly = Duplicate();
            newPoly = newPoly.FaceRemove(o);
            // Why do we do this?
            newPoly = newPoly.FaceRemove(new OpParams(Filter.Outer));
            newPoly = newPoly.FillHoles();
            return newPoly;
        }

        public PolyMesh Offset(List<float> offset)
        {
            Vector3[] points = new Vector3[Vertices.Count];
            float _offset;

            for (int i = 0; i < Vertices.Count && i < offset.Count; i++)
            {
                var vert = Vertices[i];
                _offset = offset[i];
                points[i] = vert.Position + Vertices[i].Normal * _offset;
            }

            return new PolyMesh(points, ListFacesByVertexIndices(), FaceRoles, VertexRoles, FaceTags);
        }

        /// <summary>
        /// Gives thickness to mesh faces by offsetting the mesh and connecting naked edges with new faces.
        /// </summary>
        /// <param name="distance">Distance to offset the mesh (thickness)</param>
        /// <param name="symmetric">Whether to extrude in both (-ve and +ve) directions</param>
        /// <returns>The extruded mesh (always closed)</returns>
        public PolyMesh Shell(float distance, bool symmetric = true)
        {
            var offsetList = Enumerable.Repeat(distance, Vertices.Count).ToList();
            return Shell(new OpParams(new OpFunc(x => offsetList[x.index])), symmetric);
        }

        public PolyMesh LayeredExtrude(int storeys, float storeyHeight = 1f)
        {
            var roof = Duplicate();
            var building = Duplicate();
            building = building.Flip();
            for (var i = 0; i < storeys; i++)
            {
                building = building.ExtendBoundaries(storeyHeight, 0, Vector3.up);
            }
            roof.Transform(new Vector3(0, storeys * storeyHeight, 0));
            building.Append(roof);
            building.FaceRoles = Enumerable.Repeat(Roles.Existing, building.FaceRoles.Count).ToList();
            building = building.Weld(0.001f);
            return building;
        }

        private PolyMesh Flip()
        {
            var flippedFaces = ListFacesByVertexIndices();
            for (var faceIndex = 0; faceIndex < flippedFaces.Length; faceIndex++)
            {
                var face = flippedFaces[faceIndex];
                face.Reverse();
                flippedFaces[faceIndex] = face;
            }
            return new PolyMesh(ListVerticesByPoints(), flippedFaces, FaceRoles, VertexRoles, FaceTags);
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="o"></param>
        /// <param name="symmetric"></param>
        /// <returns></returns>
        public PolyMesh Extrude(OpParams o)
        {
            o.funcB = o.funcA;
            o.funcA = new OpFunc(0);
            return Loft(o);
        }

        public PolyMesh Shell(OpParams o, bool symmetric = true, int segments = 1)
        {
            var newFaceTags = new List<HashSet<string>>();

            PolyMesh result, top;

            if (symmetric)
            {
                result = Offset(new OpParams(new OpFunc(x => -0.5f * o.funcA.eval(x))));
                top = Offset(new OpParams(new OpFunc(x => 0.5f * o.funcA.eval(x))));
            }
            else
            {
                result = Duplicate();
                top = Offset(o);
            }

            result.FaceRoles = Enumerable.Repeat(Roles.Existing, result.Faces.Count).ToList();
            result.VertexRoles = Enumerable.Repeat(Roles.Existing, result.Vertices.Count).ToList();
            newFaceTags.AddRange(FaceTags);

            result.Halfedges.Flip();

            // append top to ext (can't use Append() because copy would reverse face loops)
            foreach (var v in top.Vertices) result.Vertices.Add(v);
            foreach (var h in top.Halfedges) result.Halfedges.Add(h);
            for (var topFaceIndex = 0; topFaceIndex < top.Faces.Count; topFaceIndex++)
            {
                var f = top.Faces[topFaceIndex];
                result.Faces.Add(f);
                result.FaceRoles.Add(Roles.New);
                result.VertexRoles.AddRange(Enumerable.Repeat(Roles.New, f.Sides));
                newFaceTags.Add(new HashSet<string>(FaceTags[topFaceIndex]));
            }


            // get indices of naked halfedges in source mesh
            var naked = Halfedges.Select((item, index) => index).Where(i => Halfedges[i].Pair == null).ToList();

            if (naked.Count > 0)
            {
                int n = Halfedges.Count;
                int failed = 0;
                foreach (var i in naked)
                {
                    var newFaceTagSet = new HashSet<string>();
                    Vertex[] vertices =
                    {
                        result.Halfedges[i].Vertex,
                        result.Halfedges[i].Prev.Vertex,
                        result.Halfedges[i + n].Vertex,
                        result.Halfedges[i + n].Prev.Vertex
                    };

                    if (result.Faces.Add(vertices) == false)
                    {
                        failed++;
                    }
                    else
                    {
                        result.FaceRoles.Add(Roles.NewAlt);
                        int prevFaceIndex = result.Faces.IndexOf(result.Halfedges[i].Face);
                        var prevFaceTagSet = FaceTags[prevFaceIndex];
                        newFaceTagSet.UnionWith(prevFaceTagSet);
                        newFaceTags.Add(newFaceTagSet);
                    }
                }
            }

            result.FaceTags = newFaceTags;
            result.Halfedges.MatchPairs();

            return result;
        }

        public void ScalePolyhedra(float scale = 1)
        {
            if (Vertices.Count > 0)
            {
                // Find the furthest vertex
                Vertex max = Vertices.OrderByDescending(x => x.Position.sqrMagnitude).FirstOrDefault();
                float unitScale = 1.0f / max.Position.magnitude;
                for (var i = 0; i < Vertices.Count; i++)
                {
                    Vertices[i].Position *= unitScale * scale;
                }
            }
        }

        public (PolyMesh top, PolyMesh bottom, PolyMesh cap) SliceByPlane(Plane plane, bool Cap = false,
            bool includeTop = true, bool includeBottom = true, bool includeCap = false)
        {
            var topPoly = new PolyMesh();
            var topVerts = new List<Vector3>();
            var topFaces = new List<IEnumerable<int>>();
            var topFaceRoles = new List<Roles>();
            var topVertexRoles = new List<Roles>();
            var topNewVerts = new List<Vector3>();
            var topNewFace = new List<int>();

            var bottomPoly = new PolyMesh();
            var bottomVerts = new List<Vector3>();
            var bottomFaces = new List<IEnumerable<int>>();
            var bottomFaceRoles = new List<Roles>();
            var bottomVertexRoles = new List<Roles>();
            var bottomNewVerts = new List<Vector3>();
            var bottomNewFace = new List<int>();

            var capPoly = new PolyMesh();
            var capEdgeSegments = new List<(Vector3 start, Vector3? end)>();
            var capVerts = new List<List<Vector3>>();

            bool flipCaps;

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];

                if (includeTop)
                {
                    topNewFace.Clear();
                    topNewVerts.Clear();
                }

                if (includeBottom)
                {
                    bottomNewVerts.Clear();
                    bottomNewFace.Clear();
                }

                int edgeCounter = 0;
                var edge = face.Halfedge;

                do
                {
                    var point = edge.Vertex.Position;
                    var nextPoint = edge.Next.Vertex.Position;

                    if (plane.GetSide(point))
                    {
                        if (includeTop)
                        {
                            topNewVerts.Add(point);
                            topNewFace.Add(topNewVerts.Count - 1);
                        }
                    }
                    else
                    {
                        if (includeBottom)
                        {
                            bottomNewVerts.Add(point);
                            bottomNewFace.Add(bottomNewVerts.Count - 1);
                        }
                    }

                    bool intersected = !plane.SameSide(point, nextPoint);

                    if (intersected)
                    {
                        var vector = (nextPoint - point).normalized;
                        Ray ray = new Ray(point, vector);
                        float intersection;
                        plane.Raycast(ray, out intersection);
                        var newVert = point + (vector * intersection);

                        if (includeTop)
                        {
                            topNewVerts.Add(newVert);
                            topNewFace.Add(topNewVerts.Count - 1);
                        }

                        if (includeBottom)
                        {
                            bottomNewVerts.Add(newVert);
                            bottomNewFace.Add(bottomNewVerts.Count - 1);
                        }

                        if (Cap || includeCap)
                        {
                            if (capEdgeSegments.Count > 0)
                            {
                                var lastCap = capEdgeSegments.Last();
                                if (lastCap.Item2 == null)
                                {
                                    lastCap.Item2 = newVert;
                                    capEdgeSegments[capEdgeSegments.Count - 1] = lastCap;
                                }
                                else
                                {
                                    capEdgeSegments.Add((newVert, null));
                                }
                            }
                            else
                            {
                                capEdgeSegments.Add((newVert, null));
                            }
                        }
                    }

                    edge = edge.Next;
                    edgeCounter++;

                } while (edgeCounter < face.Sides);

                if (includeTop && topNewVerts.Count > 0)
                {
                    topFaces.Add(topNewFace.Select(x => x + topVerts.Count).ToList());
                    topVerts.AddRange(topNewVerts);
                    topFaceRoles.Add(FaceRoles[Faces.IndexOf(face)]);
                }

                if (includeBottom && bottomNewVerts.Count > 0)
                {
                    bottomFaces.Add(bottomNewFace.Select(x => x + bottomVerts.Count).ToList());
                    bottomVerts.AddRange(bottomNewVerts);
                    bottomFaceRoles.Add(FaceRoles[Faces.IndexOf(face)]);
                }
            }

            if ((Cap || includeCap) && capEdgeSegments.Count > 0)
            {
                // This isn't terribly reliable and the cap is sometimes facing the wrong direction.
                // However welding the result will fix cap orientation
                flipCaps = capEdgeSegments[0].end == capEdgeSegments[1].start;

                if (capEdgeSegments.Count > 0)
                {

                    capVerts = new List<List<Vector3>>();
                    var capEdgeSegmentsSeen = new HashSet<(Vector3 start, Vector3? end)>();
                    int failsafe1 = 0;
                    int seen = 0;

                    do
                    {
                        int failsafe2 = 0;
                        var thisCapVerts = new List<Vector3?>();
                        (Vector3 start, Vector3? end) nextSegment;
                        try
                        {
                            nextSegment = capEdgeSegments.First(x => !capEdgeSegmentsSeen.Contains(x));
                        }
                        catch (InvalidOperationException)
                        {
                            break;
                        }

                        thisCapVerts.Add(nextSegment.start);
                        thisCapVerts.Add(nextSegment.end);
                        var nextVert = nextSegment.end;
                        capEdgeSegmentsSeen.Add(nextSegment);
                        seen++;

                        do // Find a loop
                        {
                            try
                            {
                                nextSegment = capEdgeSegments.First(x =>
                                    (x.start == nextVert || x.end == nextVert) && !capEdgeSegmentsSeen.Contains(x));
                            }
                            catch (InvalidOperationException)
                            {
                                break;
                            }

                            capEdgeSegmentsSeen.Add(nextSegment);
                            seen++;
                            if (nextSegment.start == nextVert)
                            {
                                nextVert = nextSegment.end;
                            }
                            else
                            {
                                nextVert = nextSegment.start;
                            }

                            thisCapVerts.Add(nextVert);
                            failsafe2++;

                        } while (nextVert != thisCapVerts[0] && failsafe2 <= 30);

                        if (thisCapVerts.Count >= 3)
                        {
                            var capVertList = thisCapVerts.Select(x => x.GetValueOrDefault()).ToList();
                            capVertList.RemoveAt(capVertList.Count - 1);
                            capVerts.Add(capVertList);
                        }

                        failsafe1++;
                    } while (seen < capVerts.Count || failsafe1 < 30);
                }

                List<int> CalcIndices(List<Vector3> newVerts, int existingVertCount)
                {
                    var indices = Enumerable.Range(0, newVerts.Count)
                        .Select(x => x + existingVertCount)
                        .ToList();
                    return indices;
                }

                foreach (var cap in capVerts)
                {
                    if (Cap && includeTop)
                    {
                        var topCapIndices = CalcIndices(cap, topVerts.Count);
                        if (flipCaps)
                        {
                            topCapIndices.Reverse();
                        }

                        topVerts.AddRange(cap);
                        topFaces.Add(topCapIndices);
                        topFaceRoles.Add(Roles.New);
                    }

                    if (Cap && includeBottom)
                    {
                        var bottomCapIndices = CalcIndices(cap, bottomVerts.Count);
                        if (!flipCaps)
                        {
                            bottomCapIndices.Reverse();
                        }

                        bottomFaces.Add(bottomCapIndices);
                        bottomVerts.AddRange(cap);
                        bottomFaceRoles.Add(Roles.New);
                    }
                }
            }

            if (includeTop)
            {
                topVertexRoles.AddRange(Enumerable.Repeat(Roles.New, topVerts.Count));
                topPoly = new PolyMesh(topVerts, topFaces, topFaceRoles, topVertexRoles);
            }

            if (includeBottom)
            {
                bottomVertexRoles.AddRange(Enumerable.Repeat(Roles.New, bottomVerts.Count));
                bottomPoly = new PolyMesh(bottomVerts, bottomFaces, bottomFaceRoles, bottomVertexRoles);
            }

            if (includeCap && capEdgeSegments.Count > 0)
            {
                foreach (var cap in capVerts)
                {
                    capPoly = new PolyMesh(cap, new List<List<int>>
                    {
                        Enumerable.Range(0, cap.Count).ToList()
                    });
                }
            }

            return (topPoly, bottomPoly, capPoly);
        }

        public (PolyMesh inside, PolyMesh outside) SliceByPoly(PolyMesh slicePoly, bool Cap = false,
            int faceCount = 0)
        {
            var outsidePoly = new PolyMesh();

            var poly = Duplicate();

            for (var i = 0;
                 i < (faceCount == 0 ? slicePoly.Faces.Count : Mathf.Min(faceCount, slicePoly.Faces.Count));
                 i++)
            {
                var sliceFace = slicePoly.Faces[i];
                var result = poly.SliceByPlane(new Plane(sliceFace.Normal, sliceFace.Centroid), Cap);
                outsidePoly.Append(result.top);
                poly = result.bottom;
            }

            return (poly, outsidePoly);
        }

        public PolyMesh Segment(OpParams o)
        {

            var newVertices = new List<Vector3>();
            var newFaceIndices = new List<List<int>>();
            var newFaceRoles = new List<Roles>();
            var newVertexRoles = new List<Roles>();

            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var includeFace = IncludeFace(faceIndex, o.filter);

                if (includeFace)
                {
                    var oldFace = Faces[faceIndex];
                    var oldEdges = oldFace.GetHalfedges();
                    var normal = Vector3.LerpUnclamped(oldFace.Normal, oldFace.Centroid, o.GetValueB(this, faceIndex));
                    var offset = normal * o.GetValueA(this, faceIndex);
                    var centroid = Vector3.zero + offset;
                    newVertices.Add(centroid);
                    newVertexRoles.Add(Roles.New);
                    int centroidIndex = newVertices.Count - 1;
                    var newExistingFace = new List<int>();

                    var firstEdge = oldEdges[0];
                    newVertices.Add(firstEdge.Vertex.Position + offset);
                    newVertexRoles.Add(Roles.Existing);
                    int firstVertIndex = newVertices.Count - 1;

                    var he = oldFace.GetHalfedges();
                    for (var edgeIndex = 1; edgeIndex < he.Count; edgeIndex++)
                    {
                        var edge = he[edgeIndex];
                        newVertices.Add(edge.Vertex.Position + offset);
                        newVertexRoles.Add(Roles.Existing);
                        newExistingFace.Add(newVertices.Count - 1);

                        var newFace = new List<int>
                        {
                            newVertices.Count - 1,
                            newVertices.Count - 2,
                            centroidIndex
                        };
                        newFaceIndices.Add(newFace);
                        newFaceRoles.Add(Roles.New);
                        // newFaceTags.Add(new HashSet<string>());
                    }

                    newExistingFace.Add(firstVertIndex);
                    newFaceIndices.Add(new List<int>
                    {
                        firstVertIndex,
                        newVertices.Count - 1,
                        centroidIndex
                    });
                    newFaceRoles.Add(Roles.New);

                    newFaceIndices.Add(newExistingFace);
                    newFaceRoles.Add(Roles.Existing);
                }
            }

            var conway = new PolyMesh(newVertices, newFaceIndices, newFaceRoles, newVertexRoles);
            return conway;
        }

        public PolyMesh Weld(float distance)
        {
            if (distance < .00001f)
                distance = .00001f; // We always weld by a very small amount. Disable the op if you don't want to weld at all.
            var vertexPoints = new List<Vector3>();
            var faceIndices = new List<IEnumerable<int>>();
            var vertexRoles = new List<Roles>();
            var reverseDict = new Dictionary<Vertex, int>();
            var vertexReplacementDict = new Dictionary<int, int>();

            var groups = new List<Vertex[]>();
            var checkedVerts = new HashSet<Vertex>();

            InitOctree();

            for (var i = 0; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                reverseDict[v] = i;
                if (checkedVerts.Contains(v)) continue;
                checkedVerts.Add(v);
                var neighbours = FindNeighbours(v, distance);
                if (neighbours.Length < 1) continue;
                groups.Add(neighbours);
                checkedVerts.UnionWith(neighbours);
            }

            foreach (var group in groups)
            {
                vertexPoints.Add(@group[0].Position);
                int VertToKeep = -1;
                for (var i = 0; i < @group.Length; i++)
                {
                    var vertIndex = reverseDict[@group[i]];
                    if (i == 0)
                    {
                        VertToKeep = vertexPoints.Count - 1;
                    }

                    vertexReplacementDict[vertIndex] = VertToKeep;
                }
            }

            foreach (var faceVertIndices in ListFacesByVertexIndices())
            {
                var newFaceVertIndices = new List<int>();
                foreach (var vertIndex in faceVertIndices)
                {
                    newFaceVertIndices.Add(vertexReplacementDict[vertIndex]);
                }

                faceIndices.Add(newFaceVertIndices);
            }

            FaceRoles = FaceRoles.GetRange(0, faceIndices.Count);
            FaceTags = FaceTags?.GetRange(0, faceIndices.Count);
            vertexRoles = Vertices.Select(x => FaceRoles[Faces.IndexOf(x.Halfedge.Face)]).ToList();
            return new PolyMesh(vertexPoints, faceIndices, FaceRoles, vertexRoles, FaceTags);
        }

        public PolyMesh FillHoles()
        {
            var result = Duplicate();
            var boundaries = result.FindBoundaries();
            foreach (var boundary in boundaries)
            {
                if (boundary.Count < 3) continue;
                var success = result.Faces.Add(boundary.Select(x => x.Vertex));
                if (!success)
                {
                    boundary.Reverse();
                    success = result.Faces.Add(boundary.Select(x => x.Vertex));
                }

                if (success)
                {
                    result.FaceRoles.Add(Roles.New);
                    result.FaceTags.Add(new HashSet<string>());
                }
            }

            result.Halfedges.MatchPairs();
            return result;
        }

        public PolyMesh AppendMany(PolyMesh stashed, string tags = "", float scale = 1,
            float angle = 0, float offset = 0, bool toFaces = true, Filter filter = null)
        {
            var result = Duplicate();

            if (toFaces)
            {
                for (var i = 0; i < Faces.Count; i++)
                {
                    var face = Faces[i];
                    if (IncludeFace(i, filter))
                    {
                        Vector3 transform = face.Centroid + face.Normal * offset;
                        var rot = Quaternion.AngleAxis(angle, face.Normal);
                        result.Append(stashed, transform, rot, scale);
                    }
                }
            }
            else
            {
                for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
                {
                    var vert = Vertices[vertexIndex];
                    if (IncludeVertex(vertexIndex, filter))
                    {
                        Vector3 transform = vert.Position + vert.Normal * offset;
                        var rot = Quaternion.AngleAxis(angle, vert.Normal);
                        result.Append(stashed, transform, rot, scale);
                    }
                }
            }

            return result;
        }

        public PolyMesh PolyArray(List<Vector3> positionList, List<Vector3> directionList, List<Vector3> scaleList)
        {
            var result = new PolyMesh();

            for (var i = 0; i < positionList.Count; i++)
            {
                Vector3 transform = positionList[i];
                var rot = Quaternion.AngleAxis(0, directionList[i]);
                result.Append(this, transform, rot, scaleList[i]);
            }

            return result;
        }

        /// <summary>
        /// Appends a copy of another mesh to this one.
        /// </summary>
        /// <param name="other">Mesh to append to this one.</param>
        public void Append(PolyMesh other)
        {
            Append(other, Vector3.zero, Quaternion.identity, 1.0f);
        }

        private void Append(PolyMesh other, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Append(other, Matrix4x4.TRS(position, rotation, scale));
        }

        public void Append(PolyMesh other, Vector3 position, Quaternion rotation, float scale)
        {
            Append(other, position, rotation, Vector3.one * scale);
        }

        public void Append(PolyMesh other, Vector3 position)
        {
            Append(other, position, Quaternion.identity, Vector3.one);
        }

        public void Append(PolyMesh other, Vector3 position, Quaternion rotation)
        {
            Append(other, position, rotation, Vector3.one);
        }

        public void Append(PolyMesh other, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Append(other, position, Quaternion.Euler(rotation), scale);
        }

        public void Append(PolyMesh other, Vector3 position, float scale)
        {
            Append(other, position, Quaternion.identity, Vector3.one * scale);
        }

        public void Append(PolyMesh other, Matrix4x4 matrix)
        {
            if (other == null) return;
            PolyMesh dup = other.Duplicate(matrix);

            Vertices.AddRange(dup.Vertices);
            for (var i = 0; i < dup.Halfedges.Count; i++)
            {
                Halfedges.Add(dup.Halfedges[i]);
            }

            for (var i = 0; i < dup.Faces.Count; i++)
            {
                Faces.Add(dup.Faces[i]);
            }

            FaceRoles.AddRange(dup.FaceRoles);
            VertexRoles.AddRange(dup.VertexRoles);
            FaceTags?.AddRange(dup.FaceTags);
        }

        public PolyMesh Duplicate()
        {
            // Export to face/vertex and rebuild
            return new PolyMesh(ListVerticesByPoints(), ListFacesByVertexIndices(), FaceRoles, VertexRoles, FaceTags);
        }

        public PolyMesh Duplicate(Matrix4x4 matrix)
        {
            IEnumerable<Vector3> verts;

            if (matrix == Matrix4x4.identity)
            {
                // Fast path
                verts = ListVerticesByPoints();
            }
            else
            {
                verts = ListVerticesByPoints().Select(i => matrix.MultiplyPoint(i));
            }

            return new PolyMesh(verts, ListFacesByVertexIndices(), FaceRoles, VertexRoles, FaceTags);
        }

        public PolyMesh Duplicate(Vector3 transform, Quaternion rotation)
        {
            return Duplicate(Matrix4x4.TRS(transform, rotation, Vector3.one));
        }

        public PolyMesh Duplicate(Vector3 transform, float scale)
        {
            return Duplicate(Matrix4x4.TRS(transform, Quaternion.identity, Vector3.one * scale));
        }

        public PolyMesh Duplicate(Vector3 transform)
        {
            return Duplicate(Matrix4x4.TRS(transform, Quaternion.identity, Vector3.one));
        }

        public PolyMesh Duplicate(Vector3 transform, Vector3 rotation, Vector3 scale)
        {
            return Duplicate(Matrix4x4.TRS(transform, Quaternion.Euler(rotation), scale));
        }

        public PolyMesh Duplicate(Vector3 transform, Quaternion rotation, Vector3 scale)
        {
            return Duplicate(Matrix4x4.TRS(transform, rotation, scale));
        }

        public void _ExtendBoundaries(List<List<Halfedge>> boundaries, float scale, Vector3 directionVector, float angle = 0)
        {
            
            FaceRoles = Enumerable.Repeat(Roles.Existing, Faces.Count).ToList();
            var newFaceTags = new List<HashSet<string>>();
            newFaceTags.AddRange(FaceTags);

            foreach (var boundary in boundaries)
            {
                int firstNewVertexIndex = Vertices.Count;
                for (var edgeIndex = 0; edgeIndex < boundary.Count; edgeIndex++)
                {
                    Vector3 direction;
                    var edge1 = boundary[edgeIndex];
                    
                    // If directionVector is zero then calculate a direction based on angle and the edge's normal
                    if (directionVector == Vector3.zero)
                    {
                        var direction1 = edge1.Midpoint - edge1.Face.Centroid;
                        var edge2 = boundary[(edgeIndex + boundary.Count - 1) % boundary.Count];
                        var direction2 = edge2.Midpoint - edge2.Face.Centroid;
                        direction = direction1 == direction2 ? direction1 : direction1 + direction2;
                        var normal = (edge1.Face.Normal + edge2.Face.Normal) / 2f;
                        direction = Vector3.LerpUnclamped(direction, normal, angle / 90f).normalized;
                    }
                    else
                    {
                        direction = directionVector;
                    }
                    
                    Vertices.Add(new Vertex(edge1.Vertex.Position + direction * scale));
                    VertexRoles.Add(Roles.New);
                }

                for (var edgeIndex = 0; edgeIndex < boundary.Count; edgeIndex++)
                {
                    var edge = boundary[edgeIndex];
                    bool success = Faces.Add(new[]
                    {
                        edge.Vertex,
                        edge.Prev.Vertex,
                        Vertices[firstNewVertexIndex + ((edgeIndex + 1) % boundary.Count)],
                        Vertices[firstNewVertexIndex + edgeIndex],
                    });
                    if (success)
                    {
                        FaceRoles.Add(Roles.New);
                        var prevFaceTagSet = FaceTags[Faces.IndexOf(edge.Face)];
                        FaceTags.Add(prevFaceTagSet);
                    }
                }
            }
        }

        public PolyMesh ConvexHull(bool splitVerts=false)
        {
            PolyMesh poly;
            try
            {
                poly = _ConvexHull(splitVerts);
            }
#pragma warning disable CS0168
            catch (ArgumentException e)
#pragma warning restore CS0168
            {  // Coplanar
                poly = Shell(.001f);
                poly = poly._ConvexHull();
            }
            return poly;
        }
        
        public PolyMesh _ConvexHull(bool splitVerts = false)
        {
            
            var points = Vertices.Select(p => p.Position).ToList();
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            
        
            var hull = new ConvexHullCalculator();
            hull.GenerateHull(points, splitVerts, ref verts, ref tris, ref normals);
            var faces = tris.Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 3)
                .Select(x => x.Select(v => v.Value));
            var conway = new PolyMesh(verts, faces);
            return conway;
        }

        public PolyMesh Hinge(float amount)
        {
            // Rotate singly connected faces around the connected edge
            foreach (Face f in Faces)
            {
                Halfedge hinge = null;

                // Find a single connected edge
                foreach (Halfedge e in f.GetHalfedges())
                {
                    if (e.Pair != null) // This edge is connected
                    {
                        if (hinge == null) // Our first connected edge
                        {
                            // Record the first connected edge and keep looking
                            hinge = e;
                        }
                        else // We already found a hinge for this face
                        {
                            // Therefore this Face has more than 1 connected edge
                            hinge = null;
                            break;
                        }
                    }
                }

                if (hinge != null) // We found a single hinge for this face
                {
                    Vector3 axis = hinge.Vector;
                    Quaternion rotation = Quaternion.AngleAxis(amount, axis);

                    var vs = f.GetVertices();
                    for (var i = 0; i < vs.Count; i++)
                    {
                        Vertex v = vs[i];
                        // Only rotate vertices that aren't part of the hinge
                        if (v != hinge.Vertex && v != hinge.Pair.Vertex)
                        {
                            v.Position -= hinge.Vertex.Position;
                            v.Position = rotation * v.Position;
                            v.Position += hinge.Vertex.Position;
                        }
                    }
                }
            }

            return this;
        }

        public PolyMesh ExtendBoundaries(OpParams o, Vector3 directionOverride)
        {
            float amount = o.GetValueA(this, 0);
            float angle = o.GetValueB(this, 0);
            return ExtendBoundaries(amount, angle, directionOverride);
        }
        
        public PolyMesh ExtendBoundaries(float amount, float angle, Vector3 direction)
        {
            var poly = Duplicate();
            var boundaries = poly.FindBoundaries();
            poly._ExtendBoundaries(boundaries, amount, direction, angle);
            return poly;
        }

        public PolyMesh SplitLoop(List<Tuple<int, int>> loop, float ratio = 0.5f)
        {
            var poly = Duplicate();
            var vertexRoles = poly.VertexRoles;
            var newFaceTags = poly.FaceTags;
            var faces = poly.Faces;
            int firstNewVertexIndex = poly.Vertices.Count;
            var newVertLookup = new Dictionary<(Guid, Guid)?, int>();
            foreach (var loopItem in loop)
            {
                var face = faces[loopItem.Item1];
                var edge = face.GetHalfedges()[loopItem.Item2];
                Vector3 pos = edge.PointAlongEdge(ratio);
                var newVert = new Vertex(pos);
                poly.Vertices.Add(newVert);
                vertexRoles.Add(Roles.New);
                newVertLookup[edge.PairedName] = poly.Vertices.Count - 1;
            }

            if (loop.Count > 0)
            {
                var lastFace = faces[loop.Last().Item1];
                int lastEdgeIndex = loop.Last().Item2;
                lastEdgeIndex = PolyMesh.ActualMod(lastEdgeIndex + (lastFace.Sides / 2), lastFace.Sides);
                var lastEdge = lastFace.GetHalfedges()[lastEdgeIndex];
                if (lastEdge.Pair == null)
                {
                    var lastNewVert = new Vertex(lastEdge.Midpoint);
                    poly.Vertices.Add(lastNewVert);
                    vertexRoles.Add(Roles.New);
                    newVertLookup[lastEdge.PairedName] = poly.Vertices.Count - 1;
                }
            }

            var facesToRemove = new List<int>();
            var facesToAdd = new List<List<int>>();
            var newFaceRoles = new List<Roles>();
            for (var loopIndex = 0; loopIndex < loop.Count; loopIndex++)
            {
                var loopItem = loop[loopIndex];
                int nextLoopIndex = (loopIndex + 1) % loop.Count;
                var nextLoopItem = loop[nextLoopIndex];

                var face = faces[loopItem.Item1];
                var initialEdge = face.GetHalfedges()[loopItem.Item2];
                var currentEdge = initialEdge;
                var nextFace = faces[nextLoopItem.Item1];

                var newFace1 = new List<int>();
                int counter = 0;
                bool finished = false;
                while (!finished)
                {
                    newFace1.Add(poly.Vertices.IndexOf(currentEdge.Vertex));
                    currentEdge = currentEdge.Next;
                    counter++;
                    finished = counter >= face.Sides / 2;
                }

                if (newVertLookup.ContainsKey(currentEdge.PairedName))
                {
                    newFace1.Add(newVertLookup[currentEdge.PairedName]);
                }

                if (newVertLookup.ContainsKey(initialEdge.PairedName))
                {
                    newFace1.Add(newVertLookup[initialEdge.PairedName]);
                }

                initialEdge = currentEdge;
                var newFace2 = new List<int>();
                finished = false;
                while (!finished)
                {
                    newFace2.Add(poly.Vertices.IndexOf(currentEdge.Vertex));
                    currentEdge = currentEdge.Next;
                    counter++;
                    finished = counter >= face.Sides;
                }

                if (newVertLookup.ContainsKey(currentEdge.PairedName))
                {
                    newFace2.Add(newVertLookup[currentEdge.PairedName]);
                }

                if (newVertLookup.ContainsKey(initialEdge.PairedName))
                {
                    newFace2.Add(newVertLookup[initialEdge.PairedName]);
                }

                facesToRemove.Add(loopItem.Item1);

                if (newFace1.Count >= 3)
                {
                    facesToAdd.Add(newFace1);
                    newFaceRoles.Add(Roles.New);
                    var prevFaceTagSet = poly.FaceTags[loopItem.Item1];
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }

                if (newFace2.Count >= 3)
                {
                    facesToAdd.Add(newFace2);
                    newFaceRoles.Add(Roles.NewAlt);
                    var prevFaceTagSet = poly.FaceTags[loopItem.Item1];
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            //var prevTags = new Dictionary<int, HashSet<Tuple<string, TagType>>>();
            var allVertices = poly.Vertices.Select(v => v.Position).ToList();
            var faceIndices = new List<List<int>>();
            var facesVertList = poly.ListFacesByVertexIndices();
            for (var faceIndex = 0; faceIndex < facesVertList.Length; faceIndex++)
            {
                if (!facesToRemove.Contains(faceIndex)) faceIndices.Add(facesVertList[faceIndex]);
            }

            var faceRoles = Enumerable.Repeat(Roles.Existing, faceIndices.Count).ToList();
            faceIndices.AddRange(facesToAdd);
            faceRoles.AddRange(newFaceRoles);

            return new PolyMesh(allVertices, faceIndices, faceRoles, vertexRoles);
        }

        public PolyMesh MultiSplitLoop(List<Tuple<int, int>> loop, float ratio = 0.5f, int divisions = 1)
        {
            // WIP
            // Completely forgotten what this is meant to do!

            var poly = Duplicate();
            if (loop.Count == 0)
            {
                Debug.LogWarning("Empty loop");
                return poly;
            }

            var vertexRoles = poly.VertexRoles;
            var newFaceTags = poly.FaceTags;
            var faces = poly.Faces;
            var newVertLookup = new Dictionary<(Guid, Guid, int)?, int>();
            (Guid, Guid) guidTuple;

            for (int division = 1; division < divisions; division++)
            {
                foreach (var loopItem in loop)
                {
                    var face = faces[loopItem.Item1];
                    var edge = face.GetHalfedges()[loopItem.Item2];
                    float step = (1f / divisions) * division;
                    float div = (float)division / divisions;
                    Vector3 pos = edge.PointAlongEdge(div);
                    var newVert = new Vertex(pos);
                    poly.Vertices.Add(newVert);
                    vertexRoles.Add(Roles.New);
                    guidTuple = edge.PairedName.Value;
                    newVertLookup[(guidTuple.Item1, guidTuple.Item2, division)] = poly.Vertices.Count - 1;
                }

                var lastFace = faces[loop.Last().Item1];
                int lastEdgeIndex = loop.Last().Item2;
                lastEdgeIndex = ActualMod(lastEdgeIndex + (lastFace.Sides / 2), lastFace.Sides);
                var lastEdge = lastFace.GetHalfedges()[lastEdgeIndex];
                if (lastEdge.Pair == null)
                {
                    var lastNewVert = new Vertex(lastEdge.Midpoint);
                    poly.Vertices.Add(lastNewVert);
                    vertexRoles.Add(Roles.New);
                    guidTuple = lastEdge.PairedName.Value;

                    newVertLookup[(guidTuple.Item1, guidTuple.Item2, division)] = poly.Vertices.Count - 1;
                }
            }

            var facesToRemove = new List<int>();
            var facesToAdd = new List<List<int>>();
            var newFaceRoles = new List<Roles>();

            for (int division = 1; division < divisions; division++)
            {
                for (var loopIndex = 0; loopIndex < loop.Count; loopIndex++)
                {
                    var loopItem = loop[loopIndex];

                    var face = faces[loopItem.Item1];
                    var initialEdge = face.GetHalfedges()[loopItem.Item2];
                    var currentEdge = initialEdge;

                    var newFace1 = new List<int>();
                    int counter = 0;
                    bool finished = false;
                    while (!finished)
                    {
                        newFace1.Add(poly.Vertices.IndexOf(currentEdge.Vertex));
                        currentEdge = currentEdge.Next;
                        counter++;
                        finished = counter >= face.Sides / 2;
                    }

                    guidTuple = currentEdge.PairedName.Value;
                    var currentEdgeKey = (guidTuple.Item1, guidTuple.Item2, division);
                    if (newVertLookup.ContainsKey(currentEdgeKey))
                    {
                        newFace1.Add(newVertLookup[currentEdgeKey]);
                    }

                    guidTuple = initialEdge.PairedName.Value;
                    var initialEdgeKey = (guidTuple.Item1, guidTuple.Item2, division);
                    if (newVertLookup.ContainsKey(initialEdgeKey))
                    {
                        newFace1.Add(newVertLookup[initialEdgeKey]);
                    }

                    initialEdge = currentEdge;
                    var newFace2 = new List<int>();
                    finished = false;
                    while (!finished)
                    {
                        newFace2.Add(poly.Vertices.IndexOf(currentEdge.Vertex));
                        currentEdge = currentEdge.Next;
                        counter++;
                        finished = counter >= face.Sides;
                    }

                    guidTuple = currentEdge.PairedName.Value;
                    currentEdgeKey = (guidTuple.Item1, guidTuple.Item2, division);
                    if (newVertLookup.ContainsKey(currentEdgeKey))
                    {
                        newFace2.Add(newVertLookup[currentEdgeKey]);
                    }

                    guidTuple = initialEdge.PairedName.Value;
                    initialEdgeKey = (guidTuple.Item1, guidTuple.Item2, division);
                    if (newVertLookup.ContainsKey(initialEdgeKey))
                    {
                        newFace2.Add(newVertLookup[initialEdgeKey]);
                    }

                    facesToRemove.Add(loopItem.Item1);

                    if (newFace1.Count >= 3)
                    {
                        facesToAdd.Add(newFace1);
                        newFaceRoles.Add(Roles.New);
                        var prevFaceTagSet = poly.FaceTags[loopItem.Item1];
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }

                    if (newFace2.Count >= 3)
                    {
                        facesToAdd.Add(newFace2);
                        newFaceRoles.Add(Roles.NewAlt);
                        var prevFaceTagSet = poly.FaceTags[loopItem.Item1];
                        newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                    }
                }
            }


            //var prevTags = new Dictionary<int, HashSet<Tuple<string, TagType>>>();
            var allVertices = poly.Vertices.Select(v => v.Position).ToList();
            var faceIndices = new List<List<int>>();
            var facesVertList = poly.ListFacesByVertexIndices();
            for (var faceIndex = 0; faceIndex < facesVertList.Length; faceIndex++)
            {
                if (!facesToRemove.Contains(faceIndex)) faceIndices.Add(facesVertList[faceIndex]);
            }

            var faceRoles = Enumerable.Repeat(Roles.Existing, faceIndices.Count).ToList();
            faceIndices.AddRange(facesToAdd);
            faceRoles.AddRange(newFaceRoles);

            return new PolyMesh(allVertices, faceIndices, faceRoles, vertexRoles);
        }

        public List<Tuple<int, int>> GetFaceLoop(int startingEdgeIndex)
        {
            var edge = Halfedges[startingEdgeIndex];
            return GetFaceLoop(edge);
        }

        public List<Tuple<int, int>> GetFaceLoop(Halfedge startingEdge)
        {
            var loop = new List<Tuple<int, int>>();
            if (startingEdge.Face.Sides % 2 != 0) return loop;
            var faceLookup = new Dictionary<string, int>();
            for (var faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];
                faceLookup[face.Name] = faceIndex;
            }

            bool finished = false;
            int failsafe = 0;
            var currentEdge = startingEdge;
            int edgeCounter;
            while (!finished)
            {
                loop.Add(new Tuple<int, int>(
                    faceLookup[currentEdge.Face.Name],
                    currentEdge.Face.GetHalfedges().IndexOf(currentEdge)
                ));
                var currentFace = currentEdge.Face;
                edgeCounter = 0;
                while (edgeCounter < currentFace.Sides / 2)
                {
                    currentEdge = currentEdge.Next;
                    edgeCounter++;
                }

                currentEdge = currentEdge.Pair;
                failsafe++;
                finished = currentEdge == null ||
                           currentEdge.Face.Sides % 2 != 0 ||
                           currentEdge == startingEdge ||
                           failsafe > 100;
            }

            if (currentEdge == null && startingEdge.Pair != null)
            {
                currentEdge = startingEdge.Pair;
                finished = false;

                while (!finished)
                {
                    var currentFace = currentEdge.Face;
                    edgeCounter = 0;
                    while (edgeCounter < currentFace.Sides / 2)
                    {
                        currentEdge = currentEdge.Next;
                        edgeCounter++;
                    }

                    loop.Insert(0, new Tuple<int, int>(
                        faceLookup[currentEdge.Face.Name],
                        currentEdge.Face.GetHalfedges().IndexOf(currentEdge)
                    ));
                    currentEdge = currentEdge.Pair;
                    failsafe++;
                    finished = currentEdge == null ||
                               currentEdge.Face.Sides % 2 != 0 ||
                               currentEdge == startingEdge ||
                               failsafe > 100;

                }

            }

            return loop;
        }

        public PolyMesh LoftAlongProfile(float height = 1f, int segments = 4,
            Easing.EasingType easingType = Easing.EasingType.Linear, bool lace = false)
        {
            var ratioValues = new List<float>();
            var offsetValues = new List<float>();
            for (float segment = 0; segment < segments; segment++)
            {
                float i = segment / segments;
                ratioValues.Add(i);
                offsetValues.Add(Easing.Funcs[easingType](i));
            }

            return LoftAlongProfile(new OpParams(1, height), ratioValues, offsetValues);
        }

        public PolyMesh LoftAlongProfile(OpParams o, AnimationCurve profile,
            AnimationCurve shear = null, AnimationCurve shearDirection = null,
            bool flipProfile = false)
        {
            List<float> offsetValues, ratioValues;
            if (flipProfile)
            {
                offsetValues = profile.keys.Select(x => 1 - x.value).ToList();
                ratioValues = profile.keys.Select(x => x.time).ToList();
            }
            else
            {
                ratioValues = profile.keys.Select(x => 1 - x.value).ToList();
                offsetValues = profile.keys.Select(x => x.time).ToList();
            }

            List<float> shearValues = null;
            if (shear != null)
            {
                shearValues = new List<float>();
                foreach (var key in profile.keys)
                {
                    shearValues.Add(shear.Evaluate(key.time));
                }
            }

            List<float> shearDirectionValues = null;
            if (shear != null && shearDirection != null)
            {
                shearDirectionValues = new List<float>();
                foreach (var key in profile.keys)
                {
                    shearDirectionValues.Add(shearDirection.Evaluate(key.time));
                }
            }

            return LoftAlongProfile(o,
                ratioValues, offsetValues,
                shearValues, shearDirectionValues);
        }

        public PolyMesh LoftAlongProfile(OpParams o,
            List<float> ratioValues, List<float> offsetValues,
            List<float> shearValues = null, List<float> shearDirectionValues = null)
        {
            bool lace = false; // TODO

            var newFaceTags = new List<HashSet<string>>();
            var faceIndices = new List<int[]>();
            var vertexPoints = new List<Vector3>();
            var existingVertices = new Dictionary<Vector3, int>();
            var newVertices = new Dictionary<((Guid, Guid)?, int), int>();

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
                float ratio = o.GetValueA(this, faceIndex);
                float offset = o.GetValueB(this, faceIndex);
                var prevFaceTagSet = FaceTags[faceIndex];
                var face = Faces[faceIndex];
                int faceSides = face.Sides;
                var faceNormal = face.Normal;
                var offsetVector = faceNormal * offset;

                if (IncludeFace(faceIndex, o.filter))
                {

                    var edge = face.Halfedge;
                    var centroid = face.Centroid;

                    int initialVertIndex = vertexPoints.Count;

                    for (int section = 0; section < offsetValues.Count; section++)
                    {

                        float profileX = offsetValues[section];
                        float profileY = ratioValues[section];

                        // Create vertices                        
                        for (int i = 0; i < faceSides; i++)
                        {
                            Vector3 origin = (lace && section % 2 != 0) ? edge.Midpoint : edge.Vertex.Position;
                            var newVertex = Vector3.LerpUnclamped(
                                origin,
                                centroid,
                                ratio * profileY
                            );

                            newVertex += offsetVector * profileX;

                            if (shearValues != null && shearValues[section] != 0)
                            {
                                float amount = shearValues[section];
                                float direction = shearDirectionValues?[section] ?? 0;
                                Vector3 tangent, tangentLeft, tangentUp, t1, t2;

                                t1 = Vector3.Cross(faceNormal, Vector3.forward);
                                t2 = Vector3.Cross(faceNormal, Vector3.left);

                                if (t1.magnitude > t2.magnitude)
                                {
                                    tangentUp = t1;
                                }
                                else
                                {
                                    tangentUp = t2;
                                }

                                t2 = Vector3.Cross(faceNormal, Vector3.up);

                                if (t1.magnitude > t2.magnitude)
                                {
                                    tangentLeft = t1;
                                }
                                else
                                {
                                    tangentLeft = t2;
                                }

                                tangent = Vector3.SlerpUnclamped(tangentUp, tangentLeft, direction);
                                var vector = tangent * amount;
                                newVertex = newVertex + vector;
                            }

                            vertexPoints.Add(newVertex);
                            vertexRoles.Add(Roles.New);
                            newVertices[(edge.Name, section)] = vertexIndex++;
                            edge = edge.Next;
                        }

                        // Only build faces from the second row onwards
                        if (section == 0) continue;

                        int sectionVertIndexOffset = initialVertIndex + (section * faceSides);

                        // Generate new faces
                        for (int i = 0; i < faceSides; i++)
                        {
                            if (lace)
                            {
                                var newEdgeFace1 = new[]
                                {
                                    sectionVertIndexOffset + i - faceSides,
                                    sectionVertIndexOffset + ((i + 1) % faceSides) - faceSides,
                                    sectionVertIndexOffset + ((i + 1) % (faceSides))
                                };
                                faceIndices.Add(newEdgeFace1);
                                faceRoles.Add(section % 2 == 0 ? Roles.New : Roles.NewAlt);
                                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                                var newEdgeFace2 = new[]
                                {
                                    sectionVertIndexOffset + i - faceSides,
                                    sectionVertIndexOffset + ((i + 1) % (faceSides)),
                                    sectionVertIndexOffset + i
                                };
                                faceIndices.Add(newEdgeFace2);
                                faceRoles.Add(section % 2 == 0 ? Roles.New : Roles.NewAlt);
                                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));


                            }
                            else
                            {
                                var newEdgeFace = new[]
                                {
                                    sectionVertIndexOffset + i - faceSides,
                                    sectionVertIndexOffset + ((i + 1) % faceSides) - faceSides,
                                    sectionVertIndexOffset + ((i + 1) % (faceSides)),
                                    sectionVertIndexOffset + i,
                                };
                                faceIndices.Add(newEdgeFace);
                                faceRoles.Add(section % 2 == 0 ? Roles.New : Roles.NewAlt);
                                newFaceTags.Add(new HashSet<string>(prevFaceTagSet));

                            }
                        }

                    }

                    // Cap
                    var newInsetFace = new List<int>();
                    for (int i = faceSides; i > 0; i--)
                    {
                        newInsetFace.Add(vertexPoints.Count - i);
                    }

                    ;
                    faceIndices.Add(newInsetFace.ToArray());
                    faceRoles.Add(Roles.Existing);
                    newFaceTags.Add(new HashSet<string>(prevFaceTagSet));
                }
            }

            var poly = new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, newFaceTags);
            //Add the ignored faces back in
            var ignored = FaceRemove(new OpParams(o.filter));
            ignored.SetFaceRoles(Roles.Ignored);
            ignored.SetVertexRoles(Roles.Ignored);
            poly.Append(ignored);
            return poly;
        }



        /// <summary>
        /// Thickens each mesh edge in the plane of the mesh surface.
        /// </summary>
        /// <param name="offset">Distance to offset edges in plane of adjacent faces</param>
        /// <param name="boundaries">If true, attempt to ribbon boundary edges</param>
        /// <returns>The ribbon mesh</returns>
        // public PolyMesh Ribbon(float offset, Boolean boundaries, float smooth)
        // {
        //
        // 	PolyMesh ribbon = Duplicate();
        // 	var orig_faces = ribbon.Faces.ToArray();
        //
        // 	List<List<Halfedge>> incidentEdges = ribbon.Vertices.Select(v => v.Halfedges).ToList();
        //
        // 	// create new "vertex" faces
        // 	List<List<Vertex>> all_new_vertices = new List<List<Vertex>>();
        // 	for (int k = 0; k < Vertices.Count; k++)
        // 	{
        // 		Vertex v = ribbon.Vertices[k];
        // 		List<Vertex> new_vertices = new List<Vertex>();
        // 		List<Halfedge> halfedges = incidentEdges[k];
        // 		Boolean boundary = halfedges[0].Next.Pair != halfedges[halfedges.Count - 1];
        //
        // 		// if the edge loop around this vertex is open, close it with 'temporary edges'
        // 		if (boundaries && boundary)
        // 		{
        // 			Halfedge a, b;
        // 			a = halfedges[0].Next;
        // 			b = halfedges[halfedges.Count - 1];
        // 			if (a.Pair == null)
        // 			{
        // 				a.Pair = new Halfedge(a.Prev.Vertex) {Pair = a};
        // 			}
        //
        // 			if (b.Pair == null)
        // 			{
        // 				b.Pair = new Halfedge(b.Prev.Vertex) {Pair = b};
        // 			}
        //
        // 			a.Pair.Next = b.Pair;
        // 			b.Pair.Prev = a.Pair;
        // 			a.Pair.Prev = a.Pair.Prev ?? a; // temporary - to allow access to a.Pair's start/end vertices
        // 			halfedges.Add(a.Pair);
        // 		}
        //
        // 		foreach (Halfedge edge in halfedges)
        // 		{
        // 			if (halfedges.Count < 2)
        // 			{
        // 				continue;
        // 			}
        //
        // 			Vector3 normal = edge.Face != null ? edge.Face.Normal : Vertices[k].Normal;
        // 			Halfedge edge2 = edge.Next;
        //
        // 			var o1 = new Vertex(Vector3.Cross(normal, edge.Vector).normalized * offset);
        // 			var o2 = new Vertex(Vector3.Cross(normal, edge2.Vector).normalized * offset);
        //
        // 			if (edge.Face == null)
        // 			{
        // 				// boundary condition: create two new vertices in the plane defined by the vertex normal
        // 				Vertex v1 = new Vertex(v.Position + (edge.Vector * (1 / edge.Vector.magnitude) * -offset) +
        // 				                       o1.Position);
        // 				Vertex v2 = new Vertex(v.Position + (edge2.Vector * (1 / edge2.Vector.magnitude) * offset) +
        // 				                       o2.Position);
        // 				ribbon.Vertices.Add(v2);
        // 				ribbon.Vertices.Add(v1);
        // 				new_vertices.Add(v2);
        // 				new_vertices.Add(v1);
        // 				Halfedge c = new Halfedge(v2, edge2, edge, null);
        // 				edge.Next = c;
        // 				edge2.Prev = c;
        // 			}
        // 			else
        // 			{
        // 				// internal condition: offset each edge in the plane of the shared face and create a new vertex where they intersect eachother
        //
        // 				Vector3 start1 = edge.Vertex.Position + o1.Position;
        // 				Vector3 end1 = edge.Prev.Vertex.Position + o1.Position;
        // 				Line l1 = new Line(start1, end1);
        //
        // 				Vector3 start2 = edge2.Vertex.Position + o2.Position;
        // 				Vector3 end2 = edge2.Prev.Vertex.Position + o2.Position;
        // 				Line l2 = new Line(start2, end2);
        //
        // 				Vector3 intersection;
        // 				l1.Intersect(out intersection, l2);
        // 				ribbon.Vertices.Add(new Vertex(intersection));
        // 				new_vertices.Add(new Vertex(intersection));
        // 			}
        // 		}
        //
        // 		if ((!boundaries && boundary) == false) // only draw boundary node-faces in 'boundaries' mode
        // 			ribbon.Faces.Add(new_vertices);
        // 		all_new_vertices.Add(new_vertices);
        // 	}
        //
        // 	// change edges to reference new vertices
        // 	for (int k = 0; k < Vertices.Count; k++)
        // 	{
        // 		Vertex v = ribbon.Vertices[k];
        // 		if (all_new_vertices[k].Count < 1)
        // 		{
        // 			continue;
        // 		}
        //
        // 		int c = 0;
        // 		foreach (Halfedge edge in incidentEdges[k])
        // 		{
        // 			if (!ribbon.Halfedges.SetVertex(edge, all_new_vertices[k][c++]))
        // 				edge.Vertex = all_new_vertices[k][c];
        // 		}
        //
        // 		//v.Halfedge = null; // unlink from halfedge as no longer in use (culled later)
        // 		// note: new vertices don't link to any halfedges in the mesh until later
        // 	}
        //
        // 	// cull old vertices
        // 	ribbon.Vertices.RemoveRange(0, Vertices.Count);
        //
        // 	// use existing edges to create 'ribbon' faces
        // 	MeshHalfedgeList temp = new MeshHalfedgeList();
        // 	for (int i = 0; i < Halfedges.Count; i++)
        // 	{
        // 		temp.Add(ribbon.Halfedges[i]);
        // 	}
        //
        // 	List<Halfedge> items = temp.GetUnique();
        //
        // 	foreach (Halfedge halfedge in items)
        // 	{
        // 		if (halfedge.Pair != null)
        // 		{
        // 			// insert extra vertices close to the new 'vertex' vertices to preserve shape when subdividing
        // 			if (smooth > 0.0)
        // 			{
        // 				if (smooth > 0.5)
        // 				{
        // 					smooth = 0.5f;
        // 				}
        //
        // 				Vertex[] newVertices = new Vertex[]
        // 				{
        // 					new Vertex(halfedge.Vertex.Position + (-smooth * halfedge.Vector)),
        // 					new Vertex(halfedge.Prev.Vertex.Position + (smooth * halfedge.Vector)),
        // 					new Vertex(halfedge.Pair.Vertex.Position + (-smooth * halfedge.Pair.Vector)),
        // 					new Vertex(halfedge.Pair.Prev.Vertex.Position + (smooth * halfedge.Pair.Vector))
        // 				};
        // 				ribbon.Vertices.AddRange(newVertices);
        // 				Vertex[] new_vertices1 = new Vertex[]
        // 				{
        // 					halfedge.Vertex,
        // 					newVertices[0],
        // 					newVertices[3],
        // 					halfedge.Pair.Prev.Vertex
        // 				};
        // 				Vertex[] new_vertices2 = new Vertex[]
        // 				{
        // 					newVertices[1],
        // 					halfedge.Prev.Vertex,
        // 					halfedge.Pair.Vertex,
        // 					newVertices[2]
        // 				};
        // 				ribbon.Faces.Add(newVertices);
        // 				ribbon.Faces.Add(new_vertices1);
        // 				ribbon.Faces.Add(new_vertices2);
        // 			}
        // 			else
        // 			{
        // 				Vertex[] newVertices = new Vertex[]
        // 				{
        // 					halfedge.Vertex,
        // 					halfedge.Prev.Vertex,
        // 					halfedge.Pair.Vertex,
        // 					halfedge.Pair.Prev.Vertex
        // 				};
        //
        // 				ribbon.Faces.Add(newVertices);
        // 			}
        // 		}
        // 	}
        //
        // 	// remove original faces, leaving just the ribbon
        // 	//var orig_faces = Enumerable.Range(0, Faces.Count).Select(i => ribbon.Faces[i]);
        // 	foreach (Face item in orig_faces)
        // 	{
        // 		ribbon.Faces.Remove(item);
        // 	}
        //
        // 	// search and link pairs
        // 	ribbon.Halfedges.MatchPairs();
        //
        // 	return ribbon;
        // }

        public PolyMesh Cloner(List<Matrix4x4> matrices, Matrix4x4 cumulativeTransform = default,
            bool ApplyAfter = false)
        {
            var result = new PolyMesh();
            var currentCumulativeTransform = cumulativeTransform;
            foreach (var m in matrices)
            {
                var copy = Duplicate();
                foreach (var v in copy.Vertices)
                {
                    if (ApplyAfter)
                    {
                        v.Position = m.MultiplyPoint3x4(v.Position);
                        v.Position = currentCumulativeTransform.MultiplyPoint3x4(v.Position);
                    }
                    else
                    {
                        v.Position = currentCumulativeTransform.MultiplyPoint3x4(v.Position);
                        v.Position = m.MultiplyPoint3x4(v.Position);
                    }
                }

                currentCumulativeTransform *= cumulativeTransform;

                if (m.lossyScale.x < 0 || m.lossyScale.y < 0)
                {
                    // Reflection so flip faces to fix normals
                    copy.Halfedges.Flip();
                }

                result.Append(copy);
            }

            return result;
        }

        public PolyMesh SliceFaceLoop(int edgeIndex)
        {
            var startingEdge = Halfedges[edgeIndex];
            List<Tuple<int, int>> loop = GetFaceLoop(startingEdge);
            return SliceFaceLoop(loop);
        }

        public PolyMesh SliceFaceLoop(List<Tuple<int, int>> loop)
        {
            var poly = Duplicate();
            var faceIndices = loop.Select(x => x.Item1).ToList();
            poly = poly.FaceRemove(true, faceIndices);
            poly.FaceRoles = Enumerable.Repeat(Roles.Existing, poly.Faces.Count).ToList();
            poly = poly.FillHoles();
            poly.FaceRemove(new OpParams(Filter.Role(Roles.Existing)));
            return poly;
        }

        // ConnectFaces uses the Loft method from the Conway ops so probably should move there.

        // public PolyMesh ConnectFaces(OpParams o)
        // {
        //     int i1 = (int)(Faces.Count / o.GetValueA(this, 0));
        //     var f1 = Faces[i1];
        //     var validFaceIndices = Faces
        //         .Where(f => f.Sides == f1.Sides && f.Name != f1.Name)
        //         .Select(f => f.Sides)
        //         .ToList();
        //     int i2 = validFaceIndices[
        //         (int)(validFaceIndices.Count / o.GetValueB(this, 0))
        //     ];
        //     return ConnectFaces(i1, i2, .5f);
        // }

        // public PolyMesh ConnectFaces(int f1, int f2, float insetAmount)
        // {
        //     int faceOneIndex = ActualMod(f1, Faces.Count);
        //     int faceTwoIndex = ActualMod(f2, Faces.Count);
        //
        //     // Only works if both faces are distinct but have same number of edges 
        //     if (faceOneIndex == faceTwoIndex || Faces[faceOneIndex].Sides != Faces[faceTwoIndex].Sides)
        //     {
        //         Debug.LogError("Failed to connect faces");
        //         return null;
        //     }
        //
        //     var newFaceTagSet = FaceTags[faceOneIndex];
        //     newFaceTagSet.UnionWith(FaceTags[faceTwoIndex]);
        //
        //     Filter pickFace = new Filter(x => x.index == faceOneIndex || x.index == faceTwoIndex);
        //     var o = new OpParams(insetAmount, pickFace);
        //     var poly = Duplicate().Loft(o);
        //     o = new OpParams(Filter.Existing);
        //     poly = poly.FaceRemove(o);
        //     var boundaries = poly.FindBoundaries();
        //     int numEdges = boundaries[0].Count;
        //
        //     // Reset Roles
        //     poly.FaceRoles = new List<Roles>(Enumerable.Repeat(Roles.Ignored, poly.Faces.Count));
        //     poly.VertexRoles = new List<Roles>(Enumerable.Repeat(Roles.Ignored, poly.Vertices.Count));
        //
        //     bool flipDirection = true;
        //     int bestF1Edge = 0;
        //     int bestF2Edge = 0;
        //     float shortestEdgeDistance = Single.MaxValue;
        //
        //     // Find nearest pair of edges
        //     for (int i = 0; i < numEdges; i++)
        //     {
        //         for (int j = 0; j < numEdges; j++)
        //         {
        //             var edge1 = boundaries[0][i];
        //             var edge2 = boundaries[1][j];
        //
        //             float currentEdgeDistance = (edge1.Midpoint - edge2.Midpoint).sqrMagnitude;
        //             if (currentEdgeDistance <= shortestEdgeDistance)
        //             {
        //                 shortestEdgeDistance = currentEdgeDistance;
        //                 bestF1Edge = i;
        //                 bestF2Edge = j;
        //             }
        //         }
        //     }
        //
        //     int edge1Index = bestF1Edge;
        //     int edge2Index = bestF2Edge;
        //     for (int i = 0; i < numEdges; i++)
        //     {
        //         var edge1 = boundaries[0][edge1Index];
        //         var edge2 = boundaries[1][edge2Index];
        //
        //         bool success = poly.Faces.Add(new[]
        //         {
        //             edge1.Vertex,
        //             edge1.Prev.Vertex,
        //             edge2.Vertex,
        //             edge2.Prev.Vertex,
        //         });
        //
        //         if (success)
        //         {
        //             poly.FaceRoles.Add(i % 2 == 0 ? Roles.New : Roles.NewAlt);
        //             poly.VertexRoles.AddRange(Enumerable.Repeat(Roles.New, 4));
        //             poly.FaceTags.Add(new HashSet<string>(newFaceTagSet));
        //         }
        //
        //         edge1Index++;
        //         edge1Index = ActualMod(edge1Index, numEdges);
        //
        //         edge2Index += flipDirection ? -1 : 1;
        //         edge2Index = ActualMod(edge2Index, numEdges);
        //     }
        //
        //     return poly;
        // }

        // Attempt to collapse all edges that are between faces with s1 sides and faces with s2 sides
        public void CollapseEdges(int s1, int s2, bool either = true)
        {
            var edgesToCollapse = new List<Halfedge>();
            foreach (var edge in Halfedges)
            {
                if (edge.Face == null || edge.Pair == null || edge.Pair.Face == null) continue;
                if (
                    (edge.Face.Sides == s1 && edge.Pair.Face.Sides == s2) ||
                    (either && (edge.Face.Sides == s2 && edge.Pair.Face.Sides == s1))
                )
                {
                    edgesToCollapse.Add(edge);
                }
            }
            CollapseEdges(edgesToCollapse);
        }

        private void CollapseEdges(List<Halfedge> edgesToCollapse)
        {
            foreach (var edge in edgesToCollapse)
            {
                CollapseEdge(edge);
            }
        }
        
        public Face CollapseEdge(Halfedge edge)
        {
            if (edge.Pair == null) return null;
            var edgePair = edge.Pair;
            
            var face1 = edge.Face;
            var face1firstEdge = edge;
            var face2firstEdge = face1firstEdge.Pair;
            var face2 = face2firstEdge.Face;

            var hole1Verts = face1.GetHalfedges().Select(e=>e.Vertex).ToList();
            FaceRoles.RemoveAt(Faces.IndexOf(face1));
            Faces.Remove(face1);
            var hole2Verts = face2.GetHalfedges().Select(e=>e.Vertex).ToList();
            FaceRoles.RemoveAt(Faces.IndexOf(face2));
            Faces.Remove(face2);
            
            Face newFace = null;

            var newFaceVerts = _FillHole(edge, hole1Verts, hole2Verts);
            var success = Faces.Add(newFaceVerts);
            if (!success)
            {
                hole2Verts.Reverse();
                newFaceVerts = _FillHole(edge, hole1Verts, hole2Verts);
                success = Faces.Add(newFaceVerts);
                if (!success)
                {
                    //     newFaceVerts.Reverse();
                    Debug.LogError($"Failed to add new face with {newFaceVerts.Count} verts");
                }
            }
            
            if (success)
            {
                newFace = Faces.Last();
                FaceRoles.Add(Roles.ExistingAlt); // Not really the right role but works visually.
                Halfedges.MatchPairs();
            }

            return newFace;
        }
        
        private static List<Vertex> _FillHole(Halfedge edge, List<Vertex> hole1Verts, List<Vertex> hole2Verts)
        {
            var verts = new List<Vertex>();
            int failsafe1 = 0;
            int i = hole1Verts.IndexOf(edge.Vertex);
            var hole1StartingVert = hole1Verts[i];
            var hole1CurrVert = hole1StartingVert;
            bool hole2added = false;
            do
            {
                verts.Add(hole1CurrVert);
                if (hole2Verts.Contains(hole1CurrVert) && !hole2added)
                {
                    int failsafe2 = 0;
                    int j = hole2Verts.IndexOf(hole1CurrVert);
                    var hole2StartingVert = hole2Verts[j];
                    var hole2CurrVert = hole2StartingVert;
                    do
                    {
                        verts.Add(hole2CurrVert);
                        j++;
                        if (j >= hole2Verts.Count) j = 0;
                        hole2CurrVert = hole2Verts[j];
                    } while (hole2CurrVert.Name!=hole2StartingVert.Name && failsafe2++ < 10);
                    hole2added = true;
                }
                i++;
                if (i >= hole1Verts.Count) i = 0;
                hole1CurrVert = hole1Verts[i];
            } while (hole1CurrVert.Name!=hole1StartingVert.Name && failsafe1++ < 33);
            return verts.Distinct().ToList();
        }

        public int ExtendFace(Halfedge edgeToAugment, int newPolySides)
        {
            if (edgeToAugment.Pair == null)
            {
                float sideAngle = (newPolySides - 2f) * 180f / newPolySides;
                Vector3 midpoint = edgeToAugment.Midpoint;
                Vector3 edgeVector = (midpoint - edgeToAugment.Face.Centroid).normalized;

                float theta = sideAngle / 2f;
                float opposite = edgeToAugment.Vector.magnitude / 2f;
                float adjacent = Mathf.Tan(theta * Mathf.Deg2Rad) * opposite;
                var radialVector = edgeVector * adjacent;
                var newCentroid = midpoint + radialVector;
                var newVerts = new List<Vertex>();
                var vert = edgeToAugment.Vertex.Position;
                newVerts.Add(edgeToAugment.Vertex);
                newVerts.Add(edgeToAugment.Prev.Vertex);
                for (int i = 2; i < newPolySides; i++)
                {
                    var spoke = vert - newCentroid;
                    Vector3 nextSpoke =
                        Quaternion.AngleAxis((360f / newPolySides) * (i + 0f), edgeToAugment.Face.Normal) * spoke;
                    var newVert = new Vertex(newCentroid + nextSpoke);
                    Vertices.Add(newVert);
                    newVerts.Add(newVert);
                    VertexRoles.Add(Roles.New);
                }

                Faces.Add(newVerts);
                FaceRoles.Add(Roles.New);
                FaceTags.Add(FaceTags[0]); // TODO
                // Return the index of the added face
                return Faces.Count - 1;
            }

            Debug.LogWarning("Edge to augment must be on a boundary");
            return -1;
        }

        public int ExtendFace(int faceIndex, int edgeIndex, int sides)
        {
            return ExtendFace(Faces[faceIndex], edgeIndex, sides);
        }

        public List<int> ExtendFace(IEnumerable<int> faceIndices, IEnumerable<int> edgeIndices, int sides)
        {
            var results = new List<int>();
            foreach (var faceIndex in faceIndices)
            {
                var face = Faces[faceIndex];
                foreach (var edgeIndex in edgeIndices)
                {
                    Halfedge edgeToAugment = face.GetHalfEdge(edgeIndex);
                    results.Add(ExtendFace(edgeToAugment, sides));
                }
            }

            return results;
        }

        public List<int> ExtendFace(IEnumerable<int> faceIndices, int edgeIndex, int sides)
        {
            var results = new List<int>();
            foreach (var faceIndex in faceIndices)
            {
                var face = Faces[faceIndex];
                Halfedge edgeToAugment = face.GetHalfEdge(edgeIndex);
                results.Add(ExtendFace(edgeToAugment, sides));
            }

            return results;
        }

        public List<int> ExtendFace(int faceIndex, IEnumerable<int> edgeIndices, int sides)
        {
            var face = Faces[faceIndex];
            var results = new List<int>();
            foreach (var edgeIndex in edgeIndices)
            {
                Halfedge edgeToAugment = face.GetHalfEdge(edgeIndex);
                results.Add(ExtendFace(edgeToAugment, sides));
            }

            return results;
        }

        public int ExtendFace(Face face, int edgeIndex, int sides)
        {
            Halfedge edgeToAugment = face.GetHalfEdge(edgeIndex);
            return ExtendFace(edgeToAugment, sides);
        }

        public void AddKite(int faceIndexA, int edgeIndexA, int faceIndexB, int edgeIndexB)
        {
            AddKite(Faces[faceIndexA % Faces.Count], edgeIndexA, Faces[faceIndexB % Faces.Count], edgeIndexB);
        }

        // Given two edges which are assumed to form an angle
        // Attempt to add a rhombic or kite face by reflecting them to a quadrilateral
        public void AddKite(Face faceA, int edgeIndexA, Face faceB, int edgeIndexB)
        {
            var edgeA = faceA.GetHalfedges()[edgeIndexA % faceA.Sides];
            var edgeB = faceB.GetHalfedges()[edgeIndexB % faceB.Sides];
            AddKite(edgeA, edgeB);
        }

        public void AddKite(Halfedge edgeA, Halfedge edgeB)
        {

            var pivot = edgeA.Vertex.Position;
            var faceANormal = edgeA.Face.Normal;

            var angle = Vector3.Angle(
                edgeB.Next.Vertex.Position - pivot,
                edgeB.Vertex.Position - pivot
            );

            var newVert = new Vertex(
                Quaternion.AngleAxis(
                    -angle * 2,
                    faceANormal
                ) * (edgeB.Vertex.Position - pivot) + pivot
            );
            Vertices.Add(newVert);
            VertexRoles.Add(Roles.New);
            var verts = new List<Vertex>()
            {
                edgeB.Next.Vertex,
                edgeB.Vertex,
                edgeA.Vertex,
                newVert,
            };
            var result = Faces.Add(verts);
            if (result)
            {
                FaceRoles.Add(Roles.New);
                FaceTags.Add(FaceTags[0]); // TODO
            }
        }

        public void AddRhombus(int faceIndex, int edgeIndex, float angle)
        {
            AddRhombus(Faces[faceIndex], edgeIndex, angle);
        }

        public void AddRhombus(Face face, int edgeIndex, float angle)
        {
            var edge = face.GetHalfedges()[edgeIndex];
            AddRhombus(edge, angle);
        }

        public void AddRhombus(Halfedge edge, float angle)
        {

            var pivot = edge.Vertex.Position;
            var normal = edge.Face.Normal;

            var newVert1 = new Vertex(
                Quaternion.AngleAxis(
                    -angle,
                    normal
                ) * (edge.Next.Vertex.Position - pivot) + pivot
            );
            Vertices.Add(newVert1);
            VertexRoles.Add(Roles.New);

            float angle2 = (360f - (angle * 2)) / 2f;
            var pivot2 = newVert1.Position;
            var newVert2 = new Vertex(
                Quaternion.AngleAxis(
                    -angle2,
                    normal
                ) * (edge.Vertex.Position - pivot2) + pivot2
            );
            Vertices.Add(newVert2);
            VertexRoles.Add(Roles.New);

            var verts = new List<Vertex>()
            {
                edge.Next.Vertex,
                edge.Vertex,
                newVert1,
                newVert2,
            };
            var result = Faces.Add(verts);
            if (result)
            {
                FaceRoles.Add(Roles.New);
                FaceTags.Add(FaceTags[0]); // TODO
            }
        }

        public void Tile(List<int> faceIndices, float weldDistance = 0.01f)
        {

            // TODO. The intention was to allow specifying multiple

            var copy = Duplicate();
            foreach (var faceIndex in faceIndices)
            {
                var offset = Faces[faceIndex].Centroid;
                // RemoveFace(faceIndex);
                Append(copy, offset);
                // FaceRoles.AddRange(copy.FaceRoles);
                // VertexRoles.AddRange(copy.VertexRoles);
                // FaceTags.AddRange(copy.FaceTags);
                FaceRoles = Enumerable.Repeat(Roles.Existing, Faces.Count).ToList();
            }
            // return Weld(weldDistance);
        }

        public void SplitEdge(int faceIndex, int edgeIndex)
        {
            int faceCount = Faces.Count;
            var face = Faces[faceIndex % faceCount];
            SplitEdge(face.GetHalfedges()[edgeIndex % face.Sides]);
        }

        public void SplitEdges(IEnumerable<Halfedge> edgesToSplit)
        {
            foreach (var edge in edgesToSplit)
            {
                SplitEdge(edge);
            }
        }


        public void SplitEdge(Halfedge edgeToSplit)
        {
            var newVert = new Vertex(edgeToSplit.Midpoint);
            Vertices.Add(newVert);

            var face1 = edgeToSplit.Face;
            var face2 = edgeToSplit.Pair?.Face;

            List<Halfedge> face1edges = face1.GetHalfedges();
            List<Halfedge> face2edges = face2?.GetHalfedges();

            Faces.Remove(face1);
            if (face2 != null)
            {
                Faces.Remove(face2);
            }

            var face1verts = new List<Vertex>();
            var face2verts = new List<Vertex>();

            for (var edgeIndex = 0; edgeIndex < face1edges.Count; edgeIndex++)
            {
                var currentEdge = face1edges[edgeIndex];
                if (!Vertices.Contains(currentEdge.Vertex))
                {
                    Vertices.Add(currentEdge.Vertex);
                }

                if (currentEdge != edgeToSplit)
                {
                    face1verts.Add(currentEdge.Vertex);
                }
                else
                {
                    face1verts.Add(newVert);
                    face1verts.Add(currentEdge.Vertex);
                }
            }

            if (face2edges != null)
            {
                for (var edgeIndex = 0; edgeIndex < face2edges.Count; edgeIndex++)
                {
                    var currentEdge = face2edges[edgeIndex];
                    if (!Vertices.Contains(currentEdge.Vertex))
                    {
                        Vertices.Add(currentEdge.Vertex);
                    }

                    if (currentEdge != edgeToSplit.Pair)
                    {
                        face2verts.Add(currentEdge.Vertex);
                    }
                    else
                    {
                        face2verts.Add(newVert);
                        face2verts.Add(currentEdge.Vertex);
                    }
                }
            }

            Faces.Add(face1verts);
            Faces.Add(face2verts);

            Halfedges.MatchPairs();

        }

        public void MergeCoplanarFaces(float threshold)
        {
            var faceGroups = FindCoplanarFaces(threshold);
            
            foreach (var faces in faceGroups)
            {
                if (faces.Count < 2) return;
                
                var boundaryEdges = new HashSet<Halfedge>();
                
                foreach (var face in faces)
                {
                    foreach (var edge in face.GetHalfedges())
                    {
                        // Add edge unless it's other face is in our list of faces to merge
                        // This  would mean it's an inner edge not a boundary
                        if (faces.Contains(edge?.Pair?.Face)) continue;
                        boundaryEdges.Add(edge);
                    }
                }

                List<Vertex> GetLoop(Halfedge startHalfedge)
                {
                    var loop = new List<Vertex>();
                    var currLoopEdge = startHalfedge;
                    int failsafe = 0;
                    do
                    {
                        loop.Add(currLoopEdge.Vertex);
                        currLoopEdge = currLoopEdge.Prev.Vertex.Halfedges.First(
                            e=>boundaryEdges.Contains(e) && e!=currLoopEdge
                        );
                    } while (currLoopEdge != startHalfedge && failsafe++ < 1000);  // Assumes we won't have 1000 sides faces!
                    if (failsafe>=1000) loop.Clear();  // Failure
                    return loop;
                }

                List<Vertex> newFaceVerts;
                try
                {
                    newFaceVerts = GetLoop(boundaryEdges.First());
                }
#pragma warning disable CS0168
                catch (InvalidOperationException e)
#pragma warning restore CS0168
                {
                    Halfedges.MatchPairs();
                    newFaceVerts = GetLoop(boundaryEdges.First());
                }                

                foreach (var face in faces)
                {
                    int index = Faces.IndexOf(face);
                    FaceRoles.RemoveAt(index);
                    FaceTags.RemoveAt(index);
                    Faces.Remove(face);
                }

                var allVerts = new HashSet<Vertex>(Vertices);

                foreach (var v in newFaceVerts)
                {
                    if (!allVerts.Contains(v))
                    {
                        Vertices.Add(v);
                    }
                }
                                
                // TODO proper debug gizmos
                // if (DebugVerts==null) DebugVerts = new List<Vector3>();
                // DebugVerts.Clear();
                // foreach (var v in newFaceVerts)
                // {
                //     DebugVerts.Add(v.Position);
                // }
                
                bool success = Faces.Add(newFaceVerts);
                if (!success)
                {
                    newFaceVerts.Reverse();
                    success = Faces.Add(newFaceVerts);
                }
                if (success)
                {
                    FaceRoles.Add(Roles.New);
                    FaceTags.Add(new HashSet<string>());
                }
                else
                {
                    Debug.LogError("Unable to add merged face.");
                }
                // Halfedges.MatchPairs(Faces.Last());
            }
            Halfedges.MatchPairs();
            CullUnusedVertices();
        }

        private void AddNeighbours(Face f, float threshold, ref HashSet<Face> tt, ref HashSet<Face> cf)
        {
            foreach (var edge in f.GetHalfedges())
            {
                if (edge.DihedralAngle < threshold)
                {
                    var foundFace = edge.Pair.Face;
                    if (cf.Contains(foundFace)) continue;
                    cf.Add(foundFace);
                    tt.Add(foundFace);
                }
            }
            tt.Remove(f);
        }
        
        public List<HashSet<Face>> FindCoplanarFaces(float threshold)
        {
            var groups = new List<HashSet<Face>>();
            var untestedFaces = new HashSet<Face>(Faces);
            
            while (untestedFaces.Count > 0)
            {
                var face = untestedFaces.First();
                untestedFaces.Remove(face);
                var faceGroup = new HashSet<Face>();
                var toCheck = new HashSet<Face>();
                faceGroup.Add(face);
                toCheck.Add(face);
            
                AddNeighbours(face, threshold, ref toCheck, ref faceGroup);
            
                while (toCheck.Count > 0)
                {
                    var newCandidate = toCheck.First();
                    AddNeighbours(newCandidate, threshold, ref toCheck, ref faceGroup);
                }

                if (faceGroup.Count > 1) groups.Add(faceGroup);
                untestedFaces.ExceptWith(faceGroup);
            }
            return groups;
        }

        public void PerlinNoise(Vector3 axis, float strength=1,
            float xscale=1, float yscale=1,
            float xoffset=10, float yoffset=10)
        {
            foreach (var v in Vertices)
            {
                // Not entirely sure this is correct but it works well enough for simple cases.
                var resultant = Vector3.ProjectOnPlane(v.Position, axis);
                Vector2 projected;
                if (resultant.x == 0)
                {
                    projected = new Vector2(resultant.y, resultant.z);
                }
                else if (resultant.y == 0)
                {
                    projected = new Vector2(resultant.x, resultant.z);
                }
                else
                {
                    projected = new Vector2(resultant.x, resultant.y);
                }
                
                float offset = Mathf.PerlinNoise(
                    (projected.x + xoffset) * xscale,
                    (projected.y + yoffset) * yscale
                );
                v.Position += axis * offset * strength;
            }
        }
        
        public void Bulge(Vector3 axis, float strength, float frequency)
        {
            foreach (var v in Vertices)
            {
                var resultant = Vector3.ProjectOnPlane(v.Position, axis);
                Vector2 projected;
                if (resultant.x == 0)
                {
                    projected = new Vector2(resultant.y, resultant.z);
                }
                else if (resultant.y == 0)
                {
                    projected = new Vector2(resultant.x, resultant.z);
                }
                else
                {
                    projected = new Vector2(resultant.x, resultant.y);
                }

                float halfWavelength = (1f / frequency) / 2f;
                var offset = Mathf.Sign(strength) * Mathf.SmoothStep(strength, 0, projected.magnitude * halfWavelength);
                
                v.Position += axis * offset * strength;
            }
        }	
        public void Wave(Vector3 axis, float strength, float frequency)
        {
            foreach (var v in Vertices)
            {
                var resultant = Vector3.ProjectOnPlane(v.Position, axis);
                Vector2 projected;
                if (resultant.x == 0)
                {
                    projected = new Vector2(resultant.y, resultant.z);
                }
                else if (resultant.y == 0)
                {
                    projected = new Vector2(resultant.x, resultant.z);
                }
                else
                {
                    projected = new Vector2(resultant.x, resultant.y);
                }

                var offset = Mathf.Cos(projected.magnitude * frequency * Mathf.PI) * strength;
                
                v.Position += axis * offset * strength;
            }
        }	
        public PolyMesh Spherize(OpParams o)
        {
            var vertexPoints = new List<Vector3>();
            var faceIndices = ListFacesByVertexIndices();

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                float amount = o.GetValueA(this, vertexIndex);
                var vertex = Vertices[vertexIndex];
                if (IncludeVertex(vertexIndex, o.filter))
                {
                    vertexPoints.Add(Vector3.LerpUnclamped(vertex.Position, vertex.Position.normalized, amount));
                    VertexRoles[vertexIndex] = Roles.Existing;
                }
                else
                {
                    vertexPoints.Add(vertex.Position);
                    VertexRoles[vertexIndex] = Roles.Ignored;
                }
            }

            var polyMesh = new PolyMesh(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
            return polyMesh;
        }

        public void Flatten(Axis axis)
        {
            var flattenVector = Vector3.one;
            switch (axis)
            {
                case Axis.X:
                    flattenVector = Vector3.right;
                    break;
                case Axis.Y:
                    flattenVector = Vector3.up;
                    break;
                case Axis.Z:
                    flattenVector = Vector3.forward;
                    break;
            }
            
            foreach (var v in Vertices)
            {
                v.Position = Vector3.Scale(v.Position, flattenVector);
            }
        }

        public PolyMesh Cylinderize(OpParams o)
        {
	        var vertexPoints = new List<Vector3>();
	        var faceIndices = ListFacesByVertexIndices();

	        for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
	        {
		        float amount = o.GetValueA(this, vertexIndex);
		        var vertex = Vertices[vertexIndex];
		        if (IncludeVertex(vertexIndex, o.filter))
		        {
			        var normalized = new Vector2(vertex.Position.x, vertex.Position.z).normalized;
			        var result = new Vector3(normalized.x, vertex.Position.y, normalized.y);
			        vertexPoints.Add(Vector3.LerpUnclamped(vertex.Position, result, amount));
			        VertexRoles[vertexIndex] = Roles.Existing;
		        }
		        else
		        {
			        vertexPoints.Add(vertex.Position);
			        VertexRoles[vertexIndex] = Roles.Ignored;
		        }
	        }

	        var polyMesh = new PolyMesh(vertexPoints, faceIndices, FaceRoles, VertexRoles, FaceTags);
	        return polyMesh;
        }

        private void AddTag(OpParams o)
        {
            for (var i = 0; i < FaceTags.Count; i++)
            {
                HashSet<string> tagSet = FaceTags[i];
                if (IncludeFace(i, o.filter)) tagSet.Add(o.stringParam);
            }
        }

        private void RemoveTag(OpParams o)
        {
            for (var i = 0; i < FaceTags.Count; i++)
            {
                HashSet<string> tagSet = FaceTags[i];
                if (IncludeFace(i, o.filter)) tagSet.Remove(o.stringParam);
            }
        }
        
        private void ClearTags(OpParams o)
        {
            for (var i = 0; i < FaceTags.Count; i++)
            {
                if (IncludeFace(i, o.filter)) FaceTags[i].Clear();
            }
        }
    }
}