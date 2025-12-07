using UnityEngine;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Utility class for consistent strength parameter application across all brush modes.
    /// Ensures that strength values produce proportional effects regardless of brush mode.
    /// </summary>
    internal static class BrushStrengthUtility
    {
        /// <summary>
        /// Standard strength modifier for sculpting operations (raise/lower, smooth).
        /// This provides a baseline for vertex displacement per brush stroke.
        /// </summary>
        public const float SCULPT_STRENGTH_MODIFIER = 0.01f;

        /// <summary>
        /// Strength modifier for smooth operations.
        /// Smoothing requires more subtle application to avoid over-smoothing.
        /// </summary>
        public const float SMOOTH_STRENGTH_MODIFIER = 0.1f;

        /// <summary>
        /// Strength modifier for color painting operations.
        /// Controls how quickly colors blend toward the target.
        /// </summary>
        public const float PAINT_STRENGTH_MODIFIER = 0.125f; // 1/8

        /// <summary>
        /// Strength modifier for texture blending operations.
        /// Texture weights blend at a similar rate to color painting.
        /// </summary>
        public const float TEXTURE_STRENGTH_MODIFIER = 0.125f; // 1/8

        /// <summary>
        /// Strength modifier for topology operations (subdivision/unsubdivision).
        /// Topology changes should be more controlled to prevent mesh corruption.
        /// </summary>
        public const float TOPOLOGY_STRENGTH_MODIFIER = 0.05f;

        /// <summary>
        /// Calculate the effective strength for sculpting operations (raise/lower).
        /// </summary>
        /// <param name="baseStrength">The base strength value from brush settings (0-1)</param>
        /// <param name="additionalStrength">Additional strength multiplier (e.g., from mode-specific settings)</param>
        /// <returns>The effective strength value to apply</returns>
        public static float GetSculptStrength(float baseStrength, float additionalStrength = 1f)
        {
            return baseStrength * SCULPT_STRENGTH_MODIFIER * additionalStrength;
        }

        /// <summary>
        /// Calculate the effective strength for smooth operations.
        /// </summary>
        /// <param name="baseStrength">The base strength value from brush settings (0-1)</param>
        /// <returns>The effective strength value to apply</returns>
        public static float GetSmoothStrength(float baseStrength)
        {
            return baseStrength * SMOOTH_STRENGTH_MODIFIER;
        }

        /// <summary>
        /// Calculate the effective strength for color painting operations.
        /// </summary>
        /// <param name="baseStrength">The base strength value from brush settings (0-1)</param>
        /// <returns>The effective strength value to apply</returns>
        public static float GetPaintStrength(float baseStrength)
        {
            return baseStrength * PAINT_STRENGTH_MODIFIER;
        }

        /// <summary>
        /// Calculate the effective strength for texture blending operations.
        /// </summary>
        /// <param name="baseStrength">The base strength value from brush settings (0-1)</param>
        /// <returns>The effective strength value to apply</returns>
        public static float GetTextureStrength(float baseStrength)
        {
            return baseStrength * TEXTURE_STRENGTH_MODIFIER;
        }

        /// <summary>
        /// Calculate the effective strength for topology operations.
        /// </summary>
        /// <param name="baseStrength">The base strength value from brush settings (0-1)</param>
        /// <returns>The effective strength value to apply</returns>
        public static float GetTopologyStrength(float baseStrength)
        {
            return baseStrength * TOPOLOGY_STRENGTH_MODIFIER;
        }

        /// <summary>
        /// Get a user-friendly description of the current strength level.
        /// This provides consistent feedback across all brush modes.
        /// </summary>
        /// <param name="strength">The strength value (0-1)</param>
        /// <param name="modeName">Optional name of the brush mode for context</param>
        /// <returns>A descriptive string explaining the strength level</returns>
        public static string GetStrengthDescription(float strength, string modeName = "")
        {
            string prefix = string.IsNullOrEmpty(modeName) ? "Strength" : modeName + " Strength";

            if (strength < 0.2f)
                return prefix + ": Subtle - Creates gentle, fine-tuned adjustments.";
            else if (strength < 0.5f)
                return prefix + ": Moderate - Produces noticeable effects with good control.";
            else if (strength < 0.8f)
                return prefix + ": Strong - Creates significant effects.";
            else
                return prefix + ": Maximum - Produces dramatic effects. Use carefully.";
        }

        /// <summary>
        /// Validate that strength modifiers are consistent across modes.
        /// This is used for testing to ensure strength behavior remains normalized.
        /// </summary>
        /// <returns>True if all modifiers are within expected ranges</returns>
        public static bool ValidateStrengthModifiers()
        {
            // Ensure all modifiers are positive and within reasonable ranges
            bool valid = true;

            valid &= SCULPT_STRENGTH_MODIFIER > 0f && SCULPT_STRENGTH_MODIFIER <= 1f;
            valid &= SMOOTH_STRENGTH_MODIFIER > 0f && SMOOTH_STRENGTH_MODIFIER <= 1f;
            valid &= PAINT_STRENGTH_MODIFIER > 0f && PAINT_STRENGTH_MODIFIER <= 1f;
            valid &= TEXTURE_STRENGTH_MODIFIER > 0f && TEXTURE_STRENGTH_MODIFIER <= 1f;
            valid &= TOPOLOGY_STRENGTH_MODIFIER > 0f && TOPOLOGY_STRENGTH_MODIFIER <= 1f;

            // Ensure relative proportions make sense
            // Topology should be most conservative, sculpt should be subtle, smooth should be moderate
            valid &= TOPOLOGY_STRENGTH_MODIFIER <= SCULPT_STRENGTH_MODIFIER;
            valid &= SCULPT_STRENGTH_MODIFIER <= SMOOTH_STRENGTH_MODIFIER;

            return valid;
        }

        /// <summary>
        /// Calculate the application frequency for prefab placement based on strength.
        /// Higher strength = more frequent placement.
        /// </summary>
        /// <param name="baseStrength">The base strength value from brush settings (0-1)</param>
        /// <returns>The time interval between applications in seconds</returns>
        public static float GetPrefabPlacementInterval(float baseStrength)
        {
            // Invert strength so higher strength = shorter interval = more frequent placement
            return Mathf.Max(0.06f, 1f - baseStrength);
        }
    }
}
