using UnityEngine;
using VContainer;

namespace CameraMovement
{
    /// <summary>
    /// 3단계 보스 50% 이벤트 추적 카메라 전략입니다(10번 기획서 10.2/10.8/10.9절). 아군 발사 위치~보스 위치
    /// 사이 궤적을 비추는 구도를 계산하며, 유저 마우스 입력은 완전히 무시합니다(EventCameraTriggerLogicSystem이
    /// 진입 시 함께 Push하는 InputContextType.Event가 CameraFollowService 레벨에서 회전/줌을 추가로 차단합니다).
    ///
    /// IEventCameraTargetProvider는 02(아군NPC)/03(보스AI)이 아직 구현되지 않아 DI 미등록 상태(null)일 수
    /// 있습니다 — 이 경우 basePos/currentAngles로 안전하게 폴백합니다(TopViewMouseFocusStrategy의
    /// Vector3.zero 폴백과 동일한 방어 패턴).
    /// </summary>
    public class EventTargetTrackingCameraStrategy : ICameraModeCalculationStrategy
    {
        private readonly IEventCameraTargetProvider m_targetProvider;

        public CameraMode Mode => CameraMode.EventTargetTracking;

        [Inject]
        public EventTargetTrackingCameraStrategy(IEventCameraTargetProvider targetProvider = null)
        {
            m_targetProvider = targetProvider;
        }

        public Vector3 CalculateTargetPivot(Vector3 basePos, Vector2 mouseInput, float zoomRatio, PureDataCameraSetting setting)
        {
            if (m_targetProvider == null) return basePos;

            Vector3 allyPos = m_targetProvider.GetAllyLaunchPosition();
            Vector3 bossPos = m_targetProvider.GetBossPosition();
            if (allyPos == Vector3.zero && bossPos == Vector3.zero) return basePos;

            // 아군 발사 지점~보스 사이 궤적 중점을 비추는 구도(플레이스홀더 연산 - 정밀 프레이밍은 범위 밖).
            return Vector3.Lerp(allyPos, bossPos, 0.5f);
        }

        public Vector2 CalculateLookAngles(Vector2 currentAngles, Vector2 mouseInput, PureDataCameraSetting setting)
        {
            if (m_targetProvider == null) return currentAngles;

            Vector3 allyPos = m_targetProvider.GetAllyLaunchPosition();
            Vector3 bossPos = m_targetProvider.GetBossPosition();
            Vector3 direction = bossPos - allyPos;
            if (direction.sqrMagnitude < 0.0001f) return currentAngles;

            Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Vector3 euler = look.eulerAngles;

            // Unity의 0~360 pitch 표현을 기존 VerticalAngleLimits와 동일한 -180~180 좌표계로 정규화한다.
            float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            return new Vector2(pitch, euler.y);
        }
    }
}
