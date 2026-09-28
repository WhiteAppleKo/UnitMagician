using System;
using UnityEngine;
using VContainer.Unity;
using NpcSystem;
using CharacterSystem;
using EventSequencerSystem;

namespace BossPhaseSystem
{
    /// <summary>
    /// 보스 페이즈 FSM("두뇌")입니다([L] 유니티 생명주기 연동 로직 - ITickable/IDisposable). "언제 무엇을 하라고
    /// 지시할지"만 담당하며, 실제 이동/공격/모션 실행은 01번 프레임워크(INpcCombatActor 구현체)가, 무적/회복
    /// 실행은 08번(ICharacterStatService)이 전담합니다(03번 기획서 3.1/3.2/3.8절). 보스 액터를 상속하지 않고
    /// 인터페이스로만 참조합니다(컴포지션 원칙). MonoBehaviour/Transform/Collider 등 구체 타입을 직접 참조하지
    /// 않습니다(코드_가이드라인 DLV Rule 2 - Blind Logic).
    ///
    /// 06(이벤트 시퀀서)과의 연동은 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로
    /// (09/10/02번 작업에서 반복 확인된 현상), AllySupportTriggerLogicSystem/TimeSlowLogicSystem과 동일한 패턴 -
    /// BindEventSequencer() 사후 배선을 그대로 재사용합니다(BossPhaseSystemLifetimeScope.cs 참고).
    /// </summary>
    public class BossPhaseLogicSystem : ITickable, IDisposable
    {
        // 3.8절: "current/max<=0.5f 최초 도달" - PureDataBossPattern에는 별도 임계값 필드가 없으므로(3.9절 필드
        // 목록에 없음) 기획서 원문 그대로 상수로 고정한다. ReviveHpRatioOnFail만 데이터로 노출된다.
        private const float InvulnerableHealingHpThresholdRatio = 0.5f;

        private readonly INpcCombatActor bossActor;
        private readonly ICharacterStatService bossStatService;
        private readonly RuntimeDataBossPhase runtimeData;
        private readonly PureDataBossPattern pureData;
        private readonly GameObject playerTarget;

        private float elapsedSinceLastAttack;
        private float healAccumulator;

        private IEventSequencer eventSequencer;

        public BossPhaseLogicSystem(
            INpcCombatActor bossActor,
            ICharacterStatService bossStatService,
            RuntimeDataBossPhase runtimeData,
            PureDataBossPattern pureData,
            GameObject playerTarget)
        {
            this.bossActor = bossActor ?? throw new ArgumentNullException(nameof(bossActor));
            this.bossStatService = bossStatService ?? throw new ArgumentNullException(nameof(bossStatService));
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.pureData = pureData ?? throw new ArgumentNullException(nameof(pureData));
            this.playerTarget = playerTarget;

            this.bossStatService.RuntimeData.HP.OnValueChanged += HandleBossHpChanged;

            // Idle -> BasicAttack: 3.2절 FSM 표기의 첫 전이. 별도 트리거 없이 보스 두뇌가 구성되는 즉시
            // 기본 공격 순환을 시작한다(완료 기준 "BasicAttack 상태에서 attackInterval 주기로 Attack 호출").
            this.runtimeData.ChangePhase(BossPhaseType.BasicAttack);
        }

        /// <summary>
        /// 06(이벤트 시퀀서) 연동 사후 배선입니다. BossPhaseSystemLifetimeScope가 자신과
        /// EventSequencerSystemLifetimeScope의 Container가 모두 빌드된 뒤(Start()) 한 번 호출합니다.
        /// 재호출 시 기존 구독을 먼저 해제해 중복 구독을 막습니다(AllySupportTriggerLogicSystem과 동일 패턴).
        /// </summary>
        public void BindEventSequencer(IEventSequencer sequencer)
        {
            UnbindEventSequencer();

            eventSequencer = sequencer;
            if (eventSequencer != null)
            {
                eventSequencer.OnCoreHitSuccess += HandleCoreHitSuccess;
                eventSequencer.OnCoreHitFail += HandleCoreHitFail;
            }
        }

        private void UnbindEventSequencer()
        {
            if (eventSequencer == null) return;

            eventSequencer.OnCoreHitSuccess -= HandleCoreHitSuccess;
            eventSequencer.OnCoreHitFail -= HandleCoreHitFail;
            eventSequencer = null;
        }

        public void Dispose()
        {
            if (bossStatService?.RuntimeData?.HP != null)
            {
                bossStatService.RuntimeData.HP.OnValueChanged -= HandleBossHpChanged;
            }

            UnbindEventSequencer();
        }

        public void Tick()
        {
            switch (runtimeData.CurrentPhase)
            {
                case BossPhaseType.BasicAttack:
                    TickBasicAttack();
                    break;
                case BossPhaseType.InvulnerableHealing:
                    TickInvulnerableHealing();
                    break;
            }
        }

        /// <summary>BasicAttack 순환: attackInterval 주기로 bossActor.Attack(playerTarget) 호출(3.8절).</summary>
        private void TickBasicAttack()
        {
            elapsedSinceLastAttack += Time.deltaTime;
            if (elapsedSinceLastAttack >= pureData.AttackInterval)
            {
                bossActor.Attack(playerTarget);
                elapsedSinceLastAttack = 0f;
            }
        }

