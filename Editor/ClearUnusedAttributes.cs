using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using MeshUtility = UnityEditor.MeshUtility;

namespace ProBuilder.EditorExamples
{
	/// <summary>
	/// This script demonstrates one use case for the pb_EditorUtility.onMeshOptimized delegate.
	/// Whenever ProBuilder compiles a mesh it removes the colors, tangents, and uv attributes.
	/// </summary>
	[InitializeOnLoad]
	public class ClearUnusedAttributes : Editor
	{
		/// <summary>
		/// Static constructor is called and subscribes to the OnMeshCompiled delegate.
		/// </summary>
		static ClearUnusedAttributes()
		{
			EditorMeshUtility.onMeshOptimized += OnMeshCompiled;
		}

		~ClearUnusedAttributes()
		{
			EditorMeshUtility.onMeshOptimized -= OnMeshCompiled;
		}

		/// <summary>
		/// When a ProBuilder object is compiled to UnityEngine.Mesh this is called.
		/// </summary>
		/// <param name="pb"></param>
		/// <param name="mesh"></param>
		static void OnMeshCompiled(ProBuilderMesh pb, Mesh mesh)
		{
			mesh.uv = null;
			mesh.colors32 = null;
			mesh.tangents = null;
			// Print out the mesh attributes in a neatly formatted string.
			// Debug.Log( pb_MeshUtility.Print(mesh) );
		}
	}
}
