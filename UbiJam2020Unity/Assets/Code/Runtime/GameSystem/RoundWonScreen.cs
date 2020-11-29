using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
		[SerializeField] private List<Image> _victoryScreenImages;
		[SerializeField] private PlayableDirector _victoryPlayableDirector;
		[SerializeField] private AudioSource _music;

		#endregion

		#region Public methods

		public void ShowKOScreen()
		{
			StartCoroutine(ShowKOScreenRoutine());
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
			SceneManager.LoadScene(1);
		}

		#endregion

		public void ShowVictoryScreen()
		{
			StartCoroutine(ShowVictoryScreenCoroutine());
		}

		private IEnumerator ShowVictoryScreenCoroutine()
		{
			_music.Stop();
			Player player = null;
			if (GameManager.Instance.TryGetWinningPlayer(out player))
			{
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
			SceneManager.LoadScene(0);
		}
	}
}