using System;
using DG.Tweening;
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

		#endregion

		#region Properties

		protected virtual bool FirstScreen => false;

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
				                                        EventSystem.current.SetSelectedGameObject(_defaultSelection);
			                                        });
		}

		#endregion

		#region Protected methods

		protected void TransitionToScreen(IMainMenuScreenBase nextScreen)
		{
			_canvasGroup.interactable = false;
			Hide(nextScreen.Show);
		}

		protected virtual void Hide(Action action)
		{
			_canvasGroup.interactable = false;
			_canvasGroup.blocksRaycasts = false;
			_canvasGroup.DOFade(0, 0.1f).OnComplete(() => { action(); });
		}

		#endregion
	}
}