using System.Collections;
using System.Collections.Generic;
using ProBuilder.Core;
using ProBuilder.MeshOperations;
using UnityEngine;

namespace ProBuilder.Examples
{

	public class MakePrimitiveEditable : MonoBehaviour
	{

		public MeshFilter nonProBuilderMesh;

		void Start()
		{
			// Get a reference to the GameObject
			var go = nonProBuilderMesh.gameObject;

			// Add a new uninitialized pb_Object
			var pb = go.AddComponent<pb_Object>();

			// Create a new ProBuilder MeshImporter
			var importer = new pb_MeshImporter(pb);

			// Import from a GameObject - in this case we're loading and assigning to the same GameObject, but you may
			// load and apply to different Objects as well.
			importer.Import(go);

			// Since we're loading and setting from the same object, it is necessary to create a new mesh to avoid
			// overwriting the mesh that is being read from.
			nonProBuilderMesh.sharedMesh = new Mesh();

			// Do something with the pb_Object. Here we're extruding every face on the object by .25.
			pb.Extrude(pb.faces, ExtrudeMethod.IndividualFaces, .25f);

			// Apply the imported geometry to the pb_Object
			pb.ToMesh();

			// Rebuild UVs, Collisions, Tangents, etc.
			pb.Refresh();
		}
	}
}
