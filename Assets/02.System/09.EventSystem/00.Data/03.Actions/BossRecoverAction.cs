using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 보스 회복 수치와 실패 시 회복 동작입니다. 이벤트 실패(onFail)에서 실행되면 보스 HP를 recoverHpRatio(현재/최대)까지 즉시 채우고 무적을 풀어
    /// 일반 공격 순환으로 복귀시킵니다. 같은 에셋을 BossInvulnerableAction이 가리켜 무적(회복) 동안의 초당 회복량과 회복 상한(= recoverHpRatio)으로도 씁니다(단일 원천).
    /// </summary>
    [CreateAssetMenu(fileName = "BossRecoverAction_", menuName = "GameEvent/Action/BossRecover")]
    public class BossRecoverAction : EventActionSO
    {
        [Tooltip("실패 시 보스 HP를 채울 비율(현재/최대). 무적(회복) 동안의 회복 상한이기도 합니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float recoverHpRatio = 0.6f;
        [Tooltip("무적(회복) 동안의 초당 회복량(HP).")]
        [SerializeField] private float healPerSecond = 15f;
        [Tooltip("무적(회복) 상태의 최대 지속시간(초). 현재 판정에 쓰이지 않고 값만 보관합니다(13번 기획서로 시간 기반 판정 제거).")]
        [SerializeField] private float invulnerablePhaseTimeoutSeconds = 10f;

        public float RecoverHpRatio => recoverHpRatio;
        public float HealPerSecond => healPerSecond;
        public float InvulnerablePhaseTimeoutSeconds => invulnerablePhaseTimeoutSeconds;
    }
}
