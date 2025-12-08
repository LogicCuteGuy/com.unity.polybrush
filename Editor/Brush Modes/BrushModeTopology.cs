using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Polybrush;
using UnityEditor.SettingsManagement;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Brush mode for dynamic mesh subdivision and unsubdivision (detail management).
    /// Supports both adding detail (subdivision) and removing detail (unsubdivision) within brush radius.
    /// Includes ProBuilder integration to maintain mesh compatibility during topology operations.
    /// </summary>
    internal class BrushModeTopology : BrushModeMesh
    {
        /// <summary>
        /// Defines the topology operation mode
        /// </summary>
        internal enum TopologyMode
        {
            Subdivide,      // Add detail by splitting faces (default/left click)
            Unsubdivide     // Remove detail by merging vertices (shift/control + left click)
        }

        internal static class Styles
        {
            internal static GUIContent gcTopologyMode = new GUIContent("Topology Mode", "The current topology operation: Subdivide adds detail, Unsubdivide removes detail.");
            internal static GUIContent gcSubdivisionIterations = new GUIContent("Subdivision Iterations", "Number of subdivision iterations per brush stroke. Higher values create more detail.");
            internal static GUIContent gcMergeThreshold = new GUIContent("Merge Threshold", "Distance threshold for merging vertices during unsubdivision. Larger values merge more aggressively.");
            internal static GUIContent gcPreserveShape = new GUIContent("Preserve Shape", "When enabled, topology operations will attempt to preserve the original surface shape.");
            internal static GUIContent gcModifierKeyHint = new GUIContent("Tip: Hold Shift or Control while painting to switch to Unsubdivide mode.");
        }

        // User settings for topology operations
        [UserSetting]
        internal static Pref<TopologyMode> s_TopologyMode = new Pref<TopologyMode>("TopologyBrush.Mode", TopologyMode.Subdivide, SettingsScope.Project);

        [UserSetting]
        internal static Pref<int> s_SubdivisionIterations = new Pref<int>("TopologyBrush.SubdivisionIterations", 1, SettingsScope.Project);

        [UserSetting]
        internal static Pref<float> s_MergeThreshold = new Pref<float>("TopologyBrush.MergeThreshold", 0.01f, SettingsScope.Project);

        [UserSetting]
        internal static Pref<bool> s_PreserveShape = new Pref<bool>("TopologyBrush.PreserveShape", true, SettingsScope.Project);

        // Constants
        private const int k_MinimumVertexCount = 3;  // Minimum vertices to form a valid mesh

        // Internal state
        private TopologyMode m_CurrentMode = TopologyMode.Subdivide;
        private Dictionary<EditableObject, TopologyOperationData> m_OperationData = new Dictionary<EditableObject, TopologyOperationData>();

        /// <summary>
        /// Stores data for topology operations on a specific mesh
        /// </summary>
        private class TopologyOperationData
        {
            public Vector3[] originalPositions;
            public Dictionary<int, List<int>> vertexClusters;
            public List<int> modifiedVertices;
            public List<int> modifiedFaces;
        }

        /// <summary>
        /// Stores complete mesh state for undo/redo operations.
        /// This cache captures all mesh data before topology changes are applied.
        /// </summary>
        internal class MeshStateCache
        {
            public Vector3[] vertices;
            public Vector3[] normals;
            public Color[] colors;
            public Vector4[] tangents;
            public List<Vector4> uv0;
            public List<Vector4> uv1;
            public List<Vector4> uv2;
            public List<Vector4> uv3;
            public int[][] subMeshTriangles;
            public int vertexCount;
            public int triangleCount;

            /// <summary>
            /// Creates a deep copy of the mesh state from a PolyMesh
            /// </summary>
            public static MeshStateCache CaptureState(PolyMesh mesh)
            {
                if (mesh == null)
                    return null;

                var cache = new MeshStateCache();
                
                // Cache vertices
                if (mesh.vertices != null)
                {
                    cache.vertices = new Vector3[mesh.vertices.Length];
                    System.Array.Copy(mesh.vertices, cache.vertices, mesh.vertices.Length);
                }
                
                // Cache normals
                if (mesh.normals != null)
                {
                    cache.normals = new Vector3[mesh.normals.Length];
                    System.Array.Copy(mesh.normals, cache.normals, mesh.normals.Length);
                }
                
                // Cache colors
                if (mesh.colors != null)
                {
                    cache.colors = new Color[mesh.colors.Length];
                    System.Array.Copy(mesh.colors, cache.colors, mesh.colors.Length);
                }
                
                // Cache tangents
                if (mesh.tangents != null)
                {
                    cache.tangents = new Vector4[mesh.tangents.Length];
                    System.Array.Copy(mesh.tangents, cache.tangents, mesh.tangents.Length);
                }
                
                // Cache UVs
                if (mesh.uv0 != null && mesh.uv0.Count > 0)
                    cache.uv0 = new List<Vector4>(mesh.uv0);
                if (mesh.uv1 != null && mesh.uv1.Count > 0)
                    cache.uv1 = new List<Vector4>(mesh.uv1);
                if (mesh.uv2 != null && mesh.uv2.Count > 0)
                    cache.uv2 = new List<Vector4>(mesh.uv2);
                if (mesh.uv3 != null && mesh.uv3.Count > 0)
                    cache.uv3 = new List<Vector4>(mesh.uv3);
                
                // Cache submesh triangles
                if (mesh.subMeshes != null)
                {
                    cache.subMeshTriangles = new int[mesh.subMeshes.Length][];
                    for (int i = 0; i < mesh.subMeshes.Length; i++)
                    {
                        if (mesh.subMeshes[i] != null && mesh.subMeshes[i].indexes != null)
                        {
                            cache.subMeshTriangles[i] = new int[mesh.subMeshes[i].indexes.Length];
                            System.Array.Copy(mesh.subMeshes[i].indexes, cache.subMeshTriangles[i], mesh.subMeshes[i].indexes.Length);
                        }
                    }
                }
                
                cache.vertexCount = mesh.vertexCount;
                cache.triangleCount = mesh.GetTriangles()?.Length ?? 0;
                
                return cache;
            }

            /// <summary>
            /// Restores mesh state from the cache
            /// </summary>
            public void RestoreState(PolyMesh mesh)
            {
                if (mesh == null)
                    return;

                if (vertices != null)
                    mesh.vertices = (Vector3[])vertices.Clone();
                if (normals != null)
                    mesh.normals = (Vector3[])normals.Clone();
                if (colors != null)
                    mesh.colors = (Color[])colors.Clone();
                if (tangents != null)
                    mesh.tangents = (Vector4[])tangents.Clone();
                
                if (uv0 != null)
                    mesh.uv0 = new List<Vector4>(uv0);
                if (uv1 != null)
                    mesh.uv1 = new List<Vector4>(uv1);
                if (uv2 != null)
                    mesh.uv2 = new List<Vector4>(uv2);
                if (uv3 != null)
                    mesh.uv3 = new List<Vector4>(uv3);
                
                // Restore submesh triangles
                if (subMeshTriangles != null && mesh.subMeshes != null)
                {
                    for (int i = 0; i < subMeshTriangles.Length && i < mesh.subMeshes.Length; i++)
                    {
                        if (subMeshTriangles[i] != null && mesh.subMeshes[i] != null)
                        {
                            mesh.subMeshes[i].indexes = (int[])subMeshTriangles[i].Clone();
                        }
                    }
                }
            }
        }

        // Mesh state cache for undo operations
        private Dictionary<EditableObject, MeshStateCache> m_MeshStateCache = new Dictionary<EditableObject, MeshStateCache>();

        protected override string DocsLink { get { return PrefUtility.documentationSculptBrushLink; } }
        
        /// <summary>
        /// Gets the undo message based on the current topology mode.
        /// Provides descriptive messages for subdivision and unsubdivision operations.
        /// </summary>
        internal override string UndoMessage 
        { 
            get 
            { 
                return m_CurrentMode == TopologyMode.Subdivide 
                    ? "Topology Subdivision" 
                    : "Topology Unsubdivision"; 
            } 
        }
        
        protected override string ModeSettingsHeader { get { return "Topology Settings"; } }

        internal override void OnEnable()
        {
            base.OnEnable();
            m_CurrentMode = s_TopologyMode.value;
        }

        internal override void DrawGUI(BrushSettings settings)
        {
            base.DrawGUI(settings);

            EditorGUI.BeginChangeCheck();

            // Display current mode (read-only, controlled by modifier keys during painting)
            EditorGUILayout.LabelField("Current Mode", m_CurrentMode.ToString());

            // Subdivision settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Subdivision Settings", EditorStyles.boldLabel);
            
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Styles.gcSubdivisionIterations, GUILayout.Width(140));
                s_SubdivisionIterations.value = Mathf.Clamp(
                    EditorGUILayout.IntField(s_SubdivisionIterations.value),
                    1,
                    5
                );
            }

            // Unsubdivision settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unsubdivision Settings", EditorStyles.boldLabel);
            s_MergeThreshold.value = PolyGUILayout.FloatField(
                Styles.gcMergeThreshold,
                s_MergeThreshold
            );
            s_MergeThreshold.value = Mathf.Clamp(s_MergeThreshold.value, 0.001f, 1.0f);

            // Common settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);
            s_PreserveShape.value = PolyGUILayout.Toggle(
                Styles.gcPreserveShape,
                s_PreserveShape
            );

            if (EditorGUI.EndChangeCheck())
            {
                PolybrushSettings.Save();
            }

            // Display helpful hint about modifier keys
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Styles.gcModifierKeyHint.text, MessageType.Info);

            // Display ProBuilder compatibility notice if ProBuilder is available
            if (ProBuilderBridge.ProBuilderExists())
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "ProBuilder Integration: Topology operations will maintain ProBuilder mesh compatibility. " +
                    "Face and edge data will be preserved during subdivision and unsubdivision.",
                    MessageType.Info);
            }
        }

        internal override void OnBrushEnter(EditableObject target, BrushSettings settings)
        {
            base.OnBrushEnter(target, settings);

            // Initialize operation data for this target
            if (!m_OperationData.ContainsKey(target))
            {
                m_OperationData[target] = new TopologyOperationData
                {
                    originalPositions = target.editMesh.vertices,
                    vertexClusters = new Dictionary<int, List<int>>(),
                    modifiedVertices = new List<int>(),
                    modifiedFaces = new List<int>()
                };
            }
        }

        internal override void OnBrushExit(EditableObject target)
        {
            base.OnBrushExit(target);

            // Clean up operation data
            if (m_OperationData.ContainsKey(target))
            {
                m_OperationData.Remove(target);
            }
            
            // Clean up mesh state cache
            ClearMeshStateCache(target);
        }

        /// <summary>
        /// Registers undo for topology operations with mode-specific messages.
        /// Caches the complete mesh state before any topology changes are applied.
        /// </summary>
        /// <param name="brushTarget">The target being modified</param>
        internal override void RegisterUndo(BrushTarget brushTarget)
        {
            // Cache mesh state before the base RegisterUndo is called
            // This ensures we have a complete snapshot of the mesh for undo/redo
            CacheMeshState(brushTarget.editableObject);
            
            // Call base implementation which handles the actual Unity undo registration
            base.RegisterUndo(brushTarget);
        }

        /// <summary>
        /// Caches the complete mesh state before topology operations.
        /// This is essential for proper undo/redo support as topology changes
        /// modify vertex count and triangle indices.
        /// </summary>
        /// <param name="editableObject">The editable object to cache</param>
        private void CacheMeshState(EditableObject editableObject)
        {
            if (editableObject == null || editableObject.editMesh == null)
                return;

            // Create or update the mesh state cache
            m_MeshStateCache[editableObject] = MeshStateCache.CaptureState(editableObject.editMesh);
        }

        /// <summary>
        /// Gets the cached mesh state for an editable object.
        /// </summary>
        /// <param name="editableObject">The editable object</param>
        /// <returns>The cached mesh state, or null if not cached</returns>
        internal MeshStateCache GetCachedMeshState(EditableObject editableObject)
        {
            if (editableObject == null)
                return null;
                
            m_MeshStateCache.TryGetValue(editableObject, out MeshStateCache cache);
            return cache;
        }

        /// <summary>
        /// Clears the mesh state cache for an editable object.
        /// </summary>
        /// <param name="editableObject">The editable object</param>
        internal void ClearMeshStateCache(EditableObject editableObject)
        {
            if (editableObject != null && m_MeshStateCache.ContainsKey(editableObject))
            {
                m_MeshStateCache.Remove(editableObject);
            }
        }

        internal override void OnBrushBeginApply(BrushTarget target, BrushSettings settings)
        {
            base.OnBrushBeginApply(target, settings);

            // Update mode based on modifier keys
            UpdateModeFromModifierKeys(settings);

            // Synchronize ProBuilder mesh data before topology operations
            // This ensures we have the latest data from ProBuilder
            SyncProBuilderMeshBeforeOperation(target.editableObject);

            // Cache original mesh state for this operation (for internal tracking)
            if (m_OperationData.ContainsKey(target.editableObject))
            {
                m_OperationData[target.editableObject].originalPositions = 
                    (Vector3[])target.editableObject.editMesh.vertices.Clone();
            }
            
            // Ensure mesh state is cached for undo (in case RegisterUndo wasn't called yet)
            if (!m_MeshStateCache.ContainsKey(target.editableObject))
            {
                CacheMeshState(target.editableObject);
            }
        }

        internal override void OnBrushApply(BrushTarget target, BrushSettings settings)
        {
            // Update mode based on modifier keys during painting
            UpdateModeFromModifierKeys(settings);

            int rayCount = target.raycastHits.Count;
            if (rayCount < 1)
                return;

            PolyMesh mesh = target.editableObject.editMesh;

            // Apply the appropriate topology operation based on current mode
            switch (m_CurrentMode)
            {
                case TopologyMode.Subdivide:
                    ApplySubdivision(target, settings);
                    break;

                case TopologyMode.Unsubdivide:
                    ApplyUnsubdivision(target, settings);
                    break;
            }

            base.OnBrushApply(target, settings);
        }

        /// <summary>
        /// Update the current topology mode based on modifier keys
        /// </summary>
        private void UpdateModeFromModifierKeys(BrushSettings settings)
        {
            // Switch to Unsubdivide when holding Shift or Control (using base class helper)
            if (IsInvertedByAnyModifier(settings))
            {
                m_CurrentMode = TopologyMode.Unsubdivide;
            }
            else
            {
                m_CurrentMode = TopologyMode.Subdivide;
            }
        }

        /// <summary>
        /// Apply subdivision to faces within the brush radius
        /// </summary>
        private void ApplySubdivision(BrushTarget target, BrushSettings settings)
        {
            PolyMesh mesh = target.editableObject.editMesh;
            float[] weights = target.GetAllWeights(true);
            
            if (mesh == null || weights == null)
                return;

            // Get faces that are within the brush influence
            List<int> affectedFaces = GetAffectedFaces(mesh, weights);
            
            if (affectedFaces.Count == 0)
                return;

            // Calculate number of iterations based on strength
            int iterations = CalculateSubdivisionIterations(settings.strength);
            
            // Pre-check vertex limit before subdivision
            int estimatedNewVertices = ErrorHandling.EstimateSubdivisionVertices(affectedFaces.Count, iterations);
            if (!ErrorHandling.CheckVertexLimitForSubdivision(mesh.vertexCount, estimatedNewVertices, use32BitIndices: false))
            {
                return;
            }
            
            // Perform subdivision with rollback on failure
            bool success = ErrorHandling.PerformWithRollback(mesh, () =>
            {
                SubdivideFaces(mesh, affectedFaces.ToArray(), weights, iterations);
            }, "Subdivision");
            
            if (!success)
                return;
            
            // Recalculate normals after topology change
            mesh.RecalculateNormals();
            
            // Apply mesh changes and synchronize with rendering/physics systems
            target.editableObject.Apply(true);
            
            // Perform immediate mesh synchronization for topology changes
            // This ensures collider, rendering data, and bounds are updated immediately
            MeshSynchronizer.SynchronizeAfterTopologyChange(target.editableObject, true);
            
            // Handle ProBuilder mesh refresh if this is a ProBuilder object
            RefreshProBuilderMesh(target.editableObject, true);
        }

        /// <summary>
        /// Apply unsubdivision (vertex merging) within the brush radius
        /// </summary>
        private void ApplyUnsubdivision(BrushTarget target, BrushSettings settings)
        {
            PolyMesh mesh = target.editableObject.editMesh;
            float[] weights = target.GetAllWeights(true);
            
            if (mesh == null || weights == null)
                return;

            // Get vertices that are within the brush influence
            List<int> affectedVertices = GetAffectedVertices(mesh, weights);
            
            if (affectedVertices.Count < 2)
                return;

            // Check minimum vertex count protection using centralized error handling
            if (mesh.vertexCount <= ErrorHandling.MinimumVertexCount)
            {
                ErrorHandling.LogWarning($"Cannot unsubdivide: mesh already at minimum vertex count ({ErrorHandling.MinimumVertexCount})");
                return;
            }

            // Calculate merge threshold based on strength
            float threshold = CalculateMergeThreshold(settings.strength);
            
            // Perform vertex merging with rollback on failure
            bool success = ErrorHandling.PerformWithRollback(mesh, () =>
            {
                MergeVertices(mesh, affectedVertices.ToArray(), weights, threshold);
            }, "Unsubdivision");
            
            if (!success)
                return;
            
            // Recalculate normals after topology change
            mesh.RecalculateNormals();
            
            // Apply mesh changes and synchronize with rendering/physics systems
            target.editableObject.Apply(true);
            
            // Perform immediate mesh synchronization for topology changes
            // This ensures collider, rendering data, and bounds are updated immediately
            MeshSynchronizer.SynchronizeAfterTopologyChange(target.editableObject, true);
            
            // Handle ProBuilder mesh refresh if this is a ProBuilder object
            RefreshProBuilderMesh(target.editableObject, true);
        }

        /// <summary>
        /// Subdivide faces within brush influence
        /// </summary>
        internal void SubdivideFaces(
            PolyMesh mesh,
            int[] faceIndices,
            float[] weights,
            int iterations)
        {
            if (mesh == null || faceIndices == null || faceIndices.Length == 0)
                return;

            // Perform subdivision for the specified number of iterations
            for (int iter = 0; iter < iterations; iter++)
            {
                // Get current triangles
                int[] triangles = mesh.GetTriangles();
                
                // Build list of triangles to subdivide based on face indices
                List<int> trianglesToSubdivide = new List<int>();
                foreach (int faceIdx in faceIndices)
                {
                    int triIndex = faceIdx * 3;
                    if (triIndex + 2 < triangles.Length)
                    {
                        // Check if this triangle should be subdivided based on vertex weights
                        int v0 = triangles[triIndex];
                        int v1 = triangles[triIndex + 1];
                        int v2 = triangles[triIndex + 2];
                        
                        // Skip degenerate triangles
                        if (IsTriangleDegenerate(mesh, v0, v1, v2))
                            continue;
                        
                        // Only subdivide if at least one vertex has significant weight
                        if (v0 < weights.Length && v1 < weights.Length && v2 < weights.Length)
                        {
                            float maxWeight = Mathf.Max(weights[v0], weights[v1], weights[v2]);
                            if (maxWeight > 0.01f)
                            {
                                trianglesToSubdivide.Add(triIndex);
                            }
                        }
                    }
                }
                
                if (trianglesToSubdivide.Count == 0)
                    break;
                
                // Subdivide the selected triangles
                SubdivideTriangles(mesh, triangles, trianglesToSubdivide);
            }
        }

        /// <summary>
        /// Merge vertices within threshold distance
        /// </summary>
        internal void MergeVertices(
            PolyMesh mesh,
            int[] vertexIndices,
            float[] weights,
            float threshold)
        {
            if (mesh == null || vertexIndices == null || vertexIndices.Length < 2)
                return;

            // Build vertex clusters based on proximity
            Dictionary<int, List<int>> clusters = BuildVertexClusters(mesh, vertexIndices, weights, threshold);
            
            if (clusters.Count == 0)
                return;

            // Create mapping from old vertex indices to new indices
            Dictionary<int, int> vertexRemapping = new Dictionary<int, int>();
            List<Vector3> newVertices = new List<Vector3>();
            List<Vector3> newNormals = mesh.normals != null ? new List<Vector3>() : null;
            List<Color> newColors = mesh.colors != null ? new List<Color>() : null;
            List<Vector4> newTangents = mesh.tangents != null ? new List<Vector4>() : null;
            List<Vector4> newUV0 = mesh.uv0 != null && mesh.uv0.Count > 0 ? new List<Vector4>() : null;
            List<Vector4> newUV1 = mesh.uv1 != null && mesh.uv1.Count > 0 ? new List<Vector4>() : null;
            List<Vector4> newUV2 = mesh.uv2 != null && mesh.uv2.Count > 0 ? new List<Vector4>() : null;
            List<Vector4> newUV3 = mesh.uv3 != null && mesh.uv3.Count > 0 ? new List<Vector4>() : null;

            // Track which vertices have been processed
            HashSet<int> processedVertices = new HashSet<int>();
            HashSet<int> mergedVertices = new HashSet<int>();

            // First, identify all vertices that will be merged
            foreach (var cluster in clusters.Values)
            {
                if (cluster.Count > 1)
                {
                    foreach (int v in cluster)
                        mergedVertices.Add(v);
                }
            }

            // Process all vertices
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                if (processedVertices.Contains(i))
                    continue;

                // Check if this vertex is part of a cluster
                bool foundInCluster = false;
                foreach (var kvp in clusters)
                {
                    if (kvp.Value.Contains(i) && kvp.Value.Count > 1)
                    {
                        // This vertex is part of a cluster - merge all vertices in the cluster
                        int newIndex = newVertices.Count;
                        
                        // Average all attributes from the cluster
                        AverageClusterAttributes(
                            mesh, kvp.Value, newIndex,
                            newVertices, newNormals, newColors, newTangents,
                            newUV0, newUV1, newUV2, newUV3);
                        
                        // Map all cluster vertices to the new merged vertex
                        foreach (int clusterVertex in kvp.Value)
                        {
                            vertexRemapping[clusterVertex] = newIndex;
                            processedVertices.Add(clusterVertex);
                        }
                        
                        foundInCluster = true;
                        break;
                    }
                }

                if (!foundInCluster)
                {
                    // Vertex is not being merged - copy it directly
                    int newIndex = newVertices.Count;
                    vertexRemapping[i] = newIndex;
                    processedVertices.Add(i);
                    
                    newVertices.Add(mesh.vertices[i]);
                    if (newNormals != null && i < mesh.normals.Length)
                        newNormals.Add(mesh.normals[i]);
                    if (newColors != null && i < mesh.colors.Length)
                        newColors.Add(mesh.colors[i]);
                    if (newTangents != null && i < mesh.tangents.Length)
                        newTangents.Add(mesh.tangents[i]);
                    if (newUV0 != null && i < mesh.uv0.Count)
                        newUV0.Add(mesh.uv0[i]);
                    if (newUV1 != null && i < mesh.uv1.Count)
                        newUV1.Add(mesh.uv1[i]);
                    if (newUV2 != null && i < mesh.uv2.Count)
                        newUV2.Add(mesh.uv2[i]);
                    if (newUV3 != null && i < mesh.uv3.Count)
                        newUV3.Add(mesh.uv3[i]);
                }
            }

            // Check minimum vertex count protection using centralized error handling
            if (!ErrorHandling.CheckMinimumVertexCount(mesh.vertexCount, mesh.vertexCount - newVertices.Count))
            {
                return;
            }

            // Remap triangle indices
            int[] oldTriangles = mesh.GetTriangles();
            List<int> newTriangles = new List<int>();
            
            for (int i = 0; i < oldTriangles.Length; i += 3)
            {
                int v0 = oldTriangles[i];
                int v1 = oldTriangles[i + 1];
                int v2 = oldTriangles[i + 2];
                
                // Get remapped indices - all vertices should be in the mapping
                if (!vertexRemapping.ContainsKey(v0) || !vertexRemapping.ContainsKey(v1) || !vertexRemapping.ContainsKey(v2))
                {
                    // Skip triangles with unmapped vertices (shouldn't happen, but safety check)
                    continue;
                }
                
                int newV0 = vertexRemapping[v0];
                int newV1 = vertexRemapping[v1];
                int newV2 = vertexRemapping[v2];
                
                // Validate indices are within bounds
                if (newV0 >= newVertices.Count || newV1 >= newVertices.Count || newV2 >= newVertices.Count)
                    continue;
                
                // Skip degenerate triangles (where two or more vertices are the same)
                if (newV0 == newV1 || newV1 == newV2 || newV2 == newV0)
                    continue;
                
                newTriangles.Add(newV0);
                newTriangles.Add(newV1);
                newTriangles.Add(newV2);
            }

            // Update mesh with new data
            mesh.vertices = newVertices.ToArray();
            if (newNormals != null) mesh.normals = newNormals.ToArray();
            if (newColors != null) mesh.colors = newColors.ToArray();
            if (newTangents != null) mesh.tangents = newTangents.ToArray();
            if (newUV0 != null) mesh.uv0 = newUV0;
            if (newUV1 != null) mesh.uv1 = newUV1;
            if (newUV2 != null) mesh.uv2 = newUV2;
            if (newUV3 != null) mesh.uv3 = newUV3;

            // Update submesh triangles
            // For unsubdivision, we consolidate all triangles into the first submesh
            // and clear other submeshes to avoid validation errors from stale data
            if (mesh.subMeshes != null && mesh.subMeshes.Length > 0)
            {
                mesh.subMeshes[0].indexes = newTriangles.ToArray();
                
                // Clear other submeshes to prevent validation errors from stale data
                for (int i = 1; i < mesh.subMeshes.Length; i++)
                {
                    if (mesh.subMeshes[i] != null)
                    {
                        mesh.subMeshes[i].indexes = new int[0];
                    }
                }
            }
            
            // Force refresh of the triangle cache to reflect the new submesh data
            // This is critical because GetTriangles() may return cached data
            System.Reflection.FieldInfo trianglesField = typeof(PolyMesh).GetField("m_Triangles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (trianglesField != null)
            {
                trianglesField.SetValue(mesh, null);
            }
        }

        /// <summary>
        /// Build vertex clusters based on proximity within threshold distance
        /// </summary>
        private Dictionary<int, List<int>> BuildVertexClusters(
            PolyMesh mesh,
            int[] vertexIndices,
            float[] weights,
            float threshold)
        {
            Dictionary<int, List<int>> clusters = new Dictionary<int, List<int>>();
            HashSet<int> assignedVertices = new HashSet<int>();
            
            // Sort vertices by weight (higher weight vertices become cluster centers)
            List<int> sortedVertices = new List<int>(vertexIndices);
            sortedVertices.Sort((a, b) => {
                float weightA = a < weights.Length ? weights[a] : 0f;
                float weightB = b < weights.Length ? weights[b] : 0f;
                return weightB.CompareTo(weightA);
            });
            
            int clusterIndex = 0;
            foreach (int vertexIndex in sortedVertices)
            {
                if (assignedVertices.Contains(vertexIndex))
                    continue;
                
                // Only consider vertices with significant weight
                float vertexWeight = vertexIndex < weights.Length ? weights[vertexIndex] : 0f;
                if (vertexWeight < 0.01f)
                    continue;
                
                // Start a new cluster with this vertex as the center
                List<int> cluster = new List<int> { vertexIndex };
                assignedVertices.Add(vertexIndex);
                
                Vector3 centerPos = mesh.vertices[vertexIndex];
                
                // Find nearby vertices to add to this cluster
                foreach (int otherVertex in sortedVertices)
                {
                    if (assignedVertices.Contains(otherVertex))
                        continue;
                    
                    float otherWeight = otherVertex < weights.Length ? weights[otherVertex] : 0f;
                    if (otherWeight < 0.01f)
                        continue;
                    
                    Vector3 otherPos = mesh.vertices[otherVertex];
                    float distance = Vector3.Distance(centerPos, otherPos);
                    
                    // Scale threshold by the average weight of the two vertices
                    float effectiveThreshold = threshold * (vertexWeight + otherWeight) * 0.5f;
                    
                    if (distance <= effectiveThreshold)
                    {
                        cluster.Add(otherVertex);
                        assignedVertices.Add(otherVertex);
                    }
                }
                
                // Only create cluster if it has more than one vertex
                if (cluster.Count > 1)
                {
                    clusters[clusterIndex++] = cluster;
                }
            }
            
            return clusters;
        }

        /// <summary>
        /// Average attributes from all vertices in a cluster to create a merged vertex
        /// </summary>
        private void AverageClusterAttributes(
            PolyMesh mesh,
            List<int> clusterVertices,
            int newIndex,
            List<Vector3> newVertices,
            List<Vector3> newNormals,
            List<Color> newColors,
            List<Vector4> newTangents,
            List<Vector4> newUV0,
            List<Vector4> newUV1,
            List<Vector4> newUV2,
            List<Vector4> newUV3)
        {
            int count = clusterVertices.Count;
            if (count == 0)
                return;
            
            float invCount = 1f / count;
            
            // Average position
            Vector3 avgPosition = Vector3.zero;
            foreach (int v in clusterVertices)
            {
                if (v < mesh.vertices.Length)
                    avgPosition += mesh.vertices[v];
            }
            newVertices.Add(avgPosition * invCount);
            
            // Average normals
            if (newNormals != null && mesh.normals != null)
            {
                Vector3 avgNormal = Vector3.zero;
                foreach (int v in clusterVertices)
                {
                    if (v < mesh.normals.Length)
                        avgNormal += mesh.normals[v];
                }
                newNormals.Add(avgNormal.normalized);
            }
            
            // Average colors
            if (newColors != null && mesh.colors != null)
            {
                Color avgColor = Color.clear;
                foreach (int v in clusterVertices)
                {
                    if (v < mesh.colors.Length)
                        avgColor += mesh.colors[v];
                }
                newColors.Add(avgColor * invCount);
            }
            
            // Average tangents
            if (newTangents != null && mesh.tangents != null)
            {
                Vector4 avgTangent = Vector4.zero;
                foreach (int v in clusterVertices)
                {
                    if (v < mesh.tangents.Length)
                        avgTangent += mesh.tangents[v];
                }
                newTangents.Add(avgTangent * invCount);
            }
            
            // Average UVs
            AverageUVChannel(mesh.uv0, clusterVertices, newUV0, invCount);
            AverageUVChannel(mesh.uv1, clusterVertices, newUV1, invCount);
            AverageUVChannel(mesh.uv2, clusterVertices, newUV2, invCount);
            AverageUVChannel(mesh.uv3, clusterVertices, newUV3, invCount);
        }

        /// <summary>
        /// Helper to average a UV channel for a cluster of vertices
        /// </summary>
        private void AverageUVChannel(
            List<Vector4> sourceUVs,
            List<int> clusterVertices,
            List<Vector4> targetUVs,
            float invCount)
        {
            if (targetUVs == null || sourceUVs == null || sourceUVs.Count == 0)
                return;
            
            Vector4 avgUV = Vector4.zero;
            foreach (int v in clusterVertices)
            {
                if (v < sourceUVs.Count)
                    avgUV += sourceUVs[v];
            }
            targetUVs.Add(avgUV * invCount);
        }

        /// <summary>
        /// Get list of vertex indices that are affected by the brush
        /// </summary>
        private List<int> GetAffectedVertices(PolyMesh mesh, float[] weights)
        {
            List<int> affectedVertices = new List<int>();
            
            for (int i = 0; i < mesh.vertices.Length && i < weights.Length; i++)
            {
                if (weights[i] > 0.01f)
                {
                    affectedVertices.Add(i);
                }
            }
            
            return affectedVertices;
        }

        /// <summary>
        /// Calculate merge threshold based on strength
        /// </summary>
        private float CalculateMergeThreshold(float strength)
        {
            // Map strength (0-1) to threshold range
            // Higher strength = larger threshold = more aggressive merging
            float baseThreshold = s_MergeThreshold.value;
            return baseThreshold * Mathf.Lerp(0.5f, 2.0f, strength);
        }

        /// <summary>
        /// Interpolate vertex attributes for new vertices created during subdivision
        /// </summary>
        internal void InterpolateAttributes(
            PolyMesh mesh,
            int newVertexIndex,
            int[] sourceVertices,
            float[] weights)
        {
            if (mesh == null || sourceVertices == null || sourceVertices.Length == 0)
                return;
            
            // Normalize weights
            float totalWeight = 0f;
            for (int i = 0; i < weights.Length; i++)
                totalWeight += weights[i];
            
            if (totalWeight < 0.0001f)
            {
                // Equal weights if no weights provided
                totalWeight = sourceVertices.Length;
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = 1f;
            }
            
            // Interpolate position
            Vector3 position = Vector3.zero;
            for (int i = 0; i < sourceVertices.Length; i++)
            {
                if (sourceVertices[i] < mesh.vertices.Length)
                    position += mesh.vertices[sourceVertices[i]] * (weights[i] / totalWeight);
            }
            mesh.vertices[newVertexIndex] = position;
            
            // Interpolate normals
            if (mesh.normals != null && mesh.normals.Length > newVertexIndex)
            {
                Vector3 normal = Vector3.zero;
                for (int i = 0; i < sourceVertices.Length; i++)
                {
                    if (sourceVertices[i] < mesh.normals.Length)
                        normal += mesh.normals[sourceVertices[i]] * (weights[i] / totalWeight);
                }
                mesh.normals[newVertexIndex] = normal.normalized;
            }
            
            // Interpolate colors
            if (mesh.colors != null && mesh.colors.Length > newVertexIndex)
            {
                Color color = Color.clear;
                for (int i = 0; i < sourceVertices.Length; i++)
                {
                    if (sourceVertices[i] < mesh.colors.Length)
                        color += mesh.colors[sourceVertices[i]] * (weights[i] / totalWeight);
                }
                mesh.colors[newVertexIndex] = color;
            }
            
            // Interpolate tangents
            if (mesh.tangents != null && mesh.tangents.Length > newVertexIndex)
            {
                Vector4 tangent = Vector4.zero;
                for (int i = 0; i < sourceVertices.Length; i++)
                {
                    if (sourceVertices[i] < mesh.tangents.Length)
                        tangent += mesh.tangents[sourceVertices[i]] * (weights[i] / totalWeight);
                }
                mesh.tangents[newVertexIndex] = tangent;
            }
            
            // Interpolate UVs
            for (int channel = 0; channel < 4; channel++)
            {
                List<Vector4> uvs = mesh.GetUVs(channel);
                if (uvs != null && uvs.Count > newVertexIndex)
                {
                    Vector4 uv = Vector4.zero;
                    for (int i = 0; i < sourceVertices.Length; i++)
                    {
                        if (sourceVertices[i] < uvs.Count)
                            uv += uvs[sourceVertices[i]] * (weights[i] / totalWeight);
                    }
                    uvs[newVertexIndex] = uv;
                }
            }
        }

        /// <summary>
        /// Validate that mesh remains manifold after topology operations
        /// </summary>
        internal bool ValidateMeshTopology(PolyMesh mesh)
        {
            if (mesh == null)
                return false;
            
            // Check minimum vertex count
            if (mesh.vertexCount < k_MinimumVertexCount)
                return false;
            
            int[] triangles = mesh.GetTriangles();
            
            // Check that we have valid triangles
            if (triangles == null || triangles.Length < 3)
                return false;
            
            // Check triangle count is multiple of 3
            if (triangles.Length % 3 != 0)
                return false;
            
            // Check all triangle indices are valid
            for (int i = 0; i < triangles.Length; i++)
            {
                if (triangles[i] < 0 || triangles[i] >= mesh.vertexCount)
                    return false;
            }
            
            // Check for degenerate triangles
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                
                // Check for duplicate indices in triangle
                if (v0 == v1 || v1 == v2 || v2 == v0)
                    return false;
                
                // Check for zero-area triangles
                if (IsTriangleDegenerate(mesh, v0, v1, v2))
                    return false;
            }
            
            // Check for non-manifold edges (edges shared by more than 2 triangles)
            Dictionary<(int, int), int> edgeCount = new Dictionary<(int, int), int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                
                IncrementEdgeCount(edgeCount, v0, v1);
                IncrementEdgeCount(edgeCount, v1, v2);
                IncrementEdgeCount(edgeCount, v2, v0);
            }
            
            // Non-manifold if any edge is shared by more than 2 triangles
            foreach (var count in edgeCount.Values)
            {
                if (count > 2)
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Helper to increment edge count in dictionary
        /// </summary>
        private void IncrementEdgeCount(Dictionary<(int, int), int> edgeCount, int v0, int v1)
        {
            // Create ordered edge key
            (int, int) edgeKey = v0 < v1 ? (v0, v1) : (v1, v0);
            
            if (edgeCount.ContainsKey(edgeKey))
                edgeCount[edgeKey]++;
            else
                edgeCount[edgeKey] = 1;
        }

        /// <summary>
        /// Get list of face indices that are affected by the brush
        /// </summary>
        private List<int> GetAffectedFaces(PolyMesh mesh, float[] weights)
        {
            List<int> affectedFaces = new List<int>();
            int[] triangles = mesh.GetTriangles();
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];
                
                // Check if any vertex of this triangle has weight
                if (v0 < weights.Length && v1 < weights.Length && v2 < weights.Length)
                {
                    float maxWeight = Mathf.Max(weights[v0], weights[v1], weights[v2]);
                    if (maxWeight > 0.01f)
                    {
                        affectedFaces.Add(i / 3);
                    }
                }
            }
            
            return affectedFaces;
        }

        /// <summary>
        /// Calculate number of subdivision iterations based on strength
        /// </summary>
        private int CalculateSubdivisionIterations(float strength)
        {
            // Map strength (0-1) to iterations (0 to s_SubdivisionIterations)
            int maxIterations = s_SubdivisionIterations.value;
            int iterations = Mathf.RoundToInt(strength * maxIterations);
            return Mathf.Max(1, iterations);
        }

        /// <summary>
        /// Check if a triangle is degenerate (zero area or coincident vertices)
        /// </summary>
        private bool IsTriangleDegenerate(PolyMesh mesh, int v0, int v1, int v2)
        {
            if (v0 >= mesh.vertices.Length || v1 >= mesh.vertices.Length || v2 >= mesh.vertices.Length)
                return true;
            
            Vector3 p0 = mesh.vertices[v0];
            Vector3 p1 = mesh.vertices[v1];
            Vector3 p2 = mesh.vertices[v2];
            
            return IsTriangleDegenerateByPositions(p0, p1, p2);
        }

        /// <summary>
        /// Check if a triangle is degenerate using vertex indices and a vertex list
        /// </summary>
        private bool IsTriangleDegenerateByIndices(int v0, int v1, int v2, List<Vector3> vertices)
        {
            if (v0 >= vertices.Count || v1 >= vertices.Count || v2 >= vertices.Count)
                return true;
            
            if (v0 < 0 || v1 < 0 || v2 < 0)
                return true;
            
            Vector3 p0 = vertices[v0];
            Vector3 p1 = vertices[v1];
            Vector3 p2 = vertices[v2];
            
            return IsTriangleDegenerateByPositions(p0, p1, p2);
        }

        /// <summary>
        /// Check if a triangle is degenerate based on vertex positions
        /// </summary>
        private bool IsTriangleDegenerateByPositions(Vector3 p0, Vector3 p1, Vector3 p2)
        {
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

        /// <summary>
        /// Subdivide a list of triangles using midpoint subdivision
        /// </summary>
        private void SubdivideTriangles(PolyMesh mesh, int[] originalTriangles, List<int> triangleIndices)
        {
            // Dictionary to track edge midpoints to avoid duplicates
            Dictionary<(int, int), int> edgeMidpoints = new Dictionary<(int, int), int>();
            
            // Lists to build new mesh data
            List<Vector3> newVertices = new List<Vector3>(mesh.vertices);
            List<Vector3> newNormals = mesh.normals != null ? new List<Vector3>(mesh.normals) : null;
            List<Color> newColors = mesh.colors != null ? new List<Color>(mesh.colors) : null;
            List<Vector4> newTangents = mesh.tangents != null ? new List<Vector4>(mesh.tangents) : null;
            List<Vector4> newUV0 = mesh.uv0 != null ? new List<Vector4>(mesh.uv0) : null;
            List<Vector4> newUV1 = mesh.uv1 != null ? new List<Vector4>(mesh.uv1) : null;
            List<Vector4> newUV2 = mesh.uv2 != null ? new List<Vector4>(mesh.uv2) : null;
            List<Vector4> newUV3 = mesh.uv3 != null ? new List<Vector4>(mesh.uv3) : null;
            
            List<int> newTriangles = new List<int>(originalTriangles);
            
            // Process each triangle to subdivide
            foreach (int triIndex in triangleIndices)
            {
                int idx0 = triIndex;
                int idx1 = triIndex + 1;
                int idx2 = triIndex + 2;
                
                if (idx2 >= originalTriangles.Length)
                    continue;
                
                int v0 = originalTriangles[idx0];
                int v1 = originalTriangles[idx1];
                int v2 = originalTriangles[idx2];
                
                // Get or create midpoint vertices for each edge
                int m01 = GetOrCreateMidpoint(v0, v1, edgeMidpoints, newVertices, newNormals, newColors, newTangents, newUV0, newUV1, newUV2, newUV3, mesh);
                int m12 = GetOrCreateMidpoint(v1, v2, edgeMidpoints, newVertices, newNormals, newColors, newTangents, newUV0, newUV1, newUV2, newUV3, mesh);
                int m20 = GetOrCreateMidpoint(v2, v0, edgeMidpoints, newVertices, newNormals, newColors, newTangents, newUV0, newUV1, newUV2, newUV3, mesh);
                
                // Replace original triangle with 4 new triangles
                // Only add triangles that are not degenerate
                
                // Triangle 1: v0, m01, m20
                if (!IsTriangleDegenerateByIndices(v0, m01, m20, newVertices))
                {
                    newTriangles[idx0] = v0;
                    newTriangles[idx1] = m01;
                    newTriangles[idx2] = m20;
                }
                else
                {
                    // Mark original triangle slots as invalid (will be removed later)
                    newTriangles[idx0] = -1;
                    newTriangles[idx1] = -1;
                    newTriangles[idx2] = -1;
                }
                
                // Triangle 2: m01, v1, m12
                if (!IsTriangleDegenerateByIndices(m01, v1, m12, newVertices))
                {
                    newTriangles.Add(m01);
                    newTriangles.Add(v1);
                    newTriangles.Add(m12);
                }
                
                // Triangle 3: m20, m12, v2
                if (!IsTriangleDegenerateByIndices(m20, m12, v2, newVertices))
                {
                    newTriangles.Add(m20);
                    newTriangles.Add(m12);
                    newTriangles.Add(v2);
                }
                
                // Triangle 4: m01, m12, m20 (center triangle)
                if (!IsTriangleDegenerateByIndices(m01, m12, m20, newVertices))
                {
                    newTriangles.Add(m01);
                    newTriangles.Add(m12);
                    newTriangles.Add(m20);
                }
            }
            
            // Remove invalid triangles (marked with -1)
            List<int> validTriangles = new List<int>();
            for (int i = 0; i < newTriangles.Count; i += 3)
            {
                if (i + 2 < newTriangles.Count && 
                    newTriangles[i] >= 0 && 
                    newTriangles[i + 1] >= 0 && 
                    newTriangles[i + 2] >= 0)
                {
                    validTriangles.Add(newTriangles[i]);
                    validTriangles.Add(newTriangles[i + 1]);
                    validTriangles.Add(newTriangles[i + 2]);
                }
            }
            newTriangles = validTriangles;
            
            // Check vertex limit using centralized error handling
            if (!ErrorHandling.CheckVertexLimitForSubdivision(mesh.vertexCount, newVertices.Count - mesh.vertexCount, use32BitIndices: false))
            {
                return;
            }
            
            // Update mesh with new data
            mesh.vertices = newVertices.ToArray();
            if (newNormals != null) mesh.normals = newNormals.ToArray();
            if (newColors != null) mesh.colors = newColors.ToArray();
            if (newTangents != null) mesh.tangents = newTangents.ToArray();
            if (newUV0 != null) mesh.uv0 = newUV0;
            if (newUV1 != null) mesh.uv1 = newUV1;
            if (newUV2 != null) mesh.uv2 = newUV2;
            if (newUV3 != null) mesh.uv3 = newUV3;
            
            // Update submesh triangles
            if (mesh.subMeshes != null && mesh.subMeshes.Length > 0)
            {
                mesh.subMeshes[0].indexes = newTriangles.ToArray();
            }
            
            // Force refresh of the triangle cache to reflect the new submesh data
            // This is critical because GetTriangles() may return cached data
            System.Reflection.FieldInfo trianglesField = typeof(PolyMesh).GetField("m_Triangles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (trianglesField != null)
            {
                trianglesField.SetValue(mesh, null);
            }
        }

        /// <summary>
        /// Get or create a midpoint vertex between two vertices
        /// </summary>
        private int GetOrCreateMidpoint(
            int v0, int v1,
            Dictionary<(int, int), int> edgeMidpoints,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector4> tangents,
            List<Vector4> uv0,
            List<Vector4> uv1,
            List<Vector4> uv2,
            List<Vector4> uv3,
            PolyMesh mesh)
        {
            // Create ordered edge key (smaller index first)
            (int, int) edgeKey = v0 < v1 ? (v0, v1) : (v1, v0);
            
            // Check if midpoint already exists
            if (edgeMidpoints.TryGetValue(edgeKey, out int existingMidpoint))
            {
                return existingMidpoint;
            }
            
            // Create new midpoint vertex
            int newIndex = vertices.Count;
            
            // Interpolate position
            vertices.Add((mesh.vertices[v0] + mesh.vertices[v1]) * 0.5f);
            
            // Interpolate normals
            if (normals != null && v0 < mesh.normals.Length && v1 < mesh.normals.Length)
            {
                Vector3 normal = ((mesh.normals[v0] + mesh.normals[v1]) * 0.5f).normalized;
                normals.Add(normal);
            }
            
            // Interpolate colors
            if (colors != null && v0 < mesh.colors.Length && v1 < mesh.colors.Length)
            {
                colors.Add((mesh.colors[v0] + mesh.colors[v1]) * 0.5f);
            }
            
            // Interpolate tangents
            if (tangents != null && v0 < mesh.tangents.Length && v1 < mesh.tangents.Length)
            {
                tangents.Add((mesh.tangents[v0] + mesh.tangents[v1]) * 0.5f);
            }
            
            // Interpolate UVs
            if (uv0 != null && v0 < mesh.uv0.Count && v1 < mesh.uv0.Count)
            {
                uv0.Add((mesh.uv0[v0] + mesh.uv0[v1]) * 0.5f);
            }
            if (uv1 != null && v0 < mesh.uv1.Count && v1 < mesh.uv1.Count)
            {
                uv1.Add((mesh.uv1[v0] + mesh.uv1[v1]) * 0.5f);
            }
            if (uv2 != null && v0 < mesh.uv2.Count && v1 < mesh.uv2.Count)
            {
                uv2.Add((mesh.uv2[v0] + mesh.uv2[v1]) * 0.5f);
            }
            if (uv3 != null && v0 < mesh.uv3.Count && v1 < mesh.uv3.Count)
            {
                uv3.Add((mesh.uv3[v0] + mesh.uv3[v1]) * 0.5f);
            }
            
            // Store midpoint for future lookups
            edgeMidpoints[edgeKey] = newIndex;
            
            return newIndex;
        }

        internal override void DrawGizmos(BrushTarget target, BrushSettings settings)
        {
            // Update mode for gizmo display
            UpdateModeFromModifierKeys(settings);

            UpdateBrushGizmosColor();

            // Draw brush with different colors based on mode
            Color modeColor = m_CurrentMode == TopologyMode.Subdivide ? 
                new Color(0f, 1f, 0f, 0.9f) :  // Green for subdivide
                new Color(1f, 0.5f, 0f, 0.9f);  // Orange for unsubdivide

            foreach (PolyRaycastHit hit in target.raycastHits)
            {
                PolyHandles.DrawBrush(
                    hit.position,
                    hit.normal,
                    settings,
                    target.localToWorldMatrix,
                    modeColor,
                    outerColor
                );
            }
        }

        #region Undo/Redo Support

        /// <summary>
        /// Called when an undo or redo operation is performed.
        /// Handles cleanup and synchronization after topology changes are undone/redone.
        /// </summary>
        /// <param name="modified">List of GameObjects that were modified</param>
        internal override void UndoRedoPerformed(List<GameObject> modified)
        {
            // Clear all cached mesh states since undo/redo may have changed them
            m_MeshStateCache.Clear();
            m_OperationData.Clear();

            // Call base implementation to handle mesh refresh
            base.UndoRedoPerformed(modified);

            // Synchronize mesh rendering and colliders after undo/redo
            foreach (GameObject go in modified)
            {
                if (go == null)
                    continue;

                // Get the PolybrushMesh component if it exists
                var polybrushMesh = go.GetComponent<PolybrushMesh>();
                if (polybrushMesh != null && polybrushMesh.polyMesh != null)
                {
                    // Update the mesh from the PolyMesh data
                    polybrushMesh.polyMesh.UpdateMeshFromData();
                    
                    // Update mesh collider if present
                    MeshCollider meshCollider = go.GetComponent<MeshCollider>();
                    if (meshCollider != null)
                    {
                        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                        {
                            // Force collider update by reassigning the mesh
                            Mesh mesh = meshFilter.sharedMesh;
                            meshCollider.sharedMesh = null;
                            meshCollider.sharedMesh = mesh;
                        }
                    }
                    
                    // Recalculate bounds on the mesh
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        mf.sharedMesh.RecalculateBounds();
                    }
                }

                // Handle ProBuilder mesh refresh if applicable
                if (ProBuilderBridge.IsValidProBuilderMesh(go))
                {
                    try
                    {
                        ProBuilderBridge.ToMesh(go);
                        ProBuilderBridge.Refresh(go, ProBuilderBridge.RefreshMask.All);
                        ProBuilderBridge.Optimize(go);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to refresh ProBuilder mesh after undo/redo: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Called when the brush finishes applying.
        /// Cleans up temporary data and ensures mesh state is properly saved.
        /// </summary>
        /// <param name="target">The target that was being modified</param>
        /// <param name="settings">Current brush settings</param>
        internal override void OnBrushFinishApply(BrushTarget target, BrushSettings settings)
        {
            base.OnBrushFinishApply(target, settings);

            // Clear the mesh state cache for this target since the operation is complete
            if (target != null && target.editableObject != null)
            {
                ClearMeshStateCache(target.editableObject);
            }
        }

        #endregion

        #region ProBuilder Integration

        /// <summary>
        /// Checks if the given editable object is a ProBuilder mesh.
        /// </summary>
        /// <param name="editableObject">The editable object to check</param>
        /// <returns>True if the object is a ProBuilder mesh, false otherwise</returns>
        internal static bool IsProBuilderMesh(EditableObject editableObject)
        {
            if (editableObject == null || editableObject.gameObjectAttached == null)
                return false;

            return editableObject.isProBuilderObject;
        }

        /// <summary>
        /// Refreshes the ProBuilder mesh after topology operations.
        /// This ensures that ProBuilder's internal data structures are updated
        /// to reflect the changes made during subdivision or unsubdivision.
        /// </summary>
        /// <param name="editableObject">The editable object containing the ProBuilder mesh</param>
        /// <param name="vertexCountChanged">Whether the vertex count has changed</param>
        internal static void RefreshProBuilderMesh(EditableObject editableObject, bool vertexCountChanged)
        {
            if (!IsProBuilderMesh(editableObject))
                return;

            GameObject gameObject = editableObject.gameObjectAttached;
            PolyMesh editMesh = editableObject.editMesh;

            if (gameObject == null || editMesh == null)
                return;

            // Update ProBuilder's vertex positions from the edit mesh
            ProBuilderBridge.SetPositions(gameObject, editMesh.vertices);

            // Update tangents if available
            if (editMesh.tangents != null && editMesh.tangents.Length == editMesh.vertexCount)
            {
                ProBuilderBridge.SetTangents(gameObject, editMesh.tangents);
            }

            // Update colors if available
            if (editMesh.colors != null && editMesh.colors.Length == editMesh.vertexCount)
            {
                Color[] colors = System.Array.ConvertAll(editMesh.colors, x => (Color)x);
                ProBuilderBridge.SetColors(gameObject, colors);
            }

            // Rebuild the ProBuilder mesh to reflect topology changes
            // ToMesh rebuilds the Unity mesh from ProBuilder's internal data
            ProBuilderBridge.ToMesh(gameObject);

            // Refresh all mesh attributes (UVs, colors, normals, tangents, collisions)
            ProBuilderBridge.Refresh(gameObject, ProBuilderBridge.RefreshMask.All);

            // Optimize the mesh if vertex count changed (consolidates vertices, etc.)
            if (vertexCountChanged)
            {
                ProBuilderBridge.Optimize(gameObject);
            }

            // Refresh the ProBuilder editor to update the scene view
            ProBuilderBridge.RefreshEditor(vertexCountChanged);
        }

        /// <summary>
        /// Synchronizes ProBuilder mesh data before topology operations.
        /// This ensures that the edit mesh has the latest data from ProBuilder.
        /// </summary>
        /// <param name="editableObject">The editable object containing the ProBuilder mesh</param>
        internal static void SyncProBuilderMeshBeforeOperation(EditableObject editableObject)
        {
            if (!IsProBuilderMesh(editableObject))
                return;

            GameObject gameObject = editableObject.gameObjectAttached;

            if (gameObject == null)
                return;

            // Ensure ProBuilder mesh is in sync before we modify it
            // ToMesh ensures the Unity mesh reflects ProBuilder's internal state
            ProBuilderBridge.ToMesh(gameObject);
            ProBuilderBridge.Refresh(gameObject);
        }

        /// <summary>
        /// Validates that a ProBuilder mesh is still valid after topology operations.
        /// </summary>
        /// <param name="editableObject">The editable object to validate</param>
        /// <returns>True if the mesh is valid, false otherwise</returns>
        internal static bool ValidateProBuilderMesh(EditableObject editableObject)
        {
            if (!IsProBuilderMesh(editableObject))
                return true; // Non-ProBuilder meshes don't need this validation

            GameObject gameObject = editableObject.gameObjectAttached;

            if (gameObject == null)
                return false;

            // Check if ProBuilder still considers this a valid mesh
            return ProBuilderBridge.IsValidProBuilderMesh(gameObject);
        }

        #endregion
    }
}

