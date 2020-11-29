using System.Collections.Generic;
using DG.Tweening;
using Runtime.GameSystem;
using Runtime.InputSystem;
using Runtime.PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
	public class CharacterSelectionScreen : MonoBehaviour
	{
		#region Serialize Fields

		[SerializeField,] private int _playerID;
		[SerializeField,] private Image _mainImage;
		[SerializeField,] private Image _leftArrow;
		[SerializeField,] private Image _rightArrow;
		[SerializeField,] private AudioSource _leftArrowAudioSource;
		[SerializeField,] private AudioSource _rightArrowAudioSource;
		
		[SerializeField,] private AudioSource _characterVoice;

		#endregion

		#region Private Fields

		private bool _locked;
		private float _lastInputTime;
		private List<Player> _allPlayers;
		private int _currentSelection;
		[SerializeField] private float _scaleOutSize=0.8f;
		[SerializeField] private Color _scaleOutColor=Color.gray;

		#endregion

		#region Properties

		public bool SelectionDone { get; private set; }

		#endregion

		#region Unity methods

		private void Start()
		{
			_allPlayers = GameSettings.Instance.GetAllPlayerPrefabs();
			_currentSelection = Random.Range(0, _allPlayers.Count);
			UpdateSelection();
		}

		void Update()
		{
			if ((MainMenuManager.Instance.State == MainMenuState.CharacterSelection) && (Time.time - _lastInputTime > 0.2f) && (Time.time-MainMenuManager.Instance.CharacterScreenOpenedTime)>1)
			{
				var input = PlayerInputManager.Instance.GetInputForPlayer(_playerID);

				if (input.Eat)
				{
					_lastInputTime = float.MaxValue;
					Player character = _allPlayers[_currentSelection];
					GameManager.SetCharacterSelection(_playerID,character.PlayerType);
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
			_lastInputTime = Time.time;
		}

		#endregion
	}
}