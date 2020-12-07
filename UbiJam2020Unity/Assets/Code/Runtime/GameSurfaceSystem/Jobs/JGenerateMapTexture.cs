using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Runtime.GameSurfaceSystem.Jobs
{
	[BurstCompile]
	public struct JGenerateMapTexture : IJobParallelFor
	{
		#region Public Fields

		[WriteOnly,] public NativeArray<uint> GameSurfaceTex;
		[ReadOnly,] public NativeArray<SurfaceState> Surface;

		#endregion

		#region IJobFor Members

		public void Execute(int i)
		{
			const uint colorClear = 0;
			const uint colorSolid = uint.MaxValue;

			SurfaceState node = Surface[i];

			uint nodeColor;

			if (node == SurfaceState.Intact)
			{
				nodeColor = colorSolid;
			}
			else
			{
				nodeColor = colorClear;
			}
			GameSurfaceTex[i] = nodeColor;
		}

		#endregion
	}
}