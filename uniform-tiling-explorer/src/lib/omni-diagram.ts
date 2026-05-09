type Point = [number, number];

interface Segment {
  a: Point;
  b: Point;
}

const EPSILON = 1e-9;
const LINE_WIDTH = 0.04;
const DOT_RADIUS = 0.075;
const DOT_OUTLINE_WIDTH = 0.015;
const PADDING = 0.25;
const T_VALUES = [0.5, 1 / 3, 2 / 3];
const OUTSIDE_THRESHOLD = 0.05;

const DOT_POSITIONS: Record<string, Point[]> = {
  V: [[0, 0], [1, 0], [1, 1], [0, 1]],
  F: [[0.5, 0.5]],
  'F!': [[0.5, -0.2], [1.2, 0.5], [0.5, 1.2], [-0.2, 0.5]],
  E: [[0.5, 0], [1, 0.5], [0.5, 1], [0, 0.5]],
  vf: [[0.25, 0.25], [0.75, 0.25], [0.75, 0.75], [0.25, 0.75]],
  'vf!': [[0.25, -0.2], [0.75, -0.2], [1.2, 0.25], [1.2, 0.75], [0.75, 1.2], [0.25, 1.2], [-0.2, 0.75], [-0.2, 0.25]],
  ve: [[0.25, 0], [0.75, 0], [1, 0.25], [1, 0.75], [0.75, 1], [0.25, 1], [0, 0.75], [0, 0.25]],
  ve0: [[0.25, 0], [0.75, 0], [1, 0.25], [1, 0.75], [0.75, 1], [0.25, 1], [0, 0.75], [0, 0.25]],
  ve1: [[0.25, 0], [0.75, 0], [1, 0.25], [1, 0.75], [0.75, 1], [0.25, 1], [0, 0.75], [0, 0.25]],
  fe: [[0.25, 0.5], [0.75, 0.5], [0.5, 0.75], [0.5, 0.25]],
  'fe!': [[-0.2, 0.5], [1.2, 0.5], [0.5, 1.22], [0.5, -0.2]],
};

const ATOMS: Record<string, Array<[number, number[]]>> = {
  'E-E': [[0, [1]], [1, [2]], [2, [3]], [3, [0]]],
  'E-F': [[0, [0]], [1, [0]], [2, [0]], [3, [0]]],
  'E-V': [[0, [0, 1]], [1, [1, 2]], [2, [2, 3]], [3, [3, 0]]],
  'E-ve': [[0, [0, 1]], [1, [2, 3]], [2, [4, 5]], [3, [6, 7]]],
  'E-vf': [[0, [0, 1]], [1, [1, 2]], [2, [2, 3]], [3, [3, 0]]],
  'E-fe': [[0, [3]], [1, [1]], [2, [2]], [3, [0]]],
  'F-F!': [[0, [0, 1, 2, 3]]],
  'F-V': [[0, [0, 1, 2, 3]]],
  'F-ve': [[0, [0, 1, 2, 3, 4, 5, 6, 7]]],
  'F-vf': [[0, [0, 1, 2, 3]]],
  'F-fe': [[0, [0, 1, 2, 3]]],
  'V-V': [[0, [1, 3]], [1, [0, 2]], [2, [1, 3]], [3, [0, 2]]],
  'V-ve': [[0, [0, 7]], [1, [1, 2]], [2, [3, 4]], [3, [5, 6]]],
  'V-vf': [[0, [0]], [1, [1]], [2, [2]], [3, [3]]],
  'fe-V': [[0, [3]], [0, [0]], [1, [2]], [1, [1]], [2, [2]], [2, [3]], [3, [0]], [3, [1]]],
  've0-ve0': [[0, [1]], [1, [0]], [2, [3]], [3, [2]], [4, [5]], [5, [4]], [6, [7]], [7, [6]]],
  've1-ve1': [[0, [7]], [1, [2]], [2, [1]], [3, [4]], [4, [3]], [5, [6]], [6, [5]], [7, [0]]],
  've-vf': [[0, [0]], [1, [1]], [2, [1]], [3, [2]], [4, [2]], [5, [3]], [6, [3]], [7, [0]]],
  'fe-ve': [[0, [6, 7]], [1, [2, 3]], [2, [4, 5]], [3, [0, 1]]],
  'vf-vf': [[0, [1, 3]], [1, [0, 2]], [2, [1, 3]], [3, [0, 2]]],
  'vf-vf!': [[0, [7, 0]], [1, [1, 2]], [2, [3, 4]], [3, [5, 6]]],
  'fe-vf': [[0, [3, 0]], [1, [1, 2]], [2, [2, 3]], [3, [0, 1]]],
  'fe-fe': [[0, [3]], [1, [2]], [2, [0]], [3, [1]]],
  'fe-fe!': [[0, [0]], [1, [1]], [2, [2]], [3, [3]]],
};

