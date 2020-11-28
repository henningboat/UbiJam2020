using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Runtime.GameSurface
{
    [BurstCompile]
    public struct ValidateAreaJob : IJob
    {
        [ReadOnly] public NativeArray<Vector2Int> ConnectedPiecesKernel;
        public NativeQueue<Vector2Int> PositionsToValidate;
        public NativeArray<SurfacePiece> Surface;
        public int Resolution;
        public int Timestamp;
        public NativeArray<Color32> GameSurfaceTex;
        public NativeArray<bool> DidCutNewSurface;

        public void Execute()
        {
            var ColorClear = new Color32(0, 0, 0, 0);
            var ColorSolid = new Color32(255, 255, 255, 255);

            for (var x = 0; x < Resolution; x++)
            for (var y = 0; y < Resolution; y++)
            {
                var surfacePiece = GetSurfacePiece(x, y);
                if (surfacePiece.State == SurfaceState.Permanent)
                {
                    PositionsToValidate.Enqueue(surfacePiece.Position);
                }
            }

            while (PositionsToValidate.Count > 0) ValidateAllConnectedSurfaces(PositionsToValidate.Dequeue());

            bool anyNewDestroyedNodes = false;
            
            for (var x = 0; x < Resolution; x++)
            for (var y = 0; y < Resolution; y++)
            {
                var node = GetSurfacePiece(x, y);
                if (node.IsInvalid(Timestamp) && node.State!=SurfaceState.Destroyed)
                {
                    anyNewDestroyedNodes = true;
                        
                    SetNodeAtPosition(x, y, node.DestroyPiece());
                    GameSurfaceTex[x + y * Resolution] = ColorClear;
                }
                else
                {
                    switch (node.State)
                    {
                        case SurfaceState.Intact:
                        case SurfaceState.Permanent:
                            GameSurfaceTex[x + y * Resolution] = ColorSolid;
                            break;
                        case SurfaceState.Border:
                        case SurfaceState.Destroyed:
                            GameSurfaceTex[x + y * Resolution] = ColorClear;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            DidCutNewSurface[0] = anyNewDestroyedNodes;
        }

        private void ValidateAllConnectedSurfaces(Vector2Int basePosition)
        {
            var node = GetSurfacePiece(basePosition.x, basePosition.y);
            if (node.IsInvalid(Timestamp))
            {
                SetNodeAtPosition(basePosition.x, basePosition.y, node.Validate(Timestamp));

                if (node.State != SurfaceState.Border)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var offset = ConnectedPiecesKernel[i];
                        var connectionPosition = basePosition + offset;
                        if (InsideSurface(connectionPosition))
                        {
                            var connection = GetSurfacePiece(connectionPosition.x, connectionPosition.y);
                            if (connection.State != SurfaceState.Destroyed && connection.IsInvalid(Timestamp))
                            {
                                PositionsToValidate.Enqueue(connectionPosition);
                            }
                        }
                    }
                }
            }
        }

        private void SetNodeAtPosition(int positionX, int positionY, SurfacePiece surfacePiece)
        {
            Surface[positionX + positionY * Resolution] = surfacePiece;
        }

        public bool InsideSurface(Vector2Int position)
        {
            return position.x >= 0 && position.x < Resolution &&
                   position.y >= 0 && position.y < Resolution;
        }

        private SurfacePiece GetSurfacePiece(int x, int y)
        {
            return Surface[x + y * Resolution];
        }
    }
}