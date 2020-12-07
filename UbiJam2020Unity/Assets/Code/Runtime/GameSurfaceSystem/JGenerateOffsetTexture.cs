using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Runtime.GameSurfaceSystem
{
	[BurstCompile,]
	public struct JGenerateOffsetTexture : IJob
	{
		#region Static Stuff

		private const int Resolution = GameSurface.Resolution;

		#endregion

		#region Public Fields

		public float HeightScale;
		public NativeArray<half4> PositionTexture;
		public float SinScale;
		public float Time;
		public NativeArray<float3> Weights;
		public float WeightStrength;

		#endregion

		#region IJob Members

		public void Execute()
		{
			float gridToWorldPosition = (1f / GameSurface.Resolution) * GameSurface.Size;

			for (int x = 0; x < Resolution; x++)
			{
				for (int y = 0; y < Resolution; y++)
				{
					float2 positionWS = new float2(x, y) * gridToWorldPosition;

					float height = math.sin((Time + (x * SinScale)) * HeightScale);

					for (int i = 0; i < Weights.Length; i++)
					{
						float2 weightPosition = Weights[i].xy;
						float distance = math.length(positionWS - weightPosition);
						float heightOffset = math.smoothstep(0, 1, (Weights[i].z - distance) * 0.2f) * WeightStrength;
						height += heightOffset;
					}

					float4 oldValue = PositionTexture[x + (y * Resolution)];
					float4 newValue = new half4((half) (x * gridToWorldPosition), (half) (y * gridToWorldPosition), (half) height, (half) 1);



					PositionTexture[x + (y * Resolution)] = (half4) math.lerp(oldValue, newValue, DeltaTime);
				}
			}
		}

		public float DeltaTime;

		#endregion
	}
}