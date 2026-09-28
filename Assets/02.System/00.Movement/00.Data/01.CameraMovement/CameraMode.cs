namespace CameraMovement
{
    public enum CameraMode
    {
        HybridFocus = 0,
        PlayerOnly = 1,
        MouseFocus = 2,
        FirstPerson = 3,
        ThirdPersonShoulder = 4,

        /// <summary>3단계 보스 50% 이벤트 추적 카메라(10번 기획서 10.2/10.8절, 설계 정정 이후에도 유지). 06의
        /// OnBoss50Percent 신호로 진입하며, 활성 중에는 마우스 회전/줌 입력이 차단된다(InputContextType.Event).
        /// 값 6 고정(과거 FixedQuarterView=5가 제거되었으므로 기존 직렬화 데이터와의 호환을 위해 명시적으로 유지).</summary>
        EventTargetTracking = 6
    }
}

