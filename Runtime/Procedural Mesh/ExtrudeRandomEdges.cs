#if UNITY_EDITOR || UNITY_STANDALONE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;

namespace ProBuilder.Examples
{
	/// <summary>
	/// Do a snake-like thing with a quad and some extrudes.
	/// </summary>
	public class ExtrudeRandomEdges : MonoBehaviour
	{
		ProBuilderMesh pb;
		Face lastExtrudedFace = null;
		public float distance = 1f;
		static int[] s_ExtrudeEdge = new int[2];

		// Build a starting point (in this case, a quad)
		void Start()
		{
			pb = ShapeGenerator.PlaneGenerator(1, 1, 0, 0, Axis.Up);
			foreach (var f in pb.faces) f.material = BuiltinMaterials.DefaultMaterial;
			lastExtrudedFace = pb.faces[0];
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
			var sourceFace = lastExtrudedFace;

			// fetch a random perimeter edge connected to the last face extruded
			List<WingedEdge> wings = WingedEdge.GetWingedEdges(pb);
			IEnumerable<WingedEdge> sourceWings = wings.Where(x => x.face == sourceFace);
			List<Edge> nonManifoldEdges = sourceWings.Where(x => x.opposite == null).Select(y => y.edge.local).ToList();
			int rand = (int) Random.Range(0, nonManifoldEdges.Count);
			Edge sourceEdge = nonManifoldEdges[rand];

			// get the direction this edge should extrude in
			Vector3 dir = ((pb.positions[sourceEdge.a] + pb.positions[sourceEdge.b]) * .5f) -
			              sourceFace.distinctIndexes.Average(x => pb.positions[x]);
			dir.Normalize();

			Edge[] extrudedEdges = pb.Extrude(new Edge[] { sourceEdge }, 0f, false, true);

			// get the last extruded face
			lastExtrudedFace = pb.faces.Last();

			// translate the vertices
			s_ExtrudeEdge[0] = extrudedEdges[0].a;
			s_ExtrudeEdge[1] = extrudedEdges[0].b;
			
			pb.TranslateVertexes(s_ExtrudeEdge, dir * distance);

			// rebuild mesh with new geometry added by extrude
			pb.ToMesh();

			// rebuild mesh normals, textures, collisions, etc
			pb.Refresh();
		}
	}
}
#endif
