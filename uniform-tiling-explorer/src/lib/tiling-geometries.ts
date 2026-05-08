import * as THREE from 'three';

export interface TilingDefinition {
  name: string;
  config: string;
  description: string;
  generate: (rows: number, cols: number) => { 
    vertices: number[]; 
    indices: number[]; 
    faces: number[][];
  };
}

/**
 * Unified mesh builder that handles vertex deduplication and face normalization.
 * Ensures the construction "emerges naturally" with shared vertices.
 */
class TilingMeshBuilder {
  vertices: number[] = [];
  vMap = new Map<string, number>();
  faces: number[][] = [];
  faceSet = new Set<string>();

  getV(x: number, y: number): number {
    // Round to 8 decimal places and normalize zero to avoid -0.0
    const precision = 1e8;
    const vx = Math.round(x * precision) / precision + 0;
    const vy = Math.round(y * precision) / precision + 0;
    const k = `${vx.toFixed(8)},${vy.toFixed(8)}`;
    
    const existing = this.vMap.get(k);
    if (existing !== undefined) return existing;

    const idx = this.vertices.length / 3;
    this.vertices.push(vx, vy, 0);
    this.vMap.set(k, idx);
    return idx;
  }

  addFace(points: [number, number][]) {
    const indices = points.map(p => this.getV(p[0], p[1]));
    const unique = indices.filter((val, i, arr) => val !== arr[(i + 1) % arr.length]);
    if (unique.length < 3) return;

    // Force CCW orientation using 2D signed area
    let area = 0;
    for (let i = 0; i < unique.length; i++) {
        const v1Idx = unique[i];
        const v2Idx = unique[(i + 1) % unique.length];
        const x1 = this.vertices[v1Idx * 3];
        const y1 = this.vertices[v1Idx * 3 + 1];
        const x2 = this.vertices[v2Idx * 3];
        const y2 = this.vertices[v2Idx * 3 + 1];
        area += (x1 * y2 - x2 * y1);
    }
    if (area < 0) unique.reverse();

    // Canonical form for deduplication
    const minVal = Math.min(...unique);
    const minIdx = unique.indexOf(minVal);
    const canonical = [...unique.slice(minIdx), ...unique.slice(0, minIdx)];
    const key = canonical.join(',');

    if (!this.faceSet.has(key)) {
      this.faceSet.add(key);
      this.faces.push(unique);
    }
  }

  fillTriangles() {
    const vertexCount = this.vertices.length / 3;
    const neighbors = Array.from({ length: vertexCount }, () => new Set<number>());
    
    // Spatial grid for neighbor search
    const cellSize = 1.1;
    const spatial: Record<string, number[]> = {};
    for (let i = 0; i < vertexCount; i++) {
      const vx = this.vertices[i*3];
      const vy = this.vertices[i*3+1];
      const k = `${Math.floor(vx/cellSize)},${Math.floor(vy/cellSize)}`;
      if (!spatial[k]) spatial[k] = [];
      spatial[k].push(i);
    }

    for (let i = 0; i < vertexCount; i++) {
      const vx = this.vertices[i*3];
      const vy = this.vertices[i*3+1];
      const gx = Math.floor(vx/cellSize);
      const gy = Math.floor(vy/cellSize);
      
      for (let dx = -1; dx <= 1; dx++) {
        for (let dy = -1; dy <= 1; dy++) {
          const cell = spatial[`${gx+dx},${gy+dy}`];
          if (!cell) continue;
          for (const j of cell) {
            if (i === j) continue;
            const ux = this.vertices[j*3];
            const uy = this.vertices[j*3+1];
            const d2 = (vx-ux)**2 + (vy-uy)**2;
            if (Math.abs(d2 - 1.0) < 1e-6) neighbors[i].add(j);
          }
        }
      }
    }

    for (let i = 0; i < vertexCount; i++) {
      const nList = Array.from(neighbors[i]);
      for (let a = 0; a < nList.length; a++) {
        const j = nList[a];
        for (let b = a + 1; b < nList.length; b++) {
          const k = nList[b];
          if (neighbors[j].has(k)) {
            // Found equilateral triangle (i, j, k)
            const p1: [number, number] = [this.vertices[i*3], this.vertices[i*3+1]];
            const p2: [number, number] = [this.vertices[j*3], this.vertices[j*3+1]];
            const p3: [number, number] = [this.vertices[k*3], this.vertices[k*3+1]];
            this.addFace([p1, p2, p3]);
          }
        }
      }
    }
  }

  build() {
    const indices: number[] = [];
    this.faces.forEach(f => {
      for (let i = 1; i < f.length - 1; i++) {
        indices.push(f[0], f[i], f[i+1]);
      }
    });
    return { vertices: this.vertices, indices, faces: this.faces };
  }
}

/**
 * Legacy buildMesh wrapper using TilingMeshBuilder
 */
