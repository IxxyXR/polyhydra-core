import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence, Reorder } from 'motion/react';
import {
  Settings, 
  Layers, 
  Grid2X2, 
  Maximize, 
  Info, 
  ChevronRight,
  Hexagon,
  Square,
  Triangle as TriangleIcon,
  Circle,
  Eye,
  EyeOff,
  X,
  Pencil,
  List,
  Shuffle,
  ArrowLeft,
  ArrowRight,
  GripVertical,
  Download,
  Plus
} from 'lucide-react';
import { TilingCanvas } from './components/TilingCanvas';
import {
  MULTIGRID_DEFAULTS,
  MultiGridSettings,
  TilingGenerationOptions,
  UNIFORM_TILINGS,
} from './lib/tiling-geometries';
import { PALETTES, PaletteKey } from './lib/palettes';
import { exportObj, exportOff, exportSvg } from './lib/export';
import { ColorMode } from './lib/coloring';
import { createOmniOperatorDiagramSvg } from './lib/omni-diagram';
import {
  createOperatorSpec,
  DEFAULT_OMNI_PARAMS,
  OMNI_POINT_CLASSES,
  OMNI_PRESETS,
  OMNI_VALID_OPERATORS,
  findOmniAtom,
  findPresetName,
  getOmniParamVisibility,
  getUnknownAtoms,
  isCompatibleSubset,
  isCompleteOperator,
  isValidSubset,
  joinAtomList,
  orderAtoms,
  parseOperatorSpec,
  parseAtomList,
  resolveOperatorNotation,
  serializeOperatorSpec,
  OperatorSpec,
} from './lib/conway-operators';

interface OperatorState extends OperatorSpec {
  id: string;
  enabled: boolean;
}

const NO_PRESET_VALUE = '';
const CUSTOM_PRESET_VALUE = '__custom__';

function createOperator(notation: string, enabled = true, overrides: Partial<OperatorSpec> = {}): OperatorState {
  return {
    id: Math.random().toString(36).substring(7) + Date.now(),
    enabled,
    ...createOperatorSpec(notation, overrides),
  };
}

