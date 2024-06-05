using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame.Components
{
    /// <summary>
    /// This is a method I found online which essentially is a better version of XNA's Keyboard class.
    /// Importantly, IsPressed checks if the key is currently being pressed, HasBeenPressed checks if it was pressed once
    /// Source: https://community.monogame.net/t/one-shot-key-press/11669
    /// </summary>

    public class Keyboard
    {
        static KeyboardState currentKeyState;
        static KeyboardState previousKeyState;

        public static KeyboardState GetState()
        {
            previousKeyState = currentKeyState;
            currentKeyState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            return currentKeyState;
        }

        public static bool IsPressed(Keys key)
        {
            return currentKeyState.IsKeyDown(key);
        }

        public static bool HasBeenPressed(Keys key)
        {
            return currentKeyState.IsKeyDown(key) && !previousKeyState.IsKeyDown(key);
        }
    }
}
