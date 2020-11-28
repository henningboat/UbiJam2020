using UnityEngine;

namespace Runtime.GameSurface
{
    public class FallingPiece : MonoBehaviour
    {
        [SerializeField] private float _gravity = 4;

        private float velocity;
        private Texture2D _mask;

        public void SetMask(Texture2D mask)
        {
            _mask = mask;
            GetComponent<Renderer>().material.SetTexture("_Mask", mask);
        }

        private void Update()
        {
            velocity -= _gravity * Time.deltaTime;
            transform.position += Vector3.up * velocity * Time.deltaTime;

            if (transform.position.y < -30)
            {
                Destroy(_mask);
                Destroy(gameObject);
            }}
    }
}