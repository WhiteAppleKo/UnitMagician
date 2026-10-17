using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 보스를 무적(회복) 페이즈로 진입시키는 동작입니다. 무적 중에는 보스가 피해를 받지 않고 속성/크기 게이트로만 피격 판정되며, 초당 회복량으로
    /// 회복 상한까지 HP가 차오릅니다. 회복 수치는 아래 BossRecoverAction 에셋(실패 시 회복 동작과 같은 에셋)에서 읽습니다.
    /// 이 동작이 보스 페이즈 진입의 유일한 경로이므로 보스 HP 임계값은 이벤트 SO의 트리거 비율(BossHpRatioTrigger) 한 곳입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BossInvulnerableAction_", menuName = "GameEvent/Action/BossInvulnerable")]
    public class BossInvulnerableAction : EventActionSO
    {
        [Tooltip("무적(회복) 동안의 회복 수치(초당 회복량, 회복 상한 비율). 실패 시 회복 동작(BossRecoverAction)과 같은 에셋을 가리킵니다.")]
        [SerializeField] private BossRecoverAction recoverSettings;

        public BossRecoverAction RecoverSettings => recoverSettings;
    }
}
