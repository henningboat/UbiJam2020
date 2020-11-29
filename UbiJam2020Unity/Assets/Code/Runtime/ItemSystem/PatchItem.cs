using Runtime.PlayerSystem;

namespace Runtime.ItemSystem
{
	public class PatchItem : CollectableItemBase
	{
		#region Protected methods

		protected override void ActivateItem(Player player)
		{
			player.GivePatch();
		}

		#endregion
	}
}