using System;
using Runtime.PlayerSystem;
using UnityEditor.Rendering;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public class SpeedupItem : CollectableItemBase
	{
		[SerializeField] private float _speedMultiplier;
		[SerializeField] private float _duration;
		[SerializeField] private float _blinkSpeed = 10;
		[SerializeField] private SpriteRenderer _blinkingSprite;

		protected override void ActivateItem(Player player)
		{
			player.SetSpeedMultiplier(_speedMultiplier,_duration);
			AudioSource audioSource = GetComponentInChildren<AudioSource>();
			audioSource.transform.SetParent(null);
			audioSource.Play();
		}

		private void LateUpdate()
		{
			_blinkingSprite.color = new Color(1, 1, 1, Mathf.Sin(Time.time * _blinkSpeed));
		}
	}
}