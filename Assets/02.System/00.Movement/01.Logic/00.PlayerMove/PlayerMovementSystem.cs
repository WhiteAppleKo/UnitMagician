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
            ICameraFollowService cameraFollowService)
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

            // 1인칭/3인칭 전환에 따른 커서 잠금 처리 위임
            m_visualizer.SetCursorLocked(isFirstPerson);

            if (isFirstPerson)
            {
                Vector2 mouseDelta = m_runtimeData.MouseDelta;

                // 1. 캐릭터 본체 좌우 회전 (Yaw - MouseDelta.x & RotateSpeed 적용)
                if (Mathf.Abs(mouseDelta.x) > 0.001f)
                {
                    float yawRotation = mouseDelta.x * m_pureData.RotateSpeed * Time.fixedDeltaTime;
                    m_visualizer.RotateYaw(yawRotation);
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
                Vector3 relativeMoveDir = m_visualizer.Forward * m_input.y + m_visualizer.Right * m_input.x;
                m_visualizer.Move(relativeMoveDir, m_pureData.MoveSpeed);
            }
            else
            {
                // 3인칭 쿼터뷰 기존 이동/회전 연산
                m_direction = new Vector3(m_input.x, 0f, m_input.y);
                m_visualizer.Move(m_direction, m_pureData.MoveSpeed);
                m_visualizer.Rotate(m_direction, m_pureData.RotateSpeed);
            }
        }
    }
}
