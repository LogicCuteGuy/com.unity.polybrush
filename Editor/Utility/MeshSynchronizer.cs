using UnityEngine;
using UnityEngine.Polybrush;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Utility class for synchronizing mesh data with Unity's rendering and physics systems.
    /// Handles immediate updates to mesh colliders, rendering data, and bounds after topology changes.
    /// </summary>
    internal static class MeshSynchronizer
    {
        /// <summary>
        /// Performs a complete mesh synchronization after topology changes.
        /// This includes updating the mesh collider, refreshing rendering data, and recalculating bounds.
        /// </summary>
        /// <param name="editableObject">The editable object containing the mesh to synchronize</param>
        /// <param name="forceColliderUpdate">Force collider update even if the setting is disabled</param>
        public static void SynchronizeAfterTopologyChange(EditableObject editableObject, bool forceColliderUpdate = true)
        {
            if (editableObject == null || !editableObject.IsValid)
                return;

            // Clear compute buffers since topology has changed
            ClearMeshBuffers(editableObject);

            // Refresh rendering data
            RefreshRenderingData(editableObject);

            // Recalculate mesh bounds
            RecalculateBounds(editableObject);

            // Update mesh collider
            UpdateMeshCollider(editableObject, forceColliderUpdate);

            // Mark the object as dirty for the editor
            MarkDirty(editableObject);
        }

        /// <summary>
        /// Updates the mesh collider immediately after topology changes.
        /// </summary>
        /// <param name="editableObject">The editable object containing the mesh</param>
        /// <param name="forceUpdate">Force update even if the rebuild collisions setting is disabled</param>
        public static void UpdateMeshCollider(EditableObject editableObject, bool forceUpdate = false)
        {
            if (editableObject == null || editableObject.gameObjectAttached == null)
                return;

            // Check if we should update colliders (either forced or setting enabled)
            if (!forceUpdate && !EditableObject.s_RebuildCollisions.value)
                return;

            MeshCollider meshCollider = editableObject.gameObjectAttached.GetComponent<MeshCollider>();
            if (meshCollider == null)
                return;

            Mesh graphicsMesh = editableObject.graphicsMesh;
            if (graphicsMesh == null)
                return;

            // Record undo for the collider change
            Undo.RecordObject(meshCollider, "Update Mesh Collider");

            // Force the mesh collider to update by reassigning the mesh
            // This is necessary because Unity doesn't automatically detect mesh changes
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = graphicsMesh;
        }


        /// <summary>
        /// Refreshes the rendering data for the mesh.
        /// This ensures that the graphics mesh is updated with the latest vertex and triangle data.
        /// </summary>
        /// <param name="editableObject">The editable object containing the mesh</param>
        public static void RefreshRenderingData(EditableObject editableObject)
        {
            if (editableObject == null || !editableObject.IsValid)
                return;

            PolyMesh editMesh = editableObject.editMesh;
            Mesh graphicsMesh = editableObject.graphicsMesh;

            if (editMesh == null || graphicsMesh == null)
                return;

            // Apply all mesh attributes to the Unity mesh
            // This includes vertices, normals, colors, tangents, UVs, and triangles
            editMesh.ApplyAttributesToUnityMesh(graphicsMesh, MeshChannel.All);

            // Upload mesh data to the GPU for rendering
            graphicsMesh.UploadMeshData(false);

            // Mark the renderer as dirty to ensure it updates
            Renderer renderer = editableObject.gameObjectAttached.GetComponent<Renderer>();
            if (renderer != null)
            {
                EditorUtility.SetDirty(renderer);
            }
        }

        /// <summary>
        /// Recalculates the mesh bounds after topology changes.
        /// This ensures that culling and other bounds-dependent operations work correctly.
        /// </summary>
        /// <param name="editableObject">The editable object containing the mesh</param>
        public static void RecalculateBounds(EditableObject editableObject)
        {
            if (editableObject == null || !editableObject.IsValid)
                return;

            Mesh graphicsMesh = editableObject.graphicsMesh;
            if (graphicsMesh == null)
                return;

            // Recalculate the bounding box of the mesh
            graphicsMesh.RecalculateBounds();
        }

        /// <summary>
        /// Clears the compute buffers for the mesh.
        /// This is necessary after topology changes because the buffer sizes may have changed.
        /// </summary>
        /// <param name="editableObject">The editable object containing the mesh</param>
        public static void ClearMeshBuffers(EditableObject editableObject)
        {
            if (editableObject == null)
                return;

            // Clear buffers on the edit mesh
            PolyMesh editMesh = editableObject.editMesh;
            if (editMesh != null)
            {
                editMesh.ClearBuffers();
            }

            // Also clear buffers on the visual mesh if it's different
            PolyMesh visualMesh = editableObject.visualMesh;
            if (visualMesh != null && visualMesh != editMesh)
            {
                visualMesh.ClearBuffers();
            }
        }

        /// <summary>
        /// Marks the editable object and its components as dirty for the editor.
        /// This ensures that changes are saved and displayed correctly.
        /// </summary>
        /// <param name="editableObject">The editable object to mark as dirty</param>
        public static void MarkDirty(EditableObject editableObject)
        {
            if (editableObject == null || editableObject.gameObjectAttached == null)
                return;

            // Mark the game object as dirty
            EditorUtility.SetDirty(editableObject.gameObjectAttached);

            // Mark the mesh filter as dirty if present
            MeshFilter meshFilter = editableObject.meshFilter;
            if (meshFilter != null)
            {
                EditorUtility.SetDirty(meshFilter);
            }

            // Mark the PolybrushMesh component as dirty
            PolybrushMesh polybrushMesh = editableObject.polybrushMesh;
            if (polybrushMesh != null)
            {
                EditorUtility.SetDirty(polybrushMesh);
            }

            // Mark the graphics mesh as dirty
            Mesh graphicsMesh = editableObject.graphicsMesh;
            if (graphicsMesh != null)
            {
                EditorUtility.SetDirty(graphicsMesh);
            }
        }

        /// <summary>
        /// Performs a lightweight synchronization for incremental updates.
        /// Use this for frequent updates during brush strokes.
        /// </summary>
        /// <param name="editableObject">The editable object containing the mesh</param>
        public static void SynchronizeIncremental(EditableObject editableObject)
        {
            if (editableObject == null || !editableObject.IsValid)
                return;

            // For incremental updates, we only refresh rendering data and bounds
            // Collider updates are deferred until the brush stroke ends
            RefreshRenderingData(editableObject);
            RecalculateBounds(editableObject);
        }

        /// <summary>
        /// Validates that the mesh is in a consistent state after topology changes.
        /// Returns true if the mesh is valid, false otherwise.
        /// </summary>
        /// <param name="editableObject">The editable object containing the mesh</param>
        /// <returns>True if the mesh is valid, false otherwise</returns>
        public static bool ValidateMeshState(EditableObject editableObject)
        {
            if (editableObject == null || !editableObject.IsValid)
                return false;

            PolyMesh editMesh = editableObject.editMesh;
            if (editMesh == null)
                return false;

            // Check vertex count
            if (editMesh.vertexCount < 3)
                return false;

            // Check triangle count
            int[] triangles = editMesh.GetTriangles();
            if (triangles == null || triangles.Length < 3)
                return false;

            // Check that triangle count is a multiple of 3
            if (triangles.Length % 3 != 0)
                return false;

            // Check that all triangle indices are valid
            for (int i = 0; i < triangles.Length; i++)
            {
                if (triangles[i] < 0 || triangles[i] >= editMesh.vertexCount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the editable object is a ProBuilder mesh.
        /// </summary>
        /// <param name="editableObject">The editable object to check</param>
        /// <returns>True if the object is a ProBuilder mesh, false otherwise</returns>
        public static bool IsProBuilderMesh(EditableObject editableObject)
        {
            if (editableObject == null || editableObject.gameObjectAttached == null)
                return false;

            return editableObject.isProBuilderObject;
        }

        /// <summary>
        /// Performs a complete synchronization for ProBuilder meshes after topology changes.
        /// This includes updating ProBuilder's internal data structures and refreshing the editor.
        /// </summary>
        /// <param name="editableObject">The editable object containing the ProBuilder mesh</param>
        /// <param name="vertexCountChanged">Whether the vertex count has changed</param>
        public static void SynchronizeProBuilderMesh(EditableObject editableObject, bool vertexCountChanged)
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
            ProBuilderBridge.ToMesh(gameObject);

            // Refresh all mesh attributes
            ProBuilderBridge.Refresh(gameObject, ProBuilderBridge.RefreshMask.All);

            // Optimize the mesh if vertex count changed
            if (vertexCountChanged)
            {
                ProBuilderBridge.Optimize(gameObject);
            }

            // Refresh the ProBuilder editor
            ProBuilderBridge.RefreshEditor(vertexCountChanged);
        }
    }
}
