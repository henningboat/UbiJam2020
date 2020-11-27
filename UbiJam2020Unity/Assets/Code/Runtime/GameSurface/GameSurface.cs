using System;
using System.Collections;
using System.Collections.Generic;
using Runtime.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.XR.WSA;
using Unity.Collections;

namespace Runtime.GameSurface
{
    public class GameSurface : Singleton<GameSurface>
    {
        public int CurrentTimestamp { get; private set; }

        [SerializeField] private int _resolution = 10;
        [SerializeField] private Vector2Int _startPoint = new Vector2Int(4, 4);


        private Queue<SurfacePiece> PiecesToValidate;
        private NativeArray<SurfacePiece> _surface;

        private NativeArray<Vector2Int> _connectedPiecesKernel;

        protected override void Awake()
        {
            base.Awake();
            PiecesToValidate = new Queue<SurfacePiece>();
            _connectedPiecesKernel = new NativeArray<Vector2Int>(4, Allocator.Persistent);

            _surface = new NativeArray<SurfacePiece>(_resolution * _resolution, Allocator.Persistent);
            for (var x = 0; x < _resolution; x++)
            {
                for (var y = 0; y < _resolution; y++)
                {
                    var position = new Vector2Int(x, y);
                    SurfaceState surfaceState = position == _startPoint ? SurfaceState.Permanent : SurfaceState.Intact;
                    _surface[x + y * _resolution] = new SurfacePiece(position, surfaceState);
                }
            }
        }

        private void LateUpdate()
        {
            ValidateAreaJob validateAreaJob = new ValidateAreaJob()
            {
                Resolution = _resolution,
                Surface = _surface,
                ConnectedPiecesKernel = _connectedPiecesKernel,
                PositionsToValidate = new NativeQueue<Vector2Int>(Allocator.Temp),
            };
           validateAreaJob.Execute();
           
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _connectedPiecesKernel.Dispose();
        }


        private int GetIndexAtPosition(Vector2Int connectionPosition)
        {
            return connectionPosition.x + _resolution * connectionPosition.y;
        }

        public bool InsideSurface(Vector2Int position)
        {
            return position.x >= 0 && position.x < _resolution &&
                   position.y >= 0 && position.y < _resolution;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            for (var x = 0; x < _resolution; x++)
            {
                for (var y = 0; y < _resolution; y++)
                {
                    var surfaceState = _surface[x + y * _resolution].State;
                    if (surfaceState != SurfaceState.Destroyed)
                    {
                        switch (surfaceState)
                        {
                            case SurfaceState.Intact:
                                Gizmos.color = Color.gray;
                                break;
                            case SurfaceState.Border:
                                Gizmos.color = Color.red;
                                break;
                            case SurfaceState.Permanent:
                                Gizmos.color = Color.white;
                                break;
                        }

                        Gizmos.DrawCube(new Vector3(x, y), Vector3.one);
                    }
                }
            }
        }

        public void Cut(Vector2 position)
        {
            Cut(new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y)));
        }

        public void Cut(Vector2Int position)
        {
            if (InsideSurface(position))
            {
                var indexAtPosition = GetIndexAtPosition(position);
                _surface[indexAtPosition] = _surface[indexAtPosition].Cut();
            }
        }
    }

    public struct ValidateAreaJob:IJob
    {
        [ReadOnly]public NativeArray<Vector2Int> ConnectedPiecesKernel;
        [DeallocateOnJobCompletion] public NativeQueue<Vector2Int> PositionsToValidate;
        public NativeArray<SurfacePiece> Surface;
        public int Resolution;
        
        public void Execute()
        {
            for (var x = 0; x < Resolution; x++)
            {
                for (var y = 0; y < Resolution; y++)
                {
                    var surfacePiece = GetSurfacePiece(x,y);
                    if (surfacePiece.State == SurfaceState.Permanent)
                    {
                        PositionsToValidate.Enqueue(surfacePiece.Position);
                    }
                }
            }

            while (PositionsToValidate.Count > 0)
            {
                ValidateAllConnectedSurfaces(PositionsToValidate.Dequeue());
            }

            for (var x = 0; x < Resolution; x++)
            {
                for (var y = 0; y < Resolution; y++)
                {
                    var node = GetSurfacePiece(x, y);
                    if (node.IsInvalid)
                    {
                        node.DestroyPiece();
                    }
                }
            }
        }
        
        private void ValidateAllConnectedSurfaces(Vector2Int basePosition)
        {
            var node = GetSurfacePiece(basePosition.x, basePosition.y);
            if (node.IsInvalid)
            {
                node.Validate();

                if (node.State != SurfaceState.Border)
                {
                    foreach (var offset in ConnectedPiecesKernel)
                    {
                        var connectionPosition = basePosition + offset;
                        if (InsideSurface(connectionPosition))
                        {
                            var connection = GetSurfacePiece(connectionPosition.x,connectionPosition.y);
                            if (connection.State != SurfaceState.Destroyed && connection.IsInvalid)
                            {
                                PositionsToValidate.Enqueue(connectionPosition);
                            }
                        }
                    }
                }
            }
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