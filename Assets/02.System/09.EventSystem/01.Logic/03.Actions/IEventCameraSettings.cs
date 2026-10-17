namespace GameEvent
{
    /// <summary>
    /// 이벤트(3단계 투사체 비행) 구간의 이벤트 1인칭 카메라 연출 수치를 읽기 전용으로 노출하는 인터페이스입니다.
    /// 값의 원본은 이벤트 SO(CameraOverrideAction)이며, 카메라 시스템(CameraFollowService/CameraFollowVisualizer)은 구체 SO가 아니라
    /// 이 인터페이스로만 읽습니다(DIP, 22번 기획서 §11 "조정 값 원칙"). 카메라 오버라이드 시작 동작이 실행될 때 카메라 서비스에 넘겨집니다.
    /// </summary>
    public interface IEventCameraSettings
    {
        /// <summary>평상시 숄더 가상 카메라 &lt;-&gt; 이벤트 1인칭 가상 카메라 전환(Cinemachine 블렌드) 시간(초). 0 이하면 즉시 컷.</summary>
        float EventCameraBlendTime { get; }

        /// <summary>블렌드 보간 곡선 종류.</summary>
        EventCameraBlendStyle EventCameraBlendStyle { get; }

        /// <summary>이벤트 1인칭 가상 카메라의 세로 시야각(FOV).</summary>
        float EventCameraFieldOfView { get; }

        /// <summary>이벤트 1인칭 카메라(캐릭터 눈) 높이. 플레이어 루트 위치 기준 위쪽 오프셋(m).</summary>
        float EventCameraEyeHeight { get; }
    }
}
