using Runtime.Utils;
using UnityEngine;
using UnityEngine.Playables;

namespace Runtime.GameSystem
{
	public class IntroManager : Singleton<IntroManager>
	{
		#region Serialize Fields

		[SerializeField,] private PlayableDirector _introCutscene;

		#endregion

		#region Properties

		public bool Done { get; private set; }

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_introCutscene.stopped += director => Done = true;
		}

		#endregion
	}
}