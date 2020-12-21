using System;
using Photon.Pun;
using Photon.Realtime;
using Runtime.GameSystem;
using Runtime.SaveDataSystem;
using UnityEngine;
using Player = Photon.Realtime.Player;

namespace Runtime.Multiplayer
{
	public class Lobby : MonoBehaviourPunCallbacks
	{
		#region Static Stuff

		public static Lobby Instance { get; private set; }

		private static void CheckRoomJoined()
		{
			if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
			{
				PhotonNetwork.CurrentRoom.IsOpen = false;
				PhotonNetwork.LoadLevel("MainScene");
			}
		}

		#endregion

		#region Private Fields

		private GameStartParameters _startParameters;

		#endregion

		#region Unity methods

		private void Awake()
		{
			Instance = this;
		}

		#endregion

		#region Public methods

		public void Connect(GameStartParameters startParameters)
		{
			_startParameters = startParameters;
			PhotonNetwork.NickName = SaveData.NickName;
			PhotonNetwork.AutomaticallySyncScene = true;
			PhotonNetwork.SetPlayerCustomProperties(startParameters.GetLocalPlayerCustomProperties());
			PhotonNetwork.OfflineMode = _startParameters.Type == GameStartParameters.GameStartType.LocalMultiplayer;
			
			PhotonNetwork.ConnectUsingSettings();
		}

		public override void OnConnectedToMaster()
		{
			switch (_startParameters.Type)
			{
				case GameStartParameters.GameStartType.LocalMultiplayer:
					PhotonNetwork.CreateRoom("", _startParameters.GetRoomOptions());
					break;
				case GameStartParameters.GameStartType.HostPrivateMatch:
					PhotonNetwork.CreateRoom(_startParameters.GameCode, _startParameters.GetRoomOptions());
					break;
				case GameStartParameters.GameStartType.JoinPrivateMatch:
					PhotonNetwork.JoinRoom(_startParameters.GameCode);
					break;
				case GameStartParameters.GameStartType.JoinRandomMatch:
					PhotonNetwork.JoinOrCreateRoom("Public Match", _startParameters.GetRoomOptions(), TypedLobby.Default);
					break;
				case GameStartParameters.GameStartType.DebugSinglePlayer:
					PhotonNetwork.JoinOrCreateRoom("DebugMatch", _startParameters.GetRoomOptions(), TypedLobby.Default);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
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

		public void ConnectOffline()
		{
			Lobby.Instance.Connect(GameStartParameters.ConnectDebugSinglePlayerMatch());
		}

		#endregion
	}
}