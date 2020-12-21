using System;
using System.Collections.Generic;
using System.Linq;
using Runtime.Multiplayer;

namespace Runtime.UI
{
	public class CharacterSelectionScreen : MainMenuScreenBase<CharacterSelectionScreen>, IMainMenuScreenBase
	{
		#region Private Fields

		private CharacterSelectionPanel[] _characterSelectionPanels;
		private List<CharacterSelectionPanel> _activeCharacterSelectionPanels=new List<CharacterSelectionPanel>();

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_characterSelectionPanels = GetComponentsInChildren<CharacterSelectionPanel>();
			foreach (CharacterSelectionPanel panel in _characterSelectionPanels)
			{
				panel.gameObject.SetActive(false);
			}
		}

		#endregion

		private void Update()
		{
			if (Interactable)
			{
				if (_activeCharacterSelectionPanels.All(panel => panel.SelectionDone))
				{
					MainMenuManager.Instance.GameStartParameters.SetCharacters(_characterSelectionPanels.Select(panel => panel.SelectedCharacter).ToList());
					TransitionToScreen(LoadMatchScreen.Instance, false);
				}
			}
		}

		#region IMainMenuScreenBase Members

		public override void Show()
		{
			base.Show();
			_activeCharacterSelectionPanels.Clear();
			GameStartParameters gameStartParameters = MainMenuManager.Instance.GameStartParameters;
			int characterSelectionsToShow;
			if (gameStartParameters.Type == GameStartParameters.GameStartType.LocalMultiplayer)
			{
				characterSelectionsToShow = gameStartParameters.GameConfiguration.PlayerCount;
			}
			else
			{
				characterSelectionsToShow = 1;
			}

			for (int i = 0; i < _characterSelectionPanels.Length; i++)
			{
				bool active = i < characterSelectionsToShow;
				_characterSelectionPanels[i].gameObject.SetActive(active);
				if (active)
				{
					_activeCharacterSelectionPanels.Add(_characterSelectionPanels[i]);
				}
			}
		}

		#endregion
	}
}