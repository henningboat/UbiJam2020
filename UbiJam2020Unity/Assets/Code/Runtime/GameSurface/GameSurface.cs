using System;
using Runtime.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Runtime.GameSurface
{
    public class GameSurface : Singleton<GameSurface>
    {
        private NativeArray<Vector2Int> _connectedPiecesKernel;

        private Texture2D _gameSurfaceTex;

        [SerializeField] private int _resolution = 10;
        [SerializeField] private float _size;
        [SerializeField] private Vector2Int _startPoint = new Vector2Int(4, 4);
        [SerializeField] private Texture2D _gameSurfaceColorTexture;
        

        private NativeArray<SurfacePiece> _surface;
        [SerializeField] private FallingPiece _fallingPiecePrefab;
        public int CurrentTimestamp { get; private set; }
        public float WorldSpaceGridNodeSize { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            WorldSpaceGridNodeSize = (1f / _resolution) * _size;
                
            _connectedPiecesKernel = new NativeArray<Vector2Int>(4, Allocator.Persistent);
            _connectedPiecesKernel[0] = Vector2Int.up;
            _connectedPiecesKernel[1] = Vector2Int.down;
            _connectedPiecesKernel[2] = Vector2Int.left;
            _connectedPiecesKernel[3] = Vector2Int.right;

            _surface = new NativeArray<SurfacePiece>(_resolution * _resolution, Allocator.Persistent);

            _gameSurfaceTex = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
            gameObject.GetComponentInChildren<Renderer>().material.SetTexture("_Mask", _gameSurfaceTex);

            for (var x = 0; x < _resolution; x++)
            for (var y = 0; y < _resolution; y++)
            {
                var position = new Vector2Int(x, y);

                Vector2 uv = GridPositionToID(new Vector2Int(x, y));
                float gamefieldTexValue = _gameSurfaceColorTexture.GetPixelBilinear(uv.x,uv.y).a;
                
                SurfaceState surfaceState;
                if (gamefieldTexValue>0.5f)
                {
                    surfaceState = position == _startPoint ? SurfaceState.Permanent : SurfaceState.Intact;
                }
                else
                {
                    surfaceState = SurfaceState.Destroyed;
                }

                _surface[x + y * _resolution] = new SurfacePiece(position, surfaceState);
            }
        }

        public Vector2 GridPositionToID(Vector2Int position)
        {
            return  (Vector2) position * (1f / _resolution);
        }
        
        private void LateUpdate()
        {
            CurrentTimestamp++;
            NativeArray<bool> anyNewSurfaceDestroyed = new NativeArray<bool>(1,Allocator.TempJob);

            NativeArray<SurfacePiece> surfaceBackup = new NativeArray<SurfacePiece>(_surface, Allocator.Temp);
            
            var nativeQueue = new NativeQueue<Vector2Int>(Allocator.TempJob);
            var data = _gameSurfaceTex.GetRawTextureData<Color32>();
            var validateAreaJob = new ValidateAreaJob
            {
                Resolution = _resolution,
                Surface = _surface,
                ConnectedPiecesKernel = _connectedPiecesKernel,
                PositionsToValidate = nativeQueue,
                Timestamp = CurrentTimestamp,
                GameSurfaceTex = data,
                DidCutNewSurface = anyNewSurfaceDestroyed
            };
            var handle = validateAreaJob.Schedule();
            handle.Complete();
            nativeQueue.Dispose();
            _gameSurfaceTex.Apply();
            if (anyNewSurfaceDestroyed[0])
            {
                SpawnDestroyedPart(_surface, surfaceBackup);
            }

            anyNewSurfaceDestroyed.Dispose();
        }

        private void SpawnDestroyedPart(NativeArray<SurfacePiece> surface, NativeArray<SurfacePiece> surfaceBackup)
        {
            Texture2D maskTexture = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
            Color32[] colors = new Color32[_resolution * _resolution];
            for (int x = 0; x < _resolution; x++)
            {
                for (int y = 0; y < _resolution; y++)
                {
                    Color32 color = new Color32(0, 0, 0, 0);
                    var nodeNow = surface[x + y * _resolution];
                    var nodeBefore = surfaceBackup[x + y * _resolution];
                    if (nodeNow.State == SurfaceState.Destroyed && nodeBefore.State != SurfaceState.Destroyed)
                    {
                        color = new Color32(255, 255, 255, 255);
                    }

                    colors[x + y * _resolution] = color;
                }
            }

            maskTexture.SetPixels32(colors);
            maskTexture.Apply();

            var instance = Instantiate(_fallingPiecePrefab);
            instance.SetMask(maskTexture);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _connectedPiecesKernel.Dispose();
            _surface.Dispose();
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
            Gizmos.DrawWireCube(transform.position + new Vector3(_size / 2f, _size / 2f), new Vector3(_size, _size, 0));

            Gizmos.DrawSphere(new Vector3(_startPoint.x / (_resolution / _size), _startPoint.y / (_resolution / _size), 0), 0.1f);
            
            if (!Application.isPlaying)
            {
                return;
            }

            for (var x = 0; x < _resolution; x++)
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

        public void Cut(Vector2 position)
        {
            float radius =10;
            for (var x = -radius; x < radius; x++)
            for (var y = -radius; y < radius; y++)
            {
                position /= _size;
                if (position.x < 0 || position.y < 0 || position.x > 1 || position.y > 1)
                    return;

                Vector2Int positionOnGrid = new Vector2Int(Mathf.RoundToInt(position.x * _resolution), Mathf.RoundToInt(position.y * _resolution));
                CutInternal(positionOnGrid);
            }
        }

        private bool TryGetPositionOnGrid(Vector2 positionWS, out Vector2Int positionOnGrid)
        {
            Vector2 normalizedPosition = positionWS / _size;
            positionOnGrid = new Vector2Int(Mathf.RoundToInt(normalizedPosition.x * _resolution), Mathf.RoundToInt(normalizedPosition.y * _resolution));
            return (normalizedPosition.x > 0 && normalizedPosition.y > 0 && normalizedPosition.x < 1 && normalizedPosition.y < 1);
        }

        private void CutInternal(Vector2Int positionOnGrid)
        {
            if (InsideSurface(positionOnGrid))
            {
                var indexAtPosition = GetIndexAtPosition(positionOnGrid);
                _surface[indexAtPosition] = _surface[indexAtPosition].Cut();
            }
        }

        public SurfacePiece GetNodeAtPosition(Vector2 position)
        {
            if (TryGetPositionOnGrid(position, out Vector2Int positionOnGrid))
            {
                return _surface[GetIndexAtPosition(positionOnGrid)];
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}