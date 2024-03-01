using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;
using Random = System.Random;

public abstract class BaseSettings : ScriptableObject
{
    [Header("Modifications")]
    public List<Operator> Operators;
    public float SpherizeAmount = 0;
    public bool FastConicalize = true;
    public int CanonicalizeIterations = 0;
    public int PlanarizeIterations = 0;
    [Range(.1f, 1f)] public float FaceScale = 1f;
    [Range(-1f, 1f)] public float FaceExtrude = 0;
    [Range(-1f, 1f)] public float FaceInset = 0;

    public event Action OnSettingsChanged;

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
            colorMethod: appearanceSettings.ColorMethod,
            colors: appearanceSettings.CalculateColorList()
        );
        return poly.BuildUnityMesh(meshData);

    }

    public virtual PolyMesh ApplyModifiers(PolyMesh poly)
    {
        var _random = new Random();

        for (var opIndex = 0; opIndex < (Operators?.Count ?? 0); opIndex++)
        {
            var op = Operators[opIndex];
            if (!op.Active) continue;

            var opFilter = Filter.GetFilter(op.FilterType, op.FilterParam, Mathf.FloorToInt(op.FilterParam),
                op.FilterFlip);

            var opRandomValue1 = new OpFunc(_ => Mathf.Lerp(0, op.Parameter1, (float)_random.NextDouble()));
            var opRandomValue2 = new OpFunc(_ => Mathf.Lerp(0, op.Parameter2, (float)_random.NextDouble()));

            OpParams opParams = (OpParameter1Randomize: op.Parameter1Randomize, OpParameter2Randomize: op.Parameter2Randomize) switch
            {
                (false, false) => new OpParams(
                    op.Parameter1,
                    op.Parameter2,
                    filter: opFilter
                ),
                (true, false) => new OpParams(
                    opRandomValue1,
                    op.Parameter2,
                    filter: opFilter
                ),
                (false, true) => new OpParams(
                    op.Parameter1,
                    opRandomValue2,
                    filter: opFilter
                ),
                (true, true) => new OpParams(
                    opRandomValue1,
                    opRandomValue2,
                    filter: opFilter
                ),
            };

            for (int iteration = 0; iteration < op.Iterations; iteration++)
            {
                poly = poly.AppyOperation(op.OpType, opParams);
            }
        }

        poly = ModifyPostOp(poly);

        if (SpherizeAmount > 0) poly.Spherize(new OpParams(SpherizeAmount));
        if (FastConicalize)
        {
            if (CanonicalizeIterations > 0)
            {
                poly = poly.Kanonicalize(CanonicalizeIterations);
            }
        }
        else
        {
            if (CanonicalizeIterations > 0 || PlanarizeIterations > 0)
            {
                poly = poly.Canonicalize(CanonicalizeIterations, PlanarizeIterations);
            }
        }

        if (FaceInset != 0) poly = poly.FaceInset(new OpParams(FaceInset));
        if (FaceExtrude != 0) poly = poly.Extrude(new OpParams(FaceExtrude));
        if (FaceScale < 1f) poly = poly.FaceScale(new OpParams(FaceScale));
        return poly;
    }

    public virtual void AttachAction(Action settingsChanged)
    {
        OnSettingsChanged += settingsChanged;
    }

    public virtual void DetachAction(Action settingsChanged)
    {
        OnSettingsChanged -= settingsChanged;
    }
}