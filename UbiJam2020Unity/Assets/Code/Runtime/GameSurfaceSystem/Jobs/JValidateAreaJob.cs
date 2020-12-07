
#define UseArrayAsQueue

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

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
		public NativeArray<byte> Validity;

		#endregion

		#region Private Fields

		private byte Timestamp;

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

			if (nodeState != SurfaceState.Border)
			{
				for (int i = 0; i < 4; i++)
				{
					int offsetInArray = ConnectedPiecesKernel[i];
					int connectionPosition = indexAtPosition + offsetInArray;
					if (InsideSurface(connectionPosition))
					{
						SurfaceState connectedNodeState = Surface[connectionPosition];
						int connectedNodeStateValidity = Validity[connectionPosition];
						if ((connectedNodeState != SurfaceState.Destroyed) && (connectedNodeStateValidity < Timestamp))
						{
							Validity[connectionPosition] = Timestamp;
							Enqueue(connectionPosition);
						}
					}
				}
			}
		}

		private void CountAllConnectedIntactNodes(int indexAtPosition, ref int numberOfPiecesInGroup)
		{
			SurfaceState nodeState = Surface[indexAtPosition];

			numberOfPiecesInGroup++;

			if (nodeState == SurfaceState.Intact)
			{
				for (int i = 0; i < 4; i++)
				{
					int offsetInArray = ConnectedPiecesKernel[i];
					int connectionPosition = indexAtPosition + offsetInArray;
					if (InsideSurface(connectionPosition))
					{
						SurfaceState connectedNodeState = Surface[connectionPosition];
						int connectedNodeStateValidity = Validity[connectionPosition];
						if ((connectedNodeState == SurfaceState.Intact) && (connectedNodeStateValidity < Timestamp))
						{
							Validity[connectionPosition] = Timestamp;
							Enqueue(connectionPosition);
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
			ClearQueue();

			for (int x = 0; x < Resolution; x++)
			for (int y = 0; y < Resolution; y++)
			{
				int indexOfNode = GetIndexAtPosition(x, y);
				if ((Surface[indexOfNode] == SurfaceState.Intact) && (Validity[indexOfNode] < Timestamp))
				{
					int numberOfPiecesInGroup = 0;
					ClearQueue();
					Validity[indexOfNode] = Timestamp;
					Enqueue(indexOfNode);
					while (HasQueuedPosition())
					{
						int positionToValidate = Dequeue();
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

			ClearQueue();
			Validity[biggestGroupStartTile] = Timestamp;
			Enqueue(biggestGroupStartTile);
			while (HasQueuedPosition())
			{
				int positionToValidate = Dequeue();
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

		#region Queue
		private int _queueHead;
		private int _queueTail;
		public NativeArray<int> EmulatedNativeQueue;

		private void Enqueue(int connectionPosition)
		{
			#if  UseArrayAsQueue
		
			EmulatedNativeQueue[_queueHead] = connectionPosition;
			_queueHead++;

#else
			PositionsToValidate.Enqueue(connectionPosition);
#endif
		}

		private int Dequeue()
		{
			#if  UseArrayAsQueue
			int value = EmulatedNativeQueue[_queueTail];
			_queueTail++;
			return value;
#else
			return PositionsToValidate.Dequeue();
#endif
		}

		private void ClearQueue()
		{
			#if  UseArrayAsQueue
			_queueTail = 0;
			_queueHead = 0;
#else
			PositionsToValidate.Clear();
#endif
		}

		private bool HasQueuedPosition()
		{
			#if  UseArrayAsQueue
			return _queueHead > _queueTail;
#else
			return PositionsToValidate.Count > 0;
#endif
		}

		#endregion
	}
}