using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RevolveTest : TestBase
{
    [Range(3, 128)] public int Segments = 32;
    [Range(1, 360)] public float Angle = 360;

    [ContextMenu("Go")]
    public override void Go()
    {
        poly = new PolyMesh();
        var path = new List<Vector3>
        {
            new(0, 0, 0),
            new(.2f, .1f, 0),
            new(.3f, .2f, 0),
            new(.3f, .3f, 0),
            new(.2f, .4f, 0),
            new(.1f, .5f, 0),
            new(.2f, .6f, 0),
            new(.3f, .7f, 0),
            new(.4f, .8f, 0),
            new(.5f, .9f, 0),
            new(.4f, 1f, 0),
            new(.2f, 1.1f, 0),
            new(0, 1.2f, 0),
        };
        poly = poly.Revolve(path, Segments, Angle);

        Build();
    }
}
