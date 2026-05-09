import { Vector3 } from 'three';

export interface Mesh {
  vertices: number[];
  faces: number[][];
  faceValues?: number[];
}

export interface OperatorSpec {
  notation: string;
  tVe: number;
  tVf: number;
  tFe: number;
}

export interface OmniParamVisibility {
  showP1: boolean;
  showP2: boolean;
  showP3: boolean;
}

type OmniPointClass =
  | 'V'
  | 'E'
  | 'F'
  | 'F!'
  | 've'
  | 've0'
  | 've1'
  | 'vf'
  | 'vf!'
  | 'fe'
  | 'fe!';

interface SourceVertex {
  id: number;
  position: Vector3;
  normal: Vector3;
}

interface SourceFace {
  id: number;
  halfedge: SourceHalfedge;
  centroid: Vector3;
  normal: Vector3;
}

interface SourceHalfedge {
  id: number;
  vertex: SourceVertex;
  prev: SourceHalfedge;
  next: SourceHalfedge;
  pair: SourceHalfedge | null;
  face: SourceFace;
}

interface OVertex {
  id: number;
  pointClass: OmniPointClass;
  position: Vector3;
  normal: Vector3;
  sourceKeys: Set<string>;
}

interface OEdge {
  a: OVertex;
  b: OVertex;
  atom: string;
  sourceEdgeKey: string | null;
}

interface OperatorConnection {
  a: OVertex;
  b: OVertex;
  sourceEdgeIndex: number;
}

const EPSILON = 1e-4;
const OPERATOR_SPEC_DELIMITER = '~';

export const DEFAULT_OMNI_PARAMS = {
  tVe: 0.5,
  tVf: 0.5,
  tFe: 0.5,
};

export const OMNI_POINT_CLASSES: OmniPointClass[] = [
  'V',
  'E',
  'F',
  'F!',
  've',
  've0',
  've1',
  'vf',
  'vf!',
  'fe',
  'fe!',
];

export const OMNI_ATOMS = [
  'E-E', 'E-F', 'E-V', 'E-fe', 'E-ve', 'E-vf',
  'F-F!', 'F-V', 'F-fe', 'F-ve', 'F-vf',
  'V-V', 'V-ve', 'V-vf',
  'fe-V', 'fe-fe', 'fe-fe!', 'fe-ve', 'fe-vf',
  've-vf', 've0-ve0', 've1-ve1',
  'vf-vf', 'vf-vf!',
] as const;

