using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridTest : TestBase
{
    [Header("Base Shape Parameters")]
    public GridEnums.GridTypes type;
    public GridEnums.GridShapes shape;
    [Range(1, 64)] public int X = 3;
    [Range(1, 64)] public int Y = 3;
    public List<int> Faces;
    
    public override void Go()
    {
        poly = Grids.Build(type, shape, X, Y);
        
        // List<Vector3> vertexPoints = new List<Vector3>
        // {
        //     new(0,0,0), // 0
        //     new(1,0,0), // 1
        //     new(2,0,0), // 2
        //     
        //     new(0,0,1), // 3
        //     new(1,0,1), // 4
        //     new(2,0,1), // 5
        //     
        //     new(0,1,0), // 6
        //     new(1,1,0), // 7
        //     new(2,1,0), // 8
        //     
        //     new(0,1,1), // 9
        //     new(1,1,1), // 10
        //     new(2,1,1), // 11
        // };
        // List<List<int>> faceIndices = new List<List<int>>
        // {
        //     new() {0,1,4,3},
        //     new() {1,2,5,4},
        //     new() {9,10,7,6},
        //     new() {10,11,8,7},
        //     
        //     new() {1,0,6,7},
        //     new() {2,1,7,8},
        //     new() {4,3,9,10},
        //     new() {5,4,10,11},
        //
        //     new() {0,3,9,6},
        //     new() {2,5,11,8},
        // };
        // poly = new PolyMesh(vertexPoints, faceIndices);
        
        if (Faces.Count > 0) poly = poly.FaceRemove(true, Faces);
        Build();
    }
}
