﻿using System;
using System.Collections;
using System.Linq;
using Photon.Pun;
using Runtime.Data;
using Runtime.GameSurfaceSystem;
using Runtime.GameSystem;
using Runtime.InputSystem;
using UnityEngine;

namespace Runtime.PlayerSystem
{
	public class Player : PhotonStateMachine<PlayerState>
	{
		#region Static Stuff

		private const float CutDuration = 0.1f;
		private const int PixelTollelranceWhenFalling = 4;

		public static Player GetFromPhotonPlayer(Photon.Realtime.Player photonPlayer)
		{
			return GameManager.Instance.Players.FirstOrDefault(player => player.PhotonView.Owner.ActorNumber == photonPlayer.ActorNumber);
		}

		#endregion

		#region Serialize Fields

		[SerializeField,] private PlayerType _playerType;
		[SerializeField,] private float _forwardSpeed = 2;
		[SerializeField,] private float _directionAdjustmentSpeed = 2;
		[SerializeField,] private float _rotationSpeed = 100;
		[SerializeField,] private AudioSource _eatSound;
		[SerializeField,] private Sprite _characterSelectionSprite;
		[SerializeField,] private AudioClip _selectionAudioClip;
		[SerializeField,] private Sprite _playerIcon;
		[SerializeField,] private Sprite[] _victorySprites;

		#endregion

		#region Public Fields

		public string _displayName;

		#endregion

		#region Private Fields

		private float _velocity;
		private Vector2 _heading;
		private float _speedMultiplier;
		private float _speedMultiplierEndTime = float.MinValue;
		private Vector3? _cutStartPosition;
		private float _cutStartTime;
		private PhotonView _photonView;
		private Vector2Int _lastNodePosition;
		private Vector2Int _currentNodePosition;
		private Vector3 _lastFramePosition;

		#endregion

		#region Properties

		public Sprite CharacterSelectionSprite => _characterSelectionSprite;
		protected override PlayerState InitialState => PlayerState.Alive;
		public int LocalPlayerID { get; }
		public Sprite PlayerIcon => _playerIcon;
		public PlayerType PlayerType => _playerType;
		public AudioClip SelectionAudioClip => _selectionAudioClip;
		public Sprite[] VictorySprites => _victorySprites;
		public bool HasPatch { get; set; }
		public PlayerIdentifier PlayerIdentifier { get; private set; }

		#endregion

		#region Unity methods

		private void Awake()
		{
			_photonView = GetComponent<PhotonView>();
		}

		private void Start()
		{
			_heading = transform.up;
			PlayerIdentifier = _photonView.InstantiationData[0] as PlayerIdentifier;
			GameManager.Instance.RegisterPlayer(this);
			_displayName = GameManager.Instance.GetDisplayNameForPlayer(PlayerIdentifier);
		}

