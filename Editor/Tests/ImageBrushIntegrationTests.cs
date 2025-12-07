using NUnit.Framework;
using UnityEngine;
using UnityEditor.Polybrush;

namespace UnityEditor.Polybrush.Tests
{
    /// <summary>
    /// Integration tests for image brush functionality with sculpt modes.
    /// Tests the integration between ImageBrushSampler and weight calculation.
    /// </summary>
    [TestFixture]
    internal class ImageBrushIntegrationTests
    {
        private Texture2D CreateTestTexture(int width, int height, float centerValue, float edgeValue)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Create a gradient from center to edge
                    float dx = (x - width / 2f) / (width / 2f);
                    float dy = (y - height / 2f) / (height / 2f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float value = Mathf.Lerp(centerValue, edgeValue, Mathf.Clamp01(dist));
                    pixels[y * width + x] = new Color(value, value, value, 1f);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }

        [Test]
        public void ImageBrushSettings_IsValid_ReturnsTrueWhenEnabledWithReadableTexture()
        {
            // Arrange
            ImageBrushSettings settings = new ImageBrushSettings();
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            
            settings.enabled = true;
            settings.brushTexture = texture;
            
            // Act
            bool isValid = settings.IsValid();
            
            // Assert
            Assert.IsTrue(isValid, "ImageBrushSettings should be valid when enabled with a readable texture");
            
            // Cleanup
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void ImageBrushSettings_IsValid_ReturnsFalseWhenDisabled()
        {
            // Arrange
            ImageBrushSettings settings = new ImageBrushSettings();
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            
            settings.enabled = false;
            settings.brushTexture = texture;
            
            // Act
            bool isValid = settings.IsValid();
            
            // Assert
            Assert.IsFalse(isValid, "ImageBrushSettings should not be valid when disabled");
            
            // Cleanup
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void ImageBrushSettings_IsValid_ReturnsFalseWhenNoTexture()
        {
            // Arrange
            ImageBrushSettings settings = new ImageBrushSettings();
            settings.enabled = true;
            settings.brushTexture = null;
            
            // Act
            bool isValid = settings.IsValid();
            
            // Assert
            Assert.IsFalse(isValid, "ImageBrushSettings should not be valid when no texture is assigned");
        }

        [Test]
        public void BrushSettings_ImageBrushSettings_IsNotNull()
        {
            // Arrange
            BrushSettings settings = ScriptableObject.CreateInstance<BrushSettings>();
            settings.SetDefaultValues();
            
            // Act & Assert
            Assert.IsNotNull(settings.imageBrushSettings, "BrushSettings should have non-null imageBrushSettings");
            
            // Cleanup
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void BrushSettings_CopyTo_CopiesImageBrushSettings()
        {
            // Arrange
            BrushSettings source = ScriptableObject.CreateInstance<BrushSettings>();
            BrushSettings target = ScriptableObject.CreateInstance<BrushSettings>();
            
            source.SetDefaultValues();
            target.SetDefaultValues();
            
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            source.imageBrushSettings.enabled = true;
            source.imageBrushSettings.brushTexture = texture;
            source.imageBrushSettings.rotation = 45f;
            source.imageBrushSettings.preserveAspectRatio = false;
            
            // Act
            source.CopyTo(target);
            
            // Assert
            Assert.IsTrue(target.imageBrushSettings.enabled, "Image brush enabled should be copied");
            Assert.AreEqual(texture, target.imageBrushSettings.brushTexture, "Texture should be copied");
            Assert.AreEqual(45f, target.imageBrushSettings.rotation, 0.001f, "Rotation should be copied");
            Assert.IsFalse(target.imageBrushSettings.preserveAspectRatio, "Aspect ratio setting should be copied");
            
            // Cleanup
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void BrushSettings_SaveAndLoad_PreservesImageBrushTexture()
        {
            // Arrange
            string testPath = "Assets/TestBrushPreset.asset";
            
            // Create a brush settings with image brush
            BrushSettings original = ScriptableObject.CreateInstance<BrushSettings>();
            original.SetDefaultValues();
            
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            original.imageBrushSettings.enabled = true;
            original.imageBrushSettings.brushTexture = texture;
            original.imageBrushSettings.rotation = 90f;
            original.imageBrushSettings.preserveAspectRatio = false;
            original.imageBrushSettings.samplingMode = FilterMode.Point;
            
            try
            {
                // Act - Save the preset
                AssetDatabase.CreateAsset(original, testPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // Load the preset
                BrushSettings loaded = AssetDatabase.LoadAssetAtPath<BrushSettings>(testPath);
                
                // Assert
                Assert.IsNotNull(loaded, "Loaded brush settings should not be null");
                Assert.IsNotNull(loaded.imageBrushSettings, "Image brush settings should not be null");
                Assert.IsTrue(loaded.imageBrushSettings.enabled, "Image brush should be enabled");
                Assert.AreEqual(texture, loaded.imageBrushSettings.brushTexture, "Texture reference should be preserved");
                Assert.AreEqual(90f, loaded.imageBrushSettings.rotation, 0.001f, "Rotation should be preserved");
                Assert.IsFalse(loaded.imageBrushSettings.preserveAspectRatio, "Aspect ratio setting should be preserved");
                Assert.AreEqual(FilterMode.Point, loaded.imageBrushSettings.samplingMode, "Sampling mode should be preserved");
            }
            finally
            {
                // Cleanup
                if (AssetDatabase.LoadAssetAtPath<BrushSettings>(testPath) != null)
                {
                    AssetDatabase.DeleteAsset(testPath);
                }
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void BrushSettings_LoadWithMissingTexture_HandlesGracefully()
        {
            // Arrange
            string testPath = "Assets/TestBrushPresetMissing.asset";
            string texturePath = "Assets/TestTextureMissing.asset";
            
            // Create a brush settings with image brush
            BrushSettings original = ScriptableObject.CreateInstance<BrushSettings>();
            original.SetDefaultValues();
            
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            
            try
            {
                // Save texture as asset
                AssetDatabase.CreateAsset(texture, texturePath);
                AssetDatabase.SaveAssets();
                
                original.imageBrushSettings.enabled = true;
                original.imageBrushSettings.brushTexture = texture;
                original.imageBrushSettings.rotation = 45f;
                
                // Save the preset
                AssetDatabase.CreateAsset(original, testPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // Delete the texture to simulate missing reference
                AssetDatabase.DeleteAsset(texturePath);
                AssetDatabase.Refresh();
                
                // Act - Load the preset with missing texture
                BrushSettings loaded = AssetDatabase.LoadAssetAtPath<BrushSettings>(testPath);
                
                // Assert
                Assert.IsNotNull(loaded, "Loaded brush settings should not be null");
                Assert.IsNotNull(loaded.imageBrushSettings, "Image brush settings should not be null");
                Assert.IsTrue(loaded.imageBrushSettings.enabled, "Image brush should still be enabled");
                // Use Unity's null check (== null) instead of Assert.IsNull because Unity's 
                // serialization keeps a "missing" reference that's not C# null but evaluates to null
                Assert.IsTrue(loaded.imageBrushSettings.brushTexture == null, "Texture reference should be null when missing");
                Assert.IsFalse(loaded.imageBrushSettings.IsValid(), "Image brush should not be valid without texture");
                Assert.AreEqual(45f, loaded.imageBrushSettings.rotation, 0.001f, "Other settings should be preserved");
            }
            finally
            {
                // Cleanup
                if (AssetDatabase.LoadAssetAtPath<BrushSettings>(testPath) != null)
                {
                    AssetDatabase.DeleteAsset(testPath);
                }
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath) != null)
                {
                    AssetDatabase.DeleteAsset(texturePath);
                }
            }
        }

        [Test]
        public void BrushSettings_DeepCopy_PreservesImageBrushSettings()
        {
            // Arrange
            BrushSettings original = ScriptableObject.CreateInstance<BrushSettings>();
            original.SetDefaultValues();
            
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            original.imageBrushSettings.enabled = true;
            original.imageBrushSettings.brushTexture = texture;
            original.imageBrushSettings.rotation = 180f;
            original.imageBrushSettings.preserveAspectRatio = false;
            original.imageBrushSettings.samplingMode = FilterMode.Trilinear;
            
            // Act
            BrushSettings copy = original.DeepCopy();
            
            // Assert
            Assert.IsNotNull(copy, "Deep copy should not be null");
            Assert.IsNotNull(copy.imageBrushSettings, "Image brush settings should not be null");
            Assert.IsTrue(copy.imageBrushSettings.enabled, "Image brush should be enabled");
            Assert.AreEqual(texture, copy.imageBrushSettings.brushTexture, "Texture reference should be copied");
            Assert.AreEqual(180f, copy.imageBrushSettings.rotation, 0.001f, "Rotation should be copied");
            Assert.IsFalse(copy.imageBrushSettings.preserveAspectRatio, "Aspect ratio setting should be copied");
            Assert.AreEqual(FilterMode.Trilinear, copy.imageBrushSettings.samplingMode, "Sampling mode should be copied");
            
            // Verify it's a separate instance
            copy.imageBrushSettings.rotation = 270f;
            Assert.AreEqual(180f, original.imageBrushSettings.rotation, 0.001f, "Original should not be affected by changes to copy");
            
            // Cleanup
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(original);
            Object.DestroyImmediate(copy);
        }

        [Test]
        public void ImageBrushSampler_ValidateTexture_AcceptsValidTexture()
        {
            // Arrange
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            
            // Act
            string errorMessage;
            bool isValid = ImageBrushSampler.ValidateTexture(texture, out errorMessage);
            
            // Assert
            Assert.IsTrue(isValid, "Valid texture should pass validation");
            Assert.IsNull(errorMessage, "Error message should be null for valid texture");
            
            // Cleanup
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void ImageBrushSampler_ValidateTexture_RejectsNullTexture()
        {
            // Act
            string errorMessage;
            bool isValid = ImageBrushSampler.ValidateTexture(null, out errorMessage);
            
            // Assert
            Assert.IsFalse(isValid, "Null texture should fail validation");
            Assert.IsNotNull(errorMessage, "Error message should be provided for null texture");
        }

        [Test]
        public void ImageBrushSampler_SampleAtPosition_ReturnsZeroOutsideBrushRadius()
        {
            // Arrange
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            Vector3 brushCenter = Vector3.zero;
            float brushRadius = 1f;
            Vector3 positionOutside = new Vector3(5f, 0f, 0f); // Far outside radius
            
            // Act
            float weight = ImageBrushSampler.SampleAtPosition(
                texture,
                positionOutside,
                brushCenter,
                brushRadius,
                0f,
                true
            );
            
            // Assert
            Assert.AreEqual(0f, weight, 0.001f, "Weight should be zero outside brush radius");
            
            // Cleanup
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void ImageBrushSampler_SampleAtPosition_ReturnsNonZeroInsideBrushRadius()
        {
            // Arrange
            Texture2D texture = CreateTestTexture(32, 32, 1f, 0f);
            Vector3 brushCenter = Vector3.zero;
            float brushRadius = 1f;
            Vector3 positionInside = new Vector3(0.1f, 0f, 0.1f); // Inside radius
            
            // Act
            float weight = ImageBrushSampler.SampleAtPosition(
                texture,
                positionInside,
                brushCenter,
                brushRadius,
                0f,
                true
            );
            
            // Assert
            Assert.Greater(weight, 0f, "Weight should be greater than zero inside brush radius");
            
            // Cleanup
            Object.DestroyImmediate(texture);
        }
    }
}
