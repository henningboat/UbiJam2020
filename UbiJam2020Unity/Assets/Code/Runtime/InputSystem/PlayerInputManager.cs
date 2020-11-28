using System;
using Runtime.Utils;
using UnityEngine;

namespace Runtime.InputSystem
{
    public class PlayerInputManager:Singleton<PlayerInputManager>
    {
        public PlayerInput GetInputForPlayer(int playerID)
        {
            switch (playerID)
            {
                case 0:
                    return new PlayerInput(new Vector2(Input.GetAxis("Player0Horizontal"),Input.GetAxis("Player0Vertical")),Input.GetButton("Player0Eat"));
                break;
                case 1:
                    return new PlayerInput(new Vector2(Input.GetAxis("Player1Horizontal"),Input.GetAxis("Player1Vertical")),Input.GetButton("Player1Eat"));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public struct PlayerInput
    {
        public readonly Vector2 DirectionalInput;
        public readonly bool Eat;

        public PlayerInput(Vector2 directionalInput, bool eat)
        {
            DirectionalInput = directionalInput;
            Eat = eat;
        }
    }
}