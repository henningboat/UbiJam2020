using Runtime.GameSurface;
using Runtime.InputSystem;
using UnityEngine;

namespace Runtime.PlayerSystem
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 100;
        [SerializeField] private float _forwardSpeed = 2;

        void Update()
        {
            transform.Rotate(0, 0, PlayerInput.Instance.HorizontalAxis * _rotationSpeed * Time.deltaTime);

            TryTranslate(transform.up * Time.deltaTime * _forwardSpeed);

            if (PlayerInput.Instance.Eat)
            {
                GameSurface.GameSurface.Instance.Cut(transform.position);
            }
        }

        private void TryTranslate(Vector2 positionDelta)
        {
            Vector2 direction = positionDelta.normalized;
            float distanceToTravel = positionDelta.magnitude;
            float stepSize = GameSurface.GameSurface.Instance.WorldSpaceGridNodeSize;

            while (distanceToTravel > 0)
            {
                float currentStepSize = Mathf.Min(stepSize, distanceToTravel);
                var node = GameSurface.GameSurface.Instance.GetNodeAtPosition((Vector2) transform.position + direction * currentStepSize);
                if (node.State == SurfaceState.Destroyed)
                    return;
                transform.position += (Vector3)(currentStepSize * direction);
                distanceToTravel -= currentStepSize;
            }
        }
    }
}
