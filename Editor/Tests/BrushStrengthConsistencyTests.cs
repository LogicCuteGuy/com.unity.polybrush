using NUnit.Framework;
using UnityEngine;
using UnityEditor.Polybrush;

namespace UnityEditor.PolybrushTests
{
    /// <summary>
    /// Tests to validate consistent strength behavior across all brush modes.
    /// Validates Requirement 2.7: Consistent strength across modes
    /// </summary>
    internal class BrushStrengthConsistencyTests
    {
        [Test]
        public void StrengthModifiers_AreValid()
        {
            // Validate that all strength modifiers are positive and within range [0, 1]
            Assert.Greater(BrushStrengthUtility.SCULPT_STRENGTH_MODIFIER, 0f);
            Assert.LessOrEqual(BrushStrengthUtility.SCULPT_STRENGTH_MODIFIER, 1f);
            Assert.Greater(BrushStrengthUtility.SMOOTH_STRENGTH_MODIFIER, 0f);
            Assert.LessOrEqual(BrushStrengthUtility.SMOOTH_STRENGTH_MODIFIER, 1f);
            Assert.Greater(BrushStrengthUtility.PAINT_STRENGTH_MODIFIER, 0f);
            Assert.LessOrEqual(BrushStrengthUtility.PAINT_STRENGTH_MODIFIER, 1f);
            Assert.Greater(BrushStrengthUtility.TEXTURE_STRENGTH_MODIFIER, 0f);
            Assert.LessOrEqual(BrushStrengthUtility.TEXTURE_STRENGTH_MODIFIER, 1f);
            Assert.Greater(BrushStrengthUtility.TOPOLOGY_STRENGTH_MODIFIER, 0f);
            Assert.LessOrEqual(BrushStrengthUtility.TOPOLOGY_STRENGTH_MODIFIER, 1f);
        }

        [Test]
        public void StrengthModifiers_ArePositive()
        {
            // All modifiers must be positive
            Assert.Greater(BrushStrengthUtility.SCULPT_STRENGTH_MODIFIER, 0f,
                "Sculpt strength modifier must be positive");
            Assert.Greater(BrushStrengthUtility.SMOOTH_STRENGTH_MODIFIER, 0f,
                "Smooth strength modifier must be positive");
            Assert.Greater(BrushStrengthUtility.PAINT_STRENGTH_MODIFIER, 0f,
                "Paint strength modifier must be positive");
            Assert.Greater(BrushStrengthUtility.TEXTURE_STRENGTH_MODIFIER, 0f,
                "Texture strength modifier must be positive");
            Assert.Greater(BrushStrengthUtility.TOPOLOGY_STRENGTH_MODIFIER, 0f,
                "Topology strength modifier must be positive");
        }

        [Test]
        public void StrengthModifiers_AreWithinRange()
        {
            // All modifiers should be <= 1.0 for consistency
            Assert.LessOrEqual(BrushStrengthUtility.SCULPT_STRENGTH_MODIFIER, 1f,
                "Sculpt strength modifier should be <= 1.0");
            Assert.LessOrEqual(BrushStrengthUtility.SMOOTH_STRENGTH_MODIFIER, 1f,
                "Smooth strength modifier should be <= 1.0");
            Assert.LessOrEqual(BrushStrengthUtility.PAINT_STRENGTH_MODIFIER, 1f,
                "Paint strength modifier should be <= 1.0");
            Assert.LessOrEqual(BrushStrengthUtility.TEXTURE_STRENGTH_MODIFIER, 1f,
                "Texture strength modifier should be <= 1.0");
            Assert.LessOrEqual(BrushStrengthUtility.TOPOLOGY_STRENGTH_MODIFIER, 1f,
                "Topology strength modifier should be <= 1.0");
        }