		protected override void Update()
		{
			//Debug.DrawRay(transform.position, GetNormalAtWorldPosition(transform.position), Color.red);
			UpdateRotation();
			if (!_photonView.IsMine)
			{
				return;
			}

			base.Update();

			PlayerInput input = PlayerInputManager.Instance.GetInputForPlayer(LocalPlayerID);

			switch (State)
			{
				case PlayerState.Alive:
					if (GameManager.Instance.State == GameState.Active)
					{
						if (input.DirectionalInput.magnitude > Mathf.Epsilon)
						{
							_heading = input.DirectionalInput.normalized;
						}

						float speed;
						if (input.DirectionalInput.magnitude > Mathf.Epsilon)
						{
							speed = _forwardSpeed;
						}
						else
						{
							speed = 0;
						}

						if (Time.time < _speedMultiplierEndTime)
						{
							speed *= _speedMultiplier;
						}

						if (input.Eat && (_cutStartPosition == null))
						{
							_cutStartPosition = transform.position;
							_cutStartTime = Time.time;
						}

						TryTranslate(_heading * (Time.deltaTime * speed));

						if (_cutStartPosition != null)
						{
							if ((input.Eat == false) || (Time.time - _cutStartTime > CutDuration))
							{
								Vector3 cutStartPosition = _cutStartPosition.Value;
								Vector3 transformPosition = transform.position;
								GameSurface.Instance.Cut(cutStartPosition, transformPosition);
								_cutStartPosition = null;
							}
						}

						if (input.Eat != _eatSound.isPlaying)
						{
							if (input.Eat)
							{
								_eatSound.Play();
							}
							else
							{
								_eatSound.Pause();
							}
						}
					}

					break;
				case PlayerState.Dead:
					float deltaTime = Time.deltaTime;
					_velocity += GameSettings.Instance.Gravity * deltaTime;
					transform.position += deltaTime * _velocity * Vector3.forward;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		#region Public methods

		public void SetSpeedMultiplier(float speedMultiplier, float duration)
		{
			_speedMultiplier = speedMultiplier;
			_speedMultiplierEndTime = Time.time + duration;
		}

		public void GivePatch()
		{
			HasPatch = true;
		}

		#endregion

		#region Protected methods

		protected override byte StateToByte(PlayerState state)
		{
			return (byte) state;
		}

		protected override PlayerState ByteToState(byte state)
		{
			return (PlayerState) state;
		}

		[PunRPC,]
		protected override void RPCOnStateChange(byte oldStateByte, byte newStateByte)
		{
			base.RPCOnStateChange(oldStateByte, newStateByte);
		}

		protected override PlayerState GetNextState()
		{
			switch (State)
			{
				case PlayerState.Alive:
					if (GameSurface.Instance.IsWalkableAtWorldPosition(transform.position) == false)
					{
						if (!TrySafePlayerFromFalling())
						{
							if (HasPatch)
							{
								GameSurface.Instance.SpawnPatch(transform.position);
								HasPatch = false;
							}
							else
							{
								return PlayerState.Dead;
							}
						}
					}

					break;
				case PlayerState.Dead:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return State;
		}

		protected override void OnStateChange(PlayerState oldState, PlayerState newState)
		{
		}

		#endregion

		#region Private methods

		private void UpdateRotation()
		{
			if (State == PlayerState.Alive)
			{
				Vector2 positionDelta = transform.position - _lastFramePosition;
				if (positionDelta.magnitude > Mathf.Epsilon)
				{
					Quaternion newRotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Vector3.forward, positionDelta), _rotationSpeed * Time.deltaTime);
					transform.rotation = newRotation;
				}
			}

			_lastFramePosition = transform.position;
		}

		private IEnumerator Delayed(Action a)
		{
			yield return new WaitForSeconds(1);
			a.Invoke();
		}

		private bool TrySafePlayerFromFalling()
		{
			Vector2? closestSafePosition = null;
			float closestSafePositionDistance = float.MaxValue;

			for (int x = -PixelTollelranceWhenFalling; x < PixelTollelranceWhenFalling + 1; x++)
			for (int y = -PixelTollelranceWhenFalling; y < PixelTollelranceWhenFalling + 1; y++)
			{
				Vector2Int potentialSafePosition = _lastNodePosition + new Vector2Int(x, y);
				if (GameSurface.Instance.IsWalkableAtGridPosition(potentialSafePosition))
				{
					Vector2 safePositionWS = GameSurface.GridPositionToWorldPosition(potentialSafePosition);
					float distance = Vector2.Distance(transform.position, safePositionWS);
					if (distance < closestSafePositionDistance)
					{
						closestSafePosition = safePositionWS;
						closestSafePositionDistance = distance;
					}
				}
			}

			if (closestSafePosition.HasValue)
			{
				transform.position = closestSafePosition.Value;
				return true;
			}

			return false;
		}

		private void TryTranslate(Vector2 positionDelta)
		{
			Vector2 direction = positionDelta.normalized;
			float distanceToTravel = positionDelta.magnitude;
			float stepSize = GameSurface.Instance.WorldSpaceGridNodeSize;

			while (distanceToTravel > 0)
			{
				float currentStepSize = Mathf.Min(stepSize, distanceToTravel);

				bool didMove = false;

				Vector2 offset = direction * currentStepSize;

				didMove = TryTranslateStep(offset, ref distanceToTravel);

				if (!didMove)
				{
					Vector2 wallNormal = GetNormalAtWorldPosition(transform.position);
					if (Vector2.Angle(offset, wallNormal) < 170)
					{
						Vector2 reflectedOffset = Vector2.Reflect(offset, wallNormal);
						TryTranslateStep(reflectedOffset, ref distanceToTravel);
					}
				}

				if (didMove == false)
				{
					break;
				}
			}
		}

		private bool TryTranslateStep(Vector2 offset, ref float distanceToTravel)
		{
			Vector2 absOffset = new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
			bool mayorIsHorizontal = absOffset.x > absOffset.y;

			Vector2 mayorDirection = (mayorIsHorizontal ? Vector2.right : Vector2.up) * offset;
			Vector2 minorDirection = (mayorIsHorizontal ? Vector2.up : Vector2.right) * offset;

			bool didMove = false;
			TryTranslateStep(mayorDirection, ref distanceToTravel, ref didMove);
			if (minorDirection.magnitude > 0.000001f)
			{
				TryTranslateStep(minorDirection, ref distanceToTravel, ref didMove);
			}

			return didMove;
		}

		private bool TryTranslateStep(Vector2 offset, ref float distanceToTravel, ref bool didMove)
		{
			//
			// const float epsilon = (float) 1e-8;
			// if (Math.Abs(offset.magnitude) < epsilon)
			// {
			// 	return false;
			// }

			Vector3 targetPosition = transform.position + (Vector3) offset;
			bool walkable = GameSurface.Instance.IsWalkableAtWorldPosition(targetPosition);
			if (!walkable)
			{
				return false;
			}

			transform.position += (Vector3) offset;

			Vector2Int newPosition = GameSurface.WorldSpaceToGrid(transform.position);
			if (newPosition != _currentNodePosition)
			{
				_lastNodePosition = _currentNodePosition;
			}

			_currentNodePosition = newPosition;

			didMove = true;
			distanceToTravel -= offset.magnitude;
			return true;
		}

		private Vector2 GetNormalAtWorldPosition(Vector2 targetPosition)
		{
			int sampleWidth = 4;

			Vector2 wallNormal = Vector2.zero;
			Vector2Int gridPosition = GameSurface.WorldSpaceToGrid(targetPosition);

			for (int x = -sampleWidth; x < sampleWidth + 1; x++)
			for (int y = -sampleWidth; y < sampleWidth + 1; y++)
			{
				if ((x == 0) && (y == 0))
				{
					continue;
				}

				Vector2Int gridOffset = new Vector2Int(x, y);

				if (GameSurface.Instance.IsWalkableAtGridPosition(gridPosition + gridOffset))
				{
					wallNormal += ((Vector2) gridOffset).normalized;
				}
			}

			return wallNormal.normalized;
		}

		#endregion
	}

	public enum PlayerState
	{
		Alive,
		Dead,
	}
}