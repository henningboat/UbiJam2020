using System;
using System.Collections;
using DG.Tweening;
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
		[SerializeField,] private SpriteRenderer _explosionSprite2;
		[SerializeField,] private SpriteRenderer _smokeSprite;
		[SerializeField] private AudioSource _audioClip;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			yield return new WaitForSeconds(_delay);
			_audioClip.Play();
			yield return null;
			yield return null;
			_bombSprite.enabled = false;
			_explosionSprite.enabled = true;
			yield return new WaitForFixedUpdate();
			_explosionSprite.enabled = false;
			_explosionSprite2.enabled = true;
			
			GameSurface.GameSurface.Instance.DestroyCircle(transform.position, _radius);
		
			yield return new WaitForFixedUpdate();
			_explosionSprite2.enabled = false;
			_smokeSprite.enabled = true;
			_smokeSprite.transform.DOScale(_smokeSprite.transform.localScale.x * 2f, 3f);
			_smokeSprite.DOFade(0, 3f);
		}

		#endregion
	}
}