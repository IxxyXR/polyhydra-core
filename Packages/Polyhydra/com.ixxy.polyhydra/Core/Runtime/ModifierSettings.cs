using System;

namespace Polyhydra.Core
{
    /// <summary>
    /// Geometry-only settings for one ordinary PolyMesh operation. Preset assets and
    /// runtime recipe adapters share this model; asset events and rendering stay outside Core.
    /// </summary>
    [Serializable]
    public class ModifierSettings
    {
        public bool Active = true;
        public PolyMesh.Operation OpType;
        public float Parameter1 = .3f;
        public float Parameter2;
        public string StringParameter;
        public int Iterations = 1;
        public bool Parameter1Randomize;
        public bool Parameter2Randomize;
        public FilterTypes FilterType;
        public float FilterParam;
        // The example settings derive the integer from FilterParam. Richer editors may
        // retain an independent integer selection without losing the floating-point value.
        public bool UseFilterParamInt;
        public int FilterParamInt;
        public bool FilterFlip;

        // Settings contain value types and immutable strings. Copying also preserves a preset adapter's runtime type.
        public ModifierSettings Copy() => (ModifierSettings)MemberwiseClone();
    }

    /// <summary>Geometry processing performed after the ordered modifier list.</summary>
    [Serializable]
    public class GeometryPostProcessingSettings
    {
        public bool FastConicalize = true;
        public int CanonicalizeIterations;
        public int PlanarizeIterations;
        public float FaceInset;
        // Zero disables final automatic smoothing, as in the example app's presets.
        public float AutoSmoothAngle;

        public GeometryPostProcessingSettings Copy() => (GeometryPostProcessingSettings)MemberwiseClone();
    }
}
