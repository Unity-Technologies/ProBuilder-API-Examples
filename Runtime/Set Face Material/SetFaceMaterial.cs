using System.Collections;
using System.Collections.Generic;
using UnityEngine.ProBuilder;
using UnityEngine;

public class SetFaceMaterial : MonoBehaviour
{
	public Material[] materials;
	int m_MaterialIndex = 0;
	ProBuilderMesh m_SelectedObject;
	Face m_SelectedFace;

	void OnEnable()
	{
		// On entering scene, rebuild all probuilder meshes to their uncompressed state. The reason is that the raycasting
		// function depends on the UnityEngine.Mesh representation matching the ProBuilder Mesh state.
		foreach (var pb in FindObjectsOfType<ProBuilderMesh>())
		{
			pb.ToMesh();
			pb.Refresh();
		}
	}

	void Update()
	{
		if (Input.GetMouseButtonUp(0) && FaceRaycast(Input.mousePosition))
		{
			// Materials are set per-face, and ProBuilderMesh handles merging alike faces to a single submesh.
			m_SelectedFace.material = materials[(m_MaterialIndex++) % materials.Length];

			// Rebuild the mesh submeshes and vertices
			m_SelectedObject.ToMesh();

			// Rebuildd UVs, normals, tangents, collisions.
			m_SelectedObject.Refresh();
		}
	}

	bool FaceRaycast(Vector2 mouse)
	{
		m_SelectedObject = null;
		m_SelectedFace = null;

		var ray = Camera.main.ScreenPointToRay(mouse);
		RaycastHit rayHit;

		if( Physics.Raycast(ray.origin, ray.direction, out rayHit))
		{
			m_SelectedObject = rayHit.transform.gameObject.GetComponent<ProBuilderMesh>();

			if (m_SelectedObject == null)
				return false;

			Mesh m = m_SelectedObject.GetComponent<MeshFilter>().sharedMesh;

			int[] tri = new int[3] {
				m.triangles[rayHit.triangleIndex * 3 + 0],
				m.triangles[rayHit.triangleIndex * 3 + 1],
				m.triangles[rayHit.triangleIndex * 3 + 2]
			};

			m_SelectedFace = m_SelectedObject.FaceWithTriangle(tri);
			return m_SelectedFace != null;
		}

		return false;
	}
}
