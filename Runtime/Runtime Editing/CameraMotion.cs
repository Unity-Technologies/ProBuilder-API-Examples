/**
 * This script controls camera movement.
 *
 * Partially derived from FlyThrough.js available on the Unify Wiki
 * 	- http://wiki.unity3d.com/index.php/FlyThrough
 */

// #define CONTROLLER
// #define USE_DELTA_TIME

using UnityEngine;
using UnityEngine.EventSystems;

namespace GILES
{
	public enum Tool
	{
		None,
		Position,
		Rotate,
		Scale,
		View
	}

	public enum ViewTool
	{
		None,	// Camera is not in control of anything
		Orbit,	// Camera is spherically rotating around target
		Pan,	// Camera is moving right or left
		Dolly,	// Camera is moving forward or backwards
		Look 	// Camera is looking and possibly flying
	}

	/**
	 * Requires InputSettings to have:
	 * 	- "Horizontal", "Vertical", "CameraUp", with Gravity and Sensitivity set to 3.
	 */
	[RequireComponent(typeof(Camera))]
	public class CameraMotion : MonoBehaviour
	{
		static CameraMotion instance;
		public delegate void OnCameraMoveEvent(CameraMotion cam);
		public event OnCameraMoveEvent OnCameraMove;

		public delegate void OnCameraFinishMoveEvent(CameraMotion cam);
		public event OnCameraFinishMoveEvent OnCameraFinishMove;

		public static void AddOnCameraMoveDelegate(OnCameraMoveEvent del)
		{
			if(instance == null)
			{
				Debug.LogWarning("No pb_SceneCamera found, but someone is trying to subscribe to events!");
				return;
			}

			if(instance.OnCameraMove == null)
				instance.OnCameraMove = del;
			else
				instance.OnCameraMove += del;
		}

		public ViewTool cameraState { get; private set; }

		public Texture2D PanCursor;
		public Texture2D OrbitCursor;
		public Texture2D DollyCursor;
		public Texture2D LookCursor;
		private Texture2D currentCursor;

		const int CURSOR_ICON_SIZE = 64;

		const string INPUT_MOUSE_SCROLLWHEEL = "Mouse ScrollWheel";
		const string INPUT_MOUSE_X = "Mouse X";
		const string INPUT_MOUSE_Y = "Mouse Y";

		const int LEFT_MOUSE = 0;
		const int RIGHT_MOUSE = 1;
		const int MIDDLE_MOUSE = 2;

		const float MIN_CAM_DISTANCE = 1f;
		const float MAX_CAM_DISTANCE = 100f;

	#if USE_DELTA_TIME
		public float moveSpeed = 15f;		///< How fast the camera position moves.
		public float lookSpeed = 200f;		///< How fast the camera rotation adjusts.
		public float orbitSpeed = 200f;		///< How fast the camera rotation adjusts.
		public float scrollModifier = 100f; ///< How fast the mouse scroll wheel affects distance from pivot.
		public float zoomSpeed = .05f;
	#else
		public float moveSpeed = 15f;		///< How fast the camera position moves.
		public float lookSpeed = 5f;		///< How fast the camera rotation adjusts.
		public float orbitSpeed = 7f;		///< How fast the camera rotation adjusts.
		public float scrollModifier = 100f; ///< How fast the mouse scroll wheel affects distance from pivot.
		public float zoomSpeed = .1f;
	#endif

		private bool eatMouse = false;

		private Camera cam;

		private Vector3 pivot = Vector3.zero;
		public Vector3 GetPivot() { return pivot; }
		private float distanceToCamera = 10f;

		private Vector3 prev_mousePosition = Vector3.zero;	///< Store the mouse position from the last frame.  Used in calculating deltas for mouse movement.

		private Rect mouseCursorRect = new Rect(0,0,CURSOR_ICON_SIZE,CURSOR_ICON_SIZE);
		private Rect screenCenterRect = new Rect(0,0,CURSOR_ICON_SIZE,CURSOR_ICON_SIZE);

		private bool currentActionValid = true;

		Vector3 CamTarget()
		{
			return transform.position + transform.forward * distanceToCamera;
		}

		void Awake()
		{
			instance = this;
		}

		void Start()
		{
//			pb_InputManager.AddMouseInUseDelegate( IsUsingMouse );
//			pb_InputManager.AddKeyInUseDelegate( IsUsingKey );

			cam = GetComponent<Camera>();
			distanceToCamera = Vector3.Distance(pivot, Camera.main.transform.position);
		}

		public GameObject plane;

		static float CalcSignedMouseDelta(Vector2 lhs, Vector2 rhs)
		{
			float delta = Vector2.Distance(lhs, rhs);
			float scale = 1f / Mathf.Min(Screen.width, Screen.height);

			// If horizontal movement is greater than vertical movement, use the X axis for sign.
			if( Mathf.Abs(lhs.x - rhs.x) > Mathf.Abs(lhs.y - rhs.y) )
				return delta * scale * ( (lhs.x-rhs.x) > 0f ? 1f : -1f );

			return delta * scale * ( (lhs.y-rhs.y) > 0f ? 1f : -1f );
		}

