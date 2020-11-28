using UnityEngine;

namespace Runtime.GameSurface
{
    public class SurfaceTester : MonoBehaviour
    {
        [SerializeField] private int _radius;

        private void Update()
        {
            for (var x = -_radius; x < _radius; x++)
            for (var y = -_radius; y < _radius; y++)
                GameSurface.Instance.Cut(transform.position + new Vector3(x * 0.02f, y * 0.02f, 0));
        }
    }
}