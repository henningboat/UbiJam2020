using DG.Tweening;
using Runtime.GameSystem;
using Runtime.PlayerSystem;
using UnityEngine;

namespace Runtime.ItemSystem
{
	public abstract class CollectableItemBase : ItemBase
	{
		#region Private Fields

		private bool _collected;

		#endregion

		#region Unity methods

		private void Update()
		{
			if (_collected)
			{
				return;
			}

			foreach (var player in GameManager.Instance.Players)
			{
				if (Vector2.Distance(player.transform.position, transform.position) < GameSettings.Instance.ItemCollectionDistance)
				{
					_collected = true;
					ActivateItem(player);
					transform.DOScale(0, 0.2f).OnComplete(() => Destroy(gameObject));
				}
			}
		}

		#endregion

		#region Protected methods

		protected abstract void ActivateItem(Player player);

		#endregion
	}
}