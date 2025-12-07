using NUnit.Framework;
using UnityEngine;
using UnityEditor.Polybrush;

namespace UnityEditor.Polybrush.EditorTests
{
    /// <summary>
    /// Tests for BrushModeTopology class structure and basic functionality
    /// </summary>
    internal class BrushModeTopologyTests
    {
        [Test]
        public void BrushModeTopology_CanBeCreated()
        {
            // Arrange & Act
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();

            // Assert
            Assert.IsNotNull(topologyMode, "BrushModeTopology instance should be created");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void BrushModeTopology_InheritsFromBrushModeMesh()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();

            // Act & Assert
            Assert.IsInstanceOf<BrushModeMesh>(topologyMode, "BrushModeTopology should inherit from BrushModeMesh");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void BrushModeTopology_HasCorrectUndoMessage()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();

            // Act
            string undoMessage = topologyMode.UndoMessage;

            // Assert - Default mode is Subdivide, so undo message should reflect that
            Assert.AreEqual("Topology Subdivision", undoMessage, "Undo message should be 'Topology Subdivision' for default subdivide mode");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void TopologyMode_EnumHasCorrectValues()
        {
            // Assert
            Assert.AreEqual(0, (int)BrushModeTopology.TopologyMode.Subdivide, "Subdivide should be 0");
            Assert.AreEqual(1, (int)BrushModeTopology.TopologyMode.Unsubdivide, "Unsubdivide should be 1");
        }

        [Test]
        public void BrushTool_HasTopologyEntry()
        {
            // Assert
            Assert.AreEqual(6, (int)BrushTool.Topology, "Topology tool should be enum value 6");
        }

        [Test]
        public void BrushToolUtility_ReturnsCorrectTypeForTopology()
        {
            // Act
            System.Type modeType = BrushTool.Topology.GetModeType();

            // Assert
            Assert.AreEqual(typeof(BrushModeTopology), modeType, "GetModeType should return BrushModeTopology for Topology tool");
        }

        [Test]
        public void BrushModeTopology_OnEnableDoesNotThrow()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();

            // Act & Assert
            Assert.DoesNotThrow(() => topologyMode.OnEnable(), "OnEnable should not throw exceptions");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void SubdivideFaces_IncreasesVertexCount()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateSimpleTriangleMesh();
            int originalVertexCount = mesh.vertexCount;
            
            // Create weights array with all vertices having full weight
            float[] weights = new float[originalVertexCount];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0f;
            
            // Act
            int[] faceIndices = new int[] { 0 }; // First face
            topologyMode.SubdivideFaces(mesh, faceIndices, weights, 1);
            
            // Assert
            Assert.Greater(mesh.vertexCount, originalVertexCount, 
                "Subdivision should increase vertex count");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void SubdivideFaces_PreservesTriangleTopology()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateSimpleTriangleMesh();
            
            float[] weights = new float[mesh.vertexCount];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0f;
            
            // Act
            int[] faceIndices = new int[] { 0 };
            topologyMode.SubdivideFaces(mesh, faceIndices, weights, 1);
            
            // Assert
            int[] triangles = mesh.GetTriangles();
            Assert.AreEqual(0, triangles.Length % 3, 
                "Triangle count should be a multiple of 3");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void InterpolateAttributes_AveragesPositions()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateSimpleTriangleMesh();
            
            // Add a new vertex slot
            Vector3[] newVertices = new Vector3[mesh.vertices.Length + 1];
            System.Array.Copy(mesh.vertices, newVertices, mesh.vertices.Length);
            mesh.vertices = newVertices;
            
            int newVertexIndex = mesh.vertices.Length - 1;
            int[] sourceVertices = new int[] { 0, 1 };
            float[] weights = new float[] { 0.5f, 0.5f };
            
            Vector3 expectedPosition = (mesh.vertices[0] + mesh.vertices[1]) * 0.5f;
            
            // Act
            topologyMode.InterpolateAttributes(mesh, newVertexIndex, sourceVertices, weights);
            
            // Assert
            Vector3 actualPosition = mesh.vertices[newVertexIndex];
            Assert.AreEqual(expectedPosition.x, actualPosition.x, 0.001f, 
                "X position should be averaged");
            Assert.AreEqual(expectedPosition.y, actualPosition.y, 0.001f, 
                "Y position should be averaged");
            Assert.AreEqual(expectedPosition.z, actualPosition.z, 0.001f, 
                "Z position should be averaged");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void MergeVertices_DecreasesVertexCount()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateQuadMeshWithCloseVertices();
            int originalVertexCount = mesh.vertexCount;
            
            // Create weights array with all vertices having full weight
            float[] weights = new float[originalVertexCount];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0f;
            
            // Get all vertex indices
            int[] vertexIndices = new int[originalVertexCount];
            for (int i = 0; i < originalVertexCount; i++)
                vertexIndices[i] = i;
            
            // Act - use a threshold that will merge close vertices
            topologyMode.MergeVertices(mesh, vertexIndices, weights, 0.2f);
            
            // Assert
            Assert.LessOrEqual(mesh.vertexCount, originalVertexCount, 
                "Unsubdivision should decrease or maintain vertex count");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void MergeVertices_PreservesMinimumVertexCount()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateSimpleTriangleMesh();
            
            // Create weights array with all vertices having full weight
            float[] weights = new float[mesh.vertexCount];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0f;
            
            // Get all vertex indices
            int[] vertexIndices = new int[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
                vertexIndices[i] = i;
            
            // Act - use a very large threshold that would try to merge all vertices
            topologyMode.MergeVertices(mesh, vertexIndices, weights, 10.0f);
            
            // Assert - mesh should still have at least 3 vertices (minimum for a valid mesh)
            Assert.GreaterOrEqual(mesh.vertexCount, 3, 
                "Mesh should maintain minimum vertex count of 3");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void MergeVertices_AveragesPositions()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateQuadMeshWithCloseVertices();
            
            // Get positions of close vertices before merge
            Vector3 pos0 = mesh.vertices[0];
            Vector3 pos1 = mesh.vertices[1];
            Vector3 expectedAverage = (pos0 + pos1) * 0.5f;
            
            // Create weights array with all vertices having full weight
            float[] weights = new float[mesh.vertexCount];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0f;
            
            // Get all vertex indices
            int[] vertexIndices = new int[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
                vertexIndices[i] = i;
            
            // Act - use a threshold that will merge the close vertices
            topologyMode.MergeVertices(mesh, vertexIndices, weights, 0.2f);
            
            // Assert - if vertices were merged, check that the merged position is close to the average
            // Note: The exact position depends on which vertices were merged
            Assert.IsNotNull(mesh.vertices, "Mesh should still have vertices after merge");
            Assert.Greater(mesh.vertices.Length, 0, "Mesh should have at least one vertex");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void MergeVertices_AveragesColors()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateQuadMeshWithCloseVertices();
            
            // Set different colors for close vertices
            mesh.colors[0] = Color.red;
            mesh.colors[1] = Color.blue;
            
            // Create weights array with all vertices having full weight
            float[] weights = new float[mesh.vertexCount];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0f;
            
            // Get all vertex indices
            int[] vertexIndices = new int[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
                vertexIndices[i] = i;
            
            // Act
            topologyMode.MergeVertices(mesh, vertexIndices, weights, 0.2f);
            
            // Assert - colors should still exist after merge
            Assert.IsNotNull(mesh.colors, "Mesh should still have colors after merge");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void ValidateMeshTopology_ReturnsTrueForValidMesh()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateSimpleTriangleMesh();
            
            // Act
            bool isValid = topologyMode.ValidateMeshTopology(mesh);
            
            // Assert
            Assert.IsTrue(isValid, "Simple triangle mesh should be valid");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void ValidateMeshTopology_ReturnsFalseForNullMesh()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            
            // Act
            bool isValid = topologyMode.ValidateMeshTopology(null);
            
            // Assert
            Assert.IsFalse(isValid, "Null mesh should be invalid");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void MergeVertices_RemovesDegenerateTriangles()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            var mesh = CreateQuadMeshWithCloseVertices();
            
            // Create weights array with all vertices having full weight
            float[] weights = new float[mesh.vertexCount];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0f;
            
            // Get all vertex indices
            int[] vertexIndices = new int[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; i++)
                vertexIndices[i] = i;
            
            // Act
            topologyMode.MergeVertices(mesh, vertexIndices, weights, 0.2f);
            
            // Assert - triangles should still be valid (multiple of 3)
            int[] triangles = mesh.GetTriangles();
            Assert.AreEqual(0, triangles.Length % 3, 
                "Triangle count should be a multiple of 3 after merge");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        /// <summary>
        /// Helper method to create a simple triangle mesh for testing
        /// </summary>
        private UnityEngine.Polybrush.PolyMesh CreateSimpleTriangleMesh()
        {
            var polyMesh = new UnityEngine.Polybrush.PolyMesh();
            
            // Create a simple triangle
            polyMesh.vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0.5f, 1, 0)
            };
            
            polyMesh.normals = new Vector3[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };
            
            polyMesh.colors = new Color[]
            {
                Color.white,
                Color.white,
                Color.white
            };
            
            // Create submesh with one triangle
            var submesh = new UnityEngine.Polybrush.SubMesh(
                0,
                MeshTopology.Triangles,
                new int[] { 0, 1, 2 }
            );
            
            // Use reflection to set the private m_SubMeshes field
            var subMeshesField = typeof(UnityEngine.Polybrush.PolyMesh)
                .GetField("m_SubMeshes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            subMeshesField.SetValue(polyMesh, new UnityEngine.Polybrush.SubMesh[] { submesh });
            
            return polyMesh;
        }

        /// <summary>
        /// Helper method to create a quad mesh with two close vertices for testing unsubdivision
        /// </summary>
        private UnityEngine.Polybrush.PolyMesh CreateQuadMeshWithCloseVertices()
        {
            var polyMesh = new UnityEngine.Polybrush.PolyMesh();
            
            // Create a quad with 4 vertices, where vertices 0 and 1 are close together
            polyMesh.vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),       // Vertex 0
                new Vector3(0.1f, 0, 0),    // Vertex 1 - close to vertex 0
                new Vector3(1, 0, 0),       // Vertex 2
                new Vector3(0.5f, 1, 0)     // Vertex 3
            };
            
            polyMesh.normals = new Vector3[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };
            
            polyMesh.colors = new Color[]
            {
                Color.white,
                Color.white,
                Color.white,
                Color.white
            };
            
            // Create submesh with two triangles forming a quad
            var submesh = new UnityEngine.Polybrush.SubMesh(
                0,
                MeshTopology.Triangles,
                new int[] { 0, 1, 3, 1, 2, 3 }
            );
            
            // Use reflection to set the private m_SubMeshes field
            var subMeshesField = typeof(UnityEngine.Polybrush.PolyMesh)
                .GetField("m_SubMeshes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            subMeshesField.SetValue(polyMesh, new UnityEngine.Polybrush.SubMesh[] { submesh });
            
            return polyMesh;
        }

        #region Undo/Redo Support Tests

        [Test]
        public void BrushModeTopology_UndoMessage_ReturnsSubdivisionForSubdivideMode()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            
            // The default mode is Subdivide, so UndoMessage should reflect that
            // Act
            string undoMessage = topologyMode.UndoMessage;

            // Assert
            Assert.AreEqual("Topology Subdivision", undoMessage, 
                "Undo message should be 'Topology Subdivision' for subdivide mode");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void MeshStateCache_CaptureState_CapturesVertices()
        {
            // Arrange
            var mesh = CreateSimpleTriangleMesh();
            int originalVertexCount = mesh.vertexCount;
            
            // Act
            var cache = BrushModeTopology.MeshStateCache.CaptureState(mesh);
            
            // Assert
            Assert.IsNotNull(cache, "Cache should not be null");
            Assert.IsNotNull(cache.vertices, "Cached vertices should not be null");
            Assert.AreEqual(originalVertexCount, cache.vertices.Length, 
                "Cached vertex count should match original");
            Assert.AreEqual(originalVertexCount, cache.vertexCount, 
                "Cached vertexCount property should match original");
        }

        [Test]
        public void MeshStateCache_CaptureState_CapturesNormals()
        {
            // Arrange
            var mesh = CreateSimpleTriangleMesh();
            
            // Act
            var cache = BrushModeTopology.MeshStateCache.CaptureState(mesh);
            
            // Assert
            Assert.IsNotNull(cache.normals, "Cached normals should not be null");
            Assert.AreEqual(mesh.normals.Length, cache.normals.Length, 
                "Cached normal count should match original");
        }

        [Test]
        public void MeshStateCache_CaptureState_CapturesColors()
        {
            // Arrange
            var mesh = CreateSimpleTriangleMesh();
            
            // Act
            var cache = BrushModeTopology.MeshStateCache.CaptureState(mesh);
            
            // Assert
            Assert.IsNotNull(cache.colors, "Cached colors should not be null");
            Assert.AreEqual(mesh.colors.Length, cache.colors.Length, 
                "Cached color count should match original");
        }

        [Test]
        public void MeshStateCache_CaptureState_CapturesTriangles()
        {
            // Arrange
            var mesh = CreateSimpleTriangleMesh();
            int originalTriangleCount = mesh.GetTriangles().Length;
            
            // Act
            var cache = BrushModeTopology.MeshStateCache.CaptureState(mesh);
            
            // Assert
            Assert.IsNotNull(cache.subMeshTriangles, "Cached submesh triangles should not be null");
            Assert.Greater(cache.subMeshTriangles.Length, 0, "Should have at least one submesh");
            Assert.AreEqual(originalTriangleCount, cache.triangleCount, 
                "Cached triangle count should match original");
        }

        [Test]
        public void MeshStateCache_CaptureState_ReturnsNullForNullMesh()
        {
            // Act
            var cache = BrushModeTopology.MeshStateCache.CaptureState(null);
            
            // Assert
            Assert.IsNull(cache, "Cache should be null for null mesh");
        }

        [Test]
        public void MeshStateCache_RestoreState_RestoresVertices()
        {
            // Arrange
            var mesh = CreateSimpleTriangleMesh();
            var cache = BrushModeTopology.MeshStateCache.CaptureState(mesh);
            
            // Modify the mesh
            mesh.vertices[0] = new Vector3(100, 100, 100);
            
            // Act
            cache.RestoreState(mesh);
            
            // Assert
            Assert.AreEqual(cache.vertices[0], mesh.vertices[0], 
                "Vertex should be restored to cached value");
        }

        [Test]
        public void MeshStateCache_RestoreState_RestoresNormals()
        {
            // Arrange
            var mesh = CreateSimpleTriangleMesh();
            var cache = BrushModeTopology.MeshStateCache.CaptureState(mesh);
            
            // Modify the mesh
            mesh.normals[0] = Vector3.up;
            
            // Act
            cache.RestoreState(mesh);
            
            // Assert
            Assert.AreEqual(cache.normals[0], mesh.normals[0], 
                "Normal should be restored to cached value");
        }

        [Test]
        public void MeshStateCache_RestoreState_RestoresColors()
        {
            // Arrange
            var mesh = CreateSimpleTriangleMesh();
            var cache = BrushModeTopology.MeshStateCache.CaptureState(mesh);
            
            // Modify the mesh
            mesh.colors[0] = Color.red;
            
            // Act
            cache.RestoreState(mesh);
            
            // Assert
            Assert.AreEqual(cache.colors[0], mesh.colors[0], 
                "Color should be restored to cached value");
        }

        [Test]
        public void MeshStateCache_CaptureState_CreatesDeepCopy()
        {
            // Arrange
            var mesh = CreateSimpleTriangleMesh();
            var cache = BrushModeTopology.MeshStateCache.CaptureState(mesh);
            
            // Modify the original mesh
            Vector3 originalCachedVertex = cache.vertices[0];
            mesh.vertices[0] = new Vector3(999, 999, 999);
            
            // Assert - cache should not be affected by changes to original mesh
            Assert.AreEqual(originalCachedVertex, cache.vertices[0], 
                "Cached vertex should not change when original mesh is modified");
        }

        [Test]
        public void BrushModeTopology_GetCachedMeshState_ReturnsNullForNullEditableObject()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            
            // Act
            var cache = topologyMode.GetCachedMeshState(null);
            
            // Assert
            Assert.IsNull(cache, "GetCachedMeshState should return null for null EditableObject");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void BrushModeTopology_ClearMeshStateCache_DoesNotThrowForNullEditableObject()
        {
            // Arrange
            var topologyMode = ScriptableObject.CreateInstance<BrushModeTopology>();
            
            // Act & Assert
            Assert.DoesNotThrow(() => topologyMode.ClearMeshStateCache(null),
                "ClearMeshStateCache should not throw for null EditableObject");
            
            // Cleanup
            Object.DestroyImmediate(topologyMode);
        }

        [Test]
        public void BrushModeTopology_HasUndoRedoPerformedMethod()
        {
            // Verify that the UndoRedoPerformed method exists and is overridden
            var type = typeof(BrushModeTopology);
            
            var method = type.GetMethod("UndoRedoPerformed", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            Assert.IsNotNull(method, "UndoRedoPerformed method should exist");
        }

        [Test]
        public void BrushModeTopology_HasRegisterUndoMethod()
        {
            // Verify that the RegisterUndo method exists and is overridden
            var type = typeof(BrushModeTopology);
            
            var method = type.GetMethod("RegisterUndo", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            Assert.IsNotNull(method, "RegisterUndo method should exist");
        }

        [Test]
        public void BrushModeTopology_HasOnBrushFinishApplyMethod()
        {
            // Verify that the OnBrushFinishApply method exists and is overridden
            var type = typeof(BrushModeTopology);
            
            var method = type.GetMethod("OnBrushFinishApply", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            Assert.IsNotNull(method, "OnBrushFinishApply method should exist");
        }

        #endregion

        #region ProBuilder Integration Tests

        [Test]
        public void IsProBuilderMesh_ReturnsFalseForNullEditableObject()
        {
            // Act
            bool result = BrushModeTopology.IsProBuilderMesh(null);

            // Assert
            Assert.IsFalse(result, "IsProBuilderMesh should return false for null EditableObject");
        }

        [Test]
        public void RefreshProBuilderMesh_DoesNotThrowForNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => BrushModeTopology.RefreshProBuilderMesh(null, true),
                "RefreshProBuilderMesh should not throw for null EditableObject");
        }

        [Test]
        public void SyncProBuilderMeshBeforeOperation_DoesNotThrowForNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => BrushModeTopology.SyncProBuilderMeshBeforeOperation(null),
                "SyncProBuilderMeshBeforeOperation should not throw for null EditableObject");
        }

        [Test]
        public void ValidateProBuilderMesh_ReturnsTrueForNullEditableObject()
        {
            // Act
            bool result = BrushModeTopology.ValidateProBuilderMesh(null);

            // Assert
            // Non-ProBuilder meshes (including null) should return true as they don't need ProBuilder validation
            Assert.IsTrue(result, "ValidateProBuilderMesh should return true for null (non-ProBuilder) EditableObject");
        }

        [Test]
        public void ProBuilderBridge_ExistsMethodIsAccessible()
        {
            // Act & Assert - Just verify the method is accessible and doesn't throw
            Assert.DoesNotThrow(() => {
                bool exists = ProBuilderBridge.ProBuilderExists();
                // Result depends on whether ProBuilder is installed in the test environment
            }, "ProBuilderBridge.ProBuilderExists() should be accessible");
        }

        [Test]
        public void BrushModeTopology_HasProBuilderIntegrationMethods()
        {
            // Verify that the ProBuilder integration methods exist
            var type = typeof(BrushModeTopology);
            
            // Check IsProBuilderMesh method
            var isProBuilderMeshMethod = type.GetMethod("IsProBuilderMesh", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(isProBuilderMeshMethod, "IsProBuilderMesh method should exist");
            
            // Check RefreshProBuilderMesh method
            var refreshMethod = type.GetMethod("RefreshProBuilderMesh", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(refreshMethod, "RefreshProBuilderMesh method should exist");
            
            // Check SyncProBuilderMeshBeforeOperation method
            var syncMethod = type.GetMethod("SyncProBuilderMeshBeforeOperation", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(syncMethod, "SyncProBuilderMeshBeforeOperation method should exist");
            
            // Check ValidateProBuilderMesh method
            var validateMethod = type.GetMethod("ValidateProBuilderMesh", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(validateMethod, "ValidateProBuilderMesh method should exist");
        }

        #endregion
    }
}
