# Implementation Plan

- [x] 1. Set up image brush infrastructure





  - Create ImageBrushSettings class to store texture, rotation, and aspect ratio settings
  - Create ImageBrushSampler utility class for texture sampling operations
  - Add image brush UI controls to BrushSettings inspector
  - _Requirements: 1.1, 1.6_

- [ ]* 1.1 Write property test for texture coordinate mapping
  - **Property 2: Texture coordinates map to brush radius**
  - **Validates: Requirements 1.3**

- [x] 2. Implement image brush sampling logic









  - Implement CPU-based texture sampling at world positions
  - Create coordinate transformation matrices (world to brush to texture space)
  - Add rotation support for texture sampling
  - _Requirements: 1.2, 1.3, 1.4_

- [ ]* 2.1 Write property test for image intensity to weight conversion
  - **Property 1: Image intensity determines weight**
  - **Validates: Requirements 1.2, 1.5**

- [ ]* 2.2 Write property test for rotation transformation
  - **Property 3: Rotation transforms sampling coordinates**
  - **Validates: Requirements 1.4**

- [x] 3. Add compute shader acceleration for image brushes





  - Create compute shader for batch texture sampling
  - Implement GPU buffer management for weights
  - Add fallback detection and CPU path
  - _Requirements: 1.2, 1.5_

- [ ]* 3.1 Write property test for aspect ratio preservation
  - **Property 4: Aspect ratio preservation**
  - **Validates: Requirements 1.6**

- [x] 4. Integrate image brush with sculpt modes





  - Extend BrushModeSculpt to support image brush weights
  - Modify weight calculation to use image samples when enabled
  - Add image brush toggle to sculpt mode UI
  - _Requirements: 1.2, 1.5_

- [x] 5. Implement brush preset serialization for images




  - Add texture reference serialization to BrushSettings
  - Implement save/load for image brush presets
  - Handle missing texture references gracefully
  - _Requirements: 1.7_

- [ ]* 5.1 Write property test for brush preset round-trip
  - **Property 5: Brush preset round-trip**
  - **Validates: Requirements 1.7**

- [x] 6. Checkpoint - Ensure all image brush tests pass





  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Enhance sculpt power UI controls





  - Update sculpt settings panel with clearer "Sculpt Power" labeling
  - Add tooltip with clear explanation of strength parameter
  - Implement visual feedback preview for displacement magnitude
  - _Requirements: 2.1, 2.5, 2.2_

- [ ]* 7.1 Write property test for strength affecting visualization
  - **Property 6: Strength affects overlay visualization**
  - **Validates: Requirements 2.6**

- [x] 8. Implement consistent strength behavior across modes





  - Audit all brush modes for strength parameter usage
  - Normalize strength application across sculpt, smooth, and topology modes
  - Add strength consistency validation
  - _Requirements: 2.7_

- [ ]* 8.1 Write property test for cross-mode strength consistency
  - **Property 7: Consistent strength across modes**
  - **Validates: Requirements 2.7**

- [x] 9. Create BrushModeTopology class structure





  - Create new BrushModeTopology class inheriting from BrushModeMesh
  - Add TopologyMode enum (Subdivide/Unsubdivide)
  - Implement mode switching based on modifier keys (Control/Shift)
  - Add topology mode UI panel
  - _Requirements: 3.1, 3.3_

- [x] 10. Implement subdivision algorithm





  - Implement face subdivision within brush radius
  - Add vertex attribute interpolation for new vertices
  - Implement iterative subdivision based on strength
  - Add degenerate geometry detection and prevention
  - _Requirements: 3.1, 3.2, 3.5, 3.7, 3.8_

- [ ]* 10.1 Write property test for subdivision vertex count increase
  - **Property 8: Subdivision increases vertex count**
  - **Validates: Requirements 3.1**

- [ ]* 10.2 Write property test for surface shape preservation
  - **Property 9: Subdivision preserves surface shape**
  - **Validates: Requirements 3.2**

- [ ]* 10.3 Write property test for attribute interpolation
  - **Property 12: Attribute interpolation is weighted average**
  - **Validates: Requirements 3.5**

- [ ]* 10.4 Write property test for strength controlling iterations
  - **Property 14: Strength controls subdivision iterations**
  - **Validates: Requirements 3.7**

- [x] 11. Implement unsubdivision algorithm





  - Implement vertex clustering and merging within brush radius
  - Add vertex attribute averaging for merged vertices
  - Implement mesh topology validation
  - Add minimum vertex count protection
  - _Requirements: 3.3, 3.4, 3.6_

- [ ]* 11.1 Write property test for unsubdivision vertex count decrease
  - **Property 10: Unsubdivision decreases vertex count**
  - **Validates: Requirements 3.3**

- [ ]* 11.2 Write property test for overall shape preservation
  - **Property 11: Unsubdivision preserves overall shape**
  - **Validates: Requirements 3.4**

- [ ]* 11.3 Write property test for merged vertex attribute averaging
  - **Property 13: Merged vertex attributes are averaged**
  - **Validates: Requirements 3.6**

- [x] 12. Implement mesh update and rendering synchronization





  - Add immediate mesh collider updates after topology changes
  - Implement rendering data refresh
  - Add mesh bounds recalculation
  - _Requirements: 3.9_

- [ ]* 12.1 Write property test for immediate topology updates
  - **Property 15: Topology changes update rendering immediately**
  - **Validates: Requirements 3.9**

- [x] 13. Add ProBuilder integration for topology operations





  - Detect ProBuilder meshes in topology mode
  - Maintain ProBuilder face and edge data during subdivision
  - Maintain ProBuilder data during unsubdivision
  - Trigger ProBuilder mesh refresh after operations
  - _Requirements: 3.10_

- [ ]* 13.1 Write property test for ProBuilder compatibility
  - **Property 16: ProBuilder compatibility maintained**
  - **Validates: Requirements 3.10**

- [x] 14. Implement undo/redo support for topology operations





  - Register topology changes with Unity's undo system
  - Cache mesh state before topology operations
  - Implement proper undo message descriptions
  - Test undo/redo with multiple operations
  - _Requirements: 3.1, 3.3_

- [x] 15. Add default image brush texture library





  - Create 10-15 default grayscale brush textures
  - Add textures to Content folder with proper import settings
  - Create default image brush presets
  - _Requirements: 1.1_

- [x] 16. Implement error handling and edge cases





  - Add null texture fallback to standard brush
  - Implement invalid texture format warnings
  - Add vertex limit checks for subdivision
  - Implement topology validation with rollback
  - _Requirements: 1.1, 3.8_

- [x] 17. Final checkpoint - Ensure all tests pass





  - Ensure all tests pass, ask the user if questions arise.
