using Photon.Pun;
using Runtime.PlayerSystem;

namespace Runtime.ItemSystem
{
	public class PatchItem : CollectableItemBase
	{
		#region Protected methods

		#endregion

		[PunRPC]
		protected override void RPCActivateItem(PhotonView playerPhotonView)
		{
			if (IsMine)
			{
				playerPhotonView.GetComponent<Player>().GivePatch();
			}
			Despawn();
		}
	}
}