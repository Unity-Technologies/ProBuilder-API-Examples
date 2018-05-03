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
			mesh = ShapeGenerator.CubeGenerator(Vector3.one);

			// Cycle through each unique vertex in the cube (8 total), and assign a color
			// to the index in the sharedIndices array.
			int sharedIndexCount = mesh.sharedIndexes.Count;
			ReadOnlyCollection<IntArray> sharedIndexes = mesh.sharedIndexes;
			Color[] vertexColors = new Color[sharedIndexCount];
			for (int i = 0; i < sharedIndexCount; i++)
				vertexColors[i] = Color.HSVToRGB((i / (float)sharedIndexCount) * 360f, 1f, 1f);

			// Now go through each face (vertex colors are stored the pb_Face class) and
			// assign the pre-calculated index color to each index in the triangles array.
			Color[] colors = mesh.colors.ToArray();

			for (var curSharedIndex = 0; curSharedIndex < sharedIndexCount; curSharedIndex++)
			{
				foreach (var curIndex in sharedIndexes[curSharedIndex])
				{
					colors[curIndex] = vertexColors[curSharedIndex];
				}
			}

			mesh.SetColors(colors);

			// In order for these changes to take effect, you must refresh the mesh
			// object.
			mesh.Refresh();
		}
	}
}
#endif
