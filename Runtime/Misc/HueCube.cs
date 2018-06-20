#if UNITY_EDITOR || UNITY_STANDALONE
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ProBuilder.Examples
{
	/// <summary>
	/// Creates a cube on start and colors it's vertices programmitically.
	/// </summary>
	public class HueCube : MonoBehaviour
	{
		ProBuilderMesh mesh;

		void Start()
		{
			// Create a new ProBuilder cube to work with.
			mesh = ShapeGenerator.GenerateCube(Vector3.one);

			// Cycle through each unique vertex in the cube (8 total), and assign a color
			// to the index in the sharedIndices array.
			var sharedVertexes = mesh.sharedVertexes;
			var sharedVertexCount = sharedVertexes.Count();
			Color[] vertexColors = new Color[sharedVertexCount];

			for (int i = 0; i < sharedVertexCount; i++)
				vertexColors[i] = Color.HSVToRGB((i / (float)sharedVertexCount) * 360f, 1f, 1f);

			// Now go through each face (vertex colors are stored the pb_Face class) and
			// assign the pre-calculated index color to each index in the triangles array.
			Color[] colors = mesh.colors.ToArray();
			int index = 0;

			foreach(var sharedVertex in sharedVertexes)
			{
				foreach(var vertex in sharedVertex)
				{
					colors[vertex] = vertexColors[index];
				}

				index++;
			}

			mesh.colors = colors;

			// In order for these changes to take effect, you must refresh the mesh
			// object.
			mesh.Refresh();
		}
	}
}
#endif
