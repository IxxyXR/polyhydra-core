import { Mesh } from './conway-operators';
import { PALETTES, PaletteKey } from './palettes';

export type ColorMode = 'role' | 'sides' | 'value';

export function computeFaceColors(mesh: Mesh, palette: PaletteKey, colorMode: ColorMode = 'role'): string[] {
  const paletteColors = PALETTES[palette].colors;

  if (colorMode === 'sides') {
    return mesh.faces.map((face) => {
      const colorIndex = Math.max(0, face.length - 3);
      return paletteColors[colorIndex % paletteColors.length];
    });
  }

  if (colorMode === 'value' && mesh.faceValues && mesh.faceValues.length === mesh.faces.length) {
    const minValue = Math.min(...mesh.faceValues);
    const maxValue = Math.max(...mesh.faceValues);
    const range = maxValue - minValue;

    return mesh.faceValues.map((value) => {
      const normalized = range < 1e-9 ? 0 : (value - minValue) / range;
      const colorIndex = Math.max(
        0,
        Math.min(
          paletteColors.length - 1,
          Math.floor(normalized * paletteColors.length),
        ),
      );
      return paletteColors[colorIndex];
    });
  }

  const { faces } = mesh;
  const edgeMap = new Map<string, number[]>();

  faces.forEach((face, fIdx) => {
    for (let i = 0; i < face.length; i++) {
      const v1 = face[i];
      const v2 = face[(i + 1) % face.length];
      const key = v1 < v2 ? `${v1},${v2}` : `${v2},${v1}`;
      if (!edgeMap.has(key)) {
        edgeMap.set(key, []);
      }
      edgeMap.get(key)!.push(fIdx);
    }
  });

  const faceAdjacency: number[][] = Array(faces.length).fill(0).map(() => []);

  for (const neighbors of edgeMap.values()) {
    if (neighbors.length === 2) {
      const [f1, f2] = neighbors;
      faceAdjacency[f1].push(f2);
      faceAdjacency[f2].push(f1);
    }
  }

  // Welsh-Powell: Sort by degree descending
  const faceIndices = Array.from({ length: faces.length }, (_, i) => i);
  faceIndices.sort((a, b) => faceAdjacency[b].length - faceAdjacency[a].length);

  const colorIndices = new Int32Array(faces.length).fill(-1);

  for (const i of faceIndices) {
    const neighbors = faceAdjacency[i];
    const usedColors = new Set<number>();
    for (const n of neighbors) {
      if (colorIndices[n] !== -1) {
        usedColors.add(colorIndices[n]);
      }
    }

    let color = 0;
    while (usedColors.has(color)) color++;
    colorIndices[i] = color;
  }

  return Array.from(colorIndices).map(idx => paletteColors[idx % paletteColors.length]);
}
