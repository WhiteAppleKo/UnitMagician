using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CharacterSystem
{
    public class PlayerStateLogicSystem : ITickable, IDisposable
    {
        private readonly RuntimeDataPlayerState runtimeState;
        private readonly ICharacterStatService statSystem;
        private readonly IUnitMagicSlotService magicSlotSystem;
        private readonly RuntimeDataTimeSlow runtimeTimeSlow;
        private readonly IPlayerStateVisualizer visualizer;

        private readonly Dictionary<PlayerStateType, IPlayerState> states;
        private IPlayerState currentState;

        public IPlayerState CurrentState => currentState;

        [Inject]
        public PlayerStateLogicSystem(
            RuntimeDataPlayerState runtimeState,
            ICharacterStatService statSystem,
            IUnitMagicSlotService magicSlotSystem,
            RuntimeDataTimeSlow runtimeTimeSlow,
            IPlayerStateVisualizer visualizer = null)
        {
            this.runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.magicSlotSystem = magicSlotSystem ?? throw new ArgumentNullException(nameof(magicSlotSystem));
            this.runtimeTimeSlow = runtimeTimeSlow ?? throw new ArgumentNullException(nameof(runtimeTimeSlow));
            this.visualizer = visualizer;

            states = new Dictionary<PlayerStateType, IPlayerState>
            {
                { PlayerStateType.Normal, new PlayerNormalState(this, this.runtimeTimeSlow) },
                { PlayerStateType.Casting, new PlayerCastingState(this, this.runtimeState, this.runtimeTimeSlow, this.visualizer) },
                { PlayerStateType.TimeSlow, new PlayerTimeSlowState(this, this.runtimeTimeSlow) }
            };

            this.runtimeState.OnStateChanged += HandleStateChanged;

            if (states.TryGetValue(this.runtimeState.CurrentState, out var initialState))
            {
                currentState = initialState;
                currentState.Enter();
            }
        }

        public void Dispose()
        {
            if (runtimeState != null)
            {
                runtimeState.OnStateChanged -= HandleStateChanged;
            }
            currentState?.Exit();
            currentState = null;
        }

        private void HandleStateChanged(PlayerStateType previousState, PlayerStateType newState)
        {
            visualizer?.OnStateChanged(previousState, newState);
        }

        public void Tick()
        {
            currentState?.Tick(Time.unscaledDeltaTime);
        }

        public void SwitchState(PlayerStateType newStateType)
        {
            if (states.TryGetValue(newStateType, out var newState))
            {
                SwitchState(newState);
            }
        }

        public void SwitchState(IPlayerState newState)
        {
            if (newState == null || currentState == newState) return;

            currentState?.Exit();
            currentState = newState;
            runtimeState.ChangeState(newState.StateType);
            currentState.Enter();
        }

        public bool TryCastCurrentMagic()
        {
            // 현재 상태가 Casting 중이거나 다른 제약이 있는지 확인
            if (runtimeState.CurrentState == PlayerStateType.Casting)
            {
                return false;
            }

            var currentMagic = magicSlotSystem.CurrentMagic;
            if (currentMagic == null)
            {
                Debug.LogWarning("[PlayerStateLogicSystem] Cannot cast: No magic equipped.");
                return false;
            }

            // 마나 검증 및 시전 판정
            if (statSystem.RuntimeData.MP.CurrentValue < currentMagic.BaseCost)
            {
                Debug.LogWarning($"[PlayerStateLogicSystem] Cannot cast: Insufficient Mana! (Required: {currentMagic.BaseCost}, Current: {statSystem.RuntimeData.MP.CurrentValue})");
                statSystem.UseMP(currentMagic.BaseCost); // OnInsufficientMana 이벤트 발행 유도
                return false;
            }

            // 시전 실행
            bool fired = magicSlotSystem.FireCurrentMagic();
            if (fired)
            {
                SwitchState(PlayerStateType.Casting);
                return true;
            }

            return false;
        }
    }
}
