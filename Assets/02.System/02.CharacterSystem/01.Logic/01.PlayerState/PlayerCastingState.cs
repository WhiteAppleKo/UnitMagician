using System;
using UnityEngine;

namespace CharacterSystem
{
    public class PlayerCastingState : IPlayerState
    {
        private readonly PlayerStateLogicSystem context;
        private readonly RuntimeDataPlayerState runtimeState;
        private readonly RuntimeDataTimeSlow runtimeTimeSlow;
        private readonly IPlayerStateVisualizer visualizer;

        private float castingTimer;

        public PlayerStateType StateType => PlayerStateType.Casting;

        public PlayerCastingState(
            PlayerStateLogicSystem context,
            RuntimeDataPlayerState runtimeState,
            RuntimeDataTimeSlow runtimeTimeSlow,
            IPlayerStateVisualizer visualizer)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            this.runtimeTimeSlow = runtimeTimeSlow;
            this.visualizer = visualizer;
        }

        public void Enter()
        {
            float duration = runtimeState.PureData != null ? runtimeState.PureData.CastingDuration : 0.5f;
            castingTimer = duration;

            visualizer?.PlayCastingMotion(true);
            visualizer?.TriggerCastEffect();
        }

        public void Tick(float deltaTime)
        {
            castingTimer -= deltaTime;
            if (castingTimer <= 0f)
            {
                visualizer?.PlayCastingMotion(false);

                if (runtimeTimeSlow != null && runtimeTimeSlow.IsSlowActive)
                {
                    context.SwitchState(PlayerStateType.TimeSlow);
                }
                else
                {
                    context.SwitchState(PlayerStateType.Normal);
                }
            }
        }

        public void Exit()
        {
            visualizer?.PlayCastingMotion(false);
        }
    }
}
