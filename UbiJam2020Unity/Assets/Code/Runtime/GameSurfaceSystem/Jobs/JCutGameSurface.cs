using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Runtime.GameSurfaceSystem.Jobs
{
	[BurstCompile,]
	public struct JCutGameSurface : IJob
	{
		#region Public Fields

		public Vector2Int fromGridPos;
		public Vector2Int toGridPos;
		public NativeArray<SurfaceState> Surface;

		#endregion

		#region Private methods

		private void CutInternal(Vector2Int posistion)
		{
			int index = posistion.x + (posistion.y * GameSurface.Resolution);
			if (Surface[index] == SurfaceState.Intact)
			{
				Surface[index] = SurfaceState.Border;
			}
		}

		#endregion

		#region IJob Members

		public void Execute()
		{
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

		#endregion
	}
}