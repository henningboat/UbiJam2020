using Runtime.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

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
        public int CurrentTimestamp { get; private set; }

        protected override void Awake()
        {
            base.Awake();
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
            var nativeQueue = new NativeQueue<Vector2Int>(Allocator.TempJob);
            var data = _gameSurfaceTex.GetRawTextureData<Color32>();
            var validateAreaJob = new ValidateAreaJob
            {
                Resolution = _resolution,
                Surface = _surface,
                ConnectedPiecesKernel = _connectedPiecesKernel,
                PositionsToValidate = nativeQueue,
                Timestamp = CurrentTimestamp,
                GameSurfaceTex = data
            };
            var handle = validateAreaJob.Schedule();
            handle.Complete();
            nativeQueue.Dispose();
            _gameSurfaceTex.Apply();
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
            position /= _size;
            if (position.x < 0 || position.y < 0 || position.x > 1 || position.y > 1)
                return;
            
            Vector2Int positionOnGrid = new Vector2Int(Mathf.RoundToInt(position.x * _resolution), Mathf.RoundToInt(position.y * _resolution));
            Cut(positionOnGrid);

        }

        private void Cut(Vector2Int positionOnGrid)
        {
            if (InsideSurface(positionOnGrid))
            {
                var indexAtPosition = GetIndexAtPosition(positionOnGrid);
                _surface[indexAtPosition] = _surface[indexAtPosition].Cut();
            }
        }
    }
}