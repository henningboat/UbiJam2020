using System;
using System.Collections;
using System.Linq;
using Photon.Pun;
using Runtime.GameSurfaceState;
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
		[SerializeField,] private bool _allowSliding;
		[SerializeField,] private AudioSource _eatSound;
		[SerializeField,] private Sprite _characterSelectionSprite;
		[SerializeField,] private AudioClip _selectionAudioClip;
		[SerializeField,] private Sprite _playerIcon;
		[SerializeField,] private Sprite[] _victorySprites;

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

		#endregion

		#region Properties

		public Sprite CharacterSelectionSprite => _characterSelectionSprite;
		protected override PlayerState InitialState => PlayerState.Alive;
		public Sprite PlayerIcon => _playerIcon;
		public PlayerType PlayerType => _playerType;
		public AudioClip SelectionAudioClip => _selectionAudioClip;
		public Sprite[] VictorySprites => _victorySprites;
		public bool HasPatch { get; set; }
		public int PlayerID { get; private set; }

		#endregion

		#region Unity methods

		private void Awake()
		{
			_photonView = GetComponent<PhotonView>();
			PlayerID = _photonView.Owner.ActorNumber - 1;
			Debug.Log(PlayerID);
		}

		private void Start()
		{
			_heading = transform.up;
			GameManager.Instance.Register(this);
		}

		protected override void Update()
		{
			if (!_photonView.IsMine)
			{
				return;
			}

			base.Update();

			PlayerInput input = PlayerInputManager.Instance.GetInputForPlayer(PlayerID);

			switch (State)
			{
				case PlayerState.Alive:
					if (GameManager.Instance.State == GameState.Active)
					{
						if (input.DirectionalInput.magnitude > Mathf.Epsilon)
						{
							_heading = input.DirectionalInput.normalized;
						}

						transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Vector3.forward, _heading), _rotationSpeed * Time.deltaTime);

						float speed;
						if (input.DirectionalInput.magnitude > Mathf.Epsilon)
						{
							speed = Mathf.Lerp(_directionAdjustmentSpeed, _forwardSpeed, Vector2.Dot(transform.up, _heading));
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

						TryTranslate(transform.up * (Time.deltaTime * speed));

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

		private IEnumerator Delayed(Action a)
		{
			yield return new WaitForSeconds(1);
			a.Invoke();
		}

		private bool TrySafePlayerFromFalling()
		{
			Vector2? closestSafePosition=null;
			float closestSafePositionDistance=float.MaxValue;
				
			for (int x = -PixelTollelranceWhenFalling; x < PixelTollelranceWhenFalling + 1; x++)
			for (int y = -PixelTollelranceWhenFalling; y < PixelTollelranceWhenFalling + 1; y++)
			{
				Vector2Int potentialSafePosition = _lastNodePosition + new Vector2Int(x, y);
				if (GameSurface.Instance.IsWalkableAtGridPosition(potentialSafePosition))
				{
					Vector2 safePositionWS= GameSurface.Instance.GridPositionToWorldPosition(potentialSafePosition);
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

			bool? invertMinor = null;

			while (distanceToTravel > 0)
			{
				float currentStepSize = Mathf.Min(stepSize, distanceToTravel);

				bool didMove = false;

				Vector2 offset = direction * currentStepSize;

				Vector2 absOffset = new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
				bool mayorIsHorizontal = absOffset.x > absOffset.y;

				Vector2 mayorDirection = (mayorIsHorizontal ? Vector2.right : Vector2.up) * offset;
				Vector2 minorDirection = (mayorIsHorizontal ? Vector2.up : Vector2.right) * offset;

				TryTranslateStep(mayorDirection, ref distanceToTravel, ref didMove);

				if (!_allowSliding)
				{
					invertMinor = false;
				}

				if (Mathf.Abs(minorDirection.magnitude) > 0.0001f)
				{
					if (!invertMinor.HasValue)
					{
						invertMinor = TryTranslateStep(minorDirection, ref distanceToTravel, ref didMove) == false;

						if (invertMinor.Value)
						{
							TryTranslateStep(-minorDirection, ref distanceToTravel, ref didMove);
						}
					}
					else
					{
						if (invertMinor.Value)
						{
							minorDirection *= -1;
						}

						TryTranslateStep(minorDirection, ref distanceToTravel, ref didMove);
					}
				}

				if (didMove == false)
				{
					break;
				}
			}
		}

		private bool TryTranslateStep(Vector2 offset, ref float distanceToTravel, ref bool didMove)
		{
			if (Math.Abs(offset.magnitude) < Mathf.Epsilon)
			{
				return false;
			}

			bool walkable = GameSurface.Instance.IsWalkableAtWorldPosition(transform.position + (Vector3) offset);
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

		#endregion
	}

	public enum PlayerState
	{
		Alive,
		Dead,
	}
}