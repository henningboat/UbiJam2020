using UnityEngine;

namespace Runtime.GameSurface
{
    public struct SurfacePiece
    {
        public SurfacePiece(Vector2Int position, SurfaceState state = SurfaceState.Intact)
        {
            State = state;
            Position = position;
            ValidAtTimestamp = 0;
        }

        public SurfaceState State { get; private set; }
        public Vector2Int Position { get; }

        public int ValidAtTimestamp { get; private set; }

        //todo inverted bool, fix
        public bool IsInvalid(int timestamp)
        {
            return ValidAtTimestamp < timestamp;
        }

        public SurfacePiece Cut()
        {
            State = SurfaceState.Border;
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
    }
}