        [Test]
        public void StrengthModifiers_HaveCorrectRelativeProportions()
        {
            // Sculpt should be the most subtle (smallest modifier for fine control)
            Assert.LessOrEqual(BrushStrengthUtility.SCULPT_STRENGTH_MODIFIER,
                BrushStrengthUtility.TOPOLOGY_STRENGTH_MODIFIER,
                "Sculpt modifier should be <= Topology modifier (most subtle for fine control)");

            // Sculpt should be more subtle than smooth
            Assert.LessOrEqual(BrushStrengthUtility.SCULPT_STRENGTH_MODIFIER,
                BrushStrengthUtility.SMOOTH_STRENGTH_MODIFIER,
                "Sculpt modifier should be <= Smooth modifier");
        }

        [Test]
        public void GetSculptStrength_ReturnsConsistentValues()
        {
            float baseStrength = 0.5f;
            float additionalStrength = 2.0f;

            float result = BrushStrengthUtility.GetSculptStrength(baseStrength, additionalStrength);

            // Should equal baseStrength * modifier * additionalStrength
            float expected = baseStrength * BrushStrengthUtility.SCULPT_STRENGTH_MODIFIER * additionalStrength;
            Assert.AreEqual(expected, result, 0.0001f,
                "GetSculptStrength should return baseStrength * modifier * additionalStrength");
        }

        [Test]
        public void GetSmoothStrength_ReturnsConsistentValues()
        {
            float baseStrength = 0.5f;

            float result = BrushStrengthUtility.GetSmoothStrength(baseStrength);

            // Should equal baseStrength * modifier
            float expected = baseStrength * BrushStrengthUtility.SMOOTH_STRENGTH_MODIFIER;
            Assert.AreEqual(expected, result, 0.0001f,
                "GetSmoothStrength should return baseStrength * modifier");
        }

        [Test]
        public void GetPaintStrength_ReturnsConsistentValues()
        {
            float baseStrength = 0.5f;

            float result = BrushStrengthUtility.GetPaintStrength(baseStrength);

            // Should equal baseStrength * modifier
            float expected = baseStrength * BrushStrengthUtility.PAINT_STRENGTH_MODIFIER;
            Assert.AreEqual(expected, result, 0.0001f,
                "GetPaintStrength should return baseStrength * modifier");
        }

        [Test]
        public void GetTextureStrength_ReturnsConsistentValues()
        {
            float baseStrength = 0.5f;

            float result = BrushStrengthUtility.GetTextureStrength(baseStrength);

            // Should equal baseStrength * modifier
            float expected = baseStrength * BrushStrengthUtility.TEXTURE_STRENGTH_MODIFIER;
            Assert.AreEqual(expected, result, 0.0001f,
                "GetTextureStrength should return baseStrength * modifier");
        }

        [Test]
        public void GetTopologyStrength_ReturnsConsistentValues()
        {
            float baseStrength = 0.5f;

            float result = BrushStrengthUtility.GetTopologyStrength(baseStrength);

            // Should equal baseStrength * modifier
            float expected = baseStrength * BrushStrengthUtility.TOPOLOGY_STRENGTH_MODIFIER;
            Assert.AreEqual(expected, result, 0.0001f,
                "GetTopologyStrength should return baseStrength * modifier");
        }

        [Test]
        public void GetPrefabPlacementInterval_InvertsStrength()
        {
            // Higher strength should result in shorter interval (more frequent placement)
            float lowStrength = 0.2f;
            float highStrength = 0.8f;

            float lowInterval = BrushStrengthUtility.GetPrefabPlacementInterval(lowStrength);
            float highInterval = BrushStrengthUtility.GetPrefabPlacementInterval(highStrength);

            Assert.Greater(lowInterval, highInterval,
                "Lower strength should result in longer interval (less frequent placement)");
        }

        [Test]
        public void GetPrefabPlacementInterval_HasMinimumValue()
        {
            // Even at maximum strength, there should be a minimum interval
            float maxStrength = 1.0f;

            float interval = BrushStrengthUtility.GetPrefabPlacementInterval(maxStrength);

            Assert.GreaterOrEqual(interval, 0.06f,
                "Placement interval should have a minimum value to prevent performance issues");
        }

