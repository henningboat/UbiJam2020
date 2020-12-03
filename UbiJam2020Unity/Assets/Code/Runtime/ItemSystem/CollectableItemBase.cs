using DG.Tweening;
using Photon.Pun;
using Runtime.GameSystem;
using Runtime.PlayerSystem;
using UnityEngine;
using Player = Photon.Realtime.Player;

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

			if(IsMine){
				foreach (var player in GameManager.Instance.Players)
				{
					if (Vector2.Distance(player.transform.position, transform.position) < GameSettings.Instance.ItemCollectionDistance)
					{
						_collected = true;
						_photonView.RPC("RPCActivateItem", RpcTarget.AllViaServer, PhotonNetwork.LocalPlayer);
						break;
					}
				}
			}
		}

		protected override void Despawn()
		{
			transform.DOScale(0, 0.2f).OnComplete(() => base.Despawn());
		}

		#endregion

		#region Protected methods

		protected abstract void RPCActivateItem(Player photonPlayer);

		#endregion
	}
}