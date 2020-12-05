using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Runtime.GameSurfaceSystem.Jobs
{
	[BurstCompile,]
	public struct JUpdateSyncedToLocalState : IJobParallelFor
	{
		#region Public Fields

		public NativeArray<SurfacePiece> LocalStateSurface;
		public int ReceivedRpcNumber;
		public NativeArray<int> RpcNumberPerNode;
		public NativeArray<SurfacePiece> SyncedStateSurface;

		#endregion

		#region IJobParallelFor Members

		public void Execute(int i)
		{
			int rpcNumberOfNode = RpcNumberPerNode[i];
			if (ReceivedRpcNumber >= rpcNumberOfNode)
			{
				SurfacePiece surfacePiece = LocalStateSurface[i];
				surfacePiece.State = SyncedStateSurface[i].State;
				LocalStateSurface[i] = surfacePiece;
			}
		}

		#endregion
	}
}