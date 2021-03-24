using System.ComponentModel.Design;
using Photon.Pun;
using Runtime.PlayerSystem;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public class SpeedupItem : CollectableItemBase
	{
		#region Serialize Fields

		[SerializeField,] private float _speedMultiplier;
		[SerializeField,] private float _duration;
		[SerializeField,] private float _blinkSpeed = 10;
		[SerializeField,] private SpriteRenderer _blinkingSprite;

		#endregion

		#region Unity methods

		private void LateUpdate()
		{
			_blinkingSprite.color = new Color(1, 1, 1, Mathf.Sin(Time.time * _blinkSpeed));
		}

		#endregion

		#region Protected methods

		[PunRPC]
		protected override void RPCActivateItem(Photon.Realtime.Player photonPlayer)
		{
			if (IsMine)
			{
				Player player = 	PlayerSystem.Player.GetFromPhotonPlayer(photonPlayer);
				player.SetSpeedMultiplier(_speedMultiplier, _duration);
			}

			AudioSource audioSource = GetComponentInChildren<AudioSource>();
			audioSource.transform.SetParent(null);
			audioSource.Play();
			Despawn();
		}

		#endregion
	}
}