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
		[SerializeField,] private Player _playerAPrefab;
		[SerializeField,] private Player _playerBPrefab;

		#endregion

		#region Properties

		public float Gravity => _gravity;

		#endregion

		#region Public methods

		public Player GetPlayerPrefab(PlayerType playerType)
		{
			switch (playerType)
			{
				case PlayerType.PlayerA:
					return _playerAPrefab;
					break;
				case PlayerType.PlayerB:
					return _playerBPrefab;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(playerType), playerType, null);
			}
		}

		#endregion
	}
}