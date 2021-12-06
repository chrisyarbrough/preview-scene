// Author: Chris Yarbrough

namespace Nementic
{
	using System;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEditor.SceneManagement;
	using UnityEngine;
	using UnityEngine.SceneManagement;
	using Object = UnityEngine.Object;

	/// <summary>
	/// A hidden scene used to render custom previews.
	/// </summary>
	/// <remarks>
	/// Create a reference to the preview scene via its constructor
	/// and store it as a serialized member (SerializeField).
	/// Call <see cref="Load()"/> before using any other parts
	/// of the interface and dispose of the reference with <see cref="Destroy"/>
	/// to clear up resources and avoid memory leaks in the editor.
	///
	/// Populate the scene via <see cref="Instantiate"/> or <see cref="Add"/>.
	/// By default, the entire scene is rendered in the way it was set up,
	/// but it is also possible to <see cref="Focus"/> a single object.
	/// Refer to the target object by its index in the order it was added.
	/// A focused object will be positioned at scene origin.
	///
	/// Use the <see cref="Camera"/> property to setup the camera and call
	/// <see cref="Render"/> to retrieve a texture that shows the scene.
	/// </remarks>
	[Serializable]
	public class PreviewScene
	{
		/*
		 * Implementation Notes
		 * 
		 * Objects added to the preview scene are marked as HideAndDontSave
		 * so that they survive assembly reload. This design avoids unnecessary
		 * reloading and was chosen over an alternative in which the scene
		 * needs to be recreated in OnEnable e.g. before each enter-playmode.
		 * This may lead to memory leaks if callers forget to call Destroy,
		 * but still seems to be the best implementation since the Unity preview
		 * scene is also not unloaded unless explicitly called. In order for callers
		 * to dispose the struct correctly, they need to keep a serialized reference
		 * to it, so that the reference also survives assembly reload.
		 *
		 * Ideally, as a convenience to users, the code would detect memory leaks:
		 * In the constructor, remember the caller via reflection. In Destroy,
		 * set a flag indicating that resources have been cleared and prevent
		 * the finalizer from being called. In the finalizer check if the flag
		 * was set and if not, log an error. However, this doesn't work robustly,
		 * because of Unity serialization which will call the default constructor
		 * of a serialized reference
		 */

		/// <summary>
		/// Indicates that GameObjects can be added to the scene
		/// or that camera properties can be modified.
		/// </summary>
		public bool IsLoaded { get; private set; }

		/// <summary>
		/// The preview camera. Only valid when the scene is loaded.
		/// </summary>
		public CameraProxy Camera
		{
			get
			{
				if (cameraProxy == null || cameraProxy.IsValid == false)
					cameraProxy = new CameraProxy(camera);

				return cameraProxy;
			}
		}

		public CustomRenderSettings CustomRenderSettings => customRenderSettings;

		/// <summary>
		/// The number of GameObjects that have been added
		/// to the preview scene by the user.
		/// </summary>
		/// <remarks>
		/// This does not include the already existing preview camera,
		/// but it will count invisible objects added, e.g. lights.
		/// </remarks>
		public int ObjectCount => gameObjects?.Count ?? 0;

		[NonSerialized]
		private CameraProxy cameraProxy;
		
		[SerializeField]
		private CustomRenderSettings customRenderSettings = new CustomRenderSettings();

		[SerializeField]
		private Scene scene;

		[SerializeField]
		private Camera camera;

		/// <summary>
		/// All GameObjects added to the scene.
		/// </summary>
		[SerializeField]
		private List<GameObject> gameObjects;

		/// <summary>
		/// The focused scene object. Null if all GameObjects are drawn.
		/// </summary>
		[SerializeField]
		private GameObject currentTarget;

		/// <summary>
		/// A position that is not visible to the preview camera.
		/// </summary>
		private static readonly Vector3 offScreenPosition = new Vector3(1000f, 0f, 0f);

		/// <summary>
		/// Initialize the preview scene with a default render target.
		/// Pairs with <see cref="Destroy"/>.
		/// </summary>
		public void Load()
		{
			Load(renderTargetSize: new Vector2Int(128, 128));
		}

