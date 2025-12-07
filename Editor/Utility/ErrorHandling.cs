using UnityEngine;
using UnityEngine.Polybrush;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Centralized error handling utilities for Polybrush operations.
    /// Provides validation, warnings, and fallback mechanisms for image brushes,
    /// topology operations, and mesh modifications.
    /// </summary>
    internal static class ErrorHandling
    {
        #region Constants

        /// <summary>
        /// Unity's 16-bit vertex limit for meshes.
        /// </summary>
        internal const int VertexLimit16Bit = 65535;

        /// <summary>
        /// Unity's 32-bit vertex limit for meshes.
        /// </summary>
        internal const int VertexLimit32Bit = int.MaxValue;

        /// <summary>
        /// Minimum number of vertices required for a valid mesh.
        /// </summary>
        internal const int MinimumVertexCount = 3;

        /// <summary>
        /// Minimum number of triangles required for a valid mesh.
        /// </summary>
        internal const int MinimumTriangleCount = 1;

        /// <summary>
        /// Warning threshold - warn when approaching vertex limit.
        /// </summary>
        internal const float VertexLimitWarningThreshold = 0.9f;

        #endregion

        #region Image Brush Validation

        /// <summary>
        /// Validates an image brush texture and returns whether it's usable.
        /// If invalid, falls back to standard brush behavior.
        /// </summary>
        /// <param name="settings">The brush settings containing image brush configuration</param>
        /// <param name="showWarnings">Whether to display warnings in the console</param>
        /// <returns>True if image brush is valid and should be used, false to fall back to standard brush</returns>
        internal static bool ValidateImageBrushOrFallback(BrushSettings settings, bool showWarnings = true)
        {
            if (settings == null)
                return false;

            ImageBrushSettings imageBrush = settings.imageBrushSettings;
            
            // If image brush is not enabled, no validation needed
            if (imageBrush == null || !imageBrush.enabled)
                return false;

            // Check for null texture - fall back to standard brush
            if (imageBrush.brushTexture == null)
            {
                if (showWarnings)
                {
                    Debug.LogWarning("Polybrush: Image brush texture is null. Falling back to standard brush.");
                }
                return false;
            }

            // Validate texture format
            string errorMessage;
            if (!ValidateTextureFormat(imageBrush.brushTexture, out errorMessage))
            {
                if (showWarnings)
                {
                    Debug.LogWarning($"Polybrush: {errorMessage} Falling back to standard brush.");
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that a texture is suitable for use as an image brush.
        /// </summary>
        /// <param name="texture">The texture to validate</param>
        /// <param name="errorMessage">Output error message if validation fails</param>
        /// <returns>True if texture is valid for image brush use</returns>
        internal static bool ValidateTextureFormat(Texture2D texture, out string errorMessage)
        {
            errorMessage = null;

            if (texture == null)
            {
                errorMessage = "Texture is null.";
                return false;
            }

            // Check if texture is readable
            if (!texture.isReadable)
            {
                errorMessage = $"Texture '{texture.name}' is not readable. Please enable Read/Write in the texture import settings.";
                return false;
            }

            // Check minimum size
            if (texture.width < 2 || texture.height < 2)
            {
                errorMessage = $"Texture '{texture.name}' is too small ({texture.width}x{texture.height}). Minimum size is 2x2 pixels.";
                return false;
            }

            // Check for supported texture formats
            if (!IsSupportedTextureFormat(texture.format))
            {
                errorMessage = $"Texture '{texture.name}' has unsupported format '{texture.format}'. Use RGBA32, RGB24, or grayscale formats.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a texture format is supported for image brush sampling.
        /// </summary>
        /// <param name="format">The texture format to check</param>
        /// <returns>True if the format is supported</returns>
        internal static bool IsSupportedTextureFormat(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.RGB24:
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                case TextureFormat.R16:
                case TextureFormat.RFloat:
                case TextureFormat.RHalf:
                case TextureFormat.RG16:
                case TextureFormat.RG32:
                case TextureFormat.BGRA32:
                    return true;
                default:
                    // Try to sample - some compressed formats may still work
                    return true;
            }
        }

        /// <summary>
        /// Displays a warning dialog for invalid texture format.
        /// </summary>
        /// <param name="texture">The invalid texture</param>
        /// <param name="errorMessage">The error message to display</param>
        internal static void ShowTextureFormatWarning(Texture2D texture, string errorMessage)
        {
            EditorUtility.DisplayDialog(
                "Invalid Image Brush Texture",
                errorMessage + "\n\nThe brush will fall back to standard circular mode.",
                "OK"
            );
        }

        #endregion

        #region Vertex Limit Checks

        /// <summary>
        /// Checks if a subdivision operation would exceed vertex limits.
        /// </summary>
        /// <param name="currentVertexCount">Current number of vertices</param>
        /// <param name="estimatedNewVertices">Estimated number of new vertices to add</param>
        /// <param name="use32BitIndices">Whether the mesh uses 32-bit indices</param>
        /// <returns>True if the operation is safe, false if it would exceed limits</returns>
        internal static bool CheckVertexLimitForSubdivision(int currentVertexCount, int estimatedNewVertices, bool use32BitIndices = false)
        {
            int limit = use32BitIndices ? VertexLimit32Bit : VertexLimit16Bit;
            int projectedCount = currentVertexCount + estimatedNewVertices;

            if (projectedCount > limit)
            {
                Debug.LogWarning($"Polybrush: Subdivision would exceed vertex limit ({projectedCount} > {limit}). Operation cancelled.");
                return false;
            }

            // Warn if approaching limit
            if (projectedCount > limit * VertexLimitWarningThreshold)
            {
                Debug.LogWarning($"Polybrush: Approaching vertex limit ({projectedCount}/{limit}). Consider using a smaller brush or fewer iterations.");
            }

            return true;
        }

        /// <summary>
        /// Estimates the number of new vertices that will be created by subdivision.
        /// </summary>
        /// <param name="triangleCount">Number of triangles to subdivide</param>
        /// <param name="iterations">Number of subdivision iterations</param>
        /// <returns>Estimated number of new vertices</returns>
        internal static int EstimateSubdivisionVertices(int triangleCount, int iterations)
        {
            // Each triangle subdivision creates 3 new edge midpoints (shared between triangles)
            // Rough estimate: each iteration multiplies triangles by 4, adding ~1.5 vertices per original triangle
            int newVertices = 0;
            int currentTriangles = triangleCount;

            for (int i = 0; i < iterations; i++)
            {
                // Each triangle creates 3 edge midpoints, but edges are shared
                // Average is about 1.5 new vertices per triangle
                newVertices += (int)(currentTriangles * 1.5f);
                currentTriangles *= 4;
            }

            return newVertices;
        }

        /// <summary>
        /// Checks if unsubdivision would reduce vertices below minimum.
        /// </summary>
        /// <param name="currentVertexCount">Current number of vertices</param>
        /// <param name="estimatedRemovedVertices">Estimated number of vertices to remove</param>
        /// <returns>True if the operation is safe, false if it would go below minimum</returns>
        internal static bool CheckMinimumVertexCount(int currentVertexCount, int estimatedRemovedVertices)
        {
            int projectedCount = currentVertexCount - estimatedRemovedVertices;

            if (projectedCount < MinimumVertexCount)
            {
                Debug.LogWarning($"Polybrush: Unsubdivision would reduce vertices below minimum ({projectedCount} < {MinimumVertexCount}). Operation cancelled.");
                return false;
            }

            return true;
        }

        #endregion

        #region Topology Validation

        /// <summary>
        /// Result of topology validation.
        /// </summary>
        internal class TopologyValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public TopologyError ErrorType { get; set; }

            public static TopologyValidationResult Valid()
            {
                return new TopologyValidationResult { IsValid = true };
            }

            public static TopologyValidationResult Invalid(TopologyError errorType, string message)
            {
                return new TopologyValidationResult
                {
                    IsValid = false,
                    ErrorType = errorType,
                    ErrorMessage = message
                };
            }
        }

        /// <summary>
        /// Types of topology errors.
        /// </summary>
        internal enum TopologyError
        {
            None,
            NullMesh,
            InsufficientVertices,
            InsufficientTriangles,
            InvalidTriangleIndices,
            DegenerateTriangles,
            NonManifoldEdges,
            InvalidSubmesh
        }

        /// <summary>
        /// Validates mesh topology and returns detailed results.
        /// </summary>
        /// <param name="mesh">The mesh to validate</param>
        /// <returns>Validation result with error details if invalid</returns>
        internal static TopologyValidationResult ValidateMeshTopology(PolyMesh mesh)
        {
            if (mesh == null)
                return TopologyValidationResult.Invalid(TopologyError.NullMesh, "Mesh is null.");

            // Check minimum vertex count
            if (mesh.vertexCount < MinimumVertexCount)
                return TopologyValidationResult.Invalid(
                    TopologyError.InsufficientVertices,
                    $"Mesh has insufficient vertices ({mesh.vertexCount} < {MinimumVertexCount}).");

            int[] triangles = mesh.GetTriangles();

            // Check that we have valid triangles
            if (triangles == null || triangles.Length < 3)
                return TopologyValidationResult.Invalid(
                    TopologyError.InsufficientTriangles,
                    "Mesh has no valid triangles.");

            // Check triangle count is multiple of 3
            if (triangles.Length % 3 != 0)
                return TopologyValidationResult.Invalid(
                    TopologyError.InvalidTriangleIndices,
                    "Triangle array length is not a multiple of 3.");

            // Check all triangle indices are valid
            for (int i = 0; i < triangles.Length; i++)
            {
                if (triangles[i] < 0 || triangles[i] >= mesh.vertexCount)
                    return TopologyValidationResult.Invalid(
                        TopologyError.InvalidTriangleIndices,
                        $"Invalid triangle index {triangles[i]} at position {i} (vertex count: {mesh.vertexCount}).");
            }

            // Check for degenerate triangles
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                // Check for duplicate indices in triangle
                if (v0 == v1 || v1 == v2 || v2 == v0)
                    return TopologyValidationResult.Invalid(
                        TopologyError.DegenerateTriangles,
                        $"Degenerate triangle at index {i / 3}: duplicate vertex indices.");

                // Check for zero-area triangles
                if (IsTriangleDegenerate(mesh.vertices, v0, v1, v2))
                    return TopologyValidationResult.Invalid(
                        TopologyError.DegenerateTriangles,
                        $"Degenerate triangle at index {i / 3}: zero area or collinear vertices.");
            }

            return TopologyValidationResult.Valid();
        }

        /// <summary>
        /// Checks if a triangle is degenerate (zero area or coincident vertices).
        /// </summary>
        private static bool IsTriangleDegenerate(Vector3[] vertices, int v0, int v1, int v2)
        {
            if (v0 >= vertices.Length || v1 >= vertices.Length || v2 >= vertices.Length)
                return true;

            Vector3 p0 = vertices[v0];
            Vector3 p1 = vertices[v1];
            Vector3 p2 = vertices[v2];

            // Check for coincident vertices
            float epsilon = 0.0001f;
            if (Vector3.Distance(p0, p1) < epsilon ||
                Vector3.Distance(p1, p2) < epsilon ||
                Vector3.Distance(p2, p0) < epsilon)
            {
                return true;
            }

            // Check for zero area (collinear points)
            Vector3 cross = Vector3.Cross(p1 - p0, p2 - p0);
            if (cross.magnitude < epsilon)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Topology Rollback

        /// <summary>
        /// Stores mesh state for potential rollback.
        /// </summary>
        internal class MeshRollbackState
        {
            public Vector3[] Vertices { get; private set; }
            public Vector3[] Normals { get; private set; }
            public Color[] Colors { get; private set; }
            public Vector4[] Tangents { get; private set; }
            public int[][] SubMeshTriangles { get; private set; }
            public bool IsValid { get; private set; }

            /// <summary>
            /// Captures the current state of a mesh for potential rollback.
            /// </summary>
            public static MeshRollbackState Capture(PolyMesh mesh)
            {
                if (mesh == null)
                    return new MeshRollbackState { IsValid = false };

                var state = new MeshRollbackState { IsValid = true };

                // Capture vertices
                if (mesh.vertices != null)
                {
                    state.Vertices = new Vector3[mesh.vertices.Length];
                    System.Array.Copy(mesh.vertices, state.Vertices, mesh.vertices.Length);
                }

                // Capture normals
                if (mesh.normals != null)
                {
                    state.Normals = new Vector3[mesh.normals.Length];
                    System.Array.Copy(mesh.normals, state.Normals, mesh.normals.Length);
                }

                // Capture colors
                if (mesh.colors != null)
                {
                    state.Colors = new Color[mesh.colors.Length];
                    System.Array.Copy(mesh.colors, state.Colors, mesh.colors.Length);
                }

                // Capture tangents
                if (mesh.tangents != null)
                {
                    state.Tangents = new Vector4[mesh.tangents.Length];
                    System.Array.Copy(mesh.tangents, state.Tangents, mesh.tangents.Length);
                }

                // Capture submesh triangles
                if (mesh.subMeshes != null)
                {
                    state.SubMeshTriangles = new int[mesh.subMeshes.Length][];
                    for (int i = 0; i < mesh.subMeshes.Length; i++)
                    {
                        if (mesh.subMeshes[i] != null && mesh.subMeshes[i].indexes != null)
                        {
                            state.SubMeshTriangles[i] = new int[mesh.subMeshes[i].indexes.Length];
                            System.Array.Copy(mesh.subMeshes[i].indexes, state.SubMeshTriangles[i], mesh.subMeshes[i].indexes.Length);
                        }
                    }
                }

                return state;
            }

            /// <summary>
            /// Restores the mesh to the captured state.
            /// </summary>
            public bool Restore(PolyMesh mesh)
            {
                if (!IsValid || mesh == null)
                    return false;

                try
                {
                    if (Vertices != null)
                        mesh.vertices = (Vector3[])Vertices.Clone();
                    if (Normals != null)
                        mesh.normals = (Vector3[])Normals.Clone();
                    if (Colors != null)
                        mesh.colors = (Color[])Colors.Clone();
                    if (Tangents != null)
                        mesh.tangents = (Vector4[])Tangents.Clone();

                    // Restore submesh triangles
                    if (SubMeshTriangles != null && mesh.subMeshes != null)
                    {
                        for (int i = 0; i < SubMeshTriangles.Length && i < mesh.subMeshes.Length; i++)
                        {
                            if (SubMeshTriangles[i] != null && mesh.subMeshes[i] != null)
                            {
                                mesh.subMeshes[i].indexes = (int[])SubMeshTriangles[i].Clone();
                            }
                        }
                    }

                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Polybrush: Failed to restore mesh state: {e.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Performs a topology operation with automatic rollback on failure.
        /// </summary>
        /// <param name="mesh">The mesh to modify</param>
        /// <param name="operation">The operation to perform</param>
        /// <param name="operationName">Name of the operation for error messages</param>
        /// <returns>True if operation succeeded, false if rolled back</returns>
        internal static bool PerformWithRollback(PolyMesh mesh, System.Action operation, string operationName)
        {
            if (mesh == null || operation == null)
                return false;

            // Capture state before operation
            MeshRollbackState rollbackState = MeshRollbackState.Capture(mesh);

            try
            {
                // Perform the operation
                operation();

                // Validate the result
                TopologyValidationResult validation = ValidateMeshTopology(mesh);
                if (!validation.IsValid)
                {
                    Debug.LogWarning($"Polybrush: {operationName} resulted in invalid topology: {validation.ErrorMessage}. Rolling back.");
                    rollbackState.Restore(mesh);
                    return false;
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Polybrush: {operationName} failed with exception: {e.Message}. Rolling back.");
                rollbackState.Restore(mesh);
                return false;
            }
        }

        #endregion

        #region General Error Handling

        /// <summary>
        /// Logs a warning with Polybrush prefix.
        /// </summary>
        internal static void LogWarning(string message)
        {
            Debug.LogWarning($"Polybrush: {message}");
        }

        /// <summary>
        /// Logs an error with Polybrush prefix.
        /// </summary>
        internal static void LogError(string message)
        {
            Debug.LogError($"Polybrush: {message}");
        }

        /// <summary>
        /// Shows a dialog for operation failure.
        /// </summary>
        internal static void ShowOperationFailedDialog(string operationName, string reason)
        {
            EditorUtility.DisplayDialog(
                "Operation Failed",
                $"{operationName} could not be completed.\n\nReason: {reason}",
                "OK"
            );
        }

        #endregion
    }
}
