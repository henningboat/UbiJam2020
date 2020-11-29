using System;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public class Shuriken : MonoBehaviour
	{
		#region Private Fields

		private float _speed;
		private Vector2 _direction;

		#endregion

		#region Unity methods

		private void Update()
		{
			Vector3 previousPosition = transform.position;
			transform.position += _speed * Time.deltaTime * (Vector3) _direction;
			GameSurface.GameSurface.Instance.Cut(previousPosition, transform.position);
		}

		#endregion

		#region Public methods

		public void SetSpeed(float speed, Vector2 direction)
		{
			_speed = speed;
			_direction = direction;
		}

		#endregion
	}
}