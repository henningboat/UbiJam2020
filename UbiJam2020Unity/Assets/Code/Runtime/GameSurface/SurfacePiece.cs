using UnityEngine;

namespace Runtime.GameSurface
{
	public struct SurfacePiece
	{
		#region Properties

		public Vector2Int Position { get; }
		public SurfaceState State { get; private set; }
		public int ValidAtTimestamp { get; private set; }

		#endregion

		#region Constructors

		public SurfacePiece(Vector2Int position, SurfaceState state = SurfaceState.Intact)
		{
			State = state;
			Position = position;
			ValidAtTimestamp = 0;
		}

		#endregion

		#region Public methods

		//todo inverted bool, fix 
		public bool IsInvalid(int timestamp)
		{
			return ValidAtTimestamp < timestamp;
		}

		public SurfacePiece Cut()
		{
			if (State == SurfaceState.Intact)
			{
				State = SurfaceState.Border;
			}

			return this;
		}

		public SurfacePiece DestroyPiece()
		{
			State = SurfaceState.Destroyed;
			return this;
		}

		public SurfacePiece Validate(int timestamp)
		{
			ValidAtTimestamp = timestamp;
			return this;
		}

		#endregion
	}
}