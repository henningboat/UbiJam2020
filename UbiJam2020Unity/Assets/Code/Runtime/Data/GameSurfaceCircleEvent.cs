using ExitGames.Client.Photon;
using Runtime.GameSurfaceSystem;
using Runtime.GameSurfaceSystem.Jobs;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Runtime.Data
{
	public class GameSurfaceCircleEvent : IGameSurfaceEvent
	{
		#region Static Stuff

		private const byte RadiusBitMask = 127;
		private const byte HealSurfaceBitMask = 128;
		private const byte SerializationTypeID = 1;

		[RuntimeInitializeOnLoadMethod,]
		private static void InitializeSerialization()
		{
			PhotonPeer.RegisterType(typeof(GameSurfaceCircleEvent), SerializationTypeID, Serialize, Deserialize);
		}

		public static object Deserialize(byte[] data)
		{
			byte x = data[0];
			byte y = data[1];
			Vector3 position = GameSurface.GridPositionToWorldPosition(new Vector2Int(x, y));

			float radiusBitMask = (data[2] & RadiusBitMask) / GameSurface.Size;
			bool healSurface = (data[2] & HealSurfaceBitMask) != 0;
			return new GameSurfaceCircleEvent
			       {
				       WorldPosition = position,
				       Radius = radiusBitMask,
				       HealSurface = healSurface,
			       };
		}

		private static byte[] Serialize(object customType)
		{
			GameSurfaceCircleEvent gameSurfaceCircleEvent = (GameSurfaceCircleEvent) customType;

			byte normalizedRadius = (byte) math.clamp(gameSurfaceCircleEvent.Radius * GameSurface.Size, 0, RadiusBitMask);
			byte packedByte = normalizedRadius;
			if (gameSurfaceCircleEvent.HealSurface)
			{
				packedByte |= HealSurfaceBitMask;
			}

			Vector2Int gridPosition = GameSurface.WorldSpaceToGrid(gameSurfaceCircleEvent.WorldPosition);

			return new[]
			       {
				       (byte) math.clamp(gridPosition.x, 0, 255),
				       (byte) math.clamp(gridPosition.y, 0, 255),
				       packedByte,
			       };
		}

		#endregion

		#region Properties

		public bool HealSurface { get; private set; }
		public float Radius { get; private set; }
		public Vector2 WorldPosition { get; private set; }

		#endregion

		#region Constructors

		private GameSurfaceCircleEvent()
		{
		}

		public GameSurfaceCircleEvent(Vector2 position, float radius, bool healSurface)
		{
			WorldPosition = position;
			Radius = radius;
			HealSurface = healSurface;
		}

		#endregion

		#region IGameSurfaceEvent Members

		public JobHandle ScheduleJob(GameSurfaceState state, JobHandle dependencies)
		{
			JSpawnCircleSurface job = new JSpawnCircleSurface
			                          {
				                          Radius = Radius,
				                          Position = WorldPosition,
				                          Surface = state.Surface,
				                          OverwriteState = HealSurface ? SurfaceState.Intact : SurfaceState.Destroyed,
			                          };
			return job.Schedule(GameSurface.SurfacePieceCount, dependencies);
		}

		#endregion
	}
}