import React, { useEffect, useRef } from 'react';
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { TilingGenerationOptions, UNIFORM_TILINGS } from '../lib/tiling-geometries';
import { PaletteKey } from '../lib/palettes';

import { applyOperator, Mesh, OperatorSpec } from '../lib/conway-operators';
import { ColorMode, computeFaceColors } from '../lib/coloring';

interface TilingCanvasProps {
  tilingType: string;
  rows: number;
  cols: number;
  showEdges: boolean;
  showVertices: boolean;
  showFaces: boolean;
  wireframe: boolean;
  operators: OperatorSpec[];
  palette: PaletteKey;
  colorMode: ColorMode;
  edgeColor: string;
  generationOptions?: TilingGenerationOptions;
}

export const TilingCanvas: React.FC<TilingCanvasProps> = ({
  tilingType,
  rows,
  cols,
  showEdges,
  showVertices,
  showFaces,
  wireframe,
  operators,
  palette,
  colorMode,
  edgeColor,
  generationOptions,
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const sceneRef = useRef<{
    scene: THREE.Scene;
    camera: THREE.PerspectiveCamera;
    renderer: THREE.WebGLRenderer;
    controls: OrbitControls;
    meshGroup: THREE.Group;
  } | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x0a0a0a);

    const camera = new THREE.PerspectiveCamera(
      45,
      containerRef.current.clientWidth / containerRef.current.clientHeight,
      0.1,
      1000
    );
    camera.position.z = 10;

    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(containerRef.current.clientWidth, containerRef.current.clientHeight);
    renderer.setPixelRatio(window.devicePixelRatio);
    containerRef.current.appendChild(renderer.domElement);

    const controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;

    const ambientLight = new THREE.AmbientLight(0xffffff, 0.5);
    scene.add(ambientLight);

    const directLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directLight.position.set(5, 5, 5);
    scene.add(directLight);

    const meshGroup = new THREE.Group();
    scene.add(meshGroup);

    sceneRef.current = { scene, camera, renderer, controls, meshGroup };

    const animate = () => {
      requestAnimationFrame(animate);
      controls.update();
      renderer.render(scene, camera);
    };
    animate();

    const handleResize = () => {
      if (!containerRef.current || !sceneRef.current) return;
      const { camera, renderer } = sceneRef.current;
      camera.aspect = containerRef.current.clientWidth / containerRef.current.clientHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(containerRef.current.clientWidth, containerRef.current.clientHeight);
    };
    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('resize', handleResize);
      renderer.dispose();
      renderer.domElement.remove();
    };
  }, []);

  useEffect(() => {
    if (!sceneRef.current) return;
    const { meshGroup } = sceneRef.current;

    // Clear previous
    while (meshGroup.children.length > 0) {
      const child = meshGroup.children[0] as THREE.Mesh;
      if (child.geometry) child.geometry.dispose();
      if (Array.isArray(child.material)) {
        child.material.forEach((m) => m.dispose());
      } else {
        child.material.dispose();
      }
      meshGroup.remove(child);
    }

    const tiling = UNIFORM_TILINGS[tilingType];
    if (!tiling) return;

    let { vertices, indices, faces } = tiling.generate(rows, cols, generationOptions);

    if (operators.length > 0) {
      let mesh: Mesh = { vertices, faces };
      
      for (const op of operators) {
        mesh = applyOperator(mesh, op);
      }
      
      vertices = mesh.vertices;
      faces = mesh.faces;
      // Rebuild indices for the new faces
      indices = [];
      faces.forEach(f => {
        for (let i = 1; i < f.length - 1; i++) {
          indices.push(f[0], f[i], f[i + 1]);
        }
      });
    }

    let computedFaceColors = computeFaceColors({ vertices, faces }, palette, colorMode);
    const uniqueColorsUsed = new Set(computedFaceColors);
    const uniqueEdges = new Set<string>();
    faces.forEach((face) => {
      for (let i = 0; i < face.length; i++) {
        const a = face[i];
        const b = face[(i + 1) % face.length];
        uniqueEdges.add(a < b ? `${a},${b}` : `${b},${a}`);
      }
    });
    const updateStat = (ids: string[], value: string) => {
      ids.forEach((id) => {
        const element = document.getElementById(id);
        if (element) element.innerText = value;
      });
    };

    updateStat(['stat-colors'], uniqueColorsUsed.size.toString());
    updateStat(['stat-vertices'], (vertices.length / 3).toString());
    updateStat(['stat-faces'], faces.length.toString());
    updateStat(['stat-edges'], uniqueEdges.size.toString());

    if (showFaces) {
      // Create non-indexed geometry for per-face colors
      const posAttr: number[] = [];
      const colorAttr: number[] = [];
      const tmpColor = new THREE.Color();

      faces.forEach((face, fIdx) => {
        tmpColor.set(computedFaceColors[fIdx] || '#ffffff');
        // Fan triangulation
        for (let i = 1; i < face.length - 1; i++) {
          const vIdxs = [face[0], face[i], face[i+1]];
          vIdxs.forEach(vIdx => {
            posAttr.push(vertices[vIdx * 3], vertices[vIdx * 3 + 1], vertices[vIdx * 3 + 2]);
            colorAttr.push(tmpColor.r, tmpColor.g, tmpColor.b);
          });
        }
      });

      const coloredGeom = new THREE.BufferGeometry();
      coloredGeom.setAttribute('position', new THREE.Float32BufferAttribute(posAttr, 3));
      coloredGeom.setAttribute('color', new THREE.Float32BufferAttribute(colorAttr, 3));
      coloredGeom.computeVertexNormals();

      const material = new THREE.MeshPhongMaterial({
        vertexColors: true,
        side: THREE.DoubleSide,
        flatShading: true,
        wireframe: wireframe,
        transparent: true,
        opacity: 0.9,
        shininess: 30,
        polygonOffset: true,
        polygonOffsetFactor: 1,
        polygonOffsetUnits: 1,
      });
      const mesh = new THREE.Mesh(coloredGeom, material);
      mesh.renderOrder = 0;
      meshGroup.add(mesh);
    }

    if (showEdges) {
      const edgeIndices: number[] = [];
      faces.forEach(face => {
        for (let i = 0; i < face.length; i++) {
          edgeIndices.push(face[i], face[(i + 1) % face.length]);
        }
      });
      const edgeGeom = new THREE.BufferGeometry();
      edgeGeom.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
      edgeGeom.setIndex(edgeIndices);
      const edgeMat = new THREE.LineBasicMaterial({ 
        color: new THREE.Color(edgeColor),
        linewidth: 2, 
        transparent: true, 
        opacity: 0.8 
      });
      const edges = new THREE.LineSegments(edgeGeom, edgeMat);
      edges.renderOrder = 1;
      meshGroup.add(edges);
    }

    if (showVertices) {
      // Create a circular texture for the points
      const canvas = document.createElement('canvas');
      canvas.width = 64;
      canvas.height = 64;
      const ctx = canvas.getContext('2d')!;
      ctx.beginPath();
      ctx.arc(32, 32, 30, 0, Math.PI * 2);
      ctx.fillStyle = '#ffffff';
      ctx.fill();
      const texture = new THREE.CanvasTexture(canvas);

      const pointsGeom = new THREE.BufferGeometry();
      pointsGeom.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
      const pointsMat = new THREE.PointsMaterial({ 
        color: 0xffffff, 
        size: 0.1, 
        transparent: true, 
        opacity: 0.8,
        map: texture,
        alphaTest: 0.5
      });
      const points = new THREE.Points(pointsGeom, pointsMat);
      points.position.z = 0.02; // Above edges
      meshGroup.add(points);
    }

    // Raycasting for interaction
    const raycaster = new THREE.Raycaster();
    const mouse = new THREE.Vector2();

    const onClick = (event: MouseEvent) => {
      if (!containerRef.current || !sceneRef.current) return;
      const rect = containerRef.current.getBoundingClientRect();
      mouse.x = ((event.clientX - rect.left) / containerRef.current.clientWidth) * 2 - 1;
      mouse.y = -((event.clientY - rect.top) / containerRef.current.clientHeight) * 2 + 1;

      raycaster.setFromCamera(mouse, sceneRef.current.camera);
      const intersects = raycaster.intersectObjects(meshGroup.children);

      if (intersects.length > 0) {
        const intersection = intersects[0];
        console.log('Clicked face index:', intersection.faceIndex);
        // We could use the half-edge structure here to find neighbors
      }
    };

    containerRef.current.addEventListener('click', onClick);
    return () => {
      containerRef.current?.removeEventListener('click', onClick);
    };

  }, [tilingType, rows, cols, showEdges, showVertices, showFaces, wireframe, operators, palette, colorMode, edgeColor, generationOptions]);

  return <div id="canvas-container" ref={containerRef} className="w-full h-full" />;
};
