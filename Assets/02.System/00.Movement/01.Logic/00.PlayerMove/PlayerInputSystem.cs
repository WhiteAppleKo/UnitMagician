using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

using CameraMovement;

namespace PlayerMovement
{
    public class PlayerInputSystem : ITickable
    {
        private readonly RuntimeDataPlayerInput m_runtimeData;

        [Inject]
        public PlayerInputSystem(RuntimeDataPlayerInput runtimeData)
        {
            this.m_runtimeData = runtimeData;
        }

        public void Tick()
        {
            if (m_runtimeData == null) return;

            Vector2 inputDir = Vector2.zero;

            if (Keyboard.current != null)
            {
                float horizontal = 0f;
                float vertical = 0f;

                if (Keyboard.current.wKey.isPressed) vertical += 1f;
                if (Keyboard.current.sKey.isPressed) vertical -= 1f;
                if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed) horizontal += 1f;

                inputDir = new Vector2(horizontal, vertical).normalized;
            }

            m_runtimeData.SetInputDirection(inputDir);

            Vector2 mouseDelta = Vector2.zero;
            if (Mouse.current != null)
            {
                mouseDelta = Mouse.current.delta.ReadValue();
            }
            m_runtimeData.SetMouseDelta(mouseDelta);
        }
    }
}
