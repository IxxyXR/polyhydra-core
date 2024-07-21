using System;
using Polyhydra.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "WallpaperSymShapeSettings", menuName = "PolyhydraMulti/WallpaperSymShapeSettings", order = 1)]
public class WallpaperSymShapeSettings : BaseSettings
{
    [Header("Wallpaper Symmetry Parameters")]
    public BaseSettings ShapeSettings;
    public SymmetryGroup.R SymmetryGroup;
    public PolyTransform Transform;
    public int RepeatX;
    public int RepeatY;
    public Vector2 GridScale;
    public Vector2 Skew;

    public override PolyMesh BuildBaseShape()
    {
        PolyMesh finalPoly = new PolyMesh();
        var poly = ShapeSettings.BuildBaseShape();
        poly = ShapeSettings.ApplyModifiers(poly);
        poly.Transform(Transform.Matrix);
        var wallpaperSym = new WallpaperSymmetry(
            SymmetryGroup,
            RepeatX, RepeatY,
            1, GridScale.x, GridScale.y,
            Skew.x, Skew.y
        );

        foreach (var mat in wallpaperSym.matrices)
        {
            finalPoly.Append(poly.Duplicate(mat));
        }
        finalPoly = ApplyModifiers(finalPoly);
        return finalPoly;
    }

    public override Mesh BuildAll(AppearanceSettings appearanceSettings)
    {

        var finalPoly = BuildBaseShape();
        var meshData = finalPoly.BuildMeshData(
            colorMethod: GetColorMethod(appearanceSettings),
            colors: CalculateColorList(appearanceSettings)
        );
        return finalPoly.BuildUnityMesh(meshData);
    }

    public override void AttachAction(Action settingsChanged, PolyhydraGenerator generator)
    {
        OnSettingsChanged += settingsChanged;
        ShapeSettings.AttachAction(settingsChanged, generator);
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        ShapeSettings.DetachAction(settingsChanged);
    }
}