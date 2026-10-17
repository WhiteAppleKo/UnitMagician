using System;

namespace CharacterSystem
{
    public class PlayerNormalState : IPlayerState
    {
        private readonly PlayerStateLogicSystem context;
        private readonly RuntimeDataTimeSlow runtimeTimeSlow;

        public PlayerStateType StateType => PlayerStateType.Normal;

        public PlayerNormalState(PlayerStateLogicSystem context, RuntimeDataTimeSlow runtimeTimeSlow)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.runtimeTimeSlow = runtimeTimeSlow;
        }

        public void Enter()
        {
        }

        public void Tick(float deltaTime)
        {
            if (runtimeTimeSlow != null && runtimeTimeSlow.IsSlowActive)
            {
                context.SwitchState(PlayerStateType.TimeSlow);
            }
        }

        public void Exit()
        {
        }
    }
}
