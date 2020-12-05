using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Runtime.GameSurfaceSystem.Jobs
{
	[BurstCompile,]
	public struct JCompareChangesInLocalState : IJobParallelFor
	{
		#region Public Fields

		public NativeArray<SurfaceState> LastFrameLocalSurface;
		public NativeArray<SurfaceState> LocalStateSurface;
		public NativeArray<int> RpcNumberPerNode;
		public int SentRPCNumber;

		#endregion

		#region IJobParallelFor Members

		public void Execute(int i)
		{
			if (LocalStateSurface[i] != LastFrameLocalSurface[i])
			{
				RpcNumberPerNode[i] = SentRPCNumber;
			}
		}

		#endregion
	}
}