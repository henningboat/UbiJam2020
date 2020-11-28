using Runtime.Utils;
using UnityEngine;

namespace Runtime.GameSystem
{
	public class GameSettings : Singleton<GameSettings>
	{
		#region Serialize Fields

		[SerializeField,] private float _gravity = 4;

		#endregion

		#region Properties

		public float Gravity => _gravity;

		#endregion
	}
}