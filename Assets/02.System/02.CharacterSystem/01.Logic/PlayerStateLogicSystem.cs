using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace CharacterSystem
{
    public class PlayerStateLogicSystem : ITickable, IDisposable
    {
        private readonly RuntimeDataPlayerState runtimeState;
        private readonly CharacterStatSystem statSystem;
        private readonly UnitMagicSlotSystem magicSlotSystem;
        private readonly RuntimeDataTimeSlow runtimeTimeSlow;
        private readonly IPlayerStateVisualizer visualizer;

        private float castingTimer;

        [Inject]
        public PlayerStateLogicSystem(
            RuntimeDataPlayerState runtimeState,
            CharacterStatSystem statSystem,
            UnitMagicSlotSystem magicSlotSystem,
            RuntimeDataTimeSlow runtimeTimeSlow,
            IPlayerStateVisualizer visualizer = null)
        {
            this.runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.magicSlotSystem = magicSlotSystem ?? throw new ArgumentNullException(nameof(magicSlotSystem));
            this.runtimeTimeSlow = runtimeTimeSlow ?? throw new ArgumentNullException(nameof(runtimeTimeSlow));
            this.visualizer = visualizer;

            this.runtimeState.OnStateChanged += HandleStateChanged;
        }

        public void Dispose()
        {
            if (runtimeState != null)
            {
                runtimeState.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(PlayerStateType previousState, PlayerStateType newState)
        {
            visualizer?.OnStateChanged(previousState, newState);
        }

        public void Tick()
        {
            UpdateCastingState();
            UpdateSlowState();
            HandleInput();
        }

        private void UpdateCastingState()
        {
            if (runtimeState.CurrentState == PlayerStateType.Casting)
            {
                castingTimer -= Time.unscaledDeltaTime;
                if (castingTimer <= 0f)
                {
                    visualizer?.PlayCastingMotion(false);
                    runtimeState.ChangeState(PlayerStateType.Normal);
                }
            }
        }

        private void UpdateSlowState()
        {
            if (runtimeTimeSlow.IsSlowActive)
            {
                if (runtimeState.CurrentState != PlayerStateType.TimeSlow && runtimeState.CurrentState != PlayerStateType.Casting)
                {
                    runtimeState.ChangeState(PlayerStateType.TimeSlow);
                }
            }
            else if (runtimeState.CurrentState == PlayerStateType.TimeSlow)
            {
                runtimeState.ChangeState(PlayerStateType.Normal);
            }
        }

        private void HandleInput()
        {
            if (Mouse.current == null) return;

            // 마우스 좌클릭 시 단위 마법 시전 시도
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryCastCurrentMagic();
            }
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
                runtimeState.ChangeState(PlayerStateType.Casting);
                float duration = runtimeState.PureData != null ? runtimeState.PureData.CastingDuration : 0.5f;
                castingTimer = duration;

                visualizer?.PlayCastingMotion(true);
                visualizer?.TriggerCastEffect();
                return true;
            }

            return false;
        }
    }
}
