# Design Document: Polybrush Enhancements

## Overview

This design document outlines the implementation of three major enhancements to the Unity Polybrush tool:

1. **Image-Based Brushes**: Support for using grayscale textures as brush shapes, similar to Unity Terrain's brush system
2. **Improved Sculpt Power Controls**: Enhanced UI and feedback for sculpting strength parameters
3. **Subdivision/Unsubdivision Brushes**: Dynamic mesh detail management through local subdivision and simplification

These enhancements will improve Polybrush's usability for artists familiar with Unity Terrain tools while adding powerful new mesh editing capabilities.

## Architecture

### High-Level Structure

The enhancements will integrate into Polybrush's existing brush mode architecture:

```
BrushMode (abstract base)
├── BrushModeMesh (mesh-specific base)
│   ├── BrushModeSculpt (existing)
│   │   └── Enhanced with image brush support
│   └── BrushModeTopology (new - handles both subdivide and unsubdivide)
└── BrushSettings (existing)
    └── Extended with ImageBrushSettings
```

### Key Architectural Decisions

1. **Image Brush as Extension**: Image brush functionality will be added as an optional mode within existing brush types rather than a separate brush mode, allowing any sculpting operation to use image-based intensity
2. **Compute Shader Acceleration**: Image sampling and weight calculation will use compute shaders when available, falling back to CPU for compatibility
3. **Non-Destructive Subdivision**: Subdivision operations will maintain undo/redo support through Polybrush's existing mesh state management
4. **Incremental Topology Changes**: Subdivision and unsubdivision will operate incrementally within the brush radius rather than on the entire mesh
5. **Unified Topology Mode**: Subdivision and unsubdivision are combined into a single BrushModeTopology, with the operation determined by modifier keys (similar to how paint mode uses Control to erase)

## Components and Interfaces

### 1. ImageBrushSettings

Extends BrushSettings to include image-specific parameters:

```csharp
[Serializable]
internal class ImageBrushSettings
{
    public Texture2D brushTexture;
    public float rotation;  // 0-360 degrees
    public bool preserveAspectRatio;
    public FilterMode samplingMode;
}
```

### 2. ImageBrushSampler

Handles texture sampling and weight calculation:

```csharp
internal static class ImageBrushSampler
{
    // Sample texture at world position within brush radius
    public static float SampleAtPosition(
        Texture2D texture,
        Vector3 worldPos,
        Vector3 brushCenter,
        float brushRadius,
        float rotation,
        bool preserveAspect);
    
    // Compute shader variant for batch processing
    public static void SampleBatch(
        Texture2D texture,
        Vector3[] positions,
        Vector3 brushCenter,
        float brushRadius,
        float rotation,
        float[] outWeights);
}
```

### 3. BrushModeTopology

New brush mode that handles both subdivision and unsubdivision:

```csharp
internal class BrushModeTopology : BrushModeMesh
{
    public enum TopologyMode
    {
        Subdivide,    // Add detail (left click)
        Unsubdivide   // Remove detail (shift + left click or control + left click)
    }
    
    [SerializeField]
    private TopologyMode currentMode = TopologyMode.Subdivide;
    
    // Subdivide faces within brush influence
    protected void SubdivideFaces(
        PolyMesh mesh,
        int[] faceIndices,
        float[] weights,
        int iterations);
    
    // Merge vertices within threshold distance
    protected void MergeVertices(
        PolyMesh mesh,
        int[] vertexIndices,
        float[] weights,
        float threshold);
    
    // Interpolate vertex attributes for new vertices
    protected void InterpolateAttributes(
        PolyMesh mesh,
        int newVertexIndex,
        int[] sourceVertices,
        float[] weights);
    
    // Validate mesh remains manifold after operations
    protected bool ValidateMeshTopology(PolyMesh mesh);
}
```

### 4. SculptPowerUI

Enhanced UI component for sculpt strength:

```csharp
internal static class SculptPowerUI
{
    // Draw improved strength slider with visual feedback
    public static float DrawSculptPowerSlider(
        string label,
        float value,
        float min,
        float max,
        BrushTarget previewTarget);
    
    // Generate preview visualization of displacement
    public static void DrawDisplacementPreview(
        BrushTarget target,
        float strength);
}
```

## Data Models

### ImageBrushData

Runtime data for image brush operations:

```csharp
internal class ImageBrushData
{
    public Texture2D texture;
    public float rotation;
    public Matrix4x4 worldToBrushSpace;
    public Matrix4x4 brushToTextureSpace;
    public ComputeBuffer weightBuffer;  // GPU acceleration
}
```

