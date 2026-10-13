using System;
using UnityEngine;
using VContainer;
using CharacterSystem;
using InteractionSystem.Logic;
using PipeLine.Contexts;

namespace EventSequencerSystem
{
    /// <summary>
    /// 범용 상태 머신 + 이벤트 방송 프레임워크([L] 호출형 순수 로직 + 유니티 생명주기 연동 로직)입니다.
    /// MonoBehaviour를 상속하지 않으며 Transform/Collider 등 구체 컴포넌트를 직접 참조하지 않습니다.
    /// 보스 HP 50% 도달과 4단계 결정타 성공/실패를 판정해 OnBoss50Percent/OnCoreHitSuccess/OnCoreHitFail
    /// 세 신호만 방송하는 것까지가 책임이며(13번 기획서: 3단계 판정은 투사체 명중 결과로만 하며 시간 기반 타임아웃 실패 판정은 없다), 신호를 받아 실제 연출(카메라 전환/시간정지/이펙트)을 재생하는 것은
    /// 이 시스템의 책임이 아닙니다(06번 기획서 6.4/6.7절, Goal 프롬프트 "제외" 절).
    ///
    /// [구현 메모] Goal 프롬프트가 명시한 4개 생성자 인자(RuntimeDataTutorialSequence, PureDataTutorialSequence,
    /// ICharacterStatService, IInteractionService) 외에, "context.Victim == bossStatService가 속한 GameObject" 식별
    /// 비교를 수행하려면 보스의 GameObject 참조가 필요합니다. ICharacterStatService 인터페이스는 GameObject를
    /// 노출하지 않고 02.CharacterSystem 기존 파일은 이번 작업에서 수정하지 않으므로,
    /// NpcCombatActorLogicSystem 등 기존 Logic 클래스들이 이미 GameObject를 식별용 데이터로 직접 보유하는 것과
    /// 동일한 선례를 따라 bossGameObject를 추가 생성자 인자로 받습니다(Transform/Collider 등 조작용 구체 타입이
    /// 아니라 DamageContext.Victim과 동일한 성격의 식별자 데이터로만 사용).
    /// </summary>
    public class TutorialSequencerLogicSystem : IDisposable, IEventSequencer
    {
        private readonly RuntimeDataTutorialSequence runtimeData;
        private readonly PureDataTutorialSequence pureData;
        private readonly ICharacterStatService bossStatService;
        private readonly IInteractionService interactionService;
        private readonly GameObject bossGameObject;
        private readonly IEventFlightLogic flightLogic;

        public event Action OnBoss50Percent;
        public event Action OnCoreHitSuccess;
        public event Action OnCoreHitFail;
        public event Action<EventFlightPhase, float> OnEventFlightChanged;
        public event Action OnEventFlightTurnaround;

        [Inject]
        public TutorialSequencerLogicSystem(
            RuntimeDataTutorialSequence runtimeData,
            PureDataTutorialSequence pureData,
            ICharacterStatService bossStatService,
            IInteractionService interactionService,
            GameObject bossGameObject,
            IEventFlightLogic flightLogic)
        {
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.pureData = pureData ?? throw new ArgumentNullException(nameof(pureData));
            this.bossStatService = bossStatService ?? throw new ArgumentNullException(nameof(bossStatService));
            this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
            this.bossGameObject = bossGameObject;
            this.flightLogic = flightLogic ?? throw new ArgumentNullException(nameof(flightLogic));

            this.bossStatService.RuntimeData.HP.OnValueChanged += HandleBossHpChanged;
            this.interactionService.OnDamageProcessed += HandleDamageProcessed;

            // 13번 기획서: 3단계 비행 구간 계산 로직의 결과(구간/진행도 변경, 전환점)를 IEventSequencer 신호로 그대로 노출한다.
            this.flightLogic.OnFlightChanged += HandleFlightChanged;
            this.flightLogic.OnTurnaround += HandleFlightTurnaround;
            this.flightLogic.OnProjectileLost += HandleProjectileLost;
        }

