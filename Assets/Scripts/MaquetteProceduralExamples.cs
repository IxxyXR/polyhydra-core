using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A self-contained gallery of Polyhydra recipes for Maquette's procedural model families.
/// Meshes are generated only in Play mode, never baked into the scene.
/// </summary>
public class MaquetteProceduralExamples : MonoBehaviour
{
    [Min(3)] public int angularSegments = 24;
    [Min(2)] public int profileSegments = 12;
    [Min(1)] public int cornerSegments = 4;
    public bool smoothSurfaces = true;
    [Tooltip("Select the gallery root to see the editable face topology in Scene view.")]
    public bool showTopology;

    private const string LogPrefix = "[MaquetteProceduralExamples]";
    private readonly List<UnityEngine.Object> ownedAssets = new List<UnityEngine.Object>();
    private readonly List<(Transform transform, PolyMesh poly)> models = new List<(Transform, PolyMesh)>();
    private GameObject generated;
    private int sampleCount;

    private void Start() => Rebuild();
    private void OnDestroy() => Clear();

    [ContextMenu("Rebuild examples")]
    public void Rebuild()
    {
        if (!Application.isPlaying) return;
        Clear();
        sampleCount = 0;
        generated = new GameObject("Generated gallery");
        generated.transform.SetParent(transform, false);
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError($"{LogPrefix} No supported lit shader found.", this);
            return;
        }