		void OnGUI()
		{
			float screenHeight = Screen.height;

			mouseCursorRect.x = Input.mousePosition.x - 16;
			mouseCursorRect.y = (screenHeight - Input.mousePosition.y) - 16;

			screenCenterRect.x = Screen.width/2-32;
			screenCenterRect.y = screenHeight/2-32;

			Cursor.visible = cameraState == ViewTool.None;	/// THIS FLICKERS IN EDITOR, BUT NOT BUILD

			if(cameraState != ViewTool.None)
			{
				switch(cameraState)
				{
					case ViewTool.Orbit:
						GUI.Label(mouseCursorRect, OrbitCursor);
						break;
					case ViewTool.Pan:
						GUI.Label(mouseCursorRect, PanCursor);
						break;
					case ViewTool.Dolly:
						GUI.Label(mouseCursorRect, DollyCursor);
						break;
					case ViewTool.Look:
						GUI.Label(mouseCursorRect, LookCursor);
						break;
				}
			}
		}

		/**
		 * If the scene camera controls are currently using the mouse for navigation, returns false.
		 */
		public bool IsUsingMouse(Vector2 mousePosition)
		{
			return cameraState != ViewTool.None || eatMouse || Input.GetKey(KeyCode.LeftAlt);
		}

		public bool IsUsingKey()
		{
			return Input.GetKey(KeyCode.LeftAlt);
		}

		bool CheckMouseOverGUI()
		{
			return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
		}

