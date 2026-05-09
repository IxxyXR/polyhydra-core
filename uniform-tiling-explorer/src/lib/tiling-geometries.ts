import * as THREE from 'three';
import { applyOperator, createOperatorSpec, OperatorSpec } from './conway-operators';

export interface GeneratedTilingMesh {
  vertices: number[];
  indices: number[];
  faces: number[][];
  faceValues?: number[];
}

export interface MultiGridSettings {
  dimensions: number;
  divisions: number;
  offset: number;
  randomize: boolean;
  sharedVertices: boolean;
  minDistance: number;
  maxDistance: number;
  colorRatio: number;
  colorIntersect: number;
  colorIndex: number;
  randomSeed: number;
}

export interface TilingGenerationOptions {
  multigrid?: MultiGridSettings;
}

export const MULTIGRID_DEFAULTS: MultiGridSettings = {
  dimensions: 5,
  divisions: 5,
  offset: 0.2,
  randomize: false,
  sharedVertices: true,
  minDistance: 0,
  maxDistance: 0.35,
  colorRatio: 1,
  colorIntersect: 0,
  colorIndex: 0,
  randomSeed: 1,
};

export interface TilingDefinition {
  name: string;
  config: string;
  description: string;
  generate: (rows: number, cols: number, options?: TilingGenerationOptions) => GeneratedTilingMesh;
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

const buildPolygonMesh = (
  polygons: THREE.Vector2[][],
  sharedVertices = true,
  faceValues?: number[],
): GeneratedTilingMesh => {
  if (sharedVertices) {
    const builder = new TilingMeshBuilder();
    polygons.forEach((polygon) => {
      builder.addFace(polygon.map((point) => [point.x, point.y] as [number, number]));
    });
    const mesh = builder.build();
    return faceValues ? { ...mesh, faceValues: [...faceValues] } : mesh;
  }

  const vertices: number[] = [];
  const faces: number[][] = [];
  const indices: number[] = [];
  const outputFaceValues: number[] = [];

  polygons.forEach((polygon, polygonIndex) => {
    if (polygon.length < 3) {
      return;
    }

    const orientedPolygon = getPolygonSignedArea(polygon) < 0
      ? [...polygon].reverse()
      : polygon;
    const baseIndex = vertices.length / 3;
    const face: number[] = [];

    orientedPolygon.forEach((point, pointIndex) => {
      vertices.push(point.x, point.y, 0);
      face.push(baseIndex + pointIndex);
    });

    for (let i = 1; i < face.length - 1; i++) {
      indices.push(face[0], face[i], face[i + 1]);
    }

    faces.push(face);
    if (faceValues) {
      outputFaceValues.push(faceValues[polygonIndex]);
    }
  });

  return faceValues
    ? { vertices, indices, faces, faceValues: outputFaceValues }
    : { vertices, indices, faces };
};

const getPolygonSignedArea = (polygon: THREE.Vector2[]): number => {
  let area = 0;

  for (let i = 0; i < polygon.length; i++) {
    const current = polygon[i];
    const next = polygon[(i + 1) % polygon.length];
    area += current.x * next.y - next.x * current.y;
  }

  return area / 2;
};

const getPolygonEdgeLength = (polygon: THREE.Vector2[]): number => {
  if (polygon.length < 2) {
    return 0;
  }

  return polygon[0].distanceTo(polygon[polygon.length - 1]);
};

const normalizePolygons = (polygons: THREE.Vector2[][]): THREE.Vector2[][] => {
  const referencePolygon = polygons.find((polygon) => polygon.length >= 2);
  if (!referencePolygon) {
    return polygons;
  }

  const edgeLength = getPolygonEdgeLength(referencePolygon);
  if (edgeLength === 0) {
    return polygons;
  }

  const scale = 1 / edgeLength;
  return polygons.map((polygon) => polygon.map((point) => point.clone().multiplyScalar(scale)));
};

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

interface RepeatedTilePattern {
  tile: TileMesh2D;
  xOffset: THREE.Vector2;
  yOffset: THREE.Vector2;
}

interface MultiGridRhomb {
  shape: THREE.Vector2[];
  parallel1: number;
  parallel2: number;
  line1: number;
  line2: number;
}

class TileMesh2D {
  vertices: THREE.Vector2[];
  faces: number[][];

  constructor(vertices: THREE.Vector2[], faces: number[][]) {
    this.vertices = vertices;
    this.faces = faces;
  }

  static polygon(sides: number): TileMesh2D {
    const vertices: THREE.Vector2[] = [];
    const theta = (Math.PI * 2) / sides;

    for (let i = sides - 1; i >= 0; i--) {
      const angle = theta * i;
      vertices.push(new THREE.Vector2(Math.cos(angle), Math.sin(angle)));
    }

    return new TileMesh2D(
      vertices,
      [Array.from({ length: sides }, (_, index) => index)],
    );
  }

