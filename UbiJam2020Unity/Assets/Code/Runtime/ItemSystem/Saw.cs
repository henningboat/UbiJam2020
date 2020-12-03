using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public class Saw : ItemBase
	{
		#region Static Stuff

		private const float _cutDuration = 0.1f;

		#endregion

		#region Serialize Fields

		[SerializeField,] private float _startDelay;
		[SerializeField,] private float _moveDuration;

		#endregion

		#region Private Fields

		private bool _startedMoving;
		private Vector2 _cutStartedPosition;
		private float _cutStartedTime;

		#endregion

		#region Unity methods

		private IEnumerator Start()
		{
			transform.position = Vector3.right * 1000;

			if (_photonView.IsMine)
			{
				transform.position = (Random.insideUnitCircle.normalized * 5) + new Vector2(5, 5);
				transform.eulerAngles = Vector3.forward * -Vector2.Angle(Vector2.right, (Vector2) transform.position - new Vector2(5, 5));
			}

			yield return new WaitForSeconds(_startDelay);
			GetComponentInChildren<AudioSource>().Play();

			if (_photonView.IsMine)
			{
				_startedMoving = true;
				_cutStartedPosition = transform.position;
				transform.DOMove(-(transform.position - new Vector3(5, 5)) + new Vector3(5, 5), _moveDuration).OnComplete(Despawn);
			}
		}

		private void Update()
		{
			if (IsMine)
			{
				if (_startedMoving)
				{
					if (Time.time - _cutStartedTime > _cutDuration)
					{
						GameSurface.GameSurface.Instance.Cut(_cutStartedPosition, transform.position);

						_cutStartedPosition = transform.position;
						_cutStartedTime = Time.time;
					}
				}
			}
		}

		#endregion
	}
}