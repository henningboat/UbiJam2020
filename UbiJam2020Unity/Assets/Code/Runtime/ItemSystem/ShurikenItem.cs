using Photon.Pun;
using Runtime.PlayerSystem;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public class ShurikenItem : CollectableItemBase
	{
		#region Serialize Fields

		[SerializeField,] private float _distanceFromPlayer = 2;
		[SerializeField,] private float _speed = 4;
		[SerializeField,] private Shuriken _shurikenPrefab;

		#endregion

		#region Protected methods

		[PunRPC,]
		protected override void RPCActivateItem(Photon.Realtime.Player photonPlayer)
		{
			if (IsMine)
			{
				Player player = 	PlayerSystem.Player.GetFromPhotonPlayer(photonPlayer);
				SpawnShuriken(player, new Vector3(_distanceFromPlayer, _distanceFromPlayer));
				SpawnShuriken(player, new Vector3(-_distanceFromPlayer, _distanceFromPlayer));
				SpawnShuriken(player, new Vector3(_distanceFromPlayer, -_distanceFromPlayer));
				SpawnShuriken(player, new Vector3(-_distanceFromPlayer, -_distanceFromPlayer));
				AudioSource audioSource = GetComponentInChildren<AudioSource>();
				audioSource.transform.SetParent(null);
				audioSource.Play();
			}
			
			Despawn();
		}

		#endregion

		#region Private methods

		private void SpawnShuriken(Player player, Vector3 offset)
		{
			PhotonNetwork.Instantiate(_shurikenPrefab.name, player.transform.position + offset, Quaternion.identity, default, new object[] { (Vector2) (offset.normalized * _speed), });
		}

		#endregion
	}
}