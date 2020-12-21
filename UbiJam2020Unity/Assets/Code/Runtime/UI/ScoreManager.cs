using System;
using System.Collections.Generic;
using Photon.Pun;
using Runtime.Data;
using Runtime.Multiplayer;
using Runtime.PlayerSystem;
using Runtime.Utils;
using Player = Photon.Realtime.Player;

namespace Runtime.UI
{
	public class ScoreManager : Singleton<ScoreManager>
	{
		private ScoreDisplayPanel[] _scoreDisplayPanels;
		private int _activatedScorePanels = 0;
		
		
		protected override void Awake()
		{
			base.Awake();
			_scoreDisplayPanels = GetComponentsInChildren<ScoreDisplayPanel>();
		}

		private void Start()
		{
			ActivateScorePanelsForPlayer(PhotonNetwork.LocalPlayer);
			foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
			{
				if (!player.IsLocal)
				{
					ActivateScorePanelsForPlayer(player);
				}
			}
		}

		private void ActivateScorePanelsForPlayer(Player localPlayer)
		{
			var playersSelectedForPhotonPlayer = GameStartParameters.GetLocallySelectedPlayersFromPlayerProperties(localPlayer.CustomProperties);
			for (int i = 0; i < playersSelectedForPhotonPlayer.Count; i++)
			{
				var identifier = new PlayerIdentifier(localPlayer.ActorNumber, i);
				var playerType = playersSelectedForPhotonPlayer[i];
				ScoreDisplayPanel scoreDisplayPanel = _scoreDisplayPanels[_activatedScorePanels];
				scoreDisplayPanel.Initialize(identifier,playerType);
			}
		}
	}
}