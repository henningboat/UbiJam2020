using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Runtime.GameSystem
{
	public class GameConfiguration
	{
		#region Static Stuff

		public const string RoundsToWinIdentifier = "R";
		public const string PlayerCountIdentifier = "P";
		public const string ConfigurationFlagsIdentifier = "F";

		public static GameConfiguration GetConfigurationFromRoomProperties(Room room)
		{
			byte roundsToWin = (byte) room.CustomProperties[RoundsToWinIdentifier];
			byte playerCount = (byte) room.CustomProperties[PlayerCountIdentifier];
			GameConfigurationFlags configurationFlags = (GameConfigurationFlags) room.CustomProperties[ConfigurationFlagsIdentifier];
			return new GameConfiguration(playerCount, roundsToWin, configurationFlags);
		}

		/// <summary>
		///     For random online matches, we force a specific Game Configuration. The goal is that if
		///     two players search for a match online at the same time (which is unlikely), they should always
		///     bet matched together
		/// </summary>
		/// <returns></returns>
		public static GameConfiguration RandomOnlineMatch()
		{
			return new GameConfiguration(3, 7, GameConfigurationFlags.None);
		}

		#endregion

		#region Private Fields

		private GameConfigurationFlags _configurationFlags;

		#endregion

		#region Properties

		public bool IsLocalMultiplayer => _configurationFlags.HasFlag(GameConfigurationFlags.IsLocalMultiplayer);
		public byte PlayerCount { get; }
		public byte RoundsToWin { get; }

		#endregion

		#region Constructors

		public GameConfiguration(byte playerCount, byte roundsToWin, GameConfigurationFlags configurationFlags)
		{
			PlayerCount = playerCount;
			RoundsToWin = roundsToWin;
			_configurationFlags = configurationFlags;
		}

		#endregion

		#region Public methods

		public Hashtable GetRoomProperties()
		{
			Hashtable roomProperties = new Hashtable();
			roomProperties.Add(RoundsToWinIdentifier, RoundsToWin);
			roomProperties.Add(PlayerCountIdentifier, PlayerCount);
			roomProperties.Add(ConfigurationFlagsIdentifier, _configurationFlags);
			return roomProperties;
		}

		#endregion
	}
}