		/// <summary>
		/// Initializes the preview scene with the provided render target size.
		/// Pairs with <see cref="Destroy"/>.
		/// </summary>
		/// <param name="renderTargetSize">The size of the render texture in pixels.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If the render target size is less than one pixel.
		/// </exception>
		public void Load(Vector2Int renderTargetSize)
		{
			if (renderTargetSize.x < 1 || renderTargetSize.y < 1)
				throw new ArgumentOutOfRangeException(nameof(renderTargetSize));

			scene = EditorSceneManager.NewPreviewScene();
			scene.name = "Preview Scene";

			camera = CreateCamera(scene);
			camera.targetTexture = CreateRenderTarget(renderTargetSize);

			gameObjects = new List<GameObject>();

			IsLoaded = true;
		}

		/// <summary>
		/// Closes the preview scene and releases all resources.
		/// Pairs with <see cref="Load()"/>.
		/// </summary>
		public void Destroy()
		{
			if (IsLoaded == false)
				return;

			// The target texture needs to be removed from the camera
			// before destroying it to avoid Unity logging the error:
			// "Releasing render texture that is set as Camera.targetTexture!"
			RenderTexture renderTexture = camera.targetTexture;
			camera.targetTexture = null;
			Object.DestroyImmediate(renderTexture);
			Object.DestroyImmediate(camera.gameObject);

			camera = null;
			gameObjects.Clear();

			EditorSceneManager.ClosePreviewScene(scene);

			IsLoaded = false;
		}

		/// <summary>
		/// Instantiates the provided prefab into the preview scene
		/// and returns the GameObject instance for modification.
		/// </summary>
		public GameObject Instantiate(GameObject prefab)
		{
			var instance = Object.Instantiate(prefab);
			Add(instance);
			return instance;
		}

		/// <summary>
		/// Adds the provided instance to the preview scene.
		/// </summary>
		/// <remarks>
		/// Note, that the argument will be destroyed together
		/// with the preview scene automatically.
		/// </remarks>
		public void Add(GameObject instance)
		{
			instance.hideFlags = HideFlags.HideAndDontSave;
			SceneManager.MoveGameObjectToScene(instance, scene);
			gameObjects.Add(instance);
		}

		public void MoveAllOffscreen()
		{
			for (int i = 0; i < gameObjects.Count; i++)
				gameObjects[i].transform.position = offScreenPosition;
		}

		/// <summary>
		/// Focuses the preview camera on the target identified by the provided index.
		/// All other scene objects will be hidden.
		/// </summary>
		public void Focus(int targetIndex)
		{
			if (currentTarget != null)
				currentTarget.transform.position = offScreenPosition;

			currentTarget = gameObjects[targetIndex];
			currentTarget.transform.position = Vector3.zero;
		}

		/// <summary>
		/// Destroys all added GameObjects within the preview scene.
		/// </summary>
		public void Clear()
		{
			foreach (var go in gameObjects)
				Object.DestroyImmediate(go);

			gameObjects.Clear();
		}

		/// <summary>
		/// Renders the preview scene and returns a refreshed render target.
		/// </summary>
		public Texture Render()
		{
			bool overrideActive = false;

			if (customRenderSettings.UseActiveSceneSettings == false &&
			    Unsupported.SetOverrideLightingSettings(scene))
			{
				overrideActive = true;
				ApplyRenderSettings(customRenderSettings);
			}

			camera.Render();

			if (overrideActive)
				Unsupported.RestoreOverrideLightingSettings();

			return camera.targetTexture;
		}