export default function App() {
  const [tilingType, setTilingType] = useState('4.4.4.4');
  const [rows, setRows] = useState(5);
  const [cols, setCols] = useState(5);
  const [showEdges, setShowEdges] = useState(true);
  const [showVertices, setShowVertices] = useState(false);
  const [showFaces, setShowFaces] = useState(true);
  const [wireframe, setWireframe] = useState(false);
  const [operators, setOperators] = useState<OperatorState[]>([]);
  const [palette, setPalette] = useState<PaletteKey>('vibrant');
  const [colorMode, setColorMode] = useState<ColorMode>('role');
  const [edgeColor, setEdgeColor] = useState('#3b82f6');
  const [multigridSettings, setMultigridSettings] = useState<MultiGridSettings>(MULTIGRID_DEFAULTS);
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [tilingMenuOpen, setTilingMenuOpen] = useState(false);
  const [displayMenuOpen, setDisplayMenuOpen] = useState(false);
  const [paletteMenuOpen, setPaletteMenuOpen] = useState(false);
  const [selectedOperatorId, setSelectedOperatorId] = useState<string | null>(null);
  const [rawEditorOpen, setRawEditorOpen] = useState(false);
  const [presetPickerOpen, setPresetPickerOpen] = useState(false);
  const [hoveredGridAtom, setHoveredGridAtom] = useState<string | null>(null);

  // Sync state with URL
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    
    const urlTiling = params.get('tiling');
    if (urlTiling && UNIFORM_TILINGS[urlTiling]) setTilingType(urlTiling);

    const urlRows = params.get('rows');
    const urlCols = params.get('cols');
    if (urlRows && urlCols) {
      setRows(parseInt(urlRows, 10));
      setCols(parseInt(urlCols, 10));
    } else if (urlRows) {
      const size = parseInt(urlRows, 10);
      setRows(size);
      setCols(size);
    } else if (urlCols) {
      const size = parseInt(urlCols, 10);
      setRows(size);
      setCols(size);
    }

    if (params.get('edges') === 'false') setShowEdges(false);
    if (params.get('vertices') === 'true') setShowVertices(true);
    if (params.get('faces') === 'false') setShowFaces(false);
    if (params.get('wireframe') === 'true') setWireframe(true);

    const urlPalette = params.get('palette');
    if (urlPalette && PALETTES[urlPalette as PaletteKey]) setPalette(urlPalette as PaletteKey);

    const urlColorMode = params.get('colorMode');
    if (urlColorMode === 'role' || urlColorMode === 'sides' || urlColorMode === 'value') {
      setColorMode(urlColorMode);
    }
    const urlEdgeColor = params.get('edgeColor');
    if (urlEdgeColor) {
      setEdgeColor(urlEdgeColor);
    }

    const parseIntParam = (value: string | null, fallback: number) => {
      const parsed = Number.parseInt(value ?? '', 10);
      return Number.isFinite(parsed) ? parsed : fallback;
    };
    const parseFloatParam = (value: string | null, fallback: number) => {
      const parsed = Number.parseFloat(value ?? '');
      return Number.isFinite(parsed) ? parsed : fallback;
    };

    setMultigridSettings({
      dimensions: parseIntParam(params.get('mgDim'), MULTIGRID_DEFAULTS.dimensions),
      divisions: parseIntParam(params.get('mgDiv'), MULTIGRID_DEFAULTS.divisions),
      offset: parseFloatParam(params.get('mgOff'), MULTIGRID_DEFAULTS.offset),
      randomize: params.get('mgRand') === 'true',
      sharedVertices: params.get('mgShared') === null
        ? MULTIGRID_DEFAULTS.sharedVertices
        : params.get('mgShared') === 'true',
      minDistance: parseFloatParam(params.get('mgMin'), MULTIGRID_DEFAULTS.minDistance),
      maxDistance: parseFloatParam(params.get('mgMax'), MULTIGRID_DEFAULTS.maxDistance),
      colorRatio: parseFloatParam(params.get('mgRatio'), MULTIGRID_DEFAULTS.colorRatio),
      colorIntersect: parseFloatParam(params.get('mgIntersect'), MULTIGRID_DEFAULTS.colorIntersect),
      colorIndex: parseFloatParam(params.get('mgIndex'), MULTIGRID_DEFAULTS.colorIndex),
      randomSeed: parseIntParam(params.get('mgSeed'), MULTIGRID_DEFAULTS.randomSeed),
    });

    const urlOps = params.get('ops');
    if (urlOps) {
      const entries = urlOps.includes(';')
        ? urlOps.split(';').filter(Boolean)
        : urlOps.split(',').filter(Boolean);

      const loadedOperators = entries.map((op) => {
        const decoded = decodeURIComponent(op);
        const isEnabled = !decoded.startsWith('!');
        const serialized = isEnabled ? decoded : decoded.substring(1);
        const spec = parseOperatorSpec(serialized);
        return createOperator(spec.notation, isEnabled, spec);
      });
      setOperators(loadedOperators);
      setSelectedOperatorId(loadedOperators[0]?.id ?? null);
    }
  }, []);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set('tiling', tilingType);
    params.set('rows', rows.toString());
    params.set('cols', cols.toString());
    params.set('edges', showEdges.toString());
    params.set('vertices', showVertices.toString());
    params.set('faces', showFaces.toString());
    params.set('wireframe', wireframe.toString());
    params.set('palette', palette);
    params.set('colorMode', colorMode);
    params.set('edgeColor', edgeColor);
    params.set('mgDim', multigridSettings.dimensions.toString());
    params.set('mgDiv', multigridSettings.divisions.toString());
    params.set('mgOff', multigridSettings.offset.toString());
    params.set('mgRand', multigridSettings.randomize.toString());
    params.set('mgShared', multigridSettings.sharedVertices.toString());
    params.set('mgMin', multigridSettings.minDistance.toString());
    params.set('mgMax', multigridSettings.maxDistance.toString());
    params.set('mgRatio', multigridSettings.colorRatio.toString());
    params.set('mgIntersect', multigridSettings.colorIntersect.toString());
    params.set('mgIndex', multigridSettings.colorIndex.toString());
    params.set('mgSeed', multigridSettings.randomSeed.toString());
    if (operators.length > 0) {
      params.set('ops', operators.map((o) => {
        const serialized = serializeOperatorSpec(o);
        return encodeURIComponent((o.enabled ? '' : '!') + serialized);
      }).join(';'));
    }

    const newRelativePathQuery = window.location.pathname + '?' + params.toString();
    window.history.replaceState(null, '', newRelativePathQuery);
  }, [tilingType, rows, cols, showEdges, showVertices, showFaces, wireframe, operators, palette, colorMode, edgeColor, multigridSettings]);

  const addOperator = (notation: string, overrides: Partial<OperatorSpec> = {}) => {
    if (!notation.trim()) return;
    const nextOperator = createOperator(notation.trim(), true, overrides);
    setOperators((current) => [...current, nextOperator]);
    setSelectedOperatorId(nextOperator.id);
  };

  const addBlankOperator = () => {
    const nextOperator = createOperator('', true);
    setOperators((current) => [...current, nextOperator]);
    setSelectedOperatorId(nextOperator.id);
  };

  const removeOperator = (id: string) => {
    setOperators((current) => {
      const index = current.findIndex((op) => op.id === id);
      const remaining = current.filter((op) => op.id !== id);
      if (selectedOperatorId === id) {
        const fallback = remaining[Math.min(index, remaining.length - 1)] ?? remaining[index - 1] ?? null;
        setSelectedOperatorId(fallback?.id ?? null);
      }
      return remaining;
    });
  };

  const randomizeSelectedOperator = () => {
    if (!selectedOperatorId) return;
    const randomIndex = Math.floor(Math.random() * OMNI_VALID_OPERATORS.length);
    const randomAtoms = orderAtoms(OMNI_VALID_OPERATORS[randomIndex]);
    const notation = joinAtomList(randomAtoms);
    setOperators((current) => current.map((op) =>
      op.id === selectedOperatorId ? { ...op, notation } : op
    ));
  };

  const selectedOperator = operators.find((op) => op.id === selectedOperatorId) ?? null;
  const selectedOperatorNotation = selectedOperator ? resolveOperatorNotation(selectedOperator.notation) : '';
  const selectedAtoms = parseAtomList(selectedOperatorNotation);
  const orderedSelectedAtoms = orderAtoms(selectedAtoms);
  const uniqueSelectedAtoms = Array.from(new Set(selectedAtoms));
  const unknownSelectedAtoms = getUnknownAtoms(uniqueSelectedAtoms);
  const selectedOperatorIsComplete = unknownSelectedAtoms.length === 0 && isCompleteOperator(uniqueSelectedAtoms);
  const selectedOperatorIsValid = unknownSelectedAtoms.length === 0 && isValidSubset(uniqueSelectedAtoms);
  const selectedMatchingPresetName = unknownSelectedAtoms.length === 0 ? findPresetName(uniqueSelectedAtoms) : null;
  const selectedPresetValue = !selectedOperatorNotation.trim()
    ? NO_PRESET_VALUE
    : (selectedMatchingPresetName ?? CUSTOM_PRESET_VALUE);
  const selectedOperatorDiagramSvg = createOmniOperatorDiagramSvg(selectedOperatorNotation);
  const activeOperators = operators.filter((op) => {
    if (!op.enabled) return false;
    const atoms = Array.from(new Set(parseAtomList(resolveOperatorNotation(op.notation))));
    return isCompleteOperator(atoms);
  });

  const updateSelectedOperatorNotation = (notation: string) => {
    if (!selectedOperatorId) return;
    setOperators((current) => current.map((op) =>
      op.id === selectedOperatorId ? { ...op, notation } : op
    ));
  };

  const selectOperator = (id: string) => {
    setSelectedOperatorId(id);
    setRawEditorOpen(false);
    setPresetPickerOpen(false);
  };

  const toggleGridAtom = (atom: string) => {
    if (!selectedOperatorId) return;

    const nextAtoms = new Set(uniqueSelectedAtoms);
    if (nextAtoms.has(atom)) {
      nextAtoms.delete(atom);
    } else if (isCompatibleSubset([...nextAtoms], atom)) {
      nextAtoms.add(atom);
    } else {
      return;
    }

    updateSelectedOperatorNotation(joinAtomList(orderAtoms(nextAtoms)));
  };

  const toggleOperator = (id: string) => {
    setOperators(operators.map(op =>
      op.id === id ? { ...op, enabled: !op.enabled } : op
    ));
  };

  const updateOperatorParams = (id: string, field: keyof Pick<OperatorSpec, 'tVe' | 'tVf' | 'tFe'>, value: string) => {
    const parsed = Number.parseFloat(value);
    setOperators(operators.map((op) =>
      op.id === id
        ? { ...op, [field]: Number.isFinite(parsed) ? parsed : DEFAULT_OMNI_PARAMS[field] }
        : op
    ));
  };

  const updateMultigridSetting = <K extends keyof MultiGridSettings>(field: K, value: MultiGridSettings[K]) => {
    setMultigridSettings((current) => {
      if (field === 'minDistance') {
        const minDistance = value as number;
        return {
          ...current,
          minDistance,
          maxDistance: Math.max(current.maxDistance, minDistance),
        };
      }

      if (field === 'maxDistance') {
        const maxDistance = value as number;
        return {
          ...current,
          maxDistance,
          minDistance: Math.min(current.minDistance, maxDistance),
        };
      }

      return { ...current, [field]: value };
    });
  };

  const selectedTiling = UNIFORM_TILINGS[tilingType];
  const selectedPalette = PALETTES[palette];
  const isMultigrid = tilingType === 'multigrid';
  const generationOptions: TilingGenerationOptions = {
    multigrid: multigridSettings,
  };

  return (
    <div id="app-root" className="flex h-screen bg-neutral-950 text-neutral-100 font-sans overflow-hidden">
      {/* Sidebar */}
      <motion.aside
        initial={false}
        animate={{ width: sidebarOpen ? 360 : 0, opacity: sidebarOpen ? 1 : 0 }}
        className="relative h-full bg-neutral-900/50 backdrop-blur-xl border-r border-neutral-800 flex flex-col z-20"
      >
        <div className="p-6 overflow-y-auto flex-1">
          <div className="flex items-center gap-3 mb-8">
            <div className="p-2 bg-blue-600 rounded-lg">
              <Layers className="w-6 h-6 text-white" />
            </div>
            <div>
              <h1 className="font-bold text-xl tracking-tight text-white">Tiling Explorer</h1>
              <p className="text-xs text-neutral-400 font-mono uppercase tracking-widest">Three.js Powered</p>
            </div>
          </div>

          <div className="space-y-6">
            <section>
              <h2 className="text-xs font-semibold text-neutral-500 uppercase tracking-wider mb-4 flex items-center gap-2">
                <Grid2X2 className="w-3 h-3" />
                Uniform Tilings
              </h2>
              <div className="rounded-2xl border border-neutral-800 bg-neutral-800/20 overflow-hidden">
                <button
                  onClick={() => setTilingMenuOpen(!tilingMenuOpen)}
                  className="w-full p-4 text-left transition-colors hover:bg-neutral-800/40"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <div className="text-[10px] text-neutral-500 uppercase tracking-widest font-semibold mb-2">
                        Active Tiling
                      </div>
                      <div className="text-sm font-semibold text-white truncate">{selectedTiling?.name}</div>
                    </div>
                    <div className="mt-1 flex h-8 w-8 items-center justify-center rounded-full border border-neutral-700 bg-neutral-900/70 text-neutral-400">
                      <ChevronRight className={`w-4 h-4 transition-transform ${tilingMenuOpen ? 'rotate-90 text-white' : ''}`} />
                    </div>
                  </div>
                </button>

                <AnimatePresence initial={false}>
                  {tilingMenuOpen && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: 'auto', opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      className="border-t border-neutral-800"
                    >
                      <div className="max-h-72 overflow-y-auto p-2 space-y-1">
                        {Object.entries(UNIFORM_TILINGS).map(([key, tiling]) => (
                          <button
                            key={key}
                            onClick={() => {
                              setTilingType(key);
                              if (key === 'multigrid') {
                                setColorMode((current) => current === 'role' ? 'value' : current);
                              }
                              setTilingMenuOpen(false);
                            }}
                            className={`w-full rounded-xl p-3 text-left transition-all ${
                              tilingType === key
                                ? 'bg-blue-600 text-white shadow-lg shadow-blue-900/20'
                                : 'bg-neutral-900/40 text-neutral-300 hover:bg-neutral-800'
                            }`}
                          >
                            <div className="flex items-center justify-between gap-3">
                              <div className="min-w-0">
                                <div className="font-medium text-sm truncate">{tiling.name}</div>
                              </div>
                              {tilingType === key && <ChevronRight className="w-4 h-4 shrink-0" />}
                            </div>
                          </button>
                        ))}
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>
              </div>
            </section>

            <section>
              <h2 className="text-xs font-semibold text-neutral-500 uppercase tracking-wider mb-4 flex items-center gap-2">
                <Settings className="w-3 h-3" />
                Settings
              </h2>
              <div className="space-y-4 bg-neutral-800/20 p-4 rounded-2xl border border-neutral-800">
                {isMultigrid ? (
                  <>
                    <div className="space-y-2">
                      <div className="flex justify-between text-xs">
                        <span className="text-neutral-400">Dimensions</span>
                        <span className="text-blue-400 font-mono">{multigridSettings.dimensions}</span>
                      </div>
                      <input
                        type="range"
                        min="3"
                        max="30"
                        value={multigridSettings.dimensions}
                        onChange={(e) => updateMultigridSetting('dimensions', parseInt(e.target.value, 10))}
                        className="w-full accent-blue-600 h-1.5 bg-neutral-700 rounded-lg appearance-none cursor-pointer"
                      />
                    </div>
                    <div className="space-y-2">
                      <div className="flex justify-between text-xs">
                        <span className="text-neutral-400">Divisions</span>
                        <span className="text-blue-400 font-mono">{multigridSettings.divisions}</span>
                      </div>
                      <input
                        type="range"
                        min="1"
                        max="30"
                        value={multigridSettings.divisions}
                        onChange={(e) => updateMultigridSetting('divisions', parseInt(e.target.value, 10))}
                        className="w-full accent-blue-600 h-1.5 bg-neutral-700 rounded-lg appearance-none cursor-pointer"
                      />
                    </div>
                    <div className="space-y-2">
                      <div className="flex justify-between text-xs">
                        <span className="text-neutral-400">Offset</span>
                        <span className="text-blue-400 font-mono">{multigridSettings.offset.toFixed(2)}</span>
                      </div>
                      <input
                        type="range"
                        min="-2"
                        max="2"
                        step="0.01"
                        value={multigridSettings.offset}
                        onChange={(e) => updateMultigridSetting('offset', Number.parseFloat(e.target.value))}
                        className="w-full accent-blue-600 h-1.5 bg-neutral-700 rounded-lg appearance-none cursor-pointer"
                      />
                    </div>
                    <div className="rounded-xl border border-neutral-800 bg-neutral-900/40 p-3 space-y-3">
                      <div className="text-[10px] text-neutral-500 uppercase tracking-widest font-semibold">Cropping</div>
                      <div className="space-y-2">
                        <div className="flex justify-between text-xs">
                          <span className="text-neutral-400">Min Distance</span>
                          <span className="text-blue-400 font-mono">{multigridSettings.minDistance.toFixed(2)}</span>
                        </div>
                        <input
                          type="range"
                          min="0"
                          max="2"
                          step="0.01"
                          value={multigridSettings.minDistance}
                          onChange={(e) => updateMultigridSetting('minDistance', Number.parseFloat(e.target.value))}
                          className="w-full accent-blue-600 h-1.5 bg-neutral-700 rounded-lg appearance-none cursor-pointer"
                        />
                      </div>
                      <div className="space-y-2">
                        <div className="flex justify-between text-xs">
                          <span className="text-neutral-400">Max Distance</span>
                          <span className="text-blue-400 font-mono">{multigridSettings.maxDistance.toFixed(2)}</span>
                        </div>
                        <input
                          type="range"
                          min="0"
                          max="2"
                          step="0.01"
                          value={multigridSettings.maxDistance}
                          onChange={(e) => updateMultigridSetting('maxDistance', Number.parseFloat(e.target.value))}
                          className="w-full accent-blue-600 h-1.5 bg-neutral-700 rounded-lg appearance-none cursor-pointer"
                        />
                      </div>
                    </div>
                  </>
                ) : (
                  <>
                    <div className="space-y-2">
                      <div className="flex justify-between text-xs">
                        <span className="text-neutral-400">Rows</span>
                        <span className="text-blue-400 font-mono">{rows}</span>
                      </div>
                      <input 
                        type="range" min="2" max="30" value={rows} 
                        onChange={(e) => setRows(parseInt(e.target.value))}
                        className="w-full accent-blue-600 h-1.5 bg-neutral-700 rounded-lg appearance-none cursor-pointer"
                      />
                    </div>
                    <div className="space-y-2">
                      <div className="flex justify-between text-xs">
                        <span className="text-neutral-400">Columns</span>
                        <span className="text-blue-400 font-mono">{cols}</span>
                      </div>
                      <input 
                        type="range" min="2" max="30" value={cols} 
                        onChange={(e) => setCols(parseInt(e.target.value))}
                        className="w-full accent-blue-600 h-1.5 bg-neutral-700 rounded-lg appearance-none cursor-pointer"
                      />
                    </div>
                  </>
                )}
              </div>
            </section>

            <section>
              <div className="rounded-2xl border border-neutral-800 bg-neutral-800/20 overflow-hidden">
                <button
                  onClick={() => setDisplayMenuOpen(!displayMenuOpen)}
                  className="w-full p-3 text-left transition-colors hover:bg-neutral-800/40"
                >
                  <div className="flex items-center justify-between gap-3">
                    <div className="min-w-0">
                      <div className="mb-2 flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-neutral-500">
                        <Eye className="w-3 h-3" />
                        Appearance
                      </div>
                      <div className="flex items-center gap-2">
                        <div className="flex -space-x-1">
                          {selectedPalette.colors.slice(0, 5).map((c, i) => (
                            <div
                              key={i}
                              className="h-3 w-3 rounded-full border border-neutral-900"
                              style={{ backgroundColor: c }}
                            />
                          ))}
                        </div>
                        <div
                          className="h-2 w-6 rounded-full border border-neutral-700"
                          style={{ backgroundColor: edgeColor }}
                          title="Edge colour"
                        />
                      </div>
                    </div>
                    <div className="flex h-8 w-8 items-center justify-center rounded-full border border-neutral-700 bg-neutral-900/70 text-neutral-400">
                      <ChevronRight className={`w-4 h-4 transition-transform ${displayMenuOpen ? 'rotate-90 text-white' : ''}`} />
                    </div>
                  </div>
                </button>

                <AnimatePresence initial={false}>
                  {displayMenuOpen && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: 'auto', opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      className="border-t border-neutral-800"
                    >
                      <div className="p-3 space-y-3">
                        <div className="space-y-3">
                          <div className="rounded-xl border border-neutral-800 bg-neutral-900/40 overflow-hidden">
                            <button
                              onClick={() => setPaletteMenuOpen(!paletteMenuOpen)}
                              className="w-full p-3 text-left transition-colors hover:bg-neutral-800/40"
                            >
                              <div className="flex items-center justify-between gap-3">
                                <div className="min-w-0">
                                  <div className="text-[10px] text-neutral-500 uppercase tracking-widest font-semibold mb-1">
                                    Face Palette
                                  </div>
                                  <div className="text-xs font-semibold text-white truncate">{selectedPalette.name}</div>
                                  <div className="flex -space-x-1 mt-2">
                                    {selectedPalette.colors.slice(0, 5).map((c, i) => (
                                      <div
                                        key={i}
                                        className="w-3 h-3 rounded-full border border-neutral-900"
                                        style={{ backgroundColor: c }}
                                      />
                                    ))}
                                  </div>
                                </div>
                                <div className="flex h-8 w-8 items-center justify-center rounded-full border border-neutral-700 bg-neutral-900/70 text-neutral-400">
                                  <ChevronRight className={`w-4 h-4 transition-transform ${paletteMenuOpen ? 'rotate-90 text-white' : ''}`} />
                                </div>
                              </div>
                            </button>

                            <AnimatePresence initial={false}>
                              {paletteMenuOpen && (
                                <motion.div
                                  initial={{ height: 0, opacity: 0 }}
                                  animate={{ height: 'auto', opacity: 1 }}
                                  exit={{ height: 0, opacity: 0 }}
                                  className="border-t border-neutral-800"
                                >
                                  <div className="p-2 grid grid-cols-2 gap-2">
                                    {Object.entries(PALETTES).map(([key, p]) => (
                                      <button
                                        key={key}
                                        onClick={() => {
                                          setPalette(key as PaletteKey);
                                          setPaletteMenuOpen(false);
                                        }}
                                        className={`flex items-center gap-2 p-2 rounded-lg text-[10px] font-medium transition-all border ${
                                          palette === key
                                            ? 'bg-neutral-800 border-neutral-700 text-white'
                                            : 'bg-neutral-900/40 border-neutral-800/50 text-neutral-500 hover:bg-neutral-800/60'
                                        }`}
                                      >
                                        <div className="flex -space-x-1">
                                          {p.colors.slice(0, 3).map((c, i) => (
                                            <div key={i} className="w-2 h-2 rounded-full border border-neutral-900" style={{ backgroundColor: c }} />
                                          ))}
                                        </div>
                                        {p.name}
                                      </button>
                                    ))}
                                  </div>
                                </motion.div>
                              )}
                            </AnimatePresence>
                          </div>
                          <div className="grid grid-cols-2 gap-2">
                            <button
                              onClick={() => setColorMode('role')}
                              className={`rounded-lg border px-3 py-2 text-[10px] font-semibold uppercase tracking-widest transition-colors ${
                                colorMode === 'role'
                                  ? 'border-blue-700/60 bg-blue-950/20 text-blue-300'
                                  : 'border-neutral-800 bg-neutral-900/40 text-neutral-500 hover:bg-neutral-800/60'
                              }`}
                            >
                              By Role
                            </button>
                            <button
                              onClick={() => setColorMode('sides')}
                              className={`rounded-lg border px-3 py-2 text-[10px] font-semibold uppercase tracking-widest transition-colors ${
                                colorMode === 'sides'
                                  ? 'border-blue-700/60 bg-blue-950/20 text-blue-300'
                                  : 'border-neutral-800 bg-neutral-900/40 text-neutral-500 hover:bg-neutral-800/60'
                              }`}
                            >
                              By Sides
                            </button>
                          </div>
                          <div className="flex items-center justify-between gap-3 rounded-lg border border-neutral-800 bg-neutral-900/40 px-3 py-2">
                            <span className="text-[10px] font-semibold uppercase tracking-widest text-neutral-500">Edge Colour</span>
                            <label className="flex items-center gap-2">
                              <span className="font-mono text-[10px] text-neutral-400">{edgeColor}</span>
                              <input
                                type="color"
                                value={edgeColor}
                                onChange={(e) => setEdgeColor(e.target.value)}
                                className="h-8 w-10 cursor-pointer rounded border border-neutral-700 bg-transparent p-0"
                              />
                            </label>
                          </div>
                          <div className="pt-3 border-t border-neutral-800 space-y-3">
                            <label className="flex items-center justify-between cursor-pointer group">
                              <span className="text-sm text-neutral-300 group-hover:text-white transition-colors">Show Faces</span>
                              <input type="checkbox" checked={showFaces} onChange={(e) => setShowFaces(e.target.checked)} className="w-4 h-4 rounded border-neutral-700 text-blue-600 bg-neutral-800 focus:ring-blue-600" />
                            </label>
                            <label className="flex items-center justify-between cursor-pointer group">
                              <span className="text-sm text-neutral-300 group-hover:text-white transition-colors">Show Edges</span>
                              <input type="checkbox" checked={showEdges} onChange={(e) => setShowEdges(e.target.checked)} className="w-4 h-4 rounded border-neutral-700 text-blue-600 bg-neutral-800 focus:ring-blue-600" />
                            </label>
                            <label className="flex items-center justify-between cursor-pointer group">
                              <span className="text-sm text-neutral-300 group-hover:text-white transition-colors">Show Vertices</span>
                              <input type="checkbox" checked={showVertices} onChange={(e) => setShowVertices(e.target.checked)} className="w-4 h-4 rounded border-neutral-700 text-blue-600 bg-neutral-800 focus:ring-blue-600" />
                            </label>
                            <label className="flex items-center justify-between cursor-pointer group">
                              <span className="text-sm text-neutral-300 group-hover:text-white transition-colors">Wireframe</span>
                              <input type="checkbox" checked={wireframe} onChange={(e) => setWireframe(e.target.checked)} className="w-4 h-4 rounded border-neutral-700 text-blue-600 bg-neutral-800 focus:ring-blue-600" />
                            </label>
                          </div>
                        </div>
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>
              </div>
            </section>

            <section>
              <h2 className="text-xs font-semibold text-neutral-500 uppercase tracking-wider mb-4 flex items-center gap-2">
                <Layers className="w-3 h-3" />
                Operators
              </h2>
              <div className="space-y-3 bg-neutral-800/20 p-4 rounded-2xl border border-neutral-800">
                <div className="flex items-center justify-between mb-3">
                  <h3 className="text-[10px] font-semibold text-neutral-500 uppercase tracking-widest">Omni Operators</h3>
                  <div className="flex items-center gap-2">
                        <button
                          onClick={addBlankOperator}
                          className="flex items-center gap-1 text-[10px] text-blue-400 hover:text-blue-300 transition-colors uppercase font-bold"
                        >
                          <Plus className="w-3 h-3" />
                          Add Operator
                        </button>
                        {operators.length > 0 && (
                          <button
                            onClick={() => {
                              setOperators([]);
                              setSelectedOperatorId(null);
                            }}
                            className="text-[10px] text-blue-400 hover:text-blue-300 transition-colors uppercase font-bold"
                          >
                            Clear Stack
                          </button>
                        )}
                  </div>
                </div>

                {operators.length > 0 && (
                  <Reorder.Group
                    axis="y"
                    values={operators}
                    onReorder={(nextOperators) => {
                      setOperators(nextOperators);
                      if (selectedOperatorId && !nextOperators.some((op) => op.id === selectedOperatorId)) {
                        setSelectedOperatorId(nextOperators[0]?.id ?? null);
                      }
                    }}
                    className="space-y-1 mb-4"
                  >
                        {operators.map((op, idx) => (
                          <Reorder.Item
                            key={op.id}
                            value={op}
                            onClick={() => selectOperator(op.id)}
                            className={`rounded-lg border px-3 py-2 text-xs transition-colors hover:border-neutral-700 cursor-grab active:cursor-grabbing ${selectedOperatorId === op.id ? 'border-blue-700/60 bg-blue-950/20' : 'border-neutral-800/50 bg-neutral-900/50'} ${!op.enabled ? 'opacity-50' : ''}`}
                          >
                            <div className="flex items-center justify-between gap-2">
                              <div className="flex min-w-0 items-center gap-2">
                                <GripVertical className="w-3 h-3 shrink-0 text-neutral-600" />
                                <span className="w-4 shrink-0 font-mono text-[10px] text-neutral-500">{idx + 1}.</span>
                                <div className="min-w-0">
                                  <div className={`${op.enabled ? 'text-blue-400' : 'text-neutral-500'} truncate font-mono font-bold`}>
                                    {findPresetName(parseAtomList(resolveOperatorNotation(op.notation))) ?? (resolveOperatorNotation(op.notation) || 'New Operator')}
                                  </div>
                                  <div className="truncate font-mono text-[10px] text-neutral-500">
                                    {resolveOperatorNotation(op.notation) || 'No atoms'}
                                  </div>
                                </div>
                              </div>
                              <div className="flex shrink-0 items-center gap-1">
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    setSelectedOperatorId(op.id);
                                    setRawEditorOpen((current) => selectedOperatorId === op.id ? !current : true);
                                    setPresetPickerOpen(false);
                                  }}
                                  className="rounded p-1 text-neutral-600 transition-colors hover:bg-neutral-800 hover:text-neutral-300"
                                  title="Edit raw atoms"
                                >
                                  <Pencil className="w-3 h-3" />
                                </button>
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    setSelectedOperatorId(op.id);
                                    setPresetPickerOpen((current) => selectedOperatorId === op.id ? !current : true);
                                    setRawEditorOpen(false);
                                  }}
                                  className="rounded p-1 text-neutral-600 transition-colors hover:bg-neutral-800 hover:text-neutral-300"
                                  title="Choose preset"
                                >
                                  <List className="w-3 h-3" />
                                </button>
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    setSelectedOperatorId(op.id);
                                    const randomIndex = Math.floor(Math.random() * OMNI_VALID_OPERATORS.length);
                                    const randomAtoms = orderAtoms(OMNI_VALID_OPERATORS[randomIndex]);
                                    const notation = joinAtomList(randomAtoms);
                                    setOperators((current) => current.map((item) =>
                                      item.id === op.id ? { ...item, notation } : item
                                    ));
                                    setRawEditorOpen(false);
                                    setPresetPickerOpen(false);
                                  }}
                                  className="rounded p-1 text-neutral-600 transition-colors hover:bg-neutral-800 hover:text-neutral-300"
                                  title="Random operator"
                                >
                                  <Shuffle className="w-3 h-3" />
                                </button>
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    toggleOperator(op.id);
                                  }}
                                  className="rounded p-1 text-neutral-600 transition-colors hover:bg-neutral-800 hover:text-neutral-300"
                                  title={op.enabled ? 'Disable' : 'Enable'}
                                >
                                  {op.enabled ? <Eye className="w-3 h-3" /> : <EyeOff className="w-3 h-3" />}
                                </button>
                                <button
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    removeOperator(op.id);
                                  }}
                                  className="rounded p-1 text-neutral-600 transition-colors hover:bg-red-900/20 hover:text-red-400"
                                >
                                  <X className="w-3 h-3" />
                                </button>
                              </div>
                            </div>
                            {(() => {
                              const visibility = getOmniParamVisibility(op.notation);
                              const isSelectedOperator = selectedOperatorId === op.id;
                              if (!visibility.showP1 && !visibility.showP2 && !visibility.showP3 && !isSelectedOperator) {
                                return null;
                              }

                              return (
                                <div
                                  className="mt-3 grid gap-2"
                                  onPointerDown={(e) => e.stopPropagation()}
                                  onClick={(e) => e.stopPropagation()}
                                >
                                  {visibility.showP1 && (
                                    <label className="grid gap-1">
                                      <div className="flex items-center justify-between gap-2">
                                        <span className="text-[10px] font-semibold uppercase tracking-widest text-neutral-500">
                                          ve
                                        </span>
                                        <span className="font-mono text-[10px] text-neutral-400">
                                          {op.tVe.toFixed(2)}
                                        </span>
                                      </div>
                                      <input
                                        type="range"
                                        min="0"
                                        max="1"
                                        step="0.01"
                                        value={op.tVe}
                                        onChange={(e) => updateOperatorParams(op.id, 'tVe', e.target.value)}
                                        className="w-full accent-blue-600 h-1.5 cursor-pointer appearance-none rounded-lg bg-neutral-700"
                                      />
                                    </label>
                                  )}
                                  {visibility.showP2 && (
                                    <label className="grid gap-1">
                                      <div className="flex items-center justify-between gap-2">
                                        <span className="text-[10px] font-semibold uppercase tracking-widest text-neutral-500">
                                          vf
                                        </span>
                                        <span className="font-mono text-[10px] text-neutral-400">
                                          {op.tVf.toFixed(2)}
                                        </span>
                                      </div>
                                      <input
                                        type="range"
                                        min="0"
                                        max="1"
                                        step="0.01"
                                        value={op.tVf}
                                        onChange={(e) => updateOperatorParams(op.id, 'tVf', e.target.value)}
                                        className="w-full accent-blue-600 h-1.5 cursor-pointer appearance-none rounded-lg bg-neutral-700"
                                      />
                                    </label>
                                  )}
                                  {visibility.showP3 && (
                                    <label className="grid gap-1">
                                      <div className="flex items-center justify-between gap-2">
                                        <span className="text-[10px] font-semibold uppercase tracking-widest text-neutral-500">
                                          fe
                                        </span>
                                        <span className="font-mono text-[10px] text-neutral-400">
                                          {op.tFe.toFixed(2)}
                                        </span>
                                      </div>
                                      <input
                                        type="range"
                                        min="0"
                                        max="1"
                                        step="0.01"
                                        value={op.tFe}
                                        onChange={(e) => updateOperatorParams(op.id, 'tFe', e.target.value)}
                                        className="w-full accent-blue-600 h-1.5 cursor-pointer appearance-none rounded-lg bg-neutral-700"
                                      />
                                    </label>
                                  )}
                                  {isSelectedOperator && (
                                    <>
                                      <AnimatePresence initial={false}>
                                        {rawEditorOpen && (
                                          <motion.div
                                            initial={{ height: 0, opacity: 0 }}
                                            animate={{ height: 'auto', opacity: 1 }}
                                            exit={{ height: 0, opacity: 0 }}
                                            className="overflow-hidden rounded-xl border border-neutral-800 bg-neutral-900/40"
                                            onClick={(e) => e.stopPropagation()}
                                          >
                                            <div className="p-3 space-y-2">
                                              <div className="text-[10px] font-semibold uppercase tracking-widest text-neutral-500">
                                                Raw Atom List
                                              </div>
                                              <input
                                                type="text"
                                                value={selectedOperatorNotation}
                                                onChange={(e) => updateSelectedOperatorNotation(e.target.value)}
                                                placeholder="ve-vf,ve1-ve1,vf-vf,vf-vf!"
                                                className="w-full px-3 py-2 rounded-lg text-xs border bg-neutral-800/40 border-neutral-700/50 text-neutral-200 placeholder:text-neutral-500 focus:outline-none focus:border-blue-500 font-mono"
                                              />
                                            </div>
                                          </motion.div>
                                        )}
                                      </AnimatePresence>

                                      <AnimatePresence initial={false}>
                                        {presetPickerOpen && (
                                          <motion.div
                                            initial={{ height: 0, opacity: 0 }}
                                            animate={{ height: 'auto', opacity: 1 }}
                                            exit={{ height: 0, opacity: 0 }}
                                            className="overflow-hidden rounded-xl border border-neutral-800 bg-neutral-900/40"
                                            onClick={(e) => e.stopPropagation()}
                                          >
                                            <div className="p-3 space-y-2">
                                              <div className="text-[10px] font-semibold uppercase tracking-widest text-neutral-500">
                                                Conway Preset
                                              </div>
                                              <select
                                                value={selectedPresetValue}
                                                onPointerDown={(e) => e.stopPropagation()}
                                                onClick={(e) => e.stopPropagation()}
                                                onChange={(e) => {
                                                  e.stopPropagation();
                                                  const presetName = e.target.value;
                                                  if (selectedOperatorId && presetName && presetName !== CUSTOM_PRESET_VALUE) {
                                                    const notation = OMNI_PRESETS[presetName];
                                                    setOperators((current) => current.map((item) =>
                                                      item.id === selectedOperatorId ? { ...item, notation } : item
                                                    ));
                                                  }
                                                }}
                                                className="w-full px-3 py-2 rounded-lg text-xs border bg-neutral-800/40 border-neutral-700/50 text-neutral-200 focus:outline-none focus:border-blue-500"
                                              >
                                                <option value={NO_PRESET_VALUE}>---</option>
                                                <option value={CUSTOM_PRESET_VALUE}>(custom)</option>
                                                {Object.keys(OMNI_PRESETS).map((presetName) => (
                                                  <option key={presetName} value={presetName}>
                                                    {presetName}
                                                  </option>
                                                ))}
                                              </select>
                                              <p className="text-[10px] text-neutral-500 font-mono break-all">
                                                {selectedPresetValue === NO_PRESET_VALUE
                                                  ? 'Select a preset to replace the current operator.'
                                                  : selectedPresetValue === CUSTOM_PRESET_VALUE
                                                    ? '(custom)'
                                                    : OMNI_PRESETS[selectedPresetValue]}
                                              </p>
                                            </div>
                                          </motion.div>
                                        )}
                                      </AnimatePresence>

                                      <div className="rounded-xl border border-neutral-800 bg-neutral-900/40 p-2">
                                        {selectedOperatorDiagramSvg && (
                                          <div className="mb-2 rounded-lg border border-neutral-800 bg-neutral-950/60 p-3">
                                            <div className="mb-2 text-[10px] text-neutral-500 uppercase tracking-widest font-semibold">
                                              Symbol
                                            </div>
                                            <div
                                              className="mx-auto aspect-square w-28 text-white"
                                              dangerouslySetInnerHTML={{ __html: selectedOperatorDiagramSvg }}
                                            />
                                            <div className="mt-3 flex flex-wrap items-center gap-2 text-[10px]">
                                              <span
                                                className={`rounded-full px-2 py-1 font-semibold uppercase tracking-widest ${
                                                  selectedOperatorIsComplete
                                                    ? 'bg-emerald-900/30 text-emerald-300 border border-emerald-800/40'
                                                    : selectedOperatorIsValid
                                                      ? 'bg-amber-900/30 text-amber-300 border border-amber-800/40'
                                                      : 'bg-red-900/30 text-red-300 border border-red-800/40'
                                                }`}
                                              >
                                                {selectedOperatorIsComplete ? 'Complete' : selectedOperatorIsValid ? 'Incomplete' : 'Invalid'}
                                              </span>
                                              {selectedMatchingPresetName && (
                                                <span className="rounded-full border border-blue-800/40 bg-blue-900/20 px-2 py-1 font-semibold text-blue-300 uppercase tracking-widest">
                                                  {selectedMatchingPresetName}
                                                </span>
                                              )}
                                              {orderedSelectedAtoms.length > 0 && (
                                                <span className="font-mono text-neutral-500">
                                                  {orderedSelectedAtoms.length} atoms
                                                </span>
                                              )}
                                            </div>
                                          </div>
                                        )}
                                        <div className="mb-2 flex items-center justify-between gap-2">
                                          <span className="text-[10px] text-neutral-500 uppercase tracking-widest font-semibold">
                                            Atom Grid
                                          </span>
                                          <span className="text-[10px] font-mono text-neutral-500">
                                            {hoveredGridAtom ?? ''}
                                          </span>
                                        </div>
                                        <div
                                          className="grid gap-1 w-full"
                                          style={{
                                            gridTemplateColumns: `repeat(${OMNI_POINT_CLASSES.length + 1}, minmax(0, 1fr))`,
                                          }}
                                        >
                                          <div />
                                          {OMNI_POINT_CLASSES.map((pointClass) => (
                                            <div
                                              key={`col-${pointClass}`}
                                              className="aspect-square flex items-center justify-center text-[10px] font-bold text-neutral-500"
                                            >
                                              {pointClass}
                                            </div>
                                          ))}

                                          {OMNI_POINT_CLASSES.map((rowClass) => (
                                            <React.Fragment key={`row-${rowClass}`}>
                                              <div className="aspect-square flex items-center justify-center text-[10px] font-bold text-neutral-500">
                                                {rowClass}
                                              </div>
                                              {OMNI_POINT_CLASSES.map((colClass) => {
                                                const atom = findOmniAtom(rowClass, colClass);
                                                if (!atom) {
                                                  return (
                                                    <div
                                                      key={`${rowClass}-${colClass}`}
                                                      className="aspect-square rounded-md bg-red-950/30 border border-red-900/20"
                                                    />
                                                  );
                                                }

                                                const isSelected = uniqueSelectedAtoms.includes(atom);
                                                const isCompatible = isSelected || isCompatibleSubset(uniqueSelectedAtoms.filter((selected) => selected !== atom), atom);
                                                const baseClass = isSelected
                                                  ? 'bg-blue-600 border-blue-500 shadow-sm shadow-blue-900/30'
                                                  : isCompatible
                                                    ? 'bg-emerald-900/30 border-emerald-800/40 hover:bg-emerald-800/40'
                                                    : 'bg-red-950/30 border-red-900/20 opacity-60 cursor-not-allowed';

                                                return (
                                                  <button
                                                    key={`${rowClass}-${colClass}`}
                                                    type="button"
                                                    onMouseEnter={() => setHoveredGridAtom(atom)}
                                                    onMouseLeave={() => setHoveredGridAtom((current) => current === atom ? null : current)}
                                                    onClick={() => toggleGridAtom(atom)}
                                                    disabled={!isSelected && !isCompatible}
                                                    className={`aspect-square rounded-md border transition-colors ${baseClass}`}
                                                    title={atom}
                                                  />
                                                );
                                              })}
                                            </React.Fragment>
                                          ))}
                                        </div>
                                      </div>

                                      {unknownSelectedAtoms.length > 0 && (
                                        <p className="text-[10px] text-red-400 font-mono break-all">
                                          Unknown atoms: {unknownSelectedAtoms.join(', ')}
                                        </p>
                                      )}
                                    </>
                                  )}
                                </div>
                              );
                            })()}
                          </Reorder.Item>
                        ))}
                      </Reorder.Group>
                    )}
                  </div>
            </section>

            <section>
              <h2 className="text-xs font-semibold text-neutral-500 uppercase tracking-wider mb-4 flex items-center gap-2">
                <Download className="w-3 h-3" />
                Export Mesh
              </h2>
              <div className="grid grid-cols-3 gap-2">
                <button
                  onClick={() => exportSvg(tilingType, rows, cols, activeOperators, palette, colorMode, edgeColor, generationOptions)}
                  className="px-3 py-2 rounded-lg text-[10px] font-bold uppercase tracking-wider transition-all border bg-neutral-800/40 border-neutral-700/50 text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200"
                >
                  SVG
                </button>
                <button
                  onClick={() => exportObj(tilingType, rows, cols, activeOperators, generationOptions)}
                  className="px-3 py-2 rounded-lg text-[10px] font-bold uppercase tracking-wider transition-all border bg-neutral-800/40 border-neutral-700/50 text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200"
                >
                  OBJ
                </button>
                <button
                  onClick={() => exportOff(tilingType, rows, cols, activeOperators, palette, colorMode, generationOptions)}
                  className="px-3 py-2 rounded-lg text-[10px] font-bold uppercase tracking-wider transition-all border bg-neutral-800/40 border-neutral-700/50 text-neutral-400 hover:bg-neutral-800 hover:text-neutral-200"
                >
                  OFF
                </button>
              </div>
            </section>
          </div>
        </div>

        <div className="p-6 border-t border-neutral-800 bg-neutral-900/80">
          <div className="flex items-center gap-3 text-neutral-500">
            <Info className="w-4 h-4" />
            <p className="text-[10px] leading-relaxed uppercase tracking-tight">
              Interactive 2D tiling visualization using half-edge topological data structures.
            </p>
          </div>
          <div className="mt-4 flex flex-wrap gap-3 font-mono text-[10px] text-neutral-500">
            <span title="Vertices">V <span id="stat-vertices">-</span></span>
            <span title="Faces">F <span id="stat-faces">-</span></span>
            <span title="Edges">E <span id="stat-edges">-</span></span>
            <span title="Colours Used" className="text-blue-400">C <span id="stat-colors">-</span></span>
          </div>
        </div>
      </motion.aside>

      {/* Main Content */}
      <main className="flex-1 relative flex flex-col min-w-0">
        <div className="absolute top-6 left-6 z-10 pointer-events-none">
          <button 
            onClick={() => setSidebarOpen(!sidebarOpen)}
            className="pointer-events-auto p-2 bg-neutral-900/80 backdrop-blur-md border border-neutral-800 rounded-xl hover:bg-neutral-800 transition-colors text-neutral-400 hover:text-white"
          >
            <Maximize className="w-5 h-5" />
          </button>
        </div>

        <div className="w-full h-full">
          <TilingCanvas
            tilingType={tilingType}
            rows={rows}
            cols={cols}
            showEdges={showEdges}
            showVertices={showVertices}
            showFaces={showFaces}
            wireframe={wireframe}
            operators={activeOperators}
            palette={palette}
            colorMode={colorMode}
            edgeColor={edgeColor}
            generationOptions={generationOptions}
          />
        </div>

        <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-10 px-4 py-2 bg-neutral-900/60 backdrop-blur-md border border-neutral-800 rounded-full flex items-center gap-6">
          <div className="flex items-center gap-2">
             <div className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
             <span className="text-[10px] text-neutral-400 uppercase tracking-widest font-mono">Live Rendering</span>
          </div>
          <div className="w-px h-4 bg-neutral-800" />
          <div className="flex gap-4">
            <TriangleIcon className="w-4 h-4 text-neutral-600" />
            <Square className="w-4 h-4 text-neutral-600" />
            <Hexagon className="w-4 h-4 text-neutral-600" />
          </div>
        </div>
      </main>
    </div>
  );
}
