using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Polyhydra.Core
{
    /// <summary>Shared execution for settings assets and runtime recipe modifier adapters.</summary>
    public static class ModifierPipeline
    {
        public static PolyMesh Apply(PolyMesh poly, IEnumerable<ModifierSettings> modifiers)
            => Apply(poly, modifiers, new Random());

        internal static PolyMesh Apply(PolyMesh poly, IEnumerable<ModifierSettings> modifiers, Random random)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));
            if (modifiers == null) return poly;
            foreach (var modifier in modifiers)
            {
                if (!modifier.Active) continue;
                var filter = Filter.GetFilter(modifier.FilterType, modifier.FilterParam,
                    Mathf.FloorToInt(modifier.FilterParam), modifier.FilterFlip);
                var randomValue1 = new OpFunc(_ => Mathf.Lerp(0, modifier.Parameter1, (float)random.NextDouble()));
                var randomValue2 = new OpFunc(_ => Mathf.Lerp(0, modifier.Parameter2, (float)random.NextDouble()));
                OpParams parameters = (modifier.Parameter1Randomize, modifier.Parameter2Randomize) switch
                {
                    (false, false) => new OpParams(modifier.Parameter1, modifier.Parameter2, filter: filter),
                    (true, false) => new OpParams(randomValue1, modifier.Parameter2, filter: filter),
                    (false, true) => new OpParams(modifier.Parameter1, randomValue2, filter: filter),
                    (true, true) => new OpParams(randomValue1, randomValue2, filter: filter)
                };
                for (int iteration = 0; iteration < modifier.Iterations; ++iteration)
                    poly = poly.AppyOperation(modifier.OpType, parameters);
            }
            return poly;
        }

        public static PolyMesh ApplyPostProcessing(PolyMesh poly, GeometryPostProcessingSettings settings)
        {
            if (poly == null) throw new ArgumentNullException(nameof(poly));
            if (settings == null) return poly;
            if (settings.FastConicalize)
            {
                if (settings.PlanarizeIterations > 0 && settings.CanonicalizeIterations <= 0)
                    PolyMesh.PlanarizeLeastSquares(poly, .001, settings.PlanarizeIterations);
                else if (settings.CanonicalizeIterations > 0)
                    poly = poly.Kanonicalize(settings.CanonicalizeIterations);
            }
            else if (settings.CanonicalizeIterations > 0 || settings.PlanarizeIterations > 0)
                poly = poly.Canonicalize(settings.CanonicalizeIterations, settings.PlanarizeIterations);

            if (settings.FaceInset != 0) poly = poly.FaceInset(new OpParams(settings.FaceInset));
            if (settings.AutoSmoothAngle > 0) poly.AutoSmooth(settings.AutoSmoothAngle);
            return poly;
        }
    }
}
