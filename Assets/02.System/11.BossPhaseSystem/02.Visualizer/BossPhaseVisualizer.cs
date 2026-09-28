using UnityEngine;
using VContainer;

namespace BossPhaseSystem
{
    /// <summary>
    /// IBossPhaseVisualizer의 기본 구현체([V] World Visualizer)입니다. RuntimeDataBossPhase.OnPhaseChanged를
    /// 직접 구독해(03번 기획서 3.9절) 페이즈 전환 시점마다 연출을 재생하는 "수동적 수행자"입니다. 실제 화염
    /// 파티클/포효 사운드/화면 흔들림 아트 리소스는 이번 범위 밖이므로, Goal 프롬프트 10.4절 수준과 동일하게
    /// 로그 기반 플레이스홀더로 대체합니다.
    /// </summary>
    public class BossPhaseVisualizer : MonoBehaviour, IBossPhaseVisualizer
    {
        private RuntimeDataBossPhase runtimeData;

        [Inject]
        public void Construct(RuntimeDataBossPhase runtimeData)
        {
            // 재주입 시 중복 구독을 막기 위해 먼저 해제한다.
            if (this.runtimeData != null)
            {
                this.runtimeData.OnPhaseChanged -= HandlePhaseChanged;
            }

            this.runtimeData = runtimeData;
            if (this.runtimeData != null)
            {
                this.runtimeData.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (runtimeData != null)
            {
                runtimeData.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(BossPhaseType previousPhase, BossPhaseType newPhase)
        {
            PlayPhaseTransitionEffect(newPhase);
        }

        public void PlayPhaseTransitionEffect(BossPhaseType newPhase)
        {
            switch (newPhase)
            {
                case BossPhaseType.InvulnerableHealing:
                    // 3.4절: 전신 화염 이펙트 파티클 + 포효 사운드 + 화면 흔들림(약) + 체력바가 서서히
                    // 차오르는 UI 애니메이션 + 흡수 파티클(체력바 자체는 기존 HP.OnValueChanged 경로가 처리).
                    Debug.Log("<color=orange>[BossPhaseVisualizer] InvulnerableHealing 진입 - 화염 이펙트 + " +
                              "포효 사운드 + 화면 흔들림(약) 재생 (플레이스홀더)</color>");
                    break;
                case BossPhaseType.Dead:
                    // 3.3/3.8절: 성공 시 즉시 사망 처리. 사망 모션 자체는 bossActor.PlayMotion("Death")가 재생한다.
                    Debug.Log("<color=red>[BossPhaseVisualizer] Dead 전환 - 처치 연출 재생 (플레이스홀더)</color>");
                    break;
                case BossPhaseType.BasicAttack:
                    // 3.4절: 실패 시 체력바가 살짝 튀어 오르는 짧은 UI 강조 애니메이션(BasicAttack 복귀 시점과 동일).
                    Debug.Log("<color=yellow>[BossPhaseVisualizer] BasicAttack 복귀 - 체력 소폭 회복 강조 " +
                              "애니메이션 재생 (플레이스홀더)</color>");
                    break;
            }
        }
    }
}
