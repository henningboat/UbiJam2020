﻿using Photon.Pun;
using Runtime.GameSurfaceSystem.Jobs;
using Runtime.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Runtime.GameSurfaceSystem
{
	public class GameSurface : Singleton<GameSurface>
	{
		#region Static Stuff

		private const RpcTarget RPCSyncMethod = RpcTarget.AllViaServer;
		public const int Resolution = 256;
		public const int SurfacePieceCount = Resolution * Resolution;
		public const int ParallelJobBatchCount = SurfacePieceCount / 16;

		public static Vector2 GridPositionToID(Vector2Int position)
		{
			return (Vector2) position * (1f / Resolution);
		}

		public static bool InsideSurface(Vector2Int position)
		{
			return (position.x >= 0) && (position.x < Resolution) &&
			       (position.y >= 0) && (position.y < Resolution);
		}

		public static Vector2Int WorldSpaceToGrid(Vector2 position)
		{
			position /= Size;
			return new Vector2Int(Mathf.RoundToInt(position.x * Resolution), Mathf.RoundToInt(position.y * Resolution));
		}

		public static int GetIndexAtGridPosition(Vector2Int connectionPosition)
		{
			return connectionPosition.x + (Resolution * connectionPosition.y);
		}

		#endregion

		#region Serialize Fields

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
		public const float Size = 10;

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_photonView = GetComponent<PhotonView>();
			_renderer = GetComponentInChildren<Renderer>();

			WorldSpaceGridNodeSize = (1f / Resolution) * Size;

			_localState = new GameSurfaceState(Resolution, Size, _gameSurfaceColorTexture, false);
			_syncronizedState = new GameSurfaceState(Resolution, Size, _gameSurfaceColorTexture, true);

			_rpcNumberPerNode = new NativeArray<int>(Resolution * Resolution, Allocator.Persistent);

			_combinedSurface = new NativeArray<SurfaceState>(Resolution * Resolution, Allocator.Persistent);
			Material material = gameObject.GetComponentInChildren<Renderer>().material;
			material.SetTexture("_Mask", _syncronizedState.GameSurfaceTex);
			material.SetTexture("_LocalMask", _localState.GameSurfaceTex);

			_localStateBackup = new NativeArray<SurfaceState>(Resolution * Resolution, Allocator.Persistent);
			_localState.CopySurfaceTo(_localStateBackup);
		}

		private void LateUpdate()
		{
			JobHandle localStateHandle = _localState.Simulate();
			JobHandle syncedStateHandle = _syncronizedState.Simulate();

			JobHandle combinedJobHandle = JobHandle.CombineDependencies(localStateHandle, syncedStateHandle);

			// for (int i = 0; i < _rpcNumberPerNode.Length; i++)
			// {
			// 	if (_localState.Surface[i].State != _localStateBackup[i])
			// 	{
			// 		_rpcNumberPerNode[i] = _sentRPCNumber;
			// 	}
			// }

			// for (int i = 0; i < _rpcNumberPerNode.Length; i++)
			// {
			// 	int rpcNumberOfNode = _rpcNumberPerNode[i];
			// 	if (_receivedRpcNumber >= rpcNumberOfNode)
			// 	{
			// 		SurfacePiece surfacePiece = _localState.Surface[i];
			// 		surfacePiece.State = _syncronizedState.Surface[i].State;
			// 		_localState.Surface[i] = surfacePiece;
			// 	}
			// 	// else
			// 	// {
			// 	// 	differenceCount++;
			// 	// }
			// }

			JCompareChangesInLocalState jCompare = new JCompareChangesInLocalState
			                                       {
				                                       SentRPCNumber = _sentRPCNumber,
				                                       RpcNumberPerNode = _rpcNumberPerNode,
				                                       LocalStateSurface = _localState.Surface,
				                                       LastFrameLocalSurface = _localStateBackup,
			                                       };
			JUpdateSyncedToLocalState jUpdateJob = new JUpdateSyncedToLocalState
			                                       {
				                                       ReceivedRpcNumber = _receivedRpcNumber,
				                                       LocalStateSurface = _localState.Surface,
				                                       SyncedStateSurface = _syncronizedState.Surface,
				                                       RpcNumberPerNode = _rpcNumberPerNode,
			                                       };

			combinedJobHandle = jCompare.Schedule(SurfacePieceCount, ParallelJobBatchCount, combinedJobHandle);

			combinedJobHandle = jUpdateJob.Schedule(SurfacePieceCount, ParallelJobBatchCount, combinedJobHandle);

			combinedJobHandle = GameSurfaceRenderingHandler.Instance.ScheduleJobs(_localState, combinedJobHandle);
			
			combinedJobHandle.Complete();
			_localState.FinishSimulation();
			_syncronizedState.FinishSimulation();
			GameSurfaceRenderingHandler.Instance.Finish(_localState);

			_localState.CopySurfaceTo(_combinedSurface);

			_localState.CopySurfaceTo(_localStateBackup);
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
			return (Vector2) lastNodePosition * ((1f / Resolution) * Size);
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
			Vector2 normalizedPosition = positionWS / Size;
			positionOnGrid = new Vector2Int(Mathf.RoundToInt(normalizedPosition.x * Resolution), Mathf.RoundToInt(normalizedPosition.y * Resolution));
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