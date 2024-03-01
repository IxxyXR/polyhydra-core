using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

[Serializable]
public class PolyTransform
{
    public Vector3 Translation = Vector3.zero;
    public Vector3 Rotation = Vector3.zero;
    public float Scale = 1f;
    public Vector3 NonUniformScale = Vector3.one;

    public Matrix4x4 Matrix => Matrix4x4.TRS(
        Translation,
        Quaternion.Euler(Rotation),
        NonUniformScale * Scale
    );
}

[CreateAssetMenu(fileName = "LayeredShapeSettings", menuName = "PolyhydraMulti/LayeredShapeSettings", order = 1)]
public class LayeredShapeSettings : BaseSettings
{


    [Serializable]
    public class ShapeLayer
    {
        public bool Active = true;
        public BaseSettings ShapeSettings;
        public List<PolyTransform> Transforms;
    }

    public List<ShapeLayer> Layers;

    public override PolyMesh BuildBaseShape()
    {
        return null;
    }

    public override Mesh BuildAll(AppearanceSettings appearanceSettings)
    {
        PolyMesh finalPoly = new PolyMesh();
        for (var i = 0; i < Layers.Count; i++)
        {
            ShapeLayer layer = Layers[i];
            if (!layer.Active || layer.ShapeSettings == null) continue;
            var poly = layer.ShapeSettings.BuildBaseShape();
            poly = layer.ShapeSettings.ApplyModifiers(poly);
            var mat = Matrix4x4.identity;
            foreach (var tr in layer.Transforms)
            {
                mat = Matrix4x4.TRS(
                    tr.Translation,
                    Quaternion.Euler(tr.Rotation),
                    tr.NonUniformScale * tr.Scale
                );
            }
            finalPoly.Append(poly, mat);
        }
        finalPoly = ApplyModifiers(finalPoly);

        var meshData = finalPoly.BuildMeshData(
            colorMethod: appearanceSettings.ColorMethod,
            colors: appearanceSettings.CalculateColorList()
        );
        return finalPoly.BuildUnityMesh(meshData);
    }

    public override void AttachAction(Action settingsChanged)
    {
        OnSettingsChanged += settingsChanged;
        foreach (var layer in Layers)
        {
            layer.ShapeSettings.AttachAction(settingsChanged);
        }
    }

    public override void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
        foreach (var layer in Layers)
        {
            layer.ShapeSettings.DetachAction(settingsChanged);
        }
    }
}