using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Runtime.GameSystem;
using Runtime.InputSystem;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Runtime.UI
{
	public class MainMenuManager : StateMachineSingleton<MainMenuState, MainMenuManager>
	{
		#region Serialize Fields

		[SerializeField,] private CanvasGroup _titleScreenCanvasGroup;
		[SerializeField,] private CanvasGroup _blackfade;
		[SerializeField,] private List<CharacterSelectionScreen> _characterSelectionScreens;
		[SerializeField,] private AudioSource _titleScreenAudio;
		[SerializeField,] private AudioSource _titleScreenClosedAudio;
		[SerializeField,] private AudioSource _selectionScreenAudio;
		[SerializeField,] private AudioSource _chooseYourFighterAudio;
		[SerializeField,] private CanvasGroup _characterSelectionCanvasGroup;
		[SerializeField] private PlayableDirector _titleScreenPlayableDirector;

		#endregion

		#region Properties

		protected override MainMenuState InitialState => MainMenuState.TitleScreen;
		public float CharacterScreenOpenedTime { get; private set; }

		#endregion

		#region Protected methods

		protected override MainMenuState GetNextState()
		{
			switch (State)
			{
				case MainMenuState.TitleScreen:
					if (PlayerInputManager.Instance.GetInputForPlayer(0).Eat || PlayerInputManager.Instance.GetInputForPlayer(1).Eat)
					{
						return MainMenuState.CharacterSelection;
					}
					break;
				case MainMenuState.CharacterSelection:
					if (_characterSelectionScreens.All(screen => screen.SelectionDone))
					{
						return MainMenuState.Starting;
					}
					break;
				case MainMenuState.Starting:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return State;
		}

		protected override void OnStateChange(MainMenuState oldState, MainMenuState newState)
		{
			switch (newState)
			{
				case MainMenuState.TitleScreen:
					break;
				case MainMenuState.CharacterSelection:
					_titleScreenClosedAudio.Play();
					_titleScreenAudio.Stop();
					CharacterScreenOpenedTime = float.MaxValue;
					_titleScreenPlayableDirector.Play();
					_titleScreenCanvasGroup.DOFade(0, 0.3f).OnComplete(() =>
					                                                   {
						                                                   _characterSelectionCanvasGroup.DOFade(1, 0.5f);
						                                                   _selectionScreenAudio.Play();
						                                                   _chooseYourFighterAudio.Play();
						                                                   CharacterScreenOpenedTime = Time.time;
					                                                   }).SetDelay(1.5f);
					break;
				case MainMenuState.Starting:
					_blackfade.DOFade(1, 0.5f).OnComplete(() => SceneManager.LoadScene(1)).SetDelay(1f);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
			}
		}

		#endregion
	}

	public enum MainMenuState
	{
		TitleScreen,
		CharacterSelection,
		Starting,
	}
}