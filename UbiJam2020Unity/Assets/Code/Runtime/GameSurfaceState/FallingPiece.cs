using Runtime.GameSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.GameSurfaceState
{
	public class FallingPiece : MonoBehaviour
	{
		#region Private Fields

		private float _velocity;
		private Texture2D _mask;

		#endregion

		#region Unity methods

		private void OnEnable()
		{
			var audioSources = GetComponentsInChildren<AudioSource>();
			audioSources[Random.Range(0, audioSources.Length)].Play();
		}

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

		public void SetMaterial(Texture2D mask,Material originalMaterial)
		{
			_mask = mask;
			Renderer renerer = GetComponent<Renderer>();
			renerer.material.CopyPropertiesFromMaterial(originalMaterial);
			renerer.material.SetTexture("_Mask", mask);
		}

		#endregion
	}
}