using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unwrapper;

public class UnwrapperTest : MonoBehaviour
{
    [ContextMenu("Unwrap")]
    public void Unwrap()
    {
        var gen = gameObject.GetComponent<PolyhydraGenerator>();
        var poly = gen.poly;
        var unwrapper = new UvUnwrapper();
        var uvMesh = new UVMesh();
        uvMesh.vertices = poly.Vertices.Select(v => v.Position).ToList();
        uvMesh.faces = poly.ListFacesByVertexIndices();
        uvMesh.faceNormals = poly.Faces.Select(f => f.Normal).ToList();
        uvMesh.facePartitions = new List<int>(new int[poly.Faces.Count]);
        unwrapper.SetMesh(uvMesh);

        unwrapper.Unwrap();
        Debug.Log(String.Join("\n", unwrapper.FaceUvs.First()));
    }
}