const pointsEqual = (a: Point, b: Point) => (
  Math.abs(a[0] - b[0]) < EPSILON && Math.abs(a[1] - b[1]) < EPSILON
);

const pointKey = ([x, y]: Point) => `${x.toFixed(6)},${y.toFixed(6)}`;

const canonicalSegment = (a: Point, b: Point): Segment => {
  const aKey = pointKey(a);
  const bKey = pointKey(b);
  return aKey <= bKey ? { a, b } : { a: b, b: a };
};

const cross2d = (ox: number, oy: number, ax: number, ay: number, bx: number, by: number) => (
  (ax - ox) * (by - oy) - (ay - oy) * (bx - ox)
);

const collinearOverlap = (s1: Segment, s2: Segment) => {
  const dx = s1.b[0] - s1.a[0];
  const dy = s1.b[1] - s1.a[1];
  const [a1, b1, a2, b2] = Math.abs(dx) > Math.abs(dy)
    ? [s1.a[0], s1.b[0], s2.a[0], s2.b[0]]
    : [s1.a[1], s1.b[1], s2.a[1], s2.b[1]];
  const overlap = Math.min(Math.max(a1, b1), Math.max(a2, b2)) - Math.max(Math.min(a1, b1), Math.min(a2, b2));
  return overlap > EPSILON;
};

const segmentsIntersect = (s1: Segment, s2: Segment) => {
  const shared = [s1.a, s1.b].filter((point) => pointsEqual(point, s2.a) || pointsEqual(point, s2.b));
  if (shared.length === 2) {
    return true;
  }

  const [p1, p2, p3, p4] = [s1.a, s1.b, s2.a, s2.b];
  const d1x = p2[0] - p1[0];
  const d1y = p2[1] - p1[1];
  const d2x = p4[0] - p3[0];
  const d2y = p4[1] - p3[1];
  const denom = d1x * d2y - d1y * d2x;

  if (Math.abs(denom) < EPSILON) {
    const cross = (p3[0] - p1[0]) * d1y - (p3[1] - p1[1]) * d1x;
    return Math.abs(cross) <= EPSILON && collinearOverlap(s1, s2);
  }

  const t = ((p3[0] - p1[0]) * d2y - (p3[1] - p1[1]) * d2x) / denom;
  const u = ((p3[0] - p1[0]) * d1y - (p3[1] - p1[1]) * d1x) / denom;
  const tIn = EPSILON < t && t < 1 - EPSILON;
  const uIn = EPSILON < u && u < 1 - EPSILON;
  const uEnd = Math.abs(u) < EPSILON || Math.abs(u - 1) < EPSILON;
  const tEnd = Math.abs(t) < EPSILON || Math.abs(t - 1) < EPSILON;
  return (tIn && (uIn || uEnd)) || (uIn && tEnd);
};

const hasCrossingOrDuplicate = (segments: Segment[]) => {
  for (let i = 0; i < segments.length; i++) {
    for (let j = i + 1; j < segments.length; j++) {
      if (segmentsIntersect(segments[i], segments[j])) {
        return true;
      }
    }
  }
  return false;
};

const vfPositions = (t: number): Point[] => {
  const [fx, fy] = DOT_POSITIONS.F[0];
  return DOT_POSITIONS.V.map(([vx, vy]) => [vx + t * (fx - vx), vy + t * (fy - vy)]);
};

const vfOutsidePositions = (vfPts: Point[]): Point[] => {
  const NEAR = -0.2;
  const FAR = 1.2;
  return [
    [vfPts[0][0], NEAR],
    [vfPts[1][0], NEAR],
    [FAR, vfPts[1][1]],
    [FAR, vfPts[2][1]],
    [vfPts[2][0], FAR],
    [vfPts[3][0], FAR],
    [NEAR, vfPts[3][1]],
    [NEAR, vfPts[0][1]],
  ];
};

const fePositions = (t: number): Point[] => {
  const [fx, fy] = DOT_POSITIONS.F[0];
  return DOT_POSITIONS.fe.map(([px, py]) => [fx + 2 * t * (px - fx), fy + 2 * t * (py - fy)]);
};

