using Photon.Pun;
using UnityEngine;

namespace Runtime.ItemSystem
{
	[RequireComponent(typeof(PhotonView)),]
	public abstract class ItemBase : MonoBehaviour
	{
		#region Protected Fields

		protected PhotonView _photonView;

		#endregion

		#region Properties

		public bool IsMine => _photonView.IsMine;

		#endregion

		#region Unity methods

		protected virtual void Awake()
		{
			_photonView = GetComponent<PhotonView>();
		}

		#endregion

		#region Protected methods

		protected virtual void Despawn()
		{
			if (IsMine)
			{
				PhotonNetwork.Destroy(gameObject);
			}
		}

		#endregion
	}
}