namespace GameEvent
{
    /// <summary>
    /// 이벤트 카메라 블렌드(평상시 숄더 카메라 &lt;-&gt; 이벤트 1인칭 카메라)의 보간 곡선 종류입니다. CameraOverrideAction이 선택하고
    /// 카메라 시각화(CameraFollowVisualizer)가 Cinemachine 블렌드 정의로 옮깁니다(기존에는 EaseInOut이 코드에 고정이었음).
    /// </summary>
    public enum EventCameraBlendStyle
    {
        EaseInOut,
        EaseIn,
        EaseOut,
        HardIn,
        HardOut,
        Linear
    }
}
