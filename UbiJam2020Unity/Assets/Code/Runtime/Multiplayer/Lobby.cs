using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Runtime.Multiplayer
{
	public class Lobby : MonoBehaviourPunCallbacks
	{
		#region Static Stuff

		public static Lobby Instance { get; private set; }

		private static void CheckRoomJoined()
		{
			if (PhotonNetwork.IsMasterClient && ((PhotonNetwork.CurrentRoom.PlayerCount == 2) || Application.isEditor))
			{
				PhotonNetwork.CurrentRoom.IsOpen = false;
				PhotonNetwork.LoadLevel("MainScene");
			}
		}

		#endregion

		#region Unity methods

		private void Awake()
		{
			Instance = this;
		}

		#endregion

		#region Public methods

		public void Connect()
		{
			PhotonNetwork.AutomaticallySyncScene = true;
			PhotonNetwork.ConnectUsingSettings();
		}

		public override void OnConnectedToMaster()
		{
			RoomOptions roomOptions = new RoomOptions();
			roomOptions.MaxPlayers = 2;
			EnterRoomParams enterRoomParams = new EnterRoomParams();
			enterRoomParams.RoomOptions = roomOptions;
			PhotonNetwork.JoinOrCreateRoom("DefaultRoom", roomOptions, TypedLobby.Default);
		}

		public override void OnJoinedRoom()
		{
			base.OnJoinedRoom();
			CheckRoomJoined();
		}

		public override void OnPlayerEnteredRoom(Player newPlayer)
		{
			base.OnPlayerEnteredRoom(newPlayer);
			CheckRoomJoined();
		}

		#endregion
	}
}