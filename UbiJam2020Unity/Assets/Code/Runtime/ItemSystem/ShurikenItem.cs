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

		protected override void ActivateItem(Player player)
		{
			SpawnShuriken(player, new Vector3(_distanceFromPlayer, _distanceFromPlayer));
			SpawnShuriken(player, new Vector3(-_distanceFromPlayer, _distanceFromPlayer));
			SpawnShuriken(player, new Vector3(_distanceFromPlayer, -_distanceFromPlayer));
			SpawnShuriken(player, new Vector3(-_distanceFromPlayer, -_distanceFromPlayer));
			AudioSource audioSource = GetComponentInChildren<AudioSource>();
			audioSource.transform.SetParent(null);
			audioSource.Play();
		}

		#endregion

		#region Private methods

		private void SpawnShuriken(Player player, Vector3 offset)
		{
			Instantiate(_shurikenPrefab, player.transform.position + offset, Quaternion.identity).SetSpeed(_speed, offset.normalized);
		}

		#endregion
	}
}