		void LateUpdate()
		{

			if(Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
			{
				if(cameraState != ViewTool.None && OnCameraFinishMove != null)
					OnCameraFinishMove(this);

				currentActionValid = true;
				eatMouse = false;
			}
			else
			if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
			{
				currentActionValid = CheckMouseOverGUI();
			}
			else
			{
				if(transform.hasChanged && OnCameraMove != null)
				{
					OnCameraMove(this);
					transform.hasChanged = false;
				}
			}

			cameraState = ViewTool.None;

			/**
			 * Camera is flying itself to a target
			 */
			if(zooming)
			{
				transform.position = Vector3.Lerp(previousPosition, targetPosition, (zoomProgress += Time.deltaTime)/zoomSpeed);
				if( Vector3.Distance(transform.position, targetPosition) < .1f) zooming = false;
			}

			if( (Input.GetAxis(INPUT_MOUSE_SCROLLWHEEL) != 0f || (Input.GetMouseButton(RIGHT_MOUSE) && Input.GetKey(KeyCode.LeftAlt))) && CheckMouseOverGUI())
			{
				float delta = Input.GetAxis(INPUT_MOUSE_SCROLLWHEEL);

				if( Mathf.Approximately(delta, 0f) )
				{
					cameraState = ViewTool.Dolly;
					delta = CalcSignedMouseDelta(Input.mousePosition, prev_mousePosition);
				}

				distanceToCamera -= delta * (distanceToCamera/MAX_CAM_DISTANCE) * scrollModifier;
				distanceToCamera = Mathf.Clamp(distanceToCamera, MIN_CAM_DISTANCE, MAX_CAM_DISTANCE);
				transform.position = transform.localRotation * (Vector3.forward * -distanceToCamera) + pivot;
			}

			bool viewTool = true;

			/**
			 * If the current tool isn't View, or no mouse button is pressed, record the mouse position then early exit.
			 */
			if (!currentActionValid || (viewTool
#if !CONTROLLER
			                            && !Input.GetMouseButton(LEFT_MOUSE)
			                            && !Input.GetMouseButton(RIGHT_MOUSE)
			                            && !Input.GetMouseButton(MIDDLE_MOUSE)
			                            && !Input.GetKey(KeyCode.LeftAlt)
#endif
			    ))
			{
				Rect screen = new Rect(0, 0, Screen.width, Screen.height);

				if (screen.Contains(Input.mousePosition))
					prev_mousePosition = Input.mousePosition;

				return;
			}

			// FPS view camera
			if( Input.GetMouseButton(RIGHT_MOUSE) && !Input.GetKey(KeyCode.LeftAlt) )//|| Input.GetKey(KeyCode.LeftShift) )
			{
				cameraState = ViewTool.Look;

				eatMouse = true;

				// Rotation
				float rot_x = Input.GetAxis(INPUT_MOUSE_X);
				float rot_y = Input.GetAxis(INPUT_MOUSE_Y);

				Vector3 eulerRotation = transform.localRotation.eulerAngles;

				#if USE_DELTA_TIME
				eulerRotation.x -= rot_y * lookSpeed * Time.deltaTime; 	// Invert Y axis
				eulerRotation.y += rot_x * lookSpeed * Time.deltaTime;
				#else
				eulerRotation.x -= rot_y * lookSpeed;
				eulerRotation.y += rot_x * lookSpeed;
				#endif
				eulerRotation.z = 0f;
				transform.localRotation = Quaternion.Euler(eulerRotation);

				// PositionHandle-- Always use delta time when flying
				float speed = moveSpeed * Time.deltaTime;

				transform.position += transform.forward * speed * Input.GetAxis("Vertical");
				transform.position += transform.right * speed * Input.GetAxis("Horizontal");
				try {
					transform.position += transform.up * speed * Input.GetAxis("CameraUp");
				} catch {
					Debug.LogWarning("CameraUp input is not configured.  Open \"Edit/Project Settings/Input\" and add an input named \"CameraUp\", mapping q and e to Negative and Positive buttons.");
				}

				pivot = transform.position + transform.forward * distanceToCamera;
			}
			else
			/**
			 * Orbit
			 */
			if(Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(LEFT_MOUSE))
			{
				cameraState = ViewTool.Orbit;

				eatMouse = true;

				float rot_x = Input.GetAxis(INPUT_MOUSE_X);
				float rot_y = -Input.GetAxis(INPUT_MOUSE_Y);

				Vector3 eulerRotation = transform.localRotation.eulerAngles;

				if( (Mathf.Approximately(eulerRotation.x, 90f) && rot_y > 0f) ||
					(Mathf.Approximately(eulerRotation.x, 270f) && rot_y < 0f) )
					rot_y = 0f;

				#if USE_DELTA_TIME
				eulerRotation.x += rot_y * orbitSpeed * Time.deltaTime;
				eulerRotation.y += rot_x * orbitSpeed * Time.deltaTime;
				#else
				eulerRotation.x += rot_y * orbitSpeed;
				eulerRotation.y += rot_x * orbitSpeed;
				#endif

				eulerRotation.z = 0f;

				transform.localRotation = Quaternion.Euler( eulerRotation );
				transform.position = CalculateCameraPosition(pivot);
			}
			else
			/**
			 * Pan
			 */
			if(Input.GetMouseButton(MIDDLE_MOUSE) || (Input.GetMouseButton(LEFT_MOUSE) && viewTool ) )
			{
				cameraState = ViewTool.Pan;

				Vector2 delta = Input.mousePosition - prev_mousePosition;

				delta.x = ScreenToWorldDistance(delta.x, distanceToCamera);
				delta.y = ScreenToWorldDistance(delta.y, distanceToCamera);

				transform.position -= transform.right * delta.x;
				transform.position -= transform.up * delta.y;

				pivot = transform.position + transform.forward * distanceToCamera;
			}

			prev_mousePosition = Input.mousePosition;
		}

		Vector3 CalculateCameraPosition(Vector3 target)
		{
			return transform.localRotation * (Vector3.forward * -distanceToCamera) + target;
		}

		bool zooming = false;
		float zoomProgress = 0f;
		Vector3 previousPosition = Vector3.zero, targetPosition = Vector3.zero;

		/**
		 * Lerp the camera to the current selection
		 */
		public static void Focus(Vector3 target)
		{
			instance.ZoomInternal(target, 10f);
		}

		public static void Focus(Vector3 target, float distance)
		{
			instance.ZoomInternal(target, distance);
		}

		public static void Focus(GameObject target)
		{
			instance.ZoomInternal(target);
		}

		private void ZoomInternal( Vector3 target, float distance )
		{
			pivot = target;
			distanceToCamera = distance;
			previousPosition = transform.position;
			targetPosition = CalculateCameraPosition( pivot );
			zoomProgress = 0f;
			zooming = true;
		}

		/// <summary>
		/// Lerp the camera to the current selection
		/// </summary>
		/// <param name="target"></param>
		void ZoomInternal( GameObject target )
		{
			Vector3 center = target.transform.position;
			Renderer renderer = target.GetComponent<Renderer>();
			Bounds bounds = renderer != null ? renderer.bounds : new Bounds(center, Vector3.one * 10f);

			distanceToCamera = CalcMinDistanceToBounds(cam, bounds);
			distanceToCamera += distanceToCamera;
			center = bounds.center;

			ZoomInternal( center, distanceToCamera );
		}

		float ScreenToWorldDistance(float screenDistance, float distanceFromCamera)
		{
			Vector3 start = cam.ScreenToWorldPoint(Vector3.forward * distanceFromCamera);
			Vector3 end = cam.ScreenToWorldPoint( new Vector3(screenDistance, 0f, distanceFromCamera));
			return CopySign(Vector3.Distance(start, end), screenDistance);
		}

		static float CalcMinDistanceToBounds(Camera cam, Bounds bounds)
		{
			float frustumHeight = Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.y), bounds.size.z);
			float distance = frustumHeight * .5f / Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);

			return distance;
		}

		/// <summary>
		/// Return the magnitude of X with the sign of Y.
		/// </summary>
		float CopySign(float x, float y)
		{
			if(x < 0f && y < 0f || x > 0f && y > 0f || x == 0f || y == 0f)
				return x;
			else
				return -x;
		}
	}
}
