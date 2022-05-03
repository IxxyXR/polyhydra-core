using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AsImpL.MathUtil;
using Polyhydra.Core;
using Vertex = AsImpL.MathUtil.Vertex;

namespace AsImpL
{
    /// <summary>
    /// Implements triangulation of a face of a data set.
    /// </summary>
    public static class Triangulator
    {
        /// <summary>
        /// Triangulate a face of the given dataset.
        /// </summary>
        /// <param name="dataSet">Input data set.</param>
        /// <param name="face">Face to be triangulated (with more than 3 vertices)</param>
        public static List<Triangle> Triangulate(Face face)
        {
            Vector3 planeNormal = face.Normal;

            // setup the data structure used for triangulation
            List<Vertex> poly = face.GetVertices().Select((v, i)=>new Vertex(i, v.Position)).ToList();
            if (face.IsClockwise)
            {
                poly.Reverse();
            }
            // use the ear clipping triangulation
            List<Triangle> newTris = Triangulation.TriangulateByEarClipping(poly, planeNormal);
            return newTris;
            
        }

    }
}