function buildMesh(faces: [number, number][][]) {
  const builder = new TilingMeshBuilder();
  faces.forEach(f => builder.addFace(f));
  return builder.build();
}

/**
 * Fills holes with equilateral triangles (Legacy wrapper)
 */
function fillUniformTriangles(faces: [number, number][][]) {
  // This is now discouraged; use TilingMeshBuilder.fillTriangles() instead
  // But for minimal diff, we'll keep it as a wrapper that just processes the faces array
  const builder = new TilingMeshBuilder();
  faces.forEach(f => builder.addFace(f));
  builder.fillTriangles();
  
  // Re-populate the faces array from the builder's Deduplicated face list
  faces.length = 0;
  builder.faces.forEach(f => {
    faces.push(f.map(idx => [builder.vertices[idx*3], builder.vertices[idx*3+1]]));
  });
}

/**
 * Generates a regular polygon
 */
const regPoly = (cx: number, cy: number, r: number, sides: number, startAngle: number): [number, number][] => {
  const points: [number, number][] = [];
  for (let i = 0; i < sides; i++) {
    const a = startAngle + (i * 2 * Math.PI / sides);
    points.push([cx + r * Math.cos(a), cy + r * Math.sin(a)]);
  }
  return points;
};

const gridOffset = (index: number, count: number) => index - Math.floor(count / 2);

