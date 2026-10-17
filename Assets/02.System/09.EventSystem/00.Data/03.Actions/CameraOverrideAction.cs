using UnityEngine;

namespace GameEvent
{
    /// <summary>
    /// 이벤트 카메라 오버라이드를 시작/해제하는 동작입니다. 시작(release = false)하면 평상시 숄더 카메라에서 이벤트 전용 1인칭 카메라(눈 위치,
    /// 시선 = 카메라→이벤트 투사체)로 블렌드 전환되고, 해제(release = true)하면 같은 방식으로 숄더 카메라로 복귀합니다. 해제용 에셋은 release만 켜서 씁니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraOverrideAction_", menuName = "GameEvent/Action/CameraOverride")]
    public class CameraOverrideAction : EventActionSO, IEventCameraSettings
    {
        [Tooltip("켜면 이 동작은 이벤트 카메라 오버라이드를 해제합니다(아래 값은 쓰지 않음. 복귀 블렌드는 시작 때 받은 값을 씁니다).")]
        [SerializeField] private bool release;

        [Header("Event Camera (First Person)")]
        [Tooltip("평상시 숄더 가상 카메라 <-> 이벤트 1인칭 가상 카메라 전환(Cinemachine 블렌드) 시간(초). 이벤트 시작 때 카메라가 '천천히' 투사체 쪽으로 돌아가는 시간입니다. 0 이하면 즉시 컷")]
        [SerializeField] private float blendTime = 0.5f;
        [Tooltip("블렌드 보간 곡선 종류.")]
        [SerializeField] private EventCameraBlendStyle blendStyle = EventCameraBlendStyle.EaseInOut;
        [Tooltip("이벤트 1인칭 가상 카메라의 세로 시야각(FOV)")]
        [SerializeField] private float fieldOfView = 70f;
        [Tooltip("이벤트 1인칭 카메라(캐릭터 눈) 높이. 플레이어 루트 위치 기준 위쪽 오프셋(m)")]
        [SerializeField] private float eyeHeight = 1.68f;

        public bool Release => release;
        public float EventCameraBlendTime => blendTime;
        public EventCameraBlendStyle EventCameraBlendStyle => blendStyle;
        public float EventCameraFieldOfView => fieldOfView;
        public float EventCameraEyeHeight => eyeHeight;
    }
}
