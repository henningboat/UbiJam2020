using Runtime.SaveDataSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runtime.UI
{
	public class EnterNickNameScreen : MainMenuScreenBase<EnterNickNameScreen>, IMainMenuScreenBase
	{
		#region Serialize Fields

		[SerializeField,] private TMP_InputField _nameInput;
		private IMainMenuScreenBase _nextScreen;

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_nameInput.characterLimit = 15;
			_nameInput.onSubmit.AddListener(OnSubmit);
		}

		private void OnSubmit(string arg0)
		{
			if (arg0.Length > 0)
			{
				SaveData.SetNickName(arg0);
				if (_nextScreen != null)
				{
					TransitionToScreen(_nextScreen);
					_nextScreen = null;
				}
				else
				{
					TransitionToScreen(MainMenuScreen.Instance);
				}
			}
			else
			{
				EventSystem.current.SetSelectedGameObject(_nameInput.gameObject);
				_nameInput.ActivateInputField();
			}
		}

		#endregion

		#region IMainMenuScreenBase Members

		public override void Show()
		{
			base.Show();
			if (SaveData.HasNickName)
			{
				_nameInput.text = SaveData.NickName;
			}
		}

		#endregion

		public void SetScreenAfterNickNameEntered(IMainMenuScreenBase nextScreen)
		{
			_nextScreen = nextScreen;
		}
	}
}