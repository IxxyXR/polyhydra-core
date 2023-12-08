/*
 * Tactile-JS
 * Copyright 2018 Craig S. Kaplan, csk@uwaterloo.ca
 *
 * Distributed under the terms of the 3-clause BSD license.  See the
 * file "LICENSE" for more information.
 */

namespace Polyhydra.Core.IsohedralGrids
{
    public struct TilingTypeConfig
    {
        public string tiling_name;
        public string symmetry_group;
        public string grid;
        public int num_params;
        public int num_aspects;
        public int num_vertices;
        public int num_edge_shapes;
        public EdgeShape[] edge_shapes;
        public bool[] edge_orientations;
        public int[] edge_shape_ids;
        public double[] default_params;
        public double[] vertex_coeffs;
        public double[] translation_coeffs;
        public double[] aspect_coeffs;
        public int[] colouring;

        public TilingTypeConfig(string tiling_name, string symmetry_group, string grid, int num_params, int num_aspects,
            int num_vertices, int num_edge_shapes,
            EdgeShape[] edge_shapes, bool[] edge_orientations,
            int[] edge_shape_ids, double[] default_params, double[] vertex_coeffs,
            double[] translation_coeffs, double[] aspect_coeffs, int[] colouring)
        {
            this.tiling_name = tiling_name;
            this.symmetry_group = symmetry_group;
            this.grid = grid;
            this.num_params = num_params;
            this.num_aspects = num_aspects;
            this.num_vertices = num_vertices;
            this.num_edge_shapes = num_edge_shapes;
            this.edge_shapes = edge_shapes;
            this.edge_orientations = edge_orientations;
            this.edge_shape_ids = edge_shape_ids;
            this.default_params = default_params;
            this.vertex_coeffs = vertex_coeffs;
            this.translation_coeffs = translation_coeffs;
            this.aspect_coeffs = aspect_coeffs;
            this.colouring = colouring;
        }
    }
}