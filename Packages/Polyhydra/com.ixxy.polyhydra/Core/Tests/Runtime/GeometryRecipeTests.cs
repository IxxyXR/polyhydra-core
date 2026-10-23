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
                { "scale", new Vector3(1f, 1f, 3f) }
            });
            var scaledShell = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator, new[] { shell, scale }));
            var shellAfterScale = GeometryRecipeEvaluator.Build(new GeometryRecipe(generator, new[] { scale, shell }));
            Assert.That(scaledShell.GetBounds().size.z,
                Is.EqualTo(shellAfterScale.GetBounds().size.z * 3f).Within(.00001f));
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
    }
}
