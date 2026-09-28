using UnityEngine;

namespace AreaFreezeSystem
{
    /// <summary>
    /// 광역 빙결 연출 진입점([V] Visual Interface)입니다. 추상 메서드만 노출하며 구체 구현(물리 오버랩 조회,
    /// 코루틴 기반 순차 호출)은 감춥니다(DIP 연결 고리 - 코드_가이드라인 1-3절). AreaFreezeTriggerLogicSystem은
    /// 이 인터페이스만 알고 있으며 Physics/Transform을 직접 다루지 않습니다(05번 기획서 5.9절).
    /// </summary>
    public interface IAreaFreezeVisualizer
    {
        /// <summary>
        /// 06(이벤트 시퀀서)의 OnCoreHitSuccess 수신 시 호출됩니다. bossGameObject 위치를 중심으로 반경 내
        /// IFreezable 대상을 수집해 순차적으로 Freeze()를 호출합니다(보스 자신도 포함).
        /// </summary>
        void TriggerAreaFreeze(GameObject bossGameObject);
    }
}
