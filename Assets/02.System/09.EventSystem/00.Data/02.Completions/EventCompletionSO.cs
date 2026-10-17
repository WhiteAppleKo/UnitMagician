using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 "완료 조건"의 기반 클래스입니다([D] Pure Data 계열 전략 SO). 파생 SO는 판정 수치만 필드로 가지며, 실제 판정은 파생 타입마다
    /// 한 개씩 등록되는 평가기(IEventCompletionEvaluator)가 합니다. 평가기는 성공/실패 결과를 낼 수 있습니다.
    /// </summary>
    public abstract class EventCompletionSO : ScriptableObject
    {
    }
}