const createData = (notation: string, vfPts?: Point[], fePts?: Point[]) => {
  const positions: Record<string, Point[]> = {
    ...DOT_POSITIONS,
    ...(vfPts ? { vf: vfPts, 'vf!': vfOutsidePositions(vfPts) } : {}),
    ...(fePts ? { fe: fePts } : {}),
  };
  const segments: Segment[] = [];
  const points: Point[] = [];
  const seenSegments = new Set<string>();

  for (const atom of notation.split(',').map((part) => part.trim()).filter(Boolean)) {
    const [rawA, rawB] = atom.split('-');
    if (!rawA || !rawB) {
      continue;
    }

    const [startGroup, endGroup] = rawA.toUpperCase() > rawB.toUpperCase()
      ? [rawB, rawA]
      : [rawA, rawB];
    const atomKey = `${startGroup}-${endGroup}`;
    const atomConnections = ATOMS[atomKey];
    const startPoints = positions[startGroup];
    const endPoints = positions[endGroup];
    if (!atomConnections || !startPoints || !endPoints) {
      continue;
    }

    points.push(...startPoints);
    if (!endGroup.endsWith('!')) {
      points.push(...endPoints);
    }

    for (const [fromIndex, toIndices] of atomConnections) {
      const start = startPoints[fromIndex];
      for (const toIndex of toIndices) {
        const segment = canonicalSegment(start, endPoints[toIndex]);
        const key = `${pointKey(segment.a)}|${pointKey(segment.b)}`;
        if (!seenSegments.has(key)) {
          seenSegments.add(key);
          segments.push(segment);
        }
      }
    }
  }

  return { segments, points };
};

const findValidPlacement = (notation: string) => {
  for (const vfT of T_VALUES) {
    for (const feT of T_VALUES) {
      const vfPts = vfPositions(vfT);
      const fePts = fePositions(feT);
      const result = createData(notation, vfPts, fePts);
      if (!hasCrossingOrDuplicate(result.segments)) {
        return result;
      }
    }
  }

  return createData(notation);
};

const isOutsidePoint = ([x, y]: Point) => (
  x < -OUTSIDE_THRESHOLD || x > 1 + OUTSIDE_THRESHOLD || y < -OUTSIDE_THRESHOLD || y > 1 + OUTSIDE_THRESHOLD
);

const taperingLinePath = (interior: Point, outside: Point, strokeWidth: number, taper = 0.25) => {
  const dx = outside[0] - interior[0];
  const dy = outside[1] - interior[1];
  const length = Math.hypot(dx, dy);
  if (length < EPSILON) {
    return '';
  }

  const px = -dy / length;
  const py = dx / length;
  const halfWidth = strokeWidth / 2;
  const tx = interior[0] + (1 - taper) * dx;
  const ty = interior[1] + (1 - taper) * dy;
  const points: Point[] = [
    [interior[0] + px * halfWidth, interior[1] + py * halfWidth],
    [tx + px * halfWidth, ty + py * halfWidth],
    outside,
    [tx - px * halfWidth, ty - py * halfWidth],
    [interior[0] - px * halfWidth, interior[1] - py * halfWidth],
  ];
  return points.map(([x, y]) => `${x.toFixed(4)},${y.toFixed(4)}`).join(' ');
};

export function createOmniOperatorDiagramSvg(notation: string): string | null {
  const cleaned = notation.split(',').map((part) => part.trim()).filter(Boolean).join(',');
  if (!cleaned) {
    return null;
  }

  const { segments, points } = findValidPlacement(cleaned);
  if (segments.length === 0 && points.length === 0) {
    return null;
  }

  const uniquePoints = Array.from(new Map(points.map((point) => [pointKey(point), point])).values());
  const viewBoxSize = 1 + 2 * PADDING;
  let svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="-${PADDING} -${PADDING} ${viewBoxSize} ${viewBoxSize}" fill="none">`;
  svg += `<rect x="0" y="0" width="1" height="1" fill="none" stroke="#374151" stroke-width="0.024" stroke-dasharray="0.03 0.025"/>`;

  for (const segment of segments) {
    const aOutside = isOutsidePoint(segment.a);
    const bOutside = isOutsidePoint(segment.b);
    if (aOutside || bOutside) {
      const interior = aOutside ? segment.b : segment.a;
      const outside = aOutside ? segment.a : segment.b;
      const polygon = taperingLinePath(interior, outside, LINE_WIDTH);
      if (polygon) {
        svg += `<polygon points="${polygon}" fill="#f5f5f5" stroke="none"/>`;
      }
    } else {
      svg += `<line x1="${segment.a[0]}" y1="${segment.a[1]}" x2="${segment.b[0]}" y2="${segment.b[1]}" stroke="#f5f5f5" stroke-width="${LINE_WIDTH}"/>`;
    }
  }

  for (const [x, y] of uniquePoints) {
    svg += `<circle cx="${x}" cy="${y}" r="${DOT_RADIUS}" fill="#ef4444" stroke="#f5f5f5" stroke-width="${DOT_OUTLINE_WIDTH}"/>`;
  }

  svg += '</svg>';
  return svg;
}
