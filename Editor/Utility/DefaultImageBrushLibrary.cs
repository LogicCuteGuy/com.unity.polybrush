using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Provides a library of default image brush textures for Polybrush.
    /// Generates procedural grayscale brush textures that can be used for sculpting.
    /// </summary>
    internal static class DefaultImageBrushLibrary
    {
        private const string BrushTexturesFolder = "Packages/com.unity.polybrush/Content/ImageBrushes";
        private const string BrushTexturesFolderAlt = "Assets/Content/ImageBrushes";
        private const int DefaultTextureSize = 256;

        private static Dictionary<string, Texture2D> s_CachedBrushes;

        /// <summary>
        /// Brush pattern types available in the default library.
        /// </summary>
        internal enum BrushPattern
        {
            SoftCircle,
            HardCircle,
            LinearGradient,
            RadialGradient,
            Noise,
            Splatter,
            Star,
            Square,
            Diamond,
            Ring,
            Crescent,
            Gaussian
        }

        /// <summary>
        /// Gets all available default brush textures.
        /// </summary>
        internal static Dictionary<string, Texture2D> GetAllBrushTextures()
        {
            if (s_CachedBrushes == null || s_CachedBrushes.Count == 0)
            {
                s_CachedBrushes = new Dictionary<string, Texture2D>();
                
                foreach (BrushPattern pattern in System.Enum.GetValues(typeof(BrushPattern)))
                {
                    string name = GetBrushName(pattern);
                    Texture2D texture = GenerateBrushTexture(pattern, DefaultTextureSize);
                    if (texture != null)
                    {
                        s_CachedBrushes[name] = texture;
                    }
                }
            }
            
            return s_CachedBrushes;
        }

        /// <summary>
        /// Gets a specific brush texture by pattern type.
        /// </summary>
        internal static Texture2D GetBrushTexture(BrushPattern pattern)
        {
            var brushes = GetAllBrushTextures();
            string name = GetBrushName(pattern);
            
            if (brushes.TryGetValue(name, out Texture2D texture))
            {
                return texture;
            }
            
            return null;
        }

        /// <summary>
        /// Gets the display name for a brush pattern.
        /// </summary>
        internal static string GetBrushName(BrushPattern pattern)
        {
            switch (pattern)
            {
                case BrushPattern.SoftCircle: return "Soft Circle";
                case BrushPattern.HardCircle: return "Hard Circle";
                case BrushPattern.LinearGradient: return "Linear Gradient";
                case BrushPattern.RadialGradient: return "Radial Gradient";
                case BrushPattern.Noise: return "Noise";
                case BrushPattern.Splatter: return "Splatter";
                case BrushPattern.Star: return "Star";
                case BrushPattern.Square: return "Square";
                case BrushPattern.Diamond: return "Diamond";
                case BrushPattern.Ring: return "Ring";
                case BrushPattern.Crescent: return "Crescent";
                case BrushPattern.Gaussian: return "Gaussian";
                default: return pattern.ToString();
            }
        }

        /// <summary>
        /// Generates a brush texture for the specified pattern.
        /// </summary>
        internal static Texture2D GenerateBrushTexture(BrushPattern pattern, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.R8, false);
            texture.name = GetBrushName(pattern);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float halfSize = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x - halfSize) / halfSize; // -1 to 1
                    float v = (y - halfSize) / halfSize; // -1 to 1
                    float dist = Mathf.Sqrt(u * u + v * v);
                    
                    float intensity = CalculatePatternIntensity(pattern, u, v, dist, x, y, size);
                    pixels[y * size + x] = new Color(intensity, intensity, intensity, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }

        private static float CalculatePatternIntensity(BrushPattern pattern, float u, float v, float dist, int x, int y, int size)
        {
            switch (pattern)
            {
                case BrushPattern.SoftCircle:
                    return Mathf.Clamp01(1f - dist);

                case BrushPattern.HardCircle:
                    return dist <= 0.9f ? 1f : 0f;

                case BrushPattern.LinearGradient:
                    return Mathf.Clamp01((1f - v) * 0.5f) * (dist <= 1f ? 1f : 0f);

                case BrushPattern.RadialGradient:
                    return Mathf.Clamp01(1f - Mathf.Pow(dist, 0.5f));

                case BrushPattern.Noise:
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    return noise * Mathf.Clamp01(1f - dist);

                case BrushPattern.Splatter:
                    float splatterNoise = Mathf.PerlinNoise(x * 0.05f + 100f, y * 0.05f + 100f);
                    float threshold = 0.4f + dist * 0.3f;
                    return (splatterNoise > threshold && dist < 0.95f) ? 1f : 0f;

                case BrushPattern.Star:
                    float angle = Mathf.Atan2(v, u);
                    float starShape = Mathf.Abs(Mathf.Sin(angle * 5f));
                    float starRadius = 0.3f + starShape * 0.6f;
                    return dist < starRadius ? Mathf.Clamp01(1f - dist / starRadius) : 0f;

                case BrushPattern.Square:
                    float maxCoord = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
                    return maxCoord <= 0.8f ? 1f : Mathf.Clamp01((1f - maxCoord) * 5f);

                case BrushPattern.Diamond:
                    float diamondDist = Mathf.Abs(u) + Mathf.Abs(v);
                    return Mathf.Clamp01(1f - diamondDist);

                case BrushPattern.Ring:
                    float ringCenter = 0.6f;
                    float ringWidth = 0.25f;
                    float ringDist = Mathf.Abs(dist - ringCenter);
                    return ringDist < ringWidth ? Mathf.Clamp01(1f - ringDist / ringWidth) : 0f;

                case BrushPattern.Crescent:
                    float crescentOffset = 0.3f;
                    float dist2 = Mathf.Sqrt((u - crescentOffset) * (u - crescentOffset) + v * v);
                    float crescent = Mathf.Clamp01(1f - dist) - Mathf.Clamp01(1f - dist2 * 1.2f);
                    return Mathf.Max(0f, crescent);

                case BrushPattern.Gaussian:
                    float sigma = 0.4f;
                    return Mathf.Exp(-(dist * dist) / (2f * sigma * sigma));

                default:
                    return Mathf.Clamp01(1f - dist);
            }
        }

        /// <summary>
        /// Clears the cached brush textures.
        /// </summary>
        internal static void ClearCache()
        {
            if (s_CachedBrushes != null)
            {
                foreach (var kvp in s_CachedBrushes)
                {
                    if (kvp.Value != null)
                    {
                        Object.DestroyImmediate(kvp.Value);
                    }
                }
                s_CachedBrushes.Clear();
                s_CachedBrushes = null;
            }
        }

        /// <summary>
        /// Exports all default brush textures to the Content/ImageBrushes folder.
        /// This is useful for creating persistent brush texture assets.
        /// </summary>
        [MenuItem("Tools/Polybrush/Generate Default Brush Textures")]
        internal static void ExportDefaultBrushTextures()
        {
            string targetFolder = GetOrCreateBrushFolder();
            if (string.IsNullOrEmpty(targetFolder))
            {
                Debug.LogError("Failed to create brush textures folder.");
                return;
            }

            int count = 0;
            foreach (BrushPattern pattern in System.Enum.GetValues(typeof(BrushPattern)))
            {
                string fileName = GetBrushName(pattern).Replace(" ", "") + ".png";
                string assetPath = Path.Combine(targetFolder, fileName);
                
                Texture2D texture = GenerateBrushTexture(pattern, DefaultTextureSize);
                if (texture != null)
                {
                    byte[] pngData = texture.EncodeToPNG();
                    if (pngData != null)
                    {
                        File.WriteAllBytes(assetPath, pngData);
                        count++;
                    }
                    Object.DestroyImmediate(texture);
                }
            }

            AssetDatabase.Refresh();
            
            // Set import settings for all generated textures
            SetBrushTextureImportSettings(targetFolder);
            
            Debug.Log($"Generated {count} default brush textures in {targetFolder}");
        }

        private static string GetOrCreateBrushFolder()
        {
            // Try package path first
            if (Directory.Exists(BrushTexturesFolder))
            {
                return BrushTexturesFolder;
            }
            
            // Fall back to Assets path
            string assetsPath = Path.Combine(Application.dataPath, "Content/ImageBrushes");
            if (!Directory.Exists(assetsPath))
            {
                Directory.CreateDirectory(assetsPath);
            }
            
            return BrushTexturesFolderAlt;
        }

        private static void SetBrushTextureImportSettings(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.isReadable = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.sRGBTexture = false; // Linear for grayscale brushes
                    
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>
        /// Gets the names of all available brush patterns.
        /// </summary>
        internal static string[] GetBrushPatternNames()
        {
            var patterns = System.Enum.GetValues(typeof(BrushPattern));
            string[] names = new string[patterns.Length];
            
            int i = 0;
            foreach (BrushPattern pattern in patterns)
            {
                names[i++] = GetBrushName(pattern);
            }
            
            return names;
        }
    }
}
