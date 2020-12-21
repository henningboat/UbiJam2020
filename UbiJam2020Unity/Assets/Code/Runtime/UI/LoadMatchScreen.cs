using Runtime.Multiplayer;

namespace Runtime.UI
{
	public class LoadMatchScreen : MainMenuScreenBase<LoadMatchScreen>, IMainMenuScreenBase
	{
		public override void Show()
		{
			base.Show();
			Lobby.Instance.Connect(MainMenuManager.Instance.GameStartParameters);
		}
	}
}