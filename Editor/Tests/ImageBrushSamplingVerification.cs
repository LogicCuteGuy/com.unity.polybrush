using UnityEngine;
using UnityEditor;
using NUnit.Framework;

namespace UnityEditor.Polybrush.Tests
{
    /// <summary>
    /// Verification tests for Task 2: Image brush sampling logic implementation.
    /// Tests CPU-based texture sampling, coordinate transformations, and rotation support.
    /// </summary>
    [TestFixture]
    internal class ImageBrushSamplingVerification
    {
        private Texture2D CreateGradientTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
        public void Task2_CPUBasedTextureSampling_WorksCorrectly()
        {
            // Requirement 1.2: Sample grayscale image values to determine vertex displacement weights
            Texture2D texture = CreateGradientTexture(8, 8);
            
            Vector3 brushCenter = Vector3.zero;
            float brushRadius = 5f;
            
            // Sample at various positions
            Vector3 centerPos = brushCenter;
            Vector3 rightPos = brushCenter + new Vector3(2.5f, 0f, 0f);
            Vector3 leftPos = brushCenter + new Vector3(-2.5f, 0f, 0f);
            
            float centerSample = ImageBrushSampler.SampleAtPosition(
                texture, centerPos, brushCenter, brushRadius, 0f, false);
            float rightSample = ImageBrushSampler.SampleAtPosition(
                texture, rightPos, brushCenter, brushRadius, 0f, false);
            float leftSample = ImageBrushSampler.SampleAtPosition(
                texture, leftPos, brushCenter, brushRadius, 0f, false);
            
            Object.DestroyImmediate(texture);
            
            // Verify sampling works
            Assert.That(centerSample, Is.InRange(0.4f, 0.6f), "Center should sample middle of gradient");
            Assert.That(rightSample, Is.GreaterThan(centerSample), "Right should sample higher value");
            Assert.That(leftSample, Is.LessThan(centerSample), "Left should sample lower value");
        }

        [Test]
        public void Task2_CoordinateTransformation_WorldToBrushToTextureSpace()
        {
            // Requirement 1.3: Map texture coordinates to brush radius in world space
            Texture2D texture = CreateGradientTexture(8, 8);
            
            Vector3 brushCenter = new Vector3(10f, 0f, 10f);
            float brushRadius = 3f;
            
            // Test transformation at different world positions
            Vector3 worldPosLeft = brushCenter + new Vector3(-brushRadius, 0f, 0f);
            Vector3 worldPosRight = brushCenter + new Vector3(brushRadius, 0f, 0f);
            Vector3 worldPosCenter = brushCenter;
            
            float leftSample = ImageBrushSampler.SampleAtPosition(
                texture, worldPosLeft, brushCenter, brushRadius, 0f, false);
            float rightSample = ImageBrushSampler.SampleAtPosition(
                texture, worldPosRight, brushCenter, brushRadius, 0f, false);
            float centerSample = ImageBrushSampler.SampleAtPosition(
                texture, worldPosCenter, brushCenter, brushRadius, 0f, false);
            
            Object.DestroyImmediate(texture);
            
            // Verify coordinate transformation maps correctly
            Assert.That(leftSample, Is.LessThan(0.2f), "Left edge should map to low texture values");
            Assert.That(rightSample, Is.GreaterThan(0.8f), "Right edge should map to high texture values");
            Assert.That(centerSample, Is.InRange(0.4f, 0.6f), "Center should map to middle texture values");
        }

        [Test]
        public void Task2_RotationSupport_TransformsSamplingCoordinates()
        {
            // Requirement 1.4: Apply image texture at specified rotation angle
            Texture2D texture = CreateGradientTexture(8, 8);
            
            Vector3 brushCenter = Vector3.zero;
            float brushRadius = 5f;
            Vector3 testPos = brushCenter + new Vector3(2f, 0f, 0f);
            
            // Sample at same position with different rotations
            float sample0deg = ImageBrushSampler.SampleAtPosition(
                texture, testPos, brushCenter, brushRadius, 0f, false);
            float sample90deg = ImageBrushSampler.SampleAtPosition(
                texture, testPos, brushCenter, brushRadius, 90f, false);
            float sample180deg = ImageBrushSampler.SampleAtPosition(
                texture, testPos, brushCenter, brushRadius, 180f, false);
            
            Object.DestroyImmediate(texture);
            
            // Verify rotation changes sampling
            Assert.That(Mathf.Abs(sample0deg - sample90deg), Is.GreaterThan(0.01f), "90° rotation should change sample");
            Assert.That(Mathf.Abs(sample0deg - sample180deg), Is.GreaterThan(0.01f), "180° rotation should change sample");
            
            // 180° rotation should give opposite gradient value
            Assert.That(Mathf.Abs(sample0deg + sample180deg - 1f), Is.LessThan(0.2f), 
                "180° rotation should approximately invert gradient");
        }

        [Test]
        public void Task2_BatchSampling_ProcessesMultiplePositions()
        {
            // Verify batch processing works for multiple vertices
            Texture2D texture = CreateGradientTexture(8, 8);
            
            Vector3 brushCenter = Vector3.zero;
            float brushRadius = 5f;
            
            Vector3[] positions = new Vector3[]
            {
                brushCenter + new Vector3(-2f, 0f, 0f),
                brushCenter,
                brushCenter + new Vector3(2f, 0f, 0f),
                brushCenter + new Vector3(10f, 0f, 0f) // Outside radius
            };
            
            float[] weights = new float[4];
            
            ImageBrushSampler.SampleBatch(
                texture, positions, brushCenter, brushRadius, 0f, false, weights);
            
            Object.DestroyImmediate(texture);
            
            // Verify batch processing
            Assert.That(weights[0], Is.GreaterThan(0f), "First position should have weight");
            Assert.That(weights[1], Is.GreaterThan(0f), "Second position should have weight");
            Assert.That(weights[2], Is.GreaterThan(0f), "Third position should have weight");
            Assert.AreEqual(0f, weights[3], "Fourth position outside radius should be 0");
            
            // Verify gradient ordering
            Assert.That(weights[0], Is.LessThan(weights[1]), "Left should be less than center");
            Assert.That(weights[2], Is.GreaterThan(weights[1]), "Right should be greater than center");
        }

        [Test]
        public void Task2_AspectRatioPreservation_WorksForNonSquareTextures()
        {
            // Requirement 1.6: Preserve aspect ratio of non-square images
            Texture2D wideTexture = CreateGradientTexture(16, 8); // 2:1 aspect ratio
            
            Vector3 brushCenter = Vector3.zero;
            float brushRadius = 5f;
            
            // Sample with aspect ratio preservation
            float sampleWithAspect = ImageBrushSampler.SampleAtPosition(
                wideTexture, brushCenter + new Vector3(2f, 0f, 0f), 
                brushCenter, brushRadius, 0f, true);
            
            // Sample without aspect ratio preservation
            float sampleNoAspect = ImageBrushSampler.SampleAtPosition(
                wideTexture, brushCenter + new Vector3(2f, 0f, 0f), 
                brushCenter, brushRadius, 0f, false);
            
            Object.DestroyImmediate(wideTexture);
            
            // Values should differ when aspect ratio is preserved
            Assert.That(Mathf.Abs(sampleWithAspect - sampleNoAspect), Is.GreaterThan(0.01f), 
                "Aspect ratio preservation should affect sampling");
        }
    }
}
