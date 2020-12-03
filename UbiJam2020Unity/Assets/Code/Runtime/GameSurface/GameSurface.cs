using System;
using Photon.Pun;
using Runtime.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Runtime.GameSurface
{
	public class GameSurface : Singleton<GameSurface>
	{
		#region Serialize Fields

		[SerializeField,] private int _resolution = 10;
		[SerializeField,] private float _size;
		[SerializeField,] private Vector2Int _startPoint = new Vector2Int(4, 4);
		[SerializeField,] private Texture2D _gameSurfaceColorTexture;
		[SerializeField,] private FallingPiece _fallingPiecePrefab;
		[SerializeField,] private float _patchRadius = 2;

		#endregion

		#region Private Fields

		private NativeArray<Vector2Int> _connectedPiecesKernel;
		private Texture2D _gameSurfaceTex;
		private NativeArray<SurfacePiece> _surface;
		private Renderer _renderer;
		private PhotonView _photonView;

		#endregion

		#region Properties

		public int CurrentTimestamp { get; private set; }
		public float WorldSpaceGridNodeSize { get; private set; }

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_photonView = GetComponent<PhotonView>();
			_renderer = GetComponentInChildren<Renderer>();

			WorldSpaceGridNodeSize = (1f / _resolution) * _size;

			_connectedPiecesKernel = new NativeArray<Vector2Int>(4, Allocator.Persistent);
			_connectedPiecesKernel[0] = Vector2Int.up;
			_connectedPiecesKernel[1] = Vector2Int.down;
			_connectedPiecesKernel[2] = Vector2Int.left;
			_connectedPiecesKernel[3] = Vector2Int.right;

			_surface = new NativeArray<SurfacePiece>(_resolution * _resolution, Allocator.Persistent);

			_gameSurfaceTex = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
			gameObject.GetComponentInChildren<Renderer>().material.SetTexture("_Mask", _gameSurfaceTex);

			for (int x = 0; x < _resolution; x++)
			for (int y = 0; y < _resolution; y++)
			{
				Vector2Int position = new Vector2Int(x, y);

				Vector2 uv = GridPositionToID(new Vector2Int(x, y));
				float gamefieldTexValue = _gameSurfaceColorTexture.GetPixelBilinear(uv.x, uv.y).a;

				SurfaceState surfaceState;
				if (gamefieldTexValue > 0.5f)
				{
					surfaceState = position == _startPoint ? SurfaceState.Permanent : SurfaceState.Intact;
				}
				else
				{
					surfaceState = SurfaceState.Destroyed;
				}

				_surface[x + (y * _resolution)] = new SurfacePiece(position, surfaceState);
			}
		}

		private void LateUpdate()
		{
			//important, the system is doing 2 passes, so we need to increment timestamp by two
			CurrentTimestamp += 2;
			NativeArray<bool> anyNewSurfaceDestroyed = new NativeArray<bool>(1, Allocator.TempJob);

			NativeArray<SurfacePiece> surfaceBackup = new NativeArray<SurfacePiece>(_surface, Allocator.Temp);

			NativeQueue<Vector2Int> nativeQueue = new NativeQueue<Vector2Int>(Allocator.TempJob);
			NativeArray<Color32> data = _gameSurfaceTex.GetRawTextureData<Color32>();
			ValidateAreaJob validateAreaJob = new ValidateAreaJob
			                                  {
				                                  Resolution = _resolution,
				                                  Surface = _surface,
				                                  ConnectedPiecesKernel = _connectedPiecesKernel,
				                                  PositionsToValidate = nativeQueue,
				                                  Timestamp = CurrentTimestamp,
				                                  GameSurfaceTex = data,
				                                  DidCutNewSurface = anyNewSurfaceDestroyed,
			                                  };
			JobHandle handle = validateAreaJob.Schedule();
			handle.Complete();
			nativeQueue.Dispose();
			_gameSurfaceTex.Apply();
			if (anyNewSurfaceDestroyed[0])
			{
				SpawnDestroyedPart(_surface, surfaceBackup);
			}

			anyNewSurfaceDestroyed.Dispose();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			_connectedPiecesKernel.Dispose();
			_surface.Dispose();
		}

		private void OnDrawGizmos()
		{
			Gizmos.DrawWireCube(transform.position + new Vector3(_size / 2f, _size / 2f), new Vector3(_size, _size, 0));

			Gizmos.DrawSphere(new Vector3(_startPoint.x / (_resolution / _size), _startPoint.y / (_resolution / _size), 0), 0.1f);

			if (!Application.isPlaying)
			{
				return;
			}

			for (int x = 0; x < _resolution; x++)
			for (int y = 0; y < _resolution; y++)
			{
				SurfaceState surfaceState = _surface[x + (y * _resolution)].State;
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

		#endregion

		#region Public methods

		public Vector2 GridPositionToID(Vector2Int position)
		{
			return (Vector2) position * (1f / _resolution);
		}

		public bool InsideSurface(Vector2Int position)
		{
			return (position.x >= 0) && (position.x < _resolution) &&
			       (position.y >= 0) && (position.y < _resolution);
		}

		public void Cut(Vector2 from, Vector2 to)
		{
			_photonView.RPC("RPCCut", RpcTarget.AllViaServer, from, to);
		}

		public SurfacePiece GetNodeAtPosition(Vector2 position)
		{
			if (TryGetPositionOnGrid(position, out Vector2Int positionOnGrid))
			{
				return _surface[GetIndexAtPosition(positionOnGrid)];
			}

			throw new ArgumentOutOfRangeException();
		}

		public SurfacePiece GetNodeAtGridPosition(Vector2Int gridPosition)
		{
			return _surface[GetIndexAtPosition(gridPosition)];
		}

		public void DestroyCircle(Vector3 explosionPosition, float radius)
		{
			_photonView.RPC("RPCDestroyCircle", RpcTarget.AllViaServer, explosionPosition, radius);
		}

		[PunRPC]
		private void RPCDestroyCircle(Vector3 explosionPosition, float radius)
		{
			for (int x = 0; x < _resolution; x++)
			{
				for (int y = 0; y < _resolution; y++)
				{
					Vector2 positonWS = new Vector2(((float) x / _resolution) * _size, ((float) y / _resolution) * _size);
					if (Vector2.Distance(explosionPosition, positonWS) < radius)
					{
						CutInternal(positonWS, positonWS);
					}
				}
			}
		}

		public void SpawnPatch(Vector3 patchPosition)
		{
			_photonView.RPC("RPCSpawnPatch", RpcTarget.AllViaServer, patchPosition);
		}
		
		[PunRPC]
		private void RPCSpawnPatch(Vector3 patchPosition)
		{
			for (int x = 0; x < _resolution; x++)
			{
				for (int y = 0; y < _resolution; y++)
				{
					Vector2 positonWS = new Vector2(((float) x / _resolution) * _size, ((float) y / _resolution) * _size);
					if (Vector2.Distance(patchPosition, positonWS) < _patchRadius)
					{
						int index = GetIndexAtPosition(new Vector2Int(x, y));
						SurfacePiece node = _surface[index];
						node.State = SurfaceState.Intact;
						_surface[index] = node;
					}
				}
			}

			GetComponentInChildren<Renderer>().material.SetVector("_PatchTransformation", new Vector4(patchPosition.x, patchPosition.y, _patchRadius));
		}

		public Vector3 GridPositionToWorldPosition(Vector2Int lastNodePosition)
		{
			return (Vector2) lastNodePosition * ((1f / _resolution) * _size);
		}

		#endregion

		#region Private methods

		private Vector2Int WorldSpaceToGrid(Vector2 position)
		{
			position /= _size;
			return new Vector2Int(Mathf.RoundToInt(position.x * _resolution), Mathf.RoundToInt(position.y * _resolution));
		}

		[PunRPC,]
		private void RPCCut(Vector2 from, Vector2 to)
		{
			CutInternal(@from, to);
		}

		private void CutInternal(Vector2 @from, Vector2 to)
		{
			Vector2Int fromGridPos = WorldSpaceToGrid(@from);
			Vector2Int toGridPos = WorldSpaceToGrid(to);

			if (fromGridPos == toGridPos)
			{
				CutInternal(fromGridPos);
				return;
			}

			Vector2Int delta = toGridPos - fromGridPos;
			bool mayorIsHorizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);

			int fromPosOnMayorAxis = mayorIsHorizontal ? fromGridPos.x : fromGridPos.y;
			int toPosOnMayorAxis = mayorIsHorizontal ? toGridPos.x : toGridPos.y;

			int fromPosOnMinorAxis = mayorIsHorizontal ? fromGridPos.y : fromGridPos.x;
			int toPosOnMinorAxis = mayorIsHorizontal ? toGridPos.y : toGridPos.x;

			int currentMayorPos = fromPosOnMayorAxis;
			int currentMinorPos = fromPosOnMinorAxis;

			while (currentMayorPos != toPosOnMayorAxis)
			{
				if (mayorIsHorizontal)
				{
					CutInternal(new Vector2Int(currentMayorPos, currentMinorPos));
				}
				else
				{
					CutInternal(new Vector2Int(currentMinorPos, currentMayorPos));
				}

				if (toPosOnMayorAxis > currentMayorPos)
				{
					currentMayorPos++;
				}
				else
				{
					currentMayorPos--;
				}

				currentMinorPos = Mathf.RoundToInt(Mathf.Lerp(fromPosOnMinorAxis, toPosOnMinorAxis, (float) (currentMayorPos - fromPosOnMayorAxis) / (toPosOnMayorAxis - fromPosOnMayorAxis)));
			}

			CutInternal(toGridPos);
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
					SurfacePiece nodeNow = surface[x + (y * _resolution)];
					SurfacePiece nodeBefore = surfaceBackup[x + (y * _resolution)];
					if ((nodeNow.State == SurfaceState.Destroyed) && (nodeBefore.State != SurfaceState.Destroyed))
					{
						color = new Color32(255, 255, 255, 255);
					}

					colors[x + (y * _resolution)] = color;
				}
			}

			maskTexture.SetPixels32(colors);
			maskTexture.Apply();

			FallingPiece instance = Instantiate(_fallingPiecePrefab);
			instance.SetMaterial(maskTexture, _renderer.material);
		}

		private int GetIndexAtPosition(Vector2Int connectionPosition)
		{
			return connectionPosition.x + (_resolution * connectionPosition.y);
		}

		private bool TryGetPositionOnGrid(Vector2 positionWS, out Vector2Int positionOnGrid)
		{
			Vector2 normalizedPosition = positionWS / _size;
			positionOnGrid = new Vector2Int(Mathf.RoundToInt(normalizedPosition.x * _resolution), Mathf.RoundToInt(normalizedPosition.y * _resolution));
			return (normalizedPosition.x > 0) && (normalizedPosition.y > 0) && (normalizedPosition.x < 1) && (normalizedPosition.y < 1);
		}

		private void CutInternal(Vector2Int positionOnGrid)
		{
			if (InsideSurface(positionOnGrid))
			{
				int indexAtPosition = GetIndexAtPosition(positionOnGrid);
				_surface[indexAtPosition] = _surface[indexAtPosition].Cut();
			}
		}

		#endregion
	}
}