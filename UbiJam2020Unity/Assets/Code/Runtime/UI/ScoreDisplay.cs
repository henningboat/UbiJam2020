using System;
using System.Collections;
using DG.Tweening;
using Runtime.GameSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
	public class ScoreDisplay : MonoBehaviour
	{
		#region Serialize Fields

		[SerializeField,] private int _playerID;
		[SerializeField,] private Transform _scoreIconLayoutGroup;
		[SerializeField,] private Transform _backgroundIconLayoutGroup;
		[SerializeField,] private GameObject _scoreIconPrefab;
		[SerializeField,] private GameObject _backgroundIconPrefab;
		[SerializeField,] private Image _characterImage;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			for (int i = 0; i < GameManager.Score[_playerID]; i++)
			{
				Instantiate(_scoreIconPrefab, _scoreIconLayoutGroup);
			}

			for (int i = 0; i < GameSettings.Instance.RoundsToWin; i++)
			{
				Instantiate(_backgroundIconPrefab, _backgroundIconLayoutGroup);
			}

			GameManager.Instance.OnVictory += OnPlayerWon;

			_characterImage.color = new Color(1, 1, 1, 0);

			yield return null;

			_characterImage.sprite = GameManager.Instance.Players[_playerID].PlayerIcon;
			_characterImage.DOFade(1, 0.1f);
		}

		#endregion

		#region Private methods

		private void OnPlayerWon(int playerID)
		{
			if (playerID == _playerID)
			{
				GameObject instantiate = Instantiate(_scoreIconPrefab, _scoreIconLayoutGroup);
				instantiate.transform.localScale = Vector3.zero;
				instantiate.transform.DOScale(1, 0.3f);
			}
		}

		#endregion
	}
}