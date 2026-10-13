using Common.InputSystem;

namespace UnitSystem
{
    /// <summary>
    /// 마법 락온/지정 입력(좌클릭 락온, 우클릭 취소, 팝업 Q/W/E/F)을 받아도 되는 입력 컨텍스트인지 판정하는
    /// 호출형 순수 로직입니다(15-B 후속). MagicLockOnComponent와 MagicSelectionPopupUIComponent가 같은 규칙을
    /// 쓰도록 판정을 이 한 곳에 모읍니다.
    ///
    /// 허용 규칙:
    ///  - 컨텍스트 없음/Tactical: 허용(일반 시간정지 락온).
    ///  - Event: 마법 지정 창이 열려 있는 동안만 허용(13번 §0 "이벤트 중에도 락온/지정 가능", 전환점 이후 이탈 구간은
    ///    IsDesignationClosed로 13-D 차단). 이벤트 중에는 PlayerLockOnController가 Tactical을 Event 아래에 쌓으므로
    ///    스택 최상단이 Event다. 호출부(MagicLockOnComponent는 시간정지 중에만 활성, 팝업은 지정 대기 중에만 열림)가
    ///    "마법 락온 모드 활성" 전제를 이미 보장한다.
    ///  - Player/UI 등 그 외: 차단(UI 메뉴가 열린 동안 락온/팝업 입력 차단 유지).
    /// 카메라 회전 차단(CameraFollowService)과 이벤트 중 T/인벤토리 차단은 이 판정과 무관하게 그대로 유지된다.
    /// </summary>
    public static class MagicLockOnInputGate
    {
        public static bool IsInputAllowed(IInputContext currentContext, RuntimeDataMultiLockOn multiLockOnData)
        {
            if (currentContext == null) return true;

            switch (currentContext.ContextType)
            {
                case InputContextType.Tactical:
                    return true;
                case InputContextType.Event:
                    return multiLockOnData != null && !multiLockOnData.IsDesignationClosed;
                default:
                    return false;
            }
        }
    }
}
