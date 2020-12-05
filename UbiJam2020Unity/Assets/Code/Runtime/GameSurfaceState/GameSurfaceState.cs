using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime.GameSurfaceState
{
	public class GameSurfaceState
	{
		#region Public Fields

		public NativeArray<SurfacePiece> Surface;

		#endregion

		#region Private Fields

		private readonly bool _visualize;
		private NativeArray<int2> _connectedPiecesKernel;
		private int _resolution;
		private float _size;
		private JobHandle _currentJobHandle;
		private NativeQueue<int2> _nativeQueue;
		private NativeArray<SurfacePiece> _surfaceBackup;
		private NativeArray<bool> _anyNewSurfaceDestroyed;

		#endregion

		#region Properties

		public Texture2D GameSurfaceTex { get; }
		public int CurrentTimestamp { get; set; }

		#endregion

		#region Constructors

		public GameSurfaceState(int resolution, float size, Texture2D gameSurfaceTexture, bool visualize)
		{
			_size = size;
			_visualize = visualize;
			_resolution = resolution;
			_connectedPiecesKernel = new NativeArray<int2>(4, Allocator.Persistent);
			_connectedPiecesKernel[0] = new int2(0, +1);
			_connectedPiecesKernel[1] = new int2(0, -1);
			_connectedPiecesKernel[2] = new int2(+1, 0);
			_connectedPiecesKernel[3] = new int2(-1, 0);

			Surface = new NativeArray<SurfacePiece>(_resolution * _resolution, Allocator.Persistent);

			for (int x = 0; x < _resolution; x++)
			for (int y = 0; y < _resolution; y++)
			{
				Vector2Int position = new Vector2Int(x, y);

				Vector2 uv = GameSurface.GridPositionToID(new Vector2Int(x, y));
				float gamefieldTexValue = gameSurfaceTexture.GetPixelBilinear(uv.x, uv.y).a;

				SurfaceState surfaceState;
				if (gamefieldTexValue > 0.5f)
				{
					surfaceState = SurfaceState.Intact;
				}
				else
				{
					surfaceState = SurfaceState.Destroyed;
				}

				Surface[x + (y * _resolution)] = new SurfacePiece(position, surfaceState);
			}

			GameSurfaceTex = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
		}

		#endregion

		#region Public methods

		public JobHandle Simulate(JobHandle dependency = default)
		{
			CurrentTimestamp += 2;
			_anyNewSurfaceDestroyed = new NativeArray<bool>(1, Allocator.TempJob);

			_surfaceBackup = new NativeArray<SurfacePiece>(Surface, Allocator.Temp);

			_nativeQueue = new NativeQueue<int2>(Allocator.TempJob);
			NativeArray<Color32> data = GameSurfaceTex.GetRawTextureData<Color32>();
			JValidateAreaJob jValidateAreaJob = new JValidateAreaJob
			                                    {
				                                    Surface = Surface,
				                                    ConnectedPiecesKernel = _connectedPiecesKernel,
				                                    PositionsToValidate = _nativeQueue,
				                                    Timestamp = CurrentTimestamp,
				                                    GameSurfaceTex = data.Reinterpret<uint>(),
				                                    DidCutNewSurface = _anyNewSurfaceDestroyed,
			                                    };
			return jValidateAreaJob.Schedule(dependency);
		}

		public void FinishSimulation()
		{
			_currentJobHandle.Complete();
			_nativeQueue.Dispose();
			GameSurfaceTex.Apply();
			if (_anyNewSurfaceDestroyed[0] && _visualize)
			{
				SpawnDestroyedPart(Surface, _surfaceBackup);
			}

			_anyNewSurfaceDestroyed.Dispose();
		}

		public void SpawnDestroyedPart(NativeArray<SurfacePiece> surface, NativeArray<SurfacePiece> surfaceBackup)
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

			GameSurface.Instance.SpawnDestroyedPart(maskTexture);
		}

		public void FillCircle(Vector3 explosionPosition, float radius, SurfaceState overwriteState)
		{
			for (int x = 0; x < _resolution; x++)
			{
				for (int y = 0; y < _resolution; y++)
				{
					Vector2 positonWS = new Vector2(((float) x / _resolution) * _size, ((float) y / _resolution) * _size);
					if (Vector2.Distance(explosionPosition, positonWS) < radius)
					{
						int indexAtPosition = GameSurface.GetIndexAtGridPosition(new Vector2Int(x, y));
						SurfacePiece surfacePiece = Surface[indexAtPosition];
						surfacePiece.State = overwriteState;
						Surface[indexAtPosition] = surfacePiece;
					}
				}
			}
		}

		public void Cut(Vector2 from, Vector2 to)
		{
			Vector2Int fromGridPos = GameSurface.WorldSpaceToGrid(from);
			Vector2Int toGridPos = GameSurface.WorldSpaceToGrid(to);

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

		public void Dispose()
		{
			_connectedPiecesKernel.Dispose();
			Surface.Dispose();
		}

		public void CopySurfaceTo(NativeArray<SurfaceState> combinedSurface)
		{
			for (int i = 0; i < Surface.Length; i++)
			{
				combinedSurface[i] = Surface[i].State;
			}
		}

		#endregion

		#region Private methods

		private void CutInternal(Vector2Int positionOnGrid)
		{
			if (GameSurface.InsideSurface(positionOnGrid))
			{
				int indexAtPosition = GameSurface.GetIndexAtGridPosition(positionOnGrid);
				Surface[indexAtPosition] = Surface[indexAtPosition].Cut();
			}
		}

		#endregion
	}
}