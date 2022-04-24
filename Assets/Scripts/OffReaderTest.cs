using System.IO;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OffReaderTest : TestBase
{
    [Header("Base Shape Parameters")]
    public string filename = "test.off";

    [ContextMenu("Go")]
    public override void Go()
    {
        using (StreamReader reader = new StreamReader($"Assets/{filename}"))
        {
            poly = new PolyMesh(reader);
            Build(PolyMesh.ColorMethods.ByTags);
        }
    }
}
