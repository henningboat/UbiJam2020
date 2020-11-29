﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Runtime.PlayerSystem;

namespace Runtime.GameSystem
{
	public class GameManager : StateMachineSingleton<GameState, GameManager>
	{
		#region Static Stuff

		private static PlayerType[] _selectedPlayerTypes = { PlayerType.PlayerBlue, PlayerType.PlayerYellow, };

		#endregion

		#region Properties

		protected override GameState InitialState => GameState.InitializeGame;

		#endregion

		private bool _initialized;
		public List<Player> Players { get; private set; }

		#region Protected methods

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
					
					if (AlivePlayerCount <=1)
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
					RoundWonScreen.Instance.Show();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
			}
		}

		private int AlivePlayerCount => Players.Count(player => player.State == PlayerState.Alive);

		private IEnumerator SpawnPlayers()
		{
			yield return null;
			Players=new List<Player>();
			for (int i = 0; i < _selectedPlayerTypes.Length; i++)
			{
				var spawnPoint = PlayerSpawnPoints.Instance.GetForPlayer(i);
				Player playerPrefab = GameSettings.Instance.GetPlayerPrefab(_selectedPlayerTypes[i]);
				var playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
				playerInstance.SetPlayerID(i);
				Players.Add(playerInstance);
			}

			_initialized = true;
		}

		#endregion
	}

	public enum GameState
	{
		InitializeGame,
		Intro,
		Active,
		RoundWon,
	}
}