using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Polyhydra.Core.Tests
{
    public class GeometryRecipeTests
    {
        [Test]
        public void ShellAndScaleExecuteInTheSpecifiedOrder()
        {
            var generator = new GeometryStep("rectangle", new Dictionary<string, object>
            {
                { "width", 2f }, { "height", 1f }
            });
            var shell = new GeometryStep("shell", new Dictionary<string, object> { { "thickness", .2f } });
            var scale = new GeometryStep("transform", new Dictionary<string, object>
            {
                { "scale", new Vector3(1f, 3f, 1f) }
            });
            var scaledShell = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator, new[] { shell, scale }));
            var shellAfterScale = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator, new[] { scale, shell }));
            Assert.That(scaledShell.GetBounds().size.y,
                Is.EqualTo(shellAfterScale.GetBounds().size.y * 3f).Within(.00001f));
            Assert.That(scaledShell.Halfedges.All(edge => edge.Pair != null), Is.True);
        }

        [Test]
        public void CylinderRecipePreservesIndependentEndDimensionsAndOffset()
        {
            var poly = GeometryRecipeEvaluator.Build(new GeometryRecipe(new GeometryStep("cylinder",
                new Dictionary<string, object>
                {
                    { "sides", 4 }, { "height", 2f },
                    { "bottomRadius", 2f }, { "bottomAspect", .25f },
                    { "topRadius", .5f }, { "topAspect", 2f },
                    { "topOffset", new Vector2(.3f, -.2f) }
                })));
            Assert.That(poly.Vertices.Any(v => Vector3.Distance(v.Position, new Vector3(2f, -1f, 0f)) < .00001f), Is.True);
            Assert.That(poly.Vertices.Any(v => Vector3.Distance(v.Position, new Vector3(0f, -1f, .5f)) < .00001f), Is.True);
            Assert.That(poly.Vertices.Any(v => Vector3.Distance(v.Position, new Vector3(.8f, 1f, -.2f)) < .00001f), Is.True);
            Assert.That(poly.Vertices.Any(v => Vector3.Distance(v.Position, new Vector3(.3f, 1f, .8f)) < .00001f), Is.True);
        }

        [Test]
        public void UnsupportedDataIsRetainedButNeverSilentlyEvaluated()
        {
            var future = new GeometryStep("futureGenerator", new Dictionary<string, object> { { "value", 7 } });
            Assert.That(future.Parameters["value"], Is.EqualTo(7));
            Assert.Throws<NotSupportedException>(() => GeometryRecipeEvaluator.Build(new GeometryRecipe(future)));
            var typo = new GeometryStep("cylinder", new Dictionary<string, object> { { "raduis", 2f } });
            Assert.Throws<NotSupportedException>(() => GeometryRecipeEvaluator.Build(new GeometryRecipe(typo)));
            var unsupportedOperation = new GeometryRecipe(new GeometryStep("rectangle"), new[] { future });
            Assert.Throws<NotSupportedException>(() => GeometryRecipeEvaluator.Build(unsupportedOperation));
        }

        [Test]
        public void SharedModifiersPreserveOrderIterationsAndDisabledState()
        {
            var generator = new GeometryStep("rectangle");
            var modifiers = new[]
            {
                new ModifierSettings { OpType = PolyMesh.Operation.Shell, Parameter1 = .2f },
                new ModifierSettings { OpType = PolyMesh.Operation.ScaleX, Parameter1 = 10f, Active = false },
                new ModifierSettings { OpType = PolyMesh.Operation.TranslateX, Parameter1 = .25f, Iterations = 2 }
            };
            var source = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator));
            var direct = ModifierPipeline.Apply(source, modifiers);
            var recipe = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator,
                modifiers.Select(GeometryStep.FromModifier)));
            Assert.That(recipe.GetBounds().center.x, Is.EqualTo(.5f).Within(.00001f));
            Assert.That(recipe.GetBounds().size.x, Is.EqualTo(1f).Within(.00001f));
            Assert.That(recipe.GetBounds().size.y, Is.EqualTo(.2f).Within(.00001f));
            Assert.That(recipe.vef, Is.EqualTo(direct.vef));
            Assert.That(recipe.GetBounds(), Is.EqualTo(direct.GetBounds()));
        }

        [Test]
        public void SharedModifiersUseTheSameFilterSemanticsAsPresetOperations()
        {
            var modifier = new ModifierSettings
            {
                OpType = PolyMesh.Operation.Kis, Parameter1 = .2f,
                FilterType = FilterTypes.All, FilterFlip = true
            };
            var generator = new GeometryStep("rectangle");
            var excluded = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator,
                new[] { GeometryStep.FromModifier(modifier) }));
            modifier.FilterFlip = false;
            var included = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator,
                new[] { GeometryStep.FromModifier(modifier) }));
            Assert.That(excluded.Faces.Count, Is.EqualTo(1));
            Assert.That(included.Faces.Count, Is.EqualTo(4));
        }

        [Test]
        public void ModifierCopiesAndRecipeAdaptersDoNotShareMutableSettings()
        {
            var modifier = new ModifierSettings
                { OpType = PolyMesh.Operation.TranslateX, Parameter1 = .25f };
            var copy = modifier.Copy();
            var step = GeometryStep.FromModifier(modifier);
            modifier.Parameter1 = 10f;
            copy.Active = false;
            Assert.That(copy.Parameter1, Is.EqualTo(.25f));
            Assert.That(modifier.Active, Is.True);
            var result = GeometryRecipeEvaluator.Build(new GeometryRecipe(new GeometryStep("rectangle"),
                new[] { step }));
            Assert.That(result.GetBounds().center.x, Is.EqualTo(.25f).Within(.00001f));
        }

        [Test]
        public void FinalSmoothingIsASnapshotAndRunsAfterAllGeometryOperations()
        {
            var generator = new GeometryStep("panel");
            var operations = new[]
            {
                new GeometryStep("autoSmooth", new Dictionary<string, object> { { "angle", 180f } }),
                GeometryStep.FromModifier(new ModifierSettings { OpType = PolyMesh.Operation.Dual })
            };
            var post = new GeometryPostProcessingSettings { AutoSmoothAngle = 180f };
            var recipe = new GeometryRecipe(generator, operations, post);
            post.AutoSmoothAngle = 0;
            recipe.PostProcessing.AutoSmoothAngle = 0;
            var legacy = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator, operations));
            var final = GeometryRecipeEvaluator.Build(recipe);
            // The old named step retains its position; Dual replaces its marked halfedges.
            Assert.That(legacy.Halfedges.Any(edge => edge.IsEdgeSmooth), Is.False);
            Assert.That(final.Halfedges.All(edge => edge.IsEdgeSmooth), Is.True);
        }

        [Test]
        public void SharedOperatorAdaptersRejectUnknownNamesAndParameters()
        {
            var generator = new GeometryStep("rectangle");
            foreach (var name in new[] { "futureOperation", "5" })
            {
                var op = new GeometryStep("operator", new Dictionary<string, object> { { "operation", name } });
                Assert.Throws<NotSupportedException>(() =>
                    GeometryRecipeEvaluator.Build(new GeometryRecipe(generator, new[] { op })));
            }
            var typo = new GeometryStep("operator", new Dictionary<string, object>
                { { "operation", "Dual" }, { "futureParameter", 1f } });
            Assert.Throws<NotSupportedException>(() =>
                GeometryRecipeEvaluator.Build(new GeometryRecipe(generator, new[] { typo })));
        }

        [Test]
        public void OperatorSnapshotsRetainTextAndIndependentIntegerFilters()
        {
            var source = new ModifierSettings
            {
                OpType = PolyMesh.Operation.AddTag, StringParameter = "#33CC77",
                FilterType = FilterTypes.OnlyNth, FilterParam = 3.75f,
                UseFilterParamInt = true, FilterParamInt = 1
            };
            var step = GeometryStep.FromModifier(source);
            var restored = step.ToModifierSettings();
            source.StringParameter = "changed";
            source.FilterParamInt = 4;
            Assert.That(restored.StringParameter, Is.EqualTo("#33CC77"));
            Assert.That(restored.FilterParam, Is.EqualTo(3.75f));
            Assert.That(restored.UseFilterParamInt, Is.True);
            Assert.That(restored.FilterParamInt, Is.EqualTo(1));
            var poly = GeometryRecipeEvaluator.Build(new GeometryRecipe(new GeometryStep("panel")));
            poly.FaceTags = poly.FaceTags.Select(_ => new System.Collections.Generic.HashSet<string>()).ToList();
            poly = ModifierPipeline.Apply(poly, new[] { step.ToModifierSettings() });
            Assert.That(poly.FaceTags[1], Does.Contain("#33CC77"));
            Assert.That(poly.FaceTags[0], Does.Not.Contain("#33CC77"));
            Assert.That(poly.FaceTags.Count(tags => tags.Contains("#33CC77")), Is.EqualTo(1));
        }

        [Test]
        public void HostsCanPreserveTheirScalingFactorAcrossRepeatedModifiers()
        {
            var source = GeometryRecipeEvaluator.Build(new GeometryRecipe(new GeometryStep("panel")));
            source.ScalingFactor = 7f;
            var modifier = new ModifierSettings { OpType = PolyMesh.Operation.Dual, Iterations = 2 };
            float rawScalingFactor = source.Dual().Dual().ScalingFactor;
            var raw = ModifierPipeline.Apply(source.Duplicate(), new[] { modifier });
            var copy = source.Duplicate();
            copy.ScalingFactor = 7f;
            var preserved = ModifierPipeline.Apply(copy, new[] { modifier }, preserveScalingFactor: true);
            Assert.That(raw.ScalingFactor, Is.EqualTo(rawScalingFactor));
            Assert.That(preserved.ScalingFactor, Is.EqualTo(7f));
            Assert.That(preserved.vef, Is.EqualTo(raw.vef));
        }
    }
}