  rotate(angleDegrees: number): TileMesh2D {
    const angle = THREE.MathUtils.degToRad(angleDegrees);
    const cos = Math.cos(angle);
    const sin = Math.sin(angle);

    this.vertices = this.vertices.map((vertex) => (
      new THREE.Vector2(
        vertex.x * cos - vertex.y * sin,
        vertex.x * sin + vertex.y * cos,
      )
    ));

    return this;
  }

  scale(factor: number): TileMesh2D {
    this.vertices = this.vertices.map((vertex) => vertex.clone().multiplyScalar(factor));
    return this;
  }

  kis(): TileMesh2D {
    const vertices = this.vertices.map((vertex) => vertex.clone());
    const faces: number[][] = [];

    for (let faceIndex = 0; faceIndex < this.faces.length; faceIndex++) {
      const face = this.faces[faceIndex];
      const centroidIndex = vertices.length;
      vertices.push(this.getFaceCentroid(faceIndex));

      for (let edgeIndex = 0; edgeIndex < face.length; edgeIndex++) {
        const prevIndex = face[(edgeIndex - 1 + face.length) % face.length];
        const vertexIndex = face[edgeIndex];
        faces.push([prevIndex, vertexIndex, centroidIndex]);
      }
    }

    return new TileMesh2D(vertices, faces);
  }

  extendFace(faceIndex: number, edgeIndex: number, sides: number): number {
    if (!this.isBoundaryEdge(faceIndex, edgeIndex)) {
      return -1;
    }

    const face = this.faces[faceIndex];
    const normalizedEdgeIndex = mod(edgeIndex, face.length);
    const vertexIndex = face[normalizedEdgeIndex];
    const prevIndex = face[mod(normalizedEdgeIndex - 1, face.length)];
    const vertex = this.vertices[vertexIndex];
    const prevVertex = this.vertices[prevIndex];
    const midpoint = vertex.clone().add(prevVertex).multiplyScalar(0.5);
    const edgeVector = midpoint.clone().sub(this.getFaceCentroid(faceIndex)).normalize();
    const sideAngle = ((sides - 2) * 180) / sides;
    const opposite = vertex.distanceTo(prevVertex) / 2;
    const adjacent = Math.tan(THREE.MathUtils.degToRad(sideAngle / 2)) * opposite;
    const newCentroid = midpoint.clone().add(edgeVector.multiplyScalar(adjacent));
    const faceRotationSign = Math.sign(this.getFaceSignedArea(faceIndex)) || 1;
    const spoke = vertex.clone().sub(newCentroid);
    const newFace = [vertexIndex, prevIndex];

    for (let i = 2; i < sides; i++) {
      const angle = THREE.MathUtils.degToRad((360 / sides) * i * faceRotationSign);
      const rotated = rotateVector(spoke, angle);
      this.vertices.push(newCentroid.clone().add(rotated));
      newFace.push(this.vertices.length - 1);
    }

    this.faces.push(newFace);
    return this.faces.length - 1;
  }

  addKite(faceIndexA: number, edgeIndexA: number, faceIndexB: number, edgeIndexB: number): void {
    const edgeA = this.getFaceEdge(faceIndexA, edgeIndexA);
    const edgeB = this.getFaceEdge(faceIndexB, edgeIndexB);
    const pivot = this.vertices[edgeA.vertexIndex];
    const angle = new THREE.Vector2().subVectors(this.vertices[edgeB.nextIndex], pivot)
      .angleTo(new THREE.Vector2().subVectors(this.vertices[edgeB.vertexIndex], pivot));
    const reflected = rotateVector(
      this.vertices[edgeB.vertexIndex].clone().sub(pivot),
      angle * 2,
    );

    this.vertices.push(pivot.clone().add(reflected));
    this.faces.push([
      edgeB.nextIndex,
      edgeB.vertexIndex,
      edgeA.vertexIndex,
      this.vertices.length - 1,
    ]);
  }

  addRhombus(faceIndex: number, edgeIndex: number, angleDegrees: number): void {
    const edge = this.getFaceEdge(faceIndex, edgeIndex);
    const pivot = this.vertices[edge.vertexIndex];
    const angle = -THREE.MathUtils.degToRad(angleDegrees);
    const newVert1 = pivot.clone().add(
      rotateVector(this.vertices[edge.prevIndex].clone().sub(pivot), angle),
    );

    this.vertices.push(newVert1);
    const angle2 = -THREE.MathUtils.degToRad((360 - (angleDegrees * 2)) / 2);
    const pivot2 = newVert1;
    const newVert2 = pivot2.clone().add(
      rotateVector(this.vertices[edge.vertexIndex].clone().sub(pivot2), angle2),
    );

    this.vertices.push(newVert2);
    this.faces.push([
      edge.vertexIndex,
      edge.prevIndex,
      this.vertices.length - 1,
      this.vertices.length - 2,
    ]);
  }

  vertexDelta(a: number, b: number): THREE.Vector2 {
    return this.vertices[a].clone().sub(this.vertices[b]);
  }

