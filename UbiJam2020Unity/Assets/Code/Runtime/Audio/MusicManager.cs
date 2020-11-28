using System.Collections;
using Runtime.GameSystem;
using UnityEngine;

namespace Runtime.Audio
{
	public class MusicManager : MonoBehaviour
	{
		#region Serialize Fields

		[SerializeField,] private AudioSource[] _musicTracks;

		#endregion

		#region Unity methods

		private void Start()
		{
			_musicTracks[Random.Range(0, _musicTracks.Length)].Play();
		}

		#endregion
	}
}