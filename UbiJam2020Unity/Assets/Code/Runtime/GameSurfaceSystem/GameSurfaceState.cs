using System.Collections.Generic;
using Runtime.Data;
using Runtime.GameSurfaceSystem.Jobs;
using Unity.Collections;
using Unity.Jobs;
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
		private Queue<IGameSurfaceEvent> _scheduledEvents;

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
				Vector2 uv = GameSurface.GridPositionToUV(new Vector2Int(x, y));
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

			GameSurfaceTex = new Texture2D(_resolution, _resolution, TextureFormat.R8, false);
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
				NativeArray<byte> data = GameSurfaceTex.GetRawTextureData<byte>();
				JGenerateMapTexture generateMapTextureJob = new JGenerateMapTexture
				                                            {
					                                            Surface = Surface,
					                                            GameSurfaceTex = data,
				                                            };
				jobHandle = generateMapTextureJob.Schedule(jobHandle);
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

		public void Dispose()
		{
			_connectedPiecesKernel.Dispose();
			Surface.Dispose();
		}

		public void CopySurfaceTo(NativeArray<SurfaceState> combinedSurface)
		{
			Surface.CopyTo(combinedSurface);
		}

		public void AddEvent(IGameSurfaceEvent data)
		{
			//todo
			//_scheduledEvents.Enqueue(data);
			data.ScheduleJob(this, default).Complete();
		}

		#endregion
	}
}