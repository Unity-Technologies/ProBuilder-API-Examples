/**
 *	This script demonstrates how to create a new action that can be accessed from the
 *	ProBuilder toolbar.
 *
 *	A new menu item is registered under "Geometry" actions called "Gen. Shadows".
 */

using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEditor.ProBuilder;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

// When creating your own actions please use your own namespace.
namespace ProBuilder.ExampleActions
{
	/// <summary>
	/// This is the action that will be executed. It's lifecycle is managed by pb_EditorToolbar, and registered in a
	/// static constructor.
	/// </summary>
	[ProBuilderMenuAction]
	class CreateShadowObject : MenuAction
	{
		public override ToolbarGroup group
		{
			get { return ToolbarGroup.Object; }
		}

		public override Texture2D icon
		{
			get { return null; }
		}

		public override TooltipContent tooltip
		{
			get { return s_Tooltip; }
		}

		GUIContent m_VolumeSizeContent = new GUIContent("Volume Size", "How far the shadow volume extends from the base mesh.  To visualize, imagine the width of walls.\n\nYou can also select the child ShadowVolume object and turn the Shadow Casting Mode to \"One\" or \"Two\" sided to see the resulting mesh.");

		bool showPreview
		{
			get { return EditorPrefs.GetBool("pb_shadowVolumePreview", true); }
			set { EditorPrefs.SetBool("pb_shadowVolumePreview", value); }
		}

		// What to show in the hover tooltip window.  TooltipContent is similar to GUIContent, with the exception
		// that it also includes an optional params[] char list in the constructor to define shortcut keys
		// (ex, CMD_CONTROL, K).
		static readonly TooltipContent s_Tooltip = new TooltipContent
		(
			"Gen Shadow Obj",
			"Creates a new ProBuilder mesh child with inverted normals that only exists to cast shadows.  Use to create lit interior scenes with shadows from directional lights.\n\nNote that this exists largely as a workaround for real-time shadow light leaks.  Baked shadows do not require this workaround.",
			"" // Some combination of build settings can cause the compiler to not respection optional params in the TooltipContent c'tor?
		);

		// Determines if the action should be enabled or grayed out.
		public override bool IsEnabled()
		{
			return MeshSelection.Top().Any();
		}

		/// <summary>
		/// Determines if the action should be loaded in the menu (ex, face actions shouldn't be shown when in vertex editing mode).
		/// </summary>
		/// <returns></returns>
		public override bool IsHidden()
		{
			return false;
		}

		protected override MenuActionState OptionsMenuState()
		{
			return MenuActionState.VisibleAndEnabled;
		}

		protected override void OnSettingsEnable()
		{
			if (showPreview)
				DoAction();
		}

		protected override void OnSettingsGUI()
		{
			GUILayout.Label("Create Shadow Volume Options", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();

			EditorGUI.BeginChangeCheck();
			float volumeSize = EditorPrefs.GetFloat("pb_CreateShadowObject_volumeSize", .07f);
			volumeSize = EditorGUILayout.Slider(m_VolumeSizeContent, volumeSize, 0.001f, 1f);
			if (EditorGUI.EndChangeCheck())
				EditorPrefs.SetFloat("pb_CreateShadowObject_volumeSize", volumeSize);

#if !UNITY_4_6 && !UNITY_4_7
			EditorGUI.BeginChangeCheck();
			ShadowCastingMode shadowMode = (ShadowCastingMode)EditorPrefs.GetInt("pb_CreateShadowObject_shadowMode", (int)ShadowCastingMode.ShadowsOnly);
			shadowMode = (ShadowCastingMode)EditorGUILayout.EnumPopup("Shadow Casting Mode", shadowMode);
			if (EditorGUI.EndChangeCheck())
				EditorPrefs.SetInt("pb_CreateShadowObject_shadowMode", (int)shadowMode);
#endif

			EditorGUI.BeginChangeCheck();
			ExtrudeMethod extrudeMethod = (ExtrudeMethod)EditorPrefs.GetInt("pb_CreateShadowObject_extrudeMethod", (int)ExtrudeMethod.FaceNormal);
			extrudeMethod = (ExtrudeMethod)EditorGUILayout.EnumPopup("Extrude Method", extrudeMethod);
			if (EditorGUI.EndChangeCheck())
				EditorPrefs.SetInt("pb_CreateShadowObject_extrudeMethod", (int)extrudeMethod);

			if (EditorGUI.EndChangeCheck())
				DoAction();

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Create Shadow Volume"))
			{
				DoAction();
				SceneView.RepaintAll();
			}
		}

		/// <summary>
		/// Perform the action.
		/// </summary>
		/// <returns>Return an ActionResult indicating the success/failure of action.</returns>
		public override ActionResult DoAction()
		{
			ShadowCastingMode shadowMode = (ShadowCastingMode)EditorPrefs.GetInt("pb_CreateShadowObject_shadowMode", (int)ShadowCastingMode.ShadowsOnly);
			float extrudeDistance = EditorPrefs.GetFloat("pb_CreateShadowObject_volumeSize", .08f);
			ExtrudeMethod extrudeMethod = (ExtrudeMethod)EditorPrefs.GetInt("pb_CreateShadowObject_extrudeMethod", (int)ExtrudeMethod.FaceNormal);

			foreach (ProBuilderMesh pb in MeshSelection.Top())
			{
				ProBuilderMesh shadow = GetShadowObject(pb);

				if (shadow == null)
					continue;

				foreach (Face f in shadow.faces)
				{
					f.Reverse();
					f.manualUV = true;
				}

				shadow.Extrude(shadow.faces, extrudeMethod, extrudeDistance);
				shadow.ToMesh();
				shadow.Refresh();
				shadow.Optimize();

#if !UNITY_4_6 && !UNITY_4_7
				MeshRenderer mr = shadow.gameObject.GetComponent<MeshRenderer>();
				mr.shadowCastingMode = shadowMode;
				if (shadowMode == ShadowCastingMode.ShadowsOnly)
					mr.receiveShadows = false;
#endif

				Collider collider = shadow.GetComponent<Collider>();

				while (collider != null)
				{
					Object.DestroyImmediate(collider);
					collider = shadow.GetComponent<Collider>();
				}
			}

			// This is necessary, otherwise pb_Editor will be working with caches from
			// outdated meshes and throw errors.
			ProBuilderEditor.Refresh();

			return new ActionResult(ActionResult.Status.Success, "Create Shadow Object");
		}

		ProBuilderMesh GetShadowObject(ProBuilderMesh mesh)
		{
			if (mesh == null || mesh.name.Contains("-ShadowVolume"))
				return null;

			ProBuilderMesh shadow;

			for (int i = 0; i < mesh.transform.childCount; i++)
			{
				Transform t = mesh.transform.GetChild(i);

				if (t.name.Equals(string.Format("{0}-ShadowVolume", mesh.name)))
				{
					shadow = t.GetComponent<ProBuilderMesh>();

					if (shadow != null)
					{
						Undo.RecordObject(shadow, "Update Shadow Object");

						var faceCount = mesh.faceCount;
						var meshFaces = mesh.faces as Face[] ?? mesh.faces.ToArray();
						var faces = new Face[faceCount];

						for (int nn = 0, cc = faceCount; nn < cc; nn++)
							faces[nn] = new Face(meshFaces[nn]);

						shadow.RebuildWithPositionsAndFaces(mesh.positions, faces);
						return shadow;
					}
				}
			}

			shadow = new GameObject().AddComponent<ProBuilderMesh>();
			shadow.CopyFrom(mesh);
			shadow.name = string.Format("{0}-ShadowVolume", mesh.name);
			shadow.transform.SetParent(mesh.transform, false);
			Undo.RegisterCreatedObjectUndo(shadow.gameObject, "Create Shadow Object");
			return shadow;
		}
	}
}
