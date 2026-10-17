using UnityEngine;

namespace GameEvent
{
    /// <summary>이벤트 성공(onSuccess) 시 보스를 처치하는 동작입니다(HP를 0으로 반영하고 사망 페이즈/모션 재생). 값이 없습니다.</summary>
    [CreateAssetMenu(fileName = "BossDefeatAction_", menuName = "GameEvent/Action/BossDefeat")]
    public class BossDefeatAction : EventActionSO
    {
    }
}
