using UnityEngine;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Utility class for sampling textures in image brush operations.
    /// Handles coordinate transformations from world space to texture space
    /// and provides texture sampling with rotation and aspect ratio support.
    /// Supports both CPU and GPU (compute shader) acceleration.
    /// </summary>
    internal static class ImageBrushSampler
    {
        // Compute shader support
        private static ComputeShader s_ComputeShader;
        private static int s_KernelIndex = -1;
        private static bool s_ComputeShaderInitialized = false;
        private static bool s_ComputeShaderAvailable = false;

        // GPU buffer cache
        private static ComputeBuffer s_PositionBuffer;
        private static ComputeBuffer s_WeightBuffer;
        private static int s_BufferCapacity = 0;

        // Shader property IDs
        private static readonly int s_PositionBufferId = Shader.PropertyToID("positionBuffer");
        private static readonly int s_WeightBufferId = Shader.PropertyToID("weightBuffer");
        private static readonly int s_BrushTextureId = Shader.PropertyToID("brushTexture");
        private static readonly int s_BrushCenterId = Shader.PropertyToID("brushCenter");
        private static readonly int s_BrushRadiusId = Shader.PropertyToID("brushRadius");
        private static readonly int s_RotationId = Shader.PropertyToID("rotation");
        private static readonly int s_AspectRatioId = Shader.PropertyToID("aspectRatio");
        private static readonly int s_NumPositionsId = Shader.PropertyToID("numPositions");
        private static readonly int s_PreserveAspectId = Shader.PropertyToID("preserveAspect");

        /// <summary>
        /// Initialize compute shader support. Called automatically on first use.
        /// </summary>
        private static void InitializeComputeShader()
        {
            if (s_ComputeShaderInitialized)
                return;

            s_ComputeShaderInitialized = true;

            // Check if compute shaders are supported on this platform
            if (!SystemInfo.supportsComputeShaders)
            {
                s_ComputeShaderAvailable = false;
                return;
            }

            // Try to load the compute shader
            s_ComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                "Packages/com.unity.polybrush/Content/ComputeShader/ImageBrushSamplerCS.compute");

            if (s_ComputeShader == null)
            {
                // Try alternative path for development
                s_ComputeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(
                    "Assets/Content/ComputeShader/ImageBrushSamplerCS.compute");
            }

            if (s_ComputeShader != null)
            {
                s_KernelIndex = s_ComputeShader.FindKernel("ImageBrushSampleKernel");
                s_ComputeShaderAvailable = (s_KernelIndex >= 0);
            }
            else
            {
                s_ComputeShaderAvailable = false;
            }
        }

        /// <summary>
        /// Returns true if compute shader acceleration is available.
        /// </summary>
        internal static bool IsComputeShaderAvailable()
        {
            InitializeComputeShader();
            return s_ComputeShaderAvailable;
        }

        /// <summary>
        /// Ensure GPU buffers are allocated with sufficient capacity.
        /// </summary>
        private static void EnsureBufferCapacity(int requiredCapacity)
        {
            if (s_BufferCapacity >= requiredCapacity)
                return;

            // Release old buffers
            ReleaseBuffers();

            // Allocate new buffers with some headroom
            int newCapacity = Mathf.NextPowerOfTwo(requiredCapacity);
            s_PositionBuffer = new ComputeBuffer(newCapacity, sizeof(float) * 3);
            s_WeightBuffer = new ComputeBuffer(newCapacity, sizeof(float));
            s_BufferCapacity = newCapacity;
        }

        /// <summary>
        /// Release GPU buffers. Should be called when done with batch operations.
        /// </summary>
        internal static void ReleaseBuffers()
        {
            if (s_PositionBuffer != null)
            {
                s_PositionBuffer.Release();
                s_PositionBuffer = null;
            }

            if (s_WeightBuffer != null)
            {
                s_WeightBuffer.Release();
                s_WeightBuffer = null;
            }

            s_BufferCapacity = 0;
        }

        /// <summary>
        /// Clean up resources when Unity editor is closing or reloading.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseBuffers;
        }
        /// <summary>
        /// Sample a texture at a world position within the brush radius.
        /// </summary>
        /// <param name="texture">The texture to sample from</param>
        /// <param name="worldPos">The world position to sample at</param>
        /// <param name="brushCenter">The center of the brush in world space</param>
        /// <param name="brushRadius">The radius of the brush</param>
        /// <param name="rotation">Rotation angle in degrees</param>
        /// <param name="preserveAspect">Whether to preserve texture aspect ratio</param>
        /// <returns>Sampled grayscale value (0-1), or 0 if outside brush or invalid</returns>
        internal static float SampleAtPosition(
            Texture2D texture,
            Vector3 worldPos,
            Vector3 brushCenter,
            float brushRadius,
            float rotation,
            bool preserveAspect)
        {
            if (texture == null || !texture.isReadable || brushRadius <= 0f)
                return 0f;

            // Transform world position to brush-local space (-1 to 1)
            Vector3 localPos = worldPos - brushCenter;
            float distance = localPos.magnitude;

            // Early out if outside brush radius
            if (distance > brushRadius)
                return 0f;

            // Normalize to brush space (-1 to 1)
            Vector2 brushSpace = new Vector2(localPos.x, localPos.z) / brushRadius;

            // Apply rotation
            if (rotation != 0f)
            {
                float rad = rotation * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                float x = brushSpace.x * cos - brushSpace.y * sin;
                float y = brushSpace.x * sin + brushSpace.y * cos;
                brushSpace = new Vector2(x, y);
            }

            // Apply aspect ratio correction if needed
            if (preserveAspect && texture.width != texture.height)
            {
                float aspectRatio = (float)texture.width / texture.height;
                if (aspectRatio > 1f)
                {
                    // Wider than tall
                    brushSpace.x /= aspectRatio;
                }
                else
                {
                    // Taller than wide
                    brushSpace.y *= aspectRatio;
                }
            }

            // Convert from brush space (-1 to 1) to texture UV space (0 to 1)
            Vector2 uv = new Vector2(
                (brushSpace.x + 1f) * 0.5f,
                (brushSpace.y + 1f) * 0.5f
            );

            // Clamp to valid UV range
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
                return 0f;

            // Sample texture using bilinear filtering
            return SampleTextureBilinear(texture, uv);
        }

        /// <summary>
        /// Sample multiple positions in batch.
        /// Automatically uses GPU acceleration if available, otherwise falls back to CPU.
        /// 
        /// GPU acceleration is used when:
        /// - Compute shaders are supported on the platform
        /// - The compute shader asset is available
        /// - The batch size is >= 100 samples (threshold for performance benefit)
        /// 
        /// The GPU path provides 10-100x speedup for large batches.
        /// </summary>
        /// <param name="texture">The texture to sample from</param>
        /// <param name="positions">Array of world positions to sample</param>
        /// <param name="brushCenter">The center of the brush in world space</param>
        /// <param name="brushRadius">The radius of the brush</param>
        /// <param name="rotation">Rotation angle in degrees</param>
        /// <param name="preserveAspect">Whether to preserve texture aspect ratio</param>
        /// <param name="outWeights">Output array for sampled weights (must be same length as positions)</param>
        internal static void SampleBatch(
            Texture2D texture,
            Vector3[] positions,
            Vector3 brushCenter,
            float brushRadius,
            float rotation,
            bool preserveAspect,
            float[] outWeights)
        {
            if (texture == null || positions == null || outWeights == null)
                return;

            if (positions.Length != outWeights.Length)
            {
                Debug.LogError("ImageBrushSampler.SampleBatch: positions and outWeights arrays must have the same length");
                return;
            }

            // Use GPU acceleration if available and beneficial (threshold: 100 samples)
            if (IsComputeShaderAvailable() && positions.Length >= 100)
            {
                SampleBatchGPU(texture, positions, brushCenter, brushRadius, rotation, preserveAspect, outWeights);
            }
            else
            {
                SampleBatchCPU(texture, positions, brushCenter, brushRadius, rotation, preserveAspect, outWeights);
            }
        }

        /// <summary>
        /// Sample multiple positions using CPU (fallback path).
        /// </summary>
        private static void SampleBatchCPU(
            Texture2D texture,
            Vector3[] positions,
            Vector3 brushCenter,
            float brushRadius,
            float rotation,
            bool preserveAspect,
            float[] outWeights)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                outWeights[i] = SampleAtPosition(
                    texture,
                    positions[i],
                    brushCenter,
                    brushRadius,
                    rotation,
                    preserveAspect
                );
            }
        }

        /// <summary>
        /// Sample multiple positions using GPU compute shader.
        /// </summary>
        private static void SampleBatchGPU(
            Texture2D texture,
            Vector3[] positions,
            Vector3 brushCenter,
            float brushRadius,
            float rotation,
            bool preserveAspect,
            float[] outWeights)
        {
            try
            {
                int numPositions = positions.Length;

                // Ensure buffers are large enough
                EnsureBufferCapacity(numPositions);

                // Upload position data to GPU
                s_PositionBuffer.SetData(positions);

                // Set compute shader parameters
                s_ComputeShader.SetBuffer(s_KernelIndex, s_PositionBufferId, s_PositionBuffer);
                s_ComputeShader.SetBuffer(s_KernelIndex, s_WeightBufferId, s_WeightBuffer);
                s_ComputeShader.SetTexture(s_KernelIndex, s_BrushTextureId, texture);
                s_ComputeShader.SetVector(s_BrushCenterId, brushCenter);
                s_ComputeShader.SetFloat(s_BrushRadiusId, brushRadius);
                s_ComputeShader.SetFloat(s_RotationId, rotation * Mathf.Deg2Rad);
                
                float aspectRatio = (float)texture.width / texture.height;
                s_ComputeShader.SetFloat(s_AspectRatioId, aspectRatio);
                s_ComputeShader.SetInt(s_NumPositionsId, numPositions);
                s_ComputeShader.SetBool(s_PreserveAspectId, preserveAspect);

                // Dispatch compute shader
                int threadGroups = Mathf.CeilToInt(numPositions / 512.0f);
                s_ComputeShader.Dispatch(s_KernelIndex, threadGroups, 1, 1);

                // Read results back from GPU
                s_WeightBuffer.GetData(outWeights, 0, 0, numPositions);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"ImageBrushSampler: GPU sampling failed, falling back to CPU. Error: {e.Message}");
                
                // Fall back to CPU if GPU fails
                SampleBatchCPU(texture, positions, brushCenter, brushRadius, rotation, preserveAspect, outWeights);
            }
        }

        /// <summary>
        /// Sample texture with bilinear filtering.
        /// </summary>
        private static float SampleTextureBilinear(Texture2D texture, Vector2 uv)
        {
            // Convert UV to pixel coordinates
            float x = uv.x * (texture.width - 1);
            float y = uv.y * (texture.height - 1);

            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, texture.width - 1);
            int y1 = Mathf.Min(y0 + 1, texture.height - 1);

            float fx = x - x0;
            float fy = y - y0;

            // Sample four neighboring pixels
            Color c00 = texture.GetPixel(x0, y0);
            Color c10 = texture.GetPixel(x1, y0);
            Color c01 = texture.GetPixel(x0, y1);
            Color c11 = texture.GetPixel(x1, y1);

            // Convert to grayscale (using standard luminance formula)
            float v00 = c00.grayscale;
            float v10 = c10.grayscale;
            float v01 = c01.grayscale;
            float v11 = c11.grayscale;

            // Bilinear interpolation
            float v0 = Mathf.Lerp(v00, v10, fx);
            float v1 = Mathf.Lerp(v01, v11, fx);
            float result = Mathf.Lerp(v0, v1, fy);

            return result;
        }

        /// <summary>
        /// Validates that a texture is suitable for use as an image brush.
        /// </summary>
        /// <param name="texture">The texture to validate</param>
        /// <param name="errorMessage">Output error message if validation fails</param>
        /// <returns>True if texture is valid for image brush use</returns>
        internal static bool ValidateTexture(Texture2D texture, out string errorMessage)
        {
            errorMessage = null;

            if (texture == null)
            {
                errorMessage = "Texture is null";
                return false;
            }

            if (!texture.isReadable)
            {
                errorMessage = "Texture is not readable. Please enable Read/Write in the texture import settings.";
                return false;
            }

            if (texture.width < 2 || texture.height < 2)
            {
                errorMessage = "Texture is too small. Minimum size is 2x2 pixels.";
                return false;
            }

            return true;
        }
    }
}
