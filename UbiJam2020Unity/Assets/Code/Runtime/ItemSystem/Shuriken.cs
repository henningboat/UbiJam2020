using System;
using Photon.Pun;
using UnityEngine;

namespace Runtime.ItemSystem
{
	[RequireComponent(typeof(PhotonView))]
	public class Shuriken : MonoBehaviour
	{
		#region Private Fields

		private Vector2 _direction;
		private PhotonView _photonView;
		private Vector2 _cutStartedPosition;
		private float _cutStartedTime;
		private const float _cutDuration = 0.2f;

		#endregion

		#region Unity methods

		private void Awake()
		{
			_photonView = GetComponent<PhotonView>();
			_direction = (Vector2)_photonView.InstantiationData[0];
		}

		private void Update()
		{
			Vector3 previousPosition = transform.position;
			transform.position += Time.deltaTime * (Vector3) _direction;
			if (_photonView.IsMine)
			{
				if (Time.time - _cutStartedTime > _cutDuration)
				{
					GameSurface.GameSurface.Instance.Cut(_cutStartedPosition, transform.position);

					_cutStartedPosition = transform.position;
					_cutStartedTime = Time.time;
				}
			}
		}

		#endregion
	}
}