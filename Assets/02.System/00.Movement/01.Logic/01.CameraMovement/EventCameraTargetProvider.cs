using UnityEngine;

namespace CameraMovement
{
    /// <summary>
    /// IEventCameraTargetProvider의 임시 구현체입니다. 02(아군NPC)/03(보스AI)이 아직 구현되지 않아,
    /// 씬에 미리 배치된 임의 테스트 오브젝트(Transform)를 "아군 발사 위치"/"보스 위치"로 대체해 검증합니다
    /// (Goal 프롬프트 "제외" 절). MouseWorldPositionProvider와 동일하게, 실제 좌표 연산을 위해 Transform을
    /// 직접 보유하는 것이 불가피한 Provider 계층입니다(EventCameraTriggerLogicSystem 등 판정 로직 계층은
    /// Transform을 직접 참조하지 않습니다).
    /// </summary>
    public class EventCameraTargetProvider : IEventCameraTargetProvider
    {
        private readonly Transform m_allyLaunchTransform;
        private readonly Transform m_bossTransform;

        public EventCameraTargetProvider(Transform allyLaunchTransform, Transform bossTransform)
        {
            m_allyLaunchTransform = allyLaunchTransform;
            m_bossTransform = bossTransform;
        }

        public Vector3 GetAllyLaunchPosition()
        {
            return m_allyLaunchTransform != null ? m_allyLaunchTransform.position : Vector3.zero;
        }

        public Vector3 GetBossPosition()
        {
            return m_bossTransform != null ? m_bossTransform.position : Vector3.zero;
        }
    }
}