        private void HandleFlightChanged(EventFlightPhase phase, float progress)
        {
            OnEventFlightChanged?.Invoke(phase, progress);
        }

        private void HandleFlightTurnaround()
        {
            OnEventFlightTurnaround?.Invoke();
        }

        public void Dispose()
        {
            if (flightLogic != null)
            {
                flightLogic.OnFlightChanged -= HandleFlightChanged;
                flightLogic.OnTurnaround -= HandleFlightTurnaround;
                flightLogic.OnProjectileLost -= HandleProjectileLost;
            }

            if (bossStatService?.RuntimeData?.HP != null)
            {
                bossStatService.RuntimeData.HP.OnValueChanged -= HandleBossHpChanged;
            }

            if (interactionService != null)
            {
                interactionService.OnDamageProcessed -= HandleDamageProcessed;
            }
        }

        /// <summary>
        /// 보스 HP 최초 50%(설정값) 도달 감지. 이미 Stage3 이상이면 재판정하지 않아 중복 방송을 막습니다
        /// (06번 기획서 6.5절 "이미 처리된 단계 신호 무시" 요구사항).
        /// </summary>
        private void HandleBossHpChanged(int current, int max)
        {
            if (runtimeData.CurrentStage >= TutorialStage.Stage3) return;
            if (max <= 0) return;

            float ratio = current / (float)max;
            if (ratio <= pureData.BossHpThresholdRatio)
            {
                runtimeData.ChangeStage(TutorialStage.Stage3);

                // 13번 기획서: 이벤트 진입 시점에 P/B를 스냅샷하고 발사 지점 L을 플레이어 사선 밖(옆으로 이격)에 정한 뒤(아군이 그 지점에서 발사하도록
                // 준비), 구독자(감속 시작/아군 발사)에게 신호를 보낸다. 반드시 OnBoss50Percent 방송보다 먼저여야 한다.
                flightLogic.Begin();
                OnBoss50Percent?.Invoke();
                flightLogic.CloseLaunchWindow();
            }
        }

        /// <summary>
        /// 4단계 결정타 성공/실패 판정(13번 기획서 §0/§3 "3단계 판정은 명중 결과로만"). 판정 대상은 <b>이벤트 투사체</b>가 보스를
        /// 맞힌 피격으로 한정합니다(<c>context.Source</c>가 <see cref="IEventFlightLogic.IsEventProjectile"/>). 1단계 타이머 샷, 직전
        /// 사이클의 재도전 샷, 플레이어 일반 공격 등 이벤트 투사체가 아닌 피격은(보스 HP는 정상 감소하지만) 판정 신호를 만들지 않고
        /// 무시합니다 - HP를 50% 아래로 내린 바로 그 피격도 이벤트 투사체가 아니므로 자연히 무시됩니다. 발사 전(투사체 참조 없음)의
        /// 피격도 무시됩니다. 대상이 보스이고 보스가 무적(회복 페이즈) 상태인 동안만 판정합니다.
        /// 성공 = 속성 게이트 통과(<c>AttributeGatePassed</c>) AND 크기 게이트 통과(<c>SizeGatePassed</c>) AND FinalDamage가 기준치
        /// (<see cref="PureDataTutorialSequence.CoreHitRequiredDamage"/>) 이상(20번 기획서: 세 조건을 각각 개별 확인). 그 외(속성 실패 /
        /// 속성은 통과했으나 크기 부족 / 크기는 통과했으나 데미지 미달 / 마법 없음)는 모두 실패이며, 실패한 게이트는 한 줄 로그로 남깁니다. 투사체는 아군 NPC가 쏘므로 플레이어가 아무것도 하지 않아도
        /// 반드시 명중하며, 시간 경과(타임아웃)로는 판정하지 않습니다 - 판정은 이 명중 결과로만 납니다
        /// (투사체가 명중 없이 소실된 경우만 <see cref="HandleProjectileLost"/>가 소프트락 방지 안전망으로 실패 처리).
        /// Stage3을 벗어난 뒤(Stage4Success/Stage4Fail 확정 후)에는 재판정하지 않습니다 - HandleBossHpChanged와
        /// 동일한 "이미 처리된 단계 신호 무시" 패턴(06번 기획서 6.5절)으로, 무적 플래그가 아직 꺼지기 전(예:
        /// 구독자가 SetInvincible(false)를 호출하기 전 타이밍)에 추가 피격이 들어와도 판정이 뒤집히지 않도록 합니다.
        /// </summary>
        private void HandleDamageProcessed(DamageContext context)
        {
            if (runtimeData.CurrentStage != TutorialStage.Stage3) return;
            if (bossGameObject == null || context.Victim != bossGameObject) return;
            if (!bossStatService.RuntimeData.IsInvincible) return;
            if (!flightLogic.IsEventProjectile(context.Source)) return;

            bool attributePassed = context.AttributeGatePassed && !context.Aborted;
            bool sizePassed = context.SizeGatePassed;
            bool damagePassed = context.FinalDamage >= pureData.CoreHitRequiredDamage;

            if (attributePassed && sizePassed && damagePassed)
            {
                Debug.Log($"[TutorialSequencer] Core hit judgement SUCCESS - attribute/size/damage gates all passed (FinalDamage: {context.FinalDamage})");
                runtimeData.ChangeStage(TutorialStage.Stage4Success);
                flightLogic.End();
                OnCoreHitSuccess?.Invoke();
            }
            else
            {
                Debug.Log($"[TutorialSequencer] Core hit judgement FAIL - failed gate(s): {DescribeFailedGates(attributePassed, sizePassed, damagePassed)} (FinalDamage: {context.FinalDamage})");
                ResolveFail();
            }
        }

