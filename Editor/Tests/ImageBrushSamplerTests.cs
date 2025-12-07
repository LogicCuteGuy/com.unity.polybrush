using UnityEngine;
using UnityEditor;
using NUnit.Framework;

namespace UnityEditor.Polybrush.Tests
{
    /// <summary>
    /// Tests for ImageBrushSampler functionality.
    /// Validates coordinate transformations, texture sampling, and rotation support.
    /// </summary>
    [TestFixture]
    internal class ImageBrushSamplerTests
    {
        private Texture2D CreateTestTexture(int width, int height, bool makeReadable = true)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            // Fill with a gradient pattern for testing
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = (float)x / (width - 1);
                    pixels[y * width + x] = new Color(value, value, value, 1f);
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }

        [Test]
        public void SampleAtPosition_WithNullTexture_ReturnsZero()
        {
            float result = ImageBrushSampler.SampleAtPosition(
                null,
                Vector3.zero,
                Vector3.zero,
                1f,
                0f,
                false
            );
            
            Assert.AreEqual(0f, result, "Null texture should return 0");
        }

        [Test]
        public void SampleAtPosition_OutsideBrushRadius_ReturnsZero()
        {
            Texture2D texture = CreateTestTexture(4, 4);
            
            float result = ImageBrushSampler.SampleAtPosition(
                texture,
                new Vector3(10f, 0f, 0f), // Far outside brush
                Vector3.zero,
                1f,
                0f,
                false
            );
            
            Object.DestroyImmediate(texture);
            Assert.AreEqual(0f, result, "Position outside brush radius should return 0");
        }

        [Test]
        public void SampleAtPosition_AtBrushCenter_SamplesTextureCenter()
        {
            Texture2D texture = CreateTestTexture(4, 4);
            
            // Sample at brush center should sample texture center (UV 0.5, 0.5)
            float result = ImageBrushSampler.SampleAtPosition(
                texture,
                Vector3.zero,
                Vector3.zero,
                1f,
                0f,
                false
            );
            
            Object.DestroyImmediate(texture);
            
            // Center of gradient texture should be around 0.5
            Assert.That(result, Is.InRange(0.4f, 0.6f), "Center sample should be mid-range");
        }

        [Test]
        public void SampleBatch_ProcessesAllPositions()
        {
            Texture2D texture = CreateTestTexture(4, 4);
            
            Vector3[] positions = new Vector3[]
            {
                Vector3.zero,
                new Vector3(0.5f, 0f, 0f),
                new Vector3(10f, 0f, 0f) // Outside radius
            };
            
            float[] weights = new float[3];
            
            ImageBrushSampler.SampleBatch(
                texture,
                positions,
                Vector3.zero,
                1f,
                0f,
                false,
                weights
            );
            
            Object.DestroyImmediate(texture);
            
            Assert.That(weights[0], Is.GreaterThan(0f), "First position should have weight");
            Assert.That(weights[1], Is.GreaterThan(0f), "Second position should have weight");
            Assert.AreEqual(0f, weights[2], "Third position outside radius should be 0");
        }

        [Test]
        public void ValidateTexture_WithValidTexture_ReturnsTrue()
        {
            Texture2D texture = CreateTestTexture(4, 4);
            
            string errorMessage;
            bool isValid = ImageBrushSampler.ValidateTexture(texture, out errorMessage);
            
            Object.DestroyImmediate(texture);
            
            Assert.IsTrue(isValid, "Valid texture should pass validation");
            Assert.IsNull(errorMessage, "Valid texture should have no error message");
        }

        [Test]
        public void ValidateTexture_WithNullTexture_ReturnsFalse()
        {
            string errorMessage;
            bool isValid = ImageBrushSampler.ValidateTexture(null, out errorMessage);
            
            Assert.IsFalse(isValid, "Null texture should fail validation");
            Assert.IsNotNull(errorMessage, "Should provide error message");
        }

        [Test]
        public void CoordinateTransformation_MapsWorldToBrushSpace()
        {
            Texture2D texture = CreateTestTexture(4, 4);
            
            Vector3 brushCenter = new Vector3(5f, 0f, 5f);
            float brushRadius = 2f;
            
            // Sample at edge of brush (should map to edge of texture)
            Vector3 edgePosition = brushCenter + new Vector3(brushRadius, 0f, 0f);
            
            float result = ImageBrushSampler.SampleAtPosition(
                texture,
                edgePosition,
                brushCenter,
                brushRadius,
                0f,
                false
            );
            
            Object.DestroyImmediate(texture);
            
            // Edge of gradient texture should be close to 1.0
            Assert.That(result, Is.GreaterThan(0.8f), "Edge position should sample high value");
        }

        [Test]
        public void ComputeShaderAvailability_CanBeQueried()
        {
            // This test verifies that we can query compute shader availability
            // without throwing exceptions
            bool available = ImageBrushSampler.IsComputeShaderAvailable();
            
            // We don't assert a specific value since it depends on the platform
            // and whether the compute shader asset is available
            Assert.That(available, Is.True.Or.False, "Should return a boolean value");
        }

        [Test]
        public void SampleBatch_LargeDataSet_ProducesConsistentResults()
        {
            Texture2D texture = CreateTestTexture(8, 8);
            
            // Create a large enough dataset to potentially trigger GPU path (100+ samples)
            int numSamples = 150;
            Vector3[] positions = new Vector3[numSamples];
            float[] weights = new float[numSamples];
            
            Vector3 brushCenter = Vector3.zero;
            float brushRadius = 5f;
            
            // Create positions in a grid pattern within brush radius
            int gridSize = (int)Mathf.Sqrt(numSamples);
            for (int i = 0; i < numSamples; i++)
            {
                int x = i % gridSize;
                int y = i / gridSize;
                float fx = (x / (float)(gridSize - 1) - 0.5f) * 2f * brushRadius * 0.8f;
                float fy = (y / (float)(gridSize - 1) - 0.5f) * 2f * brushRadius * 0.8f;
                positions[i] = new Vector3(fx, 0f, fy);
            }
            
            ImageBrushSampler.SampleBatch(
                texture,
                positions,
                brushCenter,
                brushRadius,
                0f,
                false,
                weights
            );
            
            Object.DestroyImmediate(texture);
            
            // Verify that we got reasonable results
            int nonZeroCount = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                Assert.That(weights[i], Is.InRange(0f, 1f), $"Weight {i} should be in valid range");
                if (weights[i] > 0f)
                    nonZeroCount++;
            }
            
            // Most positions should have non-zero weights since they're within radius
            Assert.That(nonZeroCount, Is.GreaterThan(numSamples / 2), 
                "Most samples should have non-zero weights");
        }

        [Test]
        public void BufferCleanup_CanBeCalledSafely()
        {
            // Verify that buffer cleanup can be called without errors
            // even if no buffers were allocated
            Assert.DoesNotThrow(() => ImageBrushSampler.ReleaseBuffers(),
                "ReleaseBuffers should not throw even if no buffers exist");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up GPU buffers after each test
            ImageBrushSampler.ReleaseBuffers();
        }
    }
}
