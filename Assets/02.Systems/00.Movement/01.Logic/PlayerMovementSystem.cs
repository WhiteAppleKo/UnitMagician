using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PlayerMovement
{
    public class PlayerMovementSystem : IFixedTickable
    {
        private readonly PureDataPlayerMovement m_pureData;
        private readonly IPlayerMovementVisualizer m_visualizer;
        private readonly RuntimeDataPlayerInput m_runtimeData;

        private Vector2 m_input;
        private Vector3 m_direction;

        [Inject]
        public PlayerMovementSystem(
            RuntimeDataPlayerInput runtimeData, 
            PureDataPlayerMovement pureData, 
            IPlayerMovementVisualizer visualizer)
        {
            this.m_runtimeData = runtimeData;
            this.m_pureData = pureData;
            this.m_visualizer = visualizer;
        }

        public void FixedTick()
        {
            if (m_runtimeData == null || m_pureData == null || m_visualizer == null) return;

            m_input = m_runtimeData.InputDirection;
            m_direction = new Vector3(m_input.x, 0f, m_input.y);

            m_visualizer.Move(m_direction, m_pureData.MoveSpeed);
            m_visualizer.Rotate(m_direction, m_pureData.RotateSpeed);
        }
    }
}
