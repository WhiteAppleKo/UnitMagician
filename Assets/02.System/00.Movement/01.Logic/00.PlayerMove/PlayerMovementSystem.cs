using UnityEngine;
using VContainer;
using VContainer.Unity;

using CameraMovement;

namespace PlayerMovement
{
    public class PlayerMovementSystem : IFixedTickable
    {
        private readonly PureDataPlayerMovement m_pureData;
        private readonly IPlayerMovementVisualizer m_visualizer;
        private readonly RuntimeDataPlayerInput m_runtimeData;
        private readonly ICameraFollowService m_cameraFollowService;

        private Vector2 m_input;
        private Vector3 m_direction;
        private float m_currentPitch = 0f;

        [Inject]
        public PlayerMovementSystem(
            RuntimeDataPlayerInput runtimeData, 
            PureDataPlayerMovement pureData, 
            IPlayerMovementVisualizer visualizer,
            ICameraFollowService cameraFollowService = null)
        {
            this.m_runtimeData = runtimeData;
            this.m_pureData = pureData;
            this.m_visualizer = visualizer;
            this.m_cameraFollowService = cameraFollowService;
        }

        public void FixedTick()
        {
            if (m_runtimeData == null || m_pureData == null || m_visualizer == null) return;

            m_input = m_runtimeData.InputDirection;

            bool isFirstPerson = m_cameraFollowService != null && m_cameraFollowService.CurrentMode == CameraMode.FirstPerson;

            if (isFirstPerson)
            {
                // 1인칭 모드: 커서 잠금 (Cursor Lock)
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                Vector2 mouseDelta = m_runtimeData.MouseDelta;

                // 1. 캐릭터 본체 좌우 회전 (Yaw - MouseDelta.x & RotateSpeed 적용)
                if (Mathf.Abs(mouseDelta.x) > 0.001f)
                {
                    float yawRotation = mouseDelta.x * m_pureData.RotateSpeed * Time.fixedDeltaTime;
                    m_visualizer.Transform.Rotate(0f, yawRotation, 0f);
                }

                // 2. 1인칭 고개 상하 회전 (Pitch - MouseDelta.y & RotateSpeed 적용 + Min/Max Clamp 한계)
                if (Mathf.Abs(mouseDelta.y) > 0.001f)
                {
                    float minPitch = m_pureData != null ? m_pureData.MinPitchAngle : -80f;
                    float maxPitch = m_pureData != null ? m_pureData.MaxPitchAngle : 80f;

                    m_currentPitch -= mouseDelta.y * m_pureData.RotateSpeed * Time.fixedDeltaTime;
                    m_currentPitch = Mathf.Clamp(m_currentPitch, minPitch, maxPitch);
                    m_visualizer.SetFirstPersonCameraPitch(m_currentPitch);
                }

                // 바라보는 방향 기준 WASD 상대 이동
                Vector3 relativeMoveDir = m_visualizer.Transform.forward * m_input.y + m_visualizer.Transform.right * m_input.x;
                m_visualizer.Move(relativeMoveDir, m_pureData.MoveSpeed);
            }
            else
            {
                // 3인칭 모드 복귀: 커서 잠금 해제 (Cursor Unlock)
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                // 3인칭 쿼터뷰 기존 이동/회전 연산
                m_direction = new Vector3(m_input.x, 0f, m_input.y);
                m_visualizer.Move(m_direction, m_pureData.MoveSpeed);
                m_visualizer.Rotate(m_direction, m_pureData.RotateSpeed);
            }
        }
    }
}
