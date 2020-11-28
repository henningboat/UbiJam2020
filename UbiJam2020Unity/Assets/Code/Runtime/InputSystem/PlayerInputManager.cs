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
                    return new PlayerInput(Input.GetAxis("Player0Horizontal"),Input.GetKey(KeyCode.W));
                break;
                case 1:
                    return new PlayerInput(Input.GetAxis("Player1Horizontal"),Input.GetKey(KeyCode.I));
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public struct PlayerInput
    {
        public readonly float DirectionalInput;
        public readonly bool Eat;

        public PlayerInput(float directionalInput, bool eat)
        {
            DirectionalInput = directionalInput;
            Eat = eat;
        }
    }
}