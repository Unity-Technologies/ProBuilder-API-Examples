// This script demonstrates how to create a new action that can be accessed from the ProBuilder toolbar.
// A new menu item is registered under "Geometry" actions called "Make Double-Sided".
// To enable, remove the #if PROBUILDER_API_EXAMPLE and #endif directives.

using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;

namespace ProBuilder.ExampleActions
{
	/// <summary>
	/// This is the actual action that will be executed.
	/// </summary>
	[ProBuilderMenuAction]
	public class MakeFacesDoubleSided : MenuAction
	{
		public override ToolbarGroup group
		{
			get { return ToolbarGroup.Geometry; }
		}

		public override Texture2D icon
		{
			get { return null; }
		}

		public override TooltipContent tooltip
		{
			get { return _tooltip; }
		}

		/// <summary>
		/// What to show in the hover tooltip window.
		/// TooltipContent is similar to GUIContent, with the exception that it also includes an optional params[]
		/// char list in the constructor to define shortcut keys (ex, CMD_CONTROL, K).
		/// </summary>
		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"Set Double-Sided",
			"Adds another face to the back of the selected faces."
		);

		/// <summary>
		/// Determines if the action should be enabled or grayed out.
		/// </summary>
		/// <returns></returns>
		public override bool IsEnabled()
		{
			return MeshSelection.Top().Any(x => x.selectedFaceCount > 0);
		}

		/// <summary>
		/// Determines if the action should be loaded in the menu (ex, face actions shouldn't be shown when in vertex editing mode).
		/// </summary>
		/// <returns></returns>
		public override bool IsHidden()
		{
			return ProBuilderEditor.instance == null
				|| ProBuilderEditor.instance.editLevel != EditLevel.Geometry
				|| ProBuilderEditor.instance.selectionMode != SelectMode.Face;
		}

		/// <summary>
		/// Perform the menu action.
		/// </summary>
		/// <returns>An ActionResult indicating the success/failure of action.</returns>
		public override ActionResult DoAction()
		{
			Undo.RecordObjects(MeshSelection.Top().ToArray(), "Make Double-Sided Faces");

			foreach (ProBuilderMesh pb in MeshSelection.Top())
			{
				AppendElements.DuplicateAndFlip(pb, pb.GetSelectedFaces());
				pb.ToMesh();
				pb.Refresh();
				pb.Optimize();
			}

			// Rebuild the ProBuilderEditor caches
			ProBuilderEditor.Refresh();

			return new ActionResult(ActionResult.Status.Success, "Make Faces Double-Sided");
		}
	}
}
