#if UNITY_EDITOR || UNITY_STANDALONE

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace ProBuilder.Examples
{
	/// <summary>
	/// Do a snake-like thing with a quad and some extrudes.
	/// </summary>
	class ExtrudeRandomEdges : MonoBehaviour
	{
		ProBuilderMesh m_Mesh;
		Face m_LastExtrudedFace = null;
		public float distance = 1f;

		/// <summary>
		/// Build a starting point (in this case, a quad)
		/// </summary>
		void Start()
		{
			m_Mesh = ShapeGenerator.GeneratePlane(PivotLocation.Center, 1, 1, 0, 0, Axis.Up);
			m_LastExtrudedFace = m_Mesh.faces[0];
		}

		void OnGUI()
		{
			if (GUILayout.Button("Extrude Random Edge"))
			{
				ExtrudeEdge();
			}
		}

		void ExtrudeEdge()
		{
			Face sourceFace = m_LastExtrudedFace;

			// fetch a random perimeter edge connected to the last face extruded
			List<WingedEdge> wings = WingedEdge.GetWingedEdges(m_Mesh);
			IEnumerable<WingedEdge> sourceWings = wings.Where(x => x.face == sourceFace);
			List<Edge> nonManifoldEdges = sourceWings.Where(x => x.opposite == null).Select(y => y.edge.local).ToList();
			int rand = (int) Random.Range(0, nonManifoldEdges.Count);
			Edge sourceEdge = nonManifoldEdges[rand];

			// get the direction this edge should extrude in
			Vector3 dir = ((m_Mesh.positions[sourceEdge.a] + m_Mesh.positions[sourceEdge.b]) * .5f) -
			              sourceFace.distinctIndexes.Average(x => m_Mesh.positions[x]);
			dir.Normalize();

			// this will be populated with the extruded edge
			Edge[] extrudedEdges;

			// perform extrusion
			extrudedEdges = m_Mesh.Extrude(new Edge[] {sourceEdge}, 0f, false, true);

			// get the last extruded face
			m_LastExtrudedFace = m_Mesh.faces.Last();

			// translate the vertices
			m_Mesh.TranslateVertices(extrudedEdges, dir * distance);

			// rebuild mesh with new geometry added by extrude
			m_Mesh.ToMesh();

			// rebuild mesh normals, textures, collisions, etc
			m_Mesh.Refresh();
		}
	}
}
#endif
