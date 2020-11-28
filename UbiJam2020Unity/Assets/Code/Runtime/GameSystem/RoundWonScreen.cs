using System.Collections;
using System.Collections.Generic;
using Runtime.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime.GameSystem
{
	public class RoundWonScreen:Singleton<RoundWonScreen>
	{
		public void Show()
		{
			StartCoroutine(ReloadLevel());
		}

		private IEnumerator ReloadLevel()
		{
			yield return new WaitForSeconds(2);
			SceneManager.LoadScene(0);
		}
	}
}