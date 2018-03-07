using System.Collections;
using System.Collections.Generic;
using ProBuilder.Core;
using UnityEngine;

public class SetFaceMaterial : MonoBehaviour
{
	public Material[] materials;
	int m_MaterialIndex = 0;
	pb_Object selectedObject;
	pb_Face selectedFace;

	void OnEnable()
	{
		// On entering scene, rebuild all probuilder meshes to their uncompressed state. The reason is that the raycasting
		// function depends on the UnityEngine.Mesh representation matching the ProBuilder Mesh state.
		foreach (var pb in FindObjectsOfType<pb_Object>())
		{
			pb.ToMesh();
			pb.Refresh();
		}
	}

	void Update()
	{
		if (Input.GetMouseButtonUp(0) && FaceRaycast(Input.mousePosition, out selectedObject, out selectedFace))
		{
			// Materials are set per-face, and pb_Object handles merging alike faces to a single submesh.
			selectedFace.material = materials[(m_MaterialIndex++) % materials.Length];

			// Rebuild the mesh submeshes and vertices
			selectedObject.ToMesh();

			// Rebuildd UVs, normals, tangents, collisions.
			selectedObject.Refresh();
		}
	}

	bool FaceRaycast(Vector2 mouse, out pb_Object pb, out pb_Face face)
	{
		var ray = Camera.main.ScreenPointToRay(mouse);
		RaycastHit rayHit;

		if( Physics.Raycast(ray.origin, ray.direction, out rayHit))
		{
			pb = rayHit.transform.gameObject.GetComponent<pb_Object>();

			if (pb == null)
			{
				face = null;
				return false;
			}

			Mesh m = pb.GetComponent<MeshFilter>().sharedMesh;

			int[] tri = new int[3] {
				m.triangles[rayHit.triangleIndex * 3 + 0],
				m.triangles[rayHit.triangleIndex * 3 + 1],
				m.triangles[rayHit.triangleIndex * 3 + 2]
			};

			return pb.FaceWithTriangle(tri, out face);
		}

		pb = null;
		face = null;

		return false;
	}
}
