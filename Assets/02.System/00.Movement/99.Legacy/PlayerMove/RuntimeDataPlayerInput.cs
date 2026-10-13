using System;
using UnityEngine;

namespace PlayerMovement
{
    public class RuntimeDataPlayerInput
    {
        public Vector2 InputDirection { get; private set; }
        public Vector2 MouseDelta { get; private set; }
        
        public event Action<Vector2> OnInputChanged;

        public void SetInputDirection(Vector2 direction)
        {
            if (InputDirection == direction) return;
            
            InputDirection = direction;
            OnInputChanged?.Invoke(InputDirection);
        }

        public void SetMouseDelta(Vector2 delta)
        {
            MouseDelta = delta;
        }
    }
}
