using System;
using System.Collections.Generic;
using Polyhydra.Core;
using UnityEngine;

/// <summary>
/// A self-contained gallery of Polyhydra recipes for Maquette's procedural model families.
/// Meshes are generated only in Play mode, never baked into the scene.
/// </summary>
public class MaquetteProceduralExamples : MonoBehaviour
{
    [Tooltip("Select the gallery root to see the editable face topology in Scene view.")]
    public bool showTopology;

    private const string LogPrefix = "[MaquetteProceduralExamples]";
    private readonly List<UnityEngine.Object> ownedAssets = new List<UnityEngine.Object>();
    private readonly List<MaquetteProceduralExample> models = new List<MaquetteProceduralExample>();
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
        void Sample<T>(int row, int column, string title, Action<T> configure)
            where T : MaquetteProceduralExample
        {
            var cell = Child($"{row + 1}.{column + 1} {title}", generated.transform);
            cell.transform.localPosition = new Vector3(column * 4f, 0f, row * 4.5f);
            Cube("Plinth", cell.transform, new Vector3(0f, .04f, 0f),
                new Vector3(3.2f, .08f, 2.5f), pedestalMaterial);
            var example = cell.AddComponent<T>();
            configure(example);
            if (example.Initialize(rowMaterials[row])) sampleCount++;
            models.Add(example);
            Text("Label", title, new Vector3(0f, .02f, -1.65f),
                40, .055f, Color.white, font, cell.transform);
        }

        Row(0, "CYLINDER / CONE");
        Sample<MaquetteCylinderExample>(0, 0, "Cylinder", example =>
        {
            example.sides = 24;
            example.height = 1.7f;
            example.radiusBottom = .7f;
            example.radiusTop = .7f;
            example.smoothSides = true;
        });
        Sample<MaquetteCylinderExample>(0, 1, "Cone", example =>
        {
            example.sides = 24;
            example.height = 2f;
            example.radiusBottom = .9f;
            example.radiusTop = 0f;
            example.smoothSides = true;
        });
        Sample<MaquetteCylinderExample>(0, 2, "Taper", example =>
        {
            example.sides = 24;
            example.height = 1.8f;
            example.radiusBottom = .9f;
            example.radiusTop = .4f;
            example.smoothSides = true;
        });
        Sample<MaquetteCylinderExample>(0, 3, "Partial cylinder", example =>
        {
            example.sides = 3;
            example.height = 1.7f;
            example.radiusBottom = .9f;
            example.radiusTop = .9f;
            example.sweepAngle = 300f;
            example.smoothSides = true;
        });
        Sample<MaquetteCylinderExample>(0, 4, "Transformed / quads", example =>
        {
            example.sides = 5;
            example.height = 1.8f;
            example.radiusBottom = .7f * 1.2f;
            example.aspectBottom = .8f / 1.2f;
            example.radiusTop = .55f * .8f;
            example.aspectTop = 1.2f / .8f;
            example.topOffset = new Vector2(.35f, -.2f);
            example.topEulerAngles = new Vector3(18f, 45f, 10f);
            example.smoothSides = false;
        });
        Sample<MaquetteCylinderExample>(0, 5, "Transformed / triangles", example =>
        {
            example.sides = 5;
            example.height = 1.8f;
            example.radiusBottom = .7f * 1.2f;
            example.aspectBottom = .8f / 1.2f;
            example.radiusTop = .55f * .8f;
            example.aspectTop = 1.2f / .8f;
            example.topOffset = new Vector2(.35f, -.2f);
            example.topEulerAngles = new Vector3(18f, 45f, 10f);
            example.smoothSides = false;
            example.triangulateSides = true;
        });

