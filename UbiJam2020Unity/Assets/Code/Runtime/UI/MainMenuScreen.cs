using Runtime.GameSystem;
using Runtime.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
	public class MainMenuScreen : MainMenuScreenBase<MainMenuScreen>, IMainMenuScreenBase
	{
		#region Serialize Fields

		[SerializeField,] private Button _localMultiplayerButton;
		[SerializeField,] private Button _joinOnlineMatchButton;
		[SerializeField,] private Button _changeNickNameScreenButton;

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_changeNickNameScreenButton.onClick.AddListener(OnChangeNickName);
			_joinOnlineMatchButton.onClick.AddListener(JoinOnlineMatch);
		}

		#endregion

		#region Private methods

		private void JoinOnlineMatch()
		{
			GameStartParameters startGameParameters = new GameStartParameters(GameStartParameters.GameStartType.JoinRandomMatch);
			startGameParameters.GameConfiguration=GameConfiguration.RandomOnlineMatch();
			MainMenuManager.Instance.GameStartParameters = startGameParameters;
			TransitionToScreen(CharacterSelectionScreen.Instance, true);
		}

		private void OnChangeNickName()
		{
			TransitionToScreen(EnterNickNameScreen.Instance);
		}

		#endregion
	}
}