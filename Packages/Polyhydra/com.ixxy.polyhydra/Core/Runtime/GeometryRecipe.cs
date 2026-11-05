using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Polyhydra.Core
{
    /// <summary>
    /// A geometry-only generator followed by ordered operations. This runtime model
    /// has no JSON contract, document version, materials, placement or UI dependencies.
    /// Applications own their document format and convert its values to these types.
    /// </summary>
    public sealed class GeometryRecipe
    {
        public GeometryStep Generator { get; }
        public IReadOnlyList<GeometryStep> Operations { get; }
        private readonly GeometryPostProcessingSettings postProcessing;
        // Return a copy so editing a caller's settings cannot mutate an existing recipe.
        public GeometryPostProcessingSettings PostProcessing => postProcessing?.Copy();

        public GeometryRecipe(GeometryStep generator, IEnumerable<GeometryStep> operations = null,
            GeometryPostProcessingSettings postProcessing = null)
        {
            Generator = generator ?? throw new ArgumentNullException(nameof(generator));
            var steps = operations?.ToArray() ?? Array.Empty<GeometryStep>();
            if (steps.Any(step => step == null))
                throw new ArgumentException("Operations cannot contain null steps.", nameof(operations));
            Operations = Array.AsReadOnly(steps);
            this.postProcessing = postProcessing?.Copy();
        }
    }

    /// <summary>
    /// A stable type identifier and named parameter values, independent of CLR type names.
    /// Evaluation uses bool, int, float, string, string[], Vector2, Vector3 and Quaternion values.
    /// Unknown identifiers and parameters can be retained here, but are rejected when
    /// evaluating rather than silently changing the resulting geometry.
    /// </summary>
    public sealed class GeometryStep
    {
        public string Type { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }

        /// <summary>
        /// Snapshot an example-style modifier into a runtime recipe step. This Core adapter
        /// does not add fields to any application's document/JSON contract.
        /// </summary>
        public static GeometryStep FromModifier(ModifierSettings modifier)
        {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            return new GeometryStep("operator", new Dictionary<string, object>
            {
                { "operation", modifier.OpType.ToString() },
                { "amount", modifier.Parameter1 },
                { "amount2", modifier.Parameter2 },
                { "active", modifier.Active },
                { "iterations", modifier.Iterations },
                { "randomizeAmount", modifier.Parameter1Randomize },
                { "randomizeAmount2", modifier.Parameter2Randomize },
                { "filter", modifier.FilterType.ToString() },
                { "filterParameter", modifier.FilterParam },
                { "invertFilter", modifier.FilterFlip }
            });
        }

        public GeometryStep(string type, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("A geometry step requires a type identifier.", nameof(type));
            Type = type;
            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            if (parameters != null)
                foreach (var parameter in parameters) values.Add(parameter.Key, parameter.Value);
            Parameters = new ReadOnlyDictionary<string, object>(values);
        }
    }
}