        [Test]
        public void GetStrengthDescription_ReturnsValidDescriptions()
        {
            // Test all strength ranges
            string subtleDesc = BrushStrengthUtility.GetStrengthDescription(0.1f);
            string moderateDesc = BrushStrengthUtility.GetStrengthDescription(0.4f);
            string strongDesc = BrushStrengthUtility.GetStrengthDescription(0.7f);
            string maxDesc = BrushStrengthUtility.GetStrengthDescription(0.9f);

            Assert.IsNotEmpty(subtleDesc, "Should return description for subtle strength");
            Assert.IsNotEmpty(moderateDesc, "Should return description for moderate strength");
            Assert.IsNotEmpty(strongDesc, "Should return description for strong strength");
            Assert.IsNotEmpty(maxDesc, "Should return description for maximum strength");

            // Descriptions should be different
            Assert.AreNotEqual(subtleDesc, moderateDesc, "Different strength levels should have different descriptions");
            Assert.AreNotEqual(moderateDesc, strongDesc, "Different strength levels should have different descriptions");
            Assert.AreNotEqual(strongDesc, maxDesc, "Different strength levels should have different descriptions");
        }

        [Test]
        public void GetStrengthDescription_IncludesModeName()
        {
            string modeName = "Test Mode";
            string description = BrushStrengthUtility.GetStrengthDescription(0.5f, modeName);

            Assert.IsTrue(description.Contains(modeName),
                "Description should include the mode name when provided");
        }

        [Test]
        public void StrengthCalculations_AreProportional()
        {
            // For the same base strength, different modes should produce proportional results
            float baseStrength = 0.5f;

            float sculptStrength = BrushStrengthUtility.GetSculptStrength(baseStrength, 1f);
            float smoothStrength = BrushStrengthUtility.GetSmoothStrength(baseStrength);

            // The ratio should match the ratio of modifiers
            float sculptRatio = sculptStrength / baseStrength;
            float smoothRatio = smoothStrength / baseStrength;

            Assert.AreEqual(BrushStrengthUtility.SCULPT_STRENGTH_MODIFIER, sculptRatio, 0.0001f,
                "Sculpt strength ratio should match modifier");
            Assert.AreEqual(BrushStrengthUtility.SMOOTH_STRENGTH_MODIFIER, smoothRatio, 0.0001f,
                "Smooth strength ratio should match modifier");
        }

        [Test]
        public void StrengthCalculations_HandleZeroStrength()
        {
            // All calculations should handle zero strength gracefully
            Assert.AreEqual(0f, BrushStrengthUtility.GetSculptStrength(0f), 0.0001f);
            Assert.AreEqual(0f, BrushStrengthUtility.GetSmoothStrength(0f), 0.0001f);
            Assert.AreEqual(0f, BrushStrengthUtility.GetPaintStrength(0f), 0.0001f);
            Assert.AreEqual(0f, BrushStrengthUtility.GetTextureStrength(0f), 0.0001f);
            Assert.AreEqual(0f, BrushStrengthUtility.GetTopologyStrength(0f), 0.0001f);
        }

        [Test]
        public void StrengthCalculations_HandleMaxStrength()
        {
            // All calculations should handle maximum strength gracefully
            float maxStrength = 1.0f;

            Assert.Greater(BrushStrengthUtility.GetSculptStrength(maxStrength), 0f);
            Assert.Greater(BrushStrengthUtility.GetSmoothStrength(maxStrength), 0f);
            Assert.Greater(BrushStrengthUtility.GetPaintStrength(maxStrength), 0f);
            Assert.Greater(BrushStrengthUtility.GetTextureStrength(maxStrength), 0f);
            Assert.Greater(BrushStrengthUtility.GetTopologyStrength(maxStrength), 0f);

            // All should be <= 1.0 (since modifiers are <= 1.0)
            Assert.LessOrEqual(BrushStrengthUtility.GetSculptStrength(maxStrength, 1f), 1f);
            Assert.LessOrEqual(BrushStrengthUtility.GetSmoothStrength(maxStrength), 1f);
            Assert.LessOrEqual(BrushStrengthUtility.GetPaintStrength(maxStrength), 1f);
            Assert.LessOrEqual(BrushStrengthUtility.GetTextureStrength(maxStrength), 1f);
            Assert.LessOrEqual(BrushStrengthUtility.GetTopologyStrength(maxStrength), 1f);
        }
    }
}
