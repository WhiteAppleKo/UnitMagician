using UnityEngine;

namespace AreaFreezeSystem
{
    /// <summary>
    /// 보스 GameObject용 IFreezable 구현체([V] World Visualizer)입니다. 이미 QA 통과한 03번의
    /// BossPhaseVisualizer/BossPhaseLogicSystem은 수정하지 않고, 같은 보스 GameObject에 별도로 부착하는 신규
    /// 컴포넌트로 05번 요구사항을 해결합니다(Goal 프롬프트 "특히 주의할 점" 절).
    ///
    /// 사망 모션 전환(HP 0 강제 반영 + Dead 전환 + PlayMotion("Death"))은 03번의 BossPhaseLogicSystem이 06의
    /// OnCoreHitSuccess를 독립적으로 구독해 이미 처리합니다. 이 컴포넌트의 Freeze()는 빙결 시각 효과만
    /// 책임지며, 03번의 처리와는 서로 다른 구독자로서 독립적으로 실행되어 간섭하지 않습니다(05번 기획서 5.8절
    /// 마지막 문단 - 05/03은 06의 같은 신호를 각자 구독하는 형제 관계).
    /// </summary>
    public class BossFreezableComponent : MonoBehaviour, IFreezable
    {
        public void Freeze()
        {
            Debug.Log($"<color=lightblue>[BossFreezableComponent] '{name}' 보스 빙결 이펙트 재생 (플레이스홀더) - " +
                      "03번의 사망 모션(PlayMotion(\"Death\"))과 독립적으로 동작합니다.</color>");
        }
    }
}
