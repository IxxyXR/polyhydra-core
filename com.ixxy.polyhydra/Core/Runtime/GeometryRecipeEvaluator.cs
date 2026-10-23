using System;
using System.Collections.Generic;
using UnityEngine;

namespace Polyhydra.Core
{
    /// <summary>
    /// Evaluates named geometry recipes without normalising their size or building
    /// Unity meshes. Dimensions are in caller-defined local units; angles are degrees.
    /// Shape profiles use their generators' XY plane; solids are centred about Y.
    /// </summary>
    public static class GeometryRecipeEvaluator
    {
        public static PolyMesh Build(GeometryRecipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            var poly = Generate(recipe.Generator);
            foreach (var operation in recipe.Operations) poly = Apply(poly, operation);
            return poly;
        }

        private static PolyMesh Generate(GeometryStep step)
        {
            var p = new Parameters(step);
            PolyMesh poly;
            switch (step.Type)
            {
                case "rectangle":
                    poly = Shapes.Rectangle(p.Read("width", 1f), p.Read("height", 1f),
                        p.Read("radius", 0f), p.Read("cornerSegments", 4));
                    break;
                case "frame":
                    poly = Shapes.Frame(p.Read("width", 1f), p.Read("height", 1f),
                        p.Read("borderWidth", .1f), p.Read("radius", 0f), p.Read("cornerSegments", 4));
                    break;
                case "cylinder":
                    poly = VariousSolids.Cylinder(p.Read("sides", 24), p.Read("height", 1f),
                        p.Read("bottomRadius", .5f), p.Read("bottomAspect", 1f),
                        p.Read("topRadius", .5f), p.Read("topAspect", 1f), p.Read("sweepAngle", 360f),
                        p.Read("topOffset", Vector2.zero), p.Read("topRotation", Quaternion.identity),
                        p.Read("smoothSides", false));
                    break;
                case "capsule":
                    poly = VariousSolids.Capsule(p.Read("sides", 24), p.Read("capSegments", 8),
                        p.Read("cylinderHeight", 2f), p.Read("radius", 1f), p.Read("sweepAngle", 360f),
                        ReadZeroSweep(p), p.Read("smooth", false));
                    break;
                case "conic":
                    poly = VariousSolids.Conic(ReadConic(p.Read("curve", "circle")),
                        p.Read("sides", 24), p.Read("verticalSegments", 16), p.Read("radius", 1f),
                        p.Read("majorAxis", 1f), p.Read("minorAxis", 1f), p.Read("centerOffset", .2f),
                        p.Read("bottomClip", 0f), p.Read("topClip", 1f), p.Read("sweepAngle", 360f),
                        p.Read("smooth", false), ReadZeroSweep(p));
                    break;
                default:
                    throw new NotSupportedException($"Unknown geometry generator '{step.Type}'.");
            }
            p.RequireAllRead();
            return poly;
        }

        private static PolyMesh Apply(PolyMesh poly, GeometryStep step)
        {
            var p = new Parameters(step);
            switch (step.Type)
            {
                case "shell":
                    poly = poly.Shell(p.Read("thickness", .1f), p.Read("symmetric", true));
                    break;
                case "transform":
                    var matrix = Matrix4x4.TRS(p.Read("translation", Vector3.zero),
                        p.Read("rotation", Quaternion.identity), p.Read("scale", Vector3.one));
                    poly = poly.Duplicate(matrix);
                    // Baking a reflection into vertices must preserve outward winding.
                    if (matrix.determinant < 0f) poly.Halfedges.Flip();
                    break;
                case "autoSmooth":
                    poly.AutoSmooth(p.Read("angle", 60f));
                    break;
                case "flip":
                    poly.Halfedges.Flip();
                    break;
                default:
                    throw new NotSupportedException($"Unknown geometry operation '{step.Type}'.");
            }
            p.RequireAllRead();
            return poly;
        }

        private static Shapes.ConicType ReadConic(string curve)
        {
            switch (curve)
            {
                case "circle": return Shapes.ConicType.Circle;
                case "ellipse": return Shapes.ConicType.Ellipse;
                case "hyperbola": return Shapes.ConicType.Hyperbola;
                case "parabola": return Shapes.ConicType.Parabola;
                default: throw new NotSupportedException($"Unknown conic curve '{curve}'.");
            }
        }

        private static PolyMesh.ZeroSweepBehavior ReadZeroSweep(Parameters p)
        {
            var behavior = p.Read("zeroSweep", "thinWedge");
            switch (behavior)
            {
                case "thinWedge": return PolyMesh.ZeroSweepBehavior.ThinWedge;
                case "empty": return PolyMesh.ZeroSweepBehavior.Empty;
                case "doubleSidedProfile": return PolyMesh.ZeroSweepBehavior.DoubleSidedProfile;
                default: throw new NotSupportedException($"Unknown zero-sweep behavior '{behavior}'.");
            }
        }

        private sealed class Parameters
        {
            private readonly GeometryStep step;
            private readonly HashSet<string> read = new HashSet<string>(StringComparer.Ordinal);

            public Parameters(GeometryStep step) => this.step = step;

            public T Read<T>(string name, T defaultValue)
            {
                read.Add(name);
                if (!step.Parameters.TryGetValue(name, out var value)) return defaultValue;
                if (value is T typed) return typed;
                throw new ArgumentException($"Geometry step '{step.Type}' parameter '{name}' requires {typeof(T).Name}.");
            }

            public void RequireAllRead()
            {
                foreach (var name in step.Parameters.Keys)
                    if (!read.Contains(name))
                        throw new NotSupportedException($"Unknown parameter '{name}' for geometry step '{step.Type}'.");
            }
        }
    }
}
