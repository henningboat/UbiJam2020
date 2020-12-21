using System.Collections.Generic;
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

		public static GameStartParameters ConnectDebugSinglePlayerMatch()
		{
			return new GameStartParameters(GameStartType.DebugSinglePlayer)
			       {
				       LocallySelectedCharacters = new List<PlayerType> { PlayerType.PlayerBlue, },
				       GameConfiguration = new GameConfiguration(1, 3),
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
			options.MaxPlayers = GameConfiguration.PlayerCount;
			options.CustomRoomProperties = GameConfiguration.GetRoomProperties();
			return options;
		}

		public void SetCharacters(List<PlayerType> toList)
		{
			LocallySelectedCharacters = toList;
		}

		#endregion
	}
}