        /// <summary>
        /// 회복 진행(3.8절, 정정 - 튜토리얼 씬 통합 조립 중 발견된 오버슈트 버그): healPerSecond*deltaTime을
        /// 누적했다가 정수 단위가 쌓일 때만 Heal(int)을 호출한다. ICharacterStatService.Heal(int amount)가
        /// 정수만 받으므로, 매 프레임 그대로 캐스팅하면 낮은 healPerSecond 값에서 프레임당 누적치가 항상 0으로
        /// 잘려 전혀 회복되지 않는 문제를 accumulator로 방지한다(초당 총 회복량은 healPerSecond와 정확히 일치 -
        /// 정수 경계에서만 나눠 호출될 뿐).
        ///
        /// HP가 pureData.ReviveHpRatioOnFail * maxHp(기본 60%)를 넘지 않도록 상한을 둔다 - healPerSecond가
        /// 충분히 빨라 invulnerablePhaseTimeoutSeconds 안에 HP가 100%까지 회복해버리면, 실패 시 60%로
        /// "재설정"하는 HandleCoreHitFail의 diff = targetHp - currentHp가 음수가 되어 무동작이 되고 결과적으로
        /// 60%가 아니라 100%로 남아버린다(3.8절 "회복 진행(정정)" 문단). 목표치까지 남은 만큼만 Heal을
        /// 호출하고, 이미 도달했으면 그 프레임의 Heal 호출을 스킵한다.
        /// </summary>
        private void TickInvulnerableHealing()
        {
            var hp = bossStatService.RuntimeData.HP;
            int healCapHp = Mathf.RoundToInt(pureData.ReviveHpRatioOnFail * hp.MaxValue);
            int remainingToCap = healCapHp - hp.CurrentValue;
            if (remainingToCap <= 0)
            {
                healAccumulator = 0f;
                return;
            }

            healAccumulator += pureData.HealPerSecond * Time.deltaTime;
            int wholeHeal = Mathf.FloorToInt(healAccumulator);
            if (wholeHeal > 0)
            {
                wholeHeal = Mathf.Min(wholeHeal, remainingToCap);
                bossStatService.Heal(wholeHeal);
                healAccumulator -= wholeHeal;
            }
        }

        /// <summary>
        /// 50% 감지(3.8절). 이미 InvulnerableHealing/Dead 상태이면 재판정하지 않는다(완료 기준 "이후 재판정 없음").
        /// OnCoreHitFail로 BasicAttack에 복귀한 뒤에는 다시 판정 가능해져 재도전 루프(시나리오 F)를 허용한다.
        /// </summary>
        private void HandleBossHpChanged(int current, int max)
        {
            if (runtimeData.CurrentPhase == BossPhaseType.InvulnerableHealing) return;
            if (runtimeData.CurrentPhase == BossPhaseType.Dead) return;
            if (max <= 0) return;

            float ratio = current / (float)max;
            if (ratio <= InvulnerableHealingHpThresholdRatio)
            {
                bossStatService.SetInvincible(true);
                runtimeData.ChangePhase(BossPhaseType.InvulnerableHealing);
                healAccumulator = 0f;
            }
        }

        /// <summary>
        /// 성공(3.8절): HP를 0으로 강제 반영 + Dead 전환 + 사망 모션 재생. 무적 해제는 불필요(사망 처리).
        ///
        /// QA 지적(3.6절 "체력바가 0으로 떨어진다" 위반) 반영: InvulnerableHealing 상태에서는
        /// bossStatService.RuntimeData.IsInvincible이 true이므로 ICharacterStatService.TakeDamage(int)를
        /// 호출하면 무적 게이트에 막혀 HP가 실제로 줄지 않는다. 그 무적 게이트는 일반 전투 파이프라인의 피격을
        /// 막기 위한 것이지, 이 FSM이 06(이벤트 시퀀서)의 판정으로 확정한 "결정타 처치"까지 막아서는 안 되므로,
        /// TakeDamage를 우회해 RuntimeData.ReduceHP(int)를 직접 호출한다(RuntimeStatData 자신의 캡슐화 전용
        /// 메서드이므로 DLV Rule 3 - Encapsulated Data는 그대로 지켜진다).
        /// </summary>
        private void HandleCoreHitSuccess()
        {
            if (runtimeData.CurrentPhase == BossPhaseType.Dead) return;

            var hp = bossStatService.RuntimeData.HP;
            if (hp.CurrentValue > 0)
            {
                bossStatService.RuntimeData.ReduceHP(hp.CurrentValue);
            }

            runtimeData.ChangePhase(BossPhaseType.Dead);
            bossActor.PlayMotion("Death");
        }

        /// <summary>
        /// 실패/타임아웃(3.8절): 목표 HP(reviveHpRatioOnFail*maxHp)와 현재 HP의 차이만큼 Heal -> 무적 해제 ->
        /// BasicAttack 복귀 순서로 호출한다.
        /// </summary>
        private void HandleCoreHitFail()
        {
            if (runtimeData.CurrentPhase == BossPhaseType.Dead) return;

            var hp = bossStatService.RuntimeData.HP;
            int targetHp = Mathf.RoundToInt(pureData.ReviveHpRatioOnFail * hp.MaxValue);
            int diff = targetHp - hp.CurrentValue;
            if (diff > 0)
            {
                bossStatService.Heal(diff);
            }

            bossStatService.SetInvincible(false);
            elapsedSinceLastAttack = 0f;
            runtimeData.ChangePhase(BossPhaseType.BasicAttack);
        }
    }
}
