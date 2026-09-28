using System;
using UnityEngine;
using EventSequencerSystem;

namespace AreaFreezeSystem
{
    /// <summary>
    /// 06(이벤트 시퀀서)의 OnCoreHitSuccess만 구독하는 얇은 배선 로직입니다([L] 호출형 순수 로직).
    /// Physics.OverlapSphere/transform.position 등 구체 접근은 전부 IAreaFreezeVisualizer 구현체(Visualizer
    /// 계층)로 위임하고, 이 클래스는 "언제 호출할지"만 판단합니다(코드_가이드라인 DLV Rule 2 - Blind Logic,
    /// 05번 기획서 5.9절 "Logic은 transform 직접 참조 금지 - 실제 Physics.OverlapSphere+transform.position 접근은
    /// Logic이 아닌 Visualizer 쪽에서 수행"). bossGameObject는 식별용 참조로만 보관해 그대로 Visualizer에
    /// 전달할 뿐, 이 클래스 스스로 Transform/Collider/Physics를 조작하지 않습니다(BossPhaseLogicSystem의
    /// playerTarget, AllySupportTriggerLogicSystem의 stoneDoorTarget/bossTarget과 동일한 선례).
    ///
    /// 03(보스AI페이즈시퀀스시스템)에 신호를 중계하지 않습니다 - 05번과 03번은 06의 같은 신호를 각자 독립적으로
    /// 구독하는 형제 관계입니다(Goal 프롬프트 "배경" 절, 05번 기획서 5.8절 마지막 문단).
    ///
    /// 06과의 연동은 서로 다른 VContainer 스코프 트리에 있어 생성자 주입이 불가능하므로(09/10/02/03번 작업에서
    /// 반복 확인된 현상), BossPhaseLogicSystem/AllySupportTriggerLogicSystem과 동일한 패턴 - BindEventSequencer()
    /// 사후 배선을 그대로 재사용합니다(AreaFreezeSystemLifetimeScope.cs 참고).
    /// </summary>
    public class AreaFreezeTriggerLogicSystem : IDisposable
    {
        private readonly IAreaFreezeVisualizer visualizer;
        private readonly GameObject bossGameObject;

        private IEventSequencer eventSequencer;

        public AreaFreezeTriggerLogicSystem(IAreaFreezeVisualizer visualizer, GameObject bossGameObject)
        {
            this.visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
            this.bossGameObject = bossGameObject ?? throw new ArgumentNullException(nameof(bossGameObject));
        }

        /// <summary>
        /// 06(이벤트 시퀀서) 연동 사후 배선입니다. AreaFreezeSystemLifetimeScope가 자신과
        /// EventSequencerSystemLifetimeScope의 Container가 모두 빌드된 뒤(Start()) 한 번 호출합니다.
        /// 재호출 시 기존 구독을 먼저 해제해 중복 구독을 막습니다(BossPhaseLogicSystem과 동일 패턴).
        /// </summary>
        public void BindEventSequencer(IEventSequencer sequencer)
        {
            UnbindEventSequencer();

            eventSequencer = sequencer;
            if (eventSequencer != null)
            {
                eventSequencer.OnCoreHitSuccess += HandleCoreHitSuccess;
            }
        }

        private void UnbindEventSequencer()
        {
            if (eventSequencer == null) return;

            eventSequencer.OnCoreHitSuccess -= HandleCoreHitSuccess;
            eventSequencer = null;
        }

        public void Dispose()
        {
            UnbindEventSequencer();
        }

        /// <summary>
        /// OnCoreHitFail이나 그 밖의 신호는 애초에 구독하지 않으므로 자연히 무시된다(시나리오 D - 무관 신호 무시).
        /// </summary>
        private void HandleCoreHitSuccess()
        {
            visualizer.TriggerAreaFreeze(bossGameObject);
        }
    }
}
