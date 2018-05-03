using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.ProBuilder;

namespace ProBuilder.Examples
{
	/**
	 *	Move a sphere around the surface of a ProBuilder mesh, changing the
	 *	vertex color of the nearest face.
	 *
	 *	Scene setup:  Create a Unity Sphere primitive in a new scene, then attach
	 *	this script to the sphere.  Press 'Play'
	 */
	class HighlightNearestFace : MonoBehaviour
	{
		// The distance covered by the plane.
		public float travel = 50f;

		// The speed at which the sphere will move.
		public float speed = .2f;

		// ProBuilder mesh component
		ProBuilderMesh m_Mesh;

		// The nearest face to this sphere.
		Face m_NearestFace = null;

		void Start()
		{
			// Generate a 50x50 plane with 25 subdivisions, facing up, with no smoothing applied.
			m_Mesh = ShapeGenerator.PlaneGenerator(travel, travel, 25, 25, Axis.Up);

			foreach (Face face in m_Mesh.faces)
				face.material = BuiltinMaterials.DefaultMaterial;

			m_Mesh.transform.position = new Vector3(travel * .5f, 0f, travel * .5f);

			// Rebuild the mesh (apply ProBuilderMesh data to UnityEngine.Mesh)
			m_Mesh.ToMesh();

			// Rebuild UVs, Colors, Collisions, Normals, and Tangents
			m_Mesh.Refresh();

			// Orient the camera in a good position
			Camera cam = Camera.main;
			cam.transform.position = new Vector3(25f, 40f, 0f);
			cam.transform.localRotation = Quaternion.Euler(new Vector3(65f, 0f, 0f));
		}

		void Update()
		{
			float time = Time.time * speed;

			Vector3 position = new Vector3(
				Mathf.PerlinNoise(time, time) * travel,
				2,
				Mathf.PerlinNoise(time + 1f, time + 1f) * travel
			);

			transform.position = position;

			if (m_Mesh == null)
			{
				Debug.LogWarning("Missing the ProBuilder Mesh target!");
				return;
			}

			// instead of testing distance by converting each face's center to world space,
			// convert the world space of this object to the pb-Object local transform.
			Vector3 pbRelativePosition = m_Mesh.transform.InverseTransformPoint(transform.position);

			// reset the last colored face to white
			if (m_NearestFace != null)
				m_Mesh.SetFaceColor(m_NearestFace, Color.white);

			// iterate each face in the ProBuilderMesh looking for the one nearest to this object.
			int faceCount = m_Mesh.faces.Count;
			float smallestDistance = Mathf.Infinity;
			m_NearestFace = m_Mesh.faces[0];

			for (int i = 0; i < faceCount; i++)
			{
				float distance = Vector3.Distance(pbRelativePosition, FaceCenter(m_Mesh, m_Mesh.faces[i]));

				if (distance < smallestDistance)
				{
					smallestDistance = distance;
					m_NearestFace = m_Mesh.faces[i];
				}
			}

			// Set a single face's vertex colors.  If you're updating more than one face, consider using
			// the ProBuilderMesh.SetColors(Color[] colors); function instead.
			m_Mesh.SetFaceColor(m_NearestFace, Color.blue);

			// Apply the stored vertex color array to the Unity mesh.
			m_Mesh.Refresh(RefreshMask.Colors);
		}

		/**
		 *	Returns the average of each vertex position in a face.
		 *	In local space.
		 */
		Vector3 FaceCenter(ProBuilderMesh pb, Face face)
		{
			var vertices = pb.positions;

			Vector3 average = Vector3.zero;

			// face holds triangle data.  distinctIndices is a
			// cached collection of the distinct indices that
			// make up the triangles. Ex:
			// tris = {0, 1, 2, 2, 3, 0}
			// distinct indices = {0, 1, 2, 3}
			foreach (int index in face.distinctIndexes)
			{
				average.x += vertices[index].x;
				average.y += vertices[index].y;
				average.z += vertices[index].z;
			}

			var len = (float) face.distinctIndexes.Count;

			average.x /= len;
			average.y /= len;
			average.z /= len;

			return average;
		}
	}
}