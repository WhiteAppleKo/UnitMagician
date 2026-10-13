using UnityEngine;

namespace CameraMovement
{
    /// <summary>
    /// 이벤트 카메라(CameraFollowService의 이벤트 대상 오버라이드)가 바라볼 위치를 값(Vector3)으로만 노출하는 인터페이스입니다
    /// (22번 기획서 §8). 카메라 로직은 Transform이나 이벤트 시퀀서의 구체 타입을 알지 못하고 이 인터페이스에만 의존합니다.
    /// </summary>
    public interface IEventCameraLookTarget
    {
        /// <summary>바라볼 위치(현재는 이벤트 투사체의 매 프레임 월드 위치). 공급원이 아직 연결되지 않았으면 false.</summary>
        bool TryGetPosition(out Vector3 position);
    }
}
