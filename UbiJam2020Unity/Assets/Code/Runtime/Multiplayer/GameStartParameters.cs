using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Realtime;
using Runtime.GameSystem;
using Runtime.PlayerSystem;

namespace Runtime.Multiplayer
{
	public class GameStartParameters
	{
		#region GameStartType enum

		public enum GameStartType
		{
			LocalMultiplayer,
			HostPrivateMatch,
			JoinPrivateMatch,
			JoinRandomMatch,
			DebugSinglePlayer,
		}

		#endregion

		#region Static Stuff

		private const string LocallySelectedPlayersList = "P";

		public static GameStartParameters ConnectDebugSinglePlayerMatch()
		{
			return new GameStartParameters(GameStartType.DebugSinglePlayer)
			       {
				       LocallySelectedCharacters = new List<PlayerType> { PlayerType.PlayerBlue, PlayerType.PlayerPink, },
				       GameConfiguration = new GameConfiguration(2, 3, GameConfigurationFlags.IsLocalMultiplayer),
			       };
		}

		#endregion

		#region Public Fields

		public string GameCode;
		public GameConfiguration GameConfiguration;
		public GameStartType Type;

		#endregion

		#region Properties

		public List<PlayerType> LocallySelectedCharacters { get; private set; }

		#endregion

		#region Constructors

		public GameStartParameters(GameStartType type)
		{
			Type = type;
		}

		#endregion

		#region Public methods

		public RoomOptions GetRoomOptions()
		{
			RoomOptions options = new RoomOptions();
			options.IsVisible = Type == GameStartType.JoinRandomMatch;
			if (Type == GameStartType.DebugSinglePlayer)
			{
				options.MaxPlayers = 1;
			}
			else
			{
				options.MaxPlayers = GameConfiguration.PlayerCount;
			}

			options.CustomRoomProperties = GameConfiguration.GetRoomProperties();
			return options;
		}

		public void SetCharacters(List<PlayerType> toList)
		{
			LocallySelectedCharacters = toList;
		}

		public Hashtable GetLocalPlayerCustomProperties()
		{
			return new Hashtable
			       {
				       [LocallySelectedPlayersList] = LocallySelectedCharacters.Select(type => (byte) type).ToArray(),
			       };
		}

		#endregion

		public static List<PlayerType> GetLocallySelectedPlayersFromPlayerProperties(Hashtable localPlayerCustomProperties)
		{
			byte[] localPlayerCustomProperty = (byte[]) (localPlayerCustomProperties[LocallySelectedPlayersList]);
			return localPlayerCustomProperty.Select(b => (PlayerType) b).ToList();
		}
	}
}