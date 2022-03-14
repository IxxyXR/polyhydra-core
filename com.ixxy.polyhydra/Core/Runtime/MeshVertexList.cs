using System.Collections.Generic;
using System.Linq;

namespace Polyhydra.Core {
    /// <summary>
    /// 
    /// </summary>
    public class MeshVertexList : List<Vertex> {
        
        private PolyMesh _mPolyMesh;

        /// <summary>
        /// Creates a vertex list that is aware of its parent mesh
        /// </summary>
        /// <param name="polyMesh"></param>
        public MeshVertexList(PolyMesh polyMesh) : base() {
            _mPolyMesh = polyMesh;
        }

        /// <summary>
        /// Convenience constructor, for use outside of the mesh class
        /// </summary>
        public MeshVertexList() : base() {
            _mPolyMesh = null;
        }

    }
}