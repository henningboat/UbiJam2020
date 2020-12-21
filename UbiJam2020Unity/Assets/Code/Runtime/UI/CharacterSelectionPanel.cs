using System;
using System.Collections.Generic;
using DG.Tweening;
using Runtime.GameSystem;
using Runtime.InputSystem;
using Runtime.PlayerSystem;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Runtime.UI
{
	public class CharacterSelectionPanel : MonoBehaviour
	{
		#region Serialize Fields

		[SerializeField,] private int _playerID;
		[SerializeField,] private Image _mainImage;
		[SerializeField,] private Image _leftArrow;
		[SerializeField,] private Image _rightArrow;
		[SerializeField,] private AudioSource _leftArrowAudioSource;
		[SerializeField,] private AudioSource _rightArrowAudioSource;
		[SerializeField,] private AudioSource _characterVoice;
		[SerializeField,] private float _scaleOutSize = 0.8f;
		[SerializeField,] private Color _scaleOutColor = Color.gray;

		#endregion

		#region Private Fields

		private bool _locked;
		private float _nextInputAllowedTime;
		private List<Player> _allPlayers;
		private int _currentSelection;

		#endregion

		#region Properties

		public bool SelectionDone { get; private set; }
		public PlayerType SelectedCharacter { get; private set; }

		#endregion

		#region Unity methods

		private void Start()
		{
			_allPlayers = GameSettings.Instance.GetAllPlayerPrefabs();
			_currentSelection = Random.Range(0, _allPlayers.Count);
			UpdateSelection();
		}

		private void OnEnable()
		{
			_nextInputAllowedTime = Time.time + 0.5f;
			SelectionDone = false;
		}

		private void Update()
		{
			if (Time.time - _nextInputAllowedTime > 0)
			{
				PlayerInput input = PlayerInputManager.Instance.GetInputForPlayer(_playerID);

				if (input.Eat)
				{
					_nextInputAllowedTime = float.MaxValue;
					Player character = _allPlayers[_currentSelection];
					SelectedCharacter = character.PlayerType;
					SelectionDone = true;
					_leftArrow.enabled = false;
					_rightArrow.enabled = false;
					transform.DOScale(_scaleOutSize, 0.2f);
					_mainImage.DOColor(_scaleOutColor, 0.2f);
					_characterVoice.clip = character.SelectionAudioClip;
					_characterVoice.Play();
				}
				else if (input.DirectionalInput.x > 0.8f)
				{
					_currentSelection++;
					UpdateSelection();
					_rightArrow.transform.DOScale(0.5f, 0.1f).SetEase(Ease.OutQuad).OnComplete(() => _rightArrow.transform.DOScale(1, 0.1f).SetEase(Ease.InQuad));
					_rightArrowAudioSource.Play();
				}
				else if (input.DirectionalInput.x < -0.8f)
				{
					_currentSelection--;
					UpdateSelection();
					_leftArrow.transform.DOScale(0.5f, 0.1f).SetEase(Ease.OutQuad).OnComplete(() => _leftArrow.transform.DOScale(1, 0.1f).SetEase(Ease.InQuad));
					_leftArrowAudioSource.Play();
				}
			}
		}

		#endregion

		#region Private methods

		private void UpdateSelection()
		{
			_currentSelection = (_currentSelection + _allPlayers.Count) % _allPlayers.Count;
			_mainImage.sprite = _allPlayers[_currentSelection].CharacterSelectionSprite;
			_nextInputAllowedTime = Time.time + 0.2f;
		}

		#endregion
	}
}