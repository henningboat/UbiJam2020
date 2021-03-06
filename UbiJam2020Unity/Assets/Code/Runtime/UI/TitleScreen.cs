﻿using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Runtime.UI
{
	public class TitleScreen : MainMenuScreenBase<TitleScreen>, IMainMenuScreenBase
	{
		#region Serialize Fields

		[SerializeField,] private Button _invisibleButton;
		[SerializeField,] private PlayableDirector _titleScreenTimeline;
		[SerializeField,] private AudioSource _titleScreenClosedAudio;
		[SerializeField,] private AudioSource _titleScreenAudio;
		[SerializeField,] private AudioSource _mainMenuAudio;

		#endregion

		#region Private Fields

		private Action _titleScreenTimelineFinishedAction;

		#endregion

		#region Properties

		protected override bool FirstScreen => true;

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_invisibleButton.onClick.AddListener(() => TransitionToScreen(MainMenuScreen.Instance));
			_titleScreenTimeline.stopped += TimelineStopped;
		}

		#endregion

		#region Protected methods

		protected override void Hide(Action action)
		{
			_titleScreenTimelineFinishedAction = action;
			_titleScreenTimeline.Play();
			_titleScreenClosedAudio.Play();
			_titleScreenAudio.Stop();
		}

		#endregion

		#region Private methods

		private void TimelineStopped(PlayableDirector obj)
		{
			base.Hide(_titleScreenTimelineFinishedAction);
			_mainMenuAudio.Play();
		}

		#endregion

		#region IMainMenuScreenBase Members

		public override void Show()
		{
			base.Show();
			_titleScreenTimeline.Stop();
			_titleScreenAudio.Play();
		}

		#endregion
	}
}