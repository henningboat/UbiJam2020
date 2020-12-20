using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Runtime.PlayerSystem;
using Runtime.UI;
using UnityEngine;
using Player = Photon.Realtime.Player;

namespace Runtime.GameSystem
{
	public class GameManager : PhotonStateMachineSingleton<GameState, GameManager>
	{
		#region Static Stuff

		private static PlayerType[] _selectedPlayerTypes = { PlayerType.PlayerBlue, PlayerType.PlayerYellow, };
		public static int RoundCount { get; private set; }
		public static int[] Score { get; set; }

public const int PlayerCount = 5;

		[RuntimeInitializeOnLoadMethod,]
		public static void InitializeScore()
		{
			Score = new int[PlayerCount];
			RoundCount = 0;
		}

		public static void SetCharacterSelection(int playerID, PlayerType playerType)
		{
			_selectedPlayerTypes[playerID] = playerType;
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

					if (AlivePlayerCount<=1)
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
						else
						{
							return GameState.ReloadLevel;
						}
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
			PhotonNetwork.Instantiate(_selectedPlayerTypes[0].ToString(), PlayerSpawnPoints.Instance.GetForPlayer(PhotonNetwork.LocalPlayer.ActorNumber - 1).position, Quaternion.identity);

			_initialized = true;
		}

		#endregion

		#region RPC

		[PunRPC,]
		private void RPCConfirmPlayerState(GameState state, PhotonMessageInfo info)
		{
			Debug.Log($"state {state} confirmed for player {info.Sender}");
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
}