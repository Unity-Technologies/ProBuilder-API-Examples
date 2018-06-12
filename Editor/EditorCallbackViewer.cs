using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using ProBuilderEditorUtility = UnityEditor.ProBuilder.EditorUtility;

namespace ProBuilder.EditorExamples
{
	class EditorCallbackViewer : EditorWindow
	{
		[MenuItem("Tools/ProBuilder/API Examples/Log Callbacks Window")]
		static void MenuInitEditorCallbackViewer()
		{
			GetWindow<EditorCallbackViewer>(true, "ProBuilder Callbacks", true).Show();
		}

		List<string> logs = new List<string>();
		Vector2 scroll = Vector2.zero;
		bool collapse = true;

		static Color logBackgroundColor
		{
			get { return EditorGUIUtility.isProSkin ? new Color(.15f, .15f, .15f, .5f) : new Color(.8f, .8f, .8f, 1f); }
		}

		static Color disabledColor
		{
			get { return EditorGUIUtility.isProSkin ? new Color(.3f, .3f, .3f, .5f) : new Color(.8f, .8f, .8f, 1f); }
		}

		void OnEnable()
		{
			// Delegate for Top/Geometry/Texture mode changes.
			ProBuilderEditor.editLevelChanged += OnEditLevelChanged;

			// Called when a new ProBuilder object is created.
			// note - this was added in ProBuilder 2.5.1
			ProBuilderEditorUtility.meshCreated += OnProBuilderObjectCreated;

			// Called when the ProBuilder selection changes (can be object or element change).
			// Also called when the geometry is modified by ProBuilder.
			ProBuilderEditor.selectionUpdated += OnSelectionUpdate;

			// Called when vertices are about to be modified.
			ProBuilderEditor.beforeMeshModification += OnVertexMovementBegin;

			// Called when vertices have been moved by ProBuilder.
			ProBuilderEditor.afterMeshModification += OnVertexMovementFinish;

			// Called when the Unity mesh is rebuilt from ProBuilder mesh data.
			EditorMeshUtility.meshOptimized += OnMeshCompiled;
		}

		void OnDisable()
		{
			ProBuilderEditor.editLevelChanged -= OnEditLevelChanged;
			ProBuilderEditorUtility.meshCreated -= OnProBuilderObjectCreated;
			ProBuilderEditor.selectionUpdated -= OnSelectionUpdate;
			ProBuilderEditor.beforeMeshModification -= OnVertexMovementBegin;
			ProBuilderEditor.afterMeshModification -= OnVertexMovementFinish;
			EditorMeshUtility.meshOptimized -= OnMeshCompiled;
		}

		void OnProBuilderObjectCreated(ProBuilderMesh pb)
		{
			AddLog("Instantiated new ProBuilder Object: " + pb.name);
		}

		void OnEditLevelChanged(int editLevel)
		{
			AddLog("Edit Level Changed: " + (EditLevel) editLevel);
		}

		void OnSelectionUpdate(ProBuilderMesh[] selection)
		{
			AddLog("Selection Updated: " + string.Format("{0} objects selected.", selection != null ? selection.Length : 0));
		}

		void OnVertexMovementBegin(ProBuilderMesh[] selection)
		{
			AddLog("Began Moving Vertices");
		}

		void OnVertexMovementFinish(ProBuilderMesh[] selection)
		{
			AddLog("Finished Moving Vertices");
		}

		void OnMeshCompiled(ProBuilderMesh pb, Mesh mesh)
		{
			AddLog(string.Format("Mesh {0} rebuilt", pb.name));
		}

		void AddLog(string summary)
		{
			logs.Add(summary);
			Repaint();
		}

		void OnGUI()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);

			GUILayout.FlexibleSpace();

			GUI.backgroundColor = collapse ? disabledColor : Color.white;
			if (GUILayout.Button("Collapse", EditorStyles.toolbarButton))
				collapse = !collapse;
			GUI.backgroundColor = Color.white;

			if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
				logs.Clear();

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Callback Log", EditorStyles.boldLabel);
			GUILayout.EndHorizontal();

			Rect r = GUILayoutUtility.GetLastRect();
			r.x = 0;
			r.y = r.y + r.height + 6;
			r.width = this.position.width;
			r.height = this.position.height;

			GUILayout.Space(4);

			scroll = GUILayout.BeginScrollView(scroll);

			int len = logs.Count;
			int min = System.Math.Max(0, len - 1024);

			for (int i = len - 1; i >= min; i--)
			{
				if (collapse &&
				    i > 0 &&
				    i < len - 1 &&
				    logs[i].Equals(logs[i - 1]) &&
				    logs[i].Equals(logs[i + 1]))
					continue;

				GUILayout.Label(string.Format("{0,3}: {1}", i, logs[i]));
			}

			GUILayout.EndScrollView();
		}
	}
}