using UnityEngine;

namespace Runtime.SaveDataSystem
{
	public static class SaveData
	{
		#region Static Stuff

		private const string NickNameKey = "PlayerNickName";
		public static string NickName => PlayerPrefs.GetString(NickNameKey);
		public static bool HasNickName => PlayerPrefs.HasKey(NickNameKey);

		public static void SetNickName(string newNickName)
		{
			PlayerPrefs.SetString(NickNameKey, newNickName);
		}

		#endregion
	}
}