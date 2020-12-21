using System;
using DG.Tweening;
using Runtime.SaveDataSystem;
using Runtime.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runtime.UI
{
	[RequireComponent(typeof(CanvasGroup)),]
	public class MainMenuScreenBase<T> : Singleton<T> where T : MainMenuScreenBase<T>, IMainMenuScreenBase
	{
		#region Serialize Fields

		[SerializeField,] private GameObject _defaultSelection;

		#endregion

		#region Private Fields

		private CanvasGroup _canvasGroup;
		private GameObject _lastSelection;

		#endregion

		#region Properties

		protected virtual bool FirstScreen => false;
		protected bool Interactable => _canvasGroup.interactable;

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_canvasGroup = GetComponent<CanvasGroup>();
			_canvasGroup.enabled = true;

			_canvasGroup.alpha = 0;
			_canvasGroup.interactable = false;

			if (FirstScreen)
			{
				Show();
			}
		}

		#endregion

		#region Public methods

		public virtual void Show()
		{
			_canvasGroup.blocksRaycasts = true;
			_canvasGroup.DOFade(1, 0.1f).OnComplete(() =>
			                                        {
				                                        _canvasGroup.interactable = true;

				                                        GameObject selection;
				                                        if (_lastSelection != null)
				                                        {
					                                        selection = _lastSelection;
				                                        }
				                                        else
				                                        {
					                                        selection = _defaultSelection;
				                                        }

				                                        EventSystem.current.SetSelectedGameObject(selection);
			                                        });
		}

		#endregion

		#region Protected methods

		protected void TransitionToScreen(IMainMenuScreenBase nextScreen, bool requireOnlineUsernameSet = false)
		{
			_canvasGroup.interactable = false;
			if (requireOnlineUsernameSet && !SaveData.HasNickName)
			{
				EnterNickNameScreen.Instance.SetScreenAfterNickNameEntered(nextScreen);
				TransitionToScreen(EnterNickNameScreen.Instance);
			}
			else
			{
				Hide(nextScreen.Show);
			}
		}

		protected virtual void Hide(Action action)
		{
			_lastSelection = EventSystem.current.currentSelectedGameObject;
			_canvasGroup.interactable = false;
			_canvasGroup.blocksRaycasts = false;
			_canvasGroup.DOFade(0, 0.1f).OnComplete(() => { action(); });
		}

		#endregion
	}
}