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
		[SerializeField,] private SpriteRenderer _bombSprite;
		[SerializeField,] private SpriteRenderer _explosionSprite;
		[SerializeField] private AudioSource _audioClip;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			yield return new WaitForSeconds(_delay);
			_audioClip.Play();
			yield return null;
			yield return null;
			yield return null;
			_bombSprite.enabled = false;
			_explosionSprite.enabled = true;
			yield return null;
			_explosionSprite.enabled = false;
			GameSurface.GameSurface.Instance.DestroyCircle(transform.position, _radius);
		}

		#endregion
	}
}