  private getFaceCentroid(faceIndex: number): THREE.Vector2 {
    const face = this.faces[faceIndex];
    const centroid = new THREE.Vector2();

    for (const vertexIndex of face) {
      centroid.add(this.vertices[vertexIndex]);
    }

    return centroid.multiplyScalar(1 / face.length);
  }

  private getFaceSignedArea(faceIndex: number): number {
    const face = this.faces[faceIndex];
    let area = 0;

    for (let i = 0; i < face.length; i++) {
      const current = this.vertices[face[i]];
      const next = this.vertices[face[(i + 1) % face.length]];
      area += current.x * next.y - next.x * current.y;
    }

    return area / 2;
  }

  private isBoundaryEdge(faceIndex: number, edgeIndex: number): boolean {
    const face = this.faces[faceIndex];
    const normalizedEdgeIndex = mod(edgeIndex, face.length);
    const vertexIndex = face[normalizedEdgeIndex];
    const prevIndex = face[mod(normalizedEdgeIndex - 1, face.length)];

    for (let otherFaceIndex = 0; otherFaceIndex < this.faces.length; otherFaceIndex++) {
      const otherFace = this.faces[otherFaceIndex];

      for (let otherEdgeIndex = 0; otherEdgeIndex < otherFace.length; otherEdgeIndex++) {
        if (otherFaceIndex === faceIndex && otherEdgeIndex === normalizedEdgeIndex) {
          continue;
        }

        const otherVertexIndex = otherFace[otherEdgeIndex];
        const otherPrevIndex = otherFace[mod(otherEdgeIndex - 1, otherFace.length)];
        if (otherVertexIndex === prevIndex && otherPrevIndex === vertexIndex) {
          return false;
        }
      }
    }

    return true;
  }

