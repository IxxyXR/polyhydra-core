using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

public class Vox2Poly
{

    public static PolyMesh ConvertMesh(Mesh mesh)
    {
        var vertexPoints = new List<Vector3>();
        var faceIndices = new List<IEnumerable<int>>();
        var vertexRoles = new List<Roles>();
        var faceTags = new List<HashSet<string>>();
        var faceRoles = new List<Roles>();

        var meshVertices = mesh.vertices;
        var meshColors = mesh.colors;

        var debugColors = new HashSet<Color>();

        for (var vertexIndex = 0; vertexIndex < meshVertices.Length; vertexIndex+=4)
        {

            vertexPoints.Add(meshVertices[vertexIndex]);
            vertexPoints.Add(meshVertices[vertexIndex + 1]);
            vertexPoints.Add(meshVertices[vertexIndex + 2]);
            vertexPoints.Add(meshVertices[vertexIndex + 3]);

            faceIndices.Add(new []
            {
                vertexPoints.Count - 4,
                vertexPoints.Count - 3,
                vertexPoints.Count - 2,
                vertexPoints.Count - 1,
            });

            vertexRoles.AddRange(Enumerable.Repeat(Roles.New, 4));
            faceRoles.Add(Roles.New);
            var color = meshColors[vertexIndex];
            var colorString = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            debugColors.Add(color);
            Debug.Log($"{color} = {colorString} = {ParseHexColor(colorString)}");
            faceTags.Add(new HashSet<string>{colorString});
        }
        // int i = 0;
        // foreach (var c in new HashSet<Color32>(mesh.colors32))
        // {
        //     Debug.Log($"{i++}: {new Vector3(c.r, c.g, c.b)}");
        // }

        return new PolyMesh(vertexPoints, faceIndices, faceRoles, vertexRoles, faceTags);
    }

    private static Color ParseHexColor(string htmlColor)
    {
        int hex = Int32.Parse(htmlColor.Replace("#", ""), NumberStyles.HexNumber);
        return new Color(
            (hex & 0xff0000)>> 0x10,
            (hex & 0xff00)>> 8,
            hex & 0xff
        );
    }

}