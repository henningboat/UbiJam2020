using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using Runtime.PlayerSystem;
using Runtime.UI;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Player = Photon.Realtime.Player;

namespace Runtime.GameSystem
{
	public class GameManager : PhotonStateMachineSingleton<GameState, GameManager>
	{
		#region Static Stuff

//read from config
		public static int PlayerCount = 2;
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
		public List<PlayerSystem.Player> Players { get; } = new List<PlayerSystem.Player>();

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

			_confirmedState = new Dictionary<Player, GameState>();
			foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
			{
				_confirmedState[player] = GameState.Active;
			}
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

		public void Register(PlayerSystem.Player player)
		{
			Players.Add(player);
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
			Debug.Log("state changed to " + newState);
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
					for (int i = 0; i < Players.Count; i++)
					{
						PlayerSystem.Player player = Players[i];
						if (player.State == PlayerState.Alive)
						{
							Score[player.PlayerID]++;
							OnVictory?.Invoke(player.PlayerID);
						}
					}

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
			throw new NotImplementedException();
//			PhotonNetwork.Instantiate(_selectedPlayerTypes[0].ToString(), PlayerSpawnPoints.Instance.GetForPlayer(PhotonNetwork.LocalPlayer.ActorNumber - 1).position, Quaternion.identity);

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

	public class GameConfiguration
	{
		#region Static Stuff

		public const byte RoundsToWinByte = 0;
		public const byte PlayerCountByte = 1;

		public static GameConfiguration GetConfigurationFromRoomProperties(Room room)
		{
			byte roundsToWin = (byte) room.CustomProperties[RoundsToWinByte];
			byte playerCount = (byte) room.CustomProperties[PlayerCountByte];
			return new GameConfiguration(playerCount, roundsToWin);
		}

		/// <summary>
		/// For random online matches, we force a specific Game Configuration. The goal is that if
		/// two players search for a match online at the same time (which is unlikely), they should always
		/// bet matched together
		/// </summary>
		/// <returns></returns>
		public static GameConfiguration RandomOnlineMatch()
		{
			return new GameConfiguration(2, 7);
		}

		#endregion

		#region Properties

		public byte PlayerCount { get; }
		public byte RoundsToWin { get; }

		#endregion

		#region Constructors

		public GameConfiguration(byte playerCount, byte roundsToWin)
		{
			PlayerCount = playerCount;
			RoundsToWin = roundsToWin;
		}

		#endregion

		#region Public methods

		public Hashtable GetRoomProperties()
		{
			Hashtable roomProperties = new Hashtable();
			roomProperties.Add(RoundsToWinByte, RoundsToWin);
			roomProperties.Add(PlayerCountByte, PlayerCount);
			return roomProperties;
		}

		#endregion
	}
}