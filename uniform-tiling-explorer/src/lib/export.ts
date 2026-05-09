import { TilingGenerationOptions, UNIFORM_TILINGS } from './tiling-geometries';
import { applyOperator, Mesh, OperatorSpec } from './conway-operators';
import { PaletteKey } from './palettes';
import { ColorMode, computeFaceColors } from './coloring';

function generateFinalMesh(
  tilingType: string,
  rows: number,
  cols: number,
  operators: OperatorSpec[],
  generationOptions?: TilingGenerationOptions,
): Mesh | null {
  const tiling = UNIFORM_TILINGS[tilingType];
  if (!tiling) return null;

  let { vertices, faces } = tiling.generate(rows, cols, generationOptions);

  let mesh: Mesh = { vertices, faces };
  
  if (operators.length > 0) {
    for (const op of operators) {
      mesh = applyOperator(mesh, op);
    }
  }

  return mesh;
}

export function exportObj(
  tilingType: string,
  rows: number,
  cols: number,
  operators: OperatorSpec[],
  generationOptions?: TilingGenerationOptions,
) {
  const mesh = generateFinalMesh(tilingType, rows, cols, operators, generationOptions);
  if (!mesh) return;

  let obj = "# Generated Tiling\n";
  for (let i = 0; i < mesh.vertices.length; i += 3) {
    obj += `v ${mesh.vertices[i]} ${mesh.vertices[i + 1]} ${mesh.vertices[i + 2]}\n`;
  }
  
  for (const face of mesh.faces) {
    // OBJ uses 1-based indices
    obj += `f ${face.map(idx => idx + 1).join(' ')}\n`;
  }

  downloadString(obj, 'tiling.obj', 'text/plain');
}

export function exportOff(
  tilingType: string,
  rows: number,
  cols: number,
  operators: OperatorSpec[],
  paletteKey: PaletteKey,
  colorMode: ColorMode,
  generationOptions?: TilingGenerationOptions,
) {
  const mesh = generateFinalMesh(tilingType, rows, cols, operators, generationOptions);
  if (!mesh) return;

  const faceColors = computeFaceColors(mesh, paletteKey, colorMode);

  const numVertices = mesh.vertices.length / 3;
  const numFaces = mesh.faces.length;
  
  let off = `COFF\n${numVertices} ${numFaces} 0\n`;
  
  for (let i = 0; i < mesh.vertices.length; i += 3) {
    off += `${mesh.vertices[i]} ${mesh.vertices[i + 1]} ${mesh.vertices[i + 2]}\n`;
  }
  
  for (let fIdx = 0; fIdx < mesh.faces.length; fIdx++) {
    const face = mesh.faces[fIdx];
    const colorHex = faceColors[fIdx] || "#ffffff";
    const { r, g, b } = hexToRgb(colorHex);
    off += `${face.length} ${face.join(' ')} ${r} ${g} ${b} 255\n`;
  }

  downloadString(off, 'tiling.off', 'text/plain');
}

function hexToRgb(hex: string) {
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  return result ? {
    r: parseInt(result[1], 16),
    g: parseInt(result[2], 16),
    b: parseInt(result[3], 16)
  } : { r: 255, g: 255, b: 255 };
}

export function exportSvg(
  tilingType: string,
  rows: number,
  cols: number,
  operators: OperatorSpec[],
  paletteKey: PaletteKey,
  colorMode: ColorMode,
  edgeColor: string,
  generationOptions?: TilingGenerationOptions,
) {
  const mesh = generateFinalMesh(tilingType, rows, cols, operators, generationOptions);
  if (!mesh) return;

  const faceColors = computeFaceColors(mesh, paletteKey, colorMode);

  // Find bounds
  let minX = Infinity, maxX = -Infinity;
  let minY = Infinity, maxY = -Infinity;
  
  for (let i = 0; i < mesh.vertices.length; i += 3) {
    const x = mesh.vertices[i];
    const y = mesh.vertices[i + 1];
    if (x < minX) minX = x;
    if (x > maxX) maxX = x;
    if (y < minY) minY = y;
    if (y > maxY) maxY = y;
  }
  
  const padding = 1;
  const width = Math.max(0.1, maxX - minX) + padding * 2;
  const height = Math.max(0.1, maxY - minY) + padding * 2;
  const viewBox = `${minX - padding} ${-(maxY + padding)} ${width} ${height}`;

  // Y-axis in SVG goes down, but math goes up, so we negate Y.
  let svg = `<?xml version="1.0" encoding="UTF-8" standalone="no"?>\n`;
  svg += `<svg width="${width * 50}" height="${height * 50}" viewBox="${viewBox}" xmlns="http://www.w3.org/2000/svg">\n`;
  svg += `<g stroke="${edgeColor}" stroke-width="0.05" stroke-linejoin="round">\n`;

  for (let fIdx = 0; fIdx < mesh.faces.length; fIdx++) {
    const face = mesh.faces[fIdx];
    let d = "";
    for (let i = 0; i < face.length; i++) {
      const idx = face[i] * 3;
      const x = mesh.vertices[idx];
      const y = -mesh.vertices[idx + 1]; // invert y
      if (i === 0) d += `M ${x} ${y} `;
      else d += `L ${x} ${y} `;
    }
    d += "Z";
    
    // Add fill to each path
    const fill = faceColors[fIdx] || "#ffffff";
    svg += `  <path d="${d}" fill="${fill}" />\n`;
  }

  svg += `</g>\n</svg>`;

  downloadString(svg, 'tiling.svg', 'image/svg+xml');
}

function downloadString(content: string, filename: string, mimeType: string) {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