export const OMNI_VALID_OPERATORS = [
  ['E-E'],
  ['F-F!'],
  ['V-V'],
  ['vf-vf', 'vf-vf!'],
  ['fe-fe', 'fe-fe!'],
  ['F-V'],
  ['E-E', 'E-F'],
  ['E-E', 'E-V'],
  ['E-vf', 'vf-vf'],
  ['E-vf', 'vf-vf!'],
  ['F-F!', 'F-V'],
  ['F-V', 'V-V'],
  ['F-vf', 'vf-vf!'],
  ['V-vf', 'vf-vf'],
  ['fe-V', 'fe-fe'],
  ['fe-V', 'fe-fe!'],
  ['ve0-ve0', 've1-ve1'],
  ['E-E', 'E-vf', 'vf-vf'],
  ['E-E', 'E-vf', 'vf-vf!'],
  ['E-E', 'E-fe', 'fe-fe'],
  ['E-vf', 'vf-vf', 'vf-vf!'],
  ['F-F!', 'F-vf', 'vf-vf!'],
  ['F-vf', 'vf-vf', 'vf-vf!'],
  ['F-fe', 'fe-fe', 'fe-fe!'],
  ['V-V', 'V-vf', 'vf-vf'],
  ['V-V', 'fe-V', 'fe-fe'],
  ['V-vf', 'vf-vf', 'vf-vf!'],
  ['fe-V', 'fe-fe', 'fe-fe!'],
  ['vf-vf', 'fe-vf', 'fe-fe!'],
  ['vf-vf!', 'fe-vf', 'fe-fe'],
  ['vf-vf!', 'fe-vf', 'fe-fe!'],
  ['vf-vf', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['vf-vf', 'vf-vf!', 'fe-vf', 'fe-fe!'],
  ['vf-vf!', 'fe-vf', 'fe-fe', 'fe-fe!'],
  ['E-F', 'E-V'],
  ['E-vf', 'F-vf'],
  ['E-vf', 'V-vf'],
  ['F-ve', 'V-ve'],
  ['F-ve', 've0-ve0'],
  ['F-ve', 've1-ve1'],
  ['F-fe', 'fe-V'],
  ['E-E', 'E-F', 'E-V'],
  ['E-E', 'E-ve', 've1-ve1'],
  ['E-E', 'E-vf', 'F-vf'],
  ['E-E', 'E-vf', 'V-vf'],
  ['E-F', 'E-V', 'F-V'],
  ['E-F', 'E-ve', 'F-ve'],
  ['E-F', 'E-vf', 'F-vf'],
  ['E-F', 'E-vf', 'vf-vf!'],
  ['E-V', 'E-vf', 'V-vf'],
  ['E-V', 'E-vf', 'vf-vf'],
  ['E-V', 'E-fe', 'fe-V'],
  ['E-V', 'E-fe', 'fe-fe'],
  ['E-ve', 'E-vf', 've-vf'],
  ['E-vf', 'E-fe', 'fe-vf'],
  ['E-vf', 'F-vf', 'vf-vf'],
  ['E-vf', 'F-vf', 'vf-vf!'],
  ['E-vf', 'V-vf', 'vf-vf'],
  ['E-vf', 'V-vf', 'vf-vf!'],
  ['E-vf', 'fe-vf', 'fe-fe'],
  ['F-F!', 'F-ve', 'V-ve'],
  ['F-F!', 'F-ve', 've1-ve1'],
  ['F-V', 'F-ve', 'V-ve'],
  ['F-V', 'F-fe', 'fe-V'],
  ['F-V', 'fe-V', 'fe-fe!'],
  ['F-ve', 'F-vf', 've-vf'],
  ['F-ve', 'F-fe', 'fe-ve'],
  ['F-ve', 've-vf', 'vf-vf!'],
  ['F-ve', 'fe-ve', 'fe-fe!'],
  ['F-vf', 'V-vf', 'vf-vf'],
  ['F-vf', 'V-vf', 'vf-vf!'],
  ['F-vf', 'fe-vf', 'fe-fe!'],
  ['F-fe', 'V-V', 'fe-V'],
  ['F-fe', 'fe-V', 'fe-fe'],
  ['F-fe', 'fe-V', 'fe-fe!'],
  ['F-fe', 'vf-vf!', 'fe-vf'],
  ['V-ve', 'fe-V', 'fe-ve'],
  ['V-ve', 've-vf', 'vf-vf'],
  ['V-ve', 'fe-ve', 'fe-fe'],
  ['V-ve', 'fe-ve', 'fe-fe!'],
  ['V-vf', 'fe-V', 'fe-vf'],
  ['V-vf', 'fe-vf', 'fe-fe'],
  ['V-vf', 'fe-vf', 'fe-fe!'],
  ['fe-V', 'vf-vf', 'fe-vf'],
  ['ve0-ve0', 've-vf', 'vf-vf'],
  ['ve0-ve0', 've-vf', 'vf-vf!'],
  ['ve0-ve0', 'fe-ve', 'fe-fe'],
  ['ve1-ve1', 've-vf', 'vf-vf'],
  ['ve1-ve1', 'fe-ve', 'fe-fe'],
  ['ve1-ve1', 'fe-ve', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'fe-vf'],
  ['E-E', 'E-F', 'E-vf', 'F-vf'],
  ['E-E', 'E-F', 'E-vf', 'vf-vf!'],
  ['E-E', 'E-V', 'E-vf', 'V-vf'],
  ['E-E', 'E-V', 'E-vf', 'vf-vf'],
  ['E-E', 'E-V', 'E-fe', 'fe-fe'],
  ['E-E', 'E-ve', 'E-vf', 've-vf'],
  ['E-E', 'E-ve', 've-vf', 'vf-vf!'],
  ['E-E', 'E-vf', 'E-fe', 'fe-vf'],
  ['E-E', 'E-vf', 'F-vf', 'vf-vf'],
  ['E-E', 'E-vf', 'V-vf', 'vf-vf!'],
  ['E-E', 'E-vf', 'fe-vf', 'fe-fe'],
  ['E-E', 'E-fe', 'F-fe', 'fe-fe'],
  ['E-E', 'E-fe', 'vf-vf', 'fe-vf'],
  ['E-F', 'E-vf', 'F-vf', 'vf-vf!'],
  ['E-V', 'E-vf', 'V-vf', 'vf-vf'],
  ['E-V', 'E-fe', 'fe-V', 'fe-fe'],
  ['E-ve', 'E-vf', 've-vf', 'vf-vf'],
  ['E-ve', 'E-vf', 've-vf', 'vf-vf!'],
  ['E-ve', 'E-fe', 'fe-ve', 'fe-fe'],
  ['E-vf', 'E-fe', 'vf-vf', 'fe-vf'],
  ['E-vf', 'E-fe', 'vf-vf!', 'fe-vf'],
  ['E-vf', 'E-fe', 'vf-vf!', 'fe-fe'],
  ['E-vf', 'E-fe', 'fe-vf', 'fe-fe'],
  ['E-vf', 'F-vf', 'vf-vf', 'vf-vf!'],
  ['E-vf', 'V-vf', 'vf-vf', 'vf-vf!'],
  ['E-vf', 'vf-vf', 'fe-vf', 'fe-fe'],
  ['E-vf', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['F-F!', 'F-V', 'F-ve', 'V-ve'],
  ['F-F!', 'F-ve', 'F-vf', 've-vf'],
  ['F-F!', 'F-ve', 've-vf', 'vf-vf!'],
  ['F-F!', 'F-vf', 'V-vf', 'vf-vf!'],
  ['F-V', 'F-fe', 'V-V', 'fe-V'],
  ['F-V', 'F-fe', 'fe-V', 'fe-fe!'],
  ['F-ve', 'F-vf', 've-vf', 'vf-vf!'],
  ['F-ve', 'F-fe', 'fe-ve', 'fe-fe!'],
  ['F-vf', 'F-fe', 'vf-vf!', 'fe-vf'],
  ['F-vf', 'F-fe', 'fe-vf', 'fe-fe!'],
  ['F-vf', 'V-V', 'V-vf', 'vf-vf'],
  ['F-vf', 'V-vf', 'vf-vf', 'vf-vf!'],
  ['F-vf', 'vf-vf', 'fe-vf', 'fe-fe!'],
  ['F-vf', 'vf-vf!', 'fe-vf', 'fe-fe!'],
  ['F-fe', 'V-V', 'fe-V', 'fe-fe'],
  ['F-fe', 'fe-V', 'fe-fe', 'fe-fe!'],
  ['F-fe', 'vf-vf', 'vf-vf!', 'fe-vf'],
  ['F-fe', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['F-fe', 'vf-vf!', 'fe-vf', 'fe-fe!'],
  ['V-V', 'V-vf', 'fe-V', 'fe-vf'],
  ['V-V', 'V-vf', 'fe-vf', 'fe-fe'],
  ['V-V', 'fe-V', 'vf-vf', 'fe-vf'],
  ['V-ve', 'V-vf', 've-vf', 'vf-vf'],
  ['V-ve', 'fe-V', 'fe-ve', 'fe-fe'],
  ['V-ve', 'fe-V', 'fe-ve', 'fe-fe!'],
  ['V-ve', 've-vf', 'vf-vf', 'vf-vf!'],
  ['V-ve', 'fe-ve', 'fe-fe', 'fe-fe!'],
  ['V-vf', 'fe-V', 'vf-vf', 'fe-vf'],
  ['V-vf', 'fe-V', 'vf-vf', 'fe-fe!'],
  ['V-vf', 'fe-V', 'fe-vf', 'fe-fe'],
  ['V-vf', 'fe-V', 'fe-vf', 'fe-fe!'],
  ['V-vf', 'vf-vf', 'fe-vf', 'fe-fe'],
  ['V-vf', 'vf-vf', 'fe-vf', 'fe-fe!'],
  ['V-vf', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['V-vf', 'vf-vf!', 'fe-vf', 'fe-fe!'],
  ['V-vf', 'fe-vf', 'fe-fe', 'fe-fe!'],
  ['fe-V', 'vf-vf', 'fe-vf', 'fe-fe!'],
  ['ve0-ve0', 've-vf', 'vf-vf', 'vf-vf!'],
  ['ve1-ve1', 've-vf', 'vf-vf', 'vf-vf!'],
  ['ve1-ve1', 'fe-ve', 'fe-fe', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'vf-vf', 'fe-vf'],
  ['ve-vf', 'fe-ve', 'vf-vf', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'vf-vf!', 'fe-vf'],
  ['ve-vf', 'fe-ve', 'vf-vf!', 'fe-fe'],
  ['ve-vf', 'fe-ve', 'vf-vf!', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'fe-vf', 'fe-fe'],
  ['ve-vf', 'fe-ve', 'fe-vf', 'fe-fe!'],
  ['E-E', 'E-ve', 'E-vf', 've-vf', 'vf-vf!'],
  ['E-E', 'E-vf', 'E-fe', 'vf-vf', 'fe-vf'],
  ['E-E', 'E-vf', 'E-fe', 'vf-vf!', 'fe-fe'],
  ['E-E', 'E-vf', 'E-fe', 'fe-vf', 'fe-fe'],
  ['E-ve', 'E-vf', 've-vf', 'vf-vf', 'vf-vf!'],
  ['E-vf', 'E-fe', 'vf-vf', 'vf-vf!', 'fe-vf'],
  ['E-vf', 'E-fe', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['E-vf', 'vf-vf', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['F-F!', 'F-ve', 'F-vf', 've-vf', 'vf-vf!'],
  ['F-vf', 'F-fe', 'vf-vf', 'vf-vf!', 'fe-vf'],
  ['F-vf', 'F-fe', 'vf-vf!', 'fe-vf', 'fe-fe!'],
  ['F-vf', 'vf-vf', 'vf-vf!', 'fe-vf', 'fe-fe!'],
  ['F-fe', 'vf-vf', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['F-fe', 'vf-vf!', 'fe-vf', 'fe-fe', 'fe-fe!'],
  ['V-V', 'V-vf', 'fe-V', 'vf-vf', 'fe-vf'],
  ['V-V', 'V-vf', 'fe-V', 'fe-vf', 'fe-fe'],
  ['V-V', 'V-vf', 'vf-vf', 'fe-vf', 'fe-fe'],
  ['V-ve', 'V-vf', 've-vf', 'vf-vf', 'vf-vf!'],
  ['V-ve', 'fe-V', 'fe-ve', 'fe-fe', 'fe-fe!'],
  ['V-vf', 'fe-V', 'vf-vf', 'fe-vf', 'fe-fe!'],
  ['V-vf', 'fe-V', 'fe-vf', 'fe-fe', 'fe-fe!'],
  ['V-vf', 'vf-vf', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['V-vf', 'vf-vf', 'vf-vf!', 'fe-vf', 'fe-fe!'],
  ['V-vf', 'vf-vf!', 'fe-vf', 'fe-fe', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'vf-vf', 'fe-vf', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'vf-vf!', 'fe-vf', 'fe-fe'],
  ['ve-vf', 'fe-ve', 'vf-vf!', 'fe-vf', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'vf-vf!', 'fe-fe', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'fe-vf', 'fe-fe', 'fe-fe!'],
  ['ve-vf', 'fe-ve', 'vf-vf!', 'fe-vf', 'fe-fe', 'fe-fe!'],
] as const;

export const OMNI_PRESETS: Record<string, string> = {
  Ambo: 'E-E',
  Dual: 'F-F!',
  Seed: 'V-V',
  Zip: 'fe-fe,fe-fe!',
  Truncate: 've0-ve0,ve1-ve1',
  Expand: 'vf-vf,vf-vf!',
  Join: 'F-V',
  Subdivide: 'E-E,E-V',
  Needle: 'F-F!,F-V',
  Kis: 'F-V,V-V',
  Chamfer: 'V-vf,vf-vf',
  Loft: 'V-V,V-vf,vf-vf',
  'Join-Lace': 'fe-V,fe-fe',
  Lace: 'V-V,fe-V,fe-fe',
  'Opposite-Lace': 'fe-V,fe-fe,fe-fe!',
  Ortho: 'E-F,E-V',
  Meta: 'E-F,E-V,F-V',
  Quinto: 'E-V,E-fe,fe-fe',
  'Join-Stake': 'F-fe,fe-V',
  Stake: 'F-fe,V-V,fe-V',
  'Opposite-Stake': 'F-fe,fe-V,fe-fe!',
  'Join-Kis-Kis': 'F-V,F-fe,fe-V',
  Medial: 'F-ve,V-ve,ve0-ve0',
};

const OPERATOR_ALIASES: Record<string, string> = {
  dual: OMNI_PRESETS.Dual,
  kis: OMNI_PRESETS.Kis,
  ambo: OMNI_PRESETS.Ambo,
  truncate: OMNI_PRESETS.Truncate,
  join: OMNI_PRESETS.Join,
  ortho: OMNI_PRESETS.Ortho,
};

let operatorVertexId = 0;

export function parseAtomList(input: string): string[] {
  if (!input.trim()) {
    return [];
  }

  return input
    .split(',')
    .map((atom) => atom.trim())
    .filter(Boolean);
}

export function joinAtomList(atoms: string[]): string {
  return atoms.join(',');
}

export function findOmniAtom(rowClass: OmniPointClass, colClass: OmniPointClass): string | null {
  const direct = `${rowClass}-${colClass}`;
  const reverse = `${colClass}-${rowClass}`;
  for (const atom of OMNI_ATOMS) {
    if (atom === direct || atom === reverse) {
      return atom;
    }
  }
  return null;
}

export function getUnknownAtoms(atoms: string[]): string[] {
  return atoms.filter((atom) => !OMNI_ATOMS.includes(atom as (typeof OMNI_ATOMS)[number]));
}

export function isValidSubset(atoms: string[]): boolean {
  const uniqueAtoms = new Set(atoms);
  if (uniqueAtoms.size === 0) {
    return true;
  }
  if (getUnknownAtoms([...uniqueAtoms]).length > 0) {
    return false;
  }

  return OMNI_VALID_OPERATORS.some((validOperator) => {
    const validSet = new Set<string>(validOperator);
    return [...uniqueAtoms].every((atom) => validSet.has(atom));
  });
}

export function isCompatibleSubset(selected: string[], candidate: string): boolean {
  const uniqueAtoms = new Set(selected);
  uniqueAtoms.add(candidate);
  return isValidSubset([...uniqueAtoms]);
}

export function isCompleteOperator(atoms: string[]): boolean {
  const uniqueAtoms = new Set(atoms);
  if (uniqueAtoms.size === 0) {
    return false;
  }
  if (getUnknownAtoms([...uniqueAtoms]).length > 0) {
    return false;
  }

  return OMNI_VALID_OPERATORS.some((validOperator) => {
    if (validOperator.length !== uniqueAtoms.size) {
      return false;
    }

    const validSet = new Set<string>(validOperator);
    return [...uniqueAtoms].every((atom) => validSet.has(atom));
  });
}

export function findPresetName(atoms: string[]): string | null {
  const uniqueAtoms = new Set(atoms);
  if (uniqueAtoms.size === 0) {
    return null;
  }

  for (const [name, notation] of Object.entries(OMNI_PRESETS)) {
    const presetAtoms = parseAtomList(notation);
    if (presetAtoms.length !== uniqueAtoms.size) {
      continue;
    }

    const presetSet = new Set(presetAtoms);
    if ([...uniqueAtoms].every((atom) => presetSet.has(atom))) {
      return name;
    }
  }

  return null;
}

export function orderAtoms(atoms: Iterable<string>): string[] {
  const selected = new Set(atoms);
  return OMNI_ATOMS.filter((atom) => selected.has(atom));
}

function cloneMesh(mesh: Mesh): Mesh {
  return {
    vertices: [...mesh.vertices],
    faces: mesh.faces.map((face) => [...face]),
  };
}

function actualMod(value: number, modulus: number): number {
  return ((value % modulus) + modulus) % modulus;
}

function twin(index: number): number {
  return index ^ 1;
}

function edgeKey(a: number, b: number): string {
  return a <= b ? `${a}_${b}` : `${b}_${a}`;
}

function normalizeClass(pointClass: string): OmniPointClass {
  const normalized = pointClass.startsWith('!')
    ? `${pointClass.slice(1)}!`
    : pointClass;

  switch (normalized) {
    case 'V':
    case 'E':
    case 'F':
    case 'F!':
    case 've':
    case 've0':
    case 've1':
    case 'vf':
    case 'vf!':
    case 'fe':
    case 'fe!':
      return normalized;
    default:
      throw new Error(`Unknown Omni point class '${pointClass}'`);
  }
}

function parseOperatorNotation(operatorNotation: string): Array<[OmniPointClass, OmniPointClass]> {
  return operatorNotation
    .split(',')
    .map((atom) => atom.trim())
    .filter(Boolean)
    .map((atom) => {
      const dashIndex = atom.indexOf('-');
      if (dashIndex < 0) {
        throw new Error(`Invalid Omni atom '${atom}': missing '-'`);
      }

      const a = normalizeClass(atom.slice(0, dashIndex).trim());
      const b = normalizeClass(atom.slice(dashIndex + 1).trim());
      return [a, b];
    });
}

function sourceFaceHalfedges(face: SourceFace): SourceHalfedge[] {
  const halfedges: SourceHalfedge[] = [];
  let current = face.halfedge;
  do {
    halfedges.push(current);
    current = current.next;
  } while (current !== face.halfedge);
  return halfedges;
}

function computeFaceNormal(face: number[], vertices: number[]): Vector3 {
  const normal = new Vector3();

  for (let i = 0; i < face.length; i++) {
    const currentIndex = face[i] * 3;
    const nextIndex = face[(i + 1) % face.length] * 3;

    const currentX = vertices[currentIndex];
    const currentY = vertices[currentIndex + 1];
    const currentZ = vertices[currentIndex + 2];
    const nextX = vertices[nextIndex];
    const nextY = vertices[nextIndex + 1];
    const nextZ = vertices[nextIndex + 2];

    normal.x += (currentY - nextY) * (currentZ + nextZ);
    normal.y += (currentZ - nextZ) * (currentX + nextX);
    normal.z += (currentX - nextX) * (currentY + nextY);
  }

  if (normal.lengthSq() < 1e-10) {
    normal.set(0, 0, 1);
  } else {
    normal.normalize();
  }

  return normal;
}

function buildHalfedgeMesh(mesh: Mesh): { vertices: SourceVertex[]; faces: SourceFace[] } {
  const sourceVertices: SourceVertex[] = [];
  for (let index = 0; index < mesh.vertices.length; index += 3) {
    sourceVertices.push({
      id: index / 3,
      position: new Vector3(mesh.vertices[index], mesh.vertices[index + 1], mesh.vertices[index + 2]),
      normal: new Vector3(),
    });
  }

  const faces: SourceFace[] = [];
  const directedHalfedges = new Map<string, SourceHalfedge>();
  let halfedgeId = 0;

  mesh.faces.forEach((faceIndices, faceId) => {
    if (faceIndices.length < 3) {
      return;
    }

    const halfedges = faceIndices.map((_, edgeIndex) => {
      const destinationVertex = sourceVertices[faceIndices[(edgeIndex + 1) % faceIndices.length]];
      return {
        id: halfedgeId++,
        vertex: destinationVertex,
        prev: null as unknown as SourceHalfedge,
        next: null as unknown as SourceHalfedge,
        pair: null,
        face: null as unknown as SourceFace,
      };
    });

    for (let edgeIndex = 0; edgeIndex < halfedges.length; edgeIndex++) {
      halfedges[edgeIndex].prev = halfedges[actualMod(edgeIndex - 1, halfedges.length)];
      halfedges[edgeIndex].next = halfedges[(edgeIndex + 1) % halfedges.length];
    }

    const centroid = new Vector3();
    faceIndices.forEach((vertexIndex) => {
      centroid.add(sourceVertices[vertexIndex].position);
    });
    centroid.divideScalar(faceIndices.length);

    const face: SourceFace = {
      id: faceId,
      halfedge: halfedges[0],
      centroid,
      normal: computeFaceNormal(faceIndices, mesh.vertices),
    };

    halfedges.forEach((halfedge) => {
      halfedge.face = face;
      const originId = halfedge.prev.vertex.id;
      const destinationId = halfedge.vertex.id;
      directedHalfedges.set(`${originId}_${destinationId}`, halfedge);
    });

    faceIndices.forEach((vertexIndex) => {
      sourceVertices[vertexIndex].normal.add(face.normal);
    });

    faces.push(face);
  });

  faces.forEach((face) => {
    sourceFaceHalfedges(face).forEach((halfedge) => {
      const reverseKey = `${halfedge.vertex.id}_${halfedge.prev.vertex.id}`;
      halfedge.pair = directedHalfedges.get(reverseKey) ?? null;
    });
  });

  sourceVertices.forEach((vertex) => {
    if (vertex.normal.lengthSq() < 1e-10) {
      vertex.normal.set(0, 0, 1);
    } else {
      vertex.normal.normalize();
    }
  });

  return { vertices: sourceVertices, faces };
}

class OperatorVertexCache {
  private readonly cache = new Map<string, OVertex>();

  getOrCreate(key: string, pointClass: OmniPointClass, position: Vector3, normal: Vector3): OVertex {
    const existing = this.cache.get(key);
    if (existing) {
      return existing;
    }

    const created: OVertex = {
      id: ++operatorVertexId,
      pointClass,
      position: position.clone(),
      normal: normal.clone(),
      sourceKeys: new Set<string>(),
    };
    this.cache.set(key, created);
    return created;
  }
}

function computeEdgeArray(
  face: SourceFace,
  halfedges: SourceHalfedge[],
  cache: OperatorVertexCache
): OVertex[] {
  return halfedges.map((halfedge) => {
    const pairNormal = halfedge.pair?.face.normal ?? face.normal;
    const edgeNormal = face.normal.clone().add(pairNormal).multiplyScalar(0.5);
    if (edgeNormal.lengthSq() < 1e-10) {
      edgeNormal.copy(face.normal);
    } else {
      edgeNormal.normalize();
    }

    const midpoint = halfedge.prev.vertex.position.clone().add(halfedge.vertex.position).multiplyScalar(0.5);
    const vertex = cache.getOrCreate(
      `E_${edgeKey(halfedge.prev.vertex.id, halfedge.vertex.id)}`,
      'E',
      midpoint,
      edgeNormal
    );
    vertex.sourceKeys.add(`e:${edgeKey(halfedge.prev.vertex.id, halfedge.vertex.id)}`);
    vertex.sourceKeys.add(`v:${halfedge.prev.vertex.id}`);
    vertex.sourceKeys.add(`v:${halfedge.vertex.id}`);
    return vertex;
  });
}

function buildFacePoints(
  face: SourceFace,
  halfedges: SourceHalfedge[],
  classesNeeded: Set<OmniPointClass>,
  cache: OperatorVertexCache,
  tVe: number,
  tVf: number,
  tFe: number
): { singles: Map<string, OVertex>; arrays: Map<string, OVertex[]> } {
  const singles = new Map<string, OVertex>();
  const arrays = new Map<string, OVertex[]>();
  const count = halfedges.length;
  const faceNormal = face.normal;

  const needV = classesNeeded.has('V');
  const needE = classesNeeded.has('E');
  const needF = classesNeeded.has('F');
  const needVe = classesNeeded.has('ve') || classesNeeded.has('ve0') || classesNeeded.has('ve1');
  const needVf = classesNeeded.has('vf');
  const needFe = classesNeeded.has('fe');
  const needFAdjacent = classesNeeded.has('F!');
  const needFeAdjacent = classesNeeded.has('fe!');
  const needVfAdjacent = classesNeeded.has('vf!');

  let vArray: OVertex[] | null = null;
  if (needV || needVe || needVf || needVfAdjacent) {
    vArray = new Array<OVertex>(count);
    for (let index = 0; index < count; index++) {
      const vertex = halfedges[index].prev.vertex;
      const operatorVertex = cache.getOrCreate(`V_${vertex.id}`, 'V', vertex.position, vertex.normal);
      operatorVertex.sourceKeys.add(`v:${vertex.id}`);
      operatorVertex.sourceKeys.add(`f:${face.id}`);
      vArray[index] = operatorVertex;
    }
    arrays.set('V', vArray);
  }

  let eArray: OVertex[] | null = null;
  if (needE || needVe || needFe || needFeAdjacent) {
    eArray = computeEdgeArray(face, halfedges, cache);
    arrays.set('E', eArray);
  }

  let fVertex: OVertex | null = null;
  if (needF || needVf || needFe) {
    fVertex = cache.getOrCreate(`F_${face.id}`, 'F', face.centroid, faceNormal);
    fVertex.sourceKeys.add(`f:${face.id}`);
    singles.set('F', fVertex);
  }

  if (needVe) {
    if (!eArray) {
      eArray = computeEdgeArray(face, halfedges, cache);
      arrays.set('E', eArray);
    }

    const ve = new Array<OVertex>(count * 2);
    for (let index = 0; index < count; index++) {
      const halfedge = halfedges[index];
      const origin = halfedge.prev.vertex;
      const destination = halfedge.vertex;
      const edgeNormal = eArray[index].normal;
      const sourceKey = edgeKey(origin.id, destination.id);

      ve[index * 2] = cache.getOrCreate(
        `ve_${origin.id}_${sourceKey}`,
        've',
        origin.position.clone().lerp(eArray[index].position, tVe),
        edgeNormal
      );
      ve[index * 2].sourceKeys.add(`v:${origin.id}`);
      ve[index * 2].sourceKeys.add(`e:${sourceKey}`);

      ve[index * 2 + 1] = cache.getOrCreate(
        `ve_${destination.id}_${sourceKey}`,
        've',
        destination.position.clone().lerp(eArray[index].position, tVe),
        edgeNormal
      );
      ve[index * 2 + 1].sourceKeys.add(`v:${destination.id}`);
      ve[index * 2 + 1].sourceKeys.add(`e:${sourceKey}`);
    }

    arrays.set('ve', ve);
    arrays.set('ve0', ve);
    arrays.set('ve1', ve);
  }

  if (needVf) {
    if (!fVertex) {
      fVertex = cache.getOrCreate(`F_${face.id}`, 'F', face.centroid, faceNormal);
      fVertex.sourceKeys.add(`f:${face.id}`);
      singles.set('F', fVertex);
    }

    const vf = new Array<OVertex>(count);
    for (let index = 0; index < count; index++) {
      const vertex = halfedges[index].prev.vertex;
      const normal = vertex.normal.clone().lerp(faceNormal, tVf);
      if (normal.lengthSq() < 1e-10) {
        normal.copy(faceNormal);
      } else {
        normal.normalize();
      }

      vf[index] = cache.getOrCreate(
        `vf_${face.id}_${vertex.id}`,
        'vf',
        vertex.position.clone().lerp(fVertex.position, tVf),
        normal
      );
      vf[index].sourceKeys.add(`v:${vertex.id}`);
      vf[index].sourceKeys.add(`f:${face.id}`);
    }
    arrays.set('vf', vf);
  }

  if (needFe) {
    if (!fVertex) {
      fVertex = cache.getOrCreate(`F_${face.id}`, 'F', face.centroid, faceNormal);
      fVertex.sourceKeys.add(`f:${face.id}`);
      singles.set('F', fVertex);
    }
    if (!eArray) {
      eArray = computeEdgeArray(face, halfedges, cache);
      arrays.set('E', eArray);
    }

    const fe = new Array<OVertex>(count);
    for (let index = 0; index < count; index++) {
      const halfedge = halfedges[index];
      const sourceKey = edgeKey(halfedge.prev.vertex.id, halfedge.vertex.id);
      const normal = eArray[index].normal.clone().lerp(faceNormal, tFe);
      if (normal.lengthSq() < 1e-10) {
        normal.copy(faceNormal);
      } else {
        normal.normalize();
      }

      fe[index] = cache.getOrCreate(
        `fe_${face.id}_${sourceKey}`,
        'fe',
        fVertex.position.clone().lerp(eArray[index].position, tFe),
        normal
      );
      fe[index].sourceKeys.add(`e:${sourceKey}`);
      fe[index].sourceKeys.add(`f:${face.id}`);
    }
    arrays.set('fe', fe);
  }

  let adjacentFaces: OVertex[] | null = null;
  if (needFAdjacent || needVfAdjacent || needFeAdjacent) {
    adjacentFaces = new Array<OVertex>(count);
    for (let index = 0; index < count; index++) {
      const pairFace = halfedges[index].pair?.face ?? face;
      const operatorVertex = cache.getOrCreate(`F_${pairFace.id}`, 'F!', pairFace.centroid, pairFace.normal);
      operatorVertex.sourceKeys.add(`f:${pairFace.id}`);
      adjacentFaces[index] = operatorVertex;
    }
    arrays.set('F!', adjacentFaces);
  }

  if (needFeAdjacent) {
    if (!eArray) {
      eArray = computeEdgeArray(face, halfedges, cache);
      arrays.set('E', eArray);
    }
    if (!adjacentFaces) {
      throw new Error('Missing adjacent face data for fe!');
    }

    const feAdjacent = new Array<OVertex>(count);
    for (let index = 0; index < count; index++) {
      const halfedge = halfedges[index];
      const pairFace = halfedge.pair?.face ?? face;
      const sourceKey = edgeKey(halfedge.prev.vertex.id, halfedge.vertex.id);
      const normal = eArray[index].normal.clone().lerp(adjacentFaces[index].normal, tFe);
      if (normal.lengthSq() < 1e-10) {
        normal.copy(adjacentFaces[index].normal);
      } else {
        normal.normalize();
      }

      feAdjacent[index] = cache.getOrCreate(
        `fe_${pairFace.id}_${sourceKey}`,
        'fe!',
        adjacentFaces[index].position.clone().lerp(eArray[index].position, tFe),
        normal
      );
      feAdjacent[index].sourceKeys.add(`e:${sourceKey}`);
      feAdjacent[index].sourceKeys.add(`f:${pairFace.id}`);
    }
    arrays.set('fe!', feAdjacent);
  }

  if (needVfAdjacent) {
    if (!adjacentFaces) {
      throw new Error('Missing adjacent face data for vf!');
    }

    const vfAdjacent = new Array<OVertex>(count * 2);
    for (let index = 0; index < count; index++) {
      const pairFace = halfedges[index].pair?.face ?? face;
      const origin = halfedges[index].prev.vertex;
      const destination = halfedges[index].vertex;

      const originNormal = origin.normal.clone().lerp(adjacentFaces[index].normal, tVf);
      if (originNormal.lengthSq() < 1e-10) {
        originNormal.copy(adjacentFaces[index].normal);
      } else {
        originNormal.normalize();
      }

      vfAdjacent[index * 2] = cache.getOrCreate(
        `vf_${pairFace.id}_${origin.id}`,
        'vf!',
        origin.position.clone().lerp(adjacentFaces[index].position, tVf),
        originNormal
      );
      vfAdjacent[index * 2].sourceKeys.add(`v:${origin.id}`);
      vfAdjacent[index * 2].sourceKeys.add(`f:${pairFace.id}`);

      const destinationNormal = destination.normal.clone().lerp(adjacentFaces[index].normal, tVf);
      if (destinationNormal.lengthSq() < 1e-10) {
        destinationNormal.copy(adjacentFaces[index].normal);
      } else {
        destinationNormal.normalize();
      }

      vfAdjacent[index * 2 + 1] = cache.getOrCreate(
        `vf_${pairFace.id}_${destination.id}`,
        'vf!',
        destination.position.clone().lerp(adjacentFaces[index].position, tVf),
        destinationNormal
      );
      vfAdjacent[index * 2 + 1].sourceKeys.add(`v:${destination.id}`);
      vfAdjacent[index * 2 + 1].sourceKeys.add(`f:${pairFace.id}`);
    }
    arrays.set('vf!', vfAdjacent);
  }

  return { singles, arrays };
}

function tryConnections(
  classA: OmniPointClass,
  classB: OmniPointClass,
  singles: Map<string, OVertex>,
  arrays: Map<string, OVertex[]>,
  count: number,
  result: OperatorConnection[]
): boolean {
  const getArray = (key: string): OVertex[] => {
    const values = arrays.get(key);
    if (!values) {
      throw new Error(`Missing Omni point class array '${key}'`);
    }
    return values;
  };

  const getSingle = (key: string): OVertex => {
    const value = singles.get(key);
    if (!value) {
      throw new Error(`Missing Omni point class vertex '${key}'`);
    }
    return value;
  };

  const add = (a: OVertex, b: OVertex, sourceEdgeIndex = -1) => {
    result.push({ a, b, sourceEdgeIndex });
  };

  switch (`${classA}-${classB}`) {
    case 'E-E': {
      const points = getArray('E');
      for (let index = 0; index < count; index++) {
        add(points[index], points[(index + 1) % count], index);
      }
      return true;
    }
    case 'E-F': {
      const edgePoints = getArray('E');
      const facePoint = getSingle('F');
      for (let index = 0; index < count; index++) {
        add(edgePoints[index], facePoint, index);
      }
      return true;
    }
    case 'E-V': {
      const edgePoints = getArray('E');
      const vertexPoints = getArray('V');
      for (let index = 0; index < count; index++) {
        add(edgePoints[index], vertexPoints[index], index);
        add(edgePoints[index], vertexPoints[(index + 1) % count], index);
      }
      return true;
    }
    case 'E-ve':
    case 'E-ve0':
    case 'E-ve1': {
      const edgePoints = getArray('E');
      const vePoints = getArray('ve');
      for (let index = 0; index < count; index++) {
        add(edgePoints[index], vePoints[index * 2], index);
        add(edgePoints[index], vePoints[index * 2 + 1], index);
      }
      return true;
    }
    case 'E-vf': {
      const edgePoints = getArray('E');
      const vfPoints = getArray('vf');
      for (let index = 0; index < count; index++) {
        add(edgePoints[index], vfPoints[index], index);
        add(edgePoints[index], vfPoints[(index + 1) % count], index);
      }
      return true;
    }
    case 'E-fe': {
      const edgePoints = getArray('E');
      const fePoints = getArray('fe');
      for (let index = 0; index < count; index++) {
        add(edgePoints[index], fePoints[index], index);
      }
      return true;
    }
    case 'F-F!': {
      const facePoint = getSingle('F');
      const adjacentFacePoints = getArray('F!');
      for (let index = 0; index < count; index++) {
        add(facePoint, adjacentFacePoints[index], index);
      }
      return true;
    }
    case 'F-V': {
      const facePoint = getSingle('F');
      const vertexPoints = getArray('V');
      for (let index = 0; index < count; index++) {
        add(facePoint, vertexPoints[index]);
      }
      return true;
    }
    case 'F-ve':
    case 'F-ve0':
    case 'F-ve1': {
      const facePoint = getSingle('F');
      const vePoints = getArray('ve');
      for (let index = 0; index < vePoints.length; index++) {
        add(facePoint, vePoints[index]);
      }
      return true;
    }
    case 'F-vf': {
      const facePoint = getSingle('F');
      const vfPoints = getArray('vf');
      for (let index = 0; index < count; index++) {
        add(facePoint, vfPoints[index]);
      }
      return true;
    }
    case 'F-fe': {
      const facePoint = getSingle('F');
      const fePoints = getArray('fe');
      for (let index = 0; index < count; index++) {
        add(facePoint, fePoints[index], index);
      }
      return true;
    }
    case 'V-V': {
      const vertexPoints = getArray('V');
      for (let index = 0; index < count; index++) {
        add(vertexPoints[index], vertexPoints[(index + 1) % count], index);
      }
      return true;
    }
    case 'V-ve':
    case 'V-ve0':
    case 'V-ve1': {
      const vertexPoints = getArray('V');
      const vePoints = getArray('ve');
      for (let index = 0; index < count; index++) {
        add(vertexPoints[index], vePoints[actualMod(index * 2 - 1, count * 2)], actualMod(index - 1, count));
        add(vertexPoints[index], vePoints[index * 2], index);
      }
      return true;
    }
    case 'V-vf': {
      const vertexPoints = getArray('V');
      const vfPoints = getArray('vf');
      for (let index = 0; index < count; index++) {
        add(vertexPoints[index], vfPoints[index]);
      }
      return true;
    }
    case 'fe-V': {
      const fePoints = getArray('fe');
      const vertexPoints = getArray('V');
      for (let index = 0; index < count; index++) {
        add(fePoints[index], vertexPoints[index], index);
        add(fePoints[index], vertexPoints[(index + 1) % count], index);
      }
      return true;
    }
    case 've0-ve0': {
      const vePoints = getArray('ve0');
      for (let index = 0; index < count; index++) {
        add(vePoints[index * 2], vePoints[index * 2 + 1], index);
      }
      return true;
    }
    case 've1-ve1': {
      const vePoints = getArray('ve1');
      for (let index = 0; index < count; index++) {
        add(vePoints[index * 2 + 1], vePoints[(index * 2 + 2) % (count * 2)], index);
      }
      return true;
    }
    case 've-vf':
    case 've0-vf':
    case 've1-vf': {
      const vePoints = getArray('ve');
      const vfPoints = getArray('vf');
      for (let index = 0; index < count; index++) {
        add(vePoints[index * 2], vfPoints[index], index);
        add(vePoints[index * 2 + 1], vfPoints[(index + 1) % count], index);
      }
      return true;
    }
    case 'fe-ve':
    case 'fe-ve0':
    case 'fe-ve1': {
      const fePoints = getArray('fe');
      const vePoints = getArray('ve');
      for (let index = 0; index < count; index++) {
        add(fePoints[index], vePoints[index * 2], index);
        add(fePoints[index], vePoints[index * 2 + 1], index);
      }
      return true;
    }
    case 'vf-vf': {
      const vfPoints = getArray('vf');
      for (let index = 0; index < count; index++) {
        add(vfPoints[index], vfPoints[(index + 1) % count], index);
      }
      return true;
    }
    case 'vf-vf!': {
      const vfPoints = getArray('vf');
      const adjacentVfPoints = getArray('vf!');
      for (let index = 0; index < count; index++) {
        add(vfPoints[index], adjacentVfPoints[index * 2], index);
      }
      return true;
    }
    case 'fe-vf': {
      const fePoints = getArray('fe');
      const vfPoints = getArray('vf');
      for (let index = 0; index < count; index++) {
        add(fePoints[index], vfPoints[index], index);
        add(fePoints[index], vfPoints[(index + 1) % count], index);
      }
      return true;
    }
    case 'fe-fe': {
      const fePoints = getArray('fe');
      for (let index = 0; index < count; index++) {
        add(fePoints[index], fePoints[(index + 1) % count], index);
      }
      return true;
    }
    case 'fe-fe!': {
      const fePoints = getArray('fe');
      const adjacentFePoints = getArray('fe!');
      for (let index = 0; index < count; index++) {
        add(fePoints[index], adjacentFePoints[index], index);
      }
      return true;
    }
    default:
      return false;
  }
}

function addAtomConnections(
  classA: OmniPointClass,
  classB: OmniPointClass,
  sourceEdgeKeys: string[],
  singles: Map<string, OVertex>,
  arrays: Map<string, OVertex[]>,
  count: number,
  edges: OEdge[],
  edgeSeen: Set<string>
) {
  const connections: OperatorConnection[] = [];
  if (!tryConnections(classA, classB, singles, arrays, count, connections)) {
    if (!tryConnections(classB, classA, singles, arrays, count, connections)) {
      throw new Error(`Unknown Omni atom '${classA}-${classB}'`);
    }
  }

  const atom = `${classA}-${classB}`;
  connections.forEach((connection) => {
    if (connection.a.id === connection.b.id) {
      return;
    }

    const key = connection.a.id < connection.b.id
      ? `${connection.a.id}_${connection.b.id}`
      : `${connection.b.id}_${connection.a.id}`;

    if (!edgeSeen.has(key)) {
      edgeSeen.add(key);
      edges.push({
        a: connection.a,
        b: connection.b,
        atom,
        sourceEdgeKey:
          connection.sourceEdgeIndex >= 0 && connection.sourceEdgeIndex < sourceEdgeKeys.length
            ? sourceEdgeKeys[connection.sourceEdgeIndex]
            : null,
      });
    }
  });
}

function sortCCW(outgoing: number[], vertex: OVertex, halfedgeOrigins: OVertex[]) {
  const normal = vertex.normal.clone();
  if (normal.lengthSq() < 1e-10) {
    normal.set(0, 0, 1);
  } else {
    normal.normalize();
  }

  const center = vertex.position;
  const firstDestination = halfedgeOrigins[twin(outgoing[0])].position.clone().sub(center);
  let referenceDirection = firstDestination.sub(normal.clone().multiplyScalar(firstDestination.dot(normal)));
  if (referenceDirection.lengthSq() < 1e-10) {
    const arbitrary = Math.abs(normal.x) < 0.9 ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0);
    referenceDirection = new Vector3().crossVectors(normal, arbitrary);
  }
  referenceDirection.normalize();
  const perpendicularDirection = new Vector3().crossVectors(normal, referenceDirection);

  outgoing.sort((a, b) => {
    const directionA = halfedgeOrigins[twin(a)].position.clone().sub(center);
    directionA.sub(normal.clone().multiplyScalar(directionA.dot(normal)));
    const directionB = halfedgeOrigins[twin(b)].position.clone().sub(center);
    directionB.sub(normal.clone().multiplyScalar(directionB.dot(normal)));

    const angleA = Math.atan2(directionA.dot(perpendicularDirection), directionA.dot(referenceDirection));
    const angleB = Math.atan2(directionB.dot(perpendicularDirection), directionB.dot(referenceDirection));

    if (angleA !== angleB) {
      return angleA - angleB;
    }

    const radiusA = directionA.lengthSq();
    const radiusB = directionB.lengthSq();
    if (radiusA !== radiusB) {
      return radiusA - radiusB;
    }

    return a - b;
  });
}

function tryRewriteTwoBundleVfStarOrder(vertex: OVertex, outgoing: number[], edges: OEdge[]): boolean {
  if (vertex.pointClass !== 'vf' || outgoing.length !== 4) {
    return false;
  }

  const bundles = new Map<string, Array<{ halfedge: number; index: number; edge: OEdge }>>();
  outgoing.forEach((halfedge, index) => {
    const edge = edges[Math.floor(halfedge / 2)];
    if (!edge.sourceEdgeKey) {
      return;
    }

    if (!bundles.has(edge.sourceEdgeKey)) {
      bundles.set(edge.sourceEdgeKey, []);
    }
    bundles.get(edge.sourceEdgeKey)?.push({ halfedge, index, edge });
  });

  const orderedBundles = [...bundles.entries()]
    .map(([sourceEdgeKey, items]) => ({
      sourceEdgeKey,
      items: [...items].sort((a, b) => a.index - b.index),
    }))
    .sort((a, b) => a.items[0].index - b.items[0].index);

  if (orderedBundles.length !== 2 || orderedBundles.some((bundle) => bundle.items.length !== 2)) {
    return false;
  }

  const rewritten: number[] = [];
  for (const bundle of orderedBundles) {
    const far = bundle.items.find((item) => item.edge.atom === 'vf-vf');
    const near = bundle.items.find((item) => item.edge.atom === 'fe-vf');
    if (!far || !near) {
      return false;
    }

    rewritten.push(near.halfedge, far.halfedge);
  }

  [rewritten[2], rewritten[3]] = [rewritten[3], rewritten[2]];

  const changed = outgoing.some((value, index) => value !== rewritten[index]);
  if (!changed) {
    return false;
  }

  outgoing.splice(0, outgoing.length, ...rewritten);
  return true;
}

function pruneUniqueLongestPositiveOrientationLoop(
  faceLoops: OVertex[][],
  positiveOrientationIndices: number[]
) {
  if (positiveOrientationIndices.length === 0) {
    return;
  }

  const candidates = [...new Set(positiveOrientationIndices)]
    .filter((index) => index >= 0 && index < faceLoops.length)
    .map((index) => ({ index, length: faceLoops[index].length }));

  if (candidates.length === 0) {
    return;
  }

  const maxLength = Math.max(...candidates.map((candidate) => candidate.length));
  const longest = candidates.filter((candidate) => candidate.length === maxLength);
  if (longest.length !== 1) {
    return;
  }

  const secondLongest = Math.max(
    0,
    ...candidates
      .filter((candidate) => candidate.length < maxLength)
      .map((candidate) => candidate.length)
  );

  if (maxLength <= secondLongest) {
    return;
  }

  faceLoops.splice(longest[0].index, 1);
}

function buildMeshFromEdges(edges: OEdge[]): Mesh {
  if (edges.length === 0) {
    throw new Error('Omni operator produced no edges');
  }

  const halfedgeCount = edges.length * 2;
  const halfedgeOrigins = new Array<OVertex>(halfedgeCount);
  const halfedgeNext = new Array<number>(halfedgeCount).fill(-1);

  edges.forEach((edge, edgeIndex) => {
    halfedgeOrigins[edgeIndex * 2] = edge.a;
    halfedgeOrigins[edgeIndex * 2 + 1] = edge.b;
  });

  const outgoing = new Map<OVertex, number[]>();
  for (let halfedgeIndex = 0; halfedgeIndex < halfedgeCount; halfedgeIndex++) {
    const origin = halfedgeOrigins[halfedgeIndex];
    if (!outgoing.has(origin)) {
      outgoing.set(origin, []);
    }
    outgoing.get(origin)?.push(halfedgeIndex);
  }

  outgoing.forEach((halfedges, vertex) => {
    if (halfedges.length === 1) {
      halfedgeNext[twin(halfedges[0])] = halfedges[0];
      return;
    }

    sortCCW(halfedges, vertex, halfedgeOrigins);
    tryRewriteTwoBundleVfStarOrder(vertex, halfedges, edges);

    for (let index = 0; index < halfedges.length; index++) {
      halfedgeNext[twin(halfedges[index])] = halfedges[(index + 1) % halfedges.length];
    }
  });

  const visited = new Array<boolean>(halfedgeCount).fill(false);
  const faceLoops: OVertex[][] = [];
  const positiveOrientationIndices: number[] = [];

  for (let start = 0; start < halfedgeCount; start++) {
    if (visited[start] || halfedgeNext[start] < 0) {
      continue;
    }

    const loop: OVertex[] = [];
    let current = start;
    while (!visited[current]) {
      visited[current] = true;
      loop.push(halfedgeOrigins[current]);
      current = halfedgeNext[current];
      if (current < 0) {
        break;
      }
    }

    if (loop.length < 3) {
      continue;
    }

    const centroid = new Vector3();
    loop.forEach((vertex) => centroid.add(vertex.position));
    centroid.divideScalar(loop.length);

    const faceNormal = new Vector3();
    for (let index = 0; index < loop.length; index++) {
      const currentVector = loop[index].position.clone().sub(centroid);
      const nextVector = loop[(index + 1) % loop.length].position.clone().sub(centroid);
      faceNormal.add(new Vector3().crossVectors(currentVector, nextVector));
    }

    const averageVertexNormal = new Vector3();
    loop.forEach((vertex) => averageVertexNormal.add(vertex.normal));
    const dot = faceNormal.dot(averageVertexNormal);

    if (dot < 0) {
      loop.reverse();
    }

    const faceIndex = faceLoops.length;
    faceLoops.push(loop);
    if (dot >= 0) {
      positiveOrientationIndices.push(faceIndex);
    }
  }

  pruneUniqueLongestPositiveOrientationLoop(faceLoops, positiveOrientationIndices);

  const vertices: number[] = [];
  const vertexIndices = new Map<OVertex, number>();
  const faces: number[][] = [];

  faceLoops.forEach((loop) => {
    const indices = loop.map((vertex) => {
      const existingIndex = vertexIndices.get(vertex);
      if (existingIndex !== undefined) {
        return existingIndex;
      }

      const nextIndex = vertices.length / 3;
      vertexIndices.set(vertex, nextIndex);
      vertices.push(vertex.position.x, vertex.position.y, vertex.position.z);
      return nextIndex;
    });
    faces.push(indices);
  });

  return { vertices, faces };
}

export function applyOmni(
  mesh: Mesh,
  operatorNotation: string,
  tVe = DEFAULT_OMNI_PARAMS.tVe,
  tVf = DEFAULT_OMNI_PARAMS.tVf,
  tFe = DEFAULT_OMNI_PARAMS.tFe
): Mesh {
  if (!operatorNotation.trim()) {
    return cloneMesh(mesh);
  }

  let resolvedTVe = tVe;
  let resolvedTVf = tVf;
  let resolvedTFe = tFe;
  if (Math.abs(resolvedTVe - resolvedTVf) < EPSILON) resolvedTVf += EPSILON;
  if (Math.abs(resolvedTVe - resolvedTFe) < EPSILON) resolvedTFe += EPSILON;
  if (Math.abs(resolvedTVf - resolvedTFe) < EPSILON) resolvedTFe += EPSILON;

  const sourceMesh = buildHalfedgeMesh(mesh);
  const atoms = parseOperatorNotation(operatorNotation);
  if (atoms.length === 0) {
    return cloneMesh(mesh);
  }

  const classesNeeded = new Set<OmniPointClass>();
  atoms.forEach(([classA, classB]) => {
    classesNeeded.add(classA);
    classesNeeded.add(classB);
  });

  const cache = new OperatorVertexCache();
  const edgeSeen = new Set<string>();
  const edges: OEdge[] = [];

  sourceMesh.faces.forEach((face) => {
    const halfedges = sourceFaceHalfedges(face);
    const sourceEdgeKeys = halfedges.map((halfedge) => edgeKey(halfedge.prev.vertex.id, halfedge.vertex.id));
    const points = buildFacePoints(
      face,
      halfedges,
      classesNeeded,
      cache,
      resolvedTVe,
      resolvedTVf,
      resolvedTFe
    );

    atoms.forEach(([classA, classB]) => {
      addAtomConnections(
        classA,
        classB,
        sourceEdgeKeys,
        points.singles,
        points.arrays,
        halfedges.length,
        edges,
        edgeSeen
      );
    });
  });

  return buildMeshFromEdges(edges);
}

export function createOperatorSpec(notation: string, overrides: Partial<OperatorSpec> = {}): OperatorSpec {
  return {
    notation,
    tVe: overrides.tVe ?? DEFAULT_OMNI_PARAMS.tVe,
    tVf: overrides.tVf ?? DEFAULT_OMNI_PARAMS.tVf,
    tFe: overrides.tFe ?? DEFAULT_OMNI_PARAMS.tFe,
  };
}

export function resolveOperatorNotation(operator: string): string {
  return OPERATOR_ALIASES[operator] ?? OMNI_PRESETS[operator] ?? operator;
}

export function getOmniParamVisibility(operator: string): OmniParamVisibility {
  const visibility: OmniParamVisibility = {
    showP1: false,
    showP2: false,
    showP3: false,
  };

  parseAtomList(resolveOperatorNotation(operator)).forEach((atom) => {
    const [left, right] = atom.split('-');
    [left, right].forEach((pointClass) => {
      if (pointClass === 've' || pointClass === 've0' || pointClass === 've1') {
        visibility.showP1 = true;
      }
      if (pointClass === 'vf' || pointClass === 'vf!') {
        visibility.showP2 = true;
      }
      if (pointClass === 'fe' || pointClass === 'fe!') {
        visibility.showP3 = true;
      }
    });
  });

  return visibility;
}

export function serializeOperatorSpec(spec: OperatorSpec): string {
  return [
    spec.notation,
    spec.tVe.toString(),
    spec.tVf.toString(),
    spec.tFe.toString(),
  ].join(OPERATOR_SPEC_DELIMITER);
}

export function parseOperatorSpec(serialized: string): OperatorSpec {
  const parts = serialized.split(OPERATOR_SPEC_DELIMITER);
  if (parts.length !== 4) {
    return createOperatorSpec(serialized);
  }

  const [notation, tVeRaw, tVfRaw, tFeRaw] = parts;
  const tVe = Number.parseFloat(tVeRaw);
  const tVf = Number.parseFloat(tVfRaw);
  const tFe = Number.parseFloat(tFeRaw);

  return createOperatorSpec(notation, {
    tVe: Number.isFinite(tVe) ? tVe : DEFAULT_OMNI_PARAMS.tVe,
    tVf: Number.isFinite(tVf) ? tVf : DEFAULT_OMNI_PARAMS.tVf,
    tFe: Number.isFinite(tFe) ? tFe : DEFAULT_OMNI_PARAMS.tFe,
  });
}

export function applyOperator(mesh: Mesh, operator: string | OperatorSpec): Mesh {
  if (typeof operator !== 'string') {
    return applyOmni(
      mesh,
      resolveOperatorNotation(operator.notation),
      operator.tVe,
      operator.tVf,
      operator.tFe
    );
  }

  const presetNotation = OPERATOR_ALIASES[operator] ?? OMNI_PRESETS[operator];
  if (presetNotation) {
    return applyOmni(mesh, presetNotation);
  }

  if (operator.includes('-')) {
    return applyOmni(mesh, operator);
  }

  throw new Error(`Unknown operator '${operator}'`);
}

export function dual(mesh: Mesh): Mesh {
  return applyOmni(mesh, OMNI_PRESETS.Dual);
}

export function kis(mesh: Mesh): Mesh {
  return applyOmni(mesh, OMNI_PRESETS.Kis);
}

export function ambo(mesh: Mesh): Mesh {
  return applyOmni(mesh, OMNI_PRESETS.Ambo);
}

export function truncate(mesh: Mesh): Mesh {
  return applyOmni(mesh, OMNI_PRESETS.Truncate);
}

export function join(mesh: Mesh): Mesh {
  return applyOmni(mesh, OMNI_PRESETS.Join);
}

export function ortho(mesh: Mesh): Mesh {
  return applyOmni(mesh, OMNI_PRESETS.Ortho);
}
