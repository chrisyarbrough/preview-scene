// Author: Chris Yarbrough

namespace Nementic
{
	using UnityEditor;

	public class EditorTime
	{
		/*
		 * Implementation Notes
		 * It was considered making this class serializable
		 * in the hopes of preserving the known delta time
		 * during assembly reload. However, it turns out that
		 * this leads to abrupt jumping of animations while
		 * re-initializing and skipping one frame is far less
		 * irritating when observed during e.g. enter-playmode.
		 */

		public float DeltaTime { get; private set; }

		public float Time => (float)EditorApplication.timeSinceStartup;

		private double previousTimeSinceStartup = 0.0;

		public void Update()
		{
			double timeSinceStartup = EditorApplication.timeSinceStartup;

			if (previousTimeSinceStartup == 0.0)
				previousTimeSinceStartup = timeSinceStartup;

			DeltaTime = (float)(timeSinceStartup - previousTimeSinceStartup);

			previousTimeSinceStartup = timeSinceStartup;
		}
	}
}
