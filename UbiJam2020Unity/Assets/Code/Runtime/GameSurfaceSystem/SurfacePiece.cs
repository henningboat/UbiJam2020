using Unity.Mathematics;

namespace Runtime.GameSurfaceSystem
{
	public struct SurfacePiece
	{
		#region Properties

		public SurfaceState State { get;  set; }
		public int ValidAtTimestamp { get; private set; }

		#endregion

		#region Constructors

		public SurfacePiece(SurfaceState state = SurfaceState.Intact)
		{
			State = state;
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