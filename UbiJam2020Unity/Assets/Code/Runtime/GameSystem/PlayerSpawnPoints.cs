using System.Collections.Generic;
using Runtime.Utils;
using UnityEngine;

namespace Runtime.GameSystem
{
	public class PlayerSpawnPoints : Singleton<PlayerSpawnPoints>
	{
		#region Serialize Fields

		[SerializeField,] private List<Transform> _spawnPoints;

		#endregion

		#region Public methods

		public Transform GetForPlayer(int i)
		{
			return _spawnPoints[i];
		}

		#endregion
	}
}