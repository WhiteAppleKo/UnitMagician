using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using CharacterSystem;
using InteractionSystem.Logic;
using PipeLine.Contexts;

namespace EventSequencerSystem
{
    /// <summary>
    /// 범용 상태 머신 + 이벤트 방송 프레임워크([L] 호출형 순수 로직 + 유니티 생명주기 연동 로직)입니다.
    /// MonoBehaviour를 상속하지 않으며 Transform/Collider 등 구체 컴포넌트를 직접 참조하지 않습니다.
    /// 보스 HP 50% 도달과 4단계 결정타 성공/실패를 판정해 OnBoss50Percent/OnCoreHitSuccess/OnCoreHitFail
    /// 세 신호만 방송하는 것까지가 책임이며, 신호를 받아 실제 연출(카메라 전환/시간정지/이펙트)을 재생하는 것은
    /// 이 시스템의 책임이 아닙니다(06번 기획서 6.4/6.7절, Goal 프롬프트 "제외" 절).
    ///
    /// [구현 메모] Goal 프롬프트가 명시한 4개 생성자 인자(RuntimeDataTutorialSequence, PureDataTutorialSequence,
    /// ICharacterStatService, IInteractionService) 외에, "context.Victim == bossStatService가 속한 GameObject" 식별
    /// 비교를 수행하려면 보스의 GameObject 참조가 필요합니다. ICharacterStatService 인터페이스는 GameObject를
    /// 노출하지 않고 02.CharacterSystem 기존 파일은 이번 작업에서 수정하지 않으므로, TacticalCommandLogicSystem/
    /// NpcCombatActorLogicSystem 등 기존 Logic 클래스들이 이미 GameObject를 식별용 데이터로 직접 보유하는 것과
    /// 동일한 선례를 따라 bossGameObject를 추가 생성자 인자로 받습니다(Transform/Collider 등 조작용 구체 타입이
    /// 아니라 DamageContext.Victim과 동일한 성격의 식별자 데이터로만 사용).
    /// </summary>
    public class TutorialSequencerLogicSystem : ITickable, IDisposable, IEventSequencer
    {
        private readonly RuntimeDataTutorialSequence runtimeData;
        private readonly PureDataTutorialSequence pureData;
        private readonly ICharacterStatService bossStatService;
        private readonly IInteractionService interactionService;
        private readonly GameObject bossGameObject;

        private float stage3ElapsedSeconds;

        public event Action OnBoss50Percent;
        public event Action OnCoreHitSuccess;
        public event Action OnCoreHitFail;

        [Inject]
        public TutorialSequencerLogicSystem(
            RuntimeDataTutorialSequence runtimeData,
            PureDataTutorialSequence pureData,
            ICharacterStatService bossStatService,
            IInteractionService interactionService,
            GameObject bossGameObject)
        {
            this.runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            this.pureData = pureData ?? throw new ArgumentNullException(nameof(pureData));
            this.bossStatService = bossStatService ?? throw new ArgumentNullException(nameof(bossStatService));
            this.interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
            this.bossGameObject = bossGameObject;

            this.bossStatService.RuntimeData.HP.OnValueChanged += HandleBossHpChanged;
            this.interactionService.OnDamageProcessed += HandleDamageProcessed;
        }

        public void Dispose()
        {
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
                OnBoss50Percent?.Invoke();
            }
        }

        /// <summary>
        /// 4단계 결정타 성공/실패 판정. 대상이 보스이고 보스가 무적(회복 페이즈) 상태인 동안만 판정합니다.
        /// Aborted == false(속성 게이트 통과)면 성공, Aborted == true(게이트 실패)면 실패로 처리합니다.
        /// Stage3을 벗어난 뒤(Stage4Success/Stage4Fail 확정 후)에는 재판정하지 않습니다 - HandleBossHpChanged와
        /// 동일한 "이미 처리된 단계 신호 무시" 패턴(06번 기획서 6.5절)으로, 무적 플래그가 아직 꺼지기 전(예:
        /// 구독자가 SetInvincible(false)를 호출하기 전 타이밍)에 추가 피격이 들어와도 판정이 뒤집히지 않도록 합니다.
        /// </summary>
        private void HandleDamageProcessed(DamageContext context)
        {
            if (runtimeData.CurrentStage != TutorialStage.Stage3) return;
            if (bossGameObject == null || context.Victim != bossGameObject) return;
            if (!bossStatService.RuntimeData.IsInvincible) return;

            if (!context.Aborted)
            {
                runtimeData.ChangeStage(TutorialStage.Stage4Success);
                OnCoreHitSuccess?.Invoke();
            }
            else
            {
                runtimeData.ChangeStage(TutorialStage.Stage4Fail);
                OnCoreHitFail?.Invoke();

                // 정정(튜토리얼 씬 통합 조립 중 발견된 재도전 회귀 버그, 06번 기획서 6.2절): Stage4Fail도
                // TutorialStage enum 순서상 >= Stage3이라, 여기서 Stage2로 되돌리지 않으면
                // HandleBossHpChanged의 "이미 Stage3 이상이면 재판정 안 함" 가드가 영구적으로 풀리지 않아
                // 재도전 시 보스 HP가 다시 50% 밑으로 내려가도 OnBoss50Percent가 다시는 방송되지 않는다.
                // 별도 이벤트 재방송 없이 스테이지 가드만 원상복구한다.
                runtimeData.ChangeStage(TutorialStage.Stage2);
            }
        }

        /// <summary>
        /// Stage3(회복/무적 페이즈) 진입 후 CoreHitTimeoutSeconds 동안 유효 피격 판정이 한 번도 나지 않으면
        /// 타임아웃으로 실패 처리합니다(완전히 빗나간 경우 포함, 06번 기획서 6.2절).
        /// </summary>
        public void Tick()
        {
            if (runtimeData.CurrentStage != TutorialStage.Stage3)
            {
                stage3ElapsedSeconds = 0f;
                return;
            }

            stage3ElapsedSeconds += Time.unscaledDeltaTime;
            if (stage3ElapsedSeconds >= pureData.CoreHitTimeoutSeconds)
            {
                runtimeData.ChangeStage(TutorialStage.Stage4Fail);
                OnCoreHitFail?.Invoke();

                // 정정(위 HandleDamageProcessed 실패 분기와 동일한 이유, 06번 기획서 6.2절): 타임아웃 실패도
                // Stage2로 되돌려 HandleBossHpChanged의 재판정 가드를 원상복구한다.
                runtimeData.ChangeStage(TutorialStage.Stage2);
            }
        }
    }
}