using Runtime.GameSystem;
using Runtime.Utils;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime.GameSurfaceSystem
{
	public class GameSurfaceRenderingHandler : Singleton<GameSurfaceRenderingHandler>
	{
		#region Serialize Fields

		[SerializeField,] private float _heightScale = 2;
		[SerializeField,] private float _sinScale = 0.1f;
		[SerializeField,] private float _weightStrength = 1;
		[SerializeField,] private float _playerWeight = 3;

		#endregion

		#region Private Fields

		private Renderer _renderer;
		private NativeArray<float3> _weights;
		[SerializeField] private float _speed = 3;

		#endregion

		#region Properties

		public Texture2D PositionOffsetTexture { get; private set; }

		#endregion

		#region Unity methods

		protected override void Awake()
		{
			base.Awake();
			_renderer = GetComponent<Renderer>();
			PositionOffsetTexture = new Texture2D(GameSurface.Resolution, GameSurface.Resolution, TextureFormat.RGBAHalf, false);
			PositionOffsetTexture.wrapMode = TextureWrapMode.Clamp;
			Shader.EnableKeyword("ApplyTransformTex");
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Destroy(PositionOffsetTexture);
			Shader.DisableKeyword("ApplyTransformTex");
		}

		#endregion

		#region Public methods

		public JobHandle ScheduleJobs(GameSurfaceState localGameState, JobHandle dependencies)
		{
			_weights = new NativeArray<float3>(GameManager.Instance.Players.Count, Allocator.TempJob);
			for (int i = 0; i < GameManager.Instance.Players.Count; i++)
			{
				Vector3 playerPosition = GameManager.Instance.Players[0].transform.position;
				_weights[0] = new float3(playerPosition.x, playerPosition.y, _playerWeight);
			}

			JGenerateOffsetTexture job = new JGenerateOffsetTexture
			                             {
				                             Time = Time.time,
				                             HeightScale = _heightScale,
				                             PositionTexture = PositionOffsetTexture.GetRawTextureData<half4>(),
				                             SinScale = _sinScale,
				                             WeightStrength = _weightStrength,
				                             Weights = _weights,
				                             DeltaTime = Time.deltaTime*_speed
			                             };
			return job.Schedule(dependencies);
		}

		public void Finish(GameSurfaceState localStateHandle)
		{
			PositionOffsetTexture.Apply();
			_weights.Dispose();
			Shader.SetGlobalFloat("_OneOverSurfaceSize", 1.0f / GameSurface.Size);
			Shader.SetGlobalTexture("_PositionOffsetTex", PositionOffsetTexture);
		}

		#endregion
	}
}