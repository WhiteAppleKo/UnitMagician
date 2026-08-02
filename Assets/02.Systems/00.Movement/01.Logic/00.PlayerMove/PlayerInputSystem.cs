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
        private readonly ICameraFollowService m_cameraFollowService;

        [Inject]
        public PlayerInputSystem(RuntimeDataPlayerInput runtimeData, ICameraFollowService cameraFollowService = null)
        {
            this.m_runtimeData = runtimeData;
            this.m_cameraFollowService = cameraFollowService;
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

                if (m_cameraFollowService != null)
                {
                    if (Keyboard.current.digit1Key.wasPressedThisFrame)
                    {
                        m_cameraFollowService.SetCameraMode(CameraMode.HybridFocus);
                    }
                    else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                    {
                        m_cameraFollowService.SetCameraMode(CameraMode.PlayerOnly);
                    }
                    else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                    {
                        m_cameraFollowService.SetCameraMode(CameraMode.MouseFocus);
                    }
                }
            }

            m_runtimeData.SetInputDirection(inputDir);
        }
    }
}
