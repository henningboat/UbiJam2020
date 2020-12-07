using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Runtime.GameSurfaceSystem.Jobs
{
	[BurstCompile]
	public struct JGenerateMapTexture : IJob
	{
		#region Public Fields

		[WriteOnly,] public NativeArray<byte> GameSurfaceTex;
		[ReadOnly,] public NativeArray<SurfaceState> Surface;

		#endregion

		#region IJobFor Members

		public void Execute()
		{
			Surface.Reinterpret<byte>().CopyTo(GameSurfaceTex);
		}

		#endregion
	}
}