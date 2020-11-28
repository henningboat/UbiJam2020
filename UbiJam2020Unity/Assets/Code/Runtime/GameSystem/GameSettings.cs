using System;
using Runtime.PlayerSystem;
using Runtime.Utils;
using UnityEngine;

namespace Runtime.GameSystem
{
	public class GameSettings : Singleton<GameSettings>
	{
		#region Serialize Fields

		[SerializeField,] private float _gravity = 4;
		[SerializeField,] private Player _playerBluePrefab;
		[SerializeField,] private Player _playerYellowPrefab;
		[SerializeField,] private Player _playerPinkPrefab;
		[SerializeField,] private Player _playerOrangePrefab;

		#endregion

		#region Properties

		public float Gravity => _gravity;

		#endregion

		#region Public methods

		public Player GetPlayerPrefab(PlayerType playerType)
		{
			switch (playerType)
			{
				case PlayerType.PlayerBlue:
					return _playerBluePrefab;
					break;
				case PlayerType.PlayerYellow:
					return _playerYellowPrefab;
					break;
				case PlayerType.PlayerPink:
					return _playerPinkPrefab;
					break;
				case PlayerType.PlayerOrange:
					return _playerOrangePrefab;
				default:
					throw new ArgumentOutOfRangeException(nameof(playerType), playerType, null);
			}
		}

		#endregion
	}
}