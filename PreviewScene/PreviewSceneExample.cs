// Author: Chris Yarbrough

namespace Nementic
{
	using UnityEditor;
	using UnityEngine;

	/// <summary>
	/// Demonstrates how to use <see cref="PreviewScene"/>.
	/// </summary>
	public sealed class PreviewSceneExample : EditorWindow
	{
		[MenuItem("Nementic/Examples/Preview Scene")]
		public static void Open()
		{
			var window = GetWindow<PreviewSceneExample>("Preview Scene");

			const int width = 535;
			const int height = 565;

			window.position = new Rect(
				x: (Screen.currentResolution.width - width) / 2f,
				y: (Screen.currentResolution.height - height) / 2f,
				width,
				height);
		}

		// The scene stays in memory until it is unloaded,
		// so serialize its state in order to destroy it later.
		[SerializeField]
		private PreviewScene previewScene = new PreviewScene();

		private GameObject cube;
		private Light light;
		private Vector2 scroll;
		private EditorTime editorTime = new EditorTime();

		private void OnDestroy()
		{
			// Free preview scene resources to avoid memory leak.
			previewScene.Destroy();
		}

		private void Update()
		{
			editorTime.Update();

			if (cube != null && light != null)
			{
				float dt = editorTime.DeltaTime;
				cube.transform.Rotate(10f * dt, 30f * dt, 0f);

				var angles = light.transform.eulerAngles;
				angles.x = 31f + Mathf.Sin(editorTime.Time * 0.2f) * 30f;
				angles.y += 15f * dt;
				angles.z = 0f;
				light.transform.eulerAngles = angles;

				Repaint();
			}
		}

		private void OnGUI()
		{
			if (GUILayout.Button("Open Preview"))
				OpenPreview();

			if (GUILayout.Button("Close Preview"))
				previewScene.Destroy();

			if (previewScene.IsLoaded)
			{
				scroll = EditorGUILayout.BeginScrollView(scroll);
				DrawPreviews();
				EditorGUILayout.EndScrollView();
			}
		}

		private void OpenPreview()
		{
			// Unload the scene if it already exists to start from a clean slate.
			previewScene.Destroy();

			previewScene.Load(renderTargetSize: new Vector2Int(256, 256));
			previewScene.Camera.FieldOfView = 20f;
			previewScene.Camera.Position = new Vector3(0f, 0f, -7f);
			previewScene.Camera.ClearFlags = CameraClearFlags.Skybox;
			previewScene.CustomRenderSettings.UseActiveSceneSettings = true;

			cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			previewScene.Add(cube);
			previewScene.Add(GameObject.CreatePrimitive(PrimitiveType.Capsule));
			previewScene.Add(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
			previewScene.Add(GameObject.CreatePrimitive(PrimitiveType.Sphere));

			previewScene.MoveAllOffscreen();

			light = new GameObject().AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 2f;
			light.transform.eulerAngles = new Vector3(50f, -30f, 0f);
			light.color = new Color(0.6f, 0.7f, 1f);
			previewScene.Add(light.gameObject);
		}

		private void DrawPreviews()
		{
			// How large a single preview rect is in pixels.
			const int previewSize = 256;

			// Pixels between each preview.
			const int margin = 4;

			// The number of objects added in OpenPreview.
			const int objectCount = 4;
			const int columnCount = 2;
			int rowCount = Mathf.RoundToInt(objectCount / (float)columnCount);

			Rect rect = EditorGUILayout.GetControlRect(
				hasLabel: false,
				height: (previewSize * objectCount) + (margin * objectCount - 1));

			rect.height = previewSize;
			rect.width = rect.height;
			float originalX = rect.x;

			for (int row = 0; row < rowCount; row++)
			{
				for (int column = 0; column < columnCount; column++)
				{
					// It's possible to refer to the objects by their index 
					// (the order in which they were added) and then only render
					// one of them in the preview.
					int linearIndex = row * columnCount + column;

					if (linearIndex >= objectCount)
						break;

					DrawPreview(rect, linearIndex);

					// A shorthand for the previous two calls would be:
					// previewScene.RenderToGUI(rect, alphaBlend: false);

					rect.x = rect.xMax + margin;
				}

				rect.y = rect.yMax + margin;
				rect.x = originalX;
			}
		}

		private void DrawPreview(Rect rect, int index)
		{
			previewScene.Focus(index);

			// Instead of rendering every frame, it would be enough
			// to only update the texture when the scene was changed.
			Texture texture = previewScene.Render();

			GUI.DrawTexture(
				rect,
				texture,
				ScaleMode.StretchToFill,
				alphaBlend: false);
		}
	}
}
