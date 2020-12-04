using Photon.Pun;
using Runtime.Utils;
using Unity.Collections;
using UnityEngine;

namespace Runtime.GameSurfaceState
{
	public class GameSurface : Singleton<GameSurface>
	{
		#region Static Stuff

		private const RpcTarget RPCSyncMethod = RpcTarget.AllViaServer;

		public static Vector2 GridPositionToID(Vector2Int position)
		{
			return (Vector2) position * (1f / Instance._resolution);
		}

		public static bool InsideSurface(Vector2Int position)
		{
			return (position.x >= 0) && (position.x < Instance._resolution) &&
			       (position.y >= 0) && (position.y < Instance._resolution);
		}

		public static Vector2Int WorldSpaceToGrid(Vector2 position)
		{
			position /= Instance._size;
			return new Vector2Int(Mathf.RoundToInt(position.x * Instance._resolution), Mathf.RoundToInt(position.y * Instance._resolution));
		}

		public static int GetIndexAtGridPosition(Vector2Int connectionPosition)
		{
			return connectionPosition.x + (Instance._resolution * connectionPosition.y);
		}

		#endregion

		#region Serialize Fields

		[SerializeField,] private int _resolution = 10;
		[SerializeField,] private float _size;
		[SerializeField,] private Vector2Int _startPoint = new Vector2Int(4, 4);
		[SerializeField,] private Texture2D _gameSurfaceColorTexture;
		[SerializeField,] private FallingPiece _fallingPiecePrefab;
		[SerializeField,] private float _patchRadius = 2;

		#endregion

		#region Private Fields

		private Renderer _renderer;
		private PhotonView _photonView;
		private GameSurfaceState _localState;
		private GameSurfaceState _syncronizedState;
		private NativeArray<SurfaceState> _combinedSurface;
		private int _localRPCCount;
		private int _sentRPCNumber;
		private int _receivedRpcNumber;
		private NativeArray<int> _rpcNumberPerNode;
		private NativeArray<SurfaceState> _localStateBackup;

		#endregion

		#region Properties

		public float WorldSpaceGridNodeSize { get; private set; }

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_photonView = GetComponent<PhotonView>();
			_renderer = GetComponentInChildren<Renderer>();

			WorldSpaceGridNodeSize = (1f / _resolution) * _size;

			_localState = new GameSurfaceState(_resolution, _size, _gameSurfaceColorTexture, false);
			_syncronizedState = new GameSurfaceState(_resolution, _size, _gameSurfaceColorTexture, true);

			_rpcNumberPerNode = new NativeArray<int>(_resolution * _resolution, Allocator.Persistent);

			_combinedSurface = new NativeArray<SurfaceState>(_resolution * _resolution, Allocator.Persistent);
			Material material = gameObject.GetComponentInChildren<Renderer>().material;
			material.SetTexture("_Mask", _syncronizedState.GameSurfaceTex);
			material.SetTexture("_LocalMask", _localState.GameSurfaceTex);
			
			
			_localStateBackup = new NativeArray<SurfaceState>(_resolution * _resolution, Allocator.Persistent);
			_localState.CopySurfaceTo(_localStateBackup);
		}

		private void LateUpdate()
		{
			_localState.Simulate();
			_syncronizedState.Simulate();

			_localState.FinishSimulation();
			_syncronizedState.FinishSimulation();

			int differenceCount=0;
			
			

			for (int i = 0; i < _rpcNumberPerNode.Length; i++)
			{
				if (_localState.Surface[i].State != _localStateBackup[i])
				{
					_rpcNumberPerNode[i] = _sentRPCNumber;
				}
			}
			
			for (int i = 0; i < _rpcNumberPerNode.Length; i++)
			{
				int rpcNumberOfNode = _rpcNumberPerNode[i];
				if (_receivedRpcNumber >= rpcNumberOfNode)
				{
					SurfacePiece surfacePiece = _localState.Surface[i];
					surfacePiece.State = _syncronizedState.Surface[i].State;
					_localState.Surface[i] = surfacePiece;
				}
				else
				{
					differenceCount++;
				}
			}

			_localState.CopySurfaceTo(_combinedSurface);


			_localState.CopySurfaceTo(_localStateBackup);
			
			Debug.Log("diff count: " + differenceCount);

			//Debug.Log($"{_sentRPCNumber} {_receivedRpcNumber}");
		}

		protected override void OnDestroy()
		{
			_localState.Dispose();
			_syncronizedState.Dispose();
			_localStateBackup.Dispose();
			base.OnDestroy();
		}

		#endregion

		#region Public methods

		public void Cut(Vector2 from, Vector2 to)
		{
			_localState.Cut(from, to);
			_photonView.RPC("RPCCut", RPCSyncMethod, from, to, GetAndIncrementRPCNumber());
		}

		public void DestroyCircle(Vector3 explosionPosition, float radius)
		{
			_localState.FillCircle(explosionPosition, radius, SurfaceState.Destroyed);
			_photonView.RPC("RPCDestroyCircle", RPCSyncMethod, explosionPosition, radius, GetAndIncrementRPCNumber());
		}

		public void SpawnPatch(Vector3 patchPosition)
		{
			_localState.FillCircle(patchPosition, _patchRadius, SurfaceState.Intact);
			GetComponentInChildren<Renderer>().material.SetVector("_PatchTransformation", new Vector4(patchPosition.x, patchPosition.y, _patchRadius));
			_photonView.RPC("RPCSpawnPatch", RPCSyncMethod, patchPosition, GetAndIncrementRPCNumber());
		}

		public Vector3 GridPositionToWorldPosition(Vector2Int lastNodePosition)
		{
			return (Vector2) lastNodePosition * ((1f / _resolution) * _size);
		}

		public void SpawnDestroyedPart(Texture2D maskTexture)
		{
			FallingPiece instance = Instantiate(_fallingPiecePrefab);
			instance.SetMaterial(maskTexture, _renderer.material);
		}

		public bool IsWalkableAtGridPosition(Vector2Int lastNodePosition)
		{
			if (InsideSurface(lastNodePosition))
			{
				return _combinedSurface[GetIndexAtGridPosition(lastNodePosition)] != SurfaceState.Destroyed;
			}
			
			return false;
		}

		public bool IsWalkableAtWorldPosition(Vector3 positionWS)
		{
			if (TryGetPositionOnGrid(positionWS, out Vector2Int positionOnGrid))
			{
				return IsWalkableAtGridPosition(positionOnGrid);
			}

			return false;
		}

		#endregion

		#region Private methods

		private int GetAndIncrementRPCNumber()
		{
			_sentRPCNumber++;
			return _sentRPCNumber;
		}

		private bool TryGetPositionOnGrid(Vector2 positionWS, out Vector2Int positionOnGrid)
		{
			Vector2 normalizedPosition = positionWS / _size;
			positionOnGrid = new Vector2Int(Mathf.RoundToInt(normalizedPosition.x * _resolution), Mathf.RoundToInt(normalizedPosition.y * _resolution));
			return (normalizedPosition.x > 0) && (normalizedPosition.y > 0) && (normalizedPosition.x < 1) && (normalizedPosition.y < 1);
		}

		private void HandleRPCNumberReceive(int rpcNumber, PhotonMessageInfo info)
		{
			if (info.Sender.IsLocal)
			{
				_receivedRpcNumber = rpcNumber;
			}
		}

		#endregion

		#region RPC

		[PunRPC,]
		private void RPCDestroyCircle(Vector3 explosionPosition, float radius, int rpcNumber, PhotonMessageInfo info)
		{
			_syncronizedState.FillCircle(explosionPosition, radius, SurfaceState.Destroyed);
			HandleRPCNumberReceive(rpcNumber, info);
		}

		[PunRPC,]
		private void RPCSpawnPatch(Vector3 patchPosition, int rpcNumber, PhotonMessageInfo info)
		{
			_syncronizedState.FillCircle(patchPosition, _patchRadius, SurfaceState.Intact);
			GetComponentInChildren<Renderer>().material.SetVector("_PatchTransformation", new Vector4(patchPosition.x, patchPosition.y, _patchRadius));
			HandleRPCNumberReceive(rpcNumber, info);
		}

		[PunRPC,]
		private void RPCCut(Vector2 from, Vector2 to, int rpcNumber, PhotonMessageInfo info)
		{
			_syncronizedState.Cut(from, to);
			HandleRPCNumberReceive(rpcNumber, info);
		}

		#endregion
	}
}