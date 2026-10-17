using System;
using UnityEngine;
using GameEvent;

namespace AreaFreezeSystem
{
    /// <summary>
    /// AreaFreezeAction(이벤트 동작 SO)을 수행하는 핸들러입니다([L] 호출형 순수 로직). 이벤트 성공(onSuccess)에서 실행되면 보스 위치를 중심으로 광역 빙결을
    /// IAreaFreezeVisualizer에 지시합니다. Physics.OverlapSphere/transform.position 등 구체 접근은 전부 Visualizer 구현체로 위임하고, 이 클래스는
    /// "언제 호출할지"(= 이벤트 SO가 이 동작을 실행하라고 할 때)만 따릅니다(코드_가이드라인 DLV Rule 2 - Blind Logic, 05번 기획서 5.9절).
    /// bossGameObject는 식별용 참조로만 보관해 그대로 Visualizer에 전달할 뿐, 이 클래스 스스로 Transform/Collider/Physics를 조작하지 않습니다.
    /// 예전에는 06의 OnCoreHitSuccess 고정 신호를 구독했으나(AreaFreezeTriggerLogicSystem), 지금은 보스 50% 이벤트 SO가 onSuccess 동작으로 이 동작을 둡니다(Goal 51).
    /// 범위/지속 같은 빙결 수치는 PureDataAreaFreeze가 소유합니다.
    /// </summary>
    public class AreaFreezeActionHandler : EventActionHandler<AreaFreezeAction>
    {
        private readonly IAreaFreezeVisualizer visualizer;
        private readonly GameObject bossGameObject;

        public AreaFreezeActionHandler(IAreaFreezeVisualizer visualizer, GameObject bossGameObject)
        {
            this.visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
            this.bossGameObject = bossGameObject ?? throw new ArgumentNullException(nameof(bossGameObject));
        }

        protected override void Execute(AreaFreezeAction action, IEventContext context)
        {
            visualizer.TriggerAreaFreeze(bossGameObject);
        }
    }
}
