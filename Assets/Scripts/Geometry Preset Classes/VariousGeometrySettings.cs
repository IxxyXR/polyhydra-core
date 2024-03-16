using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "VariousGeometrySettings", menuName = "Polyhydra/VariousGeometrySettings", order = 1)]
public class VariousGeometrySettings : BaseSettings
{
    [Header("Various Geometry Parameters")]
    public VariousSolidTypes type;
    [Range(1, 64)] public int X = 3;
    [Range(.01f, 64f)] public float Y = 3;
    [Range(.01f, 64f)] public float Z = 3;

    public override PolyMesh BuildBaseShape()
    {
        PolyMesh poly = null;
        switch (type)
        {
            case VariousSolidTypes.UvSphere:
                poly = VariousSolids.UvSphere(
                    X,
                    Mathf.Max(Mathf.FloorToInt(Y), 1)
                );
                break;
            case VariousSolidTypes.UvHemisphere:
                poly = VariousSolids.UvHemisphere(
                    X,
                    Mathf.Max(Mathf.FloorToInt(Y), 1)
                );
                break;
            case VariousSolidTypes.Stairs:
                poly = VariousSolids.Stairs(
                    X,
                    Y,
                    Z
                );
                break;
            case VariousSolidTypes.Torus:
                poly = VariousSolids.Torus(
                    X,
                    Mathf.Max(Mathf.FloorToInt(Y), 1),
                    Z
                );
                break;
            case VariousSolidTypes.Box:
                poly = VariousSolids.Box(
                    X,
                    Mathf.Max(Mathf.FloorToInt(Y), 1),
                    Mathf.Max(Mathf.FloorToInt(Z), 1)
                );
                break;
        }
        return poly;
    }
}