using System;
using System.Collections;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public class ItemBomb : ItemBase
	{
		#region Serialize Fields

		[SerializeField,] private float _delay;
		[SerializeField,] private float _radius;
		[SerializeField,] private SpriteRenderer _explosionSprite;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			yield return new WaitForSeconds(_delay);

			_explosionSprite.enabled = true;
			yield return null;
			GameSurface.GameSurface.Instance.DestroyCircle(transform.position, _radius);
			Destroy(gameObject);
		}

		#endregion
	}
}