        int sides = Mathf.Max(3, angularSegments);
        int profile = Mathf.Max(2, profileSegments);
        int corners = Mathf.Max(1, cornerSegments);
        var palette = new[]
        {
            new Color(.25f, .7f, .92f), new Color(.95f, .61f, .28f),
            new Color(.32f, .81f, .61f), new Color(.75f, .51f, .95f),
            new Color(.95f, .45f, .57f)
        };
        var rowMaterials = new Material[palette.Length];
        for (int index = 0; index < palette.Length; index++)
            rowMaterials[index] = Material(shader, palette[index], $"Family {index + 1}");
        var pedestalMaterial = Material(shader, new Color(.14f, .17f, .22f), "Pedestals");
        var floorMaterial = Material(shader, new Color(.075f, .09f, .12f), "Floor");
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var cameraObject = Child("Gallery camera", generated.transform);
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .045f, .065f);
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 200f;
        Light("Key light", Quaternion.Euler(45f, -35f, 0f), 1.4f);
        Light("Fill light", Quaternion.Euler(65f, 145f, 0f), .55f);
        Cube("Floor", generated.transform, new Vector3(10f, -.12f, 9f),
            new Vector3(25f, .12f, 27f), floorMaterial);
        Text("Title", "MAQUETTE PROCEDURAL MODELS", new Vector3(10f, .02f, 21.6f),
            64, .08f, Color.white, font);
        Text("Subtitle", "Polyhydra generators and modifiers / 30 live recipes", new Vector3(10f, .02f, 20.7f),
            48, .06f, new Color(.7f, .76f, .84f), font);

        void Row(int row, string title)
        {
            Text($"Family {row + 1}", title, new Vector3(10f, .02f, row * 4.5f + 1.6f),
                48, .065f, palette[row], font);
        }
        void Sample(int row, int column, string title, string detail, Func<PolyMesh> build)
        {
            var cell = Child($"{row + 1}.{column + 1} {title}", generated.transform);
            cell.transform.localPosition = new Vector3(column * 4f, 0f, row * 4.5f);
            Cube("Plinth", cell.transform, new Vector3(0f, .04f, 0f),
                new Vector3(3.2f, .08f, 2.5f), pedestalMaterial);
            try
            {
                var poly = build();
                var data = poly.BuildMeshData(useSmoothing: true);
                var mesh = Own(new Mesh { name = title, indexFormat = IndexFormat.UInt32 });
                mesh.SetVertices(data.meshVertices);
                mesh.SetNormals(data.meshNormals);
                mesh.SetTriangles(data.meshTriangles, 0);
                mesh.SetUVs(0, data.meshUVs);
                mesh.RecalculateBounds();
                var model = Child("Model", cell.transform);
                model.AddComponent<MeshFilter>().sharedMesh = mesh;
                model.AddComponent<MeshRenderer>().sharedMaterial = rowMaterials[row];
                model.transform.localPosition = new Vector3(-mesh.bounds.center.x,
                    .08f - mesh.bounds.min.y, -mesh.bounds.center.z);
                models.Add((model.transform, poly));
                sampleCount++;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} {title}: {exception}", this);
                detail = "GENERATION FAILED: see Console";
            }
            Text($"{title} label", $"{title}\n{detail}",
                cell.transform.localPosition + new Vector3(0f, .02f, -1.65f),
                40, .055f, Color.white, font);
        }

        Row(0, "CYLINDER / CONE");
        Sample(0, 0, "Cylinder", "Smooth wall / sharp caps", () =>
            VariousSolids.Cylinder(sides, 1.7f, .7f, .7f, smoothSides: smoothSurfaces));
        Sample(0, 1, "Cone", "Collapsed top ring", () =>
            VariousSolids.Cylinder(sides, 2f, .9f, 0f, smoothSides: smoothSurfaces));
        Sample(0, 2, "Taper", "Independent end radii", () =>
            VariousSolids.Cylinder(sides, 1.8f, .9f, .4f, smoothSides: smoothSurfaces));
        Sample(0, 3, "Partial cylinder", "3 sides / 300 deg / sharp cuts", () =>
            VariousSolids.Cylinder(3, 1.7f, .9f, .9f, 300f, smoothSides: smoothSurfaces));
        Sample(0, 4, "Transformed / quads", "Offset, tilt, twist and scale", () =>
            VariousSolids.Cylinder(5, 1.8f, .7f, .55f, topOffset: new Vector2(.35f, -.2f),
                topRotation: Quaternion.Euler(18f, 45f, 10f), bottomScale: new Vector2(1.2f, .8f),
                topScale: new Vector2(.8f, 1.2f), smoothSides: false));
        Sample(0, 5, "Transformed / triangles", "Same ends / explicit diagonal", () =>
            VariousSolids.Cylinder(5, 1.8f, .7f, .55f, topOffset: new Vector2(.35f, -.2f),
                topRotation: Quaternion.Euler(18f, 45f, 10f), bottomScale: new Vector2(1.2f, .8f),
                topScale: new Vector2(.8f, 1.2f), smoothSides: false, triangulateSides: true));

        Row(1, "ROUNDED RECTANGLE / EXTRUSION");
        Sample(1, 0, "Square corners", "Radius 0 / four-vertex profile", () => Plate(Shapes.Rectangle(2.3f, 1.5f), .22f));
        Sample(1, 1, "Rounded rectangle", "Radius .3 / sampled corners", () => Plate(Shapes.Rectangle(2.3f, 1.5f, .3f, corners), .22f));
        Sample(1, 2, "Chamfered rectangle", "One segment per corner", () => Plate(Shapes.Rectangle(2.3f, 1.5f, .3f, 1), .22f));
        Sample(1, 3, "Maximum rounding", "Radius = half the height", () => Plate(Shapes.Rectangle(2.3f, 1.5f, .75f, corners), .22f));
        Sample(1, 4, "Tall rectangle", "Independent dimensions", () => Plate(Shapes.Rectangle(1.2f, 2.3f, .25f, corners), .22f));
        Sample(1, 5, "Thick rectangle", "Shell thickness .7", () => Plate(Shapes.Rectangle(2.1f, 1.5f, .3f, corners), .7f));

        Row(2, "ROUNDED FRAME / EXTRUSION");
        Sample(2, 0, "Square frame", "Square inner and outer contours", () => Plate(Shapes.Frame(2.3f, 1.7f, .22f), .22f));
        Sample(2, 1, "Rounded frame", "Outer .45 / inner .23 radius", () => Plate(Shapes.Frame(2.3f, 1.7f, .22f, .45f, corners), .22f));
        Sample(2, 2, "Chamfered frame", "One segment per corner", () => Plate(Shapes.Frame(2.3f, 1.7f, .22f, .45f, 1), .22f));
        Sample(2, 3, "Thin border", "Border .1 / outer radius .3", () => Plate(Shapes.Frame(2.3f, 1.7f, .1f, .3f, corners), .22f));
        Sample(2, 4, "Thick border", "Border .4 / outer radius .55", () => Plate(Shapes.Frame(2.3f, 1.7f, .4f, .55f, corners), .22f));
        Sample(2, 5, "Deep frame", "Shell thickness .7", () => Plate(Shapes.Frame(2.1f, 1.7f, .22f, .45f, corners), .7f));

        Row(3, "CONIC SOLIDS");
        Sample(3, 0, "Circle", "Full spherical surface", () =>
            VariousSolids.Conic(Shapes.ConicType.Circle, sides, profile, radius: .9f, smooth: smoothSurfaces));
        Sample(3, 1, "Ellipse", "Major .8 / minor 1.2", () =>
            VariousSolids.Conic(Shapes.ConicType.Ellipse, sides, profile, radius: .9f,
                majorAxis: .8f, minorAxis: 1.2f, smooth: smoothSurfaces));
        Sample(3, 2, "Hyperbola", "Clipped surface / sharp caps", () =>
            VariousSolids.Conic(Shapes.ConicType.Hyperbola, sides, profile, radius: .7f,
                majorAxis: .8f, minorAxis: .9f, bottomClip: .15f, topClip: .85f, smooth: smoothSurfaces));
        Sample(3, 3, "Parabola", "Major .5 / offset .4", () =>
            VariousSolids.Conic(Shapes.ConicType.Parabola, sides, profile, radius: .9f,
                majorAxis: .5f, centerOffset: .4f, smooth: smoothSurfaces));
        Sample(3, 4, "Low-resolution cut", "3 sides / 2 rows / 300 deg", () =>
            VariousSolids.Conic(Shapes.ConicType.Circle, 3, 2, radius: .9f,
                sweepAngle: 300f, smooth: smoothSurfaces));
        Sample(3, 5, "Clipped reverse sweep", "Ellipse / -240 deg / triangles", () =>
            VariousSolids.Conic(Shapes.ConicType.Ellipse, sides, profile, radius: .9f,
                majorAxis: 1.1f, minorAxis: 1f, bottomClip: .2f, topClip: .8f,
                sweepAngle: -240f, smooth: smoothSurfaces, triangulateSides: true));

        Row(4, "CAPSULE");
        Sample(4, 0, "Capsule", "Smooth hemispheres and body", () =>
            VariousSolids.Capsule(sides, profile, 1.2f, .6f, smooth: smoothSurfaces));
        Sample(4, 1, "Short capsule", "Straight section .25", () =>
            VariousSolids.Capsule(sides, profile, .25f, .8f, smooth: smoothSurfaces));
        Sample(4, 2, "Faceted capsule", "6 sides / 3 cap segments", () =>
            VariousSolids.Capsule(6, 3, 1.2f, .6f, smooth: false));
        Sample(4, 3, "Partial capsule", "240 deg / sharp sweep cuts", () =>
            VariousSolids.Capsule(sides, profile, 1.2f, .6f, 240f, smooth: smoothSurfaces));
        Sample(4, 4, "Reverse sweep capsule", "-240 deg / same smooth body", () =>
            VariousSolids.Capsule(sides, profile, 1.2f, .6f, -240f, smooth: smoothSurfaces));
        Sample(4, 5, "Elliptical capsule", "Non-uniform scale", () =>
        {
            var poly = VariousSolids.Capsule(sides, profile, 1.2f, .6f, smooth: smoothSurfaces);
            foreach (var vertex in poly.Vertices)
                vertex.Position = Vector3.Scale(vertex.Position, new Vector3(1.5f, .8f, .75f));
            return poly;
        });

        Text("Instructions", "Select this gallery root and enable Show Topology to inspect faces.\nResolution and smoothing controls rebuild the gallery in Edit or Play mode.",
            new Vector3(10f, .02f, -3.1f), 40, .05f, new Color(.7f, .76f, .84f), font);
        FrameCamera(camera);
        Debug.Log($"{LogPrefix} Generated {sampleCount}/30 examples across five model families.", this);
    }

    private PolyMesh Plate(PolyMesh profile, float thickness)
    {
        var solid = profile.Shell(thickness).Rotate(Vector3.right, 90f);
        if (smoothSurfaces) solid.AutoSmooth(89f);
        return solid;
    }

    private T Own<T>(T asset) where T : UnityEngine.Object
    {
        ownedAssets.Add(asset);
        return asset;
    }

    private Material Material(Shader shader, Color color, string name)
    {
        var material = Own(new Material(shader) { name = name, color = color });
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .3f);
        return material;
    }

    private static GameObject Child(string name, Transform parent)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child;
    }

    private void Light(string name, Quaternion rotation, float intensity)
    {
        var lightObject = Child(name, generated.transform);
        lightObject.transform.localRotation = rotation;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.shadows = LightShadows.Soft;
    }

    private static void Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        Release(cube.GetComponent<Collider>());
    }

    private void Text(string name, string value, Vector3 position, int fontSize, float size, Color color, Font font)
    {
        var label = Child(name, generated.transform);
        label.transform.localPosition = position;
        label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var text = label.AddComponent<TextMesh>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.characterSize = size;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    private void FrameCamera(Camera camera)
    {
        var renderers = generated.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        camera.transform.position = bounds.center + new Vector3(0f, 28f, -24f);
        camera.transform.LookAt(bounds.center);
        float extentX = 0f;
        float extentY = 0f;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
            var point = camera.transform.InverseTransformPoint(corner);
            extentX = Mathf.Max(extentX, Mathf.Abs(point.x));
            extentY = Mathf.Max(extentY, Mathf.Abs(point.y));
        }
        camera.orthographicSize = Mathf.Max(extentY, extentX / Mathf.Max(.1f, camera.aspect)) * 1.08f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showTopology) return;
        Gizmos.color = new Color(.9f, .95f, 1f, .85f);
        foreach (var model in models)
        {
            if (model.transform == null) continue;
            foreach (var face in model.poly.Faces)
            foreach (var edge in face.GetHalfedges())
                Gizmos.DrawLine(model.transform.TransformPoint(edge.Vertex.Position),
                    model.transform.TransformPoint(edge.Prev.Vertex.Position));
        }
    }

    private void Clear()
    {
        if (generated != null)
        {
            generated.SetActive(false);
            Release(generated);
            generated = null;
        }
        foreach (var asset in ownedAssets) Release(asset);
        ownedAssets.Clear();
        models.Clear();
    }

    private static void Release(UnityEngine.Object value)
    {
        if (value == null) return;
        Destroy(value);
    }
}
