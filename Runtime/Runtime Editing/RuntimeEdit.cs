#if UNITY_STANDALONE || UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.ProBuilder;

namespace ProBuilder.Examples
{
	/// <summary>
	/// This class allows the user to select a single face at a time and move it forwards or backwards.
	/// </summary>
	public class RuntimeEdit : MonoBehaviour
	{
		class ProBuilderSelection
		{
			public ProBuilderMesh mesh { get; set; }
			public Face face { get; set; }

			public ProBuilderSelection(ProBuilderMesh mesh, Face face)
			{
				this.mesh = mesh;
				this.face = face;
			}

			public bool HasObject()
			{
				return mesh != null;
			}

			public bool IsValid()
			{
				return mesh != null && face != null;
			}

			public bool Equals(ProBuilderSelection sel)
			{
				if (sel != null && sel.IsValid())
					return (mesh == sel.mesh && face == sel.face);
				else
					return false;
			}

			public void Destroy()
			{
				if (mesh != null)
					Object.Destroy(mesh.gameObject);
			}

			public override string ToString()
			{
				return "ProBuilderMesh: " + mesh == null
					? "Null"
					: mesh.name +
					"\nFace: " + ((face == null) ? "Null" : face.ToString());
			}
		}

		ProBuilderSelection m_CurrentSelection;
		ProBuilderSelection m_PreviousSelection;

		ProBuilderMesh m_PreviewMesh;
		[SerializeField]
		Material m_PreviewMaterial;

		Vector2 m_MousePositionInitial = Vector2.zero;
		bool m_IsDragging;
		[SerializeField]
		[Range(1f, 200f)]
		float rotateSpeed = 100f;

		void Awake()
		{
			SpawnCube();
		}

		void OnGUI()
		{
			// To reset, nuke the ProBuilderMesh and build a new one.
			if (GUI.Button(new Rect(5, Screen.height - 25, 80, 20), "Reset"))
			{
				m_CurrentSelection.Destroy();
				Destroy(m_PreviewMesh.gameObject);
				SpawnCube();
			}
		}

		/// <summary>
		/// Creates a new ProBuilder cube and sets it up with a concave MeshCollider.
		/// </summary>
		void SpawnCube()
		{
			// This creates a basic cube with ProBuilder features enabled.  See the ProBuilder.Shape enum to
			// see all possible primitive types.
			ProBuilderMesh mesh = ShapeGenerator.GenerateCube(Vector3.one);

			// The runtime component requires that a concave mesh collider be present in order for face selection
			// to work.
			mesh.gameObject.AddComponent<MeshCollider>().convex = false;

			// Now set it to the currentSelection
			m_CurrentSelection = new ProBuilderSelection(mesh, null);
		}

		void LateUpdate()
		{
			if (!m_CurrentSelection.HasObject())
				return;

			if (Input.GetMouseButtonDown(1) || (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftAlt)))
			{
				m_MousePositionInitial = Input.mousePosition;
				m_IsDragging = true;
			}

			if (m_IsDragging)
			{
				Vector2 delta = (Vector3)m_MousePositionInitial - (Vector3)Input.mousePosition;
				Vector3 dir = new Vector3(delta.y, delta.x, 0f);

				m_CurrentSelection.mesh.gameObject.transform.RotateAround(Vector3.zero, dir, rotateSpeed * Time.deltaTime);

				// If there is a currently selected face, update the preview.
				if (m_CurrentSelection.IsValid())
					RefreshSelectedFacePreview();
			}

			if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0))
			{
				m_IsDragging = false;
			}
		}

		// This listens for a click event, then checks for a positive face selection.
		// If the click has hit a ProBuilderMesh, select it.
		void Update()
		{
			if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftAlt))
			{
				if (FaceCheck(Input.mousePosition))
				{
					if (m_CurrentSelection.IsValid())
					{
						// Check if this face has been previously selected, and if so, move the face.
						// Otherwise, just accept this click as a selection.
						if (!m_CurrentSelection.Equals(m_PreviousSelection))
						{
							m_PreviousSelection = new ProBuilderSelection(m_CurrentSelection.mesh, m_CurrentSelection.face);
							RefreshSelectedFacePreview();
							return;
						}

						Vector3 localNormal = UnityEngine.ProBuilder.Math.Normal(m_CurrentSelection.mesh, m_CurrentSelection.face);

						if (Input.GetKey(KeyCode.LeftShift))
							m_CurrentSelection.mesh.TranslateVertexes(m_CurrentSelection.face.distinctIndexes, localNormal.normalized * -.5f);
						else
							m_CurrentSelection.mesh.TranslateVertexes(m_CurrentSelection.face.distinctIndexes, localNormal.normalized * .5f);

						// Refresh will update the Collision mesh volume, face UVs as applicatble, and normal information.
						m_CurrentSelection.mesh.Refresh();

						// this create the selected face preview
						RefreshSelectedFacePreview();
					}
				}
			}
		}

		bool FaceCheck(Vector3 pos)
		{
			Ray ray = Camera.main.ScreenPointToRay(pos);
			RaycastHit hit;

			if (Physics.Raycast(ray.origin, ray.direction, out hit))
			{
				ProBuilderMesh hitpb = hit.transform.gameObject.GetComponent<ProBuilderMesh>();

				if (hitpb == null)
					return false;

				Mesh m = hitpb.GetComponent<MeshFilter>().sharedMesh;

				int[] tri = new int[3]
				{
					m.triangles[hit.triangleIndex * 3 + 0],
					m.triangles[hit.triangleIndex * 3 + 1],
					m.triangles[hit.triangleIndex * 3 + 2]
				};

				m_CurrentSelection.mesh = hitpb;
				m_CurrentSelection.face = hitpb.faces.FirstOrDefault(x => x.Contains(tri[0], tri[1], tri[2]));

				return m_CurrentSelection.face != null;
			}

			return false;
		}

		void RefreshSelectedFacePreview()
		{
			// Copy the currently selected vertexes in world space.
			// World space so that we don't have to apply transforms
			// to match the current selection.
			var trs = m_CurrentSelection.mesh.transform;
			int[] indices = m_CurrentSelection.face.indexes.ToArray();
			var positions = m_CurrentSelection.mesh.positions;
			var verts = new Vector3[indices.Length];

			for (int i = 0, c = indices.Length; i < c; i++)
			{
				verts[i] = trs.TransformPoint(positions[i]);
				indices[i] = i;
			}

			// Now go through and move the verts we just grabbed out about .1m from the original face.
			Vector3 normal = trs.TransformDirection(Math.Normal(m_CurrentSelection.mesh, m_CurrentSelection.face));

			for (int i = 0; i < verts.Length; i++)
				verts[i] += normal.normalized * .01f;

			if (m_PreviewMesh)
				Destroy(m_PreviewMesh.gameObject);

			m_PreviewMesh = ProBuilderMesh.Create(verts, new Face[] { new Face(indices) });

			foreach (var face in m_PreviewMesh.faces)
				face.material = m_PreviewMaterial;

			m_PreviewMesh.ToMesh();
			m_PreviewMesh.Refresh();
		}
	}
}
#endif
