using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using Runtime.PlayerSystem;
using Runtime.Utils;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Runtime.GameSystem
{
	public class RoundWonScreen : Singleton<RoundWonScreen>
	{
		#region Serialize Fields

		[SerializeField,] private AudioSource _koAudio;
		[SerializeField,] private float _zoomIn = 2;
		[SerializeField,] private List<Image> _victoryScreenImages;
		[SerializeField,] private PlayableDirector _victoryPlayableDirector;
		[SerializeField,] private AudioSource _music;
		[SerializeField,] private Image _playerVictory;
		[SerializeField,] private Sprite _player0Sprite;
		[SerializeField,] private Sprite _player1Sprite;

		#endregion

		#region Public methods

		public void ShowKOScreen()
		{
			StartCoroutine(ShowKOScreenRoutine());
		}

		public void ShowVictoryScreen()
		{
			StartCoroutine(ShowVictoryScreenCoroutine());
		}

		#endregion

		#region Private methods

		private IEnumerator ShowKOScreenRoutine()
		{
			yield return new WaitForSeconds(0.2f);
			_koAudio.Play();
			if (GameManager.Instance.TryGetDeadPlayer(out Player player))
			{
				Vector3 targetPosition = player.transform.position;
				targetPosition.z = Camera.main.transform.position.z + _zoomIn;
				Camera.main.transform.DOMove(targetPosition, 1);
			}

			yield return new WaitForSeconds(2);

			IsDone = true;
		}

		private IEnumerator ShowVictoryScreenCoroutine()
		{
			_music.Stop();
			Player player = null;
			if (GameManager.Instance.TryGetWinningPlayer(out player))
			{
				_playerVictory.sprite = player.PlayerID == 0 ? _player0Sprite : _player1Sprite;

				Vector3 targetPosition = player.transform.position;
				targetPosition.z = Camera.main.transform.position.z + _zoomIn;
				Camera.main.transform.DOMove(targetPosition, 1);

				for (int i = 0; i < _victoryScreenImages.Count; i++)
				{
					_victoryScreenImages[i].sprite = player.VictorySprites[i];
				}

				_victoryPlayableDirector.Play();

				yield return new WaitForSeconds(5);
			}

			IsDone = true;
		}

		public bool IsDone { get; private set; }

		#endregion
	}
}