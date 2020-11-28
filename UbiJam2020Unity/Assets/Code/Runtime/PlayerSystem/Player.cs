using System;
using Runtime.GameSurface;
using Runtime.GameSystem;
using Runtime.InputSystem;
using UnityEngine;

namespace Runtime.PlayerSystem
{
	public class Player : StateMachineBase<PlayerState>
	{
		#region Serialize Fields

		[SerializeField,] private PlayerType _playerType;
		[SerializeField,] private float _forwardSpeed = 2;
		[SerializeField,] private float _rotationSpeed = 100;
		[SerializeField,] private bool _allowSliding;

		#endregion

		#region Private Fields

		private float _velocity;
		private Vector2 _lastFramePosition;
		private int _playerID;

		#endregion

		#region Properties

		protected override PlayerState InitialState => PlayerState.Alive;

		#endregion

		#region Unity methods

		protected override void Update()
		{
			base.Update();

			var input = PlayerInputManager.Instance.GetInputForPlayer(_playerID);

			switch (State)
			{
				case PlayerState.Alive:
					transform.Rotate(0, 0, input.DirectionalInput * _rotationSpeed * Time.deltaTime);

					TryTranslate(transform.up * Time.deltaTime * _forwardSpeed);

					if (input.Eat)
					{
						GameSurface.GameSurface.Instance.Cut(transform.position, _lastFramePosition);
					}

					break;
				case PlayerState.Dead:
					float deltaTime = Time.deltaTime;
					_velocity -= GameSettings.Instance.Gravity * deltaTime;
					transform.position += deltaTime * _velocity * Vector3.up;
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

		#endregion

		#region Protected methods

		protected override PlayerState GetNextState()
		{
			switch (State)
			{
				case PlayerState.Alive:
					if (GameSurface.GameSurface.Instance.GetNodeAtPosition(transform.position).State == SurfaceState.Destroyed)
					{
						return PlayerState.Dead;
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

				if (didMove == false)
				{
					return;
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