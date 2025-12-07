# Requirements Document

## Introduction

This document specifies requirements for three enhancements to the Unity Polybrush tool to improve its mesh sculpting and painting capabilities. The enhancements include: (1) image-based brush support similar to Unity Terrain, (2) improved sculpt power controls for better user understanding, and (3) mesh subdivision/unsubdivision brushes for dynamic detail management.

## Glossary

- **Polybrush**: A Unity Editor tool for mesh painting, sculpting, and geo-scattering on mesh objects
- **Brush Mode**: The active tool mode in Polybrush (sculpt, smooth, paint, texture, prefab)
- **Brush Settings**: Configuration parameters including radius, falloff, strength, and curve
- **Image Brush**: A brush that uses a grayscale image texture to define the brush shape and intensity distribution
- **Sculpt Power**: The strength parameter that controls how much vertices are displaced during sculpting operations
- **Subdivision**: The process of adding more vertices to a mesh by splitting existing faces into smaller faces
- **Unsubdivision**: The process of reducing mesh detail by merging vertices and simplifying geometry
- **Mesh Topology**: The connectivity structure of vertices, edges, and faces in a mesh
- **Vertex Weight**: A normalized value (0-1) representing the influence of a brush operation on a specific vertex
- **Falloff**: The gradual decrease in brush influence from the center to the edge of the brush radius
- **Unity Terrain**: Unity's built-in terrain system that supports image-based brushes for heightmap painting

## Requirements

### Requirement 1

**User Story:** As a 3D artist, I want to use custom image textures as brush shapes, so that I can create more varied and artistic sculpting effects similar to Unity Terrain tools.

#### Acceptance Criteria

1. WHEN a user selects an image brush mode THEN the Polybrush system SHALL display a texture selection interface for choosing grayscale images
2. WHEN a user applies an image brush to a mesh THEN the system SHALL sample the grayscale image values to determine vertex displacement weights
3. WHEN the brush is applied THEN the system SHALL map the image texture coordinates to the brush radius in world space
4. WHERE image brush mode is active, WHEN a user rotates the brush THEN the system SHALL apply the image texture at the specified rotation angle
5. WHEN a user adjusts brush strength THEN the system SHALL multiply the image intensity values by the strength parameter
6. WHEN an image texture is not square THEN the system SHALL preserve the aspect ratio of the image in the brush projection
7. WHEN a user saves a brush preset with an image texture THEN the system SHALL store the texture reference in the preset data

### Requirement 2

**User Story:** As a user transitioning from Unity Terrain tools, I want sculpt power controls that are intuitive and easy to understand, so that I can quickly adjust sculpting intensity without confusion.

#### Acceptance Criteria

1. WHEN a user views the sculpt settings panel THEN the system SHALL display the strength parameter with clear labeling as "Sculpt Power" or "Strength"
2. WHEN a user adjusts the sculpt power slider THEN the system SHALL provide real-time visual feedback showing the expected displacement magnitude
3. WHEN sculpt power is set to minimum THEN the system SHALL apply minimal vertex displacement per brush stroke
4. WHEN sculpt power is set to maximum THEN the system SHALL apply maximum vertex displacement per brush stroke within safe limits
5. WHEN a user hovers over the sculpt power control THEN the system SHALL display a tooltip explaining the parameter in clear terms
6. WHEN the sculpt power value changes THEN the system SHALL update the overlay visualization to reflect the new strength
7. WHERE sculpt power affects multiple brush modes, THEN the system SHALL maintain consistent behavior across all applicable modes

### Requirement 3

**User Story:** As a 3D modeler, I want subdivision and unsubdivision brush tools, so that I can dynamically add detail where needed and simplify areas that don't require high resolution.

#### Acceptance Criteria

1. WHEN a user selects the subdivide brush mode THEN the system SHALL add new vertices to faces within the brush radius
2. WHEN the subdivide brush is applied to a face THEN the system SHALL split the face into smaller faces while preserving the original surface shape
3. WHEN a user selects the unsubdivide brush mode THEN the system SHALL reduce vertex count by merging nearby vertices within the brush radius
4. WHEN the unsubdivide brush is applied THEN the system SHALL preserve the overall mesh shape while simplifying topology
5. WHEN subdivision creates new vertices THEN the system SHALL interpolate vertex attributes (colors, normals, UVs) from surrounding vertices
6. WHEN unsubdivision merges vertices THEN the system SHALL average the positions and attributes of merged vertices
7. WHEN the user adjusts subdivision strength THEN the system SHALL control the number of subdivision iterations per brush stroke
8. WHEN subdivision would create degenerate geometry THEN the system SHALL prevent the operation and maintain mesh validity
9. WHEN the mesh topology changes THEN the system SHALL update the mesh collider and rendering data immediately
10. WHERE ProBuilder integration is active, WHEN subdivision or unsubdivision is applied THEN the system SHALL maintain ProBuilder mesh compatibility