		private void ApplyRenderSettings(CustomRenderSettings settings)
		{
			RenderSettings.ambientMode = settings.AmbientMode;
			RenderSettings.ambientSkyColor = settings.AmbientSkyColor;
			RenderSettings.ambientEquatorColor = settings.AmbientEquatorColor;
			RenderSettings.ambientGroundColor = settings.AmbientGroundColor;
			RenderSettings.ambientIntensity = settings.AmbientIntensity;
			RenderSettings.ambientLight = settings.AmbientColor;
			RenderSettings.subtractiveShadowColor = settings.SubtractiveShadowColor;
			RenderSettings.skybox = settings.Skybox;
			RenderSettings.sun = settings.Sun;
			RenderSettings.ambientProbe = settings.AmbientProbe;
			RenderSettings.customReflection = settings.CustomReflection;
			RenderSettings.reflectionIntensity = settings.ReflectionIntensity;
			RenderSettings.reflectionBounces = settings.ReflectionBounces;
			RenderSettings.defaultReflectionMode = settings.DefaultReflectionMode;
			RenderSettings.defaultReflectionResolution = settings.DefaultReflectionResolution;
		}

		public void Render(RenderTexture renderTarget)
		{
			var previousTarget = camera.targetTexture;
			camera.targetTexture = renderTarget;
			Render();
			camera.targetTexture = previousTarget;
		}

		/// <summary>
		/// Renders the preview scene and immediately draws the render target
		/// to a GUI texture with the provided rect position and size.
		/// </summary>
		public void RenderToGUI(Rect rect, bool alphaBlend = true)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			Texture texture = Render();

			GUI.DrawTexture(
				rect,
				texture,
				ScaleMode.StretchToFill,
				alphaBlend);
		}

		private static Camera CreateCamera(Scene scene)
		{
			var go = new GameObject("Preview Camera");
			go.hideFlags = HideFlags.HideAndDontSave;
			SceneManager.MoveGameObjectToScene(go, scene);

			Camera camera = go.AddComponent<Camera>();

			camera.scene = scene;
			camera.cameraType = CameraType.Preview;
			camera.clearFlags = CameraClearFlags.Color;
			camera.backgroundColor = Color.clear;
			camera.renderingPath = RenderingPath.Forward;
			camera.useOcclusionCulling = false;
			camera.allowHDR = false;
			camera.allowMSAA = true;
			camera.nearClipPlane = 0.5f;
			camera.farClipPlane = 20f;
			camera.transform.position = new Vector3(0f, 0f, -10f);

			return camera;
		}

		private RenderTexture CreateRenderTarget(Vector2Int renderTargetSize)
		{
			var renderTexture = new RenderTexture(
				renderTargetSize.x,
				renderTargetSize.y,
				24);

			renderTexture.hideFlags = HideFlags.HideAndDontSave;
			renderTexture.name = "Preview RT";
			renderTexture.antiAliasing = 4;
			return renderTexture;
		}

		/// <summary>
		/// A wrapper that allows controlled modifications of
		/// a camera's properties without exposing the object instance.
		/// </summary>
		/// <remarks>
		/// This is used to prevent client code from making
		/// invalid changes or destroying the camera reference.
		/// </remarks>
		public class CameraProxy
		{
			public bool IsValid => camera != null;

			private readonly Camera camera;

			public CameraProxy(Camera camera)
			{
				this.camera = camera;
			}

			public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
			{
				camera.transform.SetPositionAndRotation(position, rotation);
			}

			public Vector3 Position
			{
				get => camera.transform.position;
				set => camera.transform.position = value;
			}

			public Quaternion Rotation
			{
				get => camera.transform.rotation;
				set => camera.transform.rotation = value;
			}

			public void LookAt(Vector3 targetPosition, Vector3 worldUp)
			{
				camera.transform.LookAt(targetPosition, worldUp);
			}

			public float OrthographicSize
			{
				set
				{
					camera.orthographic = true;
					camera.orthographicSize = value;
				}
			}

			public float FieldOfView
			{
				set
				{
					camera.orthographic = false;
					camera.fieldOfView = value;
				}
			}

			public void SetClipPlanes(float near, float far)
			{
				camera.nearClipPlane = near;
				camera.farClipPlane = far;
			}

			public Color BackgroundColor
			{
				get => camera.backgroundColor;
				set => camera.backgroundColor = value;
			}

			public CameraClearFlags ClearFlags
			{
				get => camera.clearFlags;
				set => camera.clearFlags = value;
			}
		}
	}
}
