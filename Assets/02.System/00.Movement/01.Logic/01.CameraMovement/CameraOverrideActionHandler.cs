using System;
using GameEvent;
using VContainer;

namespace CameraMovement
{
    /// <summary>
    /// CameraOverrideAction(이벤트 동작 SO)을 수행하는 핸들러입니다([L] 호출형 순수 로직, 10번 기획서 10.7/10.8절, 22번 기획서). MonoBehaviour나
    /// Transform 등 구체 타입을 직접 참조하지 않습니다 - 시선 대상(이벤트 투사체) 위치는 CameraFollowService가 주입받는 IEventCameraLookTarget(EventProjectileLookTarget)이
    /// 값으로 공급합니다.
    ///
    /// 설계: 평상시 3D 카메라는 다크소울3식 3인칭 숄더뷰 하나로 고정이며(22번 기획서), 강 이벤트 구간에는 숄더 카메라가 캐릭터 몸에 투사체를 가리므로
    /// 이벤트 전용 1인칭 가상 카메라(눈 위치, 시선 = 카메라→이벤트 투사체)로 Cinemachine 블렌드 전환합니다(22번 §10). 이 핸들러는 이벤트 SO의 카메라 수치
    /// (블렌드 시간/곡선, FOV, 눈 높이)를 카메라 서비스에 넘기고 오버라이드 ON/OFF 시점만 정확히 지시하며, 가상 카메라 전환은 CameraFollowVisualizer가
    /// 오버라이드 변경 이벤트를 받아 수행합니다. 해제(release) 동작은 시작 때 받은 수치를 그대로 두고 오버라이드만 끕니다(복귀 블렌드도 같은 수치를 씁니다).
    ///
    /// 예전에는 06(GameEvent)의 OnBoss50Percent/OnCoreHitSuccess/OnCoreHitFail 고정 신호를 구독하는 EventCameraTriggerLogicSystem이었습니다(Goal 51에서 이벤트 동작 핸들러로 전환).
    /// 이벤트 시스템은 이 클래스가 등록되는 LocomotionLifetimeScope(부모: CharacterLifetimeScope)와 서로 다른 VContainer 스코프 트리라 생성자 주입이 불가능하므로
    /// LocomotionLifetimeScope가 Start()에서 이벤트 시스템에 사후 등록합니다.
    /// </summary>
    public class CameraOverrideActionHandler : EventActionHandler<CameraOverrideAction>
    {
        private readonly ICameraFollowService m_cameraFollowService;

        [Inject]
        public CameraOverrideActionHandler(ICameraFollowService cameraFollowService)
        {
            m_cameraFollowService = cameraFollowService ?? throw new ArgumentNullException(nameof(cameraFollowService));
        }

        protected override void Execute(CameraOverrideAction action, IEventContext context)
        {
            if (action.Release)
            {
                m_cameraFollowService.SetEventTargetOverride(false);
                return;
            }

            m_cameraFollowService.BindEventCameraSettings(action);
            m_cameraFollowService.SetEventTargetOverride(true);
        }
    }
}