        Row(1, "ROUNDED RECTANGLE / EXTRUSION");
        Sample<MaquetteRectangleExample>(1, 0, "Square corners", example =>
        {
            example.width = 2.3f;
            example.height = 1.5f;
            example.thickness = .22f;
            example.radius = 0f;
        });
        Sample<MaquetteRectangleExample>(1, 1, "Rounded rectangle", example =>
        {
            example.width = 2.3f;
            example.height = 1.5f;
            example.radius = .3f;
            example.cornerSegments = 4;
            example.thickness = .22f;
        });
        Sample<MaquetteRectangleExample>(1, 2, "Chamfered rectangle", example =>
        {
            example.width = 2.3f;
            example.height = 1.5f;
            example.radius = .3f;
            example.cornerSegments = 1;
            example.thickness = .22f;
        });
        Sample<MaquetteRectangleExample>(1, 3, "Maximum rounding", example =>
        {
            example.width = 2.3f;
            example.height = 1.5f;
            example.radius = .75f;
            example.cornerSegments = 4;
            example.thickness = .22f;
        });
        Sample<MaquetteRectangleExample>(1, 4, "Tall rectangle", example =>
        {
            example.width = 1.2f;
            example.height = 2.3f;
            example.radius = .25f;
            example.cornerSegments = 4;
            example.thickness = .22f;
        });
        Sample<MaquetteRectangleExample>(1, 5, "Thick rectangle", example =>
        {
            example.width = 2.1f;
            example.height = 1.5f;
            example.radius = .3f;
            example.cornerSegments = 4;
            example.thickness = .7f;
        });

        Row(2, "ROUNDED FRAME / EXTRUSION");
        Sample<MaquetteFrameExample>(2, 0, "Square frame", example =>
        {
            example.width = 2.3f;
            example.height = 1.7f;
            example.borderWidth = .22f;
            example.thickness = .22f;
            example.radius = 0f;
        });
        Sample<MaquetteFrameExample>(2, 1, "Rounded frame", example =>
        {
            example.width = 2.3f;
            example.height = 1.7f;
            example.borderWidth = .22f;
            example.radius = .45f;
            example.cornerSegments = 4;
            example.thickness = .22f;
        });
        Sample<MaquetteFrameExample>(2, 2, "Chamfered frame", example =>
        {
            example.width = 2.3f;
            example.height = 1.7f;
            example.borderWidth = .22f;
            example.radius = .45f;
            example.cornerSegments = 1;
            example.thickness = .22f;
        });
        Sample<MaquetteFrameExample>(2, 3, "Thin border", example =>
        {
            example.width = 2.3f;
            example.height = 1.7f;
            example.borderWidth = .1f;
            example.radius = .3f;
            example.cornerSegments = 4;
            example.thickness = .22f;
        });
        Sample<MaquetteFrameExample>(2, 4, "Thick border", example =>
        {
            example.width = 2.3f;
            example.height = 1.7f;
            example.borderWidth = .4f;
            example.radius = .55f;
            example.cornerSegments = 4;
            example.thickness = .22f;
        });
        Sample<MaquetteFrameExample>(2, 5, "Deep frame", example =>
        {
            example.width = 2.1f;
            example.height = 1.7f;
            example.borderWidth = .22f;
            example.radius = .45f;
            example.cornerSegments = 4;
            example.thickness = .7f;
        });

