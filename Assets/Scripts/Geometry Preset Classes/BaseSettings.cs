using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

public abstract class BaseSettings : ScriptableObject
{
    // Keep the nested type and serialized list so existing preset assets retain their layout.
    [Serializable]
    public class Operator : ModifierSettings { }

    public List<Operator> Operators;
    public bool FastConicalize = true;
    public int CanonicalizeIterations = 0;
    public int PlanarizeIterations = 0;
    [Range(-1f, 1f)] public float FaceInset = 0;
    [Range(0f, 180f)]
    [Tooltip("Maximum dihedral angle to smooth across. Set to 0 to disable automatic smoothing.")]
    public float AutoSmoothAngle = 0f;
    public ColorMethods ColorMethod = ColorMethods.ByRole;

    public event Action OnSettingsChanged;

    private PolyhydraGenerator _Generator;

    void OnEnable()
    {
        OnSettingsChanged = null;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        OnSettingsChanged?.Invoke();
    }

    public virtual PolyMesh ModifyPostOp(PolyMesh poly)
    {
        return poly;
    }

    public abstract PolyMesh BuildBaseShape();

    public virtual Mesh BuildAll(AppearanceSettings appearanceSettings)
    {
        var poly = BuildBaseShape();
        poly = ApplyModifiers(poly);
        var meshData = poly.BuildMeshData(
            colorMethod: GetColorMethod(appearanceSettings),
            colors: CalculateColorList(appearanceSettings),
            useSmoothing: AutoSmoothAngle > 0f
        );
        _Generator.poly = poly;
        return poly.BuildUnityMesh(meshData);
    }

    protected virtual ColorMethods GetColorMethod(AppearanceSettings appearanceSettings)
    {
        return ColorMethod;
    }

    protected virtual Color[] CalculateColorList(AppearanceSettings appearanceSettings)
    {
        return appearanceSettings.CalculateColors();
    }

    /// <summary>Independent modifier records suitable for a runtime recipe snapshot.</summary>
    public ModifierSettings[] GetModifierSettings()
        => Operators?.Select(op => op.Copy()).ToArray() ?? Array.Empty<ModifierSettings>();

    public GeometryPostProcessingSettings GetPostProcessingSettings()
        => new GeometryPostProcessingSettings
        {
            FastConicalize = FastConicalize,
            CanonicalizeIterations = CanonicalizeIterations,
            PlanarizeIterations = PlanarizeIterations,
            FaceInset = FaceInset,
            AutoSmoothAngle = AutoSmoothAngle
        };

    public virtual PolyMesh ApplyModifiers(PolyMesh poly)
    {
        poly = ModifierPipeline.Apply(poly, Operators);
        // Preserve the example's extension point before final geometry processing.
        poly = ModifyPostOp(poly);
        return ModifierPipeline.ApplyPostProcessing(poly, GetPostProcessingSettings());
    }

    public virtual void AttachAction(Action settingsChanged, PolyhydraGenerator generator)
    {
        OnSettingsChanged += settingsChanged;
        _Generator = generator;
    }

    public virtual void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
    }
}
