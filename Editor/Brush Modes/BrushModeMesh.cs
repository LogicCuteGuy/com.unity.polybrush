using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Polybrush;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// Base class for brush modes that modify the mesh.
    /// </summary>
    [System.Serializable]
	internal abstract class BrushModeMesh : BrushMode
	{
		// All meshes that have ever been modified, ever.  Kept around to refresh mesh vertices
		// on Undo/Redo since Unity doesn't.
		private HashSet<PolyMesh> modifiedMeshes = new HashSet<PolyMesh>();

        private HashSet<GameObject> modifiedPbMeshes = new HashSet<GameObject>();

        /// <summary>
        /// Gets the direction sign based on modifier keys.
        /// Returns -1 when Shift is held (negative/inverse operation), +1 otherwise (positive/normal operation).
        /// Use this for operations that should be reversible with Shift key.
        /// </summary>
        /// <param name="settings">Current brush settings containing modifier key state</param>
        /// <returns>+1 for normal operation, -1 for inverse operation when Shift is held</returns>
        protected float GetDirectionSign(BrushSettings settings)
        {
            return settings.isUserHoldingShift ? -1f : 1f;
        }

        /// <summary>
        /// Gets the direction sign based on Control key.
        /// Returns -1 when Control is held (negative/inverse operation), +1 otherwise (positive/normal operation).
        /// Use this for operations that should be reversible with Control key.
        /// </summary>
        /// <param name="settings">Current brush settings containing modifier key state</param>
        /// <returns>+1 for normal operation, -1 for inverse operation when Control is held</returns>
        protected float GetControlDirectionSign(BrushSettings settings)
        {
            return settings.isUserHoldingControl ? -1f : 1f;
        }

        /// <summary>
        /// Gets the direction sign based on either Shift or Control key.
        /// Returns -1 when either modifier is held (negative/inverse operation), +1 otherwise.
        /// Use this for operations that should be reversible with either modifier key.
        /// </summary>
        /// <param name="settings">Current brush settings containing modifier key state</param>
        /// <returns>+1 for normal operation, -1 for inverse operation when Shift or Control is held</returns>
        protected float GetAnyModifierDirectionSign(BrushSettings settings)
        {
            return (settings.isUserHoldingShift || settings.isUserHoldingControl) ? -1f : 1f;
        }

        /// <summary>
        /// Checks if the operation should be inverted based on Shift key.
        /// </summary>
        /// <param name="settings">Current brush settings containing modifier key state</param>
        /// <returns>True if Shift is held and operation should be inverted</returns>
        protected bool IsInvertedByShift(BrushSettings settings)
        {
            return settings.isUserHoldingShift;
        }

        /// <summary>
        /// Checks if the operation should be inverted based on Control key.
        /// </summary>
        /// <param name="settings">Current brush settings containing modifier key state</param>
        /// <returns>True if Control is held and operation should be inverted</returns>
        protected bool IsInvertedByControl(BrushSettings settings)
        {
            return settings.isUserHoldingControl;
        }

        /// <summary>
        /// Checks if the operation should be inverted based on either Shift or Control key.
        /// </summary>
        /// <param name="settings">Current brush settings containing modifier key state</param>
        /// <returns>True if either Shift or Control is held and operation should be inverted</returns>
        protected bool IsInvertedByAnyModifier(BrushSettings settings)
        {
            return settings.isUserHoldingShift || settings.isUserHoldingControl;
        }

        internal override void OnBrushBeginApply(BrushTarget brushTarget, BrushSettings brushSettings)
		{
            base.OnBrushBeginApply(brushTarget, brushSettings);
		}

		internal override void OnBrushApply(BrushTarget brushTarget, BrushSettings brushSettings)
		{
			// false means no ToMesh or Refresh, true does.  Optional addl bool runs pb_Object.Optimize()
			brushTarget.editableObject.Apply(true);

            if (ProBuilderBridge.ProBuilderExists() && brushTarget.editableObject.isProBuilderObject)
                ProBuilderBridge.Refresh(brushTarget.gameObject);

            UpdateTempComponent(brushTarget, brushSettings);
		}

		internal override void RegisterUndo(BrushTarget brushTarget)
		{
            if (ProBuilderBridge.IsValidProBuilderMesh(brushTarget.gameObject))
            {
                UnityEngine.Object pbMesh = ProBuilderBridge.GetProBuilderComponent(brushTarget.gameObject);
                if (pbMesh != null)
                {
                    Undo.RegisterCompleteObjectUndo(pbMesh, UndoMessage);
                    modifiedPbMeshes.Add(brushTarget.gameObject);
                }
                else
                {
                    Undo.RegisterCompleteObjectUndo(brushTarget.editableObject.polybrushMesh, UndoMessage);
                    modifiedMeshes.Add(brushTarget.editableObject.polybrushMesh.polyMesh);
                }
            }
            else
            {
                Undo.RegisterCompleteObjectUndo(brushTarget.editableObject.polybrushMesh, UndoMessage);
                modifiedMeshes.Add(brushTarget.editableObject.polybrushMesh.polyMesh);
            }

            brushTarget.editableObject.isDirty = true;
		}

		internal override void UndoRedoPerformed(List<GameObject> modified)
		{
			modifiedMeshes = new HashSet<PolyMesh>(modifiedMeshes.Where(x => x != null));

            if (ProBuilderBridge.ProBuilderExists())
            {
                // delete & undo causes cases where object is not null but the reference to it's pb_Object is
                HashSet<GameObject> remove = new HashSet<GameObject>();

                foreach (GameObject pb in modifiedPbMeshes)
                {
                    try
                    {
                        ProBuilderBridge.ToMesh(pb);
                        ProBuilderBridge.Refresh(pb);
                        ProBuilderBridge.Optimize(pb);
                    }
                    catch
                    {
                        remove.Add(pb);
                    }

                }

                if (remove.Count() > 0)
                    modifiedPbMeshes.SymmetricExceptWith(remove);
            }

            foreach (PolyMesh m in modifiedMeshes)
			{
                m.UpdateMeshFromData();
			}

			base.UndoRedoPerformed(modified);
		}
	}
}
