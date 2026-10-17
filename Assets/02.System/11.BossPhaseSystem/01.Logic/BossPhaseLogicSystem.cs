using System;
using UnityEngine;
using VContainer.Unity;
using NpcSystem;
using CharacterSystem;
using PipeLine.Combat;

namespace BossPhaseSystem
{
    /// <summary>
    /// 보스 페이즈 FSM("두뇌")입니다([L] 유니티 생명주기 연동 로직 - ITickable). "언제 무엇을 하라고
    /// 지시할지"만 담당하며, 실제 이동/공격/모션 실행은 01번 프레임워크(INpcCombatActor 구현체)가, 무적/회복
    /// 실행은 08번(ICharacterStatService)이 전담합니다(03번 기획서 3.1/3.2/3.8절). 보스 액터를 상속하지 않고
    /// 인터페이스로만 참조합니다(컴포지션 원칙). MonoBehaviour/Transform/Collider 등 구체 타입을 직접 참조하지
    /// 않습니다(코드_가이드라인 DLV Rule 2 - Blind Logic).
    ///
    /// 무적·회복 페이즈 진입/실패 회복/처치는 이벤트 시스템(09)의 보스 50% 이벤트 SO가 동작(BossInvulnerableAction / BossRecoverAction / BossDefeatAction)으로
    /// 지시하며(Goal 51), 이 클래스는 IBossPhaseController로 그 지시를 받아 수행합니다. 예전에는 이 클래스가 보스 HP를 직접 구독하고 임계값(0.5)을 상수로 들고 있었으나,
    /// 지금은 보스 HP 임계값이 이벤트 SO의 BossHpRatioTrigger 비율 한 곳입니다. 회복 수치(초당 회복량, 회복 상한/실패 시 회복 비율)도 이벤트 SO(BossRecoverAction)에서 받습니다.
    /// </summary>
    public class BossPhaseLogicSystem : ITickable, IBossPhaseController
    {
        private readonly INpcCombatActor bossActor;
        private readonly ICharacterStatService bossStatService;
        private readonly RuntimeDataBossPhase runtimeData;
        private readonly PureDataBossPattern pureData;
        private readonly GameObject playerTarget;

        // 회복(무적) 페이즈 동안 보스 피격 플래그: 속성 게이트 + 크기 게이트(방어력 스텝은 FinalDamage의 질량 비율을 지우므로 함께 쓰지 않는다).
        private const HitStepFlags InvulnerableAddedHitFlags = HitStepFlags.AttributeGate | HitStepFlags.SizeGate;
        private const HitStepFlags InvulnerableSuppressedHitFlags = HitStepFlags.Evasion | HitStepFlags.Defense;

        private float elapsedSinceLastAttack;
        private float healAccumulator;
        private float healPerSecond;
        private float healCapHpRatio;
        private bool invulnerableHitFlagsApplied;

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

            // Idle -> BasicAttack: 3.2절 FSM 표기의 첫 전이. 별도 트리거 없이 보스 두뇌가 구성되는 즉시
            // 기본 공격 순환을 시작한다(완료 기준 "BasicAttack 상태에서 attackInterval 주기로 Attack 호출").
            this.runtimeData.ChangePhase(BossPhaseType.BasicAttack);
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
        /// HP가 회복 상한(이벤트 SO의 회복 비율 x maxHp, 기본 60%)을 넘지 않도록 상한을 둔다 - healPerSecond가
        /// 충분히 빨라 HP가 100%까지 회복해버리면, 실패 시 상한 비율로 "재설정"하는 RecoverFromFail의 diff = targetHp - currentHp가
        /// 음수가 되어 무동작이 되고 결과적으로 상한 비율이 아니라 100%로 남아버린다(3.8절 "회복 진행(정정)" 문단). 목표치까지 남은 만큼만 Heal을
        /// 호출하고, 이미 도달했으면 그 프레임의 Heal 호출을 스킵한다.
        /// </summary>
        private void TickInvulnerableHealing()
        {
            var hp = bossStatService.RuntimeData.HP;
            int healCapHp = Mathf.RoundToInt(healCapHpRatio * hp.MaxValue);
            int remainingToCap = healCapHp - hp.CurrentValue;
            if (remainingToCap <= 0)
            {
                healAccumulator = 0f;
                return;
            }

            healAccumulator += healPerSecond * Time.deltaTime;
            int wholeHeal = Mathf.FloorToInt(healAccumulator);
            if (wholeHeal > 0)
            {
                wholeHeal = Mathf.Min(wholeHeal, remainingToCap);
                bossStatService.Heal(wholeHeal);
                healAccumulator -= wholeHeal;
            }
        }

