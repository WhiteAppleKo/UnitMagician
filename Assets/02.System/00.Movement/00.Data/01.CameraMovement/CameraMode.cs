using System;

namespace CameraMovement
{
    /// <summary>
    /// 카메라 시점은 3인칭 숄더뷰 하나로 고정이다(22번 기획서). 3단계 이벤트 카메라도 별도 모드가 아니라
    /// ICameraFollowService의 이벤트 대상 오버라이드로 구현한다.
    /// 에셋(CameraSettingSO.asset)이 정수로 직렬화되어 있고 99.Legacy 파일이 멤버를 참조하므로 enum 자체와 값(0~4, 6)은
    /// 삭제하지 않고, 활성 멤버는 ThirdPersonShoulder(=4)만 남긴다. 나머지는 호환용 [Obsolete]이다.
    /// </summary>
    public enum CameraMode
    {
        [Obsolete("숄더뷰 고정 — 호환용")]
        HybridFocus = 0,

        [Obsolete("숄더뷰 고정 — 호환용")]
        PlayerOnly = 1,

        [Obsolete("숄더뷰 고정 — 호환용")]
        MouseFocus = 2,

        [Obsolete("숄더뷰 고정 — 호환용")]
        FirstPerson = 3,

        ThirdPersonShoulder = 4,

        /// <summary>과거 이벤트 추적 카메라 모드. 이제 이벤트 대상 오버라이드(ICameraFollowService.SetEventTargetOverride)로 대체되었다.
        /// 값 6은 기존 직렬화 데이터와의 호환을 위해 명시적으로 유지한다(과거 FixedQuarterView=5가 제거됨).</summary>
        [Obsolete("숄더뷰 고정 — 호환용")]
        EventTargetTracking = 6
    }
}
