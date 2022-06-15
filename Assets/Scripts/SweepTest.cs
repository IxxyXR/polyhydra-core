using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SweepTest : TestBase
{
    [Header("Base Shape Parameters")]
    [Range(3, 64)] public int PathSteps = 5;
    [Range(3, 64)] public int ShapeSides = 5;
    public bool ClosedPath;
    public bool Star;
    public float StarAmount;
    [Range(0, 1)] public float Scale;
    public float Frequency;
    public float Amplitude;
    public float Length;
    public float ShapeRotation;
    
    [ContextMenu("Go")]
    public override void Go()
    {
        
        var shape = Shapes.Build(ShapeTypes.Polygon, ShapeSides);
        shape = shape.FaceScale(new OpParams(Scale)); 

        if (ClosedPath)
        {
            var path = Star ? Shapes.Build(ShapeTypes.Star, PathSteps, StarAmount) : Shapes.Build(ShapeTypes.Polygon, PathSteps);
            path = path.FaceRotate(new OpParams(ShapeRotation));
            poly = path.Sweep(path.Faces[0].Get2DVertices(), shape.Faces[0].Get2DVertices(), true);
            
        }
        else
        {
            var path = new List<Vector2>();
            for (float i = 0; i < PathSteps; i += 1f / PathSteps)
            {
                path.Add(new Vector2(Mathf.Sin(i * Mathf.PI * Frequency) * Amplitude, i * Length));
            }
            poly = new PolyMesh();
            // {
            //     new(0, 0),
            //     new(0, -4),
            //     new(-4, -4),
            //     new(-4, 0),
            //     vec,
            //     // new(-1, 2),
            //     new(-1, 1),
            // };
            poly = poly.Sweep(path, shape.Faces[0].Get2DVertices(), false);
        }

        Build();
    }
}
