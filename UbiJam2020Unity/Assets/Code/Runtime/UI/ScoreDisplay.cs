using System;
using DG.Tweening;
using Runtime.GameSystem;
using UnityEngine;

namespace Runtime.UI
{
	public class ScoreDisplay : MonoBehaviour
	{
		#region Serialize Fields

		[SerializeField,] private int _playerID;
		[SerializeField,] private GameObject _scoreIconPrefab;

		#endregion

		#region Unity methods

		private void Start()
		{
			for (int i = 0; i < GameManager.Score[_playerID]; i++)
			{
				Instantiate(_scoreIconPrefab, transform);
			}

			GameManager.Instance.OnVictory += OnPlayerWon;
		}

		private void OnPlayerWon(int playerID)
		{
			if (playerID == _playerID)
			{
				GameObject instantiate = Instantiate(_scoreIconPrefab, transform);
				instantiate.transform.localScale = Vector3.zero;
				instantiate.transform.DOScale(1, 0.3f);
			}
		}

		#endregion
	}
}