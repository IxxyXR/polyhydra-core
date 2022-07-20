using System;
using System.Globalization;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;
using UnityEditor;
using Random = System.Random;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LuaPolyBuilder : MonoBehaviour
{
    [NonSerialized] public PolyMesh poly;

    private void Start()
    {
        poly = new PolyMesh();
    }

    public void Build(ColorMethods colorMethod = ColorMethods.ByRole)
    {
        var meshData = poly.BuildMeshData(colorMethod: colorMethod);
        var mesh = poly.BuildUnityMesh(meshData);
        if (Application.isPlaying)
        {
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
        }
        else
        {
            gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }

}
