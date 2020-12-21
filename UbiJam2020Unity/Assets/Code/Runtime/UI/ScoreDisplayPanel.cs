using System.Collections;
using DG.Tweening;
using Runtime.Data;
using Runtime.GameSystem;
using Runtime.PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
	public class ScoreDisplayPanel : MonoBehaviour
	{
		#region Serialize Fields

		[SerializeField,] private PlayerIdentifier _playerIdentifier;
		[SerializeField,] private Transform _scoreIconLayoutGroup;
		[SerializeField,] private Transform _backgroundIconLayoutGroup;
		[SerializeField,] private GameObject _scoreIconPrefab;
		[SerializeField,] private GameObject _backgroundIconPrefab;
		[SerializeField,] private Image _characterImage;
		[SerializeField,] private Image _patchImage;

		#endregion

		#region Private Fields

		private Player _playerInstance;
		private PlayerIdentifier _identifier;
		private PlayerType _playerType;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			//todo
			// for (int i = 0; i < GameManager.Score[_playerIdentifier]; i++)
			// {
			// 	Instantiate(_scoreIconPrefab, _scoreIconLayoutGroup);
			// }
			//
			// for (int i = 0; i < GameSettings.Instance.RoundsToWin; i++)
			// {
			// 	Instantiate(_backgroundIconPrefab, _backgroundIconLayoutGroup);
			// }

			GameManager.Instance.OnVictory += OnPlayerWon;

			_characterImage.color = new Color(1, 1, 1, 0);

			yield return null;

			//_playerInstance = GameManager.Instance.Players[_playerIdentifier];
			//_characterImage.sprite = _playerInstance.PlayerIcon;
			_characterImage.DOFade(1, 0.1f);
		}

		private void Update()
		{
			if (_playerInstance != null)
			{
				_patchImage.enabled = _playerInstance.HasPatch;
			}
		}

		#endregion

		#region Public methods

		public void Initialize(PlayerIdentifier identifier, PlayerType playerType)
		{
			_playerType = playerType;
			_identifier = identifier;
		}

		#endregion

		#region Private methods

		private void OnPlayerWon(int playerID)
		{
			//todo
			// if (playerID == _playerIdentifier)
			// {
			// 	GameObject instantiate = Instantiate(_scoreIconPrefab, _scoreIconLayoutGroup);
			// 	instantiate.transform.localScale = Vector3.zero;
			// 	instantiate.transform.DOScale(1, 0.3f);
			// }
		}

		#endregion
	}
}