        private static string DescribeFailedGates(bool attributePassed, bool sizePassed, bool damagePassed)
        {
            // 속성 게이트 실패(Aborted)면 이후 크기 게이트는 실행되지 않으므로 속성 게이트만 실패 사유로 남긴다.
            if (!attributePassed) return "속성게이트(attribute)";

            string failed = string.Empty;
            if (!sizePassed) failed += "크기게이트(size) ";
            if (!damagePassed) failed += "데미지(damage) ";
            return failed.TrimEnd();
        }

        /// <summary>
        /// 이벤트 투사체가 명중 판정 없이 소실(파괴/비활성화)된 경우의 소프트락 안전망입니다. 시간 기반 타임아웃이 아니라
        /// 투사체 소실 감지로, 기존 실패 경로(OnCoreHitFail -> Stage2 복귀 -> 재도전)로 이벤트를 정상 종료시킵니다.
        /// 이미 판정이 난 뒤(Stage3 이탈)에는 이중 판정(OnCoreHit*)을 만들지 않습니다.
        /// </summary>
        private void HandleProjectileLost()
        {
            if (runtimeData.CurrentStage != TutorialStage.Stage3)
            {
                flightLogic.End();
                return;
            }

            ResolveFail();
        }

        private void ResolveFail()
        {
            runtimeData.ChangeStage(TutorialStage.Stage4Fail);
            flightLogic.End();
            OnCoreHitFail?.Invoke();

            // 정정(튜토리얼 씬 통합 조립 중 발견된 재도전 회귀 버그, 06번 기획서 6.2절): Stage4Fail도
            // TutorialStage enum 순서상 >= Stage3이라, 여기서 Stage2로 되돌리지 않으면
            // HandleBossHpChanged의 "이미 Stage3 이상이면 재판정 안 함" 가드가 영구적으로 풀리지 않아
            // 재도전 시 보스 HP가 다시 50% 밑으로 내려가도 OnBoss50Percent가 다시는 방송되지 않는다.
            // 별도 이벤트 재방송 없이 스테이지 가드만 원상복구한다.
            runtimeData.ChangeStage(TutorialStage.Stage2);
        }
    }
}