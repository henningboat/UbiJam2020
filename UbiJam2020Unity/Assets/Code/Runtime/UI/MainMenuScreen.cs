using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
	public class MainMenuScreen : MainMenuScreenBase<MainMenuScreen>, IMainMenuScreenBase
	{
		#region Serialize Fields

		[SerializeField,] private Button _changeNickNameScreenButton;

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_changeNickNameScreenButton.onClick.AddListener(OnChangeNickName);
		}

		#endregion

		#region Private methods

		private void OnChangeNickName()
		{
			TransitionToScreen(EnterNickNameScreen.Instance);
		}

		#endregion
	}
}