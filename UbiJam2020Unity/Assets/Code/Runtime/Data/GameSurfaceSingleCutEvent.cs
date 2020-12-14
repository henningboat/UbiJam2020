using ExitGames.Client.Photon;
using Photon.Pun;
using Runtime.GameSurfaceSystem;
using Runtime.GameSurfaceSystem.Jobs;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime.Data
{
	public class GameSurfaceSingleCutEvent: IGameSurfaceEvent
	{
		#region Static Stuff

		private const byte SerializationTypeID = 0;

		[RuntimeInitializeOnLoadMethod,]
		private static void InitializeSerialization()
		{
			PhotonPeer.RegisterType(typeof(GameSurfaceSingleCutEvent), SerializationTypeID, Serialize, Deserialize);
		}

		public static object Deserialize(byte[] data)
		{
			return new GameSurfaceSingleCutEvent
			       {
				       _fromX = data[0],
				       _fromY = data[1],
				       _toX = data[2],
				       _toY = data[3],
			       };
		}

		private static byte[] Serialize(object customType)
		{
			var gameSurfaceSingleCutEvent = (GameSurfaceSingleCutEvent) customType;
			return new[]
			       {
				       gameSurfaceSingleCutEvent._fromX,
				       gameSurfaceSingleCutEvent._fromY,
				       gameSurfaceSingleCutEvent._toX,
				       gameSurfaceSingleCutEvent._toY,
			       };
		}

		#endregion

		#region Private Fields

		private byte _fromX;
		private byte _fromY;
		private byte _toX;
		private byte _toY;

		#endregion

		#region Constructors

		private GameSurfaceSingleCutEvent()
		{
		}

		public GameSurfaceSingleCutEvent(Vector2 from, Vector2 to)
		{
			Vector2Int fromGridPos = GameSurface.WorldSpaceToGrid(from);
			Vector2Int toGridPos = GameSurface.WorldSpaceToGrid(to);
			_fromX = (byte) math.clamp(fromGridPos.x, 0, 255);
			_fromY = (byte) math.clamp(fromGridPos.y, 0, 255);
			_toX = (byte) math.clamp(toGridPos.x, 0, 255);
			_toY = (byte) math.clamp(toGridPos.y, 0, 255);
		}

		#endregion

		#region Public methods

		public JobHandle ScheduleJob(GameSurfaceState state, JobHandle dependencies)
		{
			JCutGameSurface job = new JCutGameSurface
			                      {
				                      fromGridPos = new Vector2Int(_fromX, _fromY),
				                      toGridPos = new Vector2Int(_toX, _toY),
				                      Surface = state.Surface,
			                      };
			return job.Schedule(dependencies);
		}

		#endregion
	}
	public interface IGameSurfaceEvent
	{
		JobHandle ScheduleJob(GameSurfaceState state, JobHandle dependencies);
	}
}