export const UNIFORM_TILINGS: Record<string, TilingDefinition> = {
  '3.3.3.3.3.3': {
    name: 'Triangular',
    config: '3.3.3.3.3.3',
    description: 'Equilateral triangles. Every vertex has 6 triangles.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const h = s * Math.sqrt(3) / 2;
      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          const x = (c + (r % 2 ? 0.5 : 0)) * s;
          const y = r * h;
          // Up-pointing
          builder.addFace([[x, y], [x + s, y], [x + s / 2, y + h]]);
          // Down-pointing
          builder.addFace([[x + s, y], [x + 1.5 * s, y + h], [x + s / 2, y + h]]);
        }
      }
      return builder.build();
    }
  },

  '4.4.4.4': {
    name: 'Square',
    config: '4.4.4.4',
    description: 'Regular squares. Every vertex has 4 squares.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          builder.addFace([[c, r], [c+1, r], [c+1, r+1], [c, r+1]]);
        }
      }
      return builder.build();
    }
  },

  '6.6.6': {
    name: 'Hexagonal',
    config: '6.6.6',
    description: 'Regular hexagons. Every vertex has 3 hexagons.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0 / Math.sqrt(3); 
      const w = Math.sqrt(3) * s;
      const h = 1.5 * s;
      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          const cx = (c + (r % 2 ? 0.5 : 0)) * w;
          const cy = r * h;
          builder.addFace(regPoly(cx, cy, s, 6, Math.PI / 6));
        }
      }
      return builder.build();
    }
  },

  '3.6.3.6': {
    name: 'Trihexagonal',
    config: '3.6.3.6',
    description: 'Hexagons and triangles meeting at each vertex.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const L = 2.0 * s;
      const w = L;
      const h = L * Math.sqrt(3) / 2;

      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          const cx = (c + (r % 2 ? 0.5 : 0)) * w;
          const cy = r * h;
          // Hexagon rotated 0 -> vertices at 0, 60... (distance s)
          // Neighbors at 2s touch at vertices.
          builder.addFace(regPoly(cx, cy, s, 6, 0));
        }
      }
      builder.fillTriangles();
      return builder.build();
    }
  },

  '4.8.8': {
    name: 'Truncated Square',
    config: '4.8.8',
    description: 'Octagons and squares.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const D = s * (1 + Math.sqrt(2));
      const rOct = s / (2 * Math.sin(Math.PI / 8));
      const rSq = s / Math.sqrt(2);
      
      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          builder.addFace(regPoly(c * D, r * D, rOct, 8, Math.PI / 8));
          builder.addFace(regPoly((c + 0.5) * D, (r + 0.5) * D, rSq, 4, 0));
        }
      }
      return builder.build();
    }
  },

  '3.12.12': {
    name: 'Truncated Hexagonal',
    config: '3.12.12',
    description: 'Dodecagons and triangles.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const L = s * (2 + Math.sqrt(3));
      const w = L;
      const h = L * Math.sqrt(3) / 2;
      const rDod = s / (2 * Math.sin(Math.PI / 12));

      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          const cx = (c + (r % 2 ? 0.5 : 0)) * w;
          const cy = r * h;
          builder.addFace(regPoly(cx, cy, rDod, 12, Math.PI / 12));
        }
      }
      builder.fillTriangles();
      return builder.build();
    }
  },

  '3.4.6.4': {
    name: 'Small Rhombitrihexagonal',
    config: '3.4.6.4',
    description: 'Hexagons, squares and triangles.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const L = s * (1 + Math.sqrt(3));
      const w = L;
      const h = L * Math.sqrt(3) / 2;
      const rHex = s;
      const rSq = s / Math.sqrt(2);

      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          const cx = (c + (r % 2 ? 0.5 : 0)) * w;
          const cy = r * h;
          
          // Hexagon rotated 30 (PI/6) -> Edges at 0, 60, ...
          builder.addFace(regPoly(cx, cy, rHex, 6, Math.PI / 6));

          // Squares on edges at 0, 60, 120 (3 per hexagon to tile grid)
          for (let i = 0; i < 3; i++) {
            const angle = (i * 60) * Math.PI / 180;
            const dist = L / 2;
            builder.addFace(regPoly(cx + dist * Math.cos(angle), cy + dist * Math.sin(angle), rSq, 4, angle + Math.PI / 4));
          }
        }
      }
      builder.fillTriangles();
      return builder.build();
    }
  },

  '4.6.12': {
    name: 'Great Rhombitrihexagonal',
    config: '4.6.12',
    description: 'Dodecagons, hexagons and squares.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const L = s * (3 + Math.sqrt(3));
      const w = L;
      const h = L * Math.sqrt(3) / 2;
      const rDod = s / (2 * Math.sin(Math.PI / 12));
      const rHex = s;
      const rSq = s / Math.sqrt(2);

      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          const cx = (c + (r % 2 ? 0.5 : 0)) * w;
          const cy = r * h;
          
          // Dodecagon at 15 deg (PI/12) -> Edges at 0, 30, 60...
          builder.addFace(regPoly(cx, cy, rDod, 12, Math.PI / 12));
          
          // 3 Squares per cell on edges at 0, 60, 120 (shared by 2)
          for (let i = 0; i < 3; i++) {
            const angle = (i * 60) * Math.PI / 180;
            const dist = L / 2;
            builder.addFace(regPoly(cx + dist * Math.cos(angle), cy + dist * Math.sin(angle), rSq, 4, angle + Math.PI / 4));
          }
          
          // 2 Hexagons per cell at dual center positions (shared by 3)
          // Hex rotation 0 matches dodec edge at 30 deg.
          builder.addFace(regPoly(cx + 0.5 * w, cy + h / 3, rHex, 6, 0));
          builder.addFace(regPoly(cx, cy + (2 * h / 3), rHex, 6, 0));
        }
      }
      return builder.build();
    }
  },

  '3.3.4.3.4': {
    name: 'Snub Square',
    config: '3.3.4.3.4',
    description: 'A chiral tiling of squares and triangles.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const alpha = 15 * Math.PI / 180;
      const L = s * Math.sqrt(2 + Math.sqrt(3));
      const rSq = s / Math.sqrt(2);
      
      const getSq = (cx: number, cy: number, rot: number) => regPoly(cx, cy, rSq, 4, rot + Math.PI / 4);

      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          const x0 = c * L;
          const y0 = r * L;
          const x1 = (c + 0.5) * L;
          const y1 = (r + 0.5) * L;

          builder.addFace(getSq(x0, y0, alpha));
          builder.addFace(getSq(x1, y1, -alpha));
        }
      }
      
      builder.fillTriangles();
      return builder.build();
    }
  },

  '3.3.3.4.4': {
    name: 'Elongated Triangular',
    config: '3.3.3.4.4',
    description: 'Squares and triangles in rows with alternate row offset.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const h = s * Math.sqrt(3) / 2;
      const stepY = s + h;
      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        const offset = (r % 2 === 0) ? 0 : s / 2;
        const y0 = r * stepY;
        
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          const x0 = c * s + offset;
          // Square row
          builder.addFace([[x0, y0], [x0 + s, y0], [x0 + s, y0 + s], [x0, y0 + s]]);
        }
      }
      builder.fillTriangles();
      return builder.build();
    }
  },

  '3.3.3.3.6': {
    name: 'Snub Hexagonal',
    config: '3.3.3.3.6',
    description: 'A chiral tiling of hexagons and triangles.',
    generate: (rows, cols) => {
      const builder = new TilingMeshBuilder();
      const s = 1.0;
      const alpha = Math.atan(Math.sqrt(3) / 5);
      const L = s * Math.sqrt(7);
      const w = L;
      const h = L * Math.sqrt(3) / 2;
      
      const getHex = (c: number, r: number) => {
        const cx = (c + (r % 2 ? 0.5 : 0)) * w;
        const cy = r * h;
        return regPoly(cx, cy, s, 6, alpha);
      };

      for (let rowIndex = 0; rowIndex < rows; rowIndex++) {
        const r = gridOffset(rowIndex, rows);
        for (let colIndex = 0; colIndex < cols; colIndex++) {
          const c = gridOffset(colIndex, cols);
          builder.addFace(getHex(c, r));
        }
      }
      
      builder.fillTriangles();
      return builder.build();
    }
  }

};
