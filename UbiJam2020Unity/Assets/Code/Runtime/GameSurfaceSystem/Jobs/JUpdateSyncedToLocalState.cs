using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Runtime.GameSurfaceSystem.Jobs
{
	[BurstCompile,]
	public struct JUpdateSyncedToLocalState : IJobParallelFor
	{
		#region Public Fields

		public NativeArray<SurfaceState> LocalStateSurface;
		public int ReceivedRpcNumber;
		public NativeArray<int> RpcNumberPerNode;
		public NativeArray<SurfaceState> SyncedStateSurface;

		#endregion

		#region IJobParallelFor Members

		public void Execute(int i)
		{
			int rpcNumberOfNode = RpcNumberPerNode[i];
			if (ReceivedRpcNumber >= rpcNumberOfNode)
			{
				SurfaceState surfacePiece = LocalStateSurface[i];
				surfacePiece = SyncedStateSurface[i];
				LocalStateSurface[i] = surfacePiece;
			}
		}

		#endregion
	}
}