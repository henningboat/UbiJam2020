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
		public const int SurfacePieceCount = GameSurface.SurfacePieceCount;

		#endregion

		#region Public Fields

		[ReadOnly,] public NativeArray<int> ConnectedPiecesKernel;
		public NativeArray<bool> DidCutNewSurface;
		public NativeQueue<int> PositionsToValidate;
		public NativeArray<SurfaceState> Surface;
		private byte Timestamp;
		public NativeArray<byte> Validity;

		#endregion

		#region Public methods

		public bool InsideSurface(int position)
		{
			return (position >= 0) && (position < SurfacePieceCount);
		}

		#endregion

		#region Private methods

		private void ValidateAllConnectedSurfaces(int indexAtPosition)
		{
			SurfaceState nodeState = Surface[indexAtPosition];
			int nodeValidity = Validity[indexAtPosition];

			if (nodeValidity < Timestamp)
			{
				Validity[indexAtPosition] = Timestamp;

				if (nodeState != SurfaceState.Border)
				{
					for (int i = 0; i < 4; i++)
					{
						int offsetInArray =  ConnectedPiecesKernel[i];
						int connectionPosition = indexAtPosition + offsetInArray;
						if (InsideSurface(connectionPosition))
						{
							SurfaceState connectedNodeState = Surface[connectionPosition];
							int connectedNodeStateValidity = Validity[connectionPosition];
							if ((connectedNodeState != SurfaceState.Destroyed) && (connectedNodeStateValidity < Timestamp))
							{
								PositionsToValidate.Enqueue(connectionPosition);
							}
						}
					}
				}
			}
		}

		private void CountAllConnectedIntactNodes(int indexAtPosition, ref int numberOfPiecesInGroup)
		{
			SurfaceState nodeState = Surface[indexAtPosition];
			int nodeValidity = Validity[indexAtPosition];
			if (nodeValidity < Timestamp)
			{
				Validity[indexAtPosition] = Timestamp;
				numberOfPiecesInGroup++;

				if (nodeState != SurfaceState.Border)
				{
					for (int i = 0; i < 4; i++)
					{
						int offsetInArray =  ConnectedPiecesKernel[i];
						int connectionPosition = indexAtPosition + offsetInArray;
						if (InsideSurface(connectionPosition))
						{
							SurfaceState connectedNodeState = Surface[connectionPosition];
							int connectedNodeStateValidity = Validity[connectionPosition];
							if ((connectedNodeState == SurfaceState.Intact) && (connectedNodeStateValidity < Timestamp))
							{
								PositionsToValidate.Enqueue(connectionPosition);
							}
						}
					}
				}
			}
		}

		private int GetIndexAtPosition(int x, int y)
		{
			return x + (y * Resolution);
		}

		#endregion

		#region IJob Members

		public void Execute()
		{
			Timestamp = 1;
			int groupID = 0;
			int biggestGroupID = 0;
			int biggestGroupCount = 0;
			int biggestGroupStartTile = 0;

			for (int x = 0; x < Resolution; x++)
			for (int y = 0; y < Resolution; y++)
			{
				int indexOfNode = GetIndexAtPosition(x, y);
				if ((Surface[indexOfNode] == SurfaceState.Intact) && (Validity[indexOfNode] < Timestamp))
				{
					int numberOfPiecesInGroup = 0;
					PositionsToValidate.Clear();
					PositionsToValidate.Enqueue(indexOfNode);
					while (PositionsToValidate.Count > 0)
					{
						int positionToValidate = PositionsToValidate.Dequeue();
						CountAllConnectedIntactNodes(positionToValidate, ref numberOfPiecesInGroup);
					}

					if (numberOfPiecesInGroup > biggestGroupCount)
					{
						biggestGroupCount = numberOfPiecesInGroup;
						biggestGroupStartTile = indexOfNode;
					}
				}
			}

			Timestamp++;

			PositionsToValidate.Clear();
			PositionsToValidate.Enqueue(biggestGroupStartTile);
			while (PositionsToValidate.Count > 0)
			{
				int positionToValidate = PositionsToValidate.Dequeue();
				ValidateAllConnectedSurfaces(positionToValidate
				);
			}

			bool anyNewDestroyedNodes = false;

			for (int i = 0; i < SurfacePieceCount; i++)
			{
				SurfaceState nodeState = Surface[i];
				int validity = Validity[i];
				if ((validity < Timestamp) && (nodeState != SurfaceState.Destroyed))
				{
					anyNewDestroyedNodes = true;
					Surface[i] = SurfaceState.Destroyed;
				}
			}

			DidCutNewSurface[0] = anyNewDestroyedNodes;
		}

		#endregion
	}
}