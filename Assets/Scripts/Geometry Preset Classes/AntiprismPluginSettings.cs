using Antiprism;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "AntiprismPluginSettings", menuName = "Polyhydra/AntiprismPluginSettings", order = 1)]
public class AntiprismPluginSettings : BaseSettings
{
    public PolyhedronType polyhedronType;


    private Vector3[] vertices;
    private int[][] faceIndices;

    public override PolyMesh BuildBaseShape()
    {
        GeneratePolyhedron(polyhedronType);
        var poly = new PolyMesh(vertices, faceIndices);
        return poly;
    }

    void GeneratePolyhedron(PolyhedronType t)
    {
        using (var geom = new Geometry())
        {
            // Load the base polyhedron
            string resourceName = AntiprismPlugin.GetResourceName(t);
            Status status = geom.LoadResource(resourceName);
            if (status != Status.OK)
            {
                Debug.LogError($"Failed to load polyhedron '{resourceName}': {status}");
                return;
            }

            // geom.Unitize();
            // geom.Orient();
            geom.GetPolyhedronData(out vertices, out faceIndices);
        }
    }

}
