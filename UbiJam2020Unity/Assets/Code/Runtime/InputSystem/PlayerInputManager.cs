using System;
using System.Linq;
using Runtime.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Runtime.InputSystem
{
    public class PlayerInputManager:Singleton<PlayerInputManager>
    {
        public PlayerInput GetInputForPlayer(int playerID)
        {
            return new PlayerInput(playerID);
        }
        
    }

    public struct PlayerInput
    {
        public readonly Vector2 DirectionalInput;
        public readonly bool Eat;
        public PlayerInput(int playerID)
        {
            Vector2 moveDirection = Vector2.zero;
            Eat = false;
            if (Gamepad.all.Count > playerID)
            {
                moveDirection = Gamepad.all[playerID].leftStick.ReadValue();
                Eat = Gamepad.all[playerID].allControls.Any(control => (control is ButtonControl) && control.IsPressed());
            }

            Keyboard keyboard = Keyboard.current;
            switch (playerID)
            {
                case 0:
                    if (keyboard[Key.W].isPressed)
                    {
                        moveDirection += Vector2.up;
                    }
                    if (keyboard[Key.S].isPressed)
                    {
                        moveDirection += Vector2.down;
                    }
                    if (keyboard[Key.A].isPressed)
                    {
                        moveDirection += Vector2.left;
                    }
                    if (keyboard[Key.D].isPressed)
                    {
                        moveDirection += Vector2.right;
                    }
                    if (keyboard[Key.Space].isPressed)
                    {
                        Eat=true;
                    }
                    break;
                case 1:
                    if (keyboard[Key.UpArrow].isPressed)
                    {
                        moveDirection += Vector2.up;
                    }
                    if (keyboard[Key.DownArrow].isPressed)
                    {
                        moveDirection += Vector2.down;
                    }
                    if (keyboard[Key.LeftArrow].isPressed)
                    {
                        moveDirection += Vector2.left;
                    }
                    if (keyboard[Key.RightArrow].isPressed)
                    {
                        moveDirection += Vector2.right;
                    }
                    if (keyboard[Key.RightCtrl].isPressed)
                    {
                        Eat=true;
                    }
                    break;
                default: 
                    throw new Exception();
            }

            DirectionalInput = moveDirection;
        }
    }
}