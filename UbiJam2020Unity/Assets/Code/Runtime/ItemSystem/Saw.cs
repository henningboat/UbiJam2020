using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.ItemSystem
{
	public class Saw : ItemBase
	{
		#region Serialize Fields

		[SerializeField,] private float _startDelay;
		[SerializeField,] private float _moveDuration;

		#endregion

		#region Private Fields

		private bool _startedMoving;
		private Vector2 _lastFramePosition;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			transform.position = (Random.insideUnitCircle.normalized * 5) + new Vector2(5, 5);
			yield return new WaitForSeconds(_startDelay);
			_startedMoving = true;
			GetComponentInChildren<AudioSource>().Play();
			transform.DOMove(-(transform.position - new Vector3(5, 5)) + new Vector3(5, 5), _moveDuration).OnComplete(() => Destroy(gameObject));
		}

		private void Update()
		{
			if (_startedMoving)
			{
				GameSurface.GameSurface.Instance.Cut(transform.position, _lastFramePosition);
			}

			_lastFramePosition = transform.position;
		}

		#endregion
	}
}