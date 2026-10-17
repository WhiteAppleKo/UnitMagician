using System;

namespace CharacterSystem
{
    public class PlayerTimeSlowState : IPlayerState
    {
        private readonly PlayerStateLogicSystem context;
        private readonly RuntimeDataTimeSlow runtimeTimeSlow;

        public PlayerStateType StateType => PlayerStateType.TimeSlow;

        public PlayerTimeSlowState(PlayerStateLogicSystem context, RuntimeDataTimeSlow runtimeTimeSlow)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.runtimeTimeSlow = runtimeTimeSlow;
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            if (runtimeTimeSlow != null && !runtimeTimeSlow.IsSlowActive)
            {
                context.SwitchState(PlayerStateType.Normal);
            }
        }

        public void Exit()
        {
        }
    }
}
