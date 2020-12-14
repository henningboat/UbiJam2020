using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Runtime.GameSurfaceSystem.Jobs
{
	[BurstCompile,]
	public struct JSpawnCircleSurface : IJobFor
	{
		#region Public Fields

		public SurfaceState OverwriteState;
		public Vector2 Position;
		public float Radius;
		public NativeArray<SurfaceState> Surface;

		#endregion

		#region IJobParallelFor Members

		public void Execute(int index)
		{
			Vector2Int gridPosition = GameSurface.GridIndexToGridPosition(index);
			int x = gridPosition.x;
			int y = gridPosition.y;

			Vector2 positonWS = GameSurface.GridPositionToWorldPosition(gridPosition);
			if (Vector2.Distance(Position, positonWS) < Radius)
			{
				int indexAtPosition = GameSurface.GetIndexAtGridPosition(new Vector2Int(x, y));
				Surface[indexAtPosition] = OverwriteState;
			}
		}

		#endregion
	}
}