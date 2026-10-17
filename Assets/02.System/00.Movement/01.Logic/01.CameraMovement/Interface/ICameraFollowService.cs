using System;
using UnityEngine;
using GameEvent;

namespace CameraMovement
{
    /// <summary>
    /// 카메라 추적 서비스(22번 기획서). 평상시 시점은 3인칭 숄더뷰 하나로 고정이다. 3단계 이벤트 구간에는 숄더 카메라를 고치지 않고
    /// 별도의 이벤트 전용 1인칭 가상 카메라(캐릭터 눈 위치, 시선 = 카메라→이벤트 투사체)로 Cinemachine 블렌드 전환한다(22번 §10).
    /// 이 서비스는 "이벤트 오버라이드 ON/OFF 상태"와 "이벤트 1인칭 카메라의 눈 위치/시선 계산"을 맡고, 가상 카메라 활성화·블렌드·몸 숨김은
    /// 비주얼라이저가 수행한다.
    /// </summary>
    public interface ICameraFollowService
    {
        /// <summary>숄더 카메라 피벗 위치(플레이어 위치 + 머리 높이).</summary>
        Vector3 CurrentTargetPosition { get; }

        /// <summary>플레이어 시선각(x: pitch, y: yaw). 마우스 입력으로만 변하며 이벤트 오버라이드 중에는 변경되지 않는다.</summary>
        Vector2 CurrentLookAngles { get; }

        /// <summary>이벤트 오버라이드 요청 플래그(SetEventTargetOverride로 설정한 값). true인 동안 이벤트 1인칭 가상 카메라가 활성이어야 한다.</summary>
        bool IsEventTargetOverrideActive { get; }

        PureDataCameraSetting Setting { get; }

        /// <summary>
        /// 이벤트 1인칭 카메라 수치(블렌드 시간/곡선/FOV/눈 높이)의 읽기 전용 뷰. 원본은 이벤트 SO(CameraOverrideAction)이며 카메라 오버라이드 시작 동작
        /// (CameraOverrideActionHandler)이 BindEventCameraSettings로 넘긴다(Goal 51). 시작 동작이 한 번도 실행되기 전에는 null.
        /// </summary>
        IEventCameraSettings EventCameraSettings { get; }

        event Action<Vector3> OnTargetPositionChanged;
        event Action<Vector2> OnLookAnglesChanged;
        event Action<bool> OnEventTargetOverrideChanged;

        void SetTarget(Transform target);

        /// <summary>이벤트 카메라 수치(IEventCameraSettings)를 넘긴다. 카메라 오버라이드 시작 동작이 오버라이드를 켜기 전에 호출한다(재호출하면 새 수치로 교체).</summary>
        void BindEventCameraSettings(IEventCameraSettings settings);

        /// <summary>이벤트 오버라이드를 켜거나 끈다. 이미 같은 상태면 아무 일도 하지 않는다.</summary>
        void SetEventTargetOverride(bool active);

        /// <summary>
        /// 이벤트 1인칭 카메라의 눈 위치와 시선을 계산한다. 눈 위치 = 플레이어 위치 + 눈 높이(IEventCameraSettings.EventCameraEyeHeight),
        /// 시선 = 눈→이벤트 투사체 방향(IEventCameraLookTarget). 시선 대상 위치가 없거나 눈과 겹쳐 방향을 정의할 수 없으면 false.
        /// </summary>
        bool TryGetEventCameraPose(Vector3 playerPosition, out Vector3 eyePosition, out Quaternion lookRotation);

        void UpdateCameraOffset(Vector2 mouseInput);
        void SetRequireRightClickToRotate(bool require);
    }
}
