namespace GameEvent
{
    /// <summary>
    /// 이벤트의 강도 구분입니다(25번 기획서 사용자 확정: 약/강 = 플레이어에게 영향을 주냐 안 주냐).
    /// Weak = 플레이어에 영향 없음(NPC/월드 동작만, 예: 돌문 이벤트), Strong = 플레이어에게 영향(카메라/시간 정지/입력 락, 예: 보스 50% 이벤트).
    /// 이 값은 이벤트 정의에 붙는 분류 정보이며 실행기의 흐름을 바꾸지 않습니다.
    /// </summary>
    public enum EventIntensity
    {
        Weak,
        Strong
    }
}