  private getFaceEdge(faceIndex: number, edgeIndex: number) {
    const face = this.faces[faceIndex];
    const normalizedEdgeIndex = mod(edgeIndex, face.length);
    return {
      vertexIndex: face[normalizedEdgeIndex],
      prevIndex: face[mod(normalizedEdgeIndex - 1, face.length)],
      nextIndex: face[mod(normalizedEdgeIndex + 1, face.length)],
    };
  }
}

const mod = (value: number, divisor: number) => ((value % divisor) + divisor) % divisor;

const rotateVector = (vector: THREE.Vector2, angle: number): THREE.Vector2 => {
  const cos = Math.cos(angle);
  const sin = Math.sin(angle);
  return new THREE.Vector2(
    vector.x * cos - vector.y * sin,
    vector.x * sin + vector.y * cos,
  );
};

const normalizePattern = (pattern: RepeatedTilePattern): RepeatedTilePattern => {
  const firstFace = pattern.tile.faces[0];
  const edgeLength = pattern.tile.vertices[firstFace[0]].distanceTo(
    pattern.tile.vertices[firstFace[firstFace.length - 1]],
  );
  const scale = edgeLength === 0 ? 1 : 1 / edgeLength;

  pattern.tile.scale(scale);
  pattern.xOffset.multiplyScalar(scale);
  pattern.yOffset.multiplyScalar(scale);

  return pattern;
};

const createPattern = (
  tile: TileMesh2D,
  xOffsetA: number,
  xOffsetB: number,
  yOffsetA: number,
  yOffsetB: number,
): RepeatedTilePattern => normalizePattern({
  tile,
  xOffset: tile.vertexDelta(xOffsetA, xOffsetB),
  yOffset: tile.vertexDelta(yOffsetA, yOffsetB),
});

const buildRepeatedTile = (
  pattern: RepeatedTilePattern,
  rows: number,
  cols: number,
) : GeneratedTilingMesh => {
  const builder = new TilingMeshBuilder();
  const xCentering = pattern.xOffset.clone().multiplyScalar((cols - 1) / 2);
  const yCentering = pattern.yOffset.clone().multiplyScalar((rows - 1) / 2);

  for (let row = 0; row < rows; row++) {
    for (let col = 0; col < cols; col++) {
      const tileOffset = pattern.xOffset.clone().multiplyScalar(col).sub(xCentering)
        .add(pattern.yOffset.clone().multiplyScalar(row).sub(yCentering));

      for (const face of pattern.tile.faces) {
        builder.addFace(face.map((vertexIndex) => {
          const vertex = pattern.tile.vertices[vertexIndex].clone().add(tileOffset);
          return [vertex.x, vertex.y] as [number, number];
        }));
      }
    }
  }

  return builder.build();
};

const triangulateFaces = (faces: number[][]): number[] => {
  const indices: number[] = [];
  for (const face of faces) {
    for (let i = 1; i < face.length - 1; i++) {
      indices.push(face[0], face[i], face[i + 1]);
    }
  }
  return indices;
};

const seededRandom = (seed: number) => {
  let state = (seed >>> 0) || 1;

  return () => {
    state = ((state * 1664525) + 1013904223) >>> 0;
    return state / 4294967296;
  };
};

const intersectLines = (
  a: { start: THREE.Vector2; end: THREE.Vector2 },
  b: { start: THREE.Vector2; end: THREE.Vector2 },
): THREE.Vector2 | null => {
  const tmp = (b.end.x - b.start.x) * (a.end.y - a.start.y)
    - (b.end.y - b.start.y) * (a.end.x - a.start.x);

  if (Math.abs(tmp) < 1e-12) {
    return null;
  }

  const mu = ((a.start.x - b.start.x) * (a.end.y - a.start.y)
    - (a.start.y - b.start.y) * (a.end.x - a.start.x)) / tmp;

  return new THREE.Vector2(
    b.start.x + (b.end.x - b.start.x) * mu,
    b.start.y + (b.end.y - b.start.y) * mu,
  );
};

const getMultigridIndicesFromPoint = (
  point: THREE.Vector2,
  angles: number[],
  offset: number,
): number[] => angles.map((angle) => {
  const index = point.x * Math.sin(angle) + point.y * Math.cos(angle);
  return Math.floor(index - offset + 1);
});

const getMultigridVertex = (indices: number[], angles: number[]): THREE.Vector2 => {
  let x = 0;
  let y = 0;

  for (let i = 0; i < indices.length; i++) {
    x += indices[i] * Math.cos(angles[i]);
    y += indices[i] * Math.sin(angles[i]);
  }

  return new THREE.Vector2(x, y);
};

const generateMultigridRhombs = (settings: MultiGridSettings): MultiGridRhomb[] => {
  const rhombs: MultiGridRhomb[] = [];
  const angles: number[] = [];
  const halfLines = settings.divisions;
  const totalLines = (halfLines * 2) + 1;

  if (settings.randomize) {
    const nextRandom = seededRandom(settings.randomSeed);
    let angle = 0;
    while (angle < Math.PI) {
      const step = 0.00001 + ((Math.PI / (settings.divisions / 2)) - 0.00001) * nextRandom();
      angle += step;
      angles.push(angle);
    }
  } else {
    for (let i = 0; i < settings.dimensions; i++) {
      angles.push(2 * (Math.PI / settings.dimensions) * i);
    }
  }

  for (let i = 0; i < angles.length; i++) {
    const angle1 = angles[i];
    const p1 = new THREE.Vector2(totalLines * Math.cos(angle1), -totalLines * Math.sin(angle1));
    const p2 = p1.clone().negate();

    for (let parallel1 = 0; parallel1 < totalLines; parallel1++) {
      const index1 = halfLines - parallel1;
      const offset1 = new THREE.Vector2(
        (index1 + settings.offset) * Math.sin(angle1),
        (index1 + settings.offset) * Math.cos(angle1),
      );
      const l1 = { start: p1.clone().add(offset1), end: p2.clone().add(offset1) };

      for (let k = i + 1; k < angles.length; k++) {
        const angle2 = angles[k];
        const p3 = new THREE.Vector2(totalLines * Math.cos(angle2), -totalLines * Math.sin(angle2));
        const p4 = p3.clone().negate();

        for (let parallel2 = 0; parallel2 < totalLines; parallel2++) {
          const index2 = halfLines - parallel2;
          const offset2 = new THREE.Vector2(
            (index2 + settings.offset) * Math.sin(angle2),
            (index2 + settings.offset) * Math.cos(angle2),
          );
          const l2 = { start: p3.clone().add(offset2), end: p4.clone().add(offset2) };
          const intersect = intersectLines(l1, l2);

          if (!intersect) {
            continue;
          }

          const indices = getMultigridIndicesFromPoint(intersect, angles, settings.offset);
          indices[i] = index1 + 1;
          indices[k] = index2 + 1;
          const v0 = getMultigridVertex(indices, angles);
          indices[i] = index1;
          indices[k] = index2 + 1;
          const v1 = getMultigridVertex(indices, angles);
          indices[i] = index1;
          indices[k] = index2;
          const v2 = getMultigridVertex(indices, angles);
          indices[i] = index1 + 1;
          indices[k] = index2;
          const v3 = getMultigridVertex(indices, angles);

          rhombs.push({
            shape: [v0, v1, v2, v3],
            parallel1: index1,
            parallel2: index2,
            line1: i,
            line2: k,
          });
        }
      }
    }
  }

  return rhombs;
};

const generateMultigridMesh = (settings: MultiGridSettings): GeneratedTilingMesh => {
  const size = new THREE.Vector2(1, 1);
  const diameter = size.length();
  const scale = diameter / 2 / settings.divisions;
  let tf = new THREE.Vector2(size.x / 2, size.y / 2).multiplyScalar(scale);

  if (settings.dimensions % 2 > 0) {
    tf = rotateVector(tf, THREE.MathUtils.degToRad((Math.PI / settings.dimensions) * 0.5));
  }

  const polygons: THREE.Vector2[][] = [];
  const faceValues: number[] = [];
  const rhombs = generateMultigridRhombs(settings);
  const sqrMaxDistance = settings.maxDistance * settings.maxDistance;
  const sqrMinDistance = settings.minDistance * settings.minDistance;

  for (const rhomb of rhombs) {
    const shape = rhomb.shape.map((point) => new THREE.Vector2(point.x * tf.x, point.y * tf.y));

    if (shape.some((point) => {
      const distanceSqr = point.lengthSq();
      return distanceSqr > sqrMaxDistance || distanceSqr < sqrMinDistance;
    })) {
      continue;
    }

    const w1 = shape[2].distanceTo(shape[0]);
    const w2 = shape[3].distanceTo(shape[1]);
    const shapeRatio = Math.min(w1, w2) / Math.max(w1, w2);
    let intersectRatio = rhomb.line1 / settings.dimensions;
    intersectRatio += rhomb.line2 / settings.dimensions;
    intersectRatio *= 0.5;

    let indexRatio = 1 - Math.abs(rhomb.parallel1 / settings.divisions / 2);
    indexRatio *= 1 - Math.abs(rhomb.parallel2 / settings.divisions / 2);

    let gradientPos = 1;

    if (settings.colorRatio >= 0) {
      gradientPos *= 1 - (shapeRatio * settings.colorRatio);
    } else {
      gradientPos *= 1 - ((1 - shapeRatio) * Math.abs(settings.colorRatio));
    }

    if (settings.colorIntersect >= 0) {
      gradientPos *= 1 - (intersectRatio * settings.colorIntersect);
    } else {
      gradientPos *= 1 - ((1 - intersectRatio) * Math.abs(settings.colorIntersect));
    }

    if (settings.colorIndex >= 0) {
      gradientPos *= 1 - (indexRatio * settings.colorIndex);
    } else {
      gradientPos *= 1 - ((1 - indexRatio) * Math.abs(settings.colorIndex));
    }

    polygons.push(shape);
    faceValues.push(Number.isNaN(gradientPos) ? 0 : gradientPos);
  }

  const normalizedPolygons = normalizePolygons(polygons);
  return buildPolygonMesh(normalizedPolygons, settings.sharedVertices, faceValues);
};

const buildPatternFromFormat = (format: string): RepeatedTilePattern => {
  const lines = format.split(/\r?\n/);
  const headerParts = lines[0].trim().split(/\s+/);
  const initialSides = Number.parseInt(headerParts[0], 10);
  const angle = headerParts.length > 1 ? Number.parseFloat(headerParts[1]) : 0;
  const tile = TileMesh2D.polygon(initialSides).rotate(angle);
  const xOffsetIndices = lines[1].trim().split(/\s+/).map((part) => Number.parseInt(part, 10));
  const yOffsetIndices = lines[2].trim().split(/\s+/).map((part) => Number.parseInt(part, 10));

  for (const line of lines.slice(3)) {
    const trimmed = line.trim();
    if (!trimmed) {
      continue;
    }

    const parts = trimmed.split(/\s+/);
    const parentFaceIndex = Number.parseInt(parts[0], 10);
    const parentEdgeIndex = parts.length > 1 ? Number.parseInt(parts[1], 10) : 0;
    const sides = parts.length > 2 ? Number.parseInt(parts[2], 10) : 4;

    if (tile.extendFace(parentFaceIndex, parentEdgeIndex, sides) < 0) {
      throw new Error(`Failed to extend face ${parentFaceIndex} edge ${parentEdgeIndex} for format line "${trimmed}"`);
    }
  }

  return createPattern(
    tile,
    xOffsetIndices[0],
    xOffsetIndices[1],
    yOffsetIndices[0],
    yOffsetIndices[1],
  );
};

const createRepeatedTiling = (
  name: string,
  config: string,
  description: string,
  pattern: RepeatedTilePattern,
): TilingDefinition => ({
  name,
  config,
  description,
  generate: (rows, cols) => buildRepeatedTile(pattern, rows, cols),
});

const createDerivedTiling = (
  name: string,
  config: string,
  description: string,
  baseKey: string,
  operators: OperatorSpec[],
): TilingDefinition => ({
  name,
  config,
  description,
  generate: (rows, cols) => {
    const base = UNIFORM_TILINGS[baseKey].generate(rows, cols);
    const derived = operators.reduce(
      (mesh, operator) => applyOperator(mesh, operator),
      { vertices: base.vertices, faces: base.faces },
    );

    return {
      vertices: derived.vertices,
      faces: derived.faces,
      indices: triangulateFaces(derived.faces),
    };
  },
});

const buildDissectedRhombitrihexagonalPattern = (): RepeatedTilePattern => {
  let tile = TileMesh2D.polygon(6).kis();
  tile.extendFace(4, 1, 4);
  tile.extendFace(5, 1, 4);
  tile.extendFace(0, 1, 4);
  tile.extendFace(6, 0, 3);
  tile.extendFace(7, 0, 3);
  return createPattern(tile, 10, 1, 7, 1);
};

const buildDissectedTruncatedTrihexagonalPattern = (): RepeatedTilePattern => {
  const tile = TileMesh2D.polygon(12).rotate(15);
  tile.extendFace(0, 0, 3);
  tile.extendFace(0, 1, 4);
  tile.extendFace(0, 2, 3);
  tile.extendFace(0, 3, 4);
  tile.extendFace(0, 4, 3);
  tile.extendFace(0, 5, 4);
  tile.extendFace(0, 6, 3);
  tile.extendFace(0, 8, 3);
  tile.extendFace(0, 10, 3);
  tile.extendFace(2, 0, 3);
  tile.extendFace(2, 2, 3);
  tile.extendFace(4, 0, 3);
  tile.extendFace(4, 2, 3);
  tile.extendFace(6, 0, 3);
  tile.extendFace(6, 2, 3);
  return createPattern(tile, 20, 10, 17, 8);
};

const buildDemiregularSquarePattern = (): RepeatedTilePattern => {
  const tile = TileMesh2D.polygon(12).rotate(15);
  tile.extendFace(0, 1, 3);
  tile.extendFace(0, 0, 3);
  tile.extendFace(1, 2, 4);
  tile.extendFace(3, 0, 3);
  tile.extendFace(3, 3, 3);
  return createPattern(tile, 5, 10, 2, 7);
};

const buildDissectedRhombiHexagonalPattern = (): RepeatedTilePattern => {
  const tile = TileMesh2D.polygon(6);
  tile.extendFace(0, 5, 3);
  tile.extendFace(0, 0, 3);
  return createPattern(tile, 2, 5, 1, 3);
};

const buildTrihexSquarePattern = (): RepeatedTilePattern => {
  const tile = TileMesh2D.polygon(6);
  tile.extendFace(0, 5, 3);
  tile.extendFace(0, 0, 3);
  tile.extendFace(0, 1, 4);
  tile.extendFace(2, 0, 4);
  return createPattern(tile, 2, 5, 9, 3);
};

const buildDurer1Pattern = (): RepeatedTilePattern => {
  const tile = TileMesh2D.polygon(5).rotate(54);
  tile.extendFace(0, 5, 5);
  tile.addKite(0, 3, 1, 1);
  return createPattern(tile, 0, 8, 1, 6);
};

const buildDurer2Pattern = (): RepeatedTilePattern => {
  const tile = TileMesh2D.polygon(5).rotate(54);
  tile.extendFace(0, 5, 5);
  tile.addKite(0, 3, 1, 1);
  tile.addRhombus(0, 2, 72);
  return createPattern(tile, 0, 8, 2, 6);
};

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
  },

  'tetrakis-square': createDerivedTiling(
    'Tetrakis Square',
    '3.3.4',
    'Catalan tiling dual to the truncated square tiling.',
    '4.4.4.4',
    [createOperatorSpec('kis')],
  ),

  'cairo-pentagonal': createDerivedTiling(
    'Cairo Pentagonal',
    '5.5.5.5',
    'Catalan tiling dual to the snub square tiling.',
    '3.3.4.3.4',
    [createOperatorSpec('dual')],
  ),

  'rhombille': createDerivedTiling(
    'Rhombille',
    '4.4.4',
    'Catalan tiling dual to the trihexagonal tiling.',
    '3.6.3.6',
    [createOperatorSpec('dual')],
  ),

  'triakis-triangular': createDerivedTiling(
    'Triakis Triangular',
    '3.3.3.3.3.3',
    'Catalan tiling dual to the truncated hexagonal tiling.',
    '3.12.12',
    [createOperatorSpec('dual')],
  ),

  'deltoidal-trihexagonal': createDerivedTiling(
    'Deltoidal Trihexagonal',
    '4.4.4.4.4.4',
    'Catalan tiling dual to the rhombitrihexagonal tiling.',
    '3.4.6.4',
    [createOperatorSpec('dual')],
  ),

  'kisrhombille': createDerivedTiling(
    'Kisrhombille',
    '3.3.3.3.3.3',
    'Catalan tiling dual to the truncated trihexagonal tiling.',
    '4.6.12',
    [createOperatorSpec('dual')],
  ),

  'floret-pentagonal': createDerivedTiling(
    'Floret Pentagonal',
    '5.5.5.5',
    'Catalan tiling dual to the snub hexagonal tiling.',
    '3.3.3.3.6',
    [createOperatorSpec('dual')],
  ),

  'prismatic-pentagonal': createDerivedTiling(
    'Prismatic Pentagonal',
    '5.5.5.5',
    'Catalan tiling dual to the elongated triangular tiling.',
    '3.3.3.4.4',
    [createOperatorSpec('dual')],
  ),

  'durer-1': createRepeatedTiling(
    'Durer I',
    '5.5.6',
    'Durer periodic tiling built from pentagons and kites.',
    buildDurer1Pattern(),
  ),

  'durer-2': createRepeatedTiling(
    'Durer II',
    '5.5.6.4',
    'Durer periodic tiling built from pentagons, kites, and rhombi.',
    buildDurer2Pattern(),
  ),

  'dissected-rhombitrihexagonal': createRepeatedTiling(
    'Dissected Rhombitrihexagonal',
    '3.3.3.3.3.3;3.3.4.3.4',
    '2-uniform tiling with vertex types 3.3.3.3.3.3 and 3.3.4.3.4.',
    buildDissectedRhombitrihexagonalPattern(),
  ),

  'dissected-truncated-hexagonal-1': createRepeatedTiling(
    'Dissected Truncated Hexagonal I',
    '3.4.6.4;3.3.4.3.4',
    '2-uniform tiling with vertex types 3.4.6.4 and 3.3.4.3.4.',
    buildPatternFromFormat(`6 0 0
12 17
15 10
0 0 4 1
0 1 4 1
0 2 4 1
0 3 4 1
0 4 4 1
0 5 4 1

1 0 3 5
2 0 3 4
3 0 3 5
4 0 3 4
5 0 3 5
6 0 3 4

5 3 3 0
6 3 3 0`),
  ),

  'dissected-truncated-hexagonal-2': createRepeatedTiling(
    'Dissected Truncated Hexagonal II',
    '3.4.6.4;3.3.3.4.4',
    '2-uniform tiling with vertex types 3.4.6.4 and 3.3.3.4.4.',
    buildPatternFromFormat(`6 0 1
10 17
9 14

0 0 4 4
0 1 4 5
0 2 4 4
0 3 4 5
0 4 4 4
0 5 4 5

1 0 3 1
2 0 3 1
3 0 3 1
4 0 3 1
5 0 3 1
6 0 3 1

12 2 3 0
11 2 3 0`),
  ),

  'hexagonal-truncated-triangular': createRepeatedTiling(
    'Hexagonal Truncated Triangular',
    '3.4.6.4;3.4.4.6',
    '2-uniform tiling with vertex types 3.4.6.4 and 3.4.4.6.',
    buildPatternFromFormat(`6 30 1
11 32
27 14
0 0 4 0
0 1 4 0
0 2 4 0
0 3 4 0
0 4 4 0
0 5 4 0
1 0 3 1
2 0 3 1
3 0 3 1
4 0 3 1
5 0 3 1
6 0 3 1
1 3 4 4
2 3 4 4
12 2 6 0
6 3 4 1
7 2 6 0`),
  ),

  'demiregular-hexagonal': createRepeatedTiling(
    'Demiregular Hexagonal',
    '4.6.12;3.4.6.4',
    '2-uniform tiling with vertex types 4.6.12 and 3.4.6.4.',
    buildPatternFromFormat(`12 15 1
4 26
3 21
0 8 4 0
0 10 4 0
0 0 4 0
0 2 4 0
0 9 6 4
0 11 6 4
0 1 6 4
1 3 3 1
2 3 3 5
6 3 4 0
5 3 4 0`),
  ),

  'dissected-truncated-trihexagonal': createRepeatedTiling(
    'Dissected Truncated Trihexagonal',
    '3.3.3.3.3.3;3.3.4.12',
    '2-uniform tiling with vertex types 3.3.3.3.3.3 and 3.3.4.12.',
    buildDissectedTruncatedTrihexagonalPattern(),
  ),

  'demiregular-square': createRepeatedTiling(
    'Demiregular Square',
    '3.12.12;3.4.3.12',
    '2-uniform tiling with vertex types 3.12.12 and 3.4.3.12.',
    buildDemiregularSquarePattern(),
  ),

  'dissected-hexagonal-1': createRepeatedTiling(
    'Dissected Hexagonal I',
    '3.3.3.3.3.3;3.3.6.6',
    '2-uniform tiling with vertex types 3.3.3.3.3.3 and 3.3.6.6.',
    buildPatternFromFormat(`6 0 1
11 10
11 12
0 5 6 0
0 0 3 0
0 2 3 0
0 4 3 0
1 0 3 1
1 2 3 1
1 4 3 1`),
  ),

  'dissected-hexagonal-2': createRepeatedTiling(
    'Dissected Hexagonal II',
    '3.3.3.3.3.3;3.3.3.3.6 A',
    '2-uniform tiling with vertex types 3.3.3.3.3.3 and 3.3.3.3.6, variant A.',
    buildPatternFromFormat(`6 0 1
1 12
1 10
0 3 3 0
0 4 3 0
0 5 3 0
0 0 3 0
1 0 3 4
2 0 3 4
3 0 3 4
5 0 3 5
6 0 3 5
7 0 3 5`),
  ),

  'dissected-hexagonal-3': createRepeatedTiling(
    'Dissected Hexagonal III',
    '3.3.3.3.3.3;3.3.3.3.6 B',
    '2-uniform tiling with vertex types 3.3.3.3.3.3 and 3.3.3.3.6, variant B.',
    buildPatternFromFormat(`6 0 1
13 11
7 15
0 0 3 0
0 1 3 0
0 2 3 0
0 3 3 0
0 4 3 0
0 5 3 0
1 0 3 1
2 0 3 1
3 0 3 1
4 0 3 1
5 0 3 1
6 0 3 1
7 0 3 4
8 0 3 4
9 0 3 4
10 0 3 4
11 0 3 4
12 0 3 4
7 2 3 0
12 2 3 0`),
  ),

  'alternating-trihexagonal': createRepeatedTiling(
    'Alternating Trihexagonal',
    '3.3.6.6;3.3.3.3.6',
    '2-uniform tiling with vertex types 3.3.6.6 and 3.3.3.3.6.',
    buildPatternFromFormat(`6 30 1
2 4
3 6
0 1 3 0
0 0 3 4
1 2 3 4
2 0 3 0`),
  ),

  'dissected-rhombihexagonal': createRepeatedTiling(
    'Dissected Rhombihexagonal',
    '3.6.3.6;3.3.6.6',
    '2-uniform tiling with vertex types 3.6.3.6 and 3.3.6.6.',
    buildDissectedRhombiHexagonalPattern(),
  ),

  'alternating-trihex-square': createRepeatedTiling(
    'Alternating Trihex Square',
    '3.4.4.6;3.6.3.6 A',
    '2-uniform tiling with vertex types 3.4.4.6 and 3.6.3.6, variant A.',
    buildPatternFromFormat(`6 0 0
2 5
3 8
0 0 3 1
0 5 3 1
0 1 4 4
1 0 4 5`),
  ),

  'trihex-square': createRepeatedTiling(
    'Trihex Square',
    '3.4.4.6;3.6.3.6 B',
    '2-uniform tiling with vertex types 3.4.4.6 and 3.6.3.6, variant B.',
    buildTrihexSquarePattern(),
  ),

  'alternating-tri-square': createRepeatedTiling(
    'Alternating Tri-Square',
    '3.3.3.4.4;3.3.4.3.4 A',
    '2-uniform tiling with vertex types 3.3.3.4.4 and 3.3.4.3.4, variant A.',
    buildPatternFromFormat(`4 45 1
15 14
19 8
0 1 4 0
0 2 3 0
0 0 3 0
1 0 3 1
1 2 3 1
1 3 3 1
0 3 3 0
4 0 3 5
2 0 4 5
3 0 3 4
10 2 4 5
11 3 4 4
11 0 3 1
12 0 3 1
11 2 3 0
12 2 3 0
4 2 4 5`),
  ),

  'semi-snub-tri-square': createRepeatedTiling(
    'Semi-Snub Tri-Square',
    '3.3.3.4.4;3.3.4.3.4 B',
    '2-uniform tiling with vertex types 3.3.3.4.4 and 3.3.4.3.4, variant B.',
    buildPatternFromFormat(`4 45 1
3 6
2 15
0 1 4 0
0 2 3 0
0 0 3 0
1 0 3 1
1 2 3 1
1 3 3 1
6 0 4 4
7 2 4 5
8 0 3 4
6 2 3 5
10 0 3 0`),
  ),

  'tri-square-square-1': createRepeatedTiling(
    'Tri-Square-Square I',
    '4.4.4.4;3.3.3.4.4 A',
    '2-uniform tiling with vertex types 4.4.4.4 and 3.3.3.4.4, variant A.',
    buildPatternFromFormat(`4 45 1
4 5
6 5
0 2 4 0
0 0 3 5
1 3 3 4`),
  ),

  'tri-square-square-2': createRepeatedTiling(
    'Tri-Square-Square II',
    '4.4.4.4;3.3.3.4.4 B',
    '2-uniform tiling with vertex types 4.4.4.4 and 3.3.3.4.4, variant B.',
    buildPatternFromFormat(`4 45 1
4 5
7 8
0 2 4 0
0 0 4 0
1 3 3 5
2 3 3 4`),
  ),

  'tri-tri-square-1': createRepeatedTiling(
    'Tri-Tri-Square I',
    '3.3.3.3.3.3;3.3.3.4.4 A',
    '2-uniform tiling with vertex types 3.3.3.3.3.3 and 3.3.3.4.4, variant A.',
    buildPatternFromFormat(`4 45 0
4 6
4 5
0 2 3 1
0 0 3 2
1 2 3 2
2 0 3 1`),
  ),

  'tri-tri-square-2': createRepeatedTiling(
    'Tri-Tri-Square II',
    '3.3.3.3.3.3;3.3.3.4.4 B',
    '2-uniform tiling with vertex types 3.3.3.3.3.3 and 3.3.3.4.4, variant B.',
    buildPatternFromFormat(`4 45 0
5 7
5 9
0 2 3 1
0 0 3 2
1 2 3 2
2 0 3 1
3 0 3 1
5 0 3 2`),
  ),

  'multigrid': {
    name: 'Multigrid',
    config: 'N-grid',
    description: 'De Bruijn multigrid construction from intersecting line families.',
    generate: (_rows, _cols, options) => generateMultigridMesh({
      ...MULTIGRID_DEFAULTS,
      ...options?.multigrid,
    }),
  },
};