        /// <summary>
        /// 무적·회복 페이즈 진입(BossInvulnerableAction). 이미 InvulnerableHealing/Dead 상태이면 재진입하지 않는다(완료 기준 "이후 재판정 없음").
        /// OnCoreHitFail로 BasicAttack에 복귀한 뒤에는 다시 진입 가능해져 재도전 루프(시나리오 F)를 허용한다.
        /// healPerSecond / healCapHpRatio는 이벤트 SO(BossRecoverAction)의 값이다.
        /// </summary>
        public void EnterInvulnerableHealing(float healPerSecond, float healCapHpRatio)
        {
            if (runtimeData.CurrentPhase == BossPhaseType.InvulnerableHealing) return;
            if (runtimeData.CurrentPhase == BossPhaseType.Dead) return;

            this.healPerSecond = healPerSecond;
            this.healCapHpRatio = healCapHpRatio;

            bossStatService.SetInvincible(true);
            ApplyInvulnerableHitFlags();
            runtimeData.ChangePhase(BossPhaseType.InvulnerableHealing);
            healAccumulator = 0f;
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
        public void Defeat()
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
        /// 실패/타임아웃(3.8절, BossRecoverAction): 목표 HP(recoverHpRatio*maxHp, 이벤트 SO 값)와 현재 HP의 차이만큼 Heal -> 무적 해제 ->
        /// BasicAttack 복귀 순서로 호출한다.
        /// </summary>
        public void RecoverFromFail(float recoverHpRatio)
        {
            if (runtimeData.CurrentPhase == BossPhaseType.Dead) return;

            var hp = bossStatService.RuntimeData.HP;
            int targetHp = Mathf.RoundToInt(recoverHpRatio * hp.MaxValue);
            int diff = targetHp - hp.CurrentValue;
            if (diff > 0)
            {
                bossStatService.Heal(diff);
            }

            bossStatService.SetInvincible(false);
            RestoreInvulnerableHitFlags();
            elapsedSinceLastAttack = 0f;
            runtimeData.ChangePhase(BossPhaseType.BasicAttack);
        }

        /// <summary>
        /// 회복(무적) 페이즈 진입: 보스의 피격 플래그를 속성·크기 게이트로 바꾼다(AddHit(AttributeGate | SizeGate) + SuppressHit(Evasion|Defense)).
        /// 이전에는 전용 컴포넌트(BossEventHitFlagsComponent)가 IsInvincible을 읽어 코드로 반환했다. 페이즈를 관장하는 이 로직이 보스의
        /// CombatFlagState를 갱신하고, InteractionSystem은 피격 시 CharacterStatComponent.CurrentHitFlags로 그 값을 읽는다.
        /// </summary>
        private void ApplyInvulnerableHitFlags()
        {
            if (invulnerableHitFlagsApplied) return;

            var flags = bossStatService.RuntimeData.CombatFlags;
            flags.AddHit(InvulnerableAddedHitFlags);
            flags.SuppressHit(InvulnerableSuppressedHitFlags);
            invulnerableHitFlagsApplied = true;
        }

        /// <summary>회복(무적) 페이즈 해제: 진입 때 적용한 플래그 변경을 되돌려 보스 피격 플래그를 PureStatData 기본값으로 원복한다.</summary>
        private void RestoreInvulnerableHitFlags()
        {
            if (!invulnerableHitFlagsApplied) return;

            var flags = bossStatService.RuntimeData.CombatFlags;
            flags.RemoveHit(InvulnerableAddedHitFlags);
            flags.UnsuppressHit(InvulnerableSuppressedHitFlags);
            invulnerableHitFlagsApplied = false;
        }
    }
}
