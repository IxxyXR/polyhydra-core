using System.IO;
using Polyhydra.Core;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OffReader : MonoBehaviour
{
    public string filename = "test.off";

    [ContextMenu("Go")]
    public void Go()
    {
        using (StreamReader reader = new StreamReader($"Assets/{filename}"))
        {
            var poly = new PolyMesh(reader);
            var mesh = poly.BuildUnityMesh(colorMethod: PolyMesh.ColorMethods.ByTags);
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
        }
    }

    private void OnValidate()
    {
        Go();
    }
}
