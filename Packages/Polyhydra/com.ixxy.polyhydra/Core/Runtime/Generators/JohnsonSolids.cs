// Polyhédronisme
//===================================================================================================
//
// A toy for constructing and manipulating polyhedra and other meshes
//
// Copyright 2019f, Anselm Levskaya
// Released under the MIT License
//
// Johnson Solids - geometry extracted from george hart's VRML models.

using System;
using System.Collections.Generic;
using System.Linq;
using Polyhydra.Core;
using UnityEngine;

public class JohnsonSolids
{
    public static PolyMesh Build(int type)
    {
        var poly = Generators[type]();
        return poly;
    }
    
    public static PolyMesh Build(string name)
    {
        var poly = Generators[Names[name.ToLower()]]();
        return poly;
    }

    public static Dictionary<string, int> Names = new Dictionary<string, int>
    {
        {"square pyramid", 1},
        {"pentagonal pyramid", 2},
        {"triangular cupola", 3},
        {"square cupola", 4},
        {"pentagonal cupola", 5},
        {"pentagonal rotunda", 6},
        {"elongated triangular pyramid", 7},
        {"elongated square pyramid", 8},
        {"elongated pentagonal pyramid", 9},
        {"gyroelongated square pyramid", 10},
        {"gyroelongated pentagonal pyramid", 11},
        {"triangular bipyramid", 12},
        {"pentagonal bipyramid", 13},
        {"elongated triangular bipyramid", 14},
        {"elongated square bipyramid", 15},
        {"elongated pentagonal bipyramid", 16},
        {"gyroelongated square bipyramid", 17},
        {"elongated triangular cupola", 18},
        {"elongated square cupola", 19},
        {"elongated pentagonal cupola", 20},
        {"elongated pentagonal rotunda", 21},
        {"gyroelongated triangular cupola", 22},
        {"gyroelongated square cupola", 23},
        {"gyroelongated pentagonal cupola", 24},
        {"gyroelongated pentagonal rotunda", 25},
        {"gyrobifastigium", 26},
        {"triangular orthobicupola", 27},
        {"square orthobicupola", 28},
        {"square gyrobicupola", 29},
        {"pentagonal orthobicupola", 30},
        {"pentagonal gyrobicupola", 31},
        {"pentagonal orthocupolarotunda", 32},
        {"pentagonal gyrocupolarotunda", 33},
        {"pentagonal orthobirotunda", 34},
        {"elongated triangular orthobicupola", 35},
        {"elongated triangular gyrobicupola", 36},
        {"elongated square gyrobicupola", 37},
        {"elongated pentagonal orthobicupola", 38},
        {"elongated pentagonal gyrobicupola", 39},
        {"elongated pentagonal orthocupolarotunda", 40},
        {"elongated pentagonal gyrocupolarotunda", 41},
        {"elongated pentagonal orthobirotunda", 42},
        {"elongated pentagonal gyrobirotunda", 43},
        {"gyroelongated triangular bicupola", 44},
        {"gyroelongated square bicupola", 45},
        {"gyroelongated pentagonal bicupola", 46},
        {"gyroelongated pentagonal cupolarotunda", 47},
        {"gyroelongated pentagonal birotunda", 48},
        {"augmented triangular prism", 49},
        {"biaugmented triangular prism", 50},
        {"triaugmented triangular prism", 51},
        {"augmented pentagonal prism", 52},
        {"biaugmented pentagonal prism", 53},
        {"augmented hexagonal prism", 54},
        {"parabiaugmented hexagonal prism", 55},
        {"metabiaugmented hexagonal prism", 56},
        {"triaugmented hexagonal prism", 57},
        {"augmented dodecahedron", 58},
        {"parabiaugmented dodecahedron", 59},
        {"metabiaugmented dodecahedron", 60},
        {"triaugmented dodecahedron", 61},
        {"metabidiminished icosahedron", 62},
        {"tridiminished icosahedron", 63},
        {"augmented tridiminished icosahedron", 64},
        {"augmented truncated tetrahedron", 65},
        {"augmented truncated cube", 66},
        {"biaugmented truncated cube", 67},
        {"augmented truncated dodecahedron", 68},
        {"parabiaugmented truncated dodecahedron", 69},
        {"metabiaugmented truncated dodecahedron", 70},
        {"triaugmented truncated dodecahedron", 71},
        {"gyrate rhombicosidodecahedron", 72},
        {"parabigyrate rhombicosidodecahedron", 73},
        {"metabigyrate rhombicosidodecahedron", 74},
        {"trigyrate rhombicosidodecahedron", 75},
        {"diminished rhombicosidodecahedron", 76},
        {"paragyrate diminished rhombicosidodecahedron", 77},
        {"metagyrate diminished rhombicosidodecahedron", 78},
        {"bigyrate diminished rhombicosidodecahedron", 79},
        {"parabidiminished rhombicosidodecahedron", 80},
        {"metabidiminished rhombicosidodecahedron", 81},
        {"gyrate bidiminished rhombicosidodecahedron", 82},
        {"tridiminished rhombicosidodecahedron", 83},
        {"snub disphenoid", 84},
        {"snub square antiprism", 85},
        {"sphenocorona", 86},
        {"augmented sphenocorona", 87},
        {"sphenomegacorona", 88},
        {"hebesphenomegacorona", 89},
        {"disphenocingulum", 90},
        {"bilunabirotunda", 91},
        {"triangular hebesphenorotunda", 92},
    };

    public static Dictionary<int, Func<PolyMesh>> Generators = new Dictionary<int, Func<PolyMesh>>
    {
        {
            1, () =>
            {
                var faces = new[]
                {
                    new[] { 1, 4, 2 },
                    new[] { 0, 1, 2 },
                    new[] { 3, 0, 2 },
                    new[] { 4, 3, 2 },
                    new[] { 4, 1, 0 },
                    new[] { 0, 3, 4 }
                };
                var verts = new[]
                {
                    new Vector3(-0.729665f, 0.670121f, 0.319155f),
                    new Vector3(-0.655235f, -0.29213f, -0.754096f),
                    new Vector3(-0.093922f, -0.607123f, 0.537818f),
                    new Vector3(0.702196f, 0.595691f, 0.485187f),
                    new Vector3(0.776626f, -0.36656f, -0.588064f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            2, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 0, 2 },
                    new[] { 5, 3, 2 },
                    new[] { 4, 5, 2 },
                    new[] { 1, 4, 2 },
                    new[] { 0, 1, 2 },
                    new[] { 0, 3, 5, 4, 1 }
                };
                var verts = new[]
                {
                    new Vector3(-0.868849f, -0.100041f, 0.61257f),
                    new Vector3(-0.329458f, 0.976099f, 0.28078f),
                    new Vector3(-0.26629f, -0.013796f, -0.477654f),
                    new Vector3(-0.13392f, -1.034115f, 0.229829f),
                    new Vector3(0.738834f, 0.707117f, -0.307018f),
                    new Vector3(0.859683f, -0.535264f, -0.338508f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            3, () =>
            {
                var faces = new[]
                {
                    new[] { 2, 6, 4 },
                    new[] { 6, 5, 8 },
                    new[] { 4, 7, 3 },
                    new[] { 2, 0, 1 },
                    new[] { 6, 2, 1, 5 },
                    new[] { 4, 6, 8, 7 },
                    new[] { 2, 4, 3, 0 },
                    new[] { 0, 3, 7, 8, 5, 1 }
                };
                var verts = new[]
                {
                    new Vector3(-0.909743f, 0.523083f, 0.242386f),
                    new Vector3(-0.747863f, 0.22787f, -0.740794f),
                    new Vector3(-0.678803f, -0.467344f, 0.028562f),
                    new Vector3(-0.11453f, 0.564337f, 0.910169f),
                    new Vector3(0.11641f, -0.426091f, 0.696344f),
                    new Vector3(0.209231f, -0.02609f, -1.056192f),
                    new Vector3(0.278291f, -0.721304f, -0.286836f),
                    new Vector3(0.842564f, 0.310377f, 0.594771f),
                    new Vector3(1.004444f, 0.015163f, -0.38841f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            4, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 1, 5 },
                    new[] { 7, 9, 11 },
                    new[] { 6, 10, 8 },
                    new[] { 2, 4, 0 },
                    new[] { 2, 3, 7, 6 },
                    new[] { 3, 2, 0, 1 },
                    new[] { 7, 3, 5, 9 },
                    new[] { 6, 7, 11, 10 },
                    new[] { 2, 6, 8, 4 },
                    new[] { 4, 8, 10, 11, 9, 5, 1, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.600135f, 0.398265f, -0.852158f),
                    new Vector3(-0.585543f, -0.441941f, -0.840701f),
                    new Vector3(-0.584691f, 0.40999f, -0.011971f),
                    new Vector3(-0.570099f, -0.430216f, -0.000514f),
                    new Vector3(-0.18266f, 1.005432f, -0.447988f),
                    new Vector3(-0.147431f, -1.023005f, -0.420329f),
                    new Vector3(0.0203f, 0.428447f, 0.571068f),
                    new Vector3(0.034892f, -0.411759f, 0.582525f),
                    new Vector3(0.422331f, 1.023889f, 0.135052f),
                    new Vector3(0.457559f, -1.004548f, 0.162711f),
                    new Vector3(0.860442f, 0.442825f, 0.555424f),
                    new Vector3(0.875034f, -0.397381f, 0.566881f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            5, () =>
            {
                var faces = new[]
                {
                    new[] { 4, 1, 5 },
                    new[] { 8, 9, 12 },
                    new[] { 10, 14, 13 },
                    new[] { 7, 11, 6 },
                    new[] { 3, 2, 0 },
                    new[] { 4, 3, 0, 1 },
                    new[] { 8, 4, 5, 9 },
                    new[] { 10, 8, 12, 14 },
                    new[] { 7, 10, 13, 11 },
                    new[] { 3, 7, 6, 2 },
                    new[] { 3, 4, 8, 10, 7 },
                    new[] { 2, 6, 11, 13, 14, 12, 9, 5, 1, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.973114f, 0.120196f, -0.57615f),
                    new Vector3(-0.844191f, -0.563656f, -0.512814f),
                    new Vector3(-0.711039f, 0.75783f, -0.46202f),
                    new Vector3(-0.594483f, 0.244733f, -0.002202f),
                    new Vector3(-0.46556f, -0.439119f, 0.061133f),
                    new Vector3(-0.373515f, -1.032518f, -0.296206f),
                    new Vector3(-0.15807f, 1.105692f, -0.21402f),
                    new Vector3(-0.041514f, 0.592595f, 0.245798f),
                    new Vector3(0.167087f, -0.513901f, 0.348277f),
                    new Vector3(0.259132f, -1.1073f, -0.009062f),
                    new Vector3(0.429162f, 0.123733f, 0.462406f),
                    new Vector3(0.474577f, 1.03091f, 0.073124f),
                    new Vector3(0.812101f, -0.759438f, 0.238938f),
                    new Vector3(0.945253f, 0.562048f, 0.289732f),
                    new Vector3(1.074175f, -0.121804f, 0.353067f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            6, () =>
            {
                var faces = new[]
                {
                    new[] { 11, 16, 12 },
                    new[] { 16, 17, 19 },
                    new[] { 12, 15, 9 },
                    new[] { 15, 18, 14 },
                    new[] { 9, 5, 4 },
                    new[] { 5, 8, 3 },
                    new[] { 4, 1, 6 },
                    new[] { 1, 0, 2 },
                    new[] { 6, 10, 11 },
                    new[] { 10, 7, 13 },
                    new[] { 11, 12, 9, 4, 6 },
                    new[] { 11, 10, 13, 17, 16 },
                    new[] { 12, 16, 19, 18, 15 },
                    new[] { 9, 15, 14, 8, 5 },
                    new[] { 4, 5, 3, 0, 1 },
                    new[] { 6, 1, 2, 7, 10 },
                    new[] { 2, 0, 3, 8, 14, 18, 19, 17, 13, 7 }
                };
                var verts = new[]
                {
                    new Vector3(-0.905691f, -0.396105f, -0.539844f),
                    new Vector3(-0.883472f, -0.258791f, 0.103519f),
                    new Vector3(-0.719735f, -0.859265f, -0.110695f),
                    new Vector3(-0.703659f, 0.13708f, -0.868724f),
                    new Vector3(-0.667708f, 0.359259f, 0.17226f),
                    new Vector3(-0.556577f, 0.60392f, -0.428619f),
                    new Vector3(-0.481752f, -0.103901f, 0.60141f),
                    new Vector3(-0.21682f, -1.075487f, 0.254804f),
                    new Vector3(-0.190808f, 0.536633f, -0.971712f),
                    new Vector3(-0.154857f, 0.758811f, 0.069272f),
                    new Vector3(-0.069738f, -0.608646f, 0.694909f),
                    new Vector3(0.146026f, 0.009404f, 0.76365f),
                    new Vector3(0.348059f, 0.542589f, 0.434771f),
                    new Vector3(0.410958f, -0.962182f, 0.417045f),
                    new Vector3(0.436971f, 0.649937f, -0.809472f),
                    new Vector3(0.45919f, 0.787251f, -0.166109f),
                    new Vector3(0.760072f, 0.037844f, 0.52827f),
                    new Vector3(0.923809f, -0.562629f, 0.314056f),
                    new Vector3(0.939886f, 0.433715f, -0.443973f),
                    new Vector3(1.125842f, -0.029444f, -0.014823f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            7, () =>
            {
                var faces = new[]
                {
                    new[] { 0, 2, 4 },
                    new[] { 5, 3, 1 },
                    new[] { 5, 1, 6 },
                    new[] { 5, 6, 3 },
                    new[] { 3, 2, 0, 1 },
                    new[] { 1, 0, 4, 6 },
                    new[] { 6, 4, 2, 3 }
                };
                var verts = new[]
                {
                    new Vector3(-0.793941f, -0.708614f, 0.016702f),
                    new Vector3(-0.451882f, 0.284418f, 0.56528f),
                    new Vector3(-0.252303f, -0.348111f, -0.97361f),
                    new Vector3(0.089756f, 0.64492f, -0.425033f),
                    new Vector3(0.340161f, -0.993103f, -0.175472f),
                    new Vector3(0.385988f, 1.120562f, 0.619029f),
                    new Vector3(0.68222f, -0.000072f, 0.373105f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            8, () =>
            {
                var faces = new[]
                {
                    new[] { 8, 7, 5 },
                    new[] { 8, 5, 4 },
                    new[] { 8, 4, 6 },
                    new[] { 8, 6, 7 },
                    new[] { 1, 3, 2, 0 },
                    new[] { 7, 3, 1, 5 },
                    new[] { 5, 1, 0, 4 },
                    new[] { 4, 0, 2 },
                    new[] { 2, 6, 4 },
                    new[] { 6, 2, 3, 7 }
                };
                var verts = new[]
                {
                    new Vector3(-0.849167f, -0.427323f, 0.457421f),
                    new Vector3(-0.849167f, 0.619869f, 0.087182f),
                    new Vector3(-0.478929f, -0.776386f, -0.529881f),
                    new Vector3(-0.478929f, 0.270805f, -0.900119f),
                    new Vector3(0.198024f, -0.30391f, 0.806484f),
                    new Vector3(0.198024f, 0.743282f, 0.436246f),
                    new Vector3(0.568263f, -0.652974f, -0.180817f),
                    new Vector3(0.568263f, 0.394218f, -0.551056f),
                    new Vector3(1.12362f, 0.13242f, 0.37454f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            9, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 1, 5 },
                    new[] { 6, 5, 10 },
                    new[] { 6, 10, 9 },
                    new[] { 6, 9, 4 },
                    new[] { 6, 4, 1 },
                    new[] { 1, 0, 3, 5 },
                    new[] { 5, 3, 8, 10 },
                    new[] { 10, 8, 7, 9 },
                    new[] { 9, 7, 2, 4 },
                    new[] { 4, 2, 0, 1 },
                    new[] { 8, 3, 0, 2, 7 }
                };
                var verts = new[]
                {
                    new Vector3(-0.980309f, -0.33878f, 0.175213f),
                    new Vector3(-0.719686f, 0.629425f, 0.02221f),
                    new Vector3(-0.520232f, -0.599402f, -0.690328f),
                    new Vector3(-0.299303f, -0.403757f, 0.924054f),
                    new Vector3(-0.25961f, 0.368802f, -0.84333f),
                    new Vector3(-0.03868f, 0.564448f, 0.771051f),
                    new Vector3(0.243026f, 0.902834f, -0.142672f),
                    new Vector3(0.445117f, -0.825453f, -0.47642f),
                    new Vector3(0.581659f, -0.704537f, 0.521323f),
                    new Vector3(0.705739f, 0.142752f, -0.629422f),
                    new Vector3(0.842281f, 0.263667f, 0.36832f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            10, () =>
            {
                var faces = new[]
                {
                    new[] { 4, 7, 8 },
                    new[] { 8, 7, 6 },
                    new[] { 8, 6, 3 },
                    new[] { 3, 6, 1 },
                    new[] { 3, 1, 0 },
                    new[] { 0, 1, 2 },
                    new[] { 0, 2, 4 },
                    new[] { 4, 2, 7 },
                    new[] { 4, 8, 5 },
                    new[] { 8, 3, 5 },
                    new[] { 3, 0, 5 },
                    new[] { 0, 4, 5 },
                    new[] { 1, 6, 7, 2 }
                };
                var verts = new[]
                {
                    new Vector3(-0.776892f, 0.173498f, 0.416855f),
                    new Vector3(-0.68155f, 0.270757f, -0.747914f),
                    new Vector3(-0.646922f, -0.78715f, -0.243069f),
                    new Vector3(0.020463f, 0.897066f, -0.047806f),
                    new Vector3(0.069435f, -0.599041f, 0.666153f),
                    new Vector3(0.15263f, 0.505992f, 1.049841f),
                    new Vector3(0.480709f, 0.236129f, -0.900199f),
                    new Vector3(0.515337f, -0.821778f, -0.395353f),
                    new Vector3(0.866791f, 0.124527f, 0.201492f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            11, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 2, 8 },
                    new[] { 8, 2, 4 },
                    new[] { 8, 4, 9 },
                    new[] { 9, 4, 3 },
                    new[] { 9, 3, 7 },
                    new[] { 7, 3, 1 },
                    new[] { 7, 1, 5 },
                    new[] { 5, 1, 0 },
                    new[] { 5, 0, 6 },
                    new[] { 6, 0, 2 },
                    new[] { 6, 8, 10 },
                    new[] { 8, 9, 10 },
                    new[] { 9, 7, 10 },
                    new[] { 7, 5, 10 },
                    new[] { 5, 6, 10 },
                    new[] { 1, 3, 4, 2, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.722759f, -0.425905f, 0.628394f),
                    new Vector3(-0.669286f, 0.622275f, 0.513309f),
                    new Vector3(-0.502035f, -0.868253f, -0.304556f),
                    new Vector3(-0.415513f, 0.827739f, -0.490768f),
                    new Vector3(-0.312146f, -0.093458f, -0.996236f),
                    new Vector3(0.134982f, 0.097675f, 0.952322f),
                    new Vector3(0.238349f, -0.823522f, 0.446854f),
                    new Vector3(0.324871f, 0.872469f, 0.260642f),
                    new Vector3(0.492123f, -0.618058f, -0.557222f),
                    new Vector3(0.545596f, 0.430122f, -0.672308f),
                    new Vector3(0.88582f, -0.021082f, 0.21957f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            12, () =>
            {
                var faces = new[]
                {
                    new[] { 1, 3, 0 },
                    new[] { 3, 4, 0 },
                    new[] { 3, 1, 4 },
                    new[] { 0, 2, 1 },
                    new[] { 0, 4, 2 },
                    new[] { 2, 4, 1 }
                };
                var verts = new[]
                {
                    new Vector3(-0.610389f, 0.243975f, 0.531213f),
                    new Vector3(-0.187812f, -0.48795f, -0.664016f),
                    new Vector3(-0.187812f, 0.9759f, -0.664016f),
                    new Vector3(0.187812f, -0.9759f, 0.664016f),
                    new Vector3(0.798201f, 0.243975f, 0.132803f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            13, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 2, 0 },
                    new[] { 2, 1, 0 },
                    new[] { 2, 5, 1 },
                    new[] { 0, 4, 3 },
                    new[] { 0, 1, 4 },
                    new[] { 4, 1, 5 },
                    new[] { 2, 3, 6 },
                    new[] { 3, 4, 6 },
                    new[] { 5, 2, 6 },
                    new[] { 4, 5, 6 }
                };
                var verts = new[]
                {
                    new Vector3(-1.028778f, 0.392027f, -0.048786f),
                    new Vector3(-0.640503f, -0.646161f, 0.621837f),
                    new Vector3(-0.125162f, -0.395663f, -0.540059f),
                    new Vector3(0.004683f, 0.888447f, -0.651988f),
                    new Vector3(0.125161f, 0.395663f, 0.540059f),
                    new Vector3(0.632925f, -0.791376f, 0.433102f),
                    new Vector3(1.031672f, 0.157063f, -0.354165f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            14, () =>
            {
                var faces = new[]
                {
                    new[] { 4, 7, 6 },
                    new[] { 5, 3, 1 },
                    new[] { 2, 4, 6 },
                    new[] { 3, 0, 1 },
                    new[] { 7, 2, 6 },
                    new[] { 0, 5, 1 },
                    new[] { 7, 4, 3, 5 },
                    new[] { 4, 2, 0, 3 },
                    new[] { 2, 7, 5, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.677756f, 0.338878f, 0.309352f),
                    new Vector3(-0.446131f, 1.338394f, 0.0f),
                    new Vector3(-0.338878f, -0.677755f, 0.309352f),
                    new Vector3(-0.169439f, 0.508317f, -0.618703f),
                    new Vector3(0.169439f, -0.508317f, -0.618703f),
                    new Vector3(0.338878f, 0.677756f, 0.309352f),
                    new Vector3(0.446131f, -1.338394f, 0.0f),
                    new Vector3(0.677755f, -0.338878f, 0.309352f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            15, () =>
            {
                var faces = new[]
                {
                    new[] { 8, 9, 7 },
                    new[] { 6, 5, 2 },
                    new[] { 3, 8, 7 },
                    new[] { 5, 0, 2 },
                    new[] { 4, 3, 7 },
                    new[] { 0, 1, 2 },
                    new[] { 9, 4, 7 },
                    new[] { 1, 6, 2 },
                    new[] { 9, 8, 5, 6 },
                    new[] { 8, 3, 0, 5 },
                    new[] { 3, 4, 1, 0 },
                    new[] { 4, 9, 6, 1 }
                };
                var verts = new[]
                {
                    new Vector3(-0.669867f, 0.334933f, -0.529576f),
                    new Vector3(-0.669867f, 0.334933f, 0.529577f),
                    new Vector3(-0.4043f, 1.212901f, 0.0f),
                    new Vector3(-0.334933f, -0.669867f, -0.529576f),
                    new Vector3(-0.334933f, -0.669867f, 0.529577f),
                    new Vector3(0.334933f, 0.669867f, -0.529576f),
                    new Vector3(0.334933f, 0.669867f, 0.529577f),
                    new Vector3(0.4043f, -1.212901f, 0.0f),
                    new Vector3(0.669867f, -0.334933f, -0.529576f),
                    new Vector3(0.669867f, -0.334933f, 0.529577f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            16, () =>
            {
                var faces = new[]
                {
                    new[] { 11, 10, 8 },
                    new[] { 7, 9, 3 },
                    new[] { 6, 11, 8 },
                    new[] { 9, 5, 3 },
                    new[] { 2, 6, 8 },
                    new[] { 5, 0, 3 },
                    new[] { 4, 2, 8 },
                    new[] { 0, 1, 3 },
                    new[] { 10, 4, 8 },
                    new[] { 1, 7, 3 },
                    new[] { 10, 11, 9, 7 },
                    new[] { 11, 6, 5, 9 },
                    new[] { 6, 2, 0, 5 },
                    new[] { 2, 4, 1, 0 },
                    new[] { 4, 10, 7, 1 }
                };
                var verts = new[]
                {
                    new Vector3(-0.931836f, 0.219976f, -0.264632f),
                    new Vector3(-0.636706f, 0.318353f, 0.692816f),
                    new Vector3(-0.613483f, -0.735083f, -0.264632f),
                    new Vector3(-0.326545f, 0.979634f, 0.0f),
                    new Vector3(-0.318353f, -0.636706f, 0.692816f),
                    new Vector3(-0.159176f, 0.477529f, -0.856368f),
                    new Vector3(0.159176f, -0.477529f, -0.856368f),
                    new Vector3(0.318353f, 0.636706f, 0.692816f),
                    new Vector3(0.326545f, -0.979634f, 0.0f),
                    new Vector3(0.613482f, 0.735082f, -0.264632f),
                    new Vector3(0.636706f, -0.318353f, 0.692816f),
                    new Vector3(0.931835f, -0.219977f, -0.264632f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            17, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 8, 9 },
                    new[] { 9, 8, 7 },
                    new[] { 9, 7, 3 },
                    new[] { 3, 7, 1 },
                    new[] { 3, 1, 0 },
                    new[] { 0, 1, 2 },
                    new[] { 0, 2, 6 },
                    new[] { 6, 2, 8 },
                    new[] { 6, 9, 4 },
                    new[] { 9, 3, 4 },
                    new[] { 3, 0, 4 },
                    new[] { 0, 6, 4 },
                    new[] { 7, 8, 5 },
                    new[] { 1, 7, 5 },
                    new[] { 2, 1, 5 },
                    new[] { 8, 2, 5 }
                };
                var verts = new[]
                {
                    new Vector3(-0.777261f, 0.485581f, 0.103065f),
                    new Vector3(-0.675344f, -0.565479f, -0.273294f),
                    new Vector3(-0.379795f, -0.315718f, 0.778861f),
                    new Vector3(-0.221894f, 0.282623f, -0.849372f),
                    new Vector3(-0.034619f, 1.231562f, -0.282624f),
                    new Vector3(0.034619f, -1.231562f, 0.282624f),
                    new Vector3(0.196076f, 0.635838f, 0.638599f),
                    new Vector3(0.405612f, -0.602744f, -0.568088f),
                    new Vector3(0.701162f, -0.352983f, 0.484067f),
                    new Vector3(0.751443f, 0.43288f, -0.313837f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            18, () =>
            {
                var faces = new[]
                {
                    new[] { 4, 9, 2 },
                    new[] { 9, 12, 11 },
                    new[] { 2, 6, 0 },
                    new[] { 4, 1, 8 },
                    new[] { 0, 3, 5, 1 },
                    new[] { 1, 5, 10, 8 },
                    new[] { 8, 10, 14, 12 },
                    new[] { 12, 14, 13, 11 },
                    new[] { 11, 13, 7, 6 },
                    new[] { 6, 7, 3, 0 },
                    new[] { 9, 4, 8, 12 },
                    new[] { 2, 9, 11, 6 },
                    new[] { 4, 2, 0, 1 },
                    new[] { 14, 10, 5, 3, 7, 13 }
                };
                var verts = new[]
                {
                    new Vector3(-0.836652f, 0.050764f, 0.288421f),
                    new Vector3(-0.686658f, 0.016522f, -0.560338f),
                    new Vector3(-0.587106f, -0.771319f, 0.365687f),
                    new Vector3(-0.571616f, 0.871513f, 0.302147f),
                    new Vector3(-0.437112f, -0.805561f, -0.483073f),
                    new Vector3(-0.421621f, 0.837272f, -0.546612f),
                    new Vector3(-0.212729f, -0.16003f, 0.84551f),
                    new Vector3(0.052308f, 0.660719f, 0.859236f),
                    new Vector3(0.08726f, -0.228514f, -0.852008f),
                    new Vector3(0.186811f, -1.016355f, 0.074016f),
                    new Vector3(0.352296f, 0.592236f, -0.838282f),
                    new Vector3(0.561189f, -0.405066f, 0.55384f),
                    new Vector3(0.711183f, -0.439308f, -0.294919f),
                    new Vector3(0.826226f, 0.415684f, 0.567566f),
                    new Vector3(0.97622f, 0.381442f, -0.281193f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            19, () =>
            {
                var faces = new[]
                {
                    new[] { 10, 15, 16 },
                    new[] { 7, 13, 8 },
                    new[] { 1, 2, 0 },
                    new[] { 5, 3, 12 },
                    new[] { 2, 6, 4, 0 },
                    new[] { 0, 4, 9, 3 },
                    new[] { 3, 9, 14, 12 },
                    new[] { 12, 14, 18, 15 },
                    new[] { 15, 18, 19, 16 },
                    new[] { 16, 19, 17, 13 },
                    new[] { 13, 17, 11, 8 },
                    new[] { 8, 11, 6, 2 },
                    new[] { 5, 10, 7, 1 },
                    new[] { 10, 5, 12, 15 },
                    new[] { 7, 10, 16, 13 },
                    new[] { 1, 7, 8, 2 },
                    new[] { 5, 1, 0, 3 },
                    new[] { 18, 14, 9, 4, 6, 11, 17, 19 }
                };
                var verts = new[]
                {
                    new Vector3(-0.889715f, 0.115789f, -0.35951f),
                    new Vector3(-0.792371f, -0.231368f, 0.270291f),
                    new Vector3(-0.791598f, 0.494102f, 0.251959f),
                    new Vector3(-0.522446f, -0.406626f, -0.70424f),
                    new Vector3(-0.521352f, 0.619343f, -0.730164f),
                    new Vector3(-0.425102f, -0.753782f, -0.074439f),
                    new Vector3(-0.423235f, 0.997655f, -0.118694f),
                    new Vector3(-0.286344f, -0.218767f, 0.790309f),
                    new Vector3(-0.28557f, 0.506702f, 0.771978f),
                    new Vector3(-0.154083f, 0.096928f, -1.074893f),
                    new Vector3(0.080926f, -0.741182f, 0.44558f),
                    new Vector3(0.082793f, 1.010256f, 0.401324f),
                    new Vector3(0.095069f, -0.767118f, -0.580291f),
                    new Vector3(0.331944f, 0.146209f, 0.895926f),
                    new Vector3(0.463432f, -0.263565f, -0.950945f),
                    new Vector3(0.601096f, -0.754518f, -0.060273f),
                    new Vector3(0.699213f, -0.376205f, 0.551197f),
                    new Vector3(0.700307f, 0.649763f, 0.525272f),
                    new Vector3(0.969459f, -0.250964f, -0.430927f),
                    new Vector3(1.067576f, 0.127349f, 0.180543f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            20, () =>
            {
                var faces = new[]
                {
                    new[] { 15, 18, 21 },
                    new[] { 12, 20, 16 },
                    new[] { 6, 10, 2 },
                    new[] { 3, 0, 1 },
                    new[] { 9, 7, 13 },
                    new[] { 2, 8, 4, 0 },
                    new[] { 0, 4, 5, 1 },
                    new[] { 1, 5, 11, 7 },
                    new[] { 7, 11, 17, 13 },
                    new[] { 13, 17, 22, 18 },
                    new[] { 18, 22, 24, 21 },
                    new[] { 21, 24, 23, 20 },
                    new[] { 20, 23, 19, 16 },
                    new[] { 16, 19, 14, 10 },
                    new[] { 10, 14, 8, 2 },
                    new[] { 15, 9, 13, 18 },
                    new[] { 12, 15, 21, 20 },
                    new[] { 6, 12, 16, 10 },
                    new[] { 3, 6, 2, 0 },
                    new[] { 9, 3, 1, 7 },
                    new[] { 9, 15, 12, 6, 3 },
                    new[] { 22, 17, 11, 5, 4, 8, 14, 19, 23, 24 }
                };
                var verts = new[]
                {
                    new Vector3(-0.93465f, 0.300459f, -0.271185f),
                    new Vector3(-0.838689f, -0.260219f, -0.516017f),
                    new Vector3(-0.711319f, 0.717591f, 0.128359f),
                    new Vector3(-0.710334f, -0.156922f, 0.080946f),
                    new Vector3(-0.599799f, 0.556003f, -0.725148f),
                    new Vector3(-0.503838f, -0.004675f, -0.969981f),
                    new Vector3(-0.487004f, 0.26021f, 0.48049f),
                    new Vector3(-0.460089f, -0.750282f, -0.512622f),
                    new Vector3(-0.376468f, 0.973135f, -0.325605f),
                    new Vector3(-0.331735f, -0.646985f, 0.084342f),
                    new Vector3(-0.254001f, 0.831847f, 0.530001f),
                    new Vector3(-0.125239f, -0.494738f, -0.966586f),
                    new Vector3(0.029622f, 0.027949f, 0.730817f),
                    new Vector3(0.056536f, -0.982543f, -0.262295f),
                    new Vector3(0.08085f, 1.087391f, 0.076037f),
                    new Vector3(0.125583f, -0.532729f, 0.485984f),
                    new Vector3(0.262625f, 0.599586f, 0.780328f),
                    new Vector3(0.391387f, -0.726999f, -0.716259f),
                    new Vector3(0.513854f, -0.868287f, 0.139347f),
                    new Vector3(0.597475f, 0.85513f, 0.326364f),
                    new Vector3(0.641224f, 0.109523f, 0.783723f),
                    new Vector3(0.737185f, -0.451155f, 0.538891f),
                    new Vector3(0.848705f, -0.612742f, -0.314616f),
                    new Vector3(0.976075f, 0.365067f, 0.32976f),
                    new Vector3(1.072036f, -0.19561f, 0.084927f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            21, () =>
            {
                var faces = new[]
                {
                    new[] { 8, 19, 15 },
                    new[] { 19, 20, 25 },
                    new[] { 15, 21, 13 },
                    new[] { 21, 26, 23 },
                    new[] { 13, 10, 3 },
                    new[] { 10, 18, 11 },
                    new[] { 3, 0, 1 },
                    new[] { 0, 4, 2 },
                    new[] { 1, 5, 8 },
                    new[] { 5, 6, 14 },
                    new[] { 11, 16, 9, 4 },
                    new[] { 4, 9, 7, 2 },
                    new[] { 2, 7, 12, 6 },
                    new[] { 6, 12, 17, 14 },
                    new[] { 14, 17, 24, 20 },
                    new[] { 20, 24, 28, 25 },
                    new[] { 25, 28, 29, 26 },
                    new[] { 26, 29, 27, 23 },
                    new[] { 23, 27, 22, 18 },
                    new[] { 18, 22, 16, 11 },
                    new[] { 8, 15, 13, 3, 1 },
                    new[] { 8, 5, 14, 20, 19 },
                    new[] { 15, 19, 25, 26, 21 },
                    new[] { 13, 21, 23, 18, 10 },
                    new[] { 3, 10, 11, 4, 0 },
                    new[] { 1, 0, 2, 6, 5 },
                    new[] { 24, 17, 12, 7, 9, 16, 22, 27, 29, 28 }
                };
                var verts = new[]
                {
                    new Vector3(-0.913903f, 0.139054f, -0.10769f),
                    new Vector3(-0.801323f, 0.048332f, 0.456301f),
                    new Vector3(-0.780136f, -0.347362f, -0.398372f),
                    new Vector3(-0.694081f, 0.568652f, 0.218063f),
                    new Vector3(-0.672895f, 0.172957f, -0.63661f),
                    new Vector3(-0.597978f, -0.494154f, 0.514184f),
                    new Vector3(-0.584884f, -0.738707f, -0.014032f),
                    new Vector3(-0.468218f, -0.603725f, -0.817867f),
                    new Vector3(-0.378156f, -0.064556f, 0.839937f),
                    new Vector3(-0.360976f, -0.083405f, -1.056105f),
                    new Vector3(-0.317215f, 0.86806f, -0.109531f),
                    new Vector3(-0.304122f, 0.623508f, -0.637747f),
                    new Vector3(-0.272966f, -0.995069f, -0.433527f),
                    new Vector3(-0.204636f, 0.777338f, 0.45446f),
                    new Vector3(-0.161718f, -0.851595f, 0.369604f),
                    new Vector3(-0.009384f, 0.385994f, 0.8388f),
                    new Vector3(0.007796f, 0.367145f, -1.057242f),
                    new Vector3(0.1502f, -1.107957f, -0.049891f),
                    new Vector3(0.185324f, 0.832194f, -0.40135f),
                    new Vector3(0.193961f, -0.156492f, 0.896684f),
                    new Vector3(0.327727f, -0.642909f, 0.606002f),
                    new Vector3(0.367482f, 0.685403f, 0.511206f),
                    new Vector3(0.497242f, 0.575832f, -0.820845f),
                    new Vector3(0.60849f, 0.719306f, -0.017714f),
                    new Vector3(0.639645f, -0.899271f, 0.186507f),
                    new Vector3(0.6965f, -0.192358f, 0.604864f),
                    new Vector3(0.803742f, 0.327961f, 0.366626f),
                    new Vector3(0.920408f, 0.462943f, -0.437208f),
                    new Vector3(1.008418f, -0.44872f, 0.185369f),
                    new Vector3(1.11566f, 0.071599f, -0.052869f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            22, () =>
            {
                var faces = new[]
                {
                    new[] { 0, 1, 2 },
                    new[] { 2, 1, 5 },
                    new[] { 2, 5, 8 },
                    new[] { 8, 5, 11 },
                    new[] { 8, 11, 13 },
                    new[] { 13, 11, 14 },
                    new[] { 3, 9, 7 },
                    new[] { 9, 8, 13 },
                    new[] { 7, 12, 6 },
                    new[] { 3, 0, 2 },
                    new[] { 13, 14, 12 },
                    new[] { 12, 14, 10 },
                    new[] { 12, 10, 6 },
                    new[] { 6, 10, 4 },
                    new[] { 6, 4, 0 },
                    new[] { 0, 4, 1 },
                    new[] { 9, 3, 2, 8 },
                    new[] { 7, 9, 13, 12 },
                    new[] { 3, 7, 6, 0 },
                    new[] { 11, 5, 1, 4, 10, 14 }
                };
                var verts = new[]
                {
                    new Vector3(-0.846878f, 0.066004f, 0.311423f),
                    new Vector3(-0.766106f, 0.678635f, -0.329908f),
                    new Vector3(-0.708152f, -0.186985f, -0.531132f),
                    new Vector3(-0.64897f, -0.782761f, 0.128183f),
                    new Vector3(-0.452751f, 0.845109f, 0.48694f),
                    new Vector3(-0.21247f, 0.406919f, -0.972407f),
                    new Vector3(-0.165405f, 0.101357f, 0.883692f),
                    new Vector3(0.032503f, -0.747408f, 0.700451f),
                    new Vector3(0.112048f, -0.404621f, -0.801418f),
                    new Vector3(0.17123f, -1.000397f, -0.142104f),
                    new Vector3(0.41424f, 0.739868f, 0.66129f),
                    new Vector3(0.654521f, 0.301678f, -0.798058f),
                    new Vector3(0.654794f, -0.116279f, 0.613406f),
                    new Vector3(0.793521f, -0.369268f, -0.229149f),
                    new Vector3(0.967876f, 0.468152f, 0.018791f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            23, () =>
            {
                var faces = new[]
                {
                    new[] { 1, 0, 2 },
                    new[] { 2, 0, 4 },
                    new[] { 2, 4, 8 },
                    new[] { 8, 4, 10 },
                    new[] { 8, 10, 14 },
                    new[] { 14, 10, 16 },
                    new[] { 14, 16, 18 },
                    new[] { 18, 16, 19 },
                    new[] { 13, 14, 18 },
                    new[] { 12, 17, 11 },
                    new[] { 6, 5, 1 },
                    new[] { 7, 2, 8 },
                    new[] { 18, 19, 17 },
                    new[] { 19, 15, 17 },
                    new[] { 17, 15, 11 },
                    new[] { 11, 15, 9 },
                    new[] { 11, 9, 5 },
                    new[] { 5, 9, 3 },
                    new[] { 5, 3, 1 },
                    new[] { 1, 3, 0 },
                    new[] { 7, 13, 12, 6 },
                    new[] { 13, 7, 8, 14 },
                    new[] { 12, 13, 18, 17 },
                    new[] { 6, 12, 11, 5 },
                    new[] { 7, 6, 1, 2 },
                    new[] { 10, 4, 0, 3, 9, 15, 19, 16 }
                };
                var verts = new[]
                {
                    new Vector3(-0.96917f, 0.321358f, -0.364138f),
                    new Vector3(-0.902194f, 0.146986f, 0.353054f),
                    new Vector3(-0.885918f, -0.386527f, -0.161101f),
                    new Vector3(-0.700663f, 0.819184f, 0.114745f),
                    new Vector3(-0.670588f, -0.166619f, -0.835289f),
                    new Vector3(-0.389781f, 0.533335f, 0.723761f),
                    new Vector3(-0.377102f, -0.207546f, 0.737557f),
                    new Vector3(-0.360826f, -0.741059f, 0.223402f),
                    new Vector3(-0.350486f, -0.754679f, -0.51752f),
                    new Vector3(-0.022354f, 1.035239f, 0.320838f),
                    new Vector3(0.020179f, -0.358897f, -1.022714f),
                    new Vector3(0.351157f, 0.546203f, 0.733864f),
                    new Vector3(0.363836f, -0.194678f, 0.74766f),
                    new Vector3(0.380112f, -0.728191f, 0.233505f),
                    new Vector3(0.390452f, -0.741811f, -0.507416f),
                    new Vector3(0.668412f, 0.842961f, 0.133414f),
                    new Vector3(0.698487f, -0.142842f, -0.816621f),
                    new Vector3(0.886588f, 0.178052f, 0.377446f),
                    new Vector3(0.902865f, -0.355461f, -0.136709f),
                    new Vector3(0.966994f, 0.354984f, -0.337737f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            24, () =>
            {
                var faces = new[]
                {
                    new[] { 1, 3, 5 },
                    new[] { 5, 3, 8 },
                    new[] { 5, 8, 12 },
                    new[] { 12, 8, 14 },
                    new[] { 12, 14, 18 },
                    new[] { 18, 14, 20 },
                    new[] { 18, 20, 22 },
                    new[] { 22, 20, 24 },
                    new[] { 22, 24, 23 },
                    new[] { 23, 24, 21 },
                    new[] { 15, 18, 22 },
                    new[] { 17, 23, 19 },
                    new[] { 11, 13, 7 },
                    new[] { 6, 2, 1 },
                    new[] { 9, 5, 12 },
                    new[] { 23, 21, 19 },
                    new[] { 19, 21, 16 },
                    new[] { 19, 16, 13 },
                    new[] { 13, 16, 10 },
                    new[] { 13, 10, 7 },
                    new[] { 7, 10, 4 },
                    new[] { 7, 4, 2 },
                    new[] { 2, 4, 0 },
                    new[] { 2, 0, 1 },
                    new[] { 1, 0, 3 },
                    new[] { 15, 9, 12, 18 },
                    new[] { 17, 15, 22, 23 },
                    new[] { 11, 17, 19, 13 },
                    new[] { 6, 11, 7, 2 },
                    new[] { 9, 6, 1, 5 },
                    new[] { 9, 15, 17, 11, 6 },
                    new[] { 20, 14, 8, 3, 0, 4, 10, 16, 21, 24 }
                };
                var verts = new[]
                {
                    new Vector3(-1.007937f, 0.263193f, -0.317378f),
                    new Vector3(-0.995648f, -0.249677f, 0.04509f),
                    new Vector3(-0.928425f, 0.319026f, 0.303212f),
                    new Vector3(-0.878881f, -0.297121f, -0.570283f),
                    new Vector3(-0.751014f, 0.784617f, -0.079308f),
                    new Vector3(-0.682946f, -0.746755f, -0.177844f),
                    new Vector3(-0.534412f, -0.144902f, 0.458433f),
                    new Vector3(-0.506952f, 0.74213f, 0.497926f),
                    new Vector3(-0.413141f, -0.682306f, -0.741423f),
                    new Vector3(-0.221709f, -0.64198f, 0.235499f),
                    new Vector3(-0.206248f, 1.067984f, 0.052991f),
                    new Vector3(-0.112939f, 0.278202f, 0.653148f),
                    new Vector3(-0.109759f, -0.982341f, -0.280438f),
                    new Vector3(0.107781f, 0.858022f, 0.55486f),
                    new Vector3(0.211385f, -0.745233f, -0.765428f),
                    new Vector3(0.393024f, -0.526088f, 0.292433f),
                    new Vector3(0.418278f, 1.005057f, 0.028986f),
                    new Vector3(0.460247f, 0.042616f, 0.550554f),
                    new Vector3(0.504974f, -0.866449f, -0.223504f),
                    new Vector3(0.680968f, 0.622436f, 0.452266f),
                    new Vector3(0.756151f, -0.461866f, -0.633128f),
                    new Vector3(0.884017f, 0.619872f, -0.142153f),
                    new Vector3(0.926446f, -0.443346f, -0.028789f),
                    new Vector3(0.99367f, 0.125358f, 0.229332f),
                    new Vector3(1.013073f, 0.059558f, -0.395059f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            25, () =>
            {
                var faces = new[]
                {
                    new[] { 2, 9, 11 },
                    new[] { 11, 9, 16 },
                    new[] { 11, 16, 19 },
                    new[] { 19, 16, 23 },
                    new[] { 19, 23, 25 },
                    new[] { 25, 23, 28 },
                    new[] { 25, 28, 27 },
                    new[] { 27, 28, 29 },
                    new[] { 27, 29, 26 },
                    new[] { 26, 29, 24 },
                    new[] { 15, 22, 18 },
                    new[] { 22, 25, 27 },
                    new[] { 18, 21, 13 },
                    new[] { 21, 26, 20 },
                    new[] { 13, 7, 6 },
                    new[] { 7, 12, 3 },
                    new[] { 6, 1, 8 },
                    new[] { 1, 0, 2 },
                    new[] { 8, 14, 15 },
                    new[] { 14, 11, 19 },
                    new[] { 26, 24, 20 },
                    new[] { 20, 24, 17 },
                    new[] { 20, 17, 12 },
                    new[] { 12, 17, 10 },
                    new[] { 12, 10, 3 },
                    new[] { 3, 10, 5 },
                    new[] { 3, 5, 0 },
                    new[] { 0, 5, 4 },
                    new[] { 0, 4, 2 },
                    new[] { 2, 4, 9 },
                    new[] { 15, 18, 13, 6, 8 },
                    new[] { 15, 14, 19, 25, 22 },
                    new[] { 18, 22, 27, 26, 21 },
                    new[] { 13, 21, 20, 12, 7 },
                    new[] { 6, 7, 3, 0, 1 },
                    new[] { 8, 1, 2, 11, 14 },
                    new[] { 28, 23, 16, 9, 4, 5, 10, 17, 24, 29 }
                };
                var verts = new[]
                {
                    new Vector3(-0.897802f, -0.193467f, -0.273331f),
                    new Vector3(-0.877838f, -0.070089f, 0.304735f),
                    new Vector3(-0.73072f, -0.609618f, 0.112262f),
                    new Vector3(-0.716275f, 0.285603f, -0.568831f),
                    new Vector3(-0.703732f, -0.716856f, -0.46873f),
                    new Vector3(-0.696138f, -0.246211f, -0.826802f),
                    new Vector3(-0.683973f, 0.485232f, 0.366499f),
                    new Vector3(-0.584121f, 0.705062f, -0.173395f),
                    new Vector3(-0.51689f, 0.069081f, 0.752092f),
                    new Vector3(-0.378328f, -1.037777f, -0.09336f),
                    new Vector3(-0.358446f, 0.194389f, -1.030805f),
                    new Vector3(-0.278847f, -0.803894f, 0.440665f),
                    new Vector3(-0.255475f, 0.644603f, -0.661367f),
                    new Vector3(-0.223173f, 0.844232f, 0.273963f),
                    new Vector3(-0.146694f, -0.384435f, 0.836102f),
                    new Vector3(0.047172f, 0.170886f, 0.897866f),
                    new Vector3(0.15578f, -1.086392f, 0.15593f),
                    new Vector3(0.180355f, 0.436649f, -1.002815f),
                    new Vector3(0.228699f, 0.649956f, 0.602366f),
                    new Vector3(0.285215f, -0.70209f, 0.586439f),
                    new Vector3(0.308587f, 0.746408f, -0.515593f),
                    new Vector3(0.328551f, 0.869786f, 0.062473f),
                    new Vector3(0.598896f, 0.196439f, 0.686376f),
                    new Vector3(0.694582f, -0.844133f, 0.183918f),
                    new Vector3(0.714463f, 0.388033f, -0.753526f),
                    new Vector3(0.746014f, -0.343089f, 0.493903f),
                    new Vector3(0.760459f, 0.552132f, -0.18719f),
                    new Vector3(0.927542f, 0.135981f, 0.198403f),
                    new Vector3(1.032273f, -0.403533f, -0.020084f),
                    new Vector3(1.039867f, 0.067112f, -0.378156f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            26, () =>
            {
                var faces = new[]
                {
                    new[] { 1, 0, 4 },
                    new[] { 3, 2, 5 },
                    new[] { 3, 0, 6 },
                    new[] { 1, 2, 7 },
                    new[] { 3, 5, 4, 0 },
                    new[] { 1, 4, 5, 2 },
                    new[] { 1, 7, 6, 0 },
                    new[] { 3, 6, 7, 2 }
                };
                var verts = new[]
                {
                    new Vector3(-0.57735f, 0.57735f, 0.0f),
                    new Vector3(0.57735f, 0.57735f, 0.0f),
                    new Vector3(0.57735f, -0.57735f, 0.0f),
                    new Vector3(-0.57735f, -0.57735f, 0.0f),
                    new Vector3(0.0f, 0.57735f, 1.0f),
                    new Vector3(0.0f, -0.57735f, 1.0f),
                    new Vector3(-0.57735f, 0.0f, -1.0f),
                    new Vector3(0.57735f, 0.0f, -1.0f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            27, () =>
            {
                var faces = new[]
                {
                    new[] { 2, 5, 8 },
                    new[] { 5, 4, 10 },
                    new[] { 8, 11, 7 },
                    new[] { 2, 1, 0 },
                    new[] { 6, 3, 9 },
                    new[] { 3, 0, 1 },
                    new[] { 9, 7, 11 },
                    new[] { 6, 10, 4 },
                    new[] { 5, 2, 0, 4 },
                    new[] { 8, 5, 10, 11 },
                    new[] { 2, 8, 7, 1 },
                    new[] { 3, 6, 4, 0 },
                    new[] { 9, 3, 1, 7 },
                    new[] { 6, 9, 11, 10 }
                };
                var verts = new[]
                {
                    new Vector3(-0.96936f, 0.238651f, 0.058198f),
                    new Vector3(-0.683128f, -0.715413f, 0.146701f),
                    new Vector3(-0.623092f, -0.255511f, -0.739236f),
                    new Vector3(-0.478567f, -0.06233f, 0.875836f),
                    new Vector3(-0.286232f, 0.954064f, -0.088503f),
                    new Vector3(0.060036f, 0.459902f, -0.885938f),
                    new Vector3(0.204561f, 0.653083f, 0.729135f),
                    new Vector3(0.286232f, -0.954064f, 0.088503f),
                    new Vector3(0.346268f, -0.494162f, -0.797435f),
                    new Vector3(0.490793f, -0.300981f, 0.817638f),
                    new Vector3(0.683128f, 0.715413f, -0.146701f),
                    new Vector3(0.96936f, -0.238651f, -0.058198f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            28, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 0, 2 },
                    new[] { 9, 8, 14 },
                    new[] { 11, 15, 13 },
                    new[] { 5, 7, 1 },
                    new[] { 6, 1, 7 },
                    new[] { 12, 13, 15 },
                    new[] { 10, 14, 8 },
                    new[] { 4, 2, 0 },
                    new[] { 5, 3, 9, 11 },
                    new[] { 3, 5, 1, 0 },
                    new[] { 9, 3, 2, 8 },
                    new[] { 11, 9, 14, 15 },
                    new[] { 5, 11, 13, 7 },
                    new[] { 4, 6, 12, 10 },
                    new[] { 6, 4, 0, 1 },
                    new[] { 12, 6, 7, 13 },
                    new[] { 10, 12, 15, 14 },
                    new[] { 4, 10, 8, 2 }
                };
                var verts = new[]
                {
                    new Vector3(-1.055402f, 0.383836f, -0.00011f),
                    new Vector3(-1.017695f, -0.474869f, 0.000238f),
                    new Vector3(-0.474869f, 1.017695f, -0.000394f),
                    new Vector3(-0.448233f, 0.410252f, -0.607929f),
                    new Vector3(-0.448179f, 0.410746f, 0.607634f),
                    new Vector3(-0.410526f, -0.448453f, -0.607581f),
                    new Vector3(-0.410472f, -0.447959f, 0.607981f),
                    new Vector3(-0.383836f, -1.055402f, 0.000446f),
                    new Vector3(0.383836f, 1.055402f, -0.000447f),
                    new Vector3(0.410472f, 0.447959f, -0.607982f),
                    new Vector3(0.410526f, 0.448453f, 0.60758f),
                    new Vector3(0.448179f, -0.410746f, -0.607635f),
                    new Vector3(0.448233f, -0.410252f, 0.607928f),
                    new Vector3(0.474869f, -1.017695f, 0.000392f),
                    new Vector3(1.017695f, 0.474869f, -0.000239f),
                    new Vector3(1.055402f, -0.383836f, 0.000109f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            29, () =>
            {
                var faces = new[]
                {
                    new[] { 4, 0, 1 },
                    new[] { 10, 6, 13 },
                    new[] { 12, 15, 14 },
                    new[] { 7, 8, 2 },
                    new[] { 3, 0, 2 },
                    new[] { 9, 8, 14 },
                    new[] { 11, 15, 13 },
                    new[] { 5, 6, 1 },
                    new[] { 7, 4, 10, 12 },
                    new[] { 4, 7, 2, 0 },
                    new[] { 10, 4, 1, 6 },
                    new[] { 12, 10, 13, 15 },
                    new[] { 7, 12, 14, 8 },
                    new[] { 5, 3, 9, 11 },
                    new[] { 3, 5, 1, 0 },
                    new[] { 9, 3, 2, 8 },
                    new[] { 11, 9, 14, 15 },
                    new[] { 5, 11, 13, 6 }
                };
                var verts = new[]
                {
                    new Vector3(-1.105f, -0.077473f, -0.184867f),
                    new Vector3(-0.863019f, 0.717824f, 0.033637f),
                    new Vector3(-0.699688f, -0.827387f, -0.295079f),
                    new Vector3(-0.617244f, -0.39909f, 0.445571f),
                    new Vector3(-0.487757f, 0.321617f, -0.630438f),
                    new Vector3(-0.375262f, 0.396206f, 0.664075f),
                    new Vector3(-0.115492f, 1.092629f, 0.232437f),
                    new Vector3(-0.082444f, -0.428297f, -0.740649f),
                    new Vector3(0.115493f, -1.092629f, -0.232437f),
                    new Vector3(0.197937f, -0.664332f, 0.508212f),
                    new Vector3(0.25977f, 0.696423f, -0.431638f),
                    new Vector3(0.439918f, 0.130964f, 0.726716f),
                    new Vector3(0.665082f, -0.053491f, -0.541849f),
                    new Vector3(0.699688f, 0.827387f, 0.295079f),
                    new Vector3(0.863019f, -0.717824f, -0.033637f),
                    new Vector3(1.105f, 0.077473f, 0.184867f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            30, () =>
            {
                var faces = new[]
                {
                    new[] { 4, 0, 1 },
                    new[] { 10, 5, 11 },
                    new[] { 16, 17, 19 },
                    new[] { 13, 18, 14 },
                    new[] { 7, 8, 2 },
                    new[] { 6, 2, 8 },
                    new[] { 12, 14, 18 },
                    new[] { 15, 19, 17 },
                    new[] { 9, 11, 5 },
                    new[] { 3, 1, 0 },
                    new[] { 4, 7, 2, 0 },
                    new[] { 10, 4, 1, 5 },
                    new[] { 16, 10, 11, 17 },
                    new[] { 13, 16, 19, 18 },
                    new[] { 7, 13, 14, 8 },
                    new[] { 6, 3, 0, 2 },
                    new[] { 12, 6, 8, 14 },
                    new[] { 15, 12, 18, 19 },
                    new[] { 9, 15, 17, 11 },
                    new[] { 3, 9, 5, 1 },
                    new[] { 7, 4, 10, 16, 13 },
                    new[] { 3, 6, 12, 15, 9 }
                };
                var verts = new[]
                {
                    new Vector3(-1.197125f, -0.118752f, -0.001762f),
                    new Vector3(-1.038244f, 0.607337f, -0.020132f),
                    new Vector3(-0.898745f, -0.799482f, 0.017282f),
                    new Vector3(-0.619431f, 0.145275f, 0.38469f),
                    new Vector3(-0.61625f, 0.124807f, -0.396793f),
                    new Vector3(-0.482789f, 1.101444f, -0.030813f),
                    new Vector3(-0.321051f, -0.535454f, 0.403734f),
                    new Vector3(-0.317871f, -0.555923f, -0.37775f),
                    new Vector3(-0.257075f, -1.174837f, 0.029724f),
                    new Vector3(-0.063976f, 0.639383f, 0.37401f),
                    new Vector3(-0.060795f, 0.618915f, -0.407474f),
                    new Vector3(0.257076f, 1.174837f, -0.029725f),
                    new Vector3(0.418813f, -0.462061f, 0.404823f),
                    new Vector3(0.421993f, -0.48253f, -0.376661f),
                    new Vector3(0.482789f, -1.101444f, 0.030813f),
                    new Vector3(0.577694f, 0.264028f, 0.386452f),
                    new Vector3(0.580875f, 0.24356f, -0.395032f),
                    new Vector3(0.898745f, 0.799482f, -0.017282f),
                    new Vector3(1.038244f, -0.607337f, 0.020132f),
                    new Vector3(1.197125f, 0.118752f, 0.001761f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            31, () =>
            {
                var faces = new[]
                {
                    new[] { 4, 0, 1 },
                    new[] { 8, 3, 10 },
                    new[] { 14, 17, 19 },
                    new[] { 13, 18, 16 },
                    new[] { 7, 9, 2 },
                    new[] { 5, 0, 2 },
                    new[] { 11, 9, 16 },
                    new[] { 15, 18, 19 },
                    new[] { 12, 17, 10 },
                    new[] { 6, 3, 1 },
                    new[] { 4, 7, 2, 0 },
                    new[] { 8, 4, 1, 3 },
                    new[] { 14, 8, 10, 17 },
                    new[] { 13, 14, 19, 18 },
                    new[] { 7, 13, 16, 9 },
                    new[] { 5, 6, 1, 0 },
                    new[] { 11, 5, 2, 9 },
                    new[] { 15, 11, 16, 18 },
                    new[] { 12, 15, 19, 17 },
                    new[] { 6, 12, 10, 3 },
                    new[] { 7, 4, 8, 14, 13 },
                    new[] { 6, 5, 11, 15, 12 }
                };
                var verts = new[]
                {
                    new Vector3(-1.14213f, -0.353364f, -0.133745f),
                    new Vector3(-1.138435f, 0.385484f, -0.050817f),
                    new Vector3(-0.70957f, -0.957238f, -0.165587f),
                    new Vector3(-0.699897f, 0.97709f, 0.051522f),
                    new Vector3(-0.598391f, 0.052172f, -0.43817f),
                    new Vector3(-0.543739f, -0.405536f, 0.304426f),
                    new Vector3(-0.540044f, 0.333311f, 0.387353f),
                    new Vector3(-0.165831f, -0.551702f, -0.470012f),
                    new Vector3(-0.159853f, 0.643778f, -0.335832f),
                    new Vector3(-0.005978f, -1.19548f, -0.13418f),
                    new Vector3(0.005978f, 1.195481f, 0.134181f),
                    new Vector3(0.159852f, -0.643778f, 0.335832f),
                    new Vector3(0.165831f, 0.551702f, 0.470012f),
                    new Vector3(0.540044f, -0.333311f, -0.387353f),
                    new Vector3(0.543739f, 0.405536f, -0.304425f),
                    new Vector3(0.598391f, -0.052172f, 0.43817f),
                    new Vector3(0.699896f, -0.97709f, -0.051521f),
                    new Vector3(0.70957f, 0.957238f, 0.165587f),
                    new Vector3(1.138435f, -0.385484f, 0.050817f),
                    new Vector3(1.142129f, 0.353364f, 0.133745f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            32, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 0, 2 },
                    new[] { 9, 7, 16 },
                    new[] { 15, 21, 23 },
                    new[] { 12, 19, 14 },
                    new[] { 5, 6, 1 },
                    new[] { 18, 24, 22 },
                    new[] { 24, 19, 23 },
                    new[] { 22, 20, 17 },
                    new[] { 20, 21, 16 },
                    new[] { 17, 8, 10 },
                    new[] { 8, 7, 2 },
                    new[] { 10, 4, 11 },
                    new[] { 4, 0, 1 },
                    new[] { 11, 13, 18 },
                    new[] { 13, 6, 14 },
                    new[] { 3, 5, 1, 0 },
                    new[] { 9, 3, 2, 7 },
                    new[] { 15, 9, 16, 21 },
                    new[] { 12, 15, 23, 19 },
                    new[] { 5, 12, 14, 6 },
                    new[] { 5, 3, 9, 15, 12 },
                    new[] { 18, 22, 17, 10, 11 },
                    new[] { 18, 13, 14, 19, 24 },
                    new[] { 22, 24, 23, 21, 20 },
                    new[] { 17, 20, 16, 7, 8 },
                    new[] { 10, 8, 2, 0, 4 },
                    new[] { 11, 4, 1, 6, 13 }
                };
                var verts = new[]
                {
                    new Vector3(-1.086754f, 0.270723f, -0.02221f),
                    new Vector3(-0.951485f, 0.016307f, 0.590957f),
                    new Vector3(-0.844123f, 0.345447f, -0.65034f),
                    new Vector3(-0.727726f, -0.227595f, -0.308179f),
                    new Vector3(-0.678317f, 0.606577f, 0.401324f),
                    new Vector3(-0.592457f, -0.482012f, 0.304989f),
                    new Vector3(-0.489983f, -0.320625f, 0.954953f),
                    new Vector3(-0.316268f, 0.211936f, -1.053507f),
                    new Vector3(-0.285732f, 0.727482f, -0.61501f),
                    new Vector3(-0.199871f, -0.361106f, -0.711346f),
                    new Vector3(-0.183258f, 0.888869f, 0.034954f),
                    new Vector3(-0.047989f, 0.634452f, 0.648121f),
                    new Vector3(0.019f, -0.772761f, 0.28078f),
                    new Vector3(0.068408f, 0.06141f, 0.990283f),
                    new Vector3(0.121473f, -0.611374f, 0.930744f),
                    new Vector3(0.261631f, -0.698037f, -0.34735f),
                    new Vector3(0.295188f, -0.078813f, -1.077716f),
                    new Vector3(0.344597f, 0.755358f, -0.368213f),
                    new Vector3(0.563468f, 0.343703f, 0.623912f),
                    new Vector3(0.649328f, -0.744886f, 0.527576f),
                    new Vector3(0.703625f, 0.25704f, -0.654181f),
                    new Vector3(0.75669f, -0.415745f, -0.71372f),
                    new Vector3(0.806099f, 0.418427f, -0.004217f),
                    new Vector3(0.891959f, -0.670162f, -0.100552f),
                    new Vector3(0.922496f, -0.154616f, 0.337944f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            33, () =>
            {
                var faces = new[]
                {
                    new[] { 5, 6, 11 },
                    new[] { 9, 15, 19 },
                    new[] { 8, 17, 13 },
                    new[] { 3, 7, 4 },
                    new[] { 0, 1, 2 },
                    new[] { 20, 21, 24 },
                    new[] { 21, 13, 17 },
                    new[] { 24, 23, 22 },
                    new[] { 23, 19, 15 },
                    new[] { 22, 14, 18 },
                    new[] { 14, 11, 6 },
                    new[] { 18, 10, 16 },
                    new[] { 10, 2, 1 },
                    new[] { 16, 12, 20 },
                    new[] { 12, 4, 7 },
                    new[] { 5, 0, 2, 6 },
                    new[] { 9, 5, 11, 15 },
                    new[] { 8, 9, 19, 17 },
                    new[] { 3, 8, 13, 7 },
                    new[] { 0, 3, 4, 1 },
                    new[] { 0, 5, 9, 8, 3 },
                    new[] { 20, 24, 22, 18, 16 },
                    new[] { 20, 12, 7, 13, 21 },
                    new[] { 24, 21, 17, 19, 23 },
                    new[] { 22, 23, 15, 11, 14 },
                    new[] { 18, 14, 6, 2, 10 },
                    new[] { 16, 10, 1, 4, 12 }
                };
                var verts = new[]
                {
                    new Vector3(-0.799512f, 0.192706f, 0.001565f),
                    new Vector3(-0.776446f, 0.593934f, 0.546986f),
                    new Vector3(-0.713384f, 0.860598f, -0.072621f),
                    new Vector3(-0.640335f, -0.34095f, 0.387405f),
                    new Vector3(-0.617268f, 0.060277f, 0.932827f),
                    new Vector3(-0.538299f, 0.090521f, -0.615141f),
                    new Vector3(-0.452171f, 0.758412f, -0.689327f),
                    new Vector3(-0.296652f, -0.536533f, 0.937522f),
                    new Vector3(-0.280744f, -0.772953f, 0.009162f),
                    new Vector3(-0.217683f, -0.506289f, -0.610445f),
                    new Vector3(-0.200278f, 0.902051f, 0.367837f),
                    new Vector3(-0.09258f, 0.326409f, -1.06757f),
                    new Vector3(0.057277f, 0.038576f, 0.99214f),
                    new Vector3(0.062939f, -0.968537f, 0.559279f),
                    new Vector3(0.222374f, 0.736712f, -0.630014f),
                    new Vector3(0.228036f, -0.270402f, -1.062874f),
                    new Vector3(0.314991f, 0.558821f, 0.642957f),
                    new Vector3(0.324152f, -1.070722f, -0.057426f),
                    new Vector3(0.378052f, 0.825485f, 0.023349f),
                    new Vector3(0.387214f, -0.804058f, -0.677033f),
                    new Vector3(0.635607f, -0.037989f, 0.647653f),
                    new Vector3(0.639107f, -0.66042f, 0.38013f),
                    new Vector3(0.737643f, 0.393482f, -0.354893f),
                    new Vector3(0.741142f, -0.228948f, -0.622416f),
                    new Vector3(0.89682f, -0.140175f, 0.030947f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            34, () =>
            {
                var faces = new[]
                {
                    new[] { 15, 7, 14 },
                    new[] { 7, 3, 1 },
                    new[] { 14, 12, 21 },
                    new[] { 12, 5, 11 },
                    new[] { 21, 25, 27 },
                    new[] { 25, 20, 26 },
                    new[] { 27, 29, 23 },
                    new[] { 29, 28, 24 },
                    new[] { 23, 17, 15 },
                    new[] { 17, 18, 8 },
                    new[] { 4, 10, 13 },
                    new[] { 10, 8, 18 },
                    new[] { 13, 22, 16 },
                    new[] { 22, 24, 28 },
                    new[] { 16, 19, 9 },
                    new[] { 19, 26, 20 },
                    new[] { 9, 6, 2 },
                    new[] { 6, 11, 5 },
                    new[] { 2, 0, 4 },
                    new[] { 0, 1, 3 },
                    new[] { 15, 14, 21, 27, 23 },
                    new[] { 15, 17, 8, 3, 7 },
                    new[] { 14, 7, 1, 5, 12 },
                    new[] { 21, 12, 11, 20, 25 },
                    new[] { 27, 25, 26, 28, 29 },
                    new[] { 23, 29, 24, 18, 17 },
                    new[] { 4, 13, 16, 9, 2 },
                    new[] { 4, 0, 3, 8, 10 },
                    new[] { 13, 10, 18, 24, 22 },
                    new[] { 16, 22, 28, 26, 19 },
                    new[] { 9, 19, 20, 11, 6 },
                    new[] { 2, 6, 5, 1, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.976027f, 0.021192f, 0.216616f),
                    new Vector3(-0.8986f, -0.336852f, -0.281155f),
                    new Vector3(-0.800821f, 0.595002f, 0.068255f),
                    new Vector3(-0.778424f, -0.560713f, 0.282236f),
                    new Vector3(-0.680644f, 0.371141f, 0.631647f),
                    new Vector3(-0.675542f, 0.015675f, -0.737155f),
                    new Vector3(-0.615111f, 0.591592f, -0.521207f),
                    new Vector3(-0.523949f, -0.823998f, -0.215648f),
                    new Vector3(-0.360916f, -0.5704f, 0.737823f),
                    new Vector3(-0.319728f, 0.941541f, -0.106177f),
                    new Vector3(-0.300485f, 0.005517f, 0.953771f),
                    new Vector3(-0.19445f, 0.362214f, -0.911587f),
                    new Vector3(-0.163033f, -0.253598f, -0.953472f),
                    new Vector3(-0.125279f, 0.579327f, 0.80541f),
                    new Vector3(-0.069344f, -0.772544f, -0.631163f),
                    new Vector3(0.050833f, -0.996405f, -0.067772f),
                    new Vector3(0.097779f, 0.931854f, 0.34941f),
                    new Vector3(0.151593f, -0.839673f, 0.521506f),
                    new Vector3(0.194449f, -0.362214f, 0.911587f),
                    new Vector3(0.283489f, 0.928444f, -0.240052f),
                    new Vector3(0.360916f, 0.5704f, -0.737823f),
                    new Vector3(0.411749f, -0.426005f, -0.805595f),
                    new Vector3(0.477939f, 0.56623f, 0.671534f),
                    new Vector3(0.606198f, -0.788219f, 0.105992f),
                    new Vector3(0.675542f, -0.015675f, 0.737155f),
                    new Vector3(0.735567f, 0.083254f, -0.672317f),
                    new Vector3(0.778423f, 0.560713f, -0.282236f),
                    new Vector3(0.829257f, -0.435692f, -0.350008f),
                    new Vector3(0.8986f, 0.336852f, 0.281155f),
                    new Vector3(0.930017f, -0.27896f, 0.23927f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            35, () =>
            {
                var faces = new[]
                {
                    new[] { 9, 16, 12 },
                    new[] { 16, 13, 17 },
                    new[] { 12, 15, 7 },
                    new[] { 9, 3, 6 },
                    new[] { 8, 1, 5 },
                    new[] { 1, 2, 0 },
                    new[] { 5, 4, 11 },
                    new[] { 8, 14, 10 },
                    new[] { 16, 9, 6, 13 },
                    new[] { 12, 16, 17, 15 },
                    new[] { 9, 12, 7, 3 },
                    new[] { 1, 8, 10, 2 },
                    new[] { 5, 1, 0, 4 },
                    new[] { 8, 5, 11, 14 },
                    new[] { 7, 4, 0, 3 },
                    new[] { 3, 0, 2, 6 },
                    new[] { 6, 2, 10, 13 },
                    new[] { 13, 10, 14, 17 },
                    new[] { 17, 14, 11, 15 },
                    new[] { 15, 11, 4, 7 }
                };
                var verts = new[]
                {
                    new Vector3(-0.903332f, -0.063468f, 0.034076f),
                    new Vector3(-0.833437f, 0.28305f, 0.763458f),
                    new Vector3(-0.589483f, 0.680853f, 0.100738f),
                    new Vector3(-0.561749f, -0.142046f, -0.696749f),
                    new Vector3(-0.484641f, -0.705032f, 0.29875f),
                    new Vector3(-0.414746f, -0.358514f, 1.028133f),
                    new Vector3(-0.2479f, 0.602275f, -0.630087f),
                    new Vector3(-0.143058f, -0.78361f, -0.432074f),
                    new Vector3(-0.100897f, 0.385807f, 1.094795f),
                    new Vector3(0.065949f, 0.076155f, -1.160799f),
                    new Vector3(0.143058f, 0.78361f, 0.432075f),
                    new Vector3(0.2479f, -0.602274f, 0.630087f),
                    new Vector3(0.484641f, -0.565409f, -0.896124f),
                    new Vector3(0.484641f, 0.705032f, -0.29875f),
                    new Vector3(0.561749f, 0.142046f, 0.696749f),
                    new Vector3(0.589483f, -0.680852f, -0.100737f),
                    new Vector3(0.79849f, 0.178912f, -0.829462f),
                    new Vector3(0.903332f, 0.063468f, -0.034075f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            36, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 11, 9 },
                    new[] { 11, 10, 16 },
                    new[] { 9, 13, 5 },
                    new[] { 3, 0, 2 },
                    new[] { 14, 8, 6 },
                    new[] { 8, 12, 4 },
                    new[] { 6, 1, 7 },
                    new[] { 14, 15, 17 },
                    new[] { 11, 3, 2, 10 },
                    new[] { 9, 11, 16, 13 },
                    new[] { 3, 9, 5, 0 },
                    new[] { 8, 14, 17, 12 },
                    new[] { 6, 8, 4, 1 },
                    new[] { 14, 6, 7, 15 },
                    new[] { 5, 7, 1, 0 },
                    new[] { 0, 1, 4, 2 },
                    new[] { 2, 4, 12, 10 },
                    new[] { 10, 12, 17, 16 },
                    new[] { 16, 17, 15, 13 },
                    new[] { 13, 15, 7, 5 }
                };
                var verts = new[]
                {
                    new Vector3(-0.82124f, -0.196132f, -0.329082f),
                    new Vector3(-0.725355f, -0.267867f, 0.472553f),
                    new Vector3(-0.627806f, 0.589563f, -0.281911f),
                    new Vector3(-0.577286f, 0.20167f, -0.991803f),
                    new Vector3(-0.531921f, 0.517828f, 0.519724f),
                    new Vector3(-0.241376f, -0.749828f, -0.447988f),
                    new Vector3(-0.196012f, -0.43367f, 1.063538f),
                    new Vector3(-0.145492f, -0.821562f, 0.353647f),
                    new Vector3(-0.002578f, 0.352025f, 1.11071f),
                    new Vector3(0.002578f, -0.352025f, -1.110709f),
                    new Vector3(0.145492f, 0.821563f, -0.353646f),
                    new Vector3(0.196012f, 0.43367f, -1.063538f),
                    new Vector3(0.241376f, 0.749828f, 0.447989f),
                    new Vector3(0.531921f, -0.517828f, -0.519723f),
                    new Vector3(0.577285f, -0.20167f, 0.991804f),
                    new Vector3(0.627806f, -0.589563f, 0.281912f),
                    new Vector3(0.725355f, 0.267867f, -0.472551f),
                    new Vector3(0.82124f, 0.196133f, 0.329083f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            37, () =>
            {
                var faces = new[]
                {
                    new[] { 2, 3, 0 },
                    new[] { 4, 5, 1 },
                    new[] { 10, 7, 6 },
                    new[] { 11, 9, 8 },
                    new[] { 15, 13, 12 },
                    new[] { 19, 17, 14 },
                    new[] { 18, 21, 16 },
                    new[] { 22, 23, 20 },
                    new[] { 3, 4, 1, 0 },
                    new[] { 6, 7, 3, 2 },
                    new[] { 7, 8, 4, 3 },
                    new[] { 8, 9, 5, 4 },
                    new[] { 10, 11, 8, 7 },
                    new[] { 14, 17, 15, 12 },
                    new[] { 15, 18, 16, 13 },
                    new[] { 17, 20, 18, 15 },
                    new[] { 19, 22, 20, 17 },
                    new[] { 20, 23, 21, 18 },
                    new[] { 22, 19, 2, 0 },
                    new[] { 19, 14, 6, 2 },
                    new[] { 14, 12, 10, 6 },
                    new[] { 12, 13, 11, 10 },
                    new[] { 11, 13, 16, 9 },
                    new[] { 9, 16, 21, 5 },
                    new[] { 5, 21, 23, 1 },
                    new[] { 1, 23, 22, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.862856f, 0.357407f, -0.357407f),
                    new Vector3(-0.862856f, -0.357407f, -0.357407f),
                    new Vector3(-0.357407f, 0.862856f, -0.357407f),
                    new Vector3(-0.357407f, 0.357407f, -0.862856f),
                    new Vector3(-0.357407f, -0.357407f, -0.862856f),
                    new Vector3(-0.357407f, -0.862856f, -0.357407f),
                    new Vector3(0.357407f, 0.862856f, -0.357407f),
                    new Vector3(0.357407f, 0.357407f, -0.862856f),
                    new Vector3(0.357407f, -0.357407f, -0.862856f),
                    new Vector3(0.357407f, -0.862856f, -0.357407f),
                    new Vector3(0.862856f, 0.357407f, -0.357407f),
                    new Vector3(0.862856f, -0.357407f, -0.357407f),
                    new Vector3(0.862856f, 0.357407f, 0.357407f),
                    new Vector3(0.862856f, -0.357407f, 0.357407f),
                    new Vector3(0.357407f, 0.862856f, 0.357407f),
                    new Vector3(0.505449f, 0.0f, 0.862856f),
                    new Vector3(0.357407f, -0.862856f, 0.357407f),
                    new Vector3(-0.0f, 0.505449f, 0.862856f),
                    new Vector3(-0.0f, -0.505449f, 0.862856f),
                    new Vector3(-0.357407f, 0.862856f, 0.357407f),
                    new Vector3(-0.505449f, 0.0f, 0.862856f),
                    new Vector3(-0.357407f, -0.862856f, 0.357407f),
                    new Vector3(-0.862856f, 0.357407f, 0.357407f),
                    new Vector3(-0.862856f, -0.357407f, 0.357407f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            38, () =>
            {
                var faces = new[]
                {
                    new[] { 17, 10, 19 },
                    new[] { 24, 26, 29 },
                    new[] { 23, 28, 22 },
                    new[] { 15, 14, 7 },
                    new[] { 12, 2, 5 },
                    new[] { 4, 1, 0 },
                    new[] { 8, 3, 11 },
                    new[] { 16, 20, 25 },
                    new[] { 18, 27, 21 },
                    new[] { 9, 13, 6 },
                    new[] { 17, 12, 5, 10 },
                    new[] { 24, 17, 19, 26 },
                    new[] { 23, 24, 29, 28 },
                    new[] { 15, 23, 22, 14 },
                    new[] { 12, 15, 7, 2 },
                    new[] { 4, 9, 6, 1 },
                    new[] { 8, 4, 0, 3 },
                    new[] { 16, 8, 11, 20 },
                    new[] { 18, 16, 25, 27 },
                    new[] { 9, 18, 21, 13 },
                    new[] { 22, 20, 11, 14 },
                    new[] { 14, 11, 3, 7 },
                    new[] { 7, 3, 0, 2 },
                    new[] { 2, 0, 1, 5 },
                    new[] { 5, 1, 6, 10 },
                    new[] { 10, 6, 13, 19 },
                    new[] { 19, 13, 21, 26 },
                    new[] { 26, 21, 27, 29 },
                    new[] { 29, 27, 25, 28 },
                    new[] { 28, 25, 20, 22 },
                    new[] { 12, 17, 24, 23, 15 },
                    new[] { 9, 4, 8, 16, 18 }
                };
                var verts = new[]
                {
                    new Vector3(-1.047541f, -0.14473f, -0.164687f),
                    new Vector3(-0.97266f, 0.443085f, 0.054945f),
                    new Vector3(-0.794156f, 0.029302f, -0.716846f),
                    new Vector3(-0.77069f, -0.7105f, -0.215961f),
                    new Vector3(-0.748241f, -0.047945f, 0.383423f),
                    new Vector3(-0.719275f, 0.617116f, -0.497215f),
                    new Vector3(-0.574648f, 0.82842f, 0.359043f),
                    new Vector3(-0.517304f, -0.536469f, -0.76812f),
                    new Vector3(-0.471389f, -0.613715f, 0.332149f),
                    new Vector3(-0.350229f, 0.33739f, 0.687521f),
                    new Vector3(-0.321263f, 1.002451f, -0.193117f),
                    new Vector3(-0.247853f, -1.03812f, -0.079291f),
                    new Vector3(-0.228431f, 0.309073f, -0.749313f),
                    new Vector3(-0.005532f, 0.864089f, 0.631452f),
                    new Vector3(0.005532f, -0.864089f, -0.631452f),
                    new Vector3(0.048421f, -0.256696f, -0.800587f),
                    new Vector3(0.097726f, -0.578045f, 0.604557f),
                    new Vector3(0.169581f, 0.694408f, -0.445215f),
                    new Vector3(0.172608f, 0.009769f, 0.824189f),
                    new Vector3(0.247853f, 1.03812f, 0.079291f),
                    new Vector3(0.321263f, -1.002451f, 0.193117f),
                    new Vector3(0.517305f, 0.536469f, 0.76812f),
                    new Vector3(0.574648f, -0.82842f, -0.359043f),
                    new Vector3(0.617537f, -0.221027f, -0.528178f),
                    new Vector3(0.692418f, 0.366788f, -0.308546f),
                    new Vector3(0.719275f, -0.617116f, 0.497214f),
                    new Vector3(0.77069f, 0.7105f, 0.215961f),
                    new Vector3(0.794156f, -0.029301f, 0.716846f),
                    new Vector3(0.97266f, -0.443085f, -0.054945f),
                    new Vector3(1.047542f, 0.14473f, 0.164687f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            39, () =>
            {
                var faces = new[]
                {
                    new[] { 11, 4, 13 },
                    new[] { 19, 21, 26 },
                    new[] { 20, 28, 24 },
                    new[] { 12, 15, 7 },
                    new[] { 6, 2, 0 },
                    new[] { 9, 5, 1 },
                    new[] { 10, 3, 8 },
                    new[] { 18, 16, 25 },
                    new[] { 23, 29, 27 },
                    new[] { 17, 22, 14 },
                    new[] { 11, 6, 0, 4 },
                    new[] { 19, 11, 13, 21 },
                    new[] { 20, 19, 26, 28 },
                    new[] { 12, 20, 24, 15 },
                    new[] { 6, 12, 7, 2 },
                    new[] { 9, 17, 14, 5 },
                    new[] { 10, 9, 1, 3 },
                    new[] { 18, 10, 8, 16 },
                    new[] { 23, 18, 25, 29 },
                    new[] { 17, 23, 27, 22 },
                    new[] { 24, 25, 16, 15 },
                    new[] { 15, 16, 8, 7 },
                    new[] { 7, 8, 3, 2 },
                    new[] { 2, 3, 1, 0 },
                    new[] { 0, 1, 5, 4 },
                    new[] { 4, 5, 14, 13 },
                    new[] { 13, 14, 22, 21 },
                    new[] { 21, 22, 27, 26 },
                    new[] { 26, 27, 29, 28 },
                    new[] { 28, 29, 25, 24 },
                    new[] { 6, 11, 19, 20, 12 },
                    new[] { 17, 9, 10, 18, 23 }
                };
                var verts = new[]
                {
                    new Vector3(-1.006864f, 0.217224f, -0.290603f),
                    new Vector3(-0.990318f, 0.219795f, 0.341133f),
                    new Vector3(-0.944481f, -0.411647f, -0.289678f),
                    new Vector3(-0.927935f, -0.409077f, 0.342059f),
                    new Vector3(-0.687819f, 0.762632f, -0.301179f),
                    new Vector3(-0.671273f, 0.765203f, 0.330558f),
                    new Vector3(-0.551737f, -0.055664f, -0.63377f),
                    new Vector3(-0.524499f, -0.883775f, -0.298756f),
                    new Vector3(-0.507953f, -0.881203f, 0.33298f),
                    new Vector3(-0.446854f, 0.274173f, 0.659035f),
                    new Vector3(-0.384471f, -0.354698f, 0.65996f),
                    new Vector3(-0.232692f, 0.489744f, -0.644346f),
                    new Vector3(-0.131755f, -0.527791f, -0.642848f),
                    new Vector3(-0.10921f, 1.01625f, -0.317365f),
                    new Vector3(-0.092665f, 1.018821f, 0.314371f),
                    new Vector3(0.092664f, -1.018821f, -0.314371f),
                    new Vector3(0.10921f, -1.01625f, 0.317365f),
                    new Vector3(0.131755f, 0.527791f, 0.642848f),
                    new Vector3(0.232692f, -0.489745f, 0.644346f),
                    new Vector3(0.384471f, 0.354698f, -0.65996f),
                    new Vector3(0.446854f, -0.274173f, -0.659035f),
                    new Vector3(0.507953f, 0.881203f, -0.33298f),
                    new Vector3(0.524499f, 0.883774f, 0.298756f),
                    new Vector3(0.551737f, 0.055664f, 0.63377f),
                    new Vector3(0.671273f, -0.765203f, -0.330558f),
                    new Vector3(0.687819f, -0.762632f, 0.301179f),
                    new Vector3(0.927935f, 0.409076f, -0.342059f),
                    new Vector3(0.944481f, 0.411647f, 0.289678f),
                    new Vector3(0.990318f, -0.219795f, -0.341133f),
                    new Vector3(1.006864f, -0.217224f, 0.290603f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            40, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 1, 8 },
                    new[] { 14, 16, 24 },
                    new[] { 18, 27, 26 },
                    new[] { 11, 19, 9 },
                    new[] { 4, 2, 0 },
                    new[] { 20, 10, 17 },
                    new[] { 10, 5, 3 },
                    new[] { 17, 15, 25 },
                    new[] { 15, 7, 13 },
                    new[] { 25, 30, 32 },
                    new[] { 30, 23, 31 },
                    new[] { 32, 34, 28 },
                    new[] { 34, 33, 29 },
                    new[] { 28, 21, 20 },
                    new[] { 21, 22, 12 },
                    new[] { 6, 4, 0, 1 },
                    new[] { 14, 6, 8, 16 },
                    new[] { 18, 14, 24, 27 },
                    new[] { 11, 18, 26, 19 },
                    new[] { 4, 11, 9, 2 },
                    new[] { 26, 31, 23, 19 },
                    new[] { 19, 23, 13, 9 },
                    new[] { 9, 13, 7, 2 },
                    new[] { 2, 7, 3, 0 },
                    new[] { 0, 3, 5, 1 },
                    new[] { 1, 5, 12, 8 },
                    new[] { 8, 12, 22, 16 },
                    new[] { 16, 22, 29, 24 },
                    new[] { 24, 29, 33, 27 },
                    new[] { 27, 33, 31, 26 },
                    new[] { 4, 6, 14, 18, 11 },
                    new[] { 20, 17, 25, 32, 28 },
                    new[] { 20, 21, 12, 5, 10 },
                    new[] { 17, 10, 3, 7, 15 },
                    new[] { 25, 15, 13, 23, 30 },
                    new[] { 32, 30, 31, 33, 34 },
                    new[] { 28, 34, 29, 22, 21 }
                };
                var verts = new[]
                {
                    new Vector3(-1.05518f, -0.061289f, -0.047893f),
                    new Vector3(-0.934164f, 0.280612f, 0.409939f),
                    new Vector3(-0.859454f, -0.241561f, -0.56784f),
                    new Vector3(-0.777777f, -0.505581f, 0.210572f),
                    new Vector3(-0.776073f, 0.311702f, -0.400212f),
                    new Vector3(-0.656761f, -0.163679f, 0.668404f),
                    new Vector3(-0.655057f, 0.653604f, 0.05762f),
                    new Vector3(-0.582051f, -0.685853f, -0.309375f),
                    new Vector3(-0.542629f, 0.653549f, 0.63078f),
                    new Vector3(-0.421745f, -0.191346f, -0.9513f),
                    new Vector3(-0.400139f, -0.685942f, 0.618017f),
                    new Vector3(-0.338365f, 0.361918f, -0.783672f),
                    new Vector3(-0.265226f, 0.209257f, 0.889245f),
                    new Vector3(-0.144342f, -0.635637f, -0.692835f),
                    new Vector3(-0.142556f, 0.915126f, -0.042884f),
                    new Vector3(-0.083446f, -0.977628f, -0.223275f),
                    new Vector3(-0.030129f, 0.915071f, 0.530275f),
                    new Vector3(0.028982f, -0.977682f, 0.349884f),
                    new Vector3(0.05317f, 0.734854f, -0.562831f),
                    new Vector3(0.090755f, 0.070177f, -1.051805f),
                    new Vector3(0.149998f, -0.635781f, 0.807716f),
                    new Vector3(0.233378f, -0.082518f, 0.975345f),
                    new Vector3(0.247274f, 0.47078f, 0.788741f),
                    new Vector3(0.368158f, -0.374115f, -0.793339f),
                    new Vector3(0.40758f, 0.965287f, 0.146815f),
                    new Vector3(0.46669f, -0.927467f, -0.033576f),
                    new Vector3(0.48229f, 0.443114f, -0.830964f),
                    new Vector3(0.603306f, 0.785015f, -0.373132f),
                    new Vector3(0.662498f, -0.374258f, 0.707212f),
                    new Vector3(0.684983f, 0.520995f, 0.405281f),
                    new Vector3(0.745797f, -0.554475f, -0.385895f),
                    new Vector3(0.759693f, -0.001178f, -0.572498f),
                    new Vector3(0.858225f, -0.55453f, 0.187265f),
                    new Vector3(0.880709f, 0.340724f, -0.114666f),
                    new Vector3(0.941605f, -0.001267f, 0.354893f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            41, () =>
            {
                var faces = new[]
                {
                    new[] { 9, 3, 10 },
                    new[] { 18, 21, 27 },
                    new[] { 20, 30, 25 },
                    new[] { 12, 17, 8 },
                    new[] { 5, 1, 0 },
                    new[] { 15, 7, 16 },
                    new[] { 7, 2, 4 },
                    new[] { 16, 19, 26 },
                    new[] { 19, 11, 22 },
                    new[] { 26, 33, 31 },
                    new[] { 33, 29, 34 },
                    new[] { 31, 28, 23 },
                    new[] { 28, 32, 24 },
                    new[] { 23, 13, 15 },
                    new[] { 13, 14, 6 },
                    new[] { 9, 5, 0, 3 },
                    new[] { 18, 9, 10, 21 },
                    new[] { 20, 18, 27, 30 },
                    new[] { 12, 20, 25, 17 },
                    new[] { 5, 12, 8, 1 },
                    new[] { 25, 29, 22, 17 },
                    new[] { 17, 22, 11, 8 },
                    new[] { 8, 11, 4, 1 },
                    new[] { 1, 4, 2, 0 },
                    new[] { 0, 2, 6, 3 },
                    new[] { 3, 6, 14, 10 },
                    new[] { 10, 14, 24, 21 },
                    new[] { 21, 24, 32, 27 },
                    new[] { 27, 32, 34, 30 },
                    new[] { 30, 34, 29, 25 },
                    new[] { 5, 9, 18, 20, 12 },
                    new[] { 15, 16, 26, 31, 23 },
                    new[] { 15, 13, 6, 2, 7 },
                    new[] { 16, 7, 4, 11, 19 },
                    new[] { 26, 19, 22, 29, 33 },
                    new[] { 31, 33, 34, 32, 28 },
                    new[] { 23, 28, 24, 14, 13 }
                };
                var verts = new[]
                {
                    new Vector3(-1.045033f, 0.161365f, 0.036367f),
                    new Vector3(-0.919366f, 0.043922f, -0.521815f),
                    new Vector3(-0.855707f, -0.369212f, 0.190625f),
                    new Vector3(-0.830432f, 0.382234f, 0.532668f),
                    new Vector3(-0.73004f, -0.486655f, -0.367556f),
                    new Vector3(-0.711442f, 0.52894f, -0.271461f),
                    new Vector3(-0.641106f, -0.148342f, 0.686927f),
                    new Vector3(-0.544409f, -0.844483f, 0.055116f),
                    new Vector3(-0.501432f, 0.074765f, -0.928671f),
                    new Vector3(-0.496841f, 0.749809f, 0.224841f),
                    new Vector3(-0.357534f, 0.622166f, 0.777518f),
                    new Vector3(-0.312106f, -0.455811f, -0.774412f),
                    new Vector3(-0.293508f, 0.559783f, -0.678317f),
                    new Vector3(-0.197177f, -0.487108f, 0.858148f),
                    new Vector3(-0.168208f, 0.09159f, 0.931777f),
                    new Vector3(-0.137415f, -0.917347f, 0.467667f),
                    new Vector3(-0.011748f, -1.03479f, -0.090514f),
                    new Vector3(0.049132f, 0.242114f, -1.028796f),
                    new Vector3(0.053724f, 0.917158f, 0.124716f),
                    new Vector3(0.131823f, -0.794577f, -0.603191f),
                    new Vector3(0.17939f, 0.799715f, -0.433466f),
                    new Vector3(0.193031f, 0.789515f, 0.677394f),
                    new Vector3(0.238459f, -0.288463f, -0.874537f),
                    new Vector3(0.335483f, -0.677415f, 0.712518f),
                    new Vector3(0.382357f, 0.258938f, 0.831652f),
                    new Vector3(0.522031f, 0.482046f, -0.783946f),
                    new Vector3(0.538816f, -0.867441f, -0.190639f),
                    new Vector3(0.610965f, 0.820358f, 0.270538f),
                    new Vector3(0.693655f, -0.216333f, 0.696143f),
                    new Vector3(0.711357f, -0.048531f, -0.629687f),
                    new Vector3(0.736632f, 0.702915f, -0.287644f),
                    new Vector3(0.753417f, -0.646572f, 0.305662f),
                    new Vector3(0.800291f, 0.289782f, 0.424796f),
                    new Vector3(0.896988f, -0.406359f, -0.207015f),
                    new Vector3(0.925958f, 0.172339f, -0.133386f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            42, () =>
            {
                var faces = new[]
                {
                    new[] { 0, 3, 4 },
                    new[] { 3, 7, 11 },
                    new[] { 4, 12, 8 },
                    new[] { 12, 18, 22 },
                    new[] { 8, 15, 6 },
                    new[] { 15, 25, 23 },
                    new[] { 6, 10, 2 },
                    new[] { 10, 19, 13 },
                    new[] { 2, 1, 0 },
                    new[] { 1, 9, 5 },
                    new[] { 30, 24, 34 },
                    new[] { 24, 14, 17 },
                    new[] { 34, 32, 38 },
                    new[] { 32, 21, 28 },
                    new[] { 38, 37, 39 },
                    new[] { 37, 31, 33 },
                    new[] { 39, 35, 36 },
                    new[] { 35, 29, 26 },
                    new[] { 36, 27, 30 },
                    new[] { 27, 20, 16 },
                    new[] { 23, 31, 28, 19 },
                    new[] { 19, 28, 21, 13 },
                    new[] { 13, 21, 17, 9 },
                    new[] { 9, 17, 14, 5 },
                    new[] { 5, 14, 16, 7 },
                    new[] { 7, 16, 20, 11 },
                    new[] { 11, 20, 26, 18 },
                    new[] { 18, 26, 29, 22 },
                    new[] { 22, 29, 33, 25 },
                    new[] { 25, 33, 31, 23 },
                    new[] { 0, 4, 8, 6, 2 },
                    new[] { 0, 1, 5, 7, 3 },
                    new[] { 4, 3, 11, 18, 12 },
                    new[] { 8, 12, 22, 25, 15 },
                    new[] { 6, 15, 23, 19, 10 },
                    new[] { 2, 10, 13, 9, 1 },
                    new[] { 30, 34, 38, 39, 36 },
                    new[] { 30, 27, 16, 14, 24 },
                    new[] { 34, 24, 17, 21, 32 },
                    new[] { 38, 32, 28, 31, 37 },
                    new[] { 39, 37, 33, 29, 35 },
                    new[] { 36, 35, 26, 20, 27 }
                };
                var verts = new[]
                {
                    new Vector3(-1.094229f, 0.091579f, -0.183298f),
                    new Vector3(-0.983491f, -0.29488f, 0.177777f),
                    new Vector3(-0.97462f, -0.408146f, -0.350504f),
                    new Vector3(-0.882171f, 0.554599f, -0.002654f),
                    new Vector3(-0.873301f, 0.441333f, -0.530935f),
                    new Vector3(-0.702994f, -0.070704f, 0.581578f),
                    new Vector3(-0.679771f, -0.367238f, -0.801479f),
                    new Vector3(-0.640375f, 0.454303f, 0.470066f),
                    new Vector3(-0.617152f, 0.157769f, -0.912991f),
                    new Vector3(-0.583386f, -0.570429f, 0.414373f),
                    new Vector3(-0.569033f, -0.753697f, -0.440404f),
                    new Vector3(-0.419447f, 0.804058f, 0.122429f),
                    new Vector3(-0.405095f, 0.620789f, -0.732347f),
                    new Vector3(-0.327237f, -0.853993f, 0.032316f),
                    new Vector3(-0.251159f, -0.061675f, 0.877809f),
                    new Vector3(-0.211565f, -0.187782f, -1.002892f),
                    new Vector3(-0.18854f, 0.463332f, 0.766296f),
                    new Vector3(-0.131551f, -0.5614f, 0.710603f),
                    new Vector3(-0.124598f, 0.844965f, -0.328546f),
                    new Vector3(-0.032387f, -0.813085f, -0.41866f),
                    new Vector3(0.032387f, 0.813086f, 0.418659f),
                    new Vector3(0.124598f, -0.844964f, 0.328546f),
                    new Vector3(0.131551f, 0.561401f, -0.710603f),
                    new Vector3(0.18854f, -0.463331f, -0.766296f),
                    new Vector3(0.237051f, -0.270491f, 0.977985f),
                    new Vector3(0.251159f, 0.061676f, -0.877809f),
                    new Vector3(0.327237f, 0.853994f, -0.032316f),
                    new Vector3(0.33837f, 0.578988f, 0.797554f),
                    new Vector3(0.419448f, -0.804057f, -0.122429f),
                    new Vector3(0.583386f, 0.570429f, -0.414373f),
                    new Vector3(0.601401f, 0.125461f, 0.928384f),
                    new Vector3(0.640375f, -0.454302f, -0.470066f),
                    new Vector3(0.651509f, -0.729308f, 0.359803f),
                    new Vector3(0.702994f, 0.070705f, -0.581579f),
                    new Vector3(0.721009f, -0.374264f, 0.761179f),
                    new Vector3(0.815447f, 0.645178f, 0.06786f),
                    new Vector3(0.822328f, 0.475215f, 0.580748f),
                    new Vector3(1.008977f, -0.163393f, -0.202684f),
                    new Vector3(1.015858f, -0.333356f, 0.310203f),
                    new Vector3(1.078477f, 0.191651f, 0.198691f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            43, () =>
            {
                var faces = new[]
                {
                    new[] { 0, 2, 1 },
                    new[] { 2, 7, 10 },
                    new[] { 1, 8, 5 },
                    new[] { 8, 13, 19 },
                    new[] { 5, 14, 6 },
                    new[] { 14, 22, 24 },
                    new[] { 6, 11, 3 },
                    new[] { 11, 21, 16 },
                    new[] { 3, 4, 0 },
                    new[] { 4, 12, 9 },
                    new[] { 33, 25, 34 },
                    new[] { 25, 15, 17 },
                    new[] { 34, 31, 38 },
                    new[] { 31, 20, 26 },
                    new[] { 38, 37, 39 },
                    new[] { 37, 29, 32 },
                    new[] { 39, 35, 36 },
                    new[] { 35, 30, 27 },
                    new[] { 36, 28, 33 },
                    new[] { 28, 23, 18 },
                    new[] { 24, 32, 29, 21 },
                    new[] { 21, 29, 26, 16 },
                    new[] { 16, 26, 20, 12 },
                    new[] { 12, 20, 17, 9 },
                    new[] { 9, 17, 15, 7 },
                    new[] { 7, 15, 18, 10 },
                    new[] { 10, 18, 23, 13 },
                    new[] { 13, 23, 27, 19 },
                    new[] { 19, 27, 30, 22 },
                    new[] { 22, 30, 32, 24 },
                    new[] { 0, 1, 5, 6, 3 },
                    new[] { 0, 4, 9, 7, 2 },
                    new[] { 1, 2, 10, 13, 8 },
                    new[] { 5, 8, 19, 22, 14 },
                    new[] { 6, 14, 24, 21, 11 },
                    new[] { 3, 11, 16, 12, 4 },
                    new[] { 33, 34, 38, 39, 36 },
                    new[] { 33, 28, 18, 15, 25 },
                    new[] { 34, 25, 17, 20, 31 },
                    new[] { 38, 31, 26, 29, 37 },
                    new[] { 39, 37, 32, 30, 35 },
                    new[] { 36, 35, 27, 23, 28 }
                };
                var verts = new[]
                {
                    new Vector3(-1.099924f, -0.170755f, -0.018241f),
                    new Vector3(-1.015744f, 0.184543f, -0.41657f),
                    new Vector3(-0.979069f, 0.342745f, 0.098809f),
                    new Vector3(-0.891012f, -0.645186f, -0.17075f),
                    new Vector3(-0.854337f, -0.486985f, 0.344629f),
                    new Vector3(-0.754806f, -0.070303f, -0.81526f),
                    new Vector3(-0.677717f, -0.583103f, -0.663335f),
                    new Vector3(-0.65879f, 0.343875f, 0.53402f),
                    new Vector3(-0.633951f, 0.443197f, -0.698209f),
                    new Vector3(-0.581702f, -0.168926f, 0.685945f),
                    new Vector3(-0.57461f, 0.699173f, 0.135692f),
                    new Vector3(-0.43213f, -0.899333f, -0.300465f),
                    new Vector3(-0.37279f, -0.643357f, 0.533436f),
                    new Vector3(-0.361315f, 0.761256f, -0.356893f),
                    new Vector3(-0.295924f, -0.324449f, -0.944974f),
                    new Vector3(-0.185624f, 0.480819f, 0.756166f),
                    new Vector3(-0.111851f, -0.898203f, 0.134746f),
                    new Vector3(-0.108536f, -0.031982f, 0.908091f),
                    new Vector3(-0.101444f, 0.836117f, 0.357838f),
                    new Vector3(-0.100377f, 0.50641f, -0.755583f),
                    new Vector3(0.100376f, -0.506413f, 0.755582f),
                    new Vector3(0.101443f, -0.83612f, -0.357838f),
                    new Vector3(0.108535f, 0.031979f, -0.908092f),
                    new Vector3(0.111851f, 0.8982f, -0.134747f),
                    new Vector3(0.185623f, -0.480822f, -0.756167f),
                    new Vector3(0.295923f, 0.324447f, 0.944974f),
                    new Vector3(0.361314f, -0.761258f, 0.356892f),
                    new Vector3(0.372789f, 0.643354f, -0.533437f),
                    new Vector3(0.43213f, 0.899331f, 0.300464f),
                    new Vector3(0.574609f, -0.699176f, -0.135692f),
                    new Vector3(0.581701f, 0.168923f, -0.685946f),
                    new Vector3(0.63395f, -0.443199f, 0.698208f),
                    new Vector3(0.658789f, -0.343878f, -0.534021f),
                    new Vector3(0.677716f, 0.583101f, 0.663334f),
                    new Vector3(0.754804f, 0.0703f, 0.815259f),
                    new Vector3(0.854336f, 0.486982f, -0.344629f),
                    new Vector3(0.891011f, 0.645184f, 0.17075f),
                    new Vector3(0.979068f, -0.342747f, -0.09881f),
                    new Vector3(1.015743f, -0.184545f, 0.416569f),
                    new Vector3(1.099923f, 0.170753f, 0.018241f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            44, () =>
            {
                var faces = new[]
                {
                    new[] { 11, 14, 6 },
                    new[] { 14, 17, 12 },
                    new[] { 6, 4, 0 },
                    new[] { 11, 5, 13 },
                    new[] { 9, 10, 3 },
                    new[] { 10, 16, 8 },
                    new[] { 3, 2, 1 },
                    new[] { 9, 7, 15 },
                    new[] { 4, 1, 0 },
                    new[] { 0, 1, 2 },
                    new[] { 0, 2, 5 },
                    new[] { 5, 2, 8 },
                    new[] { 5, 8, 13 },
                    new[] { 13, 8, 16 },
                    new[] { 13, 16, 17 },
                    new[] { 17, 16, 15 },
                    new[] { 17, 15, 12 },
                    new[] { 12, 15, 7 },
                    new[] { 12, 7, 4 },
                    new[] { 4, 7, 1 },
                    new[] { 14, 11, 13, 17 },
                    new[] { 6, 14, 12, 4 },
                    new[] { 11, 6, 0, 5 },
                    new[] { 10, 9, 15, 16 },
                    new[] { 3, 10, 8, 2 },
                    new[] { 9, 3, 1, 7 }
                };
                var verts = new[]
                {
                    new Vector3(-0.789003f, 0.385273f, -0.254111f),
                    new Vector3(-0.772339f, -0.452189f, -0.185879f),
                    new Vector3(-0.761383f, 0.026005f, 0.505125f),
                    new Vector3(-0.611639f, -0.798949f, 0.562592f),
                    new Vector3(-0.381598f, -0.074991f, -0.82722f),
                    new Vector3(-0.362623f, 0.753266f, 0.369634f),
                    new Vector3(-0.289802f, 0.760316f, -0.816621f),
                    new Vector3(-0.055737f, -0.771195f, -0.487529f),
                    new Vector3(-0.033826f, 0.185193f, 0.894479f),
                    new Vector3(0.104963f, -1.117956f, 0.260942f),
                    new Vector3(0.115918f, -0.639761f, 0.951946f),
                    new Vector3(0.136578f, 1.128308f, -0.192876f),
                    new Vector3(0.452187f, -0.167262f, -0.776585f),
                    new Vector3(0.471162f, 0.660994f, 0.420269f),
                    new Vector3(0.543983f, 0.668044f, -0.765985f),
                    new Vector3(0.67182f, -0.612007f, -0.098176f),
                    new Vector3(0.682775f, -0.133813f, 0.592828f),
                    new Vector3(0.878567f, 0.200731f, -0.15284f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            45, () =>
            {
                var faces = new[]
                {
                    new[] { 13, 16, 20 },
                    new[] { 9, 18, 12 },
                    new[] { 2, 4, 0 },
                    new[] { 5, 1, 8 },
                    new[] { 14, 15, 7 },
                    new[] { 10, 3, 6 },
                    new[] { 17, 11, 19 },
                    new[] { 21, 23, 22 },
                    new[] { 12, 11, 4 },
                    new[] { 4, 11, 6 },
                    new[] { 4, 6, 0 },
                    new[] { 0, 6, 3 },
                    new[] { 0, 3, 1 },
                    new[] { 1, 3, 7 },
                    new[] { 1, 7, 8 },
                    new[] { 8, 7, 15 },
                    new[] { 8, 15, 16 },
                    new[] { 16, 15, 22 },
                    new[] { 16, 22, 20 },
                    new[] { 20, 22, 23 },
                    new[] { 20, 23, 18 },
                    new[] { 18, 23, 19 },
                    new[] { 18, 19, 12 },
                    new[] { 12, 19, 11 },
                    new[] { 5, 13, 9, 2 },
                    new[] { 13, 5, 8, 16 },
                    new[] { 9, 13, 20, 18 },
                    new[] { 2, 9, 12, 4 },
                    new[] { 5, 2, 0, 1 },
                    new[] { 21, 14, 10, 17 },
                    new[] { 14, 21, 22, 15 },
                    new[] { 10, 14, 7, 3 },
                    new[] { 17, 10, 6, 11 },
                    new[] { 21, 17, 19, 23 }
                };
                var verts = new[]
                {
                    new Vector3(-0.984615f, -0.215433f, 0.042813f),
                    new Vector3(-0.835086f, 0.417027f, 0.382665f),
                    new Vector3(-0.776626f, 0.078669f, -0.596021f),
                    new Vector3(-0.681291f, -0.220722f, 0.710519f),
                    new Vector3(-0.642753f, -0.628915f, -0.457214f),
                    new Vector3(-0.627097f, 0.71113f, -0.256169f),
                    new Vector3(-0.577202f, -0.786781f, 0.255979f),
                    new Vector3(-0.300907f, 0.381853f, 0.883943f),
                    new Vector3(-0.281757f, 0.897979f, 0.36326f),
                    new Vector3(-0.143631f, 0.126378f, -0.963315f),
                    new Vector3(-0.068623f, -0.621064f, 0.757726f),
                    new Vector3(-0.049613f, -0.984736f, -0.213412f),
                    new Vector3(-0.009758f, -0.581206f, -0.824509f),
                    new Vector3(0.005899f, 0.758839f, -0.623463f),
                    new Vector3(0.311761f, -0.018489f, 0.931151f),
                    new Vector3(0.341127f, 0.667962f, 0.674663f),
                    new Vector3(0.351238f, 0.945688f, -0.004035f),
                    new Vector3(0.458966f, -0.819019f, 0.288335f),
                    new Vector3(0.54357f, -0.100253f, -0.843914f),
                    new Vector3(0.592421f, -0.698626f, -0.422693f),
                    new Vector3(0.6931f, 0.532207f, -0.504062f),
                    new Vector3(0.83935f, -0.216444f, 0.461759f),
                    new Vector3(0.868716f, 0.470008f, 0.205272f),
                    new Vector3(0.972805f, -0.096052f, -0.249268f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            46, () =>
            {
                var faces = new[]
                {
                    new[] { 9, 11, 18 },
                    new[] { 16, 23, 25 },
                    new[] { 10, 19, 12 },
                    new[] { 3, 6, 1 },
                    new[] { 2, 0, 4 },
                    new[] { 17, 13, 7 },
                    new[] { 15, 5, 8 },
                    new[] { 21, 14, 22 },
                    new[] { 26, 28, 29 },
                    new[] { 24, 27, 20 },
                    new[] { 19, 22, 12 },
                    new[] { 12, 22, 14 },
                    new[] { 12, 14, 6 },
                    new[] { 6, 14, 8 },
                    new[] { 6, 8, 1 },
                    new[] { 1, 8, 5 },
                    new[] { 1, 5, 0 },
                    new[] { 0, 5, 7 },
                    new[] { 0, 7, 4 },
                    new[] { 4, 7, 13 },
                    new[] { 4, 13, 11 },
                    new[] { 11, 13, 20 },
                    new[] { 11, 20, 18 },
                    new[] { 18, 20, 27 },
                    new[] { 18, 27, 23 },
                    new[] { 23, 27, 29 },
                    new[] { 23, 29, 25 },
                    new[] { 25, 29, 28 },
                    new[] { 25, 28, 19 },
                    new[] { 19, 28, 22 },
                    new[] { 9, 2, 4, 11 },
                    new[] { 16, 9, 18, 23 },
                    new[] { 10, 16, 25, 19 },
                    new[] { 3, 10, 12, 6 },
                    new[] { 2, 3, 1, 0 },
                    new[] { 17, 24, 20, 13 },
                    new[] { 15, 17, 7, 5 },
                    new[] { 21, 15, 8, 14 },
                    new[] { 26, 21, 22, 28 },
                    new[] { 24, 26, 29, 27 },
                    new[] { 2, 9, 16, 10, 3 },
                    new[] { 24, 17, 15, 21, 26 }
                };
                var verts = new[]
                {
                    new Vector3(-0.962816f, 0.187793f, 0.445444f),
                    new Vector3(-0.942301f, -0.435696f, 0.287991f),
                    new Vector3(-0.736715f, 0.34226f, -0.136766f),
                    new Vector3(-0.7162f, -0.281228f, -0.294218f),
                    new Vector3(-0.680334f, 0.758077f, 0.35095f),
                    new Vector3(-0.653736f, -0.229833f, 0.824928f),
                    new Vector3(-0.626624f, -0.874236f, -0.061265f),
                    new Vector3(-0.516012f, 0.397771f, 0.858028f),
                    new Vector3(-0.47699f, -0.788175f, 0.558536f),
                    new Vector3(-0.259134f, 0.641512f, -0.447112f),
                    new Vector3(-0.22594f, -0.367313f, -0.701875f),
                    new Vector3(-0.202753f, 1.057329f, 0.040605f),
                    new Vector3(-0.136364f, -0.960322f, -0.468922f),
                    new Vector3(-0.116423f, 0.854913f, 0.645191f),
                    new Vector3(-0.053283f, -1.063987f, 0.160603f),
                    new Vector3(-0.029965f, -0.362198f, 0.739264f),
                    new Vector3(0.056543f, 0.202971f, -0.796368f),
                    new Vector3(0.107759f, 0.265405f, 0.772363f),
                    new Vector3(0.287508f, 0.971244f, -0.367052f),
                    new Vector3(0.341218f, -0.66107f, -0.779268f),
                    new Vector3(0.392401f, 0.966981f, 0.267715f),
                    new Vector3(0.393741f, -0.638011f, 0.341331f),
                    new Vector3(0.455541f, -0.951919f, -0.216873f),
                    new Vector3(0.603185f, 0.532703f, -0.716309f),
                    new Vector3(0.616584f, 0.377473f, 0.394887f),
                    new Vector3(0.6237f, -0.090786f, -0.873761f),
                    new Vector3(0.79333f, -0.180868f, 0.128495f),
                    new Vector3(0.816108f, 0.691169f, -0.130218f),
                    new Vector3(0.85513f, -0.494777f, -0.42971f),
                    new Vector3(0.992854f, 0.132827f, -0.396611f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            47, () =>
            {
                var faces = new[]
                {
                    new[] { 8, 12, 18 },
                    new[] { 13, 21, 20 },
                    new[] { 7, 16, 11 },
                    new[] { 2, 4, 0 },
                    new[] { 3, 1, 5 },
                    new[] { 30, 19, 24 },
                    new[] { 19, 15, 10 },
                    new[] { 24, 17, 27 },
                    new[] { 17, 6, 9 },
                    new[] { 27, 25, 32 },
                    new[] { 25, 14, 22 },
                    new[] { 32, 33, 34 },
                    new[] { 33, 26, 29 },
                    new[] { 34, 31, 30 },
                    new[] { 31, 28, 23 },
                    new[] { 16, 22, 11 },
                    new[] { 11, 22, 14 },
                    new[] { 11, 14, 4 },
                    new[] { 4, 14, 9 },
                    new[] { 4, 9, 0 },
                    new[] { 0, 9, 6 },
                    new[] { 0, 6, 1 },
                    new[] { 1, 6, 10 },
                    new[] { 1, 10, 5 },
                    new[] { 5, 10, 15 },
                    new[] { 5, 15, 12 },
                    new[] { 12, 15, 23 },
                    new[] { 12, 23, 18 },
                    new[] { 18, 23, 28 },
                    new[] { 18, 28, 21 },
                    new[] { 21, 28, 29 },
                    new[] { 21, 29, 20 },
                    new[] { 20, 29, 26 },
                    new[] { 20, 26, 16 },
                    new[] { 16, 26, 22 },
                    new[] { 8, 3, 5, 12 },
                    new[] { 13, 8, 18, 21 },
                    new[] { 7, 13, 20, 16 },
                    new[] { 2, 7, 11, 4 },
                    new[] { 3, 2, 0, 1 },
                    new[] { 3, 8, 13, 7, 2 },
                    new[] { 30, 24, 27, 32, 34 },
                    new[] { 30, 31, 23, 15, 19 },
                    new[] { 24, 19, 10, 6, 17 },
                    new[] { 27, 17, 9, 14, 25 },
                    new[] { 32, 25, 22, 26, 33 },
                    new[] { 34, 33, 29, 28, 31 }
                };
                var verts = new[]
                {
                    new Vector3(-0.908535f, -0.523787f, -0.144699f),
                    new Vector3(-0.894854f, -0.367641f, 0.429886f),
                    new Vector3(-0.853258f, 0.048135f, -0.301436f),
                    new Vector3(-0.839577f, 0.20428f, 0.273149f),
                    new Vector3(-0.708763f, -0.381742f, -0.687498f),
                    new Vector3(-0.672946f, 0.027052f, 0.816785f),
                    new Vector3(-0.520247f, -0.782937f, 0.225153f),
                    new Vector3(-0.51634f, 0.434114f, -0.605119f),
                    new Vector3(-0.494204f, 0.686763f, 0.324579f),
                    new Vector3(-0.422413f, -0.79035f, -0.362291f),
                    new Vector3(-0.396391f, -0.493344f, 0.730635f),
                    new Vector3(-0.371845f, 0.004237f, -0.991181f),
                    new Vector3(-0.327573f, 0.509534f, 0.868216f),
                    new Vector3(-0.294432f, 0.828807f, -0.218221f),
                    new Vector3(-0.140258f, -0.512752f, -0.807313f),
                    new Vector3(-0.098153f, -0.032185f, 0.961078f),
                    new Vector3(-0.026471f, 0.486719f, -0.939751f),
                    new Vector3(-0.010949f, -1.005224f, 0.010862f),
                    new Vector3(0.009345f, 0.895513f, 0.564533f),
                    new Vector3(0.189455f, -0.536653f, 0.828749f),
                    new Vector3(0.195436f, 0.881412f, -0.552852f),
                    new Vector3(0.209117f, 1.037558f, 0.021734f),
                    new Vector3(0.218444f, -0.056175f, -0.93993f),
                    new Vector3(0.260549f, 0.424392f, 0.828462f),
                    new Vector3(0.427671f, -0.853012f, 0.383905f),
                    new Vector3(0.445587f, -0.556061f, -0.709199f),
                    new Vector3(0.516681f, 0.404984f, -0.709486f),
                    new Vector3(0.525505f, -0.860425f, -0.203539f),
                    new Vector3(0.542703f, 0.70199f, 0.38344f),
                    new Vector3(0.640537f, 0.694577f, -0.204005f),
                    new Vector3(0.725908f, -0.391854f, 0.614349f),
                    new Vector3(0.769847f, 0.202104f, 0.614171f),
                    new Vector3(0.884207f, -0.403849f, -0.336156f),
                    new Vector3(0.928145f, 0.190109f, -0.336334f),
                    new Vector3(1.008063f, -0.114255f, 0.169326f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            48, () =>
            {
                var faces = new[]
                {
                    new[] { 0, 5, 1 },
                    new[] { 5, 10, 11 },
                    new[] { 1, 7, 3 },
                    new[] { 7, 13, 15 },
                    new[] { 3, 9, 4 },
                    new[] { 9, 17, 19 },
                    new[] { 4, 8, 2 },
                    new[] { 8, 18, 16 },
                    new[] { 2, 6, 0 },
                    new[] { 6, 14, 12 },
                    new[] { 35, 31, 37 },
                    new[] { 31, 21, 23 },
                    new[] { 37, 33, 39 },
                    new[] { 33, 25, 27 },
                    new[] { 39, 34, 38 },
                    new[] { 34, 29, 28 },
                    new[] { 38, 32, 36 },
                    new[] { 32, 26, 24 },
                    new[] { 36, 30, 35 },
                    new[] { 30, 22, 20 },
                    new[] { 17, 28, 19 },
                    new[] { 19, 28, 29 },
                    new[] { 19, 29, 18 },
                    new[] { 18, 29, 27 },
                    new[] { 18, 27, 16 },
                    new[] { 16, 27, 25 },
                    new[] { 16, 25, 14 },
                    new[] { 14, 25, 23 },
                    new[] { 14, 23, 12 },
                    new[] { 12, 23, 21 },
                    new[] { 12, 21, 10 },
                    new[] { 10, 21, 20 },
                    new[] { 10, 20, 11 },
                    new[] { 11, 20, 22 },
                    new[] { 11, 22, 13 },
                    new[] { 13, 22, 24 },
                    new[] { 13, 24, 15 },
                    new[] { 15, 24, 26 },
                    new[] { 15, 26, 17 },
                    new[] { 17, 26, 28 },
                    new[] { 0, 1, 3, 4, 2 },
                    new[] { 0, 6, 12, 10, 5 },
                    new[] { 1, 5, 11, 13, 7 },
                    new[] { 3, 7, 15, 17, 9 },
                    new[] { 4, 9, 19, 18, 8 },
                    new[] { 2, 8, 16, 14, 6 },
                    new[] { 35, 37, 39, 38, 36 },
                    new[] { 35, 30, 20, 21, 31 },
                    new[] { 37, 31, 23, 25, 33 },
                    new[] { 39, 33, 27, 29, 34 },
                    new[] { 38, 34, 28, 26, 32 },
                    new[] { 36, 32, 24, 22, 30 }
                };
                var verts = new[]
                {
                    new Vector3(-1.023844f, 0.34935f, 0.211966f),
                    new Vector3(-1.02284f, 0.245289f, -0.329944f),
                    new Vector3(-0.984402f, -0.132386f, 0.478181f),
                    new Vector3(-0.982778f, -0.300762f, -0.398647f),
                    new Vector3(-0.959023f, -0.534178f, 0.100801f),
                    new Vector3(-0.762835f, 0.690856f, -0.134076f),
                    new Vector3(-0.724398f, 0.313181f, 0.674049f),
                    new Vector3(-0.72177f, 0.040743f, -0.74469f),
                    new Vector3(-0.659577f, -0.570348f, 0.562884f),
                    new Vector3(-0.657953f, -0.738723f, -0.313945f),
                    new Vector3(-0.302078f, 0.865747f, 0.11414f),
                    new Vector3(-0.301074f, 0.761686f, -0.427769f),
                    new Vector3(-0.278322f, 0.632332f, 0.613589f),
                    new Vector3(-0.275694f, 0.359894f, -0.805149f),
                    new Vector3(-0.238881f, 0.150594f, 0.879804f),
                    new Vector3(-0.235633f, -0.186157f, -0.873853f),
                    new Vector3(-0.198819f, -0.395456f, 0.8111f),
                    new Vector3(-0.196192f, -0.667893f, -0.607638f),
                    new Vector3(-0.173439f, -0.797247f, 0.43372f),
                    new Vector3(-0.172436f, -0.901309f, -0.10819f),
                    new Vector3(0.169627f, 0.892069f, -0.170988f),
                    new Vector3(0.181588f, 0.824063f, 0.376487f),
                    new Vector3(0.183498f, 0.626126f, -0.654287f),
                    new Vector3(0.214813f, 0.448086f, 0.77902f),
                    new Vector3(0.217902f, 0.127816f, -0.888807f),
                    new Vector3(0.25661f, -0.092255f, 0.882858f),
                    new Vector3(0.259699f, -0.412524f, -0.784969f),
                    new Vector3(0.291015f, -0.590564f, 0.648338f),
                    new Vector3(0.292924f, -0.788502f, -0.382436f),
                    new Vector3(0.304885f, -0.856507f, 0.165038f),
                    new Vector3(0.65102f, 0.715912f, -0.375257f),
                    new Vector3(0.670374f, 0.605876f, 0.510575f),
                    new Vector3(0.706688f, -0.090371f, -0.754719f),
                    new Vector3(0.738003f, -0.268411f, 0.678589f),
                    new Vector3(0.760446f, -0.698716f, -0.103406f),
                    new Vector3(0.960498f, 0.539035f, 0.045972f),
                    new Vector3(0.974369f, 0.273093f, -0.437327f),
                    new Vector3(0.993723f, 0.163058f, 0.448505f),
                    new Vector3(1.016166f, -0.267247f, -0.333489f),
                    new Vector3(1.028128f, -0.335253f, 0.213985f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            49, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 3, 2 },
                    new[] { 3, 0, 2 },
                    new[] { 0, 3, 4 },
                    new[] { 2, 5, 6 },
                    new[] { 1, 0, 4 },
                    new[] { 6, 4, 3 },
                    new[] { 2, 0, 1, 5 },
                    new[] { 5, 1, 4, 6 }
                };
                var verts = new[]
                {
                    new Vector3(-0.87547f, -0.255205f, -0.086794f),
                    new Vector3(-0.276612f, -0.313401f, 1.029989f),
                    new Vector3(-0.236035f, 0.801921f, -0.374595f),
                    new Vector3(-0.051128f, -0.255205f, -1.050994f),
                    new Vector3(0.218493f, -0.889481f, 0.014004f),
                    new Vector3(0.362823f, 0.743725f, 0.742188f),
                    new Vector3(0.857929f, 0.167645f, -0.273797f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            50, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 0, 1 },
                    new[] { 1, 0, 2 },
                    new[] { 1, 2, 5 },
                    new[] { 4, 0, 3 },
                    new[] { 2, 6, 5 },
                    new[] { 4, 3, 7 },
                    new[] { 4, 7, 6 },
                    new[] { 6, 7, 5 },
                    new[] { 7, 3, 5 },
                    new[] { 1, 5, 3 },
                    new[] { 0, 4, 6, 2 }
                };
                var verts = new[]
                {
                    new Vector3(-0.878027f, -0.44614f, 0.176652f),
                    new Vector3(-0.85656f, 0.548188f, -0.533f),
                    new Vector3(-0.47761f, 0.616903f, 0.626496f),
                    new Vector3(-0.069889f, -0.364329f, -0.736024f),
                    new Vector3(0.239836f, -0.921955f, 0.306031f),
                    new Vector3(0.330528f, 0.698715f, -0.286179f),
                    new Vector3(0.640253f, 0.141088f, 0.755876f),
                    new Vector3(1.071468f, -0.272471f, -0.309853f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            51, () =>
            {
                var faces = new[]
                {
                    new[] { 1, 5, 0 },
                    new[] { 8, 4, 5 },
                    new[] { 4, 0, 5 },
                    new[] { 4, 3, 0 },
                    new[] { 8, 3, 4 },
                    new[] { 5, 6, 8 },
                    new[] { 8, 6, 7 },
                    new[] { 8, 7, 3 },
                    new[] { 3, 7, 2 },
                    new[] { 7, 6, 2 },
                    new[] { 3, 2, 0 },
                    new[] { 0, 2, 1 },
                    new[] { 2, 6, 1 },
                    new[] { 1, 6, 5 }
                };
                var verts = new[]
                {
                    new Vector3(-0.837735f, -0.140456f, -0.298855f),
                    new Vector3(-0.67808f, 0.951266f, 0.116678f),
                    new Vector3(-0.424767f, 0.019903f, 0.793738f),
                    new Vector3(-0.041529f, -0.887587f, 0.145967f),
                    new Vector3(-0.017092f, -0.613922f, -1.000561f),
                    new Vector3(0.031619f, 0.531638f, -0.726088f),
                    new Vector3(0.444587f, 0.691997f, 0.366504f),
                    new Vector3(0.695172f, -0.337344f, 0.883883f),
                    new Vector3(0.827825f, -0.215493f, -0.281266f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            52, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 8, 4 },
                    new[] { 3, 9, 8 },
                    new[] { 4, 8, 10 },
                    new[] { 10, 8, 9 },
                    new[] { 1, 0, 5, 6 },
                    new[] { 3, 1, 6, 9 },
                    new[] { 2, 4, 10, 7 },
                    new[] { 0, 2, 7, 5 },
                    new[] { 0, 1, 3, 4, 2 },
                    new[] { 7, 10, 9, 6, 5 }
                };
                var verts = new[]
                {
                    new Vector3(-0.81481f, 0.221521f, -0.662951f),
                    new Vector3(-0.660297f, -0.683519f, -0.326712f),
                    new Vector3(-0.53073f, 0.852939f, 0.027439f),
                    new Vector3(-0.280723f, -0.611446f, 0.571485f),
                    new Vector3(-0.200646f, 0.338137f, 0.790362f),
                    new Vector3(0.085423f, 0.233517f, -1.044348f),
                    new Vector3(0.239936f, -0.671523f, -0.708109f),
                    new Vector3(0.369503f, 0.864935f, -0.353959f),
                    new Vector3(0.473247f, -0.295241f, 1.107742f),
                    new Vector3(0.619511f, -0.59945f, 0.190087f),
                    new Vector3(0.699587f, 0.350133f, 0.408964f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            53, () =>
            {
                var faces = new[]
                {
                    new[] { 2, 3, 0 },
                    new[] { 2, 9, 3 },
                    new[] { 0, 3, 7 },
                    new[] { 7, 3, 9 },
                    new[] { 1, 6, 4 },
                    new[] { 1, 8, 6 },
                    new[] { 4, 6, 10 },
                    new[] { 10, 6, 8 },
                    new[] { 5, 4, 10, 11 },
                    new[] { 2, 5, 11, 9 },
                    new[] { 1, 0, 7, 8 },
                    new[] { 4, 5, 2, 0, 1 },
                    new[] { 8, 7, 9, 11, 10 }
                };
                var verts = new[]
                {
                    new Vector3(-0.736376f, 0.261231f, -0.409511f),
                    new Vector3(-0.572247f, -0.640818f, -0.200191f),
                    new Vector3(-0.430826f, 0.786388f, 0.30833f),
                    new Vector3(-0.352398f, 1.103933f, -0.573408f),
                    new Vector3(-0.16526f, -0.673158f, 0.647018f),
                    new Vector3(-0.077857f, 0.208904f, 0.961301f),
                    new Vector3(0.083202f, -1.290119f, -0.017868f),
                    new Vector3(0.11049f, 0.32145f, -0.814035f),
                    new Vector3(0.274618f, -0.5806f, -0.604714f),
                    new Vector3(0.416039f, 0.846607f, -0.096194f),
                    new Vector3(0.681606f, -0.61294f, 0.242494f),
                    new Vector3(0.769009f, 0.269122f, 0.556777f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            54, () =>
            {
                var faces = new[]
                {
                    new[] { 5, 10, 9 },
                    new[] { 5, 8, 10 },
                    new[] { 9, 10, 12 },
                    new[] { 12, 10, 8 },
                    new[] { 1, 0, 2, 4 },
                    new[] { 5, 1, 4, 8 },
                    new[] { 7, 9, 12, 11 },
                    new[] { 3, 7, 11, 6 },
                    new[] { 0, 3, 6, 2 },
                    new[] { 0, 1, 5, 9, 7, 3 },
                    new[] { 11, 12, 8, 4, 2, 6 }
                };
                var verts = new[]
                {
                    new Vector3(-0.973522f, 0.38842f, -0.100967f),
                    new Vector3(-0.837708f, -0.464036f, -0.18462f),
                    new Vector3(-0.457167f, 0.537479f, -0.781617f),
                    new Vector3(-0.449574f, 0.863826f, 0.400622f),
                    new Vector3(-0.321353f, -0.314977f, -0.865269f),
                    new Vector3(-0.177946f, -0.841086f, 0.233318f),
                    new Vector3(0.066781f, 1.012885f, -0.280028f),
                    new Vector3(0.210188f, 0.486776f, 0.818559f),
                    new Vector3(0.338409f, -0.692027f, -0.447332f),
                    new Vector3(0.346002f, -0.365679f, 0.734907f),
                    new Vector3(0.666998f, -1.030797f, 0.280259f),
                    new Vector3(0.726543f, 0.635835f, 0.13791f),
                    new Vector3(0.862357f, -0.216621f, 0.054258f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            55, () =>
            {
                var faces = new[]
                {
                    new[] { 12, 13, 11 },
                    new[] { 12, 10, 13 },
                    new[] { 11, 13, 8 },
                    new[] { 8, 13, 10 },
                    new[] { 3, 0, 5 },
                    new[] { 3, 1, 0 },
                    new[] { 5, 0, 2 },
                    new[] { 2, 0, 1 },
                    new[] { 9, 5, 2, 7 },
                    new[] { 12, 9, 7, 10 },
                    new[] { 6, 11, 8, 4 },
                    new[] { 3, 6, 4, 1 },
                    new[] { 5, 9, 12, 11, 6, 3 },
                    new[] { 4, 8, 10, 7, 2, 1 }
                };
                var verts = new[]
                {
                    new Vector3(-1.129559f, 0.266324f, 0.624401f),
                    new Vector3(-0.925111f, 0.008507f, -0.145991f),
                    new Vector3(-0.655368f, -0.416841f, 0.523404f),
                    new Vector3(-0.5883f, 0.71007f, 0.164074f),
                    new Vector3(-0.438148f, 0.074567f, -0.824427f),
                    new Vector3(-0.318557f, 0.284721f, 0.83347f),
                    new Vector3(-0.101337f, 0.77613f, -0.514362f),
                    new Vector3(0.101337f, -0.77613f, 0.514363f),
                    new Vector3(0.318557f, -0.284721f, -0.833468f),
                    new Vector3(0.438148f, -0.074567f, 0.824429f),
                    new Vector3(0.5883f, -0.710069f, -0.164073f),
                    new Vector3(0.655368f, 0.416842f, -0.523403f),
                    new Vector3(0.925111f, -0.008507f, 0.145993f),
                    new Vector3(1.129559f, -0.266324f, -0.624399f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            56, () =>
            {
                var faces = new[]
                {
                    new[] { 3, 0, 1 },
                    new[] { 3, 4, 0 },
                    new[] { 1, 0, 2 },
                    new[] { 2, 0, 4 },
                    new[] { 5, 7, 10 },
                    new[] { 5, 6, 7 },
                    new[] { 10, 7, 11 },
                    new[] { 11, 7, 6 },
                    new[] { 8, 12, 13, 9 },
                    new[] { 3, 8, 9, 4 },
                    new[] { 5, 1, 2, 6 },
                    new[] { 12, 10, 11, 13 },
                    new[] { 12, 8, 3, 1, 5, 10 },
                    new[] { 6, 2, 4, 9, 13, 11 }
                };
                var verts = new[]
                {
                    new Vector3(-1.111755f, 0.435562f, -0.458586f),
                    new Vector3(-0.808867f, 0.159752f, 0.27642f),
                    new Vector3(-0.700013f, -0.297342f, -0.421621f),
                    new Vector3(-0.454926f, 0.822531f, -0.102389f),
                    new Vector3(-0.346072f, 0.365437f, -0.80043f),
                    new Vector3(-0.331474f, -0.383478f, 0.706586f),
                    new Vector3(-0.22262f, -0.840572f, 0.008545f),
                    new Vector3(0.189019f, -1.044602f, 0.713507f),
                    new Vector3(0.376408f, 0.942079f, -0.051032f),
                    new Vector3(0.485263f, 0.484985f, -0.749073f),
                    new Vector3(0.49986f, -0.26393f, 0.757943f),
                    new Vector3(0.608714f, -0.721024f, 0.059902f),
                    new Vector3(0.853802f, 0.398849f, 0.379134f),
                    new Vector3(0.962656f, -0.058246f, -0.318907f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            57, () =>
            {
                var faces = new[]
                {
                    new[] { 9, 14, 11 },
                    new[] { 9, 12, 14 },
                    new[] { 11, 14, 13 },
                    new[] { 13, 14, 12 },
                    new[] { 8, 6, 2 },
                    new[] { 8, 10, 6 },
                    new[] { 2, 6, 5 },
                    new[] { 5, 6, 10 },
                    new[] { 0, 1, 4 },
                    new[] { 0, 3, 1 },
                    new[] { 4, 1, 7 },
                    new[] { 7, 1, 3 },
                    new[] { 9, 4, 7, 12 },
                    new[] { 8, 11, 13, 10 },
                    new[] { 0, 2, 5, 3 },
                    new[] { 0, 4, 9, 11, 8, 2 },
                    new[] { 10, 13, 12, 7, 3, 5 }
                };
                var verts = new[]
                {
                    new Vector3(-0.902174f, -0.044182f, -0.142406f),
                    new Vector3(-0.852256f, -0.16038f, -0.950443f),
                    new Vector3(-0.65694f, 0.352221f, 0.529639f),
                    new Vector3(-0.484062f, 0.483825f, -0.606421f),
                    new Vector3(-0.454289f, -0.660407f, -0.440037f),
                    new Vector3(-0.238829f, 0.880229f, 0.065624f),
                    new Vector3(-0.184058f, 0.919718f, 0.880707f),
                    new Vector3(-0.036177f, -0.1324f, -0.904053f),
                    new Vector3(0.036178f, 0.1324f, 0.904052f),
                    new Vector3(0.238829f, -0.880229f, -0.065625f),
                    new Vector3(0.454289f, 0.660407f, 0.440037f),
                    new Vector3(0.484063f, -0.483825f, 0.60642f),
                    new Vector3(0.656941f, -0.352221f, -0.52964f),
                    new Vector3(0.902174f, 0.044182f, 0.142405f),
                    new Vector3(1.036314f, -0.759338f, 0.069736f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            58, () =>
            {
                var faces = new[]
                {
                    new[] { 2, 0, 5 },
                    new[] { 2, 5, 8 },
                    new[] { 2, 1, 0 },
                    new[] { 2, 7, 1 },
                    new[] { 7, 2, 8 },
                    new[] { 18, 20, 16, 11, 12 },
                    new[] { 6, 4, 11, 16, 13 },
                    new[] { 1, 7, 12, 11, 4 },
                    new[] { 8, 15, 18, 12, 7 },
                    new[] { 17, 19, 20, 18, 15 },
                    new[] { 14, 13, 16, 20, 19 },
                    new[] { 19, 17, 10, 9, 14 },
                    new[] { 15, 8, 5, 10, 17 },
                    new[] { 4, 6, 3, 0, 1 },
                    new[] { 13, 14, 9, 3, 6 },
                    new[] { 3, 9, 10, 5, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.906673f, 0.136106f, 0.246909f),
                    new Vector3(-0.827056f, 0.097501f, -0.456089f),
                    new Vector3(-0.822039f, 0.728118f, -0.133084f),
                    new Vector3(-0.682157f, -0.474972f, 0.526574f),
                    new Vector3(-0.553334f, -0.537436f, -0.610901f),
                    new Vector3(-0.48677f, 0.654002f, 0.486704f),
                    new Vector3(-0.463781f, -0.891244f, -0.003583f),
                    new Vector3(-0.357947f, 0.591537f, -0.650771f),
                    new Vector3(-0.147639f, 0.935474f, -0.068093f),
                    new Vector3(-0.123496f, -0.334743f, 0.939211f),
                    new Vector3(-0.00274f, 0.363001f, 0.91457f),
                    new Vector3(0.084945f, -0.435813f, -0.901263f),
                    new Vector3(0.2057f, 0.261931f, -0.925903f),
                    new Vector3(0.229843f, -1.008285f, 0.0814f),
                    new Vector3(0.440151f, -0.664349f, 0.664079f),
                    new Vector3(0.545986f, 0.818432f, 0.01689f),
                    new Vector3(0.568974f, -0.726814f, -0.473396f),
                    new Vector3(0.635538f, 0.464625f, 0.624209f),
                    new Vector3(0.764361f, 0.40216f, -0.513266f),
                    new Vector3(0.90926f, -0.170313f, 0.469397f),
                    new Vector3(0.988877f, -0.208918f, -0.233601f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            59, () =>
            {
                var faces = new[]
                {
                    new[] { 0, 7, 3 },
                    new[] { 0, 3, 1 },
                    new[] { 0, 5, 7 },
                    new[] { 0, 2, 5 },
                    new[] { 2, 0, 1 },
                    new[] { 21, 18, 14 },
                    new[] { 21, 14, 16 },
                    new[] { 21, 20, 18 },
                    new[] { 21, 19, 20 },
                    new[] { 19, 21, 16 },
                    new[] { 6, 8, 14, 18, 12 },
                    new[] { 17, 11, 12, 18, 20 },
                    new[] { 5, 2, 6, 12, 11 },
                    new[] { 1, 4, 8, 6, 2 },
                    new[] { 10, 16, 14, 8, 4 },
                    new[] { 4, 1, 3, 9, 10 },
                    new[] { 11, 17, 13, 7, 5 },
                    new[] { 20, 19, 15, 13, 17 },
                    new[] { 16, 10, 9, 15, 19 },
                    new[] { 15, 9, 3, 7, 13 }
                };
                var verts = new[]
                {
                    new Vector3(-0.987924f, -0.168105f, -0.565605f),
                    new Vector3(-0.950092f, -0.216897f, 0.133657f),
                    new Vector3(-0.877263f, 0.407337f, -0.179083f),
                    new Vector3(-0.637339f, -0.699196f, -0.269274f),
                    new Vector3(-0.609869f, -0.193139f, 0.747222f),
                    new Vector3(-0.519498f, 0.310837f, -0.775297f),
                    new Vector3(-0.492028f, 0.816894f, 0.241199f),
                    new Vector3(-0.371218f, -0.373038f, -0.831038f),
                    new Vector3(-0.32677f, 0.445779f, 0.813687f),
                    new Vector3(-0.103824f, -0.973514f, 0.095267f),
                    new Vector3(-0.086847f, -0.660753f, 0.723496f),
                    new Vector3(0.086847f, 0.660753f, -0.723496f),
                    new Vector3(0.103824f, 0.973513f, -0.095267f),
                    new Vector3(0.32677f, -0.44578f, -0.813687f),
                    new Vector3(0.371218f, 0.373038f, 0.831038f),
                    new Vector3(0.492028f, -0.816894f, -0.241199f),
                    new Vector3(0.519498f, -0.310837f, 0.775297f),
                    new Vector3(0.609869f, 0.193139f, -0.747222f),
                    new Vector3(0.637339f, 0.699195f, 0.269274f),
                    new Vector3(0.877262f, -0.407338f, 0.179083f),
                    new Vector3(0.950092f, 0.216897f, -0.133657f),
                    new Vector3(0.987924f, 0.168105f, 0.565605f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            60, () =>
            {
                var faces = new[]
                {
                    new[] { 7, 13, 15 },
                    new[] { 7, 15, 9 },
                    new[] { 7, 4, 13 },
                    new[] { 7, 2, 4 },
                    new[] { 2, 7, 9 },
                    new[] { 5, 1, 8 },
                    new[] { 5, 8, 14 },
                    new[] { 5, 3, 1 },
                    new[] { 5, 11, 3 },
                    new[] { 11, 5, 14 },
                    new[] { 0, 6, 10, 8, 1 },
                    new[] { 19, 14, 8, 10, 17 },
                    new[] { 4, 2, 0, 1, 3 },
                    new[] { 9, 12, 6, 0, 2 },
                    new[] { 18, 17, 10, 6, 12 },
                    new[] { 12, 9, 15, 20, 18 },
                    new[] { 3, 11, 16, 13, 4 },
                    new[] { 14, 19, 21, 16, 11 },
                    new[] { 17, 18, 20, 21, 19 },
                    new[] { 21, 20, 15, 13, 16 }
                };
                var verts = new[]
                {
                    new Vector3(-0.858966f, -0.256781f, -0.362035f),
                    new Vector3(-0.807204f, -0.391109f, 0.326114f),
                    new Vector3(-0.719263f, 0.425042f, -0.461407f),
                    new Vector3(-0.63551f, 0.207695f, 0.652043f),
                    new Vector3(-0.58116f, 0.712103f, 0.165328f),
                    new Vector3(-0.46306f, -0.410585f, 0.938862f),
                    new Vector3(-0.364654f, -0.630479f, -0.694119f),
                    new Vector3(-0.311484f, 0.996138f, -0.418515f),
                    new Vector3(-0.280902f, -0.847826f, 0.419331f),
                    new Vector3(-0.13861f, 0.472734f, -0.854906f),
                    new Vector3(-0.007391f, -0.995764f, -0.211209f),
                    new Vector3(-0.003096f, 0.121058f, 0.946694f),
                    new Vector3(0.08055f, -0.179614f, -0.99873f),
                    new Vector3(0.084846f, 0.937209f, 0.159173f),
                    new Vector3(0.216065f, -0.531289f, 0.80287f),
                    new Vector3(0.358357f, 0.789271f, -0.471366f),
                    new Vector3(0.442109f, 0.571923f, 0.642084f),
                    new Vector3(0.658614f, -0.770659f, -0.217364f),
                    new Vector3(0.712965f, -0.26625f, -0.704079f),
                    new Vector3(0.796718f, -0.483597f, 0.409371f),
                    new Vector3(0.884659f, 0.332554f, -0.37815f),
                    new Vector3(0.936421f, 0.198226f, 0.31f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            61, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 3, 1 },
                    new[] { 6, 1, 8 },
                    new[] { 6, 11, 3 },
                    new[] { 6, 14, 11 },
                    new[] { 14, 6, 8 },
                    new[] { 19, 22, 21 },
                    new[] { 19, 21, 15 },
                    new[] { 19, 16, 22 },
                    new[] { 19, 13, 16 },
                    new[] { 13, 19, 15 },
                    new[] { 4, 0, 2 },
                    new[] { 4, 2, 9 },
                    new[] { 4, 7, 0 },
                    new[] { 4, 12, 7 },
                    new[] { 12, 4, 9 },
                    new[] { 20, 17, 18, 21, 22 },
                    new[] { 9, 15, 21, 18, 12 },
                    new[] { 11, 14, 20, 22, 16 },
                    new[] { 8, 10, 17, 20, 14 },
                    new[] { 7, 12, 18, 17, 10 },
                    new[] { 10, 8, 1, 0, 7 },
                    new[] { 16, 13, 5, 3, 11 },
                    new[] { 15, 9, 2, 5, 13 },
                    new[] { 2, 0, 1, 3, 5 }
                };
                var verts = new[]
                {
                    new Vector3(-0.875027f, -0.215344f, -0.354115f),
                    new Vector3(-0.822602f, -0.401147f, 0.315989f),
                    new Vector3(-0.733096f, 0.465836f, -0.400606f),
                    new Vector3(-0.648272f, 0.1652f, 0.683645f),
                    new Vector3(-0.625957f, 0.049095f, -0.949385f),
                    new Vector3(-0.592955f, 0.701024f, 0.240765f),
                    new Vector3(-0.479753f, -0.469083f, 0.919437f),
                    new Vector3(-0.387571f, -0.561993f, -0.712628f),
                    new Vector3(-0.302747f, -0.862629f, 0.371622f),
                    new Vector3(-0.157924f, 0.540178f, -0.787852f),
                    new Vector3(-0.033883f, -0.962037f, -0.264098f),
                    new Vector3(-0.020675f, 0.053739f, 0.966503f),
                    new Vector3(0.055623f, -0.095055f, -0.980693f),
                    new Vector3(0.068831f, 0.920721f, 0.249908f),
                    new Vector3(0.192872f, -0.581494f, 0.773662f),
                    new Vector3(0.337694f, 0.821313f, -0.385812f),
                    new Vector3(0.422519f, 0.520677f, 0.698439f),
                    new Vector3(0.627903f, -0.74234f, -0.254955f),
                    new Vector3(0.683221f, -0.206515f, -0.697835f),
                    new Vector3(0.756251f, 0.83314f, 0.171845f),
                    new Vector3(0.768045f, -0.507151f, 0.386416f),
                    new Vector3(0.85755f, 0.359831f, -0.330178f),
                    new Vector3(0.909975f, 0.174028f, 0.339925f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            62, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 3, 0 },
                    new[] { 0, 3, 1 },
                    new[] { 1, 5, 2 },
                    new[] { 1, 2, 0 },
                    new[] { 2, 4, 0 },
                    new[] { 4, 8, 6 },
                    new[] { 4, 6, 0 },
                    new[] { 9, 7, 8 },
                    new[] { 7, 6, 8 },
                    new[] { 7, 3, 6 },
                    new[] { 1, 3, 7, 9, 5 },
                    new[] { 2, 5, 9, 8, 4 }
                };
                var verts = new[]
                {
                    new Vector3(-0.821855f, 0.223834f, -0.340481f),
                    new Vector3(-0.71039f, -0.701977f, 0.157898f),
                    new Vector3(-0.692806f, 0.20039f, 0.708676f),
                    new Vector3(-0.215696f, -0.533443f, -0.761235f),
                    new Vector3(-0.187244f, 0.926618f, 0.129942f),
                    new Vector3(-0.006891f, -0.571377f, 0.936336f),
                    new Vector3(0.107626f, 0.473083f, -0.778513f),
                    new Vector3(0.793541f, -0.298683f, -0.550854f),
                    new Vector3(0.811125f, 0.603684f, -0.000076f),
                    new Vector3(0.92259f, -0.322128f, 0.498303f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            63, () =>
            {
                var faces = new[]
                {
                    new[] { 0, 4, 6 },
                    new[] { 4, 8, 6 },
                    new[] { 6, 2, 0 },
                    new[] { 0, 1, 4 },
                    new[] { 5, 7, 3 },
                    new[] { 6, 8, 7, 5, 2 },
                    new[] { 0, 2, 5, 3, 1 },
                    new[] { 7, 8, 4, 1, 3 }
                };
                var verts = new[]
                {
                    new Vector3(-0.799898f, 0.494585f, -0.153719f),
                    new Vector3(-0.680241f, -0.086273f, 0.717027f),
                    new Vector3(-0.306176f, 0.002547f, -0.943688f),
                    new Vector3(-0.112567f, -0.9373f, 0.465209f),
                    new Vector3(-0.077419f, 0.764103f, 0.564122f),
                    new Vector3(0.118618f, -0.882406f, -0.56117f),
                    new Vector3(0.153766f, 0.818996f, -0.462256f),
                    new Vector3(0.841097f, -0.612888f, 0.156672f),
                    new Vector3(0.86282f, 0.438637f, 0.217804f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            64, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 5, 8 },
                    new[] { 5, 4, 8 },
                    new[] { 8, 7, 6 },
                    new[] { 6, 2, 5 },
                    new[] { 9, 1, 0 },
                    new[] { 9, 0, 3 },
                    new[] { 9, 3, 1 },
                    new[] { 8, 4, 1, 3, 7 },
                    new[] { 6, 7, 3, 0, 2 },
                    new[] { 1, 4, 5, 2, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.777985f, -0.188235f, -0.285228f),
                    new Vector3(-0.489362f, -0.260552f, 0.643961f),
                    new Vector3(-0.32442f, 0.578558f, -0.683014f),
                    new Vector3(-0.099589f, -0.843165f, -0.034687f),
                    new Vector3(0.142581f, 0.461547f, 0.820446f),
                    new Vector3(0.24452f, 0.980145f, 0.00033f),
                    new Vector3(0.634293f, 0.397531f, -0.678318f),
                    new Vector3(0.773247f, -0.481142f, -0.27763f),
                    new Vector3(0.922916f, 0.325215f, 0.250871f),
                    new Vector3(-1.026197f, -0.969903f, 0.24327f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            65, () =>
            {
                var faces = new[]
                {
                    new[] { 12, 7, 11 },
                    new[] { 10, 13, 11 },
                    new[] { 14, 13, 9 },
                    new[] { 9, 3, 6 },
                    new[] { 0, 1, 5 },
                    new[] { 4, 2, 7 },
                    new[] { 6, 4, 8 },
                    new[] { 14, 8, 12 },
                    new[] { 12, 8, 4, 7 },
                    new[] { 13, 14, 12, 11 },
                    new[] { 14, 9, 6, 8 },
                    new[] { 11, 7, 2, 0, 5, 10 },
                    new[] { 13, 10, 5, 1, 3, 9 },
                    new[] { 3, 1, 0, 2, 4, 6 }
                };
                var verts = new[]
                {
                    new Vector3(-0.91403f, 0.409064f, -0.602734f),
                    new Vector3(-0.830829f, 0.815648f, 0.102539f),
                    new Vector3(-0.699591f, -0.379344f, -0.648261f),
                    new Vector3(-0.533188f, 0.433825f, 0.762285f),
                    new Vector3(-0.401951f, -0.761167f, 0.011485f),
                    new Vector3(-0.338469f, 0.986004f, -0.528495f),
                    new Vector3(-0.318749f, -0.354582f, 0.716757f),
                    new Vector3(0.090409f, -0.59081f, -0.619549f),
                    new Vector3(0.251663f, -0.893891f, 0.485632f),
                    new Vector3(0.256812f, 0.222359f, 0.790997f),
                    new Vector3(0.451531f, 0.774538f, -0.499782f),
                    new Vector3(0.66597f, -0.013869f, -0.545309f),
                    new Vector3(0.744023f, -0.723534f, -0.145401f),
                    new Vector3(0.749172f, 0.392715f, 0.159963f),
                    new Vector3(0.827225f, -0.31695f, 0.559871f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            66, () =>
            {
                var faces = new[]
                {
                    new[] { 24, 16, 17 },
                    new[] { 26, 19, 21 },
                    new[] { 27, 23, 22 },
                    new[] { 25, 20, 18 },
                    new[] { 21, 15, 23 },
                    new[] { 5, 7, 11 },
                    new[] { 17, 13, 19 },
                    new[] { 1, 3, 9 },
                    new[] { 18, 12, 16 },
                    new[] { 2, 0, 8 },
                    new[] { 22, 14, 20 },
                    new[] { 6, 4, 10 },
                    new[] { 25, 24, 26, 27 },
                    new[] { 24, 25, 18, 16 },
                    new[] { 26, 24, 17, 19 },
                    new[] { 27, 26, 21, 23 },
                    new[] { 25, 27, 22, 20 },
                    new[] { 23, 15, 11, 7, 6, 10, 14, 22 },
                    new[] { 20, 14, 10, 4, 2, 8, 12, 18 },
                    new[] { 16, 12, 8, 0, 1, 9, 13, 17 },
                    new[] { 19, 13, 9, 3, 5, 11, 15, 21 },
                    new[] { 4, 6, 7, 5, 3, 1, 0, 2 }
                };
                var verts = new[]
                {
                    new Vector3(-0.935384f, 0.507278f, -0.272116f),
                    new Vector3(-0.915801f, 0.536228f, 0.283002f),
                    new Vector3(-0.882043f, 0.099694f, -0.646826f),
                    new Vector3(-0.834764f, 0.169584f, 0.693348f),
                    new Vector3(-0.787023f, -0.447768f, -0.621628f),
                    new Vector3(-0.739744f, -0.377878f, 0.718546f),
                    new Vector3(-0.705986f, -0.814411f, -0.211282f),
                    new Vector3(-0.686402f, -0.785462f, 0.343836f),
                    new Vector3(-0.561956f, 0.553237f, -0.681771f),
                    new Vector3(-0.514677f, 0.623127f, 0.658404f),
                    new Vector3(-0.332557f, -0.768453f, -0.620937f),
                    new Vector3(-0.285278f, -0.698562f, 0.719237f),
                    new Vector3(-0.014265f, 0.647182f, -0.705992f),
                    new Vector3(0.033015f, 0.717073f, 0.634183f),
                    new Vector3(0.215134f, -0.674508f, -0.645158f),
                    new Vector3(0.262413f, -0.604617f, 0.695016f),
                    new Vector3(0.386859f, 0.734081f, -0.330591f),
                    new Vector3(0.406443f, 0.763031f, 0.224528f),
                    new Vector3(0.440201f, 0.326497f, -0.705301f),
                    new Vector3(0.48748f, 0.396388f, 0.634874f),
                    new Vector3(0.535221f, -0.220964f, -0.680103f),
                    new Vector3(0.5825f, -0.151074f, 0.660072f),
                    new Vector3(0.616258f, -0.587608f, -0.269757f),
                    new Vector3(0.635842f, -0.558659f, 0.285361f),
                    new Vector3(0.831438f, 0.541255f, -0.057559f),
                    new Vector3(0.884779f, 0.13367f, -0.432269f),
                    new Vector3(0.912475f, 0.174611f, 0.352787f),
                    new Vector3(0.965816f, -0.232973f, -0.021924f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            67, () =>
            {
                var faces = new[]
                {
                    new[] { 22, 25, 17 },
                    new[] { 15, 10, 7 },
                    new[] { 19, 13, 20 },
                    new[] { 28, 29, 31 },
                    new[] { 9, 14, 6 },
                    new[] { 3, 0, 2 },
                    new[] { 12, 11, 18 },
                    new[] { 16, 24, 21 },
                    new[] { 7, 4, 13 },
                    new[] { 0, 6, 1 },
                    new[] { 17, 8, 10 },
                    new[] { 11, 2, 5 },
                    new[] { 31, 30, 25 },
                    new[] { 24, 18, 27 },
                    new[] { 20, 26, 29 },
                    new[] { 14, 21, 23 },
                    new[] { 28, 22, 15, 19 },
                    new[] { 22, 28, 31, 25 },
                    new[] { 15, 22, 17, 10 },
                    new[] { 19, 15, 7, 13 },
                    new[] { 28, 19, 20, 29 },
                    new[] { 16, 9, 3, 12 },
                    new[] { 9, 16, 21, 14 },
                    new[] { 3, 9, 6, 0 },
                    new[] { 12, 3, 2, 11 },
                    new[] { 16, 12, 18, 24 },
                    new[] { 13, 4, 1, 6, 14, 23, 26, 20 },
                    new[] { 29, 26, 23, 21, 24, 27, 30, 31 },
                    new[] { 25, 30, 27, 18, 11, 5, 8, 17 },
                    new[] { 10, 8, 5, 2, 0, 1, 4, 7 }
                };
                var verts = new[]
                {
                    new Vector3(-0.85468f, -0.304544f, 0.321795f),
                    new Vector3(-0.813538f, 0.214997f, 0.467663f),
                    new Vector3(-0.753988f, -0.584165f, -0.130495f),
                    new Vector3(-0.662946f, -0.809677f, 0.352981f),
                    new Vector3(-0.654663f, 0.670118f, 0.221662f),
                    new Vector3(-0.570447f, -0.460068f, -0.624262f),
                    new Vector3(-0.567052f, -0.168344f, 0.759532f),
                    new Vector3(-0.471123f, 0.794216f, -0.272106f),
                    new Vector3(-0.411573f, -0.004947f, -0.870263f),
                    new Vector3(-0.375319f, -0.673477f, 0.790718f),
                    new Vector3(-0.370431f, 0.514594f, -0.724396f),
                    new Vector3(-0.323962f, -0.84341f, -0.332393f),
                    new Vector3(-0.23292f, -1.068921f, 0.151083f),
                    new Vector3(-0.183495f, 0.930415f, 0.165631f),
                    new Vector3(-0.059594f, -0.25535f, 0.926295f),
                    new Vector3(-0.054707f, 0.932721f, -0.588818f),
                    new Vector3(0.054707f, -0.932721f, 0.588819f),
                    new Vector3(0.059595f, 0.25535f, -0.926294f),
                    new Vector3(0.183496f, -0.930415f, -0.16563f),
                    new Vector3(0.23292f, 1.068921f, -0.151082f),
                    new Vector3(0.323962f, 0.84341f, 0.332394f),
                    new Vector3(0.370432f, -0.514594f, 0.724397f),
                    new Vector3(0.37532f, 0.673477f, -0.790717f),
                    new Vector3(0.411574f, 0.004947f, 0.870264f),
                    new Vector3(0.471123f, -0.794216f, 0.272106f),
                    new Vector3(0.567053f, 0.168344f, -0.759531f),
                    new Vector3(0.570448f, 0.460068f, 0.624263f),
                    new Vector3(0.654664f, -0.670118f, -0.221661f),
                    new Vector3(0.662947f, 0.809677f, -0.35298f),
                    new Vector3(0.753989f, 0.584165f, 0.130496f),
                    new Vector3(0.813538f, -0.214997f, -0.467662f),
                    new Vector3(0.85468f, 0.304544f, -0.321795f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            68, () =>
            {
                var faces = new[]
                {
                    new[] { 34, 30, 24 },
                    new[] { 45, 51, 41 },
                    new[] { 36, 26, 31 },
                    new[] { 53, 49, 44 },
                    new[] { 16, 9, 19 },
                    new[] { 27, 20, 21 },
                    new[] { 1, 3, 5 },
                    new[] { 4, 2, 0 },
                    new[] { 11, 17, 7 },
                    new[] { 12, 22, 13 },
                    new[] { 62, 56, 58 },
                    new[] { 59, 57, 63 },
                    new[] { 43, 40, 50 },
                    new[] { 39, 32, 38 },
                    new[] { 23, 28, 33 },
                    new[] { 10, 6, 15 },
                    new[] { 25, 35, 29 },
                    new[] { 8, 14, 18 },
                    new[] { 48, 52, 42 },
                    new[] { 47, 46, 37 },
                    new[] { 54, 48, 46 },
                    new[] { 55, 47, 49 },
                    new[] { 61, 53, 59 },
                    new[] { 62, 64, 63 },
                    new[] { 60, 58, 52 },
                    new[] { 54, 60, 52, 48 },
                    new[] { 55, 54, 46, 47 },
                    new[] { 61, 55, 49, 53 },
                    new[] { 64, 61, 59, 63 },
                    new[] { 60, 64, 62, 58 },
                    new[] { 60, 54, 55, 61, 64 },
                    new[] { 31, 26, 19, 9, 5, 3, 7, 17, 24, 30 },
                    new[] { 59, 53, 44, 36, 31, 30, 34, 41, 51, 57 },
                    new[] { 37, 27, 21, 16, 19, 26, 36, 44, 49, 47 },
                    new[] { 8, 4, 0, 1, 5, 9, 16, 21, 20, 14 },
                    new[] { 10, 12, 13, 11, 7, 3, 1, 0, 2, 6 },
                    new[] { 39, 45, 41, 34, 24, 17, 11, 13, 22, 32 },
                    new[] { 50, 40, 33, 28, 29, 35, 42, 52, 58, 56 },
                    new[] { 45, 39, 38, 43, 50, 56, 62, 63, 57, 51 },
                    new[] { 12, 10, 15, 23, 33, 40, 43, 38, 32, 22 },
                    new[] { 4, 8, 18, 25, 29, 28, 23, 15, 6, 2 },
                    new[] { 27, 37, 46, 48, 42, 35, 25, 18, 14, 20 }
                };
                var verts = new[]
                {
                    new Vector3(-1.053731f, -0.062801f, 0.140871f),
                    new Vector3(-1.047729f, -0.006643f, -0.190675f),
                    new Vector3(-0.967657f, 0.064553f, 0.440012f),
                    new Vector3(-0.951944f, 0.211579f, -0.427988f),
                    new Vector3(-0.942283f, -0.266176f, 0.38445f),
                    new Vector3(-0.92657f, -0.11915f, -0.483549f),
                    new Vector3(-0.822385f, 0.326776f, 0.592485f),
                    new Vector3(-0.802963f, 0.50851f, -0.480421f),
                    new Vector3(-0.755954f, -0.539085f, 0.447023f),
                    new Vector3(-0.736532f, -0.35735f, -0.625883f),
                    new Vector3(-0.673403f, 0.623707f, 0.540051f),
                    new Vector3(-0.65769f, 0.770733f, -0.327948f),
                    new Vector3(-0.577618f, 0.841928f, 0.302738f),
                    new Vector3(-0.571617f, 0.898087f, -0.028808f),
                    new Vector3(-0.565916f, -0.777285f, 0.304689f),
                    new Vector3(-0.561955f, 0.420332f, 0.78363f),
                    new Vector3(-0.550203f, -0.630259f, -0.56331f),
                    new Vector3(-0.536531f, 0.658225f, -0.620822f),
                    new Vector3(-0.479842f, -0.64993f, 0.603829f),
                    new Vector3(-0.454419f, -0.412037f, -0.800623f),
                    new Vector3(-0.444757f, -0.889793f, 0.011815f),
                    new Vector3(-0.438755f, -0.833633f, -0.319731f),
                    new Vector3(-0.311187f, 0.991643f, 0.162337f),
                    new Vector3(-0.285843f, 0.309485f, 0.940436f),
                    new Vector3(-0.254418f, 0.603538f, -0.795562f),
                    new Vector3(-0.219412f, -0.556374f, 0.794974f),
                    new Vector3(-0.187987f, -0.262321f, -0.941024f),
                    new Vector3(-0.162643f, -0.944479f, -0.162925f),
                    new Vector3(-0.099515f, 0.036577f, 1.003008f),
                    new Vector3(-0.07414f, -0.294152f, 0.947447f),
                    new Vector3(-0.06438f, 0.365339f, -0.937896f),
                    new Vector3(-0.039006f, 0.034609f, -0.993458f),
                    new Vector3(0.024123f, 1.015665f, 0.172476f),
                    new Vector3(0.049467f, 0.333508f, 0.950575f),
                    new Vector3(0.080892f, 0.627561f, -0.785423f),
                    new Vector3(0.115898f, -0.532352f, 0.805113f),
                    new Vector3(0.147323f, -0.238299f, -0.930885f),
                    new Vector3(0.172667f, -0.920457f, -0.152786f),
                    new Vector3(0.300235f, 0.90482f, 0.329282f),
                    new Vector3(0.306237f, 0.960979f, -0.002264f),
                    new Vector3(0.315898f, 0.483223f, 0.810173f),
                    new Vector3(0.341322f, 0.721117f, -0.594278f),
                    new Vector3(0.398011f, -0.587039f, 0.630373f),
                    new Vector3(0.411683f, 0.701445f, 0.572861f),
                    new Vector3(0.423435f, -0.349145f, -0.774079f),
                    new Vector3(0.427396f, 0.848471f, -0.295138f),
                    new Vector3(0.433096f, -0.826901f, 0.038358f),
                    new Vector3(0.439098f, -0.770742f, -0.293188f),
                    new Vector3(0.51917f, -0.699547f, 0.337499f),
                    new Vector3(0.534883f, -0.55252f, -0.5305f),
                    new Vector3(0.598012f, 0.428536f, 0.635433f),
                    new Vector3(0.617434f, 0.610271f, -0.437472f),
                    new Vector3(0.664443f, -0.437324f, 0.489972f),
                    new Vector3(0.683864f, -0.255589f, -0.582934f),
                    new Vector3(0.721659f, -0.657508f, 0.072275f),
                    new Vector3(0.727661f, -0.601349f, -0.259271f),
                    new Vector3(0.78805f, 0.190336f, 0.493099f),
                    new Vector3(0.803762f, 0.337363f, -0.3749f),
                    new Vector3(0.813424f, -0.140393f, 0.437538f),
                    new Vector3(0.829136f, 0.006634f, -0.430461f),
                    new Vector3(0.866931f, -0.395285f, 0.224748f),
                    new Vector3(0.876642f, -0.304418f, -0.311705f),
                    new Vector3(0.909209f, 0.077829f, 0.200225f),
                    new Vector3(0.91521f, 0.133987f, -0.131321f),
                    new Vector3(0.962716f, -0.177064f, -0.012565f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            69, () =>
            {
                var faces = new[]
                {
                    new[] { 4, 8, 12 },
                    new[] { 3, 0, 1 },
                    new[] { 18, 28, 15 },
                    new[] { 9, 21, 16 },
                    new[] { 49, 45, 37 },
                    new[] { 52, 62, 55 },
                    new[] { 56, 43, 50 },
                    new[] { 64, 63, 67 },
                    new[] { 29, 23, 34 },
                    new[] { 36, 27, 31 },
                    new[] { 13, 19, 26 },
                    new[] { 5, 2, 6 },
                    new[] { 20, 32, 24 },
                    new[] { 7, 17, 14 },
                    new[] { 51, 54, 41 },
                    new[] { 60, 53, 48 },
                    new[] { 65, 57, 61 },
                    new[] { 69, 66, 68 },
                    new[] { 40, 35, 46 },
                    new[] { 33, 38, 42 },
                    new[] { 30, 40, 38 },
                    new[] { 22, 33, 21 },
                    new[] { 10, 9, 5 },
                    new[] { 13, 11, 6 },
                    new[] { 25, 26, 35 },
                    new[] { 58, 56, 63 },
                    new[] { 59, 64, 60 },
                    new[] { 47, 48, 36 },
                    new[] { 29, 39, 31 },
                    new[] { 44, 34, 43 },
                    new[] { 30, 25, 35, 40 },
                    new[] { 22, 30, 38, 33 },
                    new[] { 10, 22, 21, 9 },
                    new[] { 11, 10, 5, 6 },
                    new[] { 25, 11, 13, 26 },
                    new[] { 58, 44, 43, 56 },
                    new[] { 59, 58, 63, 64 },
                    new[] { 47, 59, 60, 48 },
                    new[] { 39, 47, 36, 31 },
                    new[] { 44, 39, 29, 34 },
                    new[] { 25, 30, 22, 10, 11 },
                    new[] { 44, 58, 59, 47, 39 },
                    new[] { 15, 28, 37, 45, 50, 43, 34, 23, 12, 8 },
                    new[] { 5, 9, 16, 18, 15, 8, 4, 1, 0, 2 },
                    new[] { 42, 52, 55, 49, 37, 28, 18, 16, 21, 33 },
                    new[] { 69, 67, 63, 56, 50, 45, 49, 55, 62, 66 },
                    new[] { 67, 69, 68, 65, 61, 54, 51, 53, 60, 64 },
                    new[] { 7, 3, 1, 4, 12, 23, 29, 31, 27, 17 },
                    new[] { 24, 32, 41, 54, 61, 57, 46, 35, 26, 19 },
                    new[] { 3, 7, 14, 20, 24, 19, 13, 6, 2, 0 },
                    new[] { 36, 48, 53, 51, 41, 32, 20, 14, 17, 27 },
                    new[] { 52, 42, 38, 40, 46, 57, 65, 68, 66, 62 }
                };
                var verts = new[]
                {
                    new Vector3(-0.988041f, 0.082361f, -0.032394f),
                    new Vector3(-0.940439f, 0.10562f, 0.297446f),
                    new Vector3(-0.93903f, -0.112232f, -0.299475f),
                    new Vector3(-0.911497f, 0.377114f, 0.10495f),
                    new Vector3(-0.814408f, -0.051339f, 0.564056f),
                    new Vector3(-0.812127f, -0.403831f, -0.401783f),
                    new Vector3(-0.783185f, -0.132338f, -0.594279f),
                    new Vector3(-0.738637f, 0.65944f, 0.060095f),
                    new Vector3(-0.658086f, -0.328563f, 0.665601f),
                    new Vector3(-0.655805f, -0.681055f, -0.300238f),
                    new Vector3(-0.621075f, -0.575598f, -0.615315f),
                    new Vector3(-0.592133f, -0.304105f, -0.807812f),
                    new Vector3(-0.581542f, -0.03381f, 0.802944f),
                    new Vector3(-0.580033f, 0.029724f, -0.8042f),
                    new Vector3(-0.535485f, 0.821501f, -0.149826f),
                    new Vector3(-0.531183f, -0.620162f, 0.563294f),
                    new Vector3(-0.529773f, -0.838014f, -0.033627f),
                    new Vector3(-0.487884f, 0.844761f, 0.180013f),
                    new Vector3(-0.482172f, -0.814755f, 0.296212f),
                    new Vector3(-0.407173f, 0.31205f, -0.849055f),
                    new Vector3(-0.37964f, 0.801396f, -0.44463f),
                    new Vector3(-0.373928f, -0.858119f, -0.328432f),
                    new Vector3(-0.339198f, -0.752663f, -0.643508f),
                    new Vector3(-0.33079f, 0.15151f, 0.922864f),
                    new Vector3(-0.330629f, 0.606803f, -0.711711f),
                    new Vector3(-0.292369f, -0.313377f, -0.954974f),
                    new Vector3(-0.28027f, 0.020452f, -0.951362f),
                    new Vector3(-0.255019f, 0.862289f, 0.418901f),
                    new Vector3(-0.249306f, -0.797226f, 0.5351f),
                    new Vector3(-0.157929f, 0.433837f, 0.878008f),
                    new Vector3(-0.136047f, -0.590601f, -0.85343f),
                    new Vector3(-0.128987f, 0.70533f, 0.685512f),
                    new Vector3(-0.079877f, 0.792123f, -0.591793f),
                    new Vector3(-0.074164f, -0.867392f, -0.475594f),
                    new Vector3(-0.001607f, 0.156613f, 0.979553f),
                    new Vector3(0.001607f, -0.156613f, -0.979556f),
                    new Vector3(0.074164f, 0.867391f, 0.475591f),
                    new Vector3(0.079877f, -0.792124f, 0.59179f),
                    new Vector3(0.128987f, -0.70533f, -0.685515f),
                    new Vector3(0.136047f, 0.590601f, 0.853427f),
                    new Vector3(0.157929f, -0.433837f, -0.878011f),
                    new Vector3(0.249306f, 0.797226f, -0.535103f),
                    new Vector3(0.255019f, -0.862289f, -0.418905f),
                    new Vector3(0.280269f, -0.020452f, 0.95136f),
                    new Vector3(0.292369f, 0.313377f, 0.954971f),
                    new Vector3(0.330629f, -0.606803f, 0.711709f),
                    new Vector3(0.33079f, -0.151511f, -0.922866f),
                    new Vector3(0.339198f, 0.752662f, 0.643506f),
                    new Vector3(0.373928f, 0.858119f, 0.328428f),
                    new Vector3(0.37964f, -0.801396f, 0.444627f),
                    new Vector3(0.407172f, -0.312051f, 0.849052f),
                    new Vector3(0.482172f, 0.814754f, -0.296215f),
                    new Vector3(0.487884f, -0.844761f, -0.180016f),
                    new Vector3(0.529773f, 0.838013f, 0.033625f),
                    new Vector3(0.531183f, 0.620161f, -0.563297f),
                    new Vector3(0.535485f, -0.821502f, 0.149823f),
                    new Vector3(0.580033f, -0.029724f, 0.804197f),
                    new Vector3(0.581542f, 0.03381f, -0.802948f),
                    new Vector3(0.592133f, 0.304105f, 0.807809f),
                    new Vector3(0.621074f, 0.575598f, 0.615313f),
                    new Vector3(0.655804f, 0.681055f, 0.300235f),
                    new Vector3(0.658086f, 0.328562f, -0.665604f),
                    new Vector3(0.738637f, -0.65944f, -0.060097f),
                    new Vector3(0.783184f, 0.132337f, 0.594276f),
                    new Vector3(0.812126f, 0.40383f, 0.40178f),
                    new Vector3(0.814408f, 0.051338f, -0.564059f),
                    new Vector3(0.911497f, -0.377114f, -0.104953f),
                    new Vector3(0.939029f, 0.112232f, 0.299472f),
                    new Vector3(0.940439f, -0.10562f, -0.297449f),
                    new Vector3(0.988041f, -0.082361f, 0.032391f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            70, () =>
            {
                var faces = new[]
                {
                    new[] { 41, 46, 53 },
                    new[] { 26, 22, 34 },
                    new[] { 33, 44, 40 },
                    new[] { 14, 18, 21 },
                    new[] { 48, 58, 49 },
                    new[] { 31, 38, 43 },
                    new[] { 68, 69, 65 },
                    new[] { 59, 63, 66 },
                    new[] { 64, 62, 67 },
                    new[] { 57, 52, 61 },
                    new[] { 3, 9, 2 },
                    new[] { 11, 15, 7 },
                    new[] { 24, 23, 16 },
                    new[] { 32, 39, 28 },
                    new[] { 37, 30, 27 },
                    new[] { 60, 51, 56 },
                    new[] { 29, 19, 25 },
                    new[] { 50, 45, 36 },
                    new[] { 10, 6, 12 },
                    new[] { 17, 13, 20 },
                    new[] { 4, 10, 13 },
                    new[] { 8, 17, 18 },
                    new[] { 5, 14, 11 },
                    new[] { 3, 1, 7 },
                    new[] { 0, 2, 6 },
                    new[] { 47, 37, 51 },
                    new[] { 55, 60, 63 },
                    new[] { 54, 59, 50 },
                    new[] { 29, 42, 36 },
                    new[] { 35, 25, 30 },
                    new[] { 4, 0, 6, 10 },
                    new[] { 8, 4, 13, 17 },
                    new[] { 5, 8, 18, 14 },
                    new[] { 1, 5, 11, 7 },
                    new[] { 0, 1, 3, 2 },
                    new[] { 47, 35, 30, 37 },
                    new[] { 55, 47, 51, 60 },
                    new[] { 54, 55, 63, 59 },
                    new[] { 42, 54, 50, 36 },
                    new[] { 35, 42, 29, 25 },
                    new[] { 0, 4, 8, 5, 1 },
                    new[] { 35, 47, 55, 54, 42 },
                    new[] { 40, 44, 49, 58, 65, 69, 67, 62, 53, 46 },
                    new[] { 11, 14, 21, 33, 40, 46, 41, 34, 22, 15 },
                    new[] { 20, 31, 43, 48, 49, 44, 33, 21, 18, 17 },
                    new[] { 50, 59, 66, 68, 65, 58, 48, 43, 38, 45 },
                    new[] { 56, 57, 61, 64, 67, 69, 68, 66, 63, 60 },
                    new[] { 32, 26, 34, 41, 53, 62, 64, 61, 52, 39 },
                    new[] { 16, 23, 27, 30, 25, 19, 12, 6, 2, 9 },
                    new[] { 26, 32, 28, 24, 16, 9, 3, 7, 15, 22 },
                    new[] { 57, 56, 51, 37, 27, 23, 24, 28, 39, 52 },
                    new[] { 31, 20, 13, 10, 12, 19, 29, 36, 45, 38 }
                };
                var verts = new[]
                {
                    new Vector3(-0.989194f, -0.091477f, -0.102373f),
                    new Vector3(-0.986156f, -0.109996f, 0.232129f),
                    new Vector3(-0.908902f, 0.23333f, -0.08512f),
                    new Vector3(-0.905864f, 0.214811f, 0.249382f),
                    new Vector3(-0.892465f, -0.388533f, -0.22339f),
                    new Vector3(-0.887549f, -0.418497f, 0.317846f),
                    new Vector3(-0.850998f, 0.061184f, -0.366646f),
                    new Vector3(-0.843044f, 0.012701f, 0.509091f),
                    new Vector3(-0.829645f, -0.590642f, 0.03632f),
                    new Vector3(-0.810408f, 0.497156f, 0.096369f),
                    new Vector3(-0.754269f, -0.235871f, -0.487662f),
                    new Vector3(-0.744438f, -0.2958f, 0.594808f),
                    new Vector3(-0.658813f, 0.046474f, -0.640675f),
                    new Vector3(-0.655662f, -0.544372f, -0.401946f),
                    new Vector3(-0.647709f, -0.592856f, 0.473792f),
                    new Vector3(-0.645943f, -0.031974f, 0.776297f),
                    new Vector3(-0.593136f, 0.75189f, 0.108498f),
                    new Vector3(-0.592842f, -0.746482f, -0.142236f),
                    new Vector3(-0.589804f, -0.765001f, 0.192266f),
                    new Vector3(-0.405755f, 0.194817f, -0.802538f),
                    new Vector3(-0.400657f, -0.761193f, -0.416265f),
                    new Vector3(-0.392703f, -0.809676f, 0.459472f),
                    new Vector3(-0.389847f, 0.09785f, 0.948936f),
                    new Vector3(-0.340078f, 0.900233f, -0.053365f),
                    new Vector3(-0.33704f, 0.881715f, 0.281137f),
                    new Vector3(-0.188483f, 0.449551f, -0.790409f),
                    new Vector3(-0.172576f, 0.352585f, 0.961066f),
                    new Vector3(-0.147893f, 0.885523f, -0.327394f),
                    new Vector3(-0.139939f, 0.83704f, 0.548343f),
                    new Vector3(-0.091754f, 0.152496f, -0.911425f),
                    new Vector3(-0.089989f, 0.713378f, -0.60892f),
                    new Vector3(-0.086657f, -0.803514f, -0.525152f),
                    new Vector3(-0.07712f, 0.63493f, 0.808053f),
                    new Vector3(-0.076826f, -0.863443f, 0.557318f),
                    new Vector3(-0.073969f, 0.044084f, 1.046782f),
                    new Vector3(0.126005f, 0.562254f, -0.815685f),
                    new Vector3(0.163251f, -0.064325f, -0.925745f),
                    new Vector3(0.166107f, 0.843202f, -0.436281f),
                    new Vector3(0.166401f, -0.655171f, -0.687015f),
                    new Vector3(0.175938f, 0.783273f, 0.64619f),
                    new Vector3(0.179271f, -0.733618f, 0.729957f),
                    new Vector3(0.181036f, -0.172737f, 1.032463f),
                    new Vector3(0.222734f, 0.265199f, -0.936702f),
                    new Vector3(0.229221f, -0.857281f, -0.427306f),
                    new Vector3(0.237175f, -0.905764f, 0.448431f),
                    new Vector3(0.261857f, -0.372826f, -0.840028f),
                    new Vector3(0.277765f, -0.469792f, 0.911446f),
                    new Vector3(0.382101f, 0.692078f, -0.643046f),
                    new Vector3(0.426322f, -0.901955f, -0.1601f),
                    new Vector3(0.42936f, -0.920474f, 0.174402f),
                    new Vector3(0.479129f, -0.118091f, -0.827899f),
                    new Vector3(0.481985f, 0.789435f, -0.338434f),
                    new Vector3(0.489939f, 0.740952f, 0.537303f),
                    new Vector3(0.495036f, -0.215058f, 0.923576f),
                    new Vector3(0.538612f, 0.211432f, -0.838855f),
                    new Vector3(0.637106f, 0.475258f, -0.657366f),
                    new Vector3(0.679086f, 0.74476f, -0.071228f),
                    new Vector3(0.682124f, 0.726241f, 0.263273f),
                    new Vector3(0.682418f, -0.772131f, 0.012539f),
                    new Vector3(0.735225f, 0.011733f, -0.65526f),
                    new Vector3(0.73699f, 0.572615f, -0.352754f),
                    new Vector3(0.744944f, 0.524132f, 0.522983f),
                    new Vector3(0.748094f, -0.066715f, 0.761713f),
                    new Vector3(0.833719f, 0.275559f, -0.473771f),
                    new Vector3(0.84355f, 0.21563f, 0.6087f),
                    new Vector3(0.899689f, -0.517397f, 0.024669f),
                    new Vector3(0.932326f, -0.032942f, -0.388054f),
                    new Vector3(0.940279f, -0.081425f, 0.487683f),
                    new Vector3(0.995146f, -0.235051f, -0.128344f),
                    new Vector3(0.998184f, -0.253571f, 0.206158f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            71, () =>
            {
                var faces = new[]
                {
                    new[] { 70, 73, 74 },
                    new[] { 54, 53, 63 },
                    new[] { 60, 64, 68 },
                    new[] { 43, 39, 51 },
                    new[] { 49, 56, 59 },
                    new[] { 28, 27, 37 },
                    new[] { 52, 62, 58 },
                    new[] { 30, 35, 41 },
                    new[] { 65, 71, 67 },
                    new[] { 50, 55, 46 },
                    new[] { 3, 0, 1 },
                    new[] { 20, 19, 10 },
                    new[] { 8, 6, 2 },
                    new[] { 25, 29, 17 },
                    new[] { 22, 15, 11 },
                    new[] { 45, 34, 40 },
                    new[] { 26, 14, 16 },
                    new[] { 48, 47, 38 },
                    new[] { 12, 4, 9 },
                    new[] { 32, 23, 36 },
                    new[] { 13, 12, 23 },
                    new[] { 21, 32, 35 },
                    new[] { 18, 30, 20 },
                    new[] { 3, 7, 10 },
                    new[] { 5, 1, 4 },
                    new[] { 31, 22, 34 },
                    new[] { 42, 45, 53 },
                    new[] { 44, 54, 48 },
                    new[] { 26, 33, 38 },
                    new[] { 24, 16, 15 },
                    new[] { 66, 39, 29 },
                    new[] { 57, 25, 28 },
                    new[] { 61, 37, 49 },
                    new[] { 69, 59, 64 },
                    new[] { 72, 60, 51 },
                    new[] { 13, 5, 4, 12 },
                    new[] { 21, 13, 23, 32 },
                    new[] { 18, 21, 35, 30 },
                    new[] { 7, 18, 20, 10 },
                    new[] { 5, 7, 3, 1 },
                    new[] { 31, 24, 15, 22 },
                    new[] { 42, 31, 34, 45 },
                    new[] { 44, 42, 53, 54 },
                    new[] { 33, 44, 48, 38 },
                    new[] { 24, 33, 26, 16 },
                    new[] { 66, 72, 51, 39 },
                    new[] { 66, 29, 25, 57 },
                    new[] { 57, 28, 37, 61 },
                    new[] { 61, 49, 59, 69 },
                    new[] { 69, 64, 60, 72 },
                    new[] { 5, 13, 21, 18, 7 },
                    new[] { 24, 31, 42, 44, 33 },
                    new[] { 72, 66, 57, 61, 69 },
                    new[] { 68, 64, 59, 56, 58, 62, 67, 71, 74, 73 },
                    new[] { 40, 43, 51, 60, 68, 73, 70, 63, 53, 45 },
                    new[] { 54, 63, 70, 74, 71, 65, 55, 50, 47, 48 },
                    new[] { 20, 30, 41, 52, 58, 56, 49, 37, 27, 19 },
                    new[] { 36, 46, 55, 65, 67, 62, 52, 41, 35, 32 },
                    new[] { 2, 6, 11, 15, 16, 14, 9, 4, 1, 0 },
                    new[] { 28, 25, 17, 8, 2, 0, 3, 10, 19, 27 },
                    new[] { 43, 40, 34, 22, 11, 6, 8, 17, 29, 39 },
                    new[] { 46, 36, 23, 12, 9, 14, 26, 38, 47, 50 }
                };
                var verts = new[]
                {
                    new Vector3(-0.942902f, 0.07027f, -0.149675f),
                    new Vector3(-0.907058f, 0.322313f, 0.065363f),
                    new Vector3(-0.897048f, -0.257164f, -0.191336f),
                    new Vector3(-0.851819f, 0.371393f, -0.259585f),
                    new Vector3(-0.803207f, 0.402693f, 0.37164f),
                    new Vector3(-0.802539f, 0.632087f, 0.129919f),
                    new Vector3(-0.787011f, -0.534922f, -0.043707f),
                    new Vector3(-0.7473f, 0.681167f, -0.195029f),
                    new Vector3(-0.731772f, -0.485841f, -0.368655f),
                    new Vector3(-0.671018f, 0.280706f, 0.65217f),
                    new Vector3(-0.658589f, 0.531186f, -0.479085f),
                    new Vector3(-0.654821f, -0.656908f, 0.236823f),
                    new Vector3(-0.579935f, 0.581829f, 0.542259f),
                    new Vector3(-0.579266f, 0.811224f, 0.300537f),
                    new Vector3(-0.560981f, 0.002949f, 0.799799f),
                    new Vector3(-0.550971f, -0.576528f, 0.5431f),
                    new Vector3(-0.515127f, -0.324486f, 0.758138f),
                    new Vector3(-0.510203f, -0.528414f, -0.613902f),
                    new Vector3(-0.489887f, 0.890637f, -0.225239f),
                    new Vector3(-0.437021f, 0.488613f, -0.724333f),
                    new Vector3(-0.401177f, 0.740656f, -0.509295f),
                    new Vector3(-0.386037f, 0.971017f, 0.081037f),
                    new Vector3(-0.385694f, -0.805205f, 0.365781f),
                    new Vector3(-0.322522f, 0.791299f, 0.512049f),
                    new Vector3(-0.317618f, -0.591958f, 0.780503f),
                    new Vector3(-0.316974f, -0.368621f, -0.833402f),
                    new Vector3(-0.291854f, -0.145349f, 0.928756f),
                    new Vector3(-0.271745f, 0.259936f, -0.901652f),
                    new Vector3(-0.225891f, -0.067498f, -0.943313f),
                    new Vector3(-0.206937f, -0.646378f, -0.685774f),
                    new Vector3(-0.177904f, 0.919793f, -0.338677f),
                    new Vector3(-0.152342f, -0.820635f, 0.603184f),
                    new Vector3(-0.129293f, 0.951092f, 0.292549f),
                    new Vector3(-0.094345f, -0.412822f, 0.951122f),
                    new Vector3(-0.082428f, -0.92317f, 0.29391f),
                    new Vector3(-0.074054f, 1.000172f, -0.032399f),
                    new Vector3(0.002897f, 0.829106f, 0.573078f),
                    new Vector3(0.031522f, 0.141972f, -0.973523f),
                    new Vector3(0.033565f, -0.107542f, 0.989786f),
                    new Vector3(0.06219f, -0.794676f, -0.556816f),
                    new Vector3(0.13914f, -0.965743f, 0.048662f),
                    new Vector3(0.147515f, 0.9576f, -0.277647f),
                    new Vector3(0.173077f, -0.782829f, 0.664213f),
                    new Vector3(0.19438f, -0.916662f, -0.276286f),
                    new Vector3(0.208921f, -0.530786f, 0.87925f),
                    new Vector3(0.242991f, -0.885363f, 0.354939f),
                    new Vector3(0.272024f, 0.680808f, 0.702036f),
                    new Vector3(0.290978f, 0.101928f, 0.959576f),
                    new Vector3(0.336832f, -0.225507f, 0.917915f),
                    new Vector3(0.356941f, 0.179778f, -0.912493f),
                    new Vector3(0.382061f, 0.403051f, 0.849665f),
                    new Vector3(0.387609f, -0.75687f, -0.495786f),
                    new Vector3(0.450781f, 0.839635f, -0.349518f),
                    new Vector3(0.466264f, -0.706226f, 0.525558f),
                    new Vector3(0.502108f, -0.454184f, 0.740596f),
                    new Vector3(0.57529f, 0.562844f, 0.630165f),
                    new Vector3(0.580214f, 0.358915f, -0.741875f),
                    new Vector3(-0.018946f, -0.326587f, -0.976456f),
                    new Vector3(0.616058f, 0.610958f, -0.526838f),
                    new Vector3(0.626068f, 0.031481f, -0.783536f),
                    new Vector3(0.645022f, -0.5474f, -0.525996f),
                    new Vector3(0.238466f, -0.117117f, -1.006666f),
                    new Vector3(0.719908f, 0.691337f, -0.22056f),
                    new Vector3(0.723676f, -0.496756f, 0.495348f),
                    new Vector3(0.736105f, -0.246276f, -0.635907f),
                    new Vector3(0.796859f, 0.520271f, 0.384917f),
                    new Vector3(0.091091f, -0.604344f, -0.828827f),
                    new Vector3(0.852098f, 0.569351f, 0.059969f),
                    new Vector3(0.868294f, -0.368263f, -0.355377f),
                    new Vector3(0.507593f, -0.265415f, -0.877708f),
                    new Vector3(0.916906f, -0.336964f, 0.275848f),
                    new Vector3(0.962135f, 0.291594f, 0.207599f),
                    new Vector3(0.41651f, -0.566538f, -0.767798f),
                    new Vector3(0.972145f, -0.287883f, -0.0491f),
                    new Vector3(1.007989f, -0.035841f, 0.165937f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            72, () =>
            {
                var faces = new[]
                {
                    new[] { 48, 45, 55 },
                    new[] { 35, 42, 47 },
                    new[] { 23, 18, 31 },
                    new[] { 20, 30, 34 },
                    new[] { 49, 57, 51 },
                    new[] { 59, 54, 53 },
                    new[] { 56, 50, 58 },
                    new[] { 46, 41, 52 },
                    new[] { 39, 40, 28 },
                    new[] { 37, 25, 22 },
                    new[] { 43, 44, 32 },
                    new[] { 38, 24, 26 },
                    new[] { 29, 19, 17 },
                    new[] { 9, 1, 7 },
                    new[] { 11, 13, 3 },
                    new[] { 14, 12, 4 },
                    new[] { 36, 21, 33 },
                    new[] { 15, 16, 27 },
                    new[] { 5, 0, 6 },
                    new[] { 2, 10, 8 },
                    new[] { 36, 33, 45, 48 },
                    new[] { 45, 47, 56, 55 },
                    new[] { 47, 42, 50, 56 },
                    new[] { 23, 31, 42, 35 },
                    new[] { 18, 20, 34, 31 },
                    new[] { 34, 30, 41, 46 },
                    new[] { 48, 55, 57, 49 },
                    new[] { 57, 59, 53, 51 },
                    new[] { 53, 54, 44, 43 },
                    new[] { 58, 52, 54, 59 },
                    new[] { 50, 46, 52, 58 },
                    new[] { 41, 30, 24, 38 },
                    new[] { 49, 51, 40, 39 },
                    new[] { 40, 37, 22, 28 },
                    new[] { 22, 25, 13, 11 },
                    new[] { 43, 32, 25, 37 },
                    new[] { 44, 38, 26, 32 },
                    new[] { 26, 24, 12, 14 },
                    new[] { 39, 28, 19, 29 },
                    new[] { 19, 9, 7, 17 },
                    new[] { 7, 1, 0, 5 },
                    new[] { 11, 3, 1, 9 },
                    new[] { 13, 14, 4, 3 },
                    new[] { 4, 12, 10, 2 },
                    new[] { 29, 17, 21, 36 },
                    new[] { 21, 15, 27, 33 },
                    new[] { 27, 16, 23, 35 },
                    new[] { 5, 6, 16, 15 },
                    new[] { 0, 2, 8, 6 },
                    new[] { 8, 10, 20, 18 },
                    new[] { 33, 27, 35, 47, 45 },
                    new[] { 31, 34, 46, 50, 42 },
                    new[] { 55, 56, 58, 59, 57 },
                    new[] { 52, 41, 38, 44, 54 },
                    new[] { 51, 53, 43, 37, 40 },
                    new[] { 32, 26, 14, 13, 25 },
                    new[] { 28, 22, 11, 9, 19 },
                    new[] { 3, 4, 2, 0, 1 },
                    new[] { 17, 7, 5, 15, 21 },
                    new[] { 6, 8, 18, 23, 16 },
                    new[] { 36, 48, 49, 39, 29 },
                    new[] { 10, 12, 24, 30, 20 }
                };
                var verts = new[]
                {
                    new Vector3(-0.976612f, 0.213547f, -0.025026f),
                    new Vector3(-0.942883f, -0.134569f, -0.304734f),
                    new Vector3(-0.917763f, 0.061996f, 0.392261f),
                    new Vector3(-0.863187f, -0.501268f, -0.060316f),
                    new Vector3(-0.847662f, -0.379783f, 0.37045f),
                    new Vector3(-0.806942f, 0.483006f, -0.339927f),
                    new Vector3(-0.791417f, 0.60449f, 0.090839f),
                    new Vector3(-0.773213f, 0.13489f, -0.619635f),
                    new Vector3(-0.732567f, 0.452939f, 0.508126f),
                    new Vector3(-0.703113f, -0.306889f, -0.641445f),
                    new Vector3(-0.652871f, 0.086241f, 0.752543f),
                    new Vector3(-0.623417f, -0.673588f, -0.397028f),
                    new Vector3(-0.582771f, -0.355538f, 0.730732f),
                    new Vector3(-0.564567f, -0.825138f, 0.020258f),
                    new Vector3(-0.549042f, -0.703654f, 0.451025f),
                    new Vector3(-0.47356f, 0.76745f, -0.432161f),
                    new Vector3(-0.458035f, 0.888934f, -0.001394f),
                    new Vector3(-0.418985f, 0.204186f, -0.884738f),
                    new Vector3(-0.362814f, 0.643719f, 0.673789f),
                    new Vector3(-0.348885f, -0.237593f, -0.906548f),
                    new Vector3(-0.283118f, 0.277021f, 0.918206f),
                    new Vector3(-0.23379f, 0.595129f, -0.768872f),
                    new Vector3(-0.219935f, -0.830923f, -0.511073f),
                    new Vector3(-0.193143f, 0.913179f, 0.358888f),
                    new Vector3(-0.169693f, -0.437793f, 0.882916f),
                    new Vector3(-0.161085f, -0.982474f, -0.093786f),
                    new Vector3(-0.135964f, -0.785909f, 0.603208f),
                    new Vector3(-0.103807f, 0.95823f, -0.266497f),
                    new Vector3(-0.050264f, -0.561464f, -0.825973f),
                    new Vector3(-0.015503f, 0.04685f, -0.998782f),
                    new Vector3(0.015502f, -0.04685f, 0.998781f),
                    new Vector3(0.075132f, 0.737294f, 0.671381f),
                    new Vector3(0.103807f, -0.958229f, 0.266497f),
                    new Vector3(0.135964f, 0.78591f, -0.603209f),
                    new Vector3(0.154828f, 0.370596f, 0.915798f),
                    new Vector3(0.161084f, 0.982475f, 0.093785f),
                    new Vector3(0.169693f, 0.437794f, -0.882917f),
                    new Vector3(0.193143f, -0.913178f, -0.358889f),
                    new Vector3(0.233789f, -0.595129f, 0.768871f),
                    new Vector3(0.283118f, -0.277021f, -0.918207f),
                    new Vector3(0.362813f, -0.643719f, -0.67379f),
                    new Vector3(0.418985f, -0.204186f, 0.884737f),
                    new Vector3(0.42936f, 0.80659f, 0.406278f),
                    new Vector3(0.458035f, -0.888933f, 0.001394f),
                    new Vector3(0.47356f, -0.767449f, 0.43216f),
                    new Vector3(0.549042f, 0.703655f, -0.451025f),
                    new Vector3(0.558311f, 0.21326f, 0.801754f),
                    new Vector3(0.564567f, 0.825139f, -0.020259f),
                    new Vector3(0.582771f, 0.355539f, -0.730733f),
                    new Vector3(0.652871f, -0.08624f, -0.752544f),
                    new Vector3(0.727981f, 0.48272f, 0.486853f),
                    new Vector3(0.732567f, -0.452939f, -0.508127f),
                    new Vector3(0.773213f, -0.13489f, 0.619634f),
                    new Vector3(0.791417f, -0.60449f, -0.09084f),
                    new Vector3(0.806942f, -0.483006f, 0.339926f),
                    new Vector3(0.847662f, 0.379784f, -0.370451f),
                    new Vector3(0.863187f, 0.501268f, 0.060316f),
                    new Vector3(0.917763f, -0.061995f, -0.392261f),
                    new Vector3(0.942883f, 0.13457f, 0.304733f),
                    new Vector3(0.976612f, -0.213546f, 0.025025f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            73, () =>
            {
                var faces = new[]
                {
                    new[] { 36, 33, 44 },
                    new[] { 19, 28, 32 },
                    new[] { 9, 10, 22 },
                    new[] { 20, 34, 29 },
                    new[] { 47, 53, 55 },
                    new[] { 57, 58, 59 },
                    new[] { 43, 41, 51 },
                    new[] { 42, 45, 52 },
                    new[] { 49, 50, 37 },
                    new[] { 40, 27, 31 },
                    new[] { 56, 54, 48 },
                    new[] { 46, 35, 38 },
                    new[] { 39, 30, 25 },
                    new[] { 17, 7, 14 },
                    new[] { 18, 16, 8 },
                    new[] { 26, 23, 15 },
                    new[] { 24, 13, 21 },
                    new[] { 5, 3, 11 },
                    new[] { 1, 2, 0 },
                    new[] { 6, 12, 4 },
                    new[] { 24, 21, 33, 36 },
                    new[] { 33, 32, 43, 44 },
                    new[] { 32, 28, 41, 43 },
                    new[] { 9, 22, 28, 19 },
                    new[] { 10, 20, 29, 22 },
                    new[] { 29, 34, 45, 42 },
                    new[] { 36, 44, 53, 47 },
                    new[] { 53, 57, 59, 55 },
                    new[] { 59, 58, 54, 56 },
                    new[] { 51, 52, 58, 57 },
                    new[] { 41, 42, 52, 51 },
                    new[] { 45, 34, 35, 46 },
                    new[] { 47, 55, 49, 39 },
                    new[] { 50, 40, 31, 37 },
                    new[] { 31, 27, 16, 18 },
                    new[] { 56, 48, 40, 50 },
                    new[] { 54, 46, 38, 48 },
                    new[] { 38, 35, 23, 26 },
                    new[] { 49, 37, 30, 39 },
                    new[] { 30, 17, 14, 25 },
                    new[] { 7, 8, 2, 1 },
                    new[] { 18, 8, 7, 17 },
                    new[] { 27, 26, 15, 16 },
                    new[] { 15, 23, 12, 6 },
                    new[] { 25, 14, 13, 24 },
                    new[] { 13, 5, 11, 21 },
                    new[] { 11, 3, 9, 19 },
                    new[] { 1, 0, 3, 5 },
                    new[] { 2, 6, 4, 0 },
                    new[] { 4, 12, 20, 10 },
                    new[] { 21, 11, 19, 32, 33 },
                    new[] { 22, 29, 42, 41, 28 },
                    new[] { 44, 43, 51, 57, 53 },
                    new[] { 52, 45, 46, 54, 58 },
                    new[] { 55, 59, 56, 50, 49 },
                    new[] { 48, 38, 26, 27, 40 },
                    new[] { 37, 31, 18, 17, 30 },
                    new[] { 16, 15, 6, 2, 8 },
                    new[] { 14, 7, 1, 5, 13 },
                    new[] { 0, 4, 10, 9, 3 },
                    new[] { 24, 36, 47, 39, 25 },
                    new[] { 12, 23, 35, 34, 20 }
                };
                var verts = new[]
                {
                    new Vector3(-0.984691f, 0.125192f, 0.121281f),
                    new Vector3(-0.957572f, -0.075612f, -0.278094f),
                    new Vector3(-0.942455f, -0.319982f, 0.096891f),
                    new Vector3(-0.856557f, 0.512005f, -0.064506f),
                    new Vector3(-0.832097f, 0.116607f, 0.542233f),
                    new Vector3(-0.829438f, 0.3112f, -0.463882f),
                    new Vector3(-0.789861f, -0.328568f, 0.517842f),
                    new Vector3(-0.761099f, -0.409107f, -0.503346f),
                    new Vector3(-0.745982f, -0.653477f, -0.128361f),
                    new Vector3(-0.624772f, 0.742482f, 0.241623f),
                    new Vector3(-0.609655f, 0.498112f, 0.616608f),
                    new Vector3(-0.606996f, 0.692705f, -0.389506f),
                    new Vector3(-0.558076f, -0.098091f, 0.823971f),
                    new Vector3(-0.553774f, 0.216768f, -0.803956f),
                    new Vector3(-0.511538f, -0.228406f, -0.828346f),
                    new Vector3(-0.49908f, -0.667369f, 0.552753f),
                    new Vector3(-0.471962f, -0.868174f, 0.153378f),
                    new Vector3(-0.40552f, -0.630422f, -0.661906f),
                    new Vector3(-0.390403f, -0.874792f, -0.286921f),
                    new Vector3(-0.375211f, 0.923183f, -0.083378f),
                    new Vector3(-0.335634f, 0.283415f, 0.898346f),
                    new Vector3(-0.331332f, 0.598273f, -0.729581f),
                    new Vector3(-0.287416f, 0.79711f, 0.531045f),
                    new Vector3(-0.267295f, -0.436892f, 0.858882f),
                    new Vector3(-0.134858f, 0.264778f, -0.954832f),
                    new Vector3(-0.092623f, -0.180396f, -0.979223f),
                    new Vector3(-0.070822f, -0.770387f, 0.63363f),
                    new Vector3(-0.043703f, -0.971192f, 0.234255f),
                    new Vector3(-0.037854f, 0.977811f, 0.206045f),
                    new Vector3(-0.013394f, 0.582413f, 0.812784f),
                    new Vector3(0.013396f, -0.582411f, -0.812783f),
                    new Vector3(0.037856f, -0.977809f, -0.206044f),
                    new Vector3(0.043705f, 0.971193f, -0.234254f),
                    new Vector3(0.070824f, 0.770388f, -0.63363f),
                    new Vector3(0.092624f, 0.180397f, 0.979223f),
                    new Vector3(0.13486f, -0.264777f, 0.954833f),
                    new Vector3(0.267297f, 0.436893f, -0.858881f),
                    new Vector3(0.287417f, -0.797109f, -0.531044f),
                    new Vector3(0.331333f, -0.598272f, 0.729581f),
                    new Vector3(0.335635f, -0.283414f, -0.898346f),
                    new Vector3(0.375212f, -0.923181f, 0.083378f),
                    new Vector3(0.390404f, 0.874793f, 0.286922f),
                    new Vector3(0.405521f, 0.630423f, 0.661907f),
                    new Vector3(0.471963f, 0.868175f, -0.153377f),
                    new Vector3(0.499082f, 0.66737f, -0.552753f),
                    new Vector3(0.511539f, 0.228408f, 0.828347f),
                    new Vector3(0.553775f, -0.216766f, 0.803957f),
                    new Vector3(0.558078f, 0.098092f, -0.823971f),
                    new Vector3(0.606997f, -0.692704f, 0.389507f),
                    new Vector3(0.609656f, -0.498111f, -0.616607f),
                    new Vector3(0.624773f, -0.74248f, -0.241622f),
                    new Vector3(0.745984f, 0.653478f, 0.128361f),
                    new Vector3(0.761101f, 0.409108f, 0.503346f),
                    new Vector3(0.789863f, 0.328569f, -0.517842f),
                    new Vector3(0.82944f, -0.311199f, 0.463882f),
                    new Vector3(0.832099f, -0.116605f, -0.542232f),
                    new Vector3(0.856558f, -0.512003f, 0.064507f),
                    new Vector3(0.942457f, 0.319983f, -0.09689f),
                    new Vector3(0.957574f, 0.075614f, 0.278095f),
                    new Vector3(0.984692f, -0.125191f, -0.121281f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            74, () =>
            {
                var faces = new[]
                {
                    new[] { 31, 25, 37 },
                    new[] { 13, 23, 24 },
                    new[] { 7, 10, 17 },
                    new[] { 20, 32, 29 },
                    new[] { 48, 54, 56 },
                    new[] { 58, 57, 59 },
                    new[] { 36, 35, 47 },
                    new[] { 41, 44, 52 },
                    new[] { 43, 50, 39 },
                    new[] { 49, 42, 38 },
                    new[] { 55, 53, 46 },
                    new[] { 51, 40, 45 },
                    new[] { 27, 26, 15 },
                    new[] { 18, 12, 8 },
                    new[] { 30, 34, 22 },
                    new[] { 33, 28, 21 },
                    new[] { 19, 9, 14 },
                    new[] { 3, 1, 5 },
                    new[] { 2, 4, 0 },
                    new[] { 11, 16, 6 },
                    new[] { 19, 14, 25, 31 },
                    new[] { 25, 24, 36, 37 },
                    new[] { 24, 23, 35, 36 },
                    new[] { 7, 17, 23, 13 },
                    new[] { 10, 20, 29, 17 },
                    new[] { 29, 32, 44, 41 },
                    new[] { 31, 37, 48, 43 },
                    new[] { 54, 58, 59, 56 },
                    new[] { 59, 57, 53, 55 },
                    new[] { 47, 52, 58, 54 },
                    new[] { 35, 41, 52, 47 },
                    new[] { 44, 32, 40, 51 },
                    new[] { 48, 56, 50, 43 },
                    new[] { 50, 49, 38, 39 },
                    new[] { 42, 46, 34, 30 },
                    new[] { 55, 46, 42, 49 },
                    new[] { 57, 51, 45, 53 },
                    new[] { 45, 40, 28, 33 },
                    new[] { 39, 38, 26, 27 },
                    new[] { 26, 18, 8, 15 },
                    new[] { 8, 12, 4, 2 },
                    new[] { 30, 22, 12, 18 },
                    new[] { 34, 33, 21, 22 },
                    new[] { 21, 28, 16, 11 },
                    new[] { 27, 15, 9, 19 },
                    new[] { 9, 3, 5, 14 },
                    new[] { 5, 1, 7, 13 },
                    new[] { 2, 0, 1, 3 },
                    new[] { 4, 11, 6, 0 },
                    new[] { 6, 16, 20, 10 },
                    new[] { 14, 5, 13, 24, 25 },
                    new[] { 17, 29, 41, 35, 23 },
                    new[] { 37, 36, 47, 54, 48 },
                    new[] { 52, 44, 51, 57, 58 },
                    new[] { 56, 59, 55, 49, 50 },
                    new[] { 53, 45, 33, 34, 46 },
                    new[] { 38, 42, 30, 18, 26 },
                    new[] { 22, 21, 11, 4, 12 },
                    new[] { 15, 8, 2, 3, 9 },
                    new[] { 0, 6, 10, 7, 1 },
                    new[] { 19, 31, 43, 39, 27 },
                    new[] { 16, 28, 40, 32, 20 }
                };
                var verts = new[]
                {
                    new Vector3(-0.966046f, -0.051368f, 0.253213f),
                    new Vector3(-0.95229f, 0.30466f, -0.018105f),
                    new Vector3(-0.940892f, -0.323f, -0.101952f),
                    new Vector3(-0.927135f, 0.033028f, -0.37327f),
                    new Vector3(-0.826492f, -0.473879f, 0.30389f),
                    new Vector3(-0.790478f, 0.458215f, -0.40643f),
                    new Vector3(-0.767188f, 0.060532f, 0.63856f),
                    new Vector3(-0.744929f, 0.636598f, 0.199559f),
                    new Vector3(-0.701332f, -0.650609f, -0.291274f),
                    new Vector3(-0.679074f, -0.074543f, -0.730275f),
                    new Vector3(-0.63053f, 0.485719f, 0.6054f),
                    new Vector3(-0.627634f, -0.361979f, 0.689237f),
                    new Vector3(-0.586933f, -0.801488f, 0.114568f),
                    new Vector3(-0.583117f, 0.790154f, -0.188766f),
                    new Vector3(-0.542416f, 0.350644f, -0.763435f),
                    new Vector3(-0.53952f, -0.497054f, -0.679599f),
                    new Vector3(-0.420274f, -0.03004f, 0.9069f),
                    new Vector3(-0.397364f, 0.817911f, 0.416083f),
                    new Vector3(-0.338872f, -0.909059f, -0.242438f),
                    new Vector3(-0.302857f, 0.023036f, -0.952757f),
                    new Vector3(-0.283617f, 0.395147f, 0.87374f),
                    new Vector3(-0.265174f, -0.620429f, 0.738072f),
                    new Vector3(-0.240019f, -0.892061f, 0.382907f),
                    new Vector3(-0.235552f, 0.971466f, 0.027758f),
                    new Vector3(-0.206901f, 0.887732f, -0.411248f),
                    new Vector3(-0.181746f, 0.6161f, -0.766413f),
                    new Vector3(-0.17706f, -0.755504f, -0.630763f),
                    new Vector3(-0.163303f, -0.399475f, -0.902081f),
                    new Vector3(-0.057814f, -0.288491f, 0.955736f),
                    new Vector3(-0.05045f, 0.727338f, 0.684423f),
                    new Vector3(0.008042f, -0.999632f, 0.025902f),
                    new Vector3(0.057813f, 0.288491f, -0.955736f),
                    new Vector3(0.163303f, 0.399477f, 0.902082f),
                    new Vector3(0.181745f, -0.616099f, 0.766414f),
                    new Vector3(0.2069f, -0.887731f, 0.411249f),
                    new Vector3(0.211368f, 0.975796f, 0.0561f),
                    new Vector3(0.240019f, 0.892062f, -0.382907f),
                    new Vector3(0.265173f, 0.62043f, -0.738072f),
                    new Vector3(0.26986f, -0.751174f, -0.602422f),
                    new Vector3(0.283616f, -0.395146f, -0.873739f),
                    new Vector3(0.302857f, -0.023034f, 0.952758f),
                    new Vector3(0.325767f, 0.824917f, 0.461941f),
                    new Vector3(0.384259f, -0.902053f, -0.19658f),
                    new Vector3(0.420273f, 0.030041f, -0.9069f),
                    new Vector3(0.539519f, 0.497055f, 0.6796f),
                    new Vector3(0.542416f, -0.350643f, 0.763436f),
                    new Vector3(0.583117f, -0.790152f, 0.188767f),
                    new Vector3(0.586933f, 0.801489f, -0.114567f),
                    new Vector3(0.627634f, 0.36198f, -0.689236f),
                    new Vector3(0.658347f, -0.625824f, -0.418238f),
                    new Vector3(0.672103f, -0.269795f, -0.689555f),
                    new Vector3(0.679073f, 0.074544f, 0.730276f),
                    new Vector3(0.701332f, 0.65061f, 0.291275f),
                    new Vector3(0.790477f, -0.458214f, 0.40643f),
                    new Vector3(0.826492f, 0.473881f, -0.303889f),
                    new Vector3(0.857205f, -0.513923f, -0.03289f),
                    new Vector3(0.879463f, 0.062143f, -0.471892f),
                    new Vector3(0.927134f, -0.033027f, 0.37327f),
                    new Vector3(0.940891f, 0.323002f, 0.101953f),
                    new Vector3(0.993863f, -0.088736f, -0.06605f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            75, () =>
            {
                var faces = new[]
                {
                    new[] { 30, 24, 37 },
                    new[] { 14, 28, 26 },
                    new[] { 11, 13, 22 },
                    new[] { 25, 38, 31 },
                    new[] { 48, 56, 55 },
                    new[] { 59, 57, 58 },
                    new[] { 39, 41, 50 },
                    new[] { 43, 49, 54 },
                    new[] { 40, 46, 34 },
                    new[] { 45, 36, 32 },
                    new[] { 53, 52, 44 },
                    new[] { 51, 42, 47 },
                    new[] { 21, 19, 10 },
                    new[] { 27, 15, 16 },
                    new[] { 33, 35, 23 },
                    new[] { 29, 20, 17 },
                    new[] { 18, 8, 12 },
                    new[] { 1, 3, 7 },
                    new[] { 5, 4, 0 },
                    new[] { 6, 9, 2 },
                    new[] { 18, 12, 24, 30 },
                    new[] { 24, 26, 39, 37 },
                    new[] { 26, 28, 41, 39 },
                    new[] { 11, 22, 28, 14 },
                    new[] { 13, 25, 31, 22 },
                    new[] { 31, 38, 49, 43 },
                    new[] { 30, 37, 48, 40 },
                    new[] { 56, 59, 58, 55 },
                    new[] { 58, 57, 52, 53 },
                    new[] { 50, 54, 59, 56 },
                    new[] { 41, 43, 54, 50 },
                    new[] { 49, 38, 42, 51 },
                    new[] { 48, 55, 46, 40 },
                    new[] { 46, 45, 32, 34 },
                    new[] { 36, 44, 33, 27 },
                    new[] { 53, 44, 36, 45 },
                    new[] { 57, 51, 47, 52 },
                    new[] { 47, 42, 29, 35 },
                    new[] { 34, 32, 19, 21 },
                    new[] { 19, 16, 5, 10 },
                    new[] { 16, 15, 4, 5 },
                    new[] { 33, 23, 15, 27 },
                    new[] { 35, 29, 17, 23 },
                    new[] { 17, 20, 9, 6 },
                    new[] { 21, 10, 8, 18 },
                    new[] { 8, 1, 7, 12 },
                    new[] { 7, 3, 11, 14 },
                    new[] { 0, 2, 3, 1 },
                    new[] { 4, 6, 2, 0 },
                    new[] { 9, 20, 25, 13 },
                    new[] { 12, 7, 14, 26, 24 },
                    new[] { 22, 31, 43, 41, 28 },
                    new[] { 37, 39, 50, 56, 48 },
                    new[] { 54, 49, 51, 57, 59 },
                    new[] { 55, 58, 53, 45, 46 },
                    new[] { 52, 47, 35, 33, 44 },
                    new[] { 32, 36, 27, 16, 19 },
                    new[] { 23, 17, 6, 4, 15 },
                    new[] { 10, 5, 0, 1, 8 },
                    new[] { 2, 9, 13, 11, 3 },
                    new[] { 18, 30, 40, 34, 21 },
                    new[] { 20, 29, 42, 38, 25 }
                };
                var verts = new[]
                {
                    new Vector3(-0.980376f, -0.197048f, -0.005857f),
                    new Vector3(-0.946332f, 0.150802f, -0.285858f),
                    new Vector3(-0.935804f, 0.079716f, 0.34339f),
                    new Vector3(-0.901759f, 0.427566f, 0.063389f),
                    new Vector3(-0.812124f, -0.53251f, 0.23851f),
                    new Vector3(-0.8048f, -0.555655f, -0.20867f),
                    new Vector3(-0.767552f, -0.255746f, 0.587757f),
                    new Vector3(-0.759203f, 0.553784f, -0.341957f),
                    new Vector3(-0.749715f, 0.007177f, -0.661721f),
                    new Vector3(-0.688108f, 0.168923f, 0.705672f),
                    new Vector3(-0.662244f, -0.429437f, -0.614016f),
                    new Vector3(-0.633023f, 0.731755f, 0.252621f),
                    new Vector3(-0.562586f, 0.41016f, -0.71782f),
                    new Vector3(-0.500979f, 0.571905f, 0.649573f),
                    new Vector3(-0.490467f, 0.857973f, -0.152725f),
                    new Vector3(-0.483464f, -0.835984f, 0.259599f),
                    new Vector3(-0.47614f, -0.859129f, -0.18758f),
                    new Vector3(-0.411344f, -0.388171f, 0.824694f),
                    new Vector3(-0.387011f, 0.051553f, -0.920632f),
                    new Vector3(-0.333584f, -0.732911f, -0.592926f),
                    new Vector3(-0.331901f, 0.036498f, 0.942608f),
                    new Vector3(-0.299539f, -0.385062f, -0.872928f),
                    new Vector3(-0.245677f, 0.868112f, 0.431306f),
                    new Vector3(-0.235768f, -0.746778f, 0.621881f),
                    new Vector3(-0.172335f, 0.625585f, -0.760884f),
                    new Vector3(-0.144772f, 0.43948f, 0.88651f),
                    new Vector3(-0.127762f, 0.902348f, -0.411637f),
                    new Vector3(-0.119933f, -0.991554f, 0.049356f),
                    new Vector3(-0.103121f, 0.994331f, 0.02596f),
                    new Vector3(-0.003241f, -0.266976f, 0.963698f),
                    new Vector3(0.003241f, 0.266978f, -0.963697f),
                    new Vector3(0.11053f, 0.735688f, 0.668243f),
                    new Vector3(0.110728f, -0.787329f, -0.606508f),
                    new Vector3(0.127763f, -0.902347f, 0.411638f),
                    new Vector3(0.144773f, -0.439479f, -0.886509f),
                    new Vector3(0.172335f, -0.625583f, 0.760885f),
                    new Vector3(0.242772f, -0.947178f, -0.209555f),
                    new Vector3(0.271977f, 0.571167f, -0.774465f),
                    new Vector3(0.29954f, 0.385063f, 0.872928f),
                    new Vector3(0.31655f, 0.847931f, -0.425218f),
                    new Vector3(0.331901f, -0.036497f, -0.942608f),
                    new Vector3(0.341191f, 0.939913f, 0.012379f),
                    new Vector3(0.387011f, -0.051552f, 0.920633f),
                    new Vector3(0.473235f, 0.780063f, 0.409331f),
                    new Vector3(0.490467f, -0.857972f, 0.152726f),
                    new Vector3(0.528066f, -0.712229f, -0.462467f),
                    new Vector3(0.562111f, -0.364379f, -0.742468f),
                    new Vector3(0.562587f, -0.410159f, 0.717821f),
                    new Vector3(0.600637f, 0.267692f, -0.753376f),
                    new Vector3(0.662244f, 0.429438f, 0.614017f),
                    new Vector3(0.672757f, 0.715506f, -0.188282f),
                    new Vector3(0.749716f, -0.007176f, 0.661722f),
                    new Vector3(0.759203f, -0.553783f, 0.341958f),
                    new Vector3(0.775762f, -0.623022f, -0.100185f),
                    new Vector3(0.804801f, 0.555656f, 0.208671f),
                    new Vector3(0.830847f, -0.06019f, -0.553237f),
                    new Vector3(0.848333f, 0.356899f, -0.391094f),
                    new Vector3(0.946332f, -0.1508f, 0.285859f),
                    new Vector3(0.96289f, -0.22004f, -0.156284f),
                    new Vector3(0.980377f, 0.197049f, 0.005858f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            76, () =>
            {
                var faces = new[]
                {
                    new[] { 26, 17, 18 },
                    new[] { 6, 1, 9 },
                    new[] { 3, 5, 0 },
                    new[] { 8, 10, 2 },
                    new[] { 35, 28, 37 },
                    new[] { 23, 24, 34 },
                    new[] { 11, 4, 15 },
                    new[] { 7, 13, 16 },
                    new[] { 44, 46, 52 },
                    new[] { 49, 51, 54 },
                    new[] { 40, 32, 42 },
                    new[] { 27, 25, 36 },
                    new[] { 53, 48, 45 },
                    new[] { 39, 30, 38 },
                    new[] { 29, 21, 19 },
                    new[] { 31, 20, 17, 26 },
                    new[] { 17, 6, 9, 18 },
                    new[] { 9, 1, 4, 11 },
                    new[] { 3, 0, 1, 6 },
                    new[] { 5, 8, 2, 0 },
                    new[] { 2, 10, 13, 7 },
                    new[] { 26, 18, 28, 35 },
                    new[] { 28, 23, 34, 37 },
                    new[] { 34, 24, 32, 40 },
                    new[] { 11, 15, 24, 23 },
                    new[] { 4, 7, 16, 15 },
                    new[] { 16, 13, 25, 27 },
                    new[] { 35, 37, 46, 44 },
                    new[] { 46, 49, 54, 52 },
                    new[] { 54, 51, 48, 53 },
                    new[] { 40, 42, 51, 49 },
                    new[] { 32, 27, 36, 42 },
                    new[] { 36, 25, 30, 39 },
                    new[] { 44, 52, 47, 41 },
                    new[] { 53, 45, 43, 50 },
                    new[] { 48, 39, 38, 45 },
                    new[] { 38, 30, 21, 29 },
                    new[] { 12, 14, 5, 3 },
                    new[] { 33, 29, 19, 22 },
                    new[] { 19, 21, 10, 8 },
                    new[] { 20, 12, 3, 6, 17 },
                    new[] { 0, 2, 7, 4, 1 },
                    new[] { 18, 9, 11, 23, 28 },
                    new[] { 15, 16, 27, 32, 24 },
                    new[] { 37, 34, 40, 49, 46 },
                    new[] { 42, 36, 39, 48, 51 },
                    new[] { 52, 54, 53, 50, 47 },
                    new[] { 45, 38, 29, 33, 43 },
                    new[] { 22, 19, 8, 5, 14 },
                    new[] { 31, 26, 35, 44, 41 },
                    new[] { 21, 30, 25, 13, 10 },
                    new[] { 41, 47, 50, 43, 33, 22, 14, 12, 20, 31 }
                };
                var verts = new[]
                {
                    new Vector3(-0.975594f, -0.152802f, -0.163647f),
                    new Vector3(-0.930864f, 0.290583f, -0.105382f),
                    new Vector3(-0.885816f, -0.354103f, 0.228021f),
                    new Vector3(-0.840234f, -0.110417f, -0.590106f),
                    new Vector3(-0.813441f, 0.363309f, 0.322296f),
                    new Vector3(-0.812393f, -0.508854f, -0.384052f),
                    new Vector3(-0.795504f, 0.332968f, -0.531841f),
                    new Vector3(-0.7856f, -0.035128f, 0.52835f),
                    new Vector3(-0.722614f, -0.710155f, 0.007616f),
                    new Vector3(-0.695288f, 0.651943f, -0.231511f),
                    new Vector3(-0.605191f, -0.637428f, 0.435294f),
                    new Vector3(-0.577865f, 0.724669f, 0.196167f),
                    new Vector3(-0.531438f, -0.243136f, -0.888462f),
                    new Vector3(-0.504975f, -0.318454f, 0.735624f),
                    new Vector3(-0.503596f, -0.641573f, -0.682408f),
                    new Vector3(-0.488086f, 0.523369f, 0.587835f),
                    new Vector3(-0.460245f, 0.124931f, 0.79389f),
                    new Vector3(-0.459063f, 0.474276f, -0.794187f),
                    new Vector3(-0.358847f, 0.793251f, -0.493857f),
                    new Vector3(-0.358332f, -0.967284f, -0.048676f),
                    new Vector3(-0.295861f, 0.118224f, -1.014591f),
                    new Vector3(-0.240909f, -0.894558f, 0.379002f),
                    new Vector3(-0.222972f, -0.924899f, -0.475135f),
                    new Vector3(-0.168853f, 0.910925f, 0.19814f),
                    new Vector3(-0.079074f, 0.709624f, 0.589809f),
                    new Vector3(-0.078756f, -0.378446f, 0.864946f),
                    new Vector3(-0.050051f, 0.660532f, -0.792214f),
                    new Vector3(-0.034026f, 0.064939f, 0.923211f),
                    new Vector3(-0.033492f, 0.95331f, -0.228318f),
                    new Vector3(0.067887f, -1.027277f, 0.080646f),
                    new Vector3(0.084446f, -0.734498f, 0.644541f),
                    new Vector3(0.113151f, 0.30448f, -1.012618f),
                    new Vector3(0.201551f, 0.426299f, 0.797082f),
                    new Vector3(0.203247f, -0.984891f, -0.345813f),
                    new Vector3(0.257366f, 0.850932f, 0.327462f),
                    new Vector3(0.275304f, 0.820591f, -0.526675f),
                    new Vector3(0.330256f, -0.192191f, 0.866919f),
                    new Vector3(0.392727f, 0.893318f, -0.098997f),
                    new Vector3(0.393242f, -0.867217f, 0.346185f),
                    new Vector3(0.493458f, -0.548243f, 0.646514f),
                    new Vector3(0.537991f, 0.567607f, 0.534736f),
                    new Vector3(0.53937f, 0.244487f, -0.883296f),
                    new Vector3(0.565833f, 0.169169f, 0.74079f),
                    new Vector3(0.61226f, -0.798636f, -0.34384f),
                    new Vector3(0.639586f, 0.563462f, -0.582967f),
                    new Vector3(0.729683f, -0.725909f, 0.083838f),
                    new Vector3(0.757009f, 0.636188f, -0.155289f),
                    new Vector3(0.819995f, -0.038838f, -0.676023f),
                    new Vector3(0.829899f, -0.406935f, 0.384168f),
                    new Vector3(0.846787f, 0.434888f, 0.236379f),
                    new Vector3(0.847836f, -0.437276f, -0.469969f),
                    new Vector3(0.874629f, 0.03645f, 0.442434f),
                    new Vector3(0.920211f, 0.280136f, -0.375693f),
                    new Vector3(0.965259f, -0.364549f, -0.042291f),
                    new Vector3(1.009989f, 0.078836f, 0.015975f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            77, () =>
            {
                var faces = new[]
                {
                    new[] { 27, 16, 19 },
                    new[] { 7, 1, 9 },
                    new[] { 5, 6, 0 },
                    new[] { 8, 10, 2 },
                    new[] { 36, 26, 38 },
                    new[] { 11, 18, 24 },
                    new[] { 3, 4, 12 },
                    new[] { 13, 25, 20 },
                    new[] { 44, 46, 52 },
                    new[] { 48, 49, 54 },
                    new[] { 34, 30, 40 },
                    new[] { 31, 35, 41 },
                    new[] { 53, 47, 45 },
                    new[] { 39, 28, 37 },
                    new[] { 29, 21, 17 },
                    new[] { 32, 22, 16, 27 },
                    new[] { 16, 7, 9, 19 },
                    new[] { 9, 1, 3, 11 },
                    new[] { 5, 0, 1, 7 },
                    new[] { 6, 8, 2, 0 },
                    new[] { 2, 10, 13, 4 },
                    new[] { 27, 19, 26, 36 },
                    new[] { 26, 24, 34, 38 },
                    new[] { 24, 18, 30, 34 },
                    new[] { 3, 12, 18, 11 },
                    new[] { 4, 13, 20, 12 },
                    new[] { 20, 25, 35, 31 },
                    new[] { 36, 38, 46, 44 },
                    new[] { 46, 48, 54, 52 },
                    new[] { 54, 49, 47, 53 },
                    new[] { 40, 41, 49, 48 },
                    new[] { 30, 31, 41, 40 },
                    new[] { 35, 25, 28, 39 },
                    new[] { 44, 52, 50, 42 },
                    new[] { 53, 45, 43, 51 },
                    new[] { 47, 39, 37, 45 },
                    new[] { 37, 28, 21, 29 },
                    new[] { 14, 15, 6, 5 },
                    new[] { 33, 29, 17, 23 },
                    new[] { 17, 21, 10, 8 },
                    new[] { 22, 14, 5, 7, 16 },
                    new[] { 0, 2, 4, 3, 1 },
                    new[] { 19, 9, 11, 24, 26 },
                    new[] { 12, 20, 31, 30, 18 },
                    new[] { 38, 34, 40, 48, 46 },
                    new[] { 41, 35, 39, 47, 49 },
                    new[] { 52, 54, 53, 51, 50 },
                    new[] { 45, 37, 29, 33, 43 },
                    new[] { 23, 17, 8, 6, 15 },
                    new[] { 32, 27, 36, 44, 42 },
                    new[] { 21, 28, 25, 13, 10 },
                    new[] { 42, 50, 51, 43, 33, 23, 15, 14, 22, 32 }
                };
                var verts = new[]
                {
                    new Vector3(-0.965273f, -0.178368f, -0.195819f),
                    new Vector3(-0.921193f, 0.268836f, -0.202934f),
                    new Vector3(-0.899923f, -0.317663f, 0.226452f),
                    new Vector3(-0.828599f, 0.405929f, 0.214939f),
                    new Vector3(-0.815454f, 0.043453f, 0.480314f),
                    new Vector3(-0.804857f, -0.20085f, -0.615041f),
                    new Vector3(-0.791711f, -0.563327f, -0.349666f),
                    new Vector3(-0.760776f, 0.246354f, -0.622156f),
                    new Vector3(-0.726361f, -0.702622f, 0.072605f),
                    new Vector3(-0.676307f, 0.60747f, -0.368293f),
                    new Vector3(-0.633767f, -0.565529f, 0.490477f),
                    new Vector3(-0.583713f, 0.744563f, 0.049579f),
                    new Vector3(-0.582414f, 0.411487f, 0.590902f),
                    new Vector3(-0.549298f, -0.204413f, 0.74434f),
                    new Vector3(-0.479947f, -0.376522f, -0.871086f),
                    new Vector3(-0.466801f, -0.738998f, -0.605711f),
                    new Vector3(-0.408623f, 0.347071f, -0.882598f),
                    new Vector3(-0.361062f, -0.964382f, 0.077537f),
                    new Vector3(-0.337528f, 0.750121f, 0.425542f),
                    new Vector3(-0.324153f, 0.708187f, -0.628736f),
                    new Vector3(-0.316259f, 0.163621f, 0.854928f),
                    new Vector3(-0.268468f, -0.827289f, 0.49541f),
                    new Vector3(-0.235061f, -0.037888f, -1.036445f),
                    new Vector3(-0.200645f, -0.986864f, -0.341685f),
                    new Vector3(-0.174334f, 0.930008f, 0.047397f),
                    new Vector3(-0.131794f, -0.242991f, 0.906168f),
                    new Vector3(-0.013917f, 0.907526f, -0.371825f),
                    new Vector3(0.000756f, 0.532515f, -0.88478f),
                    new Vector3(0.041768f, -0.62795f, 0.752321f),
                    new Vector3(0.056442f, -1.00296f, 0.239365f),
                    new Vector3(0.079975f, 0.711542f, 0.58737f),
                    new Vector3(0.093121f, 0.349066f, 0.852745f),
                    new Vector3(0.174319f, 0.147557f, -1.038628f),
                    new Vector3(0.216858f, -1.025442f, -0.179857f),
                    new Vector3(0.24317f, 0.89143f, 0.209225f),
                    new Vector3(0.277585f, -0.057546f, 0.903985f),
                    new Vector3(0.310993f, 0.731855f, -0.62787f),
                    new Vector3(0.366678f, -0.803621f, 0.496276f),
                    new Vector3(0.403586f, 0.868948f, -0.209997f),
                    new Vector3(0.451147f, -0.442505f, 0.750138f),
                    new Vector3(0.509326f, 0.643564f, 0.473251f),
                    new Vector3(0.522471f, 0.281088f, 0.738626f),
                    new Vector3(0.591822f, 0.108979f, -0.8768f),
                    new Vector3(0.626237f, -0.839998f, -0.182039f),
                    new Vector3(0.676292f, 0.470095f, -0.622937f),
                    new Vector3(0.718831f, -0.702904f, 0.235834f),
                    new Vector3(0.768885f, 0.607188f, -0.205064f),
                    new Vector3(0.8033f, -0.341788f, 0.489696f),
                    new Vector3(0.834235f, 0.467893f, 0.217206f),
                    new Vector3(0.847381f, 0.105416f, 0.482581f),
                    new Vector3(0.857978f, -0.138887f, -0.612774f),
                    new Vector3(0.871123f, -0.501364f, -0.347399f),
                    new Vector3(0.942447f, 0.222229f, -0.358911f),
                    new Vector3(0.963717f, -0.36427f, 0.070474f),
                    new Vector3(1.007798f, 0.082934f, 0.063359f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            78, () =>
            {
                var faces = new[]
                {
                    new[] { 29, 19, 26 },
                    new[] { 10, 6, 16 },
                    new[] { 4, 1, 0 },
                    new[] { 3, 5, 2 },
                    new[] { 40, 35, 44 },
                    new[] { 30, 32, 41 },
                    new[] { 20, 11, 22 },
                    new[] { 7, 13, 17 },
                    new[] { 49, 51, 52 },
                    new[] { 45, 48, 53 },
                    new[] { 36, 27, 37 },
                    new[] { 23, 25, 33 },
                    new[] { 54, 50, 47 },
                    new[] { 39, 28, 38 },
                    new[] { 18, 15, 9 },
                    new[] { 31, 21, 19, 29 },
                    new[] { 19, 10, 16, 26 },
                    new[] { 16, 6, 11, 20 },
                    new[] { 4, 0, 6, 10 },
                    new[] { 1, 3, 2, 0 },
                    new[] { 2, 5, 13, 7 },
                    new[] { 29, 26, 35, 40 },
                    new[] { 35, 30, 41, 44 },
                    new[] { 41, 32, 36, 45 },
                    new[] { 20, 22, 32, 30 },
                    new[] { 11, 7, 17, 22 },
                    new[] { 17, 13, 23, 27 },
                    new[] { 40, 44, 51, 49 },
                    new[] { 51, 53, 54, 52 },
                    new[] { 53, 48, 50, 54 },
                    new[] { 36, 37, 48, 45 },
                    new[] { 27, 23, 33, 37 },
                    new[] { 33, 25, 28, 39 },
                    new[] { 49, 52, 46, 42 },
                    new[] { 47, 38, 34, 43 },
                    new[] { 50, 39, 38, 47 },
                    new[] { 28, 25, 15, 18 },
                    new[] { 12, 8, 1, 4 },
                    new[] { 24, 18, 9, 14 },
                    new[] { 9, 15, 5, 3 },
                    new[] { 21, 12, 4, 10, 19 },
                    new[] { 0, 2, 7, 11, 6 },
                    new[] { 26, 16, 20, 30, 35 },
                    new[] { 22, 17, 27, 36, 32 },
                    new[] { 44, 41, 45, 53, 51 },
                    new[] { 37, 33, 39, 50, 48 },
                    new[] { 52, 54, 47, 43, 46 },
                    new[] { 38, 28, 18, 24, 34 },
                    new[] { 14, 9, 3, 1, 8 },
                    new[] { 31, 29, 40, 49, 42 },
                    new[] { 15, 25, 23, 13, 5 },
                    new[] { 42, 46, 43, 34, 24, 14, 8, 12, 21, 31 }
                };
                var verts = new[]
                {
                    new Vector3(-0.969175f, 0.215371f, -0.127391f),
                    new Vector3(-0.952173f, -0.166819f, -0.363246f),
                    new Vector3(-0.946899f, -0.019645f, 0.255045f),
                    new Vector3(-0.929897f, -0.401835f, 0.01919f),
                    new Vector3(-0.840084f, 0.222491f, -0.557822f),
                    new Vector3(-0.781766f, -0.392789f, 0.443408f),
                    new Vector3(-0.765536f, 0.610272f, -0.059785f),
                    new Vector3(-0.729493f, 0.230008f, 0.559009f),
                    new Vector3(-0.721025f, -0.390315f, -0.677262f),
                    new Vector3(-0.684982f, -0.770578f, -0.058468f),
                    new Vector3(-0.636445f, 0.617392f, -0.490216f),
                    new Vector3(-0.617404f, 0.619318f, 0.364433f),
                    new Vector3(-0.608936f, -0.001005f, -0.871838f),
                    new Vector3(-0.56436f, -0.143136f, 0.747372f),
                    new Vector3(-0.555892f, -0.763458f, -0.488899f),
                    new Vector3(-0.536851f, -0.761533f, 0.36575f),
                    new Vector3(-0.419039f, 0.867044f, -0.186253f),
                    new Vector3(-0.360721f, 0.251764f, 0.814977f),
                    new Vector3(-0.305703f, -0.985028f, 0.051734f),
                    new Vector3(-0.279442f, 0.637958f, -0.76245f),
                    new Vector3(-0.270908f, 0.87609f, 0.237966f),
                    new Vector3(-0.26244f, 0.255768f, -0.998305f),
                    new Vector3(-0.248632f, 0.641074f, 0.620402f),
                    new Vector3(-0.185081f, -0.357586f, 0.857574f),
                    new Vector3(-0.176613f, -0.977908f, -0.378698f),
                    new Vector3(-0.168079f, -0.739776f, 0.621718f),
                    new Vector3(-0.062036f, 0.887611f, -0.458486f),
                    new Vector3(0.018558f, 0.037315f, 0.925179f),
                    new Vector3(0.063069f, -0.963271f, 0.307702f),
                    new Vector3(0.169112f, 0.664116f, -0.772503f),
                    new Vector3(0.177646f, 0.902248f, 0.227913f),
                    new Vector3(0.186114f, 0.281925f, -1.008358f),
                    new Vector3(0.199922f, 0.667232f, 0.610349f),
                    new Vector3(0.211888f, -0.546211f, 0.763647f),
                    new Vector3(0.271941f, -0.951751f, -0.38875f),
                    new Vector3(0.306736f, 0.909368f, -0.202518f),
                    new Vector3(0.365054f, 0.294088f, 0.798712f),
                    new Vector3(0.415527f, -0.151311f, 0.831252f),
                    new Vector3(0.420072f, -0.942705f, 0.035468f),
                    new Vector3(0.443036f, -0.769707f, 0.44963f),
                    new Vector3(0.537884f, 0.685872f, -0.516534f),
                    new Vector3(0.556925f, 0.687798f, 0.338115f),
                    new Vector3(0.565393f, 0.067476f, -0.898156f),
                    new Vector3(0.618437f, -0.694978f, -0.515217f),
                    new Vector3(0.686015f, 0.694918f, -0.092316f),
                    new Vector3(0.722058f, 0.314654f, 0.526478f),
                    new Vector3(0.730526f, -0.305668f, -0.709793f),
                    new Vector3(0.766569f, -0.685932f, -0.090999f),
                    new Vector3(0.77253f, -0.130744f, 0.559019f),
                    new Vector3(0.782799f, 0.317129f, -0.594192f),
                    new Vector3(0.789532f, -0.512934f, 0.323163f),
                    new Vector3(0.93093f, 0.326175f, -0.169974f),
                    new Vector3(0.947932f, -0.056015f, -0.405829f),
                    new Vector3(0.953206f, 0.091159f, 0.212462f),
                    new Vector3(0.970208f, -0.291031f, -0.023393f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            79, () =>
            {
                var faces = new[]
                {
                    new[] { 7, 17, 16 },
                    new[] { 30, 41, 28 },
                    new[] { 18, 25, 31 },
                    new[] { 37, 47, 42 },
                    new[] { 4, 11, 6 },
                    new[] { 23, 27, 15 },
                    new[] { 34, 44, 40 },
                    new[] { 50, 53, 51 },
                    new[] { 5, 12, 8 },
                    new[] { 35, 36, 24 },
                    new[] { 45, 52, 46 },
                    new[] { 54, 48, 49 },
                    new[] { 19, 29, 20 },
                    new[] { 39, 38, 26 },
                    new[] { 33, 43, 32 },
                    new[] { 0, 2, 7, 4 },
                    new[] { 17, 30, 28, 16 },
                    new[] { 28, 41, 44, 34 },
                    new[] { 18, 31, 30, 17 },
                    new[] { 25, 37, 42, 31 },
                    new[] { 42, 47, 53, 50 },
                    new[] { 7, 16, 11, 4 },
                    new[] { 11, 23, 15, 6 },
                    new[] { 27, 40, 45, 35 },
                    new[] { 34, 40, 27, 23 },
                    new[] { 41, 50, 51, 44 },
                    new[] { 51, 53, 54, 52 },
                    new[] { 6, 15, 12, 5 },
                    new[] { 12, 24, 19, 8 },
                    new[] { 24, 36, 29, 19 },
                    new[] { 45, 46, 36, 35 },
                    new[] { 52, 54, 49, 46 },
                    new[] { 49, 48, 38, 39 },
                    new[] { 5, 8, 3, 1 },
                    new[] { 20, 26, 14, 10 },
                    new[] { 29, 39, 26, 20 },
                    new[] { 38, 48, 43, 33 },
                    new[] { 9, 13, 25, 18 },
                    new[] { 22, 33, 32, 21 },
                    new[] { 32, 43, 47, 37 },
                    new[] { 2, 9, 18, 17, 7 },
                    new[] { 31, 42, 50, 41, 30 },
                    new[] { 16, 28, 34, 23, 11 },
                    new[] { 44, 51, 52, 45, 40 },
                    new[] { 15, 27, 35, 24, 12 },
                    new[] { 46, 49, 39, 29, 36 },
                    new[] { 8, 19, 20, 10, 3 },
                    new[] { 26, 38, 33, 22, 14 },
                    new[] { 21, 32, 37, 25, 13 },
                    new[] { 0, 4, 6, 5, 1 },
                    new[] { 43, 48, 54, 53, 47 },
                    new[] { 1, 3, 10, 14, 22, 21, 13, 9, 2, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-1.035663f, 0.17951f, 0.161516f),
                    new Vector3(-1.027893f, 0.048482f, -0.268318f),
                    new Vector3(-0.891113f, 0.078842f, 0.574986f),
                    new Vector3(-0.870771f, -0.264193f, -0.550334f),
                    new Vector3(-0.833419f, 0.579967f, 0.188311f),
                    new Vector3(-0.820848f, 0.367959f, -0.507174f),
                    new Vector3(-0.700657f, 0.696435f, -0.224962f),
                    new Vector3(-0.68887f, 0.479299f, 0.601781f),
                    new Vector3(-0.663727f, 0.055284f, -0.78919f),
                    new Vector3(-0.649458f, -0.215069f, 0.814161f),
                    new Vector3(-0.624314f, -0.639084f, -0.576811f),
                    new Vector3(-0.485083f, 0.858791f, 0.134418f),
                    new Vector3(-0.461483f, 0.455741f, -0.762395f),
                    new Vector3(-0.403f, -0.58996f, 0.787683f),
                    new Vector3(-0.382659f, -0.932996f, -0.337637f),
                    new Vector3(-0.341293f, 0.784217f, -0.480183f),
                    new Vector3(-0.340534f, 0.758124f, 0.547888f),
                    new Vector3(-0.322222f, 0.432884f, 0.857516f),
                    new Vector3(-0.297863f, 0.00374f, 0.988774f),
                    new Vector3(-0.289308f, -0.122159f, -0.96329f),
                    new Vector3(-0.26495f, -0.551303f, -0.832032f),
                    new Vector3(-0.245879f, -0.902636f, 0.505667f),
                    new Vector3(-0.238109f, -1.033664f, 0.075833f),
                    new Vector3(-0.125719f, 0.946573f, -0.120803f),
                    new Vector3(-0.087065f, 0.278298f, -0.936494f),
                    new Vector3(-0.051406f, -0.371151f, 0.962296f),
                    new Vector3(-0.023294f, -0.845214f, -0.592857f),
                    new Vector3(0.107408f, 0.809782f, -0.479865f),
                    new Vector3(0.108167f, 0.783689f, 0.548206f),
                    new Vector3(0.110226f, -0.32589f, -0.934099f),
                    new Vector3(0.126479f, 0.458449f, 0.857834f),
                    new Vector3(0.150837f, 0.029306f, 0.989092f),
                    new Vector3(0.202822f, -0.87707f, 0.505985f),
                    new Vector3(0.210592f, -1.008098f, 0.076151f),
                    new Vector3(0.24093f, 0.900157f, 0.134932f),
                    new Vector3(0.264529f, 0.497107f, -0.761881f),
                    new Vector3(0.312469f, 0.074567f, -0.907304f),
                    new Vector3(0.323013f, -0.548594f, 0.788197f),
                    new Vector3(0.343354f, -0.89163f, -0.337123f),
                    new Vector3(0.351882f, -0.619801f, -0.694925f),
                    new Vector3(0.474057f, 0.763367f, -0.224131f),
                    new Vector3(0.485843f, 0.546231f, 0.602613f),
                    new Vector3(0.525256f, -0.148138f, 0.814992f),
                    new Vector3(0.562186f, -0.789288f, 0.250764f),
                    new Vector3(0.618606f, 0.662699f, 0.189339f),
                    new Vector3(0.631178f, 0.450691f, -0.506147f),
                    new Vector3(0.679118f, 0.028151f, -0.651569f),
                    new Vector3(0.682377f, -0.460813f, 0.532976f),
                    new Vector3(0.694949f, -0.672821f, -0.16251f),
                    new Vector3(0.703476f, -0.400992f, -0.520312f),
                    new Vector3(0.7323f, 0.17134f, 0.576135f),
                    new Vector3(0.865063f, 0.287808f, 0.162862f),
                    new Vector3(0.872833f, 0.15678f, -0.266972f),
                    new Vector3(0.889422f, -0.141336f, 0.29412f),
                    new Vector3(0.897192f, -0.272364f, -0.135714f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            80, () =>
            {
                var faces = new[]
                {
                    new[] { 12, 10, 4 },
                    new[] { 8, 6, 2 },
                    new[] { 16, 26, 14 },
                    new[] { 31, 29, 17 },
                    new[] { 11, 3, 9 },
                    new[] { 20, 18, 32 },
                    new[] { 23, 33, 35 },
                    new[] { 43, 41, 47 },
                    new[] { 39, 37, 45 },
                    new[] { 46, 38, 40 },
                    new[] { 22, 19, 10, 12 },
                    new[] { 10, 8, 2, 4 },
                    new[] { 2, 6, 5, 0 },
                    new[] { 16, 14, 6, 8 },
                    new[] { 26, 31, 17, 14 },
                    new[] { 17, 29, 21, 13 },
                    new[] { 12, 4, 3, 11 },
                    new[] { 3, 1, 7, 9 },
                    new[] { 11, 9, 18, 20 },
                    new[] { 18, 23, 35, 32 },
                    new[] { 35, 33, 41, 43 },
                    new[] { 15, 24, 33, 23 },
                    new[] { 30, 27, 37, 39 },
                    new[] { 20, 32, 36, 28 },
                    new[] { 43, 47, 49, 44 },
                    new[] { 41, 39, 45, 47 },
                    new[] { 45, 37, 38, 46 },
                    new[] { 25, 34, 26, 16 },
                    new[] { 48, 46, 40, 42 },
                    new[] { 40, 38, 29, 31 },
                    new[] { 19, 25, 16, 8, 10 },
                    new[] { 14, 17, 13, 5, 6 },
                    new[] { 4, 2, 0, 1, 3 },
                    new[] { 9, 7, 15, 23, 18 },
                    new[] { 24, 30, 39, 41, 33 },
                    new[] { 32, 35, 43, 44, 36 },
                    new[] { 47, 45, 46, 48, 49 },
                    new[] { 42, 40, 31, 26, 34 },
                    new[] { 22, 12, 11, 20, 28 },
                    new[] { 38, 37, 27, 21, 29 },
                    new[] { 24, 15, 7, 1, 0, 5, 13, 21, 27, 30 },
                    new[] { 28, 36, 44, 49, 48, 42, 34, 25, 19, 22 }
                };
                var verts = new[]
                {
                    new Vector3(-0.981435f, -0.157408f, -0.109585f),
                    new Vector3(-0.942153f, -0.069788f, 0.327838f),
                    new Vector3(-0.91551f, 0.22466f, -0.333721f),
                    new Vector3(-0.85195f, 0.366431f, 0.374044f),
                    new Vector3(-0.835484f, 0.54841f, -0.034822f),
                    new Vector3(-0.80426f, -0.38784f, -0.450273f),
                    new Vector3(-0.738335f, -0.005772f, -0.67441f),
                    new Vector3(-0.701418f, -0.15845f, 0.694914f),
                    new Vector3(-0.648132f, 0.430447f, -0.628204f),
                    new Vector3(-0.611216f, 0.27777f, 0.74112f),
                    new Vector3(-0.568107f, 0.754197f, -0.329305f),
                    new Vector3(-0.545291f, 0.659837f, 0.516984f),
                    new Vector3(-0.528825f, 0.841816f, 0.108118f),
                    new Vector3(-0.478303f, -0.673069f, -0.564096f),
                    new Vector3(-0.371634f, -0.05487f, -0.926756f),
                    new Vector3(-0.351184f, -0.389527f, 0.851433f),
                    new Vector3(-0.281432f, 0.381349f, -0.88055f),
                    new Vector3(-0.210926f, -0.467282f, -0.858579f),
                    new Vector3(-0.205234f, 0.316291f, 0.926196f),
                    new Vector3(-0.151948f, 0.905188f, -0.396922f),
                    new Vector3(-0.139309f, 0.698359f, 0.70206f),
                    new Vector3(-0.128068f, -0.904145f, -0.407577f),
                    new Vector3(-0.112666f, 0.992807f, 0.040501f),
                    new Vector3(-0.044525f, -0.09612f, 0.994373f),
                    new Vector3(-0.025227f, -0.674755f, 0.73761f),
                    new Vector3(0.025227f, 0.674755f, -0.73761f),
                    new Vector3(0.044525f, 0.096121f, -0.994373f),
                    new Vector3(0.112666f, -0.992807f, -0.040501f),
                    new Vector3(0.128068f, 0.904146f, 0.407577f),
                    new Vector3(0.139309f, -0.698358f, -0.70206f),
                    new Vector3(0.151948f, -0.905188f, 0.396922f),
                    new Vector3(0.205234f, -0.316291f, -0.926196f),
                    new Vector3(0.210926f, 0.467282f, 0.858579f),
                    new Vector3(0.281432f, -0.381349f, 0.88055f),
                    new Vector3(0.351184f, 0.389527f, -0.851433f),
                    new Vector3(0.371634f, 0.05487f, 0.926756f),
                    new Vector3(0.478303f, 0.673069f, 0.564096f),
                    new Vector3(0.528825f, -0.841816f, -0.108118f),
                    new Vector3(0.545291f, -0.659837f, -0.516984f),
                    new Vector3(0.568107f, -0.754197f, 0.329305f),
                    new Vector3(0.611216f, -0.277769f, -0.74112f),
                    new Vector3(0.648133f, -0.430447f, 0.628204f),
                    new Vector3(0.701419f, 0.15845f, -0.694914f),
                    new Vector3(0.738335f, 0.005773f, 0.67441f),
                    new Vector3(0.80426f, 0.387841f, 0.450273f),
                    new Vector3(0.835484f, -0.54841f, 0.034822f),
                    new Vector3(0.85195f, -0.366431f, -0.374044f),
                    new Vector3(0.91551f, -0.22466f, 0.333721f),
                    new Vector3(0.942153f, 0.069789f, -0.327838f),
                    new Vector3(0.981435f, 0.157408f, 0.109585f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            81, () =>
            {
                var faces = new[]
                {
                    new[] { 35, 29, 25 },
                    new[] { 21, 11, 15 },
                    new[] { 24, 22, 14 },
                    new[] { 17, 12, 8 },
                    new[] { 33, 23, 27 },
                    new[] { 13, 7, 16 },
                    new[] { 6, 3, 2 },
                    new[] { 1, 4, 0 },
                    new[] { 42, 36, 45 },
                    new[] { 30, 19, 26 },
                    new[] { 44, 39, 29, 35 },
                    new[] { 29, 21, 15, 25 },
                    new[] { 15, 11, 3, 6 },
                    new[] { 24, 14, 11, 21 },
                    new[] { 22, 17, 8, 14 },
                    new[] { 8, 12, 4, 1 },
                    new[] { 35, 25, 23, 33 },
                    new[] { 23, 13, 16, 27 },
                    new[] { 16, 7, 10, 20 },
                    new[] { 6, 2, 7, 13 },
                    new[] { 3, 1, 0, 2 },
                    new[] { 0, 4, 9, 5 },
                    new[] { 33, 27, 36, 42 },
                    new[] { 36, 31, 40, 45 },
                    new[] { 42, 45, 49, 47 },
                    new[] { 43, 38, 46, 48 },
                    new[] { 28, 18, 19, 30 },
                    new[] { 34, 32, 22, 24 },
                    new[] { 41, 30, 26, 37 },
                    new[] { 26, 19, 12, 17 },
                    new[] { 39, 34, 24, 21, 29 },
                    new[] { 14, 8, 1, 3, 11 },
                    new[] { 25, 15, 6, 13, 23 },
                    new[] { 2, 0, 5, 10, 7 },
                    new[] { 27, 16, 20, 31, 36 },
                    new[] { 45, 40, 43, 48, 49 },
                    new[] { 38, 28, 30, 41, 46 },
                    new[] { 37, 26, 17, 22, 32 },
                    new[] { 44, 35, 33, 42, 47 },
                    new[] { 19, 18, 9, 4, 12 },
                    new[] { 38, 43, 40, 31, 20, 10, 5, 9, 18, 28 },
                    new[] { 47, 49, 48, 46, 41, 37, 32, 34, 39, 44 }
                };
                var verts = new[]
                {
                    new Vector3(-0.91554f, -0.17156f, 0.190699f),
                    new Vector3(-0.874123f, -0.21861f, -0.254883f),
                    new Vector3(-0.873513f, 0.274357f, 0.14752f),
                    new Vector3(-0.832096f, 0.227307f, -0.298062f),
                    new Vector3(-0.790984f, -0.567471f, 0.016881f),
                    new Vector3(-0.738991f, -0.290113f, 0.587244f),
                    new Vector3(-0.680957f, 0.599954f, -0.096163f),
                    new Vector3(-0.670991f, 0.431396f, 0.517379f),
                    new Vector3(-0.630562f, -0.413291f, -0.579305f),
                    new Vector3(-0.614436f, -0.686024f, 0.413427f),
                    new Vector3(-0.587852f, 0.082534f, 0.789144f),
                    new Vector3(-0.562561f, 0.308217f, -0.64917f),
                    new Vector3(-0.547423f, -0.762153f, -0.30754f),
                    new Vector3(-0.478435f, 0.756993f, 0.273697f),
                    new Vector3(-0.438006f, -0.087694f, -0.822987f),
                    new Vector3(-0.411421f, 0.680864f, -0.44727f),
                    new Vector3(-0.301886f, 0.63844f, 0.670242f),
                    new Vector3(-0.277887f, -0.681242f, -0.658648f),
                    new Vector3(-0.261761f, -0.953976f, 0.334084f),
                    new Vector3(-0.220345f, -1.001026f, -0.111498f),
                    new Vector3(-0.218747f, 0.289578f, 0.942007f),
                    new Vector3(-0.16786f, 0.486183f, -0.771692f),
                    new Vector3(-0.085331f, -0.355646f, -0.90233f),
                    new Vector3(-0.083733f, 0.934958f, 0.151175f),
                    new Vector3(-0.043304f, 0.090271f, -0.945509f),
                    new Vector3(-0.042317f, 0.887908f, -0.294407f),
                    new Vector3(0.049191f, -0.920115f, -0.462606f),
                    new Vector3(0.092815f, 0.816405f, 0.54772f),
                    new Vector3(0.184323f, -0.991618f, 0.379522f),
                    new Vector3(0.201245f, 0.693227f, -0.618829f),
                    new Vector3(0.225739f, -1.038668f, -0.06606f),
                    new Vector3(0.227337f, 0.251936f, 0.987445f),
                    new Vector3(0.360753f, -0.393288f, -0.856892f),
                    new Vector3(0.362351f, 0.897316f, 0.196613f),
                    new Vector3(0.40278f, 0.052629f, -0.900071f),
                    new Vector3(0.403767f, 0.850266f, -0.24897f),
                    new Vector3(0.419893f, 0.577533f, 0.743762f),
                    new Vector3(0.443892f, -0.74215f, -0.585128f),
                    new Vector3(0.553427f, -0.784574f, 0.532385f),
                    new Vector3(0.553919f, 0.425276f, -0.698172f),
                    new Vector3(0.580011f, -0.016016f, 0.908102f),
                    new Vector3(0.62044f, -0.860702f, -0.188582f),
                    new Vector3(0.689428f, 0.658443f, 0.392655f),
                    new Vector3(0.704567f, -0.411927f, 0.734284f),
                    new Vector3(0.756442f, 0.582315f, -0.328312f),
                    new Vector3(0.772567f, 0.309582f, 0.664419f),
                    new Vector3(0.822963f, -0.703664f, 0.181277f),
                    new Vector3(0.93299f, 0.463762f, 0.068233f),
                    new Vector3(0.974102f, -0.331017f, 0.383177f),
                    new Vector3(1.016129f, 0.1149f, 0.339998f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            82, () =>
            {
                var faces = new[]
                {
                    new[] { 25, 23, 15 },
                    new[] { 27, 16, 19 },
                    new[] { 30, 28, 20 },
                    new[] { 21, 13, 12 },
                    new[] { 22, 11, 17 },
                    new[] { 3, 2, 8 },
                    new[] { 9, 6, 1 },
                    new[] { 4, 5, 0 },
                    new[] { 32, 26, 37 },
                    new[] { 42, 31, 38 },
                    new[] { 36, 33, 23, 25 },
                    new[] { 23, 19, 9, 15 },
                    new[] { 19, 16, 6, 9 },
                    new[] { 30, 20, 16, 27 },
                    new[] { 28, 21, 12, 20 },
                    new[] { 12, 13, 5, 4 },
                    new[] { 25, 15, 11, 22 },
                    new[] { 11, 3, 8, 17 },
                    new[] { 8, 2, 7, 14 },
                    new[] { 1, 0, 2, 3 },
                    new[] { 6, 4, 0, 1 },
                    new[] { 5, 13, 18, 10 },
                    new[] { 22, 17, 26, 32 },
                    new[] { 26, 24, 34, 37 },
                    new[] { 32, 37, 46, 41 },
                    new[] { 43, 44, 49, 48 },
                    new[] { 39, 29, 31, 42 },
                    new[] { 35, 40, 30, 27 },
                    new[] { 47, 42, 38, 45 },
                    new[] { 38, 31, 21, 28 },
                    new[] { 33, 35, 27, 19, 23 },
                    new[] { 20, 12, 4, 6, 16 },
                    new[] { 15, 9, 1, 3, 11 },
                    new[] { 0, 5, 10, 7, 2 },
                    new[] { 17, 8, 14, 24, 26 },
                    new[] { 37, 34, 43, 48, 46 },
                    new[] { 44, 39, 42, 47, 49 },
                    new[] { 45, 38, 28, 30, 40 },
                    new[] { 36, 25, 22, 32, 41 },
                    new[] { 31, 29, 18, 13, 21 },
                    new[] { 44, 43, 34, 24, 14, 7, 10, 18, 29, 39 },
                    new[] { 41, 46, 48, 49, 47, 45, 40, 35, 33, 36 }
                };
                var verts = new[]
                {
                    new Vector3(-0.915244f, -0.149547f, 0.004878f),
                    new Vector3(-0.857812f, 0.261033f, -0.170045f),
                    new Vector3(-0.853796f, 0.017864f, 0.418001f),
                    new Vector3(-0.796364f, 0.428444f, 0.243078f),
                    new Vector3(-0.772518f, -0.333547f, -0.380149f),
                    new Vector3(-0.759676f, -0.571754f, 0.001381f),
                    new Vector3(-0.715086f, 0.077033f, -0.555071f),
                    new Vector3(-0.660252f, -0.300878f, 0.669827f),
                    new Vector3(-0.638771f, 0.334916f, 0.65404f),
                    new Vector3(-0.609317f, 0.503159f, -0.456572f),
                    new Vector3(-0.602083f, -0.665282f, 0.412342f),
                    new Vector3(-0.509893f, 0.774035f, 0.211875f),
                    new Vector3(-0.463372f, -0.606112f, -0.56073f),
                    new Vector3(-0.45053f, -0.844319f, -0.1792f),
                    new Vector3(-0.445227f, 0.016174f, 0.905866f),
                    new Vector3(-0.394292f, 0.820211f, -0.220533f),
                    new Vector3(-0.370445f, 0.05822f, -0.84376f),
                    new Vector3(-0.3523f, 0.680507f, 0.622836f),
                    new Vector3(-0.292937f, -0.937847f, 0.231761f),
                    new Vector3(-0.264676f, 0.484346f, -0.745261f),
                    new Vector3(-0.214877f, -0.363987f, -0.847257f),
                    new Vector3(-0.105889f, -0.863132f, -0.467889f),
                    new Vector3(-0.103806f, 0.922633f, 0.336308f),
                    new Vector3(-0.049651f, 0.801399f, -0.509222f),
                    new Vector3(-0.039139f, 0.164772f, 1.0303f),
                    new Vector3(0.011796f, 0.968809f, -0.096099f),
                    new Vector3(0.018293f, 0.575352f, 0.855378f),
                    new Vector3(0.04447f, 0.211781f, -0.925842f),
                    new Vector3(0.142605f, -0.621006f, -0.754417f),
                    new Vector3(0.149102f, -1.014463f, 0.197061f),
                    new Vector3(0.200037f, -0.210426f, -0.929339f),
                    new Vector3(0.264704f, -0.968287f, -0.235347f),
                    new Vector3(0.266787f, 0.817478f, 0.56885f),
                    new Vector3(0.392388f, 0.724782f, -0.543923f),
                    new Vector3(0.4029f, 0.088156f, 0.9956f),
                    new Vector3(0.450558f, 0.360379f, -0.801408f),
                    new Vector3(0.453835f, 0.892193f, -0.1308f),
                    new Vector3(0.460331f, 0.498736f, 0.820677f),
                    new Vector3(0.513198f, -0.726161f, -0.521875f),
                    new Vector3(0.55519f, -0.865866f, 0.321494f),
                    new Vector3(0.606125f, -0.061828f, -0.804905f),
                    new Vector3(0.611428f, 0.798665f, 0.280161f),
                    new Vector3(0.670792f, -0.819689f, -0.110913f),
                    new Vector3(0.712045f, -0.18441f, 0.815019f),
                    new Vector3(0.770215f, -0.548813f, 0.557534f),
                    new Vector3(0.799669f, -0.38057f, -0.553078f),
                    new Vector3(0.804972f, 0.479923f, 0.531988f),
                    new Vector3(0.957263f, -0.474098f, -0.142117f),
                    new Vector3(0.96054f, 0.057716f, 0.528491f),
                    new Vector3(1.01871f, -0.306688f, 0.271006f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            83, () =>
            {
                var faces = new[]
                {
                    new[] { 25, 27, 17 },
                    new[] { 20, 11, 12 },
                    new[] { 5, 0, 6 },
                    new[] { 26, 18, 28 },
                    new[] { 34, 19, 33 },
                    new[] { 35, 37, 27, 25 },
                    new[] { 27, 21, 13, 17 },
                    new[] { 25, 17, 11, 20 },
                    new[] { 11, 5, 6, 12 },
                    new[] { 6, 0, 2, 8 },
                    new[] { 7, 1, 0, 5 },
                    new[] { 3, 9, 10, 4 },
                    new[] { 20, 12, 18, 26 },
                    new[] { 18, 14, 22, 28 },
                    new[] { 26, 28, 38, 36 },
                    new[] { 30, 32, 42, 40 },
                    new[] { 24, 16, 19, 34 },
                    new[] { 39, 41, 31, 29 },
                    new[] { 44, 34, 33, 43 },
                    new[] { 33, 19, 15, 23 },
                    new[] { 37, 39, 29, 21, 27 },
                    new[] { 17, 13, 7, 5, 11 },
                    new[] { 1, 3, 4, 2, 0 },
                    new[] { 12, 6, 8, 14, 18 },
                    new[] { 28, 22, 30, 40, 38 },
                    new[] { 32, 24, 34, 44, 42 },
                    new[] { 43, 33, 23, 31, 41 },
                    new[] { 35, 25, 20, 26, 36 },
                    new[] { 19, 16, 10, 9, 15 },
                    new[] { 1, 7, 13, 21, 29, 31, 23, 15, 9, 3 },
                    new[] { 32, 30, 22, 14, 8, 2, 4, 10, 16, 24 },
                    new[] { 36, 38, 40, 42, 44, 43, 41, 39, 37, 35 }
                };
                var verts = new[]
                {
                    new Vector3(-0.932936f, 0.189273f, 0.192295f),
                    new Vector3(-0.922398f, 0.241407f, -0.253129f),
                    new Vector3(-0.911995f, -0.249962f, 0.280987f),
                    new Vector3(-0.894943f, -0.165606f, -0.439724f),
                    new Vector3(-0.888514f, -0.469289f, -0.109623f),
                    new Vector3(-0.719187f, 0.580372f, 0.243128f),
                    new Vector3(-0.712758f, 0.27669f, 0.57323f),
                    new Vector3(-0.708649f, 0.632507f, -0.202295f),
                    new Vector3(-0.691817f, -0.162545f, 0.661922f),
                    new Vector3(-0.636773f, -0.433068f, -0.690809f),
                    new Vector3(-0.630343f, -0.736751f, -0.360707f),
                    new Vector3(-0.352393f, 0.77395f, 0.41407f),
                    new Vector3(-0.345964f, 0.470268f, 0.744172f),
                    new Vector3(-0.335341f, 0.858306f, -0.306641f),
                    new Vector3(-0.31208f, -0.240428f, 0.887678f),
                    new Vector3(-0.246498f, -0.458817f, -0.910477f),
                    new Vector3(-0.236095f, -0.950186f, -0.376361f),
                    new Vector3(-0.115163f, 0.945723f, 0.074294f),
                    new Vector3(-0.098331f, 0.150671f, 0.938511f),
                    new Vector3(0.001135f, -0.778414f, -0.716137f),
                    new Vector3(0.027344f, 0.696067f, 0.639826f),
                    new Vector3(0.054934f, 0.832556f, -0.526309f),
                    new Vector3(0.082168f, -0.453864f, 0.872024f),
                    new Vector3(0.12681f, -0.233018f, -1.014822f),
                    new Vector3(0.143641f, -1.02807f, -0.150605f),
                    new Vector3(0.264573f, 0.867839f, 0.30005f),
                    new Vector3(0.274976f, 0.37647f, 0.834165f),
                    new Vector3(0.275112f, 0.919973f, -0.145374f),
                    new Vector3(0.295917f, -0.062764f, 0.922857f),
                    new Vector3(0.313104f, 0.565095f, -0.777393f),
                    new Vector3(0.340339f, -0.721326f, 0.620939f),
                    new Vector3(0.340559f, 0.158081f, -0.963989f),
                    new Vector3(0.36382f, -0.940653f, 0.23033f),
                    new Vector3(0.374442f, -0.552615f, -0.820483f),
                    new Vector3(0.380871f, -0.856297f, -0.490381f),
                    new Vector3(0.658822f, 0.654404f, 0.284396f),
                    new Vector3(0.665251f, 0.350721f, 0.614498f),
                    new Vector3(0.66936f, 0.706538f, -0.161028f),
                    new Vector3(0.686192f, -0.088513f, 0.703189f),
                    new Vector3(0.692841f, 0.487211f, -0.551637f),
                    new Vector3(0.713646f, -0.495527f, 0.516594f),
                    new Vector3(0.720295f, 0.080198f, -0.738233f),
                    new Vector3(0.737127f, -0.714854f, 0.125984f),
                    new Vector3(0.741236f, -0.359037f, -0.649541f),
                    new Vector3(0.747665f, -0.662719f, -0.31944f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            84, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 7, 3 },
                    new[] { 3, 7, 5 },
                    new[] { 3, 5, 1 },
                    new[] { 4, 7, 6 },
                    new[] { 7, 4, 5 },
                    new[] { 5, 4, 1 },
                    new[] { 4, 2, 1 },
                    new[] { 1, 2, 0 },
                    new[] { 1, 0, 3 },
                    new[] { 6, 2, 4 },
                    new[] { 2, 6, 0 },
                    new[] { 0, 6, 3 }
                };
                var verts = new[]
                {
                    new Vector3(-0.768016f, 0.559678f, 0.635844f),
                    new Vector3(-0.720709f, -0.093633f, -0.405339f),
                    new Vector3(-0.6358f, -0.662351f, 0.681929f),
                    new Vector3(0.09848f, 0.800885f, -0.202562f),
                    new Vector3(0.269587f, -0.77416f, -0.143135f),
                    new Vector3(0.285934f, -0.010665f, -1.107297f),
                    new Vector3(0.352377f, 0.066396f, 0.750712f),
                    new Vector3(1.118151f, 0.11385f, -0.210152f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            85, () =>
            {
                var faces = new[]
                {
                    new[] { 13, 9, 14 },
                    new[] { 9, 13, 6 },
                    new[] { 9, 6, 3 },
                    new[] { 11, 15, 12 },
                    new[] { 15, 11, 13 },
                    new[] { 15, 13, 14 },
                    new[] { 4, 7, 1 },
                    new[] { 7, 4, 11 },
                    new[] { 7, 11, 12 },
                    new[] { 6, 0, 3 },
                    new[] { 0, 6, 4 },
                    new[] { 0, 4, 1 },
                    new[] { 2, 3, 0 },
                    new[] { 3, 2, 8 },
                    new[] { 3, 8, 9 },
                    new[] { 5, 1, 7 },
                    new[] { 1, 5, 2 },
                    new[] { 1, 2, 0 },
                    new[] { 10, 12, 15 },
                    new[] { 12, 10, 5 },
                    new[] { 12, 5, 7 },
                    new[] { 8, 14, 9 },
                    new[] { 14, 8, 10 },
                    new[] { 14, 10, 15 },
                    new[] { 13, 11, 4, 6 },
                    new[] { 2, 5, 10, 8 }
                };
                var verts = new[]
                {
                    new Vector3(-0.984789f, 0.388776f, -0.318546f),
                    new Vector3(-0.905986f, -0.50645f, -0.380955f),
                    new Vector3(-0.774402f, -0.096785f, 0.410496f),
                    new Vector3(-0.648503f, 0.795226f, 0.411689f),
                    new Vector3(-0.32762f, 0.028199f, -0.818195f),
                    new Vector3(-0.278924f, -0.81528f, 0.187334f),
                    new Vector3(-0.177548f, 0.78687f, -0.356208f),
                    new Vector3(-0.134682f, -0.843968f, -0.701432f),
                    new Vector3(-0.06669f, 0.257644f, 0.840681f),
                    new Vector3(0.229454f, 0.996879f, 0.419536f),
                    new Vector3(0.428788f, -0.460851f, 0.617519f),
                    new Vector3(0.523163f, -0.229235f, -0.671807f),
                    new Vector3(0.55373f, -0.948137f, -0.129793f),
                    new Vector3(0.673235f, 0.529435f, -0.20982f),
                    new Vector3(0.811213f, 0.353538f, 0.662851f),
                    new Vector3(1.079561f, -0.235865f, 0.03665f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            86, () =>
            {
                var faces = new[]
                {
                    new[] { 7, 3, 2 },
                    new[] { 2, 1, 6 },
                    new[] { 2, 3, 0 },
                    new[] { 2, 0, 1 },
                    new[] { 1, 5, 6 },
                    new[] { 6, 5, 9 },
                    new[] { 3, 4, 0 },
                    new[] { 4, 3, 8 },
                    new[] { 9, 7, 6 },
                    new[] { 9, 8, 7 },
                    new[] { 7, 8, 3 },
                    new[] { 6, 7, 2 },
                    new[] { 1, 0, 4, 5 },
                    new[] { 5, 4, 8, 9 }
                };
                var verts = new[]
                {
                    new Vector3(-1.10165f, -0.110367f, -0.010645f),
                    new Vector3(-0.640942f, 0.825699f, 0.365159f),
                    new Vector3(-0.414409f, 0.468967f, -0.660083f),
                    new Vector3(-0.324965f, -0.634882f, -0.603383f),
                    new Vector3(-0.317654f, -0.702618f, 0.503439f),
                    new Vector3(0.143053f, 0.233445f, 0.879242f),
                    new Vector3(0.402426f, 0.84303f, -0.010043f),
                    new Vector3(0.585642f, -0.008843f, -0.69593f),
                    new Vector3(0.603895f, -0.925247f, -0.07178f),
                    new Vector3(1.064602f, 0.010817f, 0.304023f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            87, () =>
            {
                var faces = new[]
                {
                    new[] { 5, 0, 1 },
                    new[] { 9, 3, 8 },
                    new[] { 8, 6, 10 },
                    new[] { 8, 3, 2 },
                    new[] { 8, 2, 6 },
                    new[] { 6, 5, 10 },
                    new[] { 10, 5, 7 },
                    new[] { 3, 1, 2 },
                    new[] { 1, 3, 4 },
                    new[] { 7, 9, 10 },
                    new[] { 7, 4, 9 },
                    new[] { 9, 4, 3 },
                    new[] { 10, 9, 8 },
                    new[] { 6, 0, 5 },
                    new[] { 6, 2, 0 },
                    new[] { 0, 2, 1 },
                    new[] { 5, 1, 4, 7 }
                };
                var verts = new[]
                {
                    new Vector3(-0.858193f, -0.792464f, 0.432564f),
                    new Vector3(-0.785643f, 0.057917f, -0.211154f),
                    new Vector3(-0.533044f, 0.147795f, 0.823687f),
                    new Vector3(-0.229952f, 0.904287f, 0.131849f),
                    new Vector3(-0.212337f, 0.637025f, -0.903062f),
                    new Vector3(-0.091064f, -0.748583f, -0.310653f),
                    new Vector3(0.161537f, -0.658707f, 0.724189f),
                    new Vector3(0.482242f, -0.169476f, -1.002562f),
                    new Vector3(0.505859f, 0.352206f, 0.676436f),
                    new Vector3(0.693906f, 0.639069f, -0.336051f),
                    new Vector3(0.86669f, -0.369064f, -0.025245f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            88, () =>
            {
                var faces = new[]
                {
                    new[] { 6, 7, 10 },
                    new[] { 3, 7, 1 },
                    new[] { 3, 5, 7 },
                    new[] { 10, 11, 8 },
                    new[] { 10, 9, 11 },
                    new[] { 1, 7, 6 },
                    new[] { 6, 10, 8 },
                    new[] { 2, 4, 0 },
                    new[] { 11, 4, 8 },
                    new[] { 11, 9, 4 },
                    new[] { 0, 3, 1 },
                    new[] { 0, 5, 3 },
                    new[] { 8, 2, 6 },
                    new[] { 8, 4, 2 },
                    new[] { 2, 1, 6 },
                    new[] { 2, 0, 1 },
                    new[] { 7, 5, 9, 10 },
                    new[] { 4, 9, 5, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.710639f, -0.297668f, -0.15267f),
                    new Vector3(-0.651151f, -0.105949f, 0.829841f),
                    new Vector3(-0.621335f, 0.64788f, 0.169179f),
                    new Vector3(-0.614162f, -1.052419f, 0.500527f),
                    new Vector3(-0.166396f, 0.361269f, -0.677289f),
                    new Vector3(-0.002058f, -0.993534f, -0.291612f),
                    new Vector3(0.165944f, 0.471894f, 0.764865f),
                    new Vector3(0.225836f, -0.507426f, 0.555374f),
                    new Vector3(0.279224f, 1.020494f, -0.066987f),
                    new Vector3(0.542185f, -0.334598f, -0.816231f),
                    new Vector3(0.770079f, 0.151511f, 0.030755f),
                    new Vector3(0.782476f, 0.638548f, -0.845752f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            89, () =>
            {
                var faces = new[]
                {
                    new[] { 9, 8, 13 },
                    new[] { 1, 8, 2 },
                    new[] { 1, 7, 8 },
                    new[] { 13, 11, 10 },
                    new[] { 13, 12, 11 },
                    new[] { 2, 8, 9 },
                    new[] { 9, 13, 10 },
                    new[] { 12, 6, 11 },
                    new[] { 3, 7, 1 },
                    new[] { 4, 5, 0 },
                    new[] { 11, 5, 10 },
                    new[] { 11, 6, 5 },
                    new[] { 0, 1, 2 },
                    new[] { 0, 3, 1 },
                    new[] { 10, 4, 9 },
                    new[] { 10, 5, 4 },
                    new[] { 4, 2, 9 },
                    new[] { 4, 0, 2 },
                    new[] { 8, 7, 12, 13 },
                    new[] { 3, 6, 12, 7 },
                    new[] { 5, 6, 3, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.83117f, 0.133549f, -0.011648f),
                    new Vector3(-0.700039f, -0.806136f, -0.111242f),
                    new Vector3(-0.631074f, -0.403619f, 0.750934f),
                    new Vector3(-0.576095f, -0.225432f, -0.857931f),
                    new Vector3(-0.446282f, 0.532211f, 0.764918f),
                    new Vector3(-0.172487f, 0.817558f, -0.103263f),
                    new Vector3(0.082589f, 0.458576f, -0.949546f),
                    new Vector3(0.105206f, -0.849768f, -0.620949f),
                    new Vector3(0.145599f, -0.76155f, 0.32811f),
                    new Vector3(0.235014f, -0.09212f, 1.001899f),
                    new Vector3(0.469795f, 0.739577f, 0.597817f),
                    new Vector3(0.750772f, 0.700456f, -0.313032f),
                    new Vector3(0.76389f, -0.165759f, -0.712564f),
                    new Vector3(0.804283f, -0.077541f, 0.236495f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            90, () =>
            {
                var faces = new[]
                {
                    new[] { 13, 6, 7 },
                    new[] { 9, 15, 11 },
                    new[] { 12, 13, 7 },
                    new[] { 15, 14, 11 },
                    new[] { 12, 10, 14 },
                    new[] { 12, 7, 5 },
                    new[] { 12, 5, 10 },
                    new[] { 10, 5, 3 },
                    new[] { 7, 6, 2 },
                    new[] { 5, 7, 2 },
                    new[] { 2, 6, 1 },
                    new[] { 2, 1, 0 },
                    new[] { 10, 3, 8 },
                    new[] { 10, 8, 14 },
                    new[] { 8, 11, 14 },
                    new[] { 8, 4, 11 },
                    new[] { 11, 4, 9 },
                    new[] { 9, 1, 6 },
                    new[] { 4, 1, 9 },
                    new[] { 0, 1, 4 },
                    new[] { 9, 6, 13, 15 },
                    new[] { 15, 13, 12, 14 },
                    new[] { 5, 2, 0, 3 },
                    new[] { 3, 0, 4, 8 }
                };
                var verts = new[]
                {
                    new Vector3(-1.052782f, 0.264006f, 0.098264f),
                    new Vector3(-0.753999f, -0.411397f, 0.610751f),
                    new Vector3(-0.732127f, -0.483215f, -0.285043f),
                    new Vector3(-0.599216f, 0.763406f, -0.495842f),
                    new Vector3(-0.413906f, 0.414477f, 0.712494f),
                    new Vector3(-0.278562f, 0.016184f, -0.879148f),
                    new Vector3(-0.105415f, -0.890983f, 0.213994f),
                    new Vector3(0.009423f, -0.81027f, -0.673913f),
                    new Vector3(0.03966f, 0.913876f, 0.118388f),
                    new Vector3(0.101994f, -0.305887f, 0.864167f),
                    new Vector3(0.267869f, 0.713733f, -0.727747f),
                    new Vector3(0.47671f, 0.507934f, 0.790906f),
                    new Vector3(0.590474f, -0.124774f, -0.697516f),
                    new Vector3(0.722293f, -0.806255f, -0.126296f),
                    new Vector3(0.797883f, 0.460322f, -0.047343f),
                    new Vector3(0.929701f, -0.221158f, 0.523878f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            91, () =>
            {
                var faces = new[]
                {
                    new[] { 11, 13, 12 },
                    new[] { 7, 11, 4 },
                    new[] { 10, 7, 5 },
                    new[] { 13, 10, 9 },
                    new[] { 3, 8, 6 },
                    new[] { 4, 3, 0 },
                    new[] { 2, 1, 0 },
                    new[] { 9, 2, 6 },
                    new[] { 13, 11, 7, 10 },
                    new[] { 6, 2, 0, 3 },
                    new[] { 12, 13, 9, 6, 8 },
                    new[] { 11, 12, 8, 3, 4 },
                    new[] { 5, 7, 4, 0, 1 },
                    new[] { 10, 5, 1, 2, 9 }
                };
                var verts = new[]
                {
                    new Vector3(-0.932446f, -0.071511f, 0.062428f),
                    new Vector3(-0.890073f, 0.716495f, 0.434115f),
                    new Vector3(-0.483326f, 0.038238f, 0.802122f),
                    new Vector3(-0.479875f, -0.798287f, -0.104525f),
                    new Vector3(-0.363346f, -0.08879f, -0.598427f),
                    new Vector3(-0.294782f, 1.186233f, 0.002976f),
                    new Vector3(-0.030753f, -0.688537f, 0.635167f),
                    new Vector3(0.030753f, 0.688538f, -0.635171f),
                    new Vector3(0.294779f, -1.186232f, -0.002974f),
                    new Vector3(0.363347f, 0.088791f, 0.598425f),
                    new Vector3(0.479874f, 0.79829f, 0.104525f),
                    new Vector3(0.483327f, -0.038241f, -0.802125f),
                    new Vector3(0.890072f, -0.716499f, -0.434115f),
                    new Vector3(0.932449f, 0.071511f, -0.062429f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        },

        {
            92, () =>
            {
                var faces = new[]
                {
                    new[] { 12, 11, 4 },
                    new[] { 11, 6, 4 },
                    new[] { 6, 11, 15 },
                    new[] { 13, 15, 17 },
                    new[] { 5, 13, 8 },
                    new[] { 2, 5, 8 },
                    new[] { 2, 8, 9 },
                    new[] { 7, 9, 14 },
                    new[] { 16, 14, 17 },
                    new[] { 3, 0, 7 },
                    new[] { 3, 1, 0 },
                    new[] { 1, 3, 10 },
                    new[] { 12, 10, 16 },
                    new[] { 6, 15, 13, 5 },
                    new[] { 2, 9, 7, 0 },
                    new[] { 12, 4, 1, 10 },
                    new[] { 11, 12, 16, 17, 15 },
                    new[] { 8, 13, 17, 14, 9 },
                    new[] { 16, 10, 3, 7, 14 },
                    new[] { 1, 4, 6, 5, 2, 0 }
                };
                var verts = new[]
                {
                    new Vector3(-0.748928f, 0.557858f, -0.030371f),
                    new Vector3(-0.638635f, 0.125804f, -0.670329f),
                    new Vector3(-0.593696f, 0.259282f, 0.67329f),
                    new Vector3(-0.427424f, 0.876636f, -0.665507f),
                    new Vector3(-0.373109f, -0.604827f, -0.606627f),
                    new Vector3(-0.32817f, -0.471348f, 0.736992f),
                    new Vector3(-0.217876f, -0.903403f, 0.097033f),
                    new Vector3(-0.141658f, 1.042101f, 0.041134f),
                    new Vector3(-0.021021f, 0.094954f, 1.176701f),
                    new Vector3(0.013575f, 0.743525f, 0.744795f),
                    new Vector3(0.036802f, 0.343022f, -0.994341f),
                    new Vector3(0.267732f, -1.036179f, -0.498733f),
                    new Vector3(0.302328f, -0.387609f, -0.93064f),
                    new Vector3(0.443205f, -0.438661f, 0.847867f),
                    new Vector3(0.499183f, 0.610749f, 0.149029f),
                    new Vector3(0.553499f, -0.870715f, 0.207908f),
                    new Vector3(0.609478f, 0.178694f, -0.490931f),
                    new Vector3(0.76471f, -0.119883f, 0.212731f)
                };
                PolyMesh poly = new PolyMesh(verts, faces);
                return poly;
            }
        }
    };
}