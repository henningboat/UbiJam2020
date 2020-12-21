using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Runtime.Data;
using Runtime.Multiplayer;
using Runtime.PlayerSystem;
using Runtime.UI;
using UnityEngine;
using Player = Photon.Realtime.Player;

namespace Runtime.GameSystem
{
	public class GameManager : PhotonStateMachineSingleton<GameState, GameManager>
	{
		#region Static Stuff

		public static int RoundCount { get; private set; }
		public static int[] Score { get; set; }

		[RuntimeInitializeOnLoadMethod,]
		public static void InitializeScore()
		{
			Score = new int[8];
			RoundCount = 0;
		}

		public static void DisconnectAndLoadMenu()
		{
			PhotonNetwork.Disconnect();
			MainMenuManager.OpenMainMenu(MainMenuOpenReason.PlayerDisconnected);
		}

		public static void ReloadLevel()
		{
			PhotonNetwork.LoadLevel("MainScene");
		}

		#endregion

		#region Events

		public event Action<int> OnVictory;

		#endregion

		#region Public Fields

		public bool[] _playerLoaded;

		#endregion

		#region Private Fields

		private bool _initialized;
		private Dictionary<Player, GameState> _confirmedState;
		private bool _gameIsOver;

		#endregion

		#region Properties

		private int AlivePlayerCount => Players.Count(player => player.State == PlayerState.Alive);

		private bool AllPlayersReceivedState =>
			_confirmedState.Values.All(state => state == State);

		protected override GameState InitialState => GameState.SceneLoaded;
		public int PlayerCount => GameConfiguration.PlayerCount;
		public List<PlayerSystem.Player> Players { get; } = new List<PlayerSystem.Player>();
		public GameConfiguration GameConfiguration { get; private set; }

		#endregion

		#region Unity methods

		private void Start()
		{
			if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
			{
				if (Application.isEditor)
				{
					MainMenuManager.OpenMainMenu(MainMenuOpenReason.StartOfflineDebugSession);
				}
				else
				{
					MainMenuManager.OpenMainMenu(MainMenuOpenReason.SelfDisconnected);
				}

				return;
			}

			GameConfiguration = GameConfiguration.GetConfigurationFromRoomProperties(PhotonNetwork.CurrentRoom);

			_confirmedState = new Dictionary<Player, GameState>();
			foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
			{
				_confirmedState[player] = GameState.Active;
			}
		}

		public void RegisterPlayer(PlayerSystem.Player player)
		{
			Players.Add(player);
		}

		protected override void Update()
		{
			base.Update();
			if ((PhotonNetwork.CurrentRoom.PlayerCount < PlayerCount) && !Application.isEditor)
			{
				DisconnectAndLoadMenu();
			}
		}

		#endregion

		#region Public methods

		public bool TryGetDeadPlayer(out PlayerSystem.Player deadPlayer)
		{
			foreach (PlayerSystem.Player player in Players)
			{
				if (player.State == PlayerState.Dead)
				{
					deadPlayer = player;
					return true;
				}
			}

			deadPlayer = null;
			return false;
		}

		public bool TryGetWinningPlayer(out PlayerSystem.Player alivePlayer)
		{
			foreach (PlayerSystem.Player player in Players)
			{
				if (player.State == PlayerState.Alive)
				{
					alivePlayer = player;
					return true;
				}
			}

			alivePlayer = null;
			return false;
		}

		public string GetDisplayNameForPlayer(PlayerIdentifier playerIdentifier)
		{
			string displayName;
			if (GameConfiguration.IsLocalMultiplayer)
			{
				displayName = $"Player {playerIdentifier.LocalPlayerID + 1}";
			}
			else
			{
				displayName = PhotonNetwork.CurrentRoom.Players[playerIdentifier.LocalPlayerID].NickName;
			}

			return displayName;
		}

		#endregion

		#region Protected methods

		protected override byte StateToByte(GameState state)
		{
			return (byte) state;
		}