        Row(3, "CONIC SOLIDS");
        Sample<MaquetteConicExample>(3, 0, "Circle", example =>
        {
            example.type = Shapes.ConicType.Circle;
            example.sides = 24;
            example.verticalSegments = 12;
            example.radius = .9f;
            example.smooth = true;
        });
        Sample<MaquetteConicExample>(3, 1, "Ellipse", example =>
        {
            example.type = Shapes.ConicType.Ellipse;
            example.sides = 24;
            example.verticalSegments = 12;
            example.radius = .9f;
            example.majorAxis = .8f;
            example.minorAxis = 1.2f;
            example.smooth = true;
        });
        Sample<MaquetteConicExample>(3, 2, "Hyperbola", example =>
        {
            example.type = Shapes.ConicType.Hyperbola;
            example.sides = 24;
            example.verticalSegments = 12;
            example.radius = .7f;
            example.majorAxis = .8f;
            example.minorAxis = .9f;
            example.bottomClip = .15f;
            example.topClip = .85f;
            example.smooth = true;
        });
        Sample<MaquetteConicExample>(3, 3, "Parabola", example =>
        {
            example.type = Shapes.ConicType.Parabola;
            example.sides = 24;
            example.verticalSegments = 12;
            example.radius = .9f;
            example.majorAxis = .5f;
            example.centerOffset = .4f;
            example.smooth = true;
        });
        Sample<MaquetteConicExample>(3, 4, "Low-resolution cut", example =>
        {
            example.type = Shapes.ConicType.Circle;
            example.sides = 3;
            example.verticalSegments = 2;
            example.radius = .9f;
            example.sweepAngle = 300f;
            example.smooth = true;
        });
        Sample<MaquetteConicExample>(3, 5, "Clipped reverse sweep", example =>
        {
            example.type = Shapes.ConicType.Ellipse;
            example.sides = 24;
            example.verticalSegments = 12;
            example.radius = .9f;
            example.majorAxis = 1.1f;
            example.minorAxis = 1f;
            example.bottomClip = .2f;
            example.topClip = .8f;
            example.sweepAngle = -240f;
            example.smooth = true;
            example.triangulateSides = true;
        });

        Row(4, "CAPSULE");
        Sample<MaquetteCapsuleExample>(4, 0, "Capsule", example =>
        {
            example.sides = 24;
            example.capSegments = 12;
            example.cylinderHeight = 1.2f;
            example.radius = .6f;
            example.smooth = true;
        });
        Sample<MaquetteCapsuleExample>(4, 1, "Short capsule", example =>
        {
            example.sides = 24;
            example.capSegments = 12;
            example.cylinderHeight = .25f;
            example.radius = .8f;
            example.smooth = true;
        });
        Sample<MaquetteCapsuleExample>(4, 2, "Faceted capsule", example =>
        {
            example.sides = 6;
            example.capSegments = 3;
            example.cylinderHeight = 1.2f;
            example.radius = .6f;
            example.smooth = false;
        });
        Sample<MaquetteCapsuleExample>(4, 3, "Partial capsule", example =>
        {
            example.sides = 24;
            example.capSegments = 12;
            example.cylinderHeight = 1.2f;
            example.radius = .6f;
            example.sweepAngle = 240f;
            example.smooth = true;
        });
        Sample<MaquetteCapsuleExample>(4, 4, "Reverse sweep capsule", example =>
        {
            example.sides = 24;
            example.capSegments = 12;
            example.cylinderHeight = 1.2f;
            example.radius = .6f;
            example.sweepAngle = -240f;
            example.smooth = true;
        });
        Sample<MaquetteCapsuleExample>(4, 5, "Elliptical capsule", example =>
        {
            example.sides = 24;
            example.capSegments = 12;
            example.cylinderHeight = 1.2f;
            example.radius = .6f;
            example.smooth = true;
            example.scale = new Vector3(1.5f, .8f, .75f);
        });

        Text("Instructions", "Select this gallery root and enable Show Topology to inspect faces.\nSelect an example to edit its parameters during Play mode.",
            new Vector3(10f, .02f, -3.1f), 40, .05f, new Color(.7f, .76f, .84f), font);
        FrameCamera(camera);
        Debug.Log($"{LogPrefix} Generated {sampleCount}/30 examples across five model families.", this);
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

    private void Text(string name, string value, Vector3 position, int fontSize, float size,
        Color color, Font font, Transform parent = null)
    {
        var label = Child(name, parent != null ? parent : generated.transform);
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
            if (model == null || model.Poly == null) continue;
            foreach (var face in model.Poly.Faces)
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
