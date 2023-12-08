/*
 * Tactile-JS
 * Copyright 2018 Craig S. Kaplan, csk@uwaterloo.ca
 *
 * Distributed under the terms of the 3-clause BSD license.  See the
 * file "LICENSE" for more information.
 */

using System.Collections.Generic;
using UnityEngine;

namespace Polyhydra.Core.IsohedralGrids
{
    public struct Rhomb
    {
        public List<Vector2> shape;
        public int line1;
        public int parallel1;
        public int line2;
        public int parallel2;
    }
}