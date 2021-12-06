// Author: Chris Yarbrough

namespace Nementic
{
	using System;
	using UnityEngine;
	using UnityEngine.Rendering;

	[Serializable]
	public class CustomRenderSettings
	{
		/// <summary>
		/// Uses the active scene's lighting setting instead.
		/// </summary>
		public bool UseActiveSceneSettings;

		public AmbientMode AmbientMode = AmbientMode.Trilight;
		public Color AmbientSkyColor = new Color(0.5f, 0.51f, 0.55f);
		public Color AmbientEquatorColor = new Color(0.37f, 0.39f, 0.4f);
		public Color AmbientGroundColor = new Color(0.24f, 0.23f, 0.21f);
		public float AmbientIntensity = 1f;
		public Color AmbientColor = new Color(0.5f, 0.51f, 0.55f);

		public Color SubtractiveShadowColor = new Color(0.42f, 0.48f, 0.63f);

		public Material Skybox;
		public Light Sun;
		public SphericalHarmonicsL2 AmbientProbe;
		public Cubemap CustomReflection;
		public float ReflectionIntensity = 1f;
		public int ReflectionBounces = 1;
		public DefaultReflectionMode DefaultReflectionMode;
		public int DefaultReflectionResolution = 64;
	}
}
