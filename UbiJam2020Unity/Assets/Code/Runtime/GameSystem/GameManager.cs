using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Runtime.PlayerSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime.GameSystem
{
	public class GameManager : PhotonStateMachineSingleton<GameState, GameManager>
	{
		#region Static Stuff

		private static PlayerType[] _selectedPlayerTypes = { PlayerType.PlayerBlue, PlayerType.PlayerYellow, };
		public static int RoundCount { get; private set; }
		public static int[] Score { get; set; }

		public bool[] _playerLoaded;

		[RuntimeInitializeOnLoadMethod,]
		public static void InitializeScore()
		{
			Score = new int[2];
			RoundCount = 0;
		}

		public static void SetCharacterSelection(int playerID, PlayerType playerType)
		{
			_selectedPlayerTypes[playerID] = playerType;
		}

		#endregion

		#region Events

		public event Action<int> OnVictory;

		#endregion

		#region Private Fields

		private bool _initialized;

		#endregion

		#region Properties

		private int AlivePlayerCount => Players.Count(player => player.State == PlayerState.Alive);
		protected override GameState InitialState => GameState.InitializeGame;
		public List<Player> Players { get; private set; } = new List<Player>();

		#endregion

		#region Public methods

		public bool TryGetDeadPlayer(out Player deadPlayer)
		{
			foreach (Player player in Players)
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

		public bool TryGetWinningPlayer(out Player alivePlayer)
		{
			foreach (Player player in Players)
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

		#endregion

		#region Protected methods

		[PunRPC,]
		protected override void RPCOnStateChange(byte oldStateByte, byte newStateByte)
		{
			base.RPCOnStateChange(oldStateByte, newStateByte);
		}

		protected override byte StateToByte(GameState state)
		{
			return (byte) state;
		}

		protected override GameState ByteToState(byte state)
		{
			return (GameState) state;
		}

		protected override void Update()
		{
			base.Update();
			if (PhotonNetwork.CurrentRoom.PlayerCount < 2 && !UnityEngine.Application.isEditor)
			{
				DisconnectAndLoadMenu();
			}
		}

		public static void DisconnectAndLoadMenu()
		{
			PhotonNetwork.Disconnect();
			SceneManager.LoadScene(0);
		}

		protected override GameState GetNextState()
		{
			switch (State)
			{
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

					if (Players.Any(player => player.State == PlayerState.Dead))
					{
						return GameState.RoundWon;
					}

					break;
				case GameState.RoundWon:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return State;
		}

		protected override void OnStateChange(GameState oldState, GameState newState)
		{
			Debug.Log("state changed to " + newState);
			switch (newState)
			{
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
						Player player = Players[i];
						if (player.State == PlayerState.Alive)
						{
							Score[player.PlayerID]++;
							OnVictory?.Invoke(player.PlayerID);
						}
					}

					RoundCount++;

					bool playerWon = false;
					for (int i = 0; i < Score.Length; i++)
					{
						if (Score[i] >= GameSettings.Instance.RoundsToWin)
						{
							RoundWonScreen.Instance.ShowVictoryScreen();
							playerWon = true;
						}
					}

					if (playerWon == false)
					{
						for (int i = 0; i < Score.Length; i++)
						{
							RoundWonScreen.Instance.ShowKOScreen();
						}
					}

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

		public void Register(Player player)
		{
			Players.Add(player);
		}
	}

	public enum GameState
	{
		WaitForOtherPlayers,
		InitializeGame,
		Intro,
		Active,
		RoundWon,
	}
}