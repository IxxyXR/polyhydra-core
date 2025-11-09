using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unwrapper
{
    public class UVMesh
    {
        public List<Vector3> vertices = new();
        public List<int>[] faces = Array.Empty<List<int>>();
        public List<Vector3> faceNormals = new();
        public List<int> facePartitions = new();
    }
}