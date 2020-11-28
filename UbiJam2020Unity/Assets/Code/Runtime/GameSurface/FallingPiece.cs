using Runtime.GameSystem;
using UnityEngine;

namespace Runtime.GameSurface
{
	public class FallingPiece : MonoBehaviour
	{
		#region Private Fields

		private float _velocity;
		private Texture2D _mask;

		#endregion

		#region Unity methods

		private void Update()
		{
			_velocity += GameSettings.Instance.Gravity * Time.deltaTime;
			transform.position += Vector3.forward * _velocity * Time.deltaTime;

			if (transform.position.z > 30)
			{
				Destroy(_mask);
				Destroy(gameObject);
			}
		}

		#endregion

		#region Public methods

		public void SetMask(Texture2D mask)
		{
			_mask = mask;
			GetComponent<Renderer>().material.SetTexture("_Mask", mask);
		}

		#endregion
	}
}