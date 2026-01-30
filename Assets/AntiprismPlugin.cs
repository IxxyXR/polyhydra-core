/*
   Antiprism Unity Plugin - C# API

   C# wrapper for the Antiprism polyhedra library.
   Provides easy-to-use Unity integration for creating and manipulating polyhedra.

   Copyright (c) 2003-2025, Adrian Rossiter
   Antiprism - http://www.antiprism.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Antiprism
{
    /// <summary>
    /// Simplified polyhedron types - parameterized types use separate parameter fields
    /// Covers 200+ polyhedra via ~21 enum entries through parameterization
    /// </summary>
    /// <remarks>
    /// All uniform polyhedra (U1-U80) are accessed via UniformPolyhedron with their U-number.
    /// This includes:
    /// - Platonic solids: U1 (tet), U5 (oct), U6 (cube), U22 (ico), U23 (dod)
    /// - Archimedean solids: U2-U14, U15-U21, U24-U29
    /// - Kepler-Poinsot polyhedra: U34, U35, U52, U53
    ///
    /// Specific prisms should use the Prism parameterized type:
    /// - Tetrahedral prism: Prism with sides=4
    /// - Octahedral prism: Prism with sides=8
    /// </remarks>
    public enum PolyhedronType
    {
        // === PARAMETERIZED TYPES (require integer parameter) ===
        Prism,              // N-sided prism (n >= 3)
        Antiprism,          // N-sided antiprism (n >= 3)
        Pyramid,            // N-sided pyramid (n >= 3)
        Dipyramid,          // N-sided dipyramid/bipyramid (n >= 3)
        Cupola,             // N-sided cupola (n >= 2)
        Geodesic,           // Geodesic sphere (requires frequency and method)
        Unitile2D,          // 2D tiling on a surface (11 patterns x 4 surfaces)
        IsoKite,            // Isohedral kite-faced polyhedra (Schwarz triangles T1-T2, O1-O2, I1-I10)
        Trapezohedron,      // Kite-faced dipyramids (n/d fraction)
        Symmetrohedra,      // Symmetrohedra using Kaplan-Hart notation
        JohnsonSolid,       // Johnson solid by number (J1-J92)
        UniformPolyhedron,  // Uniform polyhedron by number (U1-U80) - includes all Archimedean & Kepler-Poinsot
        Wenninger,          // Wenninger stellation by number (W1-W119)

        // === CATALAN SOLIDS (duals of Archimedean - not in uniform series) ===
        RhombicDodecahedron,
        RhombicTriacontahedron,

        // === UNIFORM COMPOUNDS (famous ones kept for convenience) ===
        StellaOctangula,
        CompoundCubeOctahedron,
        CompoundDodecahedronIcosahedron,
        CompoundTwoTetrahedra,
        CompoundFiveTetrahedra,

        // === MISCELLANEOUS ===
        RhombicEnneacontahedron,
        RhombicHexecontahedron,
        Csaszar,
        Szilassi,
    }

    /// <summary>
    /// Geodesic subdivision methods
    /// </summary>
    public enum GeodesicMethod
    {
        Icosahedron,    // Subdivide icosahedron
        Octahedron,     // Subdivide octahedron
        Tetrahedron,    // Subdivide tetrahedron
    }

    /// <summary>
    /// Uniform tiling patterns (11 possible tilings)
    /// </summary>
    public enum TilingPattern
    {
        Squares_4444 = 1,        // 4,4,4,4 (square tiling)
        Triangles_333333 = 2,    // 3,3,3,3,3,3 (triangle tiling)
        Hexagons_666 = 3,        // 6,6,6 (hexagon tiling)
        TriHex_3636 = 4,         // 3,6,3,6 (triangle-hexagon)
        TriSquare_33344 = 5,     // 3,3,3,4,4 (triangle-square)
        SnubSquare_33434 = 6,    // 3,3,4,3,4 (snub square)
        SnubHex_33336 = 7,       // 3,3,3,3,6 (snub hexagon)
        TriDodec_31212 = 8,      // 3,12,12 (triangle-dodecagon)
        SquareOct_488 = 9,       // 4,8,8 (square-octagon)
        TriSquareHex_3464 = 10,  // 3,4,6,4 (triangle-square-hexagon)
        SquareHexDodec_4612 = 11 // 4,6,12 (square-hexagon-dodecagon)
    }

    /// <summary>
    /// Surface types for tilings
    /// </summary>
    public enum TilingSurface
    {
        Plane = 0,         // Flat rectangular plane
        Torus = 1,         // Donut/torus surface
        KleinBottle = 2,   // Klein bottle (non-orientable)
        MobiusStrip = 3    // Mobius strip (single-sided)
    }

    /// <summary>
    /// Schwarz triangle models for iso_kite (kite-faced polyhedra)
    /// </summary>
    public enum SchwarzTriangle
    {
        // Tetrahedral models
        T1,  // (3,3,2)
        T2,  // (3,3,3/2)

        // Octahedral models
        O1,  // (4,3,2)
        O2,  // (4/3,4,3)
        O2B, // (4,3,4/3)

        // Icosahedral models
        I1,   // (5,3,2)
        I2,   // (5/2,3,2)
        I3,   // (5/2,5,2)
        I4,   // (5/2,3,3)
        I4B,  // (3,5/2,3)
        I5,   // (5/4,3,3)
        I5B,  // (3,5/4,3)
        I6,   // (5/3,5,3)
        I6B,  // (5,5/3,3)
        I6C,  // (5,3,5/3)
        I7,   // (5/4,5,3)
        I7B,  // (5,5/4,3)
        I8,   // (5/3,5/2,3)
        I8B,  // (5/2,5/3,3)
        I9,   // (5/4,5,5)
        I9B,  // (5,5/4,5)
        I10   // (5/2,5/2,5/2)
    }

    /// <summary>
    /// Modifier/Conway operator types
    /// </summary>
    public enum ModifierType
    {
        None,
        Dual,
        Truncate,
        Kis,
        Ambo,
        Gyro,
        Join,
        Needle,
        Zip,
        Subdivide,
        Expand,
        Meta,
        Bevel,
        Snub,
        Ortho,
        ConvexHull
    }

    /// <summary>
    /// Status codes returned by Antiprism operations
    /// </summary>
    public enum Status
    {
        OK = 0,
        ErrorMemory = -1,
        ErrorInvalidHandle = -2,
        ErrorInvalidIndex = -3,
        ErrorParse = -4,
        ErrorFile = -5,
        ErrorUnknown = -99
    }

    /// <summary>
    /// Main Antiprism plugin class
    /// </summary>
    public static class AntiprismPlugin
    {
        public const string LIBRARY_NAME = "antiprism";
        private static bool dataPathInitialized = false;

        static AntiprismPlugin()
        {
            TrySetDataPath();
        }

        private static void TrySetDataPath()
        {
            if (dataPathInitialized)
                return;

            try
            {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "antiprism");
                anti_set_data_path(path);
                dataPathInitialized = true;
            }
            catch
            {
                // Ignore failures; caller can still set ANTIPRISM_DATA manually.
            }
        }

        internal static void EnsureDataPath()
        {
            TrySetDataPath();
        }

        /// <summary>
        /// Get the Antiprism library version
        /// </summary>
        public static string GetVersion()
        {
            IntPtr ptr = anti_get_version();
            return Marshal.PtrToStringAnsi(ptr);
        }

        /// <summary>
        /// Get resource name for non-parameterized polyhedron types
        /// </summary>
        public static string GetResourceName(PolyhedronType type)
        {
            switch (type)
            {
                // Catalan Solids (duals of Archimedean - not in uniform series)
                case PolyhedronType.RhombicDodecahedron: return "rhombic_dodecahedron";
                case PolyhedronType.RhombicTriacontahedron: return "rhombic_triacontahedron";

                // Uniform Compounds
                case PolyhedronType.StellaOctangula: return "UC1";
                case PolyhedronType.CompoundCubeOctahedron: return "UC2";
                case PolyhedronType.CompoundDodecahedronIcosahedron: return "UC3";
                case PolyhedronType.CompoundTwoTetrahedra: return "UC4";
                case PolyhedronType.CompoundFiveTetrahedra: return "UC5";

                // Miscellaneous
                case PolyhedronType.RhombicEnneacontahedron: return "rhombic_e90";
                case PolyhedronType.RhombicHexecontahedron: return "rhombic_h60";
                case PolyhedronType.Csaszar: return "csaszar";
                case PolyhedronType.Szilassi: return "szilassi";

                // Parameterized types - these should use Create methods or be handled in CreateBasePolyhedron
                case PolyhedronType.Prism:
                case PolyhedronType.Antiprism:
                case PolyhedronType.Pyramid:
                case PolyhedronType.Dipyramid:
                case PolyhedronType.Cupola:
                case PolyhedronType.Geodesic:
                case PolyhedronType.Unitile2D:
                case PolyhedronType.IsoKite:
                case PolyhedronType.Trapezohedron:
                case PolyhedronType.Symmetrohedra:
                case PolyhedronType.JohnsonSolid:
                case PolyhedronType.UniformPolyhedron:
                case PolyhedronType.Wenninger:
                    throw new ArgumentException($"{type} is parameterized - should be handled in CreateBasePolyhedron()");

                default:
                    return "ico"; // Fallback
            }
        }

        /// <summary>
        /// Create the base polyhedron based on type and parameters
        /// </summary>
        /// <param name="polyhedronType">Type of polyhedron to create</param>
        /// <param name="sides">Number of sides for Prism/Antiprism/Pyramid/Dipyramid/Cupola</param>
        /// <param name="geodesicFrequency">Subdivision frequency for Geodesic</param>
        /// <param name="geodesicMethod">Base polyhedron for Geodesic</param>
        /// <param name="tilingPattern">Tiling pattern for Unitile2D</param>
        /// <param name="tilingSurface">Surface type for Unitile2D</param>
        /// <param name="tilingWidth">Width of tiling</param>
        /// <param name="tilingHeight">Height of tiling</param>
        /// <param name="tilingMinorRadius">Minor radius for torus/klein/mobius</param>
        /// <param name="tilingMajorRadius">Major radius for torus/klein/mobius</param>
        /// <param name="isoKiteModel">Schwarz triangle model for IsoKite</param>
        /// <param name="isoKiteHeightA">Height of kite apex on OA</param>
        /// <param name="isoKiteHeightB">Height of kite apex on OB</param>
        /// <param name="isoKiteHeightC">Height of kite side vertex on OC</param>
        /// <param name="trapezohedronN">Numerator of fraction for Trapezohedron</param>
        /// <param name="trapezohedronD">Denominator of fraction for Trapezohedron</param>
        /// <param name="trapezohedronHeightA">Height of kite apex on OA for Trapezohedron</param>
        /// <param name="trapezohedronHeightB">Height of kite apex on OB for Trapezohedron</param>
        /// <param name="symmetroSym">Symmetry type for Symmetrohedra</param>
        /// <param name="symmetroMult0">Multiplier for primary axis for Symmetrohedra</param>
        /// <param name="symmetroMult1">Multiplier for secondary axis for Symmetrohedra</param>
        /// <param name="symmetroMult2">Multiplier for tertiary axis for Symmetrohedra</param>
        /// <param name="johnsonNumber">Johnson solid number (1-92)</param>
        /// <param name="uniformNumber">Uniform polyhedron number (1-80)</param>
        /// <param name="wenningerNumber">Wenninger stellation number (1-119)</param>
        /// <returns>New Geometry containing the polyhedron</returns>
        public static Geometry CreateBasePolyhedron(
            PolyhedronType polyhedronType,
            int sides = 5,
            int geodesicFrequency = 2,
            GeodesicMethod geodesicMethod = GeodesicMethod.Icosahedron,
            TilingPattern tilingPattern = TilingPattern.Squares_4444,
            TilingSurface tilingSurface = TilingSurface.Torus,
            float tilingWidth = 20,
            float tilingHeight = 0,
            float tilingMinorRadius = 1.0f,
            float tilingMajorRadius = 3.0f,
            SchwarzTriangle isoKiteModel = SchwarzTriangle.I1,
            float isoKiteHeightA = 0,
            float isoKiteHeightB = 0,
            float isoKiteHeightC = 0,
            int trapezohedronN = 5,
            int trapezohedronD = 2,
            float trapezohedronHeightA = 0,
            float trapezohedronHeightB = 0,
            char symmetroSym = 'O',
            int symmetroMult0 = 1,
            int symmetroMult1 = 1,
            int symmetroMult2 = 0,
            int johnsonNumber = 6,
            int uniformNumber = 4,
            int wenningerNumber = 1)
        {
            // Check if it's a parameterized type
            switch (polyhedronType)
            {
                case PolyhedronType.Prism:
                    return Geometry.CreatePrism(sides);

                case PolyhedronType.Antiprism:
                    return Geometry.CreateAntiprism(sides);

                case PolyhedronType.Pyramid:
                    return Geometry.CreatePyramid(sides);

                case PolyhedronType.Dipyramid:
                    return Geometry.CreateDipyramid(sides);

                case PolyhedronType.Cupola:
                    return Geometry.CreateCupola(sides);

                case PolyhedronType.Geodesic:
                    return Geometry.CreateGeodesic(geodesicFrequency, geodesicMethod);

                case PolyhedronType.Unitile2D:
                    return Geometry.CreateUnitile2D(tilingPattern, tilingSurface,
                        tilingWidth, tilingHeight, tilingMinorRadius, tilingMajorRadius);

                case PolyhedronType.IsoKite:
                    return Geometry.CreateIsoKite(isoKiteModel,
                        isoKiteHeightA, isoKiteHeightB, isoKiteHeightC);

                case PolyhedronType.Trapezohedron:
                    // Validate fraction before creating
                    if (trapezohedronD >= trapezohedronN)
                    {
                        UnityEngine.Debug.LogError($"Invalid trapezohedron fraction {trapezohedronN}/{trapezohedronD}: d must be < n");
                        trapezohedronD = trapezohedronN - 1;
                    }
                    return Geometry.CreateTrapezohedron(trapezohedronN, trapezohedronD,
                        trapezohedronHeightA, trapezohedronHeightB);

                case PolyhedronType.Symmetrohedra:
                    // Validate symmetry type
                    char validSym = char.ToUpper(symmetroSym);
                    if (validSym != 'T' && validSym != 'O' && validSym != 'I')
                    {
                        UnityEngine.Debug.LogError($"Invalid symmetry type '{symmetroSym}' - must be T, O, or I. Using O.");
                        validSym = 'O';
                    }

                    // Validate multipliers
                    int m0 = System.Math.Max(0, symmetroMult0);
                    int m1 = System.Math.Max(0, symmetroMult1);
                    int m2 = System.Math.Max(0, symmetroMult2);
                    int numMult = (m0 > 0 ? 1 : 0) + (m1 > 0 ? 1 : 0) + (m2 > 0 ? 1 : 0);

                    if (numMult == 0)
                    {
                        UnityEngine.Debug.LogError("All symmetro multipliers are zero - using default (1,1,0)");
                        m0 = 1; m1 = 1; m2 = 0;
                    }
                    else if (numMult == 3)
                    {
                        UnityEngine.Debug.LogError("All three symmetro multipliers are non-zero (invalid) - setting mult2=0");
                        m2 = 0;
                    }

                    return Geometry.CreateSymmetroKaplanHart(validSym, m0, m1, m2);

                case PolyhedronType.JohnsonSolid:
                    {
                        var geom = new Geometry();
                        string resourceName = $"J{johnsonNumber}";
                        Status status = geom.LoadResource(resourceName);
                        if (status != Status.OK)
                        {
                            geom.Dispose();
                            throw new Exception($"Failed to load Johnson solid '{resourceName}': {status}");
                        }
                        return geom;
                    }

                case PolyhedronType.UniformPolyhedron:
                    {
                        var geom = new Geometry();
                        string resourceName = $"U{uniformNumber}";
                        Status status = geom.LoadResource(resourceName);
                        if (status != Status.OK)
                        {
                            geom.Dispose();
                            throw new Exception($"Failed to load Uniform polyhedron '{resourceName}': {status}");
                        }
                        return geom;
                    }

                case PolyhedronType.Wenninger:
                    {
                        var geom = new Geometry();
                        string resourceName = $"W{wenningerNumber}";
                        Status status = geom.LoadResource(resourceName);
                        if (status != Status.OK)
                        {
                            geom.Dispose();
                            throw new Exception($"Failed to load Wenninger stellation '{resourceName}': {status}");
                        }
                        return geom;
                    }

                default:
                    // Non-parameterized type - load from resource
                    var defaultGeom = new Geometry();
                    string defaultResourceName = GetResourceName(polyhedronType);
                    Status defaultStatus = defaultGeom.LoadResource(defaultResourceName);
                    if (defaultStatus != Status.OK)
                    {
                        defaultGeom.Dispose();
                        throw new Exception($"Failed to load polyhedron '{defaultResourceName}': {defaultStatus}");
                    }
                    return defaultGeom;
            }
        }

        [DllImport(LIBRARY_NAME)]
        private static extern IntPtr anti_get_version();

        [DllImport(LIBRARY_NAME)]
        private static extern void anti_set_data_path(string path);
    }

    /// <summary>
    /// Geometry class - represents a 3D polyhedron
    /// </summary>
    public class Geometry : IDisposable
    {
        internal IntPtr handle;
        private bool disposed = false;

        /// <summary>
        /// Create a new empty geometry
        /// </summary>
        public Geometry()
        {
            AntiprismPlugin.EnsureDataPath();
            handle = anti_geometry_create();
            if (handle == IntPtr.Zero)
                throw new Exception("Failed to create geometry");
        }

        ~Geometry()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (handle != IntPtr.Zero)
                {
                    anti_geometry_destroy(handle);
                    handle = IntPtr.Zero;
                }
                disposed = true;
            }
        }

        private void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("Geometry");
        }

        // === FACTORY METHODS FOR PARAMETERIZED POLYHEDRA ===

        /// <summary>
        /// Create an N-sided prism
        /// </summary>
        /// <param name="n">Number of sides (must be >= 3)</param>
        public static Geometry CreatePrism(int n)
        {
            if (n < 3)
                throw new ArgumentException("Prism must have at least 3 sides");

            Geometry geom = new Geometry();
            Status status = anti_make_prism(geom.handle, n);
            if (status != Status.OK)
                throw new Exception($"Failed to create prism: {status}");

            return geom;
        }

        /// <summary>
        /// Create an N-sided antiprism
        /// </summary>
        /// <param name="n">Number of sides (must be >= 3)</param>
        public static Geometry CreateAntiprism(int n)
        {
            if (n < 3)
                throw new ArgumentException("Antiprism must have at least 3 sides");

            Geometry geom = new Geometry();
            Status status = anti_make_antiprism(geom.handle, n);
            if (status != Status.OK)
                throw new Exception($"Failed to create antiprism: {status}");

            return geom;
        }

        /// <summary>
        /// Create an N-sided pyramid
        /// </summary>
        /// <param name="n">Number of sides (must be >= 3)</param>
        public static Geometry CreatePyramid(int n)
        {
            if (n < 3)
                throw new ArgumentException("Pyramid must have at least 3 sides");

            Geometry geom = new Geometry();
            Status status = anti_make_pyramid(geom.handle, n);
            if (status != Status.OK)
                throw new Exception($"Failed to create pyramid: {status}");

            return geom;
        }

        /// <summary>
        /// Create an N-sided dipyramid (bipyramid)
        /// </summary>
        /// <param name="n">Number of sides (must be >= 3)</param>
        public static Geometry CreateDipyramid(int n)
        {
            if (n < 3)
                throw new ArgumentException("Dipyramid must have at least 3 sides");

            Geometry geom = new Geometry();
            Status status = anti_make_dipyramid(geom.handle, n);
            if (status != Status.OK)
                throw new Exception($"Failed to create dipyramid: {status}");

            return geom;
        }

        /// <summary>
        /// Create an N-sided cupola
        /// </summary>
        /// <param name="n">Number of sides (must be >= 2)</param>
        public static Geometry CreateCupola(int n)
        {
            if (n < 2)
                throw new ArgumentException("Cupola must have at least 2 sides");

            Geometry geom = new Geometry();
            Status status = anti_make_cupola(geom.handle, n);
            if (status != Status.OK)
                throw new Exception($"Failed to create cupola: {status}");

            return geom;
        }

        /// <summary>
        /// Create a geodesic sphere by subdividing a base polyhedron
        /// </summary>
        /// <param name="frequency">Subdivision frequency (1-10 recommended)</param>
        /// <param name="method">Base polyhedron to subdivide</param>
        public static Geometry CreateGeodesic(int frequency, GeodesicMethod method = GeodesicMethod.Icosahedron)
        {
            if (frequency < 1)
                throw new ArgumentException("Frequency must be >= 1");

            Geometry geom = new Geometry();
            Status status = anti_make_geodesic(geom.handle, frequency, (int)method);
            if (status != Status.OK)
                throw new Exception($"Failed to create geodesic sphere: {status}");

            return geom;
        }

        /// <summary>
        /// Create a uniform 2D tiling on a surface
        /// </summary>
        /// <param name="pattern">Tiling pattern (1-11)</param>
        /// <param name="surface">Surface to tile on</param>
        /// <param name="width">Width of tiling (number of pattern repeats, typically 10-40)</param>
        /// <param name="height">Height of tiling (0 = use width)</param>
        /// <param name="minorRadius">Minor radius for torus/klein/mobius (tube/strip width, default 1)</param>
        /// <param name="majorRadius">Major radius for torus/klein/mobius (ring radius, default 3)</param>
        /// <returns>New Geometry containing the tiling</returns>
        /// <remarks>
        /// Creates uniform tilings (tessellations) mapped onto various surfaces.
        /// Examples:
        ///   - Square tiling on plane: pattern=Squares_4444, surface=Plane, width=20
        ///   - Triangle tiling on torus: pattern=Triangles_333333, surface=Torus, width=30
        ///   - Hexagon tiling on Klein bottle: pattern=Hexagons_666, surface=KleinBottle
        /// </remarks>
        public static Geometry CreateUnitile2D(TilingPattern pattern, TilingSurface surface = TilingSurface.Plane,
            double width = 20, double height = 0, double minorRadius = 1.0, double majorRadius = 3.0)
        {
            Geometry geom = new Geometry();
            Status status = anti_make_unitile2d(geom.handle, (int)pattern, (int)surface,
                width, height, minorRadius, majorRadius);
            if (status != Status.OK)
                throw new Exception($"Failed to create tiling: {status}");

            return geom;
        }

        /// <summary>
        /// Create an isohedral kite-faced polyhedron from a Schwarz triangle
        /// </summary>
        /// <param name="model">Schwarz triangle model (T1, T2, O1, O2, I1-I10, etc.)</param>
        /// <param name="heightA">Height of kite apex on OA (0 = auto-calculate)</param>
        /// <param name="heightB">Height of kite apex on OB (0 = auto-calculate)</param>
        /// <param name="heightC">Height of kite side vertex on OC (0 = auto-calculate)</param>
        /// <returns>New Geometry containing the kite-faced polyhedron</returns>
        /// <remarks>
        /// Creates isohedral kite-faced polyhedra based on Schwarz triangles.
        /// Examples:
        ///   - T1: Cube/Rhombic Dodecahedron family (tetrahedral symmetry)
        ///   - O1: Rhombic Dodecahedron (octahedral symmetry)
        ///   - I1: Rhombic Triacontahedron (icosahedral symmetry)
        /// Heights are auto-calculated if set to 0.
        /// </remarks>
        public static Geometry CreateIsoKite(SchwarzTriangle model,
            double heightA = 0, double heightB = 0, double heightC = 0)
        {
            Geometry geom = new Geometry();
            string modelName = model.ToString();
            Status status = anti_make_iso_kite(geom.handle, modelName, heightA, heightB, heightC);
            if (status != Status.OK)
                throw new Exception($"Failed to create iso_kite {modelName}: {status}");

            return geom;
        }

        /// <summary>
        /// Create a trapezohedron (kite-faced dipyramid)
        /// </summary>
        /// <param name="n">Numerator of fraction (n >= 2)</param>
        /// <param name="d">Denominator of fraction (0 < d < n)</param>
        /// <param name="heightA">Height of kite apex on OA (0 = use default)</param>
        /// <param name="heightB">Height of kite apex on OB (0 = use default)</param>
        /// <returns>New Geometry containing the trapezohedron</returns>
        /// <remarks>
        /// Creates a trapezohedron based on fraction n/d.
        /// Examples:
        ///   - n=3, d=1: Triangular trapezohedron (cube)
        ///   - n=4, d=1: Square trapezohedron
        ///   - n=5, d=2: Pentagonal trapezohedron
        /// </remarks>
        public static Geometry CreateTrapezohedron(int n, int d, double heightA = 0, double heightB = 0)
        {
            if (n < 2 || d <= 0 || d >= n)
                throw new ArgumentException($"Invalid fraction {n}/{d}: n must be >= 2 and 0 < d < n");

            Geometry geom = new Geometry();
            Status status = anti_make_trapezohedron(geom.handle, n, d, heightA, heightB);
            if (status != Status.OK)
                throw new Exception($"Failed to create trapezohedron {n}/{d}: {status}");

            return geom;
        }

        /// <summary>
        /// Create a symmetrohedron using Kaplan-Hart notation (matches CLI: -k sym,mult0,mult1,mult2)
        /// </summary>
        /// <param name="sym">Symmetry type: 'T' (tetrahedral), 'O' (octahedral), 'I' (icosahedral)</param>
        /// <param name="mult0">Multiplier for primary axis (0 = skip)</param>
        /// <param name="mult1">Multiplier for secondary axis (0 = skip)</param>
        /// <param name="mult2">Multiplier for tertiary axis (0 = skip)</param>
        /// <returns>New Geometry containing the symmetrohedron</returns>
        /// <remarks>
        /// Axis orders: T=[3,3,2], O=[4,3,2], I=[5,3,2]
        /// Examples:
        ///   - Cuboctahedron: sym='O', mult0=1, mult1=1, mult2=0
        ///   - Snub Cube: sym='O', mult0=1, mult1=0, mult2=1
        /// At least one and at most two multipliers must be non-zero.
        /// </remarks>
        public static Geometry CreateSymmetroKaplanHart(char sym, int mult0, int mult1, int mult2)
        {
            if (sym != 'T' && sym != 'O' && sym != 'I')
                throw new ArgumentException("Symmetry must be 'T', 'O', or 'I'");

            if (mult0 < 0 || mult1 < 0 || mult2 < 0)
                throw new ArgumentException("Multipliers must be >= 0");

            int numMultipliers = (mult0 > 0 ? 1 : 0) + (mult1 > 0 ? 1 : 0) + (mult2 > 0 ? 1 : 0);
            if (numMultipliers == 0 || numMultipliers == 3)
                throw new ArgumentException("At least one and at most two multipliers must be non-zero");

            Geometry geom = new Geometry();
            Status status = anti_make_symmetro_kaplan_hart(geom.handle, sym, mult0, mult1, mult2);
            if (status != Status.OK)
                throw new Exception($"Failed to create symmetrohedron: {status}");

            return geom;
        }

        /// <summary>
        /// Create a symmetrohedron with advanced parameters
        /// </summary>
        /// <param name="sym">Symmetry type: 'T', 'O', 'I', 'D', 'S', 'C', 'V', 'H'</param>
        /// <param name="p">First Schläfli parameter</param>
        /// <param name="q">Second Schläfli parameter</param>
        /// <param name="l">Multiplier for first axis</param>
        /// <param name="m">Multiplier for second axis</param>
        /// <param name="d0">D value for first axis (default 1)</param>
        /// <param name="d1">D value for second axis (default 1)</param>
        /// <param name="rotation">Rotation angle in degrees (default 0)</param>
        /// <param name="symId">Symmetry ID number (typically 1)</param>
        /// <returns>New Geometry containing the symmetrohedron</returns>
        public static Geometry CreateSymmetroAdvanced(char sym, int p, int q, int l, int m,
            int d0 = 1, int d1 = 1, double rotation = 0.0, int symId = 1)
        {
            if (sym != 'T' && sym != 'O' && sym != 'I' && sym != 'D' &&
                sym != 'S' && sym != 'C' && sym != 'V' && sym != 'H')
                throw new ArgumentException("Invalid symmetry type");

            if (p < 2 || q < 2)
                throw new ArgumentException("Schläfli parameters must be >= 2");

            if (l < 0 || m < 0)
                throw new ArgumentException("Multipliers must be >= 0");

            Geometry geom = new Geometry();
            Status status = anti_make_symmetro_advanced(geom.handle, sym, p, q, l, m, d0, d1, rotation, symId);
            if (status != Status.OK)
                throw new Exception($"Failed to create symmetrohedron: {status}");

            return geom;
        }

        /// <summary>
        /// Create a zonohedron from a star of vectors
        /// </summary>
        /// <param name="starVectors">Star of vectors defining the zones</param>
        /// <returns>New Geometry containing the zonohedron</returns>
        public static Geometry CreateZonohedron(Vector3[] starVectors)
        {
            if (starVectors == null || starVectors.Length < 1)
                throw new ArgumentException("Star must contain at least one vector");

            // Flatten Vector3[] to double[]
            double[] flatVectors = new double[starVectors.Length * 3];
            for (int i = 0; i < starVectors.Length; i++)
            {
                flatVectors[i * 3 + 0] = starVectors[i].x;
                flatVectors[i * 3 + 1] = starVectors[i].y;
                flatVectors[i * 3 + 2] = starVectors[i].z;
            }

            Geometry geom = new Geometry();
            Status status = anti_make_zonohedron(geom.handle, flatVectors, starVectors.Length);
            if (status != Status.OK)
                throw new Exception($"Failed to create zonohedron: {status}");

            return geom;
        }

        /// <summary>
        /// Create a polar zonohedron from an ordered star of vectors
        /// </summary>
        /// <param name="starVectors">Ordered star of vectors</param>
        /// <param name="step">Step this many places to get to next vector (default: 1)</param>
        /// <param name="spiralStep">Step between ridges of spirallohedron, 0 for regular (default: 0)</param>
        /// <returns>New Geometry containing the polar zonohedron</returns>
        public static Geometry CreatePolarZonohedron(Vector3[] starVectors, int step = 1, int spiralStep = 0)
        {
            if (starVectors == null || starVectors.Length < 1)
                throw new ArgumentException("Star must contain at least one vector");

            if (step < 1)
                throw new ArgumentException("Step must be >= 1");

            // Flatten Vector3[] to double[]
            double[] flatVectors = new double[starVectors.Length * 3];
            for (int i = 0; i < starVectors.Length; i++)
            {
                flatVectors[i * 3 + 0] = starVectors[i].x;
                flatVectors[i * 3 + 1] = starVectors[i].y;
                flatVectors[i * 3 + 2] = starVectors[i].z;
            }

            Geometry geom = new Geometry();
            Status status = anti_make_polar_zonohedron(geom.handle, flatVectors, starVectors.Length, step, spiralStep);
            if (status != Status.OK)
                throw new Exception($"Failed to create polar zonohedron: {status}");

            return geom;
        }

        /// <summary>
        /// Create a zonohedron from the vertices of a seed polyhedron
        /// </summary>
        /// <param name="seed">Seed polyhedron whose vertices define the star</param>
        /// <returns>New Geometry containing the zonohedron</returns>
        /// <remarks>
        /// Examples:
        /// - Cube vertices → Rhombic Dodecahedron
        /// - Dodecahedron vertices → Rhombic Triacontahedron
        /// - Icosahedron vertices → Rhombic Hexecontahedron
        /// </remarks>
        public static Geometry CreateZonohedronFromVertices(Geometry seed)
        {
            if (seed == null)
                throw new ArgumentNullException(nameof(seed));

            // Use vertices as star
            Vector3[] star = seed.GetVertices();
            return CreateZonohedron(star);
        }

        /// <summary>
        /// Create geometry from custom vertices and face indices
        /// This allows you to build Antiprism geometry from any mesh data, then apply
        /// Conway operators and other transformations.
        /// </summary>
        /// <param name="vertices">Collection of vertex positions (List, array, etc.)</param>
        /// <param name="faceIndices">Collection of face index collections (each face is a collection of vertex indices)</param>
        /// <returns>New Geometry object, or null on error</returns>
        public static Geometry CreateFromMesh(IEnumerable<Vector3> vertices, IEnumerable<IEnumerable<int>> faceIndices)
        {
            if (vertices == null)
                throw new ArgumentNullException("vertices");
            if (faceIndices == null)
                throw new ArgumentNullException("faceIndices");

            Geometry geom = new Geometry();
            if (!geom.SetPolyhedronData(vertices, faceIndices))
            {
                geom.Dispose();
                return null;
            }

            return geom;
        }

        /// <summary>
        /// Create geometry from a Unity Mesh
        /// Converts the mesh triangles to polygonal faces (merging coplanar triangles is NOT automatic)
        /// </summary>
        /// <param name="mesh">Unity mesh to convert</param>
        /// <returns>New Geometry object, or null on error</returns>
        public static Geometry CreateFromUnityMesh(Mesh mesh)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Convert triangles to face format
            int[][] faceIndices = new int[triangles.Length / 3][];
            for (int i = 0; i < faceIndices.Length; i++)
            {
                faceIndices[i] = new int[3];
                faceIndices[i][0] = triangles[i * 3];
                faceIndices[i][1] = triangles[i * 3 + 1];
                faceIndices[i][2] = triangles[i * 3 + 2];
            }

            return CreateFromMesh(vertices, faceIndices);
        }

        /// <summary>
        /// Load a built-in polyhedron (e.g., "cube", "tet", "ico", "dodec")
        /// </summary>
        public Status LoadResource(string name)
        {
            CheckDisposed();
            return anti_geometry_read_resource(handle, name);
        }

        /// <summary>
        /// Get the number of vertices
        /// </summary>
        public int VertexCount
        {
            get
            {
                CheckDisposed();
                return anti_geometry_num_verts(handle);
            }
        }

        /// <summary>
        /// Get the number of faces
        /// </summary>
        public int FaceCount
        {
            get
            {
                CheckDisposed();
                return anti_geometry_num_faces(handle);
            }
        }

        /// <summary>
        /// Get all vertices as a Vector3 array
        /// </summary>
        public Vector3[] GetVertices()
        {
            CheckDisposed();
            int count = VertexCount;
            Vector3[] vertices = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                double x, y, z;
                if (anti_geometry_get_vert(handle, i, out x, out y, out z) == Status.OK)
                {
                    vertices[i] = new Vector3((float)x, (float)y, (float)z);
                }
            }

            return vertices;
        }

        /// <summary>
        /// Get all face indices as arrays
        /// </summary>
        public int[][] GetFaces()
        {
            CheckDisposed();
            int faceCount = FaceCount;
            int[][] faces = new int[faceCount][];

            for (int i = 0; i < faceCount; i++)
            {
                int faceSize = anti_geometry_face_num_verts(handle, i);
                faces[i] = new int[faceSize];
                anti_geometry_get_face(handle, i, faces[i], faceSize);
            }

            return faces;
        }

        /// <summary>
        /// Get polyhedron data (vertices and faces) for use with mesh libraries
        /// This is more efficient than GetVertices() + GetFaces() separately
        /// </summary>
        /// <param name="vertices">Output array of vertex positions</param>
        /// <param name="faceIndices">Output array of face index arrays</param>
        public void GetPolyhedronData(out Vector3[] vertices, out int[][] faceIndices)
        {
            Color[] dummyColors;
            GetPolyhedronData(out vertices, out faceIndices, out dummyColors);
        }

        /// <summary>
        /// Get polyhedron data including face colors for use with mesh libraries
        /// This is more efficient than GetVertices() + GetFaces() + individual color queries
        /// </summary>
        /// <param name="vertices">Output array of vertex positions</param>
        /// <param name="faceIndices">Output array of face index arrays</param>
        /// <param name="faceColors">Output array of face colors (null entries for uncolored faces)</param>
        public void GetPolyhedronData(out Vector3[] vertices, out int[][] faceIndices, out Color[] faceColors)
        {
            CheckDisposed();

            // Get vertices
            vertices = GetVertices();

            // Get faces in original form (not triangulated)
            int faceCount = FaceCount;
            if (faceCount == 0)
            {
                faceIndices = new int[0][];
                faceColors = new Color[0];
                return;
            }

            // Allocate buffer for face data (estimate size)
            int bufferSize = faceCount * 10; // Most faces won't exceed 10 vertices
            int[] buffer = new int[bufferSize];
            int totalSize = anti_geometry_get_all_faces(handle, buffer, bufferSize);

            if (totalSize < 0)
            {
                faceIndices = new int[0][];
                faceColors = new Color[0];
                return;
            }

            // Parse the buffer into face arrays
            System.Collections.Generic.List<int[]> faces = new System.Collections.Generic.List<int[]>();
            int offset = 0;
            while (offset < totalSize && faces.Count < faceCount)
            {
                int faceSize = buffer[offset];
                offset++;

                int[] face = new int[faceSize];
                for (int i = 0; i < faceSize; i++)
                {
                    face[i] = buffer[offset];
                    offset++;
                }
                faces.Add(face);
            }

            faceIndices = faces.ToArray();

            // Get face colors
            faceColors = new Color[faceCount];
            for (int i = 0; i < faceCount; i++)
            {
                int r, g, b, a;
                Status status = anti_geometry_get_face_color(handle, i, out r, out g, out b, out a);
                if (status == Status.OK)
                {
                    // Convert from 0-255 to 0-1 range
                    faceColors[i] = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                }
                else
                {
                    // No color set for this face - use white/clear
                    faceColors[i] = new Color(1, 1, 1, 0);
                }
            }
        }

        /// <summary>
        /// Set polyhedron data from vertices and face indices (reverse of GetPolyhedronData)
        /// This allows you to create custom Antiprism geometry that can then be processed
        /// with Conway operators and other transformations.
        /// </summary>
        /// <param name="vertices">Collection of vertex positions</param>
        /// <param name="faceIndices">Collection of face index collections (each face is a collection of vertex indices)</param>
        /// <returns>True if successful, false on error</returns>
        public bool SetPolyhedronData(IEnumerable<Vector3> vertices, IEnumerable<IEnumerable<int>> faceIndices)
        {
            CheckDisposed();

            if (vertices == null)
                throw new ArgumentNullException("vertices");
            if (faceIndices == null)
                throw new ArgumentNullException("faceIndices");

            // Add all vertices
            foreach (var vertex in vertices)
            {
                int vertIdx = anti_geometry_add_vert(handle, vertex.x, vertex.y, vertex.z);
                if (vertIdx < 0)
                {
                    return false; // Error adding vertex
                }
            }

            // Add all faces
            foreach (var face in faceIndices)
            {
                if (face == null)
                    continue;

                // Convert to array for P/Invoke call
                int[] faceArray = face.ToArray();
                if (faceArray.Length < 3)
                {
                    continue; // Skip invalid faces (need at least 3 vertices)
                }

                int faceIdx = anti_geometry_add_face(handle, faceArray, faceArray.Length);
                if (faceIdx < 0)
                {
                    return false; // Error adding face
                }
            }

            return true;
        }

        /// <summary>
        /// Apply geometry to a Unity Mesh with optional flat shading
        /// </summary>
        public void ApplyToMesh(Mesh mesh, bool flatShading = true)
        {
            CheckDisposed();

            if (mesh == null)
                throw new ArgumentNullException("mesh");

            mesh.Clear();

            Vector3[] vertices = GetVertices();
            int[][] faces = GetFaces();

            if (flatShading)
            {
                // Flat shading: duplicate vertices for each triangle so normals are per-face
                System.Collections.Generic.List<Vector3> vertList = new System.Collections.Generic.List<Vector3>();
                System.Collections.Generic.List<int> triList = new System.Collections.Generic.List<int>();

                foreach (int[] face in faces)
                {
                    // Triangulate face using fan triangulation
                    for (int i = 1; i < face.Length - 1; i++)
                    {
                        int baseIdx = vertList.Count;
                        vertList.Add(vertices[face[0]]);
                        vertList.Add(vertices[face[i]]);
                        vertList.Add(vertices[face[i + 1]]);

                        triList.Add(baseIdx);
                        triList.Add(baseIdx + 1);
                        triList.Add(baseIdx + 2);
                    }
                }

                mesh.vertices = vertList.ToArray();
                mesh.triangles = triList.ToArray();
                mesh.RecalculateNormals();
            }
            else
            {
                // Smooth shading: share vertices
                mesh.vertices = vertices;

                System.Collections.Generic.List<int> triList = new System.Collections.Generic.List<int>();
                foreach (int[] face in faces)
                {
                    // Triangulate face using fan triangulation
                    for (int i = 1; i < face.Length - 1; i++)
                    {
                        triList.Add(face[0]);
                        triList.Add(face[i]);
                        triList.Add(face[i + 1]);
                    }
                }

                mesh.triangles = triList.ToArray();
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
        }

        // === GEOMETRY OPERATIONS ===

        /// <summary>
        /// Create the dual polyhedron
        /// </summary>
        public Status Dual(double radius = 1.0)
        {
            CheckDisposed();
            return anti_geometry_dual(handle, radius);
        }

        /// <summary>
        /// Unitize: scale to unit sphere
        /// </summary>
        public Status Unitize()
        {
            CheckDisposed();
            return anti_geometry_unitize(handle);
        }

        /// <summary>
        /// Scale by a factor
        /// </summary>
        public Status Scale(double factor)
        {
            CheckDisposed();
            return anti_geometry_scale(handle, factor);
        }

        /// <summary>
        /// Orient faces consistently
        /// </summary>
        public Status Orient()
        {
            CheckDisposed();
            return anti_geometry_orient(handle);
        }

        /// <summary>
        /// Canonicalize geometry (adjust vertices for more uniform edge lengths)
        /// </summary>
        /// <param name="numIters">Maximum number of iterations (0 for default 1000)</param>
        public Status Canonicalize(int numIters = 0)
        {
            CheckDisposed();
            return anti_geometry_canonicalize(handle, numIters);
        }

        /// <summary>
        /// Apply convex hull
        /// </summary>
        public Status ConvexHull()
        {
            CheckDisposed();
            return anti_geometry_convex_hull(handle);
        }

        // === CONWAY OPERATORS ===

        /// <summary>
        /// Conway Truncate operator
        /// </summary>
        /// <param name="ratio">Truncation ratio (0.0-1.0, typically 0.333)</param>
        /// <param name="order">Truncate only vertices with this order (0 for all)</param>
        public Status Truncate(double ratio = 0.3333, int order = 0)
        {
            CheckDisposed();
            return anti_geometry_truncate(handle, ratio, order);
        }

        /// <summary>
        /// Conway Kis operator (place pyramid on each face)
        /// </summary>
        /// <param name="faceSides">Only kis faces with n sides (0 for all faces, 3 for triangles, 4 for quads, etc.)</param>
        public Status Kis(int faceSides = 0)
        {
            CheckDisposed();
            return anti_geometry_kis(handle, faceSides);
        }

        /// <summary>
        /// Conway Ambo operator (rectify - vertices at edge midpoints)
        /// </summary>
        public Status Ambo()
        {
            CheckDisposed();
            return anti_geometry_ambo(handle);
        }

        /// <summary>
        /// Conway Gyro operator (rotate and subdivide faces)
        /// </summary>
        /// <param name="n">Gyro subscript parameter (default 1)</param>
        public Status Gyro(int n = 1)
        {
            CheckDisposed();
            return anti_geometry_gyro(handle, n);
        }

        /// <summary>
        /// Conway Join operator (dual of ambo)
        /// </summary>
        public Status Join()
        {
            CheckDisposed();
            return anti_geometry_join(handle);
        }

        /// <summary>
        /// Conway Needle operator (elongated kis)
        /// </summary>
        /// <param name="height">Height multiplier for needle points (default 2.0)</param>
        public Status Needle(double height = 2.0)
        {
            CheckDisposed();
            return anti_geometry_needle(handle, height);
        }

        /// <summary>
        /// Conway Zip operator (dual of kis)
        /// </summary>
        public Status Zip()
        {
            CheckDisposed();
            return anti_geometry_zip(handle);
        }

        /// <summary>
        /// Conway Subdivide operator
        /// </summary>
        /// <param name="n">First subscript parameter (default 2)</param>
        /// <param name="m">Second subscript parameter (default 0)</param>
        public Status Subdivide(int n = 2, int m = 0)
        {
            CheckDisposed();
            return anti_geometry_subdivide(handle, n, m);
        }

        /// <summary>
        /// Conway Expand operator (ambo + ambo)
        /// </summary>
        /// <param name="n">First subscript parameter (default 2)</param>
        /// <param name="m">Second subscript parameter (default 0)</param>
        public Status Expand(int n = 2, int m = 0)
        {
            CheckDisposed();
            return anti_geometry_expand(handle, n, m);
        }

        /// <summary>
        /// Conway Meta operator (kis + dual)
        /// </summary>
        /// <param name="n">Meta subscript parameter (default 2)</param>
        public Status Meta(int n = 2)
        {
            CheckDisposed();
            return anti_geometry_meta(handle, n);
        }

        /// <summary>
        /// Conway Bevel operator (truncate + ambo)
        /// </summary>
        /// <param name="n">Bevel subscript parameter (default 2)</param>
        /// <param name="ratio">Truncation ratio (0.0-1.0, typically 0.333)</param>
        public Status Bevel(int n = 2, double ratio = 0.3333)
        {
            CheckDisposed();
            return anti_geometry_bevel(handle, n, ratio);
        }

        /// <summary>
        /// Conway Snub operator (dual + gyro)
        /// </summary>
        /// <param name="n">Snub subscript parameter (default 2)</param>
        public Status Snub(int n = 2)
        {
            CheckDisposed();
            return anti_geometry_snub(handle, n);
        }

        /// <summary>
        /// Conway Ortho operator (join + join)
        /// </summary>
        /// <param name="n">First subscript parameter (default 2)</param>
        /// <param name="m">Second subscript parameter (default 0)</param>
        public Status Ortho(int n = 2, int m = 0)
        {
            CheckDisposed();
            return anti_geometry_ortho(handle, n, m);
        }

        // === P/INVOKE DECLARATIONS ===

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern IntPtr anti_geometry_create();

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern void anti_geometry_destroy(IntPtr geom);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_read_resource(IntPtr geom, string name);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern int anti_geometry_num_verts(IntPtr geom);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern int anti_geometry_num_faces(IntPtr geom);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_get_vert(IntPtr geom, int idx,
            out double x, out double y, out double z);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern int anti_geometry_face_num_verts(IntPtr geom, int faceIdx);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern int anti_geometry_get_face(IntPtr geom, int faceIdx, int[] indices, int maxIndices);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern int anti_geometry_get_all_faces(IntPtr geom, int[] buffer, int bufferSize);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_get_face_color(IntPtr geom, int faceIdx,
            out int r, out int g, out int b, out int a);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern int anti_geometry_add_vert(IntPtr geom, double x, double y, double z);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern int anti_geometry_add_face(IntPtr geom, int[] indices, int numIndices);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_unitize(IntPtr geom);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_scale(IntPtr geom, double factor);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_orient(IntPtr geom);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_canonicalize(IntPtr geom, int num_iters);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_dual(IntPtr geom, double radius);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_convex_hull(IntPtr geom);

        // Conway operators
        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_truncate(IntPtr geom, double ratio, int order);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_kis(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_ambo(IntPtr geom);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_gyro(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_join(IntPtr geom);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_needle(IntPtr geom, double height);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_zip(IntPtr geom);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_subdivide(IntPtr geom, int n, int m);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_expand(IntPtr geom, int n, int m);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_meta(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_bevel(IntPtr geom, int n, double ratio);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_snub(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_geometry_ortho(IntPtr geom, int n, int m);

        // Parameterized polyhedra generators
        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_prism(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_antiprism(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_pyramid(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_dipyramid(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_cupola(IntPtr geom, int n);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_geodesic(IntPtr geom, int frequency, int method);

        // 2D Tiling generators
        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_unitile2d(
            IntPtr geom, int pattern, int surface_type,
            double width, double height, double minor_radius, double major_radius);

        // Kite-faced polyhedra generators
        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_iso_kite(
            IntPtr geom, string model_name,
            double height_a, double height_b, double height_c);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_trapezohedron(
            IntPtr geom, int n, int d,
            double height_a, double height_b);

        // Symmetrohedra generators
        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_symmetro_kaplan_hart(
            IntPtr geom, char sym, int mult0, int mult1, int mult2);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_symmetro_advanced(
            IntPtr geom, char sym, int p, int q, int l, int m,
            int d0, int d1, double rotation, int sym_id);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_zonohedron(
            IntPtr geom, double[] star_vectors, int num_vectors);

        [DllImport(AntiprismPlugin.LIBRARY_NAME)]
        private static extern Status anti_make_polar_zonohedron(
            IntPtr geom, double[] star_vectors, int num_vectors, int step, int spiral_step);
    }
}
