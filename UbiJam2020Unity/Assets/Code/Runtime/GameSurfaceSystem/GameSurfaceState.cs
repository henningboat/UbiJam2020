using Runtime.GameSurfaceSystem.Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime.GameSurfaceSystem
{
	public class GameSurfaceState
	{
		#region Public Fields

		public NativeArray<SurfaceState> Surface;

		#endregion

		#region Private Fields

		private readonly bool _visualize;
		private NativeArray<int> _connectedPiecesKernel;
		private int _resolution;
		private float _size;
		private JobHandle _currentJobHandle;
		private NativeQueue<int> _nativeQueue;
		private NativeArray<SurfaceState> _surfaceBackup;
		private NativeArray<bool> _anyNewSurfaceDestroyed;
		private NativeArray<byte> _validity;
		private NativeArray<int> _nativeQueueArray;

		#endregion

		#region Properties

		public Texture2D GameSurfaceTex { get; }

		#endregion

		#region Constructors

		public GameSurfaceState(int resolution, float size, Texture2D gameSurfaceTexture, bool visualize)
		{
			_size = size;
			_visualize = visualize;
			_resolution = resolution;
			
			_connectedPiecesKernel = new NativeArray<int>(4, Allocator.Persistent);
			_connectedPiecesKernel[0] = GameSurface.Resolution;
			_connectedPiecesKernel[1] = -GameSurface.Resolution;
			_connectedPiecesKernel[2] = 1;
			_connectedPiecesKernel[3] = -1;

			Surface = new NativeArray<SurfaceState>(GameSurface.SurfacePieceCount, Allocator.Persistent);

			for (int x = 0; x < _resolution; x++)
			for (int y = 0; y < _resolution; y++)
			{
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

				Surface[x + (y * _resolution)] = surfaceState;
			}

			GameSurfaceTex = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
		}

		#endregion

		#region Public methods

		public JobHandle Simulate(JobHandle dependency = default)
		{
			_anyNewSurfaceDestroyed = new NativeArray<bool>(1, Allocator.TempJob);

			_surfaceBackup = new NativeArray<SurfaceState>(Surface, Allocator.Temp);

			_nativeQueue = new NativeQueue<int>(Allocator.TempJob);
			_nativeQueueArray = new NativeArray<int>(GameSurface.SurfacePieceCount, Allocator.TempJob);

			_validity = new NativeArray<byte>(GameSurface.SurfacePieceCount, Allocator.TempJob);
			JValidateAreaJob jValidateAreaJob = new JValidateAreaJob
			                                    {
				                                    Surface = Surface,
				                                    ConnectedPiecesKernel = _connectedPiecesKernel,
				                                    PositionsToValidate = _nativeQueue,
				                                    DidCutNewSurface = _anyNewSurfaceDestroyed,
				                                    EmulatedNativeQueue = _nativeQueueArray,
				                                    Validity = _validity,
			                                    };

			JobHandle jobHandle = jValidateAreaJob.Schedule(dependency);

			if (_visualize)
			{
				NativeArray<Color32> data = GameSurfaceTex.GetRawTextureData<Color32>();
				JGenerateMapTexture generateMapTextureJob = new JGenerateMapTexture
				                                            {
					                                            Surface = Surface,
					                                            GameSurfaceTex = data.Reinterpret<uint4>(),
				                                            };
				jobHandle = generateMapTextureJob.Schedule(GameSurface.SurfacePieceCount, GameSurface.ParallelJobBatchCount, jobHandle);
			}

			return jobHandle;
		}

		public void FinishSimulation()
		{
			_currentJobHandle.Complete();
			_nativeQueue.Dispose();
			_validity.Dispose();
			GameSurfaceTex.Apply();
			_nativeQueueArray.Dispose();
			if (_anyNewSurfaceDestroyed[0] && _visualize)
			{
				SpawnDestroyedPart(Surface, _surfaceBackup);
			}

			_anyNewSurfaceDestroyed.Dispose();
		}

		public void SpawnDestroyedPart(NativeArray<SurfaceState> surface, NativeArray<SurfaceState> surfaceBackup)
		{
			Texture2D maskTexture = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
			Color32[] colors = new Color32[GameSurface.SurfacePieceCount];
			for (int x = 0; x < _resolution; x++)
			{
				for (int y = 0; y < _resolution; y++)
				{
					Color32 color = new Color32(0, 0, 0, 0);
					SurfaceState nodeNow = surface[x + (y * _resolution)];
					SurfaceState nodeBefore = surfaceBackup[x + (y * _resolution)];
					if ((nodeNow == SurfaceState.Destroyed) && (nodeBefore != SurfaceState.Destroyed))
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
						Surface[indexAtPosition] = overwriteState;
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
			Surface.CopyTo(combinedSurface);
		}

		#endregion

		#region Private methods

		private void CutInternal(Vector2Int positionOnGrid)
		{
			if (GameSurface.InsideSurface(positionOnGrid))
			{
				int indexAtPosition = GameSurface.GetIndexAtGridPosition(positionOnGrid);
				SurfaceState surfaceState = Surface[indexAtPosition];
				if (surfaceState == SurfaceState.Intact)
				{
					Surface[indexAtPosition] = SurfaceState.Border;
				}
			}
		}

		#endregion
	}
}