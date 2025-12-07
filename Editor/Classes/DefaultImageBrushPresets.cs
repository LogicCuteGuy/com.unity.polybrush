using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Manages default image brush presets for Polybrush.
    /// Provides pre-configured brush settings with various image brush patterns.
    /// </summary>
    internal static class DefaultImageBrushPresets
    {
        private const string PresetsFolder = "Assets/Polybrush Data/Brush Settings";
        
        /// <summary>
        /// Preset configuration for an image brush.
        /// </summary>
        internal struct PresetConfig
        {
            public string name;
            public DefaultImageBrushLibrary.BrushPattern pattern;
            public float radius;
            public float falloff;
            public float strength;
            public float rotation;
            public bool preserveAspectRatio;

            public PresetConfig(
                string name,
                DefaultImageBrushLibrary.BrushPattern pattern,
                float radius = 1f,
                float falloff = 0.5f,
                float strength = 0.5f,
                float rotation = 0f,
                bool preserveAspectRatio = true)
            {
                this.name = name;
                this.pattern = pattern;
                this.radius = radius;
                this.falloff = falloff;
                this.strength = strength;
                this.rotation = rotation;
                this.preserveAspectRatio = preserveAspectRatio;
            }
        }

        /// <summary>
        /// Gets the default preset configurations.
        /// </summary>
        internal static PresetConfig[] GetDefaultPresetConfigs()
        {
            return new PresetConfig[]
            {
                new PresetConfig("Soft Sculpt", DefaultImageBrushLibrary.BrushPattern.SoftCircle, 1f, 0.5f, 0.3f),
                new PresetConfig("Hard Edge", DefaultImageBrushLibrary.BrushPattern.HardCircle, 0.8f, 0.8f, 0.5f),
                new PresetConfig("Gaussian Smooth", DefaultImageBrushLibrary.BrushPattern.Gaussian, 1.2f, 0.3f, 0.4f),
                new PresetConfig("Noise Detail", DefaultImageBrushLibrary.BrushPattern.Noise, 0.6f, 0.4f, 0.3f),
                new PresetConfig("Splatter Effect", DefaultImageBrushLibrary.BrushPattern.Splatter, 1.5f, 0.6f, 0.4f),
                new PresetConfig("Star Pattern", DefaultImageBrushLibrary.BrushPattern.Star, 1f, 0.5f, 0.5f),
                new PresetConfig("Square Stamp", DefaultImageBrushLibrary.BrushPattern.Square, 0.8f, 0.7f, 0.5f),
                new PresetConfig("Diamond Stamp", DefaultImageBrushLibrary.BrushPattern.Diamond, 0.9f, 0.5f, 0.5f),
                new PresetConfig("Ring Brush", DefaultImageBrushLibrary.BrushPattern.Ring, 1f, 0.4f, 0.4f),
                new PresetConfig("Crescent Moon", DefaultImageBrushLibrary.BrushPattern.Crescent, 1.2f, 0.5f, 0.4f),
                new PresetConfig("Radial Falloff", DefaultImageBrushLibrary.BrushPattern.RadialGradient, 1f, 0.3f, 0.5f),
                new PresetConfig("Linear Gradient", DefaultImageBrushLibrary.BrushPattern.LinearGradient, 1f, 0.5f, 0.4f, 0f)
            };
        }

        /// <summary>
        /// Creates a BrushSettings instance configured with the specified preset.
        /// </summary>
        internal static BrushSettings CreatePreset(PresetConfig config)
        {
            BrushSettings settings = ScriptableObject.CreateInstance<BrushSettings>();
            settings.name = config.name;
            settings.radius = config.radius;
            settings.falloff = config.falloff;
            settings.strength = config.strength;

            // Configure image brush settings
            settings.imageBrushSettings.enabled = true;
            settings.imageBrushSettings.brushTexture = DefaultImageBrushLibrary.GetBrushTexture(config.pattern);
            settings.imageBrushSettings.rotation = config.rotation;
            settings.imageBrushSettings.preserveAspectRatio = config.preserveAspectRatio;
            settings.imageBrushSettings.samplingMode = FilterMode.Bilinear;

            return settings;
        }

        /// <summary>
        /// Creates all default presets as ScriptableObject assets.
        /// </summary>
        [MenuItem("Tools/Polybrush/Create Default Image Brush Presets")]
        internal static void CreateDefaultPresetAssets()
        {
            // Ensure the presets folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Polybrush Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Polybrush Data");
            }
            if (!AssetDatabase.IsValidFolder(PresetsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Polybrush Data", "Brush Settings");
            }

            // First generate the brush textures
            DefaultImageBrushLibrary.ExportDefaultBrushTextures();
            AssetDatabase.Refresh();

            int count = 0;
            foreach (var config in GetDefaultPresetConfigs())
            {
                string assetPath = $"{PresetsFolder}/{config.name}.asset";
                
                // Check if preset already exists
                BrushSettings existing = AssetDatabase.LoadAssetAtPath<BrushSettings>(assetPath);
                if (existing != null)
                {
                    Debug.Log($"Preset '{config.name}' already exists, skipping.");
                    continue;
                }

                BrushSettings preset = CreatePreset(config);
                AssetDatabase.CreateAsset(preset, assetPath);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created {count} default image brush presets in {PresetsFolder}");
        }

        /// <summary>
        /// Gets a list of runtime-generated preset instances (not saved as assets).
        /// Useful for providing default options in the UI without requiring asset creation.
        /// </summary>
        internal static List<BrushSettings> GetRuntimePresets()
        {
            var presets = new List<BrushSettings>();
            
            foreach (var config in GetDefaultPresetConfigs())
            {
                presets.Add(CreatePreset(config));
            }
            
            return presets;
        }

        /// <summary>
        /// Applies a preset configuration to existing brush settings.
        /// </summary>
        internal static void ApplyPreset(BrushSettings target, PresetConfig config)
        {
            if (target == null)
                return;

            target.radius = config.radius;
            target.falloff = config.falloff;
            target.strength = config.strength;

            target.imageBrushSettings.enabled = true;
            target.imageBrushSettings.brushTexture = DefaultImageBrushLibrary.GetBrushTexture(config.pattern);
            target.imageBrushSettings.rotation = config.rotation;
            target.imageBrushSettings.preserveAspectRatio = config.preserveAspectRatio;
        }

        /// <summary>
        /// Gets the preset configuration names for UI display.
        /// </summary>
        internal static string[] GetPresetNames()
        {
            var configs = GetDefaultPresetConfigs();
            string[] names = new string[configs.Length];
            
            for (int i = 0; i < configs.Length; i++)
            {
                names[i] = configs[i].name;
            }
            
            return names;
        }
    }
}
