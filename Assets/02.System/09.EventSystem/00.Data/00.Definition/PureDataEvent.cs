using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 한 개의 정의([D] Pure Data, "이벤트 SO")입니다. 시작 조건(trigger) + 강제할 동작(onStart) + 완료 조건(completion) +
    /// 결과별 동작(onSuccess/onFail)을 담고, 실행기(EventLogicSystem)는 어떤 PureDataEvent가 들어오든 같은 방식으로 실행합니다.
    /// 사용자 확정(25번 기획서 §1-1): "모든 값은 이벤트 SO를 통해 값을 조정할 수 있어야 한다" - 이벤트에 쓰이는 수치는 트리거/완료/동작 SO의 필드에만 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataEvent_", menuName = "GameEvent/PureDataEvent")]
    public class PureDataEvent : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("이벤트 식별자. 같은 씬에 등록된 이벤트들 사이에서 유일해야 합니다(requiresCompleted가 이 값으로 선행 이벤트를 가리킵니다).")]
        [SerializeField] private string eventId;
        [Tooltip("약/강 구분: 플레이어에게 영향을 주냐 안 주냐(Weak = 영향 없음).")]
        [SerializeField] private EventIntensity intensity = EventIntensity.Weak;

        [Header("Start Condition")]
        [Tooltip("시작 조건(트리거). 선행 이벤트가 모두 완료된 뒤부터 감시를 시작합니다.")]
        [SerializeField] private EventTriggerSO trigger;
        [Tooltip("선행으로 완료돼야 하는 이벤트 id 목록. 비어 있으면 씬 시작과 함께 트리거 감시를 시작합니다.")]
        [SerializeField] private string[] requiresCompleted = new string[0];

        [Header("Actions / Completion")]
        [Tooltip("시작 시 위에서부터 순서대로 실행할 동작.")]
        [SerializeField] private EventActionSO[] onStart = new EventActionSO[0];
        [Tooltip("완료 판정 방식. 비어 있으면 onStart 실행 직후 성공으로 완료됩니다.")]
        [SerializeField] private EventCompletionSO completion;
        [Tooltip("완료 결과가 Success일 때 순서대로 실행할 동작.")]
        [SerializeField] private EventActionSO[] onSuccess = new EventActionSO[0];
        [Tooltip("완료 결과가 Fail일 때 순서대로 실행할 동작.")]
        [SerializeField] private EventActionSO[] onFail = new EventActionSO[0];
        [Tooltip("실패 후 다시 시작 조건을 감시할지 여부.")]
        [SerializeField] private bool rearmOnFail;

        public string EventId => eventId;
        public EventIntensity Intensity => intensity;
        public EventTriggerSO Trigger => trigger;
        public string[] RequiresCompleted => requiresCompleted;
        public EventActionSO[] OnStart => onStart;
        public EventCompletionSO Completion => completion;
        public EventActionSO[] OnSuccess => onSuccess;
        public EventActionSO[] OnFail => onFail;
        public bool RearmOnFail => rearmOnFail;
    }
}
