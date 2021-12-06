// Author: Chris Yarbrough

namespace Nementic
{
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.Rendering;
	using UnityEngine.UIElements;

	public class PreviewSceneExample_UITK : EditorWindow
	{
		[MenuItem("Nementic/Examples/Preview Scene UITK")]
		public static void ShowExample()
		{
			var window = GetWindow<PreviewSceneExample_UITK>("Preview Scene");

			const int width = 535;
			const int height = 565;

			window.position = new Rect(
				x: (Screen.currentResolution.width - width) / 2f,
				y: (Screen.currentResolution.height - height) / 2f,
				width,
				height);
		}

		[SerializeField]
		private PreviewScene previewScene = new PreviewScene();

		private VisualElement previewsRoot;
		private List<Image> previewElements = new List<Image>();
		private EditorTime editorTime = new EditorTime();

		private GameObject cube;
		private Light light;
		private bool previewIsOpen;

		public void CreateGUI()
		{
			string uxmlPath = AssetDatabase.GUIDToAssetPath("20812a6955f0b2042946fc5818b64a75");
			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
			visualTree.CloneTree(rootVisualElement);

			string styleSheetPath = AssetDatabase.GUIDToAssetPath("e95d4e50f07375a4caf8ac29ade18c2d");
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
			rootVisualElement.styleSheets.Add(styleSheet);

			previewsRoot = rootVisualElement.Q("Previews");
			previewElements.Clear();

			var openButton = rootVisualElement.Q<Button>("OpenButton");
			openButton.clicked += () =>
			{
				OnDestroy();
				SetupPreviewScene();
				CreatePreviewElements();
				previewIsOpen = true;
			};

			var closeButton = rootVisualElement.Q<Button>("CloseButton");
			closeButton.clicked += () =>
			{
				OnDestroy();
				previewIsOpen = false;
			};

			// If the window is reconstructed after assembly load while
			// the preview was already open.
			if (previewIsOpen)
			{
				CreatePreviewElements();
			}
		}

		private void OnDestroy()
		{
			// Free preview scene resources to avoid memory leak.
			previewScene?.Destroy();
			previewsRoot.Clear();

			foreach (var image in previewElements)
			{
				if (image.image != null)
					DestroyImmediate(image.image);
			}
			previewElements.Clear();
		}

		private void SetupPreviewScene()
		{
			previewScene.Load(renderTargetSize: new Vector2Int(256, 256));
			previewScene.Camera.FieldOfView = 20f;
			previewScene.Camera.Position = new Vector3(0f, 0f, -7f);
			previewScene.Camera.ClearFlags = CameraClearFlags.Color;
			previewScene.Camera.BackgroundColor = Color.black;
			previewScene.CustomRenderSettings.AmbientMode = AmbientMode.Flat;
			previewScene.CustomRenderSettings.AmbientColor = Color.red;

			cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			previewScene.Add(cube);
			previewScene.Add(GameObject.CreatePrimitive(PrimitiveType.Capsule));
			previewScene.Add(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
			previewScene.Add(GameObject.CreatePrimitive(PrimitiveType.Sphere));

			light = new GameObject().AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 0.9f;
			light.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, -1f, 1f));
			light.color = new Color(0.6f, 0.7f, 1f);
			previewScene.Add(light.gameObject);
		}

		private void CreatePreviewElements()
		{
			previewScene.MoveAllOffscreen();

			const int objectCount = 4;
			const int columnCount = 2;
			int rowCount = Mathf.RoundToInt(objectCount / (float)columnCount);

			for (int row = 0; row < rowCount; row++)
			{
				var rowElement = new VisualElement();
				previewsRoot.Add(rowElement);

				for (int column = 0; column < columnCount; column++)
				{
					int linearIndex = row * columnCount + column;
					var previewElement = CreatePreviewElement(linearIndex);
					rowElement.Add(previewElement);

					this.previewElements.Add(previewElement);
				}
			}
		}

		private Image CreatePreviewElement(int index)
		{
			previewScene.Focus(index);
			RenderTexture texture = previewScene.Render() as RenderTexture;
			var renderTexture = new RenderTexture(texture.width, texture.height, texture.depth);
			renderTexture.hideFlags = HideFlags.DontSave;
			renderTexture.antiAliasing = texture.antiAliasing;
			Graphics.CopyTexture(texture, renderTexture);

			var ve = new Image();
			ve.image = renderTexture;
			return ve;
		}

		private void Update()
		{
			editorTime.Update();

			if (cube != null && light != null)
			{
				float dt = editorTime.DeltaTime;
				cube.transform.Rotate(10f * dt, 40f * dt, 0f);
				light.transform.Rotate(0f, 10f * dt, 0f);
				RedrawPreviews();
			}
		}

		private void RedrawPreviews()
		{
			for (var i = 0; i < previewElements.Count; i++)
			{
				previewScene.Focus(i);
				var image = previewElements[i];
				previewScene.Render(image.image as RenderTexture);
			}
			Repaint();
		}
	}
}