		protected override GameState ByteToState(byte state)
		{
			return (GameState) state;
		}

		protected override GameState GetNextState()
		{
			switch (State)
			{
				case GameState.SceneLoaded:
					return GameState.WaitForOtherPlayers;
				case GameState.WaitForOtherPlayers:
					if (AllPlayersReceivedState)
					{
						return GameState.InitializeGame;
					}

					break;
				case GameState.InitializeGame:
					if (_initialized)
					{
						return GameState.Intro;
					}

					break;
				case GameState.Intro:
					if (IntroManager.Instance.Done)
					{
						return GameState.Active;
					}

					break;
				case GameState.Active:

					if (AlivePlayerCount <= 1)
					{
						return GameState.RoundWon;
					}

					break;
				case GameState.RoundWon:
					if (RoundWonScreen.Instance.IsDone)
					{
						if (_gameIsOver)
						{
							return GameState.LoadMainMenu;
						}

						return GameState.ReloadLevel;
					}

					break;
				case GameState.ReloadLevel:
					break;
				case GameState.LoadMainMenu:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return State;
		}

		protected override void OnStateChange(GameState oldState, GameState newState)
		{
			PhotonView.RPC("RPCConfirmPlayerState", RpcTarget.MasterClient, newState);
			switch (newState)
			{
				case GameState.SceneLoaded:
					break;
				case GameState.WaitForOtherPlayers:
					break;
				case GameState.InitializeGame:
					StartCoroutine(SpawnPlayers());
					break;
				case GameState.Intro:
					break;
				case GameState.Active:

					break;
				case GameState.RoundWon:
					RoundCount++;

					_gameIsOver = false;
					for (int i = 0; i < Score.Length; i++)
					{
						if (Score[i] >= GameSettings.Instance.RoundsToWin)
						{
							RoundWonScreen.Instance.ShowVictoryScreen();
							_gameIsOver = true;
						}
					}

					if (_gameIsOver == false)
					{
						for (int i = 0; i < Score.Length; i++)
						{
							RoundWonScreen.Instance.ShowKOScreen();
						}
					}

					break;
				case GameState.LoadMainMenu:
					DisconnectAndLoadMenu();
					break;
				case GameState.ReloadLevel:
					ReloadLevel();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
			}
		}

		#endregion

		#region Private methods

		private IEnumerator SpawnPlayers()
		{
			yield return null;
			List<PlayerType> localPlayers = GameStartParameters.GetLocallySelectedPlayersFromPlayerProperties(PhotonNetwork.LocalPlayer.CustomProperties);
			for (int i = 0; i < localPlayers.Count; i++)
			{
				PlayerIdentifier playerIdentifier = new PlayerIdentifier(PhotonNetwork.LocalPlayer.ActorNumber, i);
				PlayerType localPlayer = localPlayers[i];
				PhotonNetwork.Instantiate(localPlayer.ToString(), PlayerSpawnPoints.Instance.GetForPlayer(PhotonNetwork.LocalPlayer.ActorNumber - 1).position, Quaternion.identity, 0, new[] { playerIdentifier, });
			}

			_initialized = true;
		}

		#endregion

		#region RPC

		[PunRPC,]
		private void RPCConfirmPlayerState(GameState state, PhotonMessageInfo info)
		{
			_confirmedState[info.Sender] = state;
		}

		[PunRPC,]
		protected override void RPCOnStateChange(byte oldStateByte, byte newStateByte)
		{
			base.RPCOnStateChange(oldStateByte, newStateByte);
		}

		#endregion
	}

	public enum GameState
	{
		SceneLoaded,
		WaitForOtherPlayers,
		InitializeGame,
		Intro,
		Active,
		RoundWon,
		ReloadLevel,
		LoadMainMenu,
	}

	[Flags,]
	public enum GameConfigurationFlags : byte
	{
		None = 0,
		IsLocalMultiplayer = 1 << 0,
	}
}