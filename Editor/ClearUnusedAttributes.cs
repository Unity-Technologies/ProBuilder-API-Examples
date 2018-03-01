// Uncomment this line to enable this script.
// #define PROBUILDER_API_EXAMPLE

using UnityEngine;
using UnityEditor;
using System.Collections;
using ProBuilder.Core;
using ProBuilder.EditorCore;

namespace ProBuilder.EditorExamples
{
	/// <summary>
	/// This script demonstrates one use case for the pb_EditorUtility.onMeshCompiled delegate.
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
			pb_EditorApi.AddOnMeshCompiledListener(OnMeshCompiled);
		}

		~ClearUnusedAttributes()
		{
			pb_EditorApi.RemoveOnMeshCompiledListener(OnMeshCompiled);
		}

		/// <summary>
		/// When a ProBuilder object is compiled to UnityEngine.Mesh this is called.
		/// </summary>
		/// <param name="pb"></param>
		/// <param name="mesh"></param>
		static void OnMeshCompiled(pb_Object pb, Mesh mesh)
		{
#if PROBUILDER_API_EXAMPLE
			mesh.uv = null;
			mesh.colors32 = null;
			mesh.tangents = null;
#endif

			// Print out the mesh attributes in a neatly formatted string.
			// Debug.Log( pb_MeshUtility.Print(mesh) );
		}
	}
}