### SubdivisionCache

Tracks subdivision state for undo/redo:

```csharp
internal class SubdivisionCache
{
    public Dictionary<int, int[]> originalToSubdivided;  // vertex mapping
    public List<int> newVertexIndices;
    public List<int> modifiedFaceIndices;
    public MeshChannel modifiedChannels;
}
```

### MeshSimplificationData

Data for unsubdivision operations:

```csharp
internal class MeshSimplificationData
{
    public Dictionary<int, List<int>> vertexClusters;  // vertices to merge
    public Vector3[] originalPositions;
    public float[] mergeWeights;
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Image Brush Properties

Property 1: Image intensity determines weight
*For any* grayscale image texture and brush application, the vertex displacement weight should equal the sampled grayscale intensity value multiplied by the brush strength
**Validates: Requirements 1.2, 1.5**

Property 2: Texture coordinates map to brush radius
*For any* brush position and radius, texture coordinates (0,0) to (1,1) should map to world space positions from brush center minus radius to brush center plus radius
**Validates: Requirements 1.3**

Property 3: Rotation transforms sampling coordinates
*For any* rotation angle θ, sampling at world position P with rotation θ should equal sampling at position P rotated by -θ around the brush center with rotation 0
**Validates: Requirements 1.4**

Property 4: Aspect ratio preservation
*For any* non-square texture with aspect ratio A, the brush projection should maintain aspect ratio A in world space
**Validates: Requirements 1.6**

Property 5: Brush preset round-trip
*For any* brush preset with an image texture, saving then loading the preset should restore the same texture reference
**Validates: Requirements 1.7**

### Sculpt Power Properties

Property 6: Strength affects overlay visualization
*For any* two strength values S1 and S2 where S1 < S2, the overlay visualization intensity at any vertex should be proportionally greater for S2 than S1
**Validates: Requirements 2.6**

Property 7: Consistent strength across modes
*For any* strength value S and two brush modes M1 and M2 that support strength, applying S in M1 and M2 to equivalent geometry should produce proportional effects
**Validates: Requirements 2.7**

### Subdivision Properties

Property 8: Subdivision increases vertex count
*For any* mesh and subdivision operation, the vertex count after subdivision should be greater than before subdivision
**Validates: Requirements 3.1**

Property 9: Subdivision preserves surface shape
*For any* mesh face, the maximum distance from any point on the subdivided surface to the original surface should be less than a small epsilon value
**Validates: Requirements 3.2**

Property 10: Unsubdivision decreases vertex count
*For any* mesh and unsubdivision operation with non-zero strength, the vertex count after unsubdivision should be less than or equal to before unsubdivision
**Validates: Requirements 3.3**

Property 11: Unsubdivision preserves overall shape
*For any* mesh, the bounding box and overall silhouette after unsubdivision should remain within a tolerance of the original
**Validates: Requirements 3.4**

Property 12: Attribute interpolation is weighted average
*For any* new vertex created by subdivision, each attribute (color, normal, UV) should equal the weighted average of the surrounding source vertices' attributes
**Validates: Requirements 3.5**

Property 13: Merged vertex attributes are averaged
*For any* set of vertices merged by unsubdivision, the resulting vertex attributes should equal the average of the merged vertices' attributes
**Validates: Requirements 3.6**

Property 14: Strength controls subdivision iterations
*For any* two strength values S1 and S2 where S1 < S2, the number of subdivision iterations (and thus new vertices created) should be greater for S2 than S1
**Validates: Requirements 3.7**

Property 15: Topology changes update rendering immediately
*For any* mesh topology modification, querying the mesh collider and renderer immediately after should reflect the new topology
**Validates: Requirements 3.9**

Property 16: ProBuilder compatibility maintained
*For any* ProBuilder mesh, after subdivision or unsubdivision operations, the mesh should remain a valid ProBuilder mesh with all ProBuilder-specific data intact
**Validates: Requirements 3.10**

## Error Handling

### Image Brush Errors

1. **Missing Texture**: If no texture is assigned, fall back to standard circular brush behavior
2. **Invalid Texture Format**: Display warning and disable image brush mode if texture is not readable or not grayscale-compatible
3. **Texture Loading Failure**: Cache last valid texture and revert on load failure

### Subdivision Errors

1. **Degenerate Geometry Detection**: Before subdivision, check for zero-area faces or coincident vertices and skip those faces
2. **Maximum Vertex Limit**: Prevent subdivision if it would exceed Unity's mesh vertex limit (65,535 for 16-bit or 4,294,967,295 for 32-bit)
3. **Invalid Topology**: Validate mesh remains manifold after operations; rollback if validation fails

### Unsubdivision Errors

1. **Minimum Vertex Count**: Prevent unsubdivision if mesh would have fewer than 3 vertices
2. **Topology Collapse**: Detect and prevent operations that would create non-manifold geometry or inverted faces
3. **Attribute Mismatch**: Handle cases where vertices have different attribute counts gracefully

### General Error Handling

1. **Undo/Redo Safety**: All operations must be fully reversible through Unity's undo system
2. **Null Reference Protection**: Validate all mesh and component references before operations
3. **Performance Safeguards**: Limit operations to reasonable brush sizes to prevent editor freezing

## Testing Strategy

### Unit Testing

Unit tests will cover:

- Image texture sampling at specific coordinates
- Coordinate transformation matrices (world to brush to texture space)
- Vertex attribute interpolation calculations
- Mesh topology validation functions
- Edge cases: null textures, zero-radius brushes, empty meshes

### Property-Based Testing

Property-based tests will use **Unity Test Framework with NUnit** for C# testing. Each test will run a minimum of 100 iterations with randomized inputs.

**Property Test Configuration**:
- Framework: Unity Test Framework (NUnit)
- Minimum iterations per test: 100
- Random seed: Configurable for reproducibility
- Test data generators: Custom generators for meshes, textures, brush parameters

**Test Tagging Convention**:
Each property-based test must include a comment tag in this exact format:
```csharp
// **Feature: polybrush-enhancements, Property {N}: {property description}**
```

**Property Test Coverage**:

1. **Image Brush Properties** (Properties 1-5):
   - Generate random grayscale textures (various sizes, patterns)
   - Generate random brush positions, radii, rotations
   - Generate random mesh geometries
   - Verify sampling, mapping, and persistence properties

2. **Sculpt Power Properties** (Properties 6-7):
   - Generate random strength values across valid range
   - Generate random mesh targets
   - Verify visualization and cross-mode consistency

3. **Subdivision Properties** (Properties 8-16):
   - Generate random meshes (various topologies: planar, curved, complex)
   - Generate random brush parameters
   - Verify vertex counts, shape preservation, attribute interpolation
   - Test ProBuilder mesh compatibility

**Generator Strategies**:
- **Mesh Generator**: Create meshes with controlled complexity (vertex count, face count, topology type)
- **Texture Generator**: Create grayscale textures with various patterns (solid, gradient, noise, shapes)
- **Brush Parameter Generator**: Generate valid combinations of radius, strength, position, rotation
- **Attribute Generator**: Generate valid vertex colors, normals, UVs for testing interpolation

### Integration Testing

Integration tests will verify:
- Image brush integration with existing sculpt modes
- Subdivision/unsubdivision interaction with undo/redo system
- ProBuilder integration for topology operations
- Compute shader fallback to CPU implementation
- UI updates in response to parameter changes

### Manual Testing Checklist

- Visual verification of image brush patterns on various mesh types
- Sculpt power slider responsiveness and visual feedback
- Subdivision quality on organic and hard-surface meshes
- Unsubdivision behavior on high-poly meshes
- Performance with large brushes and complex meshes
- Undo/redo functionality for all new operations

## Implementation Notes

### Performance Considerations

1. **Compute Shader Usage**: Image sampling and weight calculation should use GPU when available for 10-100x speedup
2. **Spatial Hashing**: Use spatial hashing for vertex proximity queries during unsubdivision
3. **Incremental Updates**: Only update mesh regions affected by brush, not entire mesh
4. **LOD for Preview**: Use lower resolution preview for real-time feedback during brush movement

### Unity Version Compatibility

- Minimum Unity version: 2018.3 (matching current Polybrush requirement)
- Compute shader support: Optional, with CPU fallback
- Mesh API: Use both legacy and modern mesh APIs for compatibility

### ProBuilder Integration

- Detect ProBuilder meshes using existing `ProBuilderInterface`
- Maintain ProBuilder face and edge data during topology changes
- Trigger ProBuilder mesh refresh after operations
- Respect ProBuilder's smoothing groups during subdivision

### Asset Management

- Store image brush textures in project Assets folder
- Include default brush texture library (10-15 common patterns)
- Support texture import from external sources
- Serialize texture references in brush presets using AssetDatabase paths
