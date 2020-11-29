using System;
using Runtime.GameSurface;
using Runtime.GameSystem;
using Runtime.InputSystem;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime.PlayerSystem
{
	public class Player : StateMachineBase<PlayerState>
	{
		#region Serialize Fields

		[SerializeField,] private PlayerType _playerType;
		[SerializeField,] private float _forwardSpeed = 2;
		[SerializeField,] private float _directionAdjustmentSpeed = 2;
		[SerializeField,] private float _rotationSpeed = 100;
		[SerializeField,] private bool _allowSliding;
		[SerializeField,] private AudioSource _eatSound;

		#endregion

		#region Private Fields

		private bool _hasPatch;
		private float _velocity;
		private Vector2 _lastFramePosition;
		private int _playerID;
		private Vector2 _heading;
		private float _speedMultiplier;
		private float _speedMultiplierEndTime = float.MinValue;

		#endregion

		#region Properties

		protected override PlayerState InitialState => PlayerState.Alive;

		#endregion

		#region Unity methods

		private void Start()
		{
			_heading = transform.up;
		}

		protected override void Update()
		{
			base.Update();

			var input = PlayerInputManager.Instance.GetInputForPlayer(_playerID);

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

						TryTranslate(transform.up * (Time.deltaTime * speed));

						if (input.Eat)
						{
							GameSurface.GameSurface.Instance.Cut(transform.position, _lastFramePosition);
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

			_lastFramePosition = transform.position;
		}

		#endregion

		#region Public methods

		public void SetPlayerID(int i)
		{
			_playerID = i;
		}

		public void SetSpeedMultiplier(float speedMultiplier, float duration)
		{
			_speedMultiplier = speedMultiplier;
			_speedMultiplierEndTime = Time.time + duration;
		}

		public void GivePatch()
		{
			_hasPatch = true;
		}

		#endregion

		#region Protected methods

		protected override PlayerState GetNextState()
		{
			switch (State)
			{
				case PlayerState.Alive:
					if (GameSurface.GameSurface.Instance.GetNodeAtPosition(transform.position).State == SurfaceState.Destroyed)
					{
						if (_hasPatch)
						{
							GameSurface.GameSurface.Instance.SpawnPatch(transform.position);
							_hasPatch = false;
						}
						else
						{
							return PlayerState.Dead;
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

		private void TryTranslate(Vector2 positionDelta)
		{
			var direction = positionDelta.normalized;
			var distanceToTravel = positionDelta.magnitude;
			var stepSize = GameSurface.GameSurface.Instance.WorldSpaceGridNodeSize;

			bool? invertMinor = null;

			while (distanceToTravel > 0)
			{
				var currentStepSize = Mathf.Min(stepSize, distanceToTravel);

				var didMove = false;

				var offset = direction * currentStepSize;

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

			var node = GameSurface.GameSurface.Instance.GetNodeAtPosition((Vector2) transform.position + offset);
			if (node.State == SurfaceState.Destroyed)
			{
				return false;
			}

			transform.position += (Vector3) offset;
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