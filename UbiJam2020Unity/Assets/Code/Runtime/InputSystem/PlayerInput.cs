using Runtime.Utils;
using UnityEngine;

namespace Runtime.InputSystem
{
    public class PlayerInput:Singleton<PlayerInput>
    {
        public float HorizontalAxis => Input.GetAxis("Horizontal");
        public bool Eat => Input.GetKey(KeyCode.Space);
    }
}