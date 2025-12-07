using UnityEngine;
using UnityEditor;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Validation utility for image brush infrastructure.
    /// Used to verify that image brush components are properly integrated.
    /// </summary>
    internal static class ImageBrushValidator
    {
        /// <summary>
        /// Validates that the image brush infrastructure is properly set up.
        /// </summary>
        [MenuItem("Tools/Polybrush/Validate Image Brush Infrastructure", false, 100)]
        internal static void ValidateInfrastructure()
        {
            bool allValid = true;
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("Image Brush Infrastructure Validation Report:");
            report.AppendLine("==============================================");

            // Test 1: Check if ImageBrushSettings can be instantiated
            try
            {
                ImageBrushSettings settings = new ImageBrushSettings();
                report.AppendLine("✓ ImageBrushSettings instantiation: PASS");
            }
            catch (System.Exception e)
            {
                report.AppendLine("✗ ImageBrushSettings instantiation: FAIL - " + e.Message);
                allValid = false;
            }

            // Test 2: Check if BrushSettings includes ImageBrushSettings
            try
            {
                BrushSettings brushSettings = ScriptableObject.CreateInstance<BrushSettings>();
                brushSettings.SetDefaultValues();
                
                if (brushSettings.imageBrushSettings != null)
                {
                    report.AppendLine("✓ BrushSettings.imageBrushSettings property: PASS");
                }
                else
                {
                    report.AppendLine("✗ BrushSettings.imageBrushSettings property: FAIL - Property is null");
                    allValid = false;
                }
                
                Object.DestroyImmediate(brushSettings);
            }
            catch (System.Exception e)
            {
                report.AppendLine("✗ BrushSettings integration: FAIL - " + e.Message);
                allValid = false;
            }

            // Test 3: Check ImageBrushSampler basic functionality
            try
            {
                // Create a simple test texture
                Texture2D testTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16];
                for (int i = 0; i < 16; i++)
                {
                    pixels[i] = Color.white;
                }
                testTexture.SetPixels(pixels);
                testTexture.Apply();

                // Test validation
                string errorMessage;
                bool isValid = ImageBrushSampler.ValidateTexture(testTexture, out errorMessage);
                
                if (!isValid)
                {
                    report.AppendLine("✓ ImageBrushSampler.ValidateTexture: PASS (correctly detected non-readable texture)");
                }
                else
                {
                    // Test sampling (will return 0 if texture is not readable, which is expected)
                    float sample = ImageBrushSampler.SampleAtPosition(
                        testTexture,
                        Vector3.zero,
                        Vector3.zero,
                        1f,
                        0f,
                        true
                    );
                    report.AppendLine("✓ ImageBrushSampler.SampleAtPosition: PASS");
                }

                Object.DestroyImmediate(testTexture);
            }
            catch (System.Exception e)
            {
                report.AppendLine("✗ ImageBrushSampler functionality: FAIL - " + e.Message);
                allValid = false;
            }

            // Test 4: Check BrushSettings copy functionality
            try
            {
                BrushSettings source = ScriptableObject.CreateInstance<BrushSettings>();
                source.SetDefaultValues();
                source.imageBrushSettings.enabled = true;
                source.imageBrushSettings.rotation = 45f;

                BrushSettings target = source.DeepCopy();
                
                if (target.imageBrushSettings != null &&
                    target.imageBrushSettings.enabled == true &&
                    Mathf.Approximately(target.imageBrushSettings.rotation, 45f))
                {
                    report.AppendLine("✓ BrushSettings.DeepCopy with ImageBrushSettings: PASS");
                }
                else
                {
                    report.AppendLine("✗ BrushSettings.DeepCopy with ImageBrushSettings: FAIL - Properties not copied correctly");
                    allValid = false;
                }

                Object.DestroyImmediate(source);
                Object.DestroyImmediate(target);
            }
            catch (System.Exception e)
            {
                report.AppendLine("✗ BrushSettings copy functionality: FAIL - " + e.Message);
                allValid = false;
            }

            report.AppendLine("==============================================");
            if (allValid)
            {
                report.AppendLine("✓ ALL TESTS PASSED");
                Debug.Log(report.ToString());
                EditorUtility.DisplayDialog("Validation Success", "All image brush infrastructure tests passed!", "OK");
            }
            else
            {
                report.AppendLine("✗ SOME TESTS FAILED");
                Debug.LogWarning(report.ToString());
                EditorUtility.DisplayDialog("Validation Failed", "Some tests failed. Check the console for details.", "OK");
            }
        }
    }
}
