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
        public Vector2Int Position { get; private set; }

        public int ValidAtTimestamp { get; private set; }

        //todo inverted bool, fix
        public bool IsInvalid => ValidAtTimestamp < GameSurface.Instance.CurrentTimestamp;

        public SurfacePiece Cut()
        {
            State = SurfaceState.Border;
            return this;
        }

        public void DestroyPiece()
        {
            State = SurfaceState.Destroyed;
        }

        public void Validate()
        {
            ValidAtTimestamp = GameSurface.Instance.CurrentTimestamp;
        }
    }
}