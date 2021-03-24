using Photon.Pun;
using Runtime.PlayerSystem;

namespace Runtime.ItemSystem
{
	public class PatchItem : CollectableItemBase
	{
		#region Protected methods

		#endregion

		[PunRPC]
		protected override void RPCActivateItem(Photon.Realtime.Player photonPlayer)
		{
			if (IsMine)
			{
				Player player = Player.GetFromPhotonPlayer(photonPlayer);
				player.GivePatch();
			}
			Despawn();
		}
	}
}