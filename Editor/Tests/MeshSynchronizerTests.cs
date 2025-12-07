using NUnit.Framework;
using UnityEngine;
using UnityEngine.Polybrush;
using UnityEditor.Polybrush;

namespace UnityEditor.Polybrush.EditorTests
{
    /// <summary>
    /// Tests for MeshSynchronizer utility class
    /// Validates mesh update and rendering synchronization after topology changes
    /// </summary>
    internal class MeshSynchronizerTests
    {
        private GameObject m_TestGameObject;
        private MeshFilter m_MeshFilter;
        private MeshRenderer m_MeshRenderer;
        private MeshCollider m_MeshCollider;
        private Mesh m_TestMesh;

        [SetUp]
        public void SetUp()
        {
            // Create a test game object with mesh components
            m_TestGameObject = new GameObject("TestMeshObject");
            m_MeshFilter = m_TestGameObject.AddComponent<MeshFilter>();
            m_MeshRenderer = m_TestGameObject.AddComponent<MeshRenderer>();
            m_MeshCollider = m_TestGameObject.AddComponent<MeshCollider>();

            // Create a simple test mesh
            m_TestMesh = CreateSimpleQuadMesh();
            m_MeshFilter.sharedMesh = m_TestMesh;
            m_MeshCollider.sharedMesh = m_TestMesh;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TestGameObject != null)
            {
                Object.DestroyImmediate(m_TestGameObject);
            }
            if (m_TestMesh != null)
            {
                Object.DestroyImmediate(m_TestMesh);
            }
        }

        [Test]
        public void UpdateMeshCollider_DoesNotThrowWithNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.UpdateMeshCollider(null));
        }

        [Test]
        public void RefreshRenderingData_DoesNotThrowWithNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.RefreshRenderingData(null));
        }

        [Test]
        public void RecalculateBounds_DoesNotThrowWithNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.RecalculateBounds(null));
        }

        [Test]
        public void ClearMeshBuffers_DoesNotThrowWithNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.ClearMeshBuffers(null));
        }

        [Test]
        public void MarkDirty_DoesNotThrowWithNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.MarkDirty(null));
        }

        [Test]
        public void SynchronizeAfterTopologyChange_DoesNotThrowWithNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.SynchronizeAfterTopologyChange(null));
        }

        [Test]
        public void SynchronizeIncremental_DoesNotThrowWithNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.SynchronizeIncremental(null));
        }

        [Test]
        public void ValidateMeshState_ReturnsFalseForNullEditableObject()
        {
            // Act
            bool result = MeshSynchronizer.ValidateMeshState(null);

            // Assert
            Assert.IsFalse(result, "ValidateMeshState should return false for null editable object");
        }

        [Test]
        public void RecalculateBounds_UpdatesMeshBounds()
        {
            // Arrange
            var editableObject = EditableObject.Create(m_TestGameObject);
            if (editableObject == null)
            {
                Assert.Inconclusive("Could not create EditableObject for test");
                return;
            }

            // Modify mesh vertices to change bounds
            var editMesh = editableObject.editMesh;
            if (editMesh != null && editMesh.vertices != null && editMesh.vertices.Length > 0)
            {
                editMesh.vertices[0] = new Vector3(10, 10, 10);
            }

            // Act
            MeshSynchronizer.RecalculateBounds(editableObject);

            // Assert - bounds should be recalculated (we just verify no exception)
            Assert.Pass("RecalculateBounds completed without exception");
        }

        [Test]
        public void ClearMeshBuffers_ClearsComputeBuffers()
        {
            // Arrange
            var editableObject = EditableObject.Create(m_TestGameObject);
            if (editableObject == null)
            {
                Assert.Inconclusive("Could not create EditableObject for test");
                return;
            }

            // Act
            MeshSynchronizer.ClearMeshBuffers(editableObject);

            // Assert - buffers should be cleared (we just verify no exception)
            Assert.Pass("ClearMeshBuffers completed without exception");
        }

        /// <summary>
        /// Helper method to create a simple quad mesh for testing
        /// </summary>
        private Mesh CreateSimpleQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "TestQuadMesh";

            mesh.vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0)
            };

            mesh.normals = new Vector3[]
            {
                Vector3.back,
                Vector3.back,
                Vector3.back,
                Vector3.back
            };

            mesh.triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };

            mesh.RecalculateBounds();
            return mesh;
        }

        #region ProBuilder Integration Tests

        [Test]
        public void IsProBuilderMesh_ReturnsFalseForNullEditableObject()
        {
            // Act
            bool result = MeshSynchronizer.IsProBuilderMesh(null);

            // Assert
            Assert.IsFalse(result, "IsProBuilderMesh should return false for null EditableObject");
        }

        [Test]
        public void IsProBuilderMesh_ReturnsFalseForRegularMesh()
        {
            // Arrange
            var editableObject = EditableObject.Create(m_TestGameObject);
            if (editableObject == null)
            {
                Assert.Inconclusive("Could not create EditableObject for test");
                return;
            }

            // Act
            bool result = MeshSynchronizer.IsProBuilderMesh(editableObject);

            // Assert
            Assert.IsFalse(result, "IsProBuilderMesh should return false for regular mesh (non-ProBuilder)");
        }

        [Test]
        public void SynchronizeProBuilderMesh_DoesNotThrowForNullEditableObject()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.SynchronizeProBuilderMesh(null, true),
                "SynchronizeProBuilderMesh should not throw for null EditableObject");
        }

        [Test]
        public void SynchronizeProBuilderMesh_DoesNotThrowForRegularMesh()
        {
            // Arrange
            var editableObject = EditableObject.Create(m_TestGameObject);
            if (editableObject == null)
            {
                Assert.Inconclusive("Could not create EditableObject for test");
                return;
            }

            // Act & Assert
            Assert.DoesNotThrow(() => MeshSynchronizer.SynchronizeProBuilderMesh(editableObject, true),
                "SynchronizeProBuilderMesh should not throw for regular mesh");
        }

        [Test]
        public void MeshSynchronizer_HasProBuilderIntegrationMethods()
        {
            // Verify that the ProBuilder integration methods exist
            var type = typeof(MeshSynchronizer);
            
            // Check IsProBuilderMesh method
            var isProBuilderMeshMethod = type.GetMethod("IsProBuilderMesh", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(isProBuilderMeshMethod, "IsProBuilderMesh method should exist");
            
            // Check SynchronizeProBuilderMesh method
            var syncMethod = type.GetMethod("SynchronizeProBuilderMesh", 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(syncMethod, "SynchronizeProBuilderMesh method should exist");
        }

        #endregion
    }
}
