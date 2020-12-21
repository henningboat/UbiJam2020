using ExitGames.Client.Photon;
using UnityEngine;

namespace Runtime.Data
{
	public class PlayerIdentifier
	{
		#region Static Stuff

		//this can be optimized to only use one byte, but it does not matter much because we don't send player identifiers often
		[RuntimeInitializeOnLoadMethod,]
		private static void InitializeSerialization()
		{
			PhotonPeer.RegisterType(typeof(PlayerIdentifier), CustomSerializationIDs.PlayerIdentifierID, Serialize, Deserialize);
		}

		private static object Deserialize(byte[] data)
		{
			return new PlayerIdentifier(data[0], data[1]);
		}

		private static byte[] Serialize(object data)
		{
			PlayerIdentifier playerIdentifier = (PlayerIdentifier) data;
			return new[]
			       {
				       playerIdentifier.PhotonPlayerActorNumber,
				       playerIdentifier.LocalPlayerID,
			       };
		}

		#endregion

		#region Public Fields

		public readonly byte LocalPlayerID;
		public readonly byte PhotonPlayerActorNumber;

		#endregion

		#region Constructors

		public PlayerIdentifier(int photonPlayerActorNumber, int localPlayerID)
		{
			LocalPlayerID = (byte) localPlayerID;
			PhotonPlayerActorNumber = (byte) photonPlayerActorNumber;
		}

		#endregion
	}
}