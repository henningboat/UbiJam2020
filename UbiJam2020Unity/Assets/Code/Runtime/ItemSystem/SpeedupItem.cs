using Runtime.PlayerSystem;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public class SpeedupItem : CollectableItemBase
	{
		[SerializeField] private float _speedMultiplier;
		[SerializeField] private float _duration;
		
		protected override void ActivateItem(Player player)
		{
			player.SetSpeedMultiplier(_speedMultiplier,_duration);
			AudioSource audioSource = GetComponentInChildren<AudioSource>();
			audioSource.transform.SetParent(null);
			audioSource.Play();
		}
	}
}