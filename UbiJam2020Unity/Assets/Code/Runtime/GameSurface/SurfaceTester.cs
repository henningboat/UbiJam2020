using UnityEngine;

namespace Runtime.GameSurface
{
    public class SurfaceTester : MonoBehaviour
    {
        [SerializeField] private int _radius;
        
        private void Update()
        {
            for (int x = -_radius; x < _radius; x++)
            {
                for (int y = -_radius; y < _radius; y++)
                {
                    GameSurface.Instance.Cut(transform.position + new Vector3(x, y, 0));
                }
            }
        }
    }
}