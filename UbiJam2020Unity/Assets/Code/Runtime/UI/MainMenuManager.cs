using System.Collections.Generic;
using System.Linq;
using Runtime.Multiplayer;
using Runtime.Utils;
using UnityEngine.SceneManagement;

namespace Runtime.UI
{
	public class MainMenuManager : Singleton<MainMenuManager>
	{
		#region Static Stuff

		private static MainMenuOpenReason _mainMenuOpenReason;
		public GameStartParameters GameStartParameters { get; set; }

		public static void OpenMainMenu(MainMenuOpenReason mainMenuOpenReason)
		{
			_mainMenuOpenReason = mainMenuOpenReason;
			SceneManager.LoadScene(0);
		}

		#endregion

		#region Private Fields

		private List<CharacterSelectionPanel> _characterSelectionScreens;

		#endregion

		#region Properties

		public float CharacterScreenOpenedTime { get; private set; }

		#endregion

		#region Unity methods

		private void Start()
		{
			if (_mainMenuOpenReason == MainMenuOpenReason.StartOfflineDebugSession)
			{
				Lobby.Instance.ConnectOffline();
			}

			_characterSelectionScreens = FindObjectsOfType<CharacterSelectionPanel>().ToList();
		}

		#endregion
	}

	public enum MainMenuOpenReason
	{
		FirstGameStart,
		StartOfflineDebugSession,
		PlayerDisconnected,
		SelfDisconnected,
	}
}