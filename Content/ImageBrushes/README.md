# Image Brush Textures

This folder contains default grayscale brush textures for Polybrush's image brush feature.

## Generating Default Textures

To generate the default brush textures, use the menu:
**Tools > Polybrush > Generate Default Brush Textures**

This will create 12 procedural brush patterns:
- Soft Circle - Smooth falloff from center
- Hard Circle - Sharp-edged circular brush
- Linear Gradient - Directional gradient
- Radial Gradient - Smooth radial falloff
- Noise - Perlin noise pattern
- Splatter - Random splatter effect
- Star - 5-pointed star shape
- Square - Square brush with soft edges
- Diamond - Diamond/rhombus shape
- Ring - Hollow ring pattern
- Crescent - Crescent moon shape
- Gaussian - Gaussian distribution falloff

## Texture Requirements

Image brush textures should be:
- Grayscale (lighter = stronger influence)
- Read/Write enabled in import settings
- Power of 2 dimensions recommended (e.g., 256x256)
- Uncompressed for best quality

## Creating Custom Brushes

You can create your own brush textures:
1. Create a grayscale image in any image editor
2. Import into Unity
3. Enable Read/Write in texture import settings
4. Assign to the Image Brush texture slot in Brush Settings
