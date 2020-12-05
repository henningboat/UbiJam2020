using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Runtime.GameSurfaceSystem.Jobs
{
	[BurstCompile,]
	public struct JValidateAreaJob : IJob
	{
		#region Static Stuff

		public const int Resolution = GameSurface.Resolution;

		#endregion

		#region Public Fields

		[ReadOnly,] public NativeArray<int2> ConnectedPiecesKernel;
		public NativeArray<bool> DidCutNewSurface;
		public NativeArray<uint> GameSurfaceTex;
		public NativeQueue<int2> PositionsToValidate;
		public NativeArray<SurfacePiece> Surface;
		public int Timestamp;

		#endregion

		#region Public methods

		public bool InsideSurface(int2 position)
		{
			return (position.x >= 0) && (position.x < Resolution) &&
			       (position.y >= 0) && (position.y < Resolution);
		}

		#endregion

		#region Private methods

		private void ValidateAllConnectedSurfaces(int2 basePosition)
		{
			SurfacePiece node = GetSurfacePiece(basePosition.x, basePosition.y);
			if (node.IsInvalid(Timestamp))
			{
				SetNodeAtPosition(basePosition.x, basePosition.y, node.Validate(Timestamp));

				if (node.State != SurfaceState.Border)
				{
					for (int i = 0; i < 4; i++)
					{
						int2 offset = ConnectedPiecesKernel[i];
						int2 connectionPosition = basePosition + offset;
						if (InsideSurface(connectionPosition))
						{
							SurfacePiece connection = GetSurfacePiece(connectionPosition.x, connectionPosition.y);
							if ((connection.State != SurfaceState.Destroyed) && connection.IsInvalid(Timestamp))
							{
								PositionsToValidate.Enqueue(connectionPosition);
							}
						}
					}
				}
			}
		}

		private void CountAllConnectedIntactNodes(int2 basePosition, ref int numberOfPiecesInGroup)
		{
			SurfacePiece node = GetSurfacePiece(basePosition.x, basePosition.y);
			if (node.IsInvalid(Timestamp))
			{
				SetNodeAtPosition(basePosition.x, basePosition.y, node.Validate(Timestamp));
				numberOfPiecesInGroup++;

				if (node.State != SurfaceState.Border)
				{
					for (int i = 0; i < 4; i++)
					{
						int2 offset = ConnectedPiecesKernel[i];
						int2 connectionPosition = basePosition + offset;
						if (InsideSurface(connectionPosition))
						{
							SurfacePiece connection = GetSurfacePiece(connectionPosition.x, connectionPosition.y);
							if ((connection.State == SurfaceState.Intact) && connection.IsInvalid(Timestamp))
							{
								PositionsToValidate.Enqueue(connectionPosition);
							}
						}
					}
				}
			}
		}

		private void SetNodeAtPosition(int positionX, int positionY, SurfacePiece surfacePiece)
		{
			Surface[positionX + (positionY * Resolution)] = surfacePiece;
		}

		private SurfacePiece GetSurfacePiece(int x, int y)
		{
			return Surface[x + (y * Resolution)];
		}

		#endregion

		#region IJob Members

		public void Execute()
		{
			const uint ColorClear = 0;
			const uint ColorSolid = uint.MaxValue;

			int groupID = 0;
			int biggestGroupID = 0;
			int biggestGroupCount = 0;
			int2 biggestGroupStartTile = 0;

			for (int x = 0; x < Resolution; x++)
			for (int y = 0; y < Resolution; y++)
			{
				SurfacePiece surfacePiece = GetSurfacePiece(x, y);
				if ((surfacePiece.State == SurfaceState.Intact) && surfacePiece.IsInvalid(Timestamp))
				{
					int numberOfPiecesInGroup = 0;
					PositionsToValidate.Clear();
					PositionsToValidate.Enqueue(new int2(x, y));
					while (PositionsToValidate.Count > 0)
					{
						CountAllConnectedIntactNodes(PositionsToValidate.Dequeue(), ref numberOfPiecesInGroup);
					}

					if (numberOfPiecesInGroup > biggestGroupCount)
					{
						biggestGroupCount = numberOfPiecesInGroup;
						biggestGroupStartTile = new int2(x, y);
					}
				}
			}

			Timestamp++;

			PositionsToValidate.Clear();
			PositionsToValidate.Enqueue(biggestGroupStartTile);
			while (PositionsToValidate.Count > 0)
			{
				ValidateAllConnectedSurfaces(PositionsToValidate.Dequeue());
			}

			bool anyNewDestroyedNodes = false;

			for (int x = 0; x < Resolution; x++)
			for (int y = 0; y < Resolution; y++)
			{
				SurfacePiece node = GetSurfacePiece(x, y);
				if (node.IsInvalid(Timestamp) && (node.State != SurfaceState.Destroyed))
				{
					anyNewDestroyedNodes = true;

					SetNodeAtPosition(x, y, node.DestroyPiece());
					GameSurfaceTex[x + (y * Resolution)] = ColorClear;
				}
				else
				{
					switch (node.State)
					{
						case SurfaceState.Intact:
						case SurfaceState.Permanent:
							GameSurfaceTex[x + (y * Resolution)] = ColorSolid;
							break;
						case SurfaceState.Border:
						case SurfaceState.Destroyed:
							GameSurfaceTex[x + (y * Resolution)] = ColorClear;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			DidCutNewSurface[0] = anyNewDestroyedNodes;
		}

		#endregion
	}
}