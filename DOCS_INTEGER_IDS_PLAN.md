# Integer IDs Implementation Plan

## Overview

Replace Guid-based IDs with integers for 30-40% memory reduction and 10-15% performance improvement.

**Current System:**
- `Vertex.Name`: `Guid` (16 bytes)
- `Face.Name`: `Guid` → `string` (16 bytes + allocation)
- `Halfedge.Name`: `(Guid, Guid)?` (32 bytes)

**Proposed System:**
- `Vertex.ID`: `int` (4 bytes)
- `Face.ID`: `int` (4 bytes)
- `Halfedge.Name`: `(int, int)?` (8 bytes)

**Benefits:**
- ✅ 75% memory reduction per ID (16→4 bytes)
- ✅ Faster comparisons and hashing
- ✅ Better cache locality
- ✅ More readable debugging

**Trade-offs:**
- ❌ Requires ID remapping in Duplicate/Append operations
- ❌ Not globally unique (only per-mesh unique)
- ❌ Moderate implementation complexity

---

## The Collision Problem

### Why Naive Integer IDs Don't Work

```csharp
// ❌ BROKEN: Global counter causes collisions
public class Vertex {
    private static int _nextId = 0;  // SHARED GLOBAL STATE
    public int ID { get; private set; }

    public Vertex(Vector3 point) {
        ID = _nextId++;  // Collisions on Duplicate/Append!
    }
}
```

### Collision Scenarios

**1. Mesh Duplication:**
```csharp
var mesh1 = new PolyMesh(verts, faces);  // Creates IDs 0-99
var mesh2 = mesh1.Duplicate();           // ❌ SAME IDs 0-99!
mesh1.Append(mesh2);                     // ❌ DUPLICATE IDs!
```

**2. Halfedge Dictionary Keys:**
```csharp
// Halfedge.Name is (int, int)?
var edge1 = (5, 10);  // From mesh1
var edge2 = (5, 10);  // From mesh2 after append
// ❌ Same key, different edges → KeyNotFoundException
```

---

## Solution: Per-Mesh ID Space

Each PolyMesh maintains its own ID counter. IDs are unique **within a mesh**, not globally.

### Core Implementation

```csharp
public partial class PolyMesh {
    // Per-mesh ID counters
    private int _nextVertexId = 0;
    private int _nextFaceId = 0;

    public class Vertex {
        public int ID { get; internal set; }  // Set by PolyMesh only
        public Vector3 Position { get; set; }

        // No constructor sets ID - PolyMesh does it
        internal Vertex(Vector3 pos) {
            Position = pos;
            // ID assigned by PolyMesh when added
        }
    }

    public class Face {
        public int ID { get; internal set; }
        public string Name => ID.ToString();  // For compatibility

        internal Face(Halfedge edge) {
            Halfedge = edge;
            // ID assigned by PolyMesh when added
        }
    }

    public class Halfedge {
        public (int, int)? Name {
            get {
                if (Vertex == null || Prev?.Vertex == null)
                    return null;
                return (Vertex.ID, Prev.Vertex.ID);
            }
        }
    }
}
```

### ID Assignment

```csharp
// In PolyMesh constructor/InitIndexed
private void InitIndexed(IEnumerable<Vector3> verticesByPoints,
    IEnumerable<IEnumerable<int>> facesByVertexIndices)
{
    // Add vertices with sequential IDs
    foreach (Vector3 p in verticesByPoints)
    {
        var vertex = new Vertex(p);
        vertex.ID = _nextVertexId++;
        Vertices.Add(vertex);
    }

    // Add faces (IDs assigned by MeshFaceList.Add)
    // ... face addition logic
}
```

---

## Handling Operations

### 1. Duplicate

```csharp
public PolyMesh Duplicate(Vector3 offset = default, float scale = 1)
{
    var newMesh = new PolyMesh();  // New ID space: starts at 0

    // Map old IDs to new IDs
    var vertexIdMap = new Dictionary<int, int>(Vertices.Count);

    // Copy vertices with new IDs
    for (int i = 0; i < Vertices.Count; i++)
    {
        var oldVertex = Vertices[i];
        var newVertex = new Vertex(oldVertex.Position * scale + offset);
        newVertex.ID = newMesh._nextVertexId++;  // NEW ID in new mesh

        vertexIdMap[oldVertex.ID] = newVertex.ID;  // Track mapping
        newMesh.Vertices.Add(newVertex);
    }

    // Rebuild faces with new vertex IDs
    foreach (var face in Faces)
    {
        var oldVerts = face.GetVertices();
        var newVerts = new List<Vertex>(oldVerts.Count);

        for (int i = 0; i < oldVerts.Count; i++)
        {
            int newId = vertexIdMap[oldVerts[i].ID];
            var newVert = newMesh.Vertices.First(v => v.ID == newId);
            newVerts.Add(newVert);
        }

        newMesh.Faces.Add(newVerts);  // Assigns new face ID internally
    }

    // Copy roles/tags
    newMesh.FaceRoles = new List<Roles>(FaceRoles);
    newMesh.FaceTags = FaceTags.Select(t => new HashSet<string>(t)).ToList();
    newMesh.VertexRoles = new List<Roles>(VertexRoles);

    return newMesh;
}
```

### 2. Append

```csharp
public void Append(PolyMesh other, Matrix4x4 matrix, bool forceDuplicate = false)
{
    if (other == null) return;

    // Force duplication to avoid modifying original
    if (forceDuplicate || other == this)
    {
        other = other.Duplicate();
    }

    // Remap other's vertex IDs to this mesh's ID space
    var vertexIdMap = new Dictionary<int, int>(other.Vertices.Count);
    int originalVertexCount = Vertices.Count;

    foreach (var otherVertex in other.Vertices)
    {
        var newVertex = new Vertex(otherVertex.Position);

        // Transform if needed
        if (matrix != Matrix4x4.identity)
        {
            newVertex.Position = matrix.MultiplyPoint(newVertex.Position);
        }

        // Assign NEW ID in this mesh's space
        newVertex.ID = _nextVertexId++;
        vertexIdMap[otherVertex.ID] = newVertex.ID;

        Vertices.Add(newVertex);
    }

    // Rebuild faces with remapped vertex IDs
    for (int i = 0; i < other.Faces.Count; i++)
    {
        var otherFace = other.Faces[i];
        var oldVerts = otherFace.GetVertices();
        var remappedVerts = new List<Vertex>(oldVerts.Count);

        for (int j = 0; j < oldVerts.Count; j++)
        {
            int newId = vertexIdMap[oldVerts[j].ID];
            var vert = Vertices.First(v => v.ID == newId);
            remappedVerts.Add(vert);
        }

        Faces.Add(remappedVerts);  // Assigns new face ID
    }

    // Append roles/tags
    FaceRoles.AddRange(other.FaceRoles);
    FaceTags.AddRange(other.FaceTags.Select(t => new HashSet<string>(t)));
    VertexRoles.AddRange(other.VertexRoles);

    // Halfedge pairing happens incrementally (Phase 3A)
}
```

### 3. MeshFaceList - Face ID Assignment

```csharp
// In MeshFaceList._AddOrInsert()
private Boolean _AddOrInsert(IEnumerable<Vertex> vertices, bool insert, int index=-1)
{
    // ... existing validation logic ...

    Face newFace = new Face(newEdges[0]);

    // Assign face ID from mesh's counter
    newFace.ID = _mPolyMesh._nextFaceId++;

    // ... rest of face addition logic ...
}
```

---

## Implementation Checklist

### Phase 1: Core Structure Changes
- [ ] Add `_nextVertexId` and `_nextFaceId` to PolyMesh
- [ ] Change `Vertex.Name` from `Guid` to `int ID`
- [ ] Change `Face.Name` from `string` to `int ID` (keep `Name` property as `ID.ToString()` for compatibility)
- [ ] Update `Halfedge.Name` to use `(int, int)?`
- [ ] Update all constructors to not generate IDs

### Phase 2: ID Assignment
- [ ] Update `InitIndexed()` to assign vertex IDs
- [ ] Update `MeshFaceList.Add()` to assign face IDs
- [ ] Update `MeshVertexList` if needed for ID assignment

### Phase 3: Operation Updates
- [ ] Implement ID remapping in `Duplicate()`
- [ ] Implement ID remapping in `Append()`
- [ ] Update `FaceRemove()` if needed
- [ ] Update Conway operations that create vertices/faces

### Phase 4: Dictionary/Collection Updates
- [ ] Update `MeshHalfedgeList` - change key type from `(Guid, Guid)?` to `(int, int)?`
- [ ] Update `MeshFaceList` - change key type from `string` to `int`
- [ ] Update all dictionary lookups

### Phase 5: Serialization (if applicable)
- [ ] Update OFF parser to handle integer IDs
- [ ] Update OBJ parser to handle integer IDs
- [ ] Save/restore max ID counters if needed

### Phase 6: Testing
- [ ] Test basic mesh creation
- [ ] Test Duplicate operation
- [ ] Test Append operation
- [ ] Test mesh with boundary edges (naked edges)
- [ ] Test Conway operations
- [ ] Test complex workflows (create → operations → output)

---

## Migration Strategy

### Option A: Clean Break (Recommended)
Replace all Guids with ints in one pass. Simplest and cleanest.

### Option B: Backward Compatibility
Keep both systems temporarily:

```csharp
public class Vertex {
    public int ID { get; internal set; }

    // Legacy property for backward compatibility
    [Obsolete("Use ID instead")]
    public Guid Name => new Guid(ID, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
```

**Recommendation:** Use Option A (clean break) since this is a major optimization and the API is already being improved.

---

## Collision Prevention Summary

| Scenario | Solution |
|----------|----------|
| **Duplicate** | New mesh = new ID space, remap all vertex IDs when building faces |
| **Append** | Remap appended mesh's vertex IDs before adding to this mesh |
| **Thread safety** | Per-mesh counters (no shared global state) |
| **Operations creating vertices** | Use mesh's `_nextVertexId++` counter |
| **Serialization** | Save/restore max ID on load if needed |

---

## Performance Impact Estimate

### Memory Savings

**Per Vertex:**
- Before: 16 bytes (Guid)
- After: 4 bytes (int)
- **Savings: 12 bytes (75%)**

**Per Face:**
- Before: 16 bytes (Guid) + ~8 bytes (string allocation)
- After: 4 bytes (int)
- **Savings: ~20 bytes (83%)**

**Per Halfedge:**
- Before: 32 bytes ((Guid, Guid)?)
- After: 8 bytes ((int, int)?)
- **Savings: 24 bytes (75%)**

**Example Mesh (1000 vertices, 2000 faces, 6000 halfedges):**
- Before: ~196 KB just for IDs
- After: ~56 KB just for IDs
- **Total Savings: ~140 KB (71%)**

### Speed Improvements

- **Hashing:** int hash is ~2-3x faster than Guid hash
- **Comparison:** int comparison is ~2-3x faster than Guid comparison
- **Dictionary lookups:** ~15-20% faster with int keys
- **Cache locality:** Better cache performance with smaller IDs

**Expected Overall Gain:** 10-15% speedup in operations that frequently lookup halfedges or faces by ID.

---

## Risks & Considerations

### Low Risk
- ✅ ID remapping is straightforward
- ✅ Per-mesh ID space prevents most collisions
- ✅ Existing operations already handle vertex position remapping

### Medium Risk
- ⚠️ Need to ensure ID counters never overflow (int.MaxValue = 2.1 billion)
- ⚠️ Serialization may need updates
- ⚠️ Custom user code that relies on Guid uniqueness may break

### Mitigation
- Add ID overflow checks (throw exception if approaching int.MaxValue)
- Add migration guide for users
- Thorough testing of all operations

---

## Conclusion

**Recommended:** Implement this optimization if:
- You frequently create/process large meshes (1000+ vertices)
- Memory usage is a concern
- You're doing many mesh operations per frame

**Skip if:**
- Phase 1-3A optimizations (9-15x speedup) are sufficient
- Codebase stability is more important than additional optimization
- Not frequently creating very large meshes

**Effort Estimate:** 2-3 days for full implementation and testing

**Reward:** Additional 10-15% speedup + 30-40% memory reduction
