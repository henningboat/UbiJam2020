using Photon.Pun;
using Runtime.GameSurfaceSystem;
using UnityEngine;

namespace Runtime.ItemSystem
{
	[RequireComponent(typeof(PhotonView)),]
	public class Shuriken : MonoBehaviour
	{
		#region Static Stuff

		private const float CutDuration = 0.2f;

		#endregion

		#region Private Fields

		private Vector2 _direction;
		private PhotonView _photonView;
		private Vector2 _cutStartedPosition;
		private float _cutStartedTime;

		#endregion

		#region Unity methods

		private void Awake()
		{
			_photonView = GetComponent<PhotonView>();
			_direction = (Vector2) _photonView.InstantiationData[0];
			_cutStartedPosition = transform.position;
		}

		private void Update()
		{
			transform.position += Time.deltaTime * (Vector3) _direction;
			if (_photonView.IsMine)
			{
				if (Time.time - _cutStartedTime > CutDuration)
				{
					GameSurface.Instance.Cut(_cutStartedPosition, transform.position);

					_cutStartedPosition = transform.position;
					_cutStartedTime = Time.time;
				}
			}
		}

		#endregion
	}
}