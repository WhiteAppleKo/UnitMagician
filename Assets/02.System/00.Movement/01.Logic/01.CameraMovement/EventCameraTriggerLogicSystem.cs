using System;
using Common.InputSystem;
using EventSequencerSystem;
using VContainer;

namespace CameraMovement
{
    /// <summary>
    /// 06(EventSequencerSystem)의 신호(OnBoss50Percent/OnCoreHitSuccess/OnCoreHitFail)를 구독해
    /// ICameraFollowService.SetCameraMode + IInputContextManager.PushContext/PopContext를 정확한 시점에
    /// 호출하는 판정 전용 로직입니다([L] 호출형 순수 로직, 10번 기획서 10.7/10.8절, 설계 정정 이후). MonoBehaviour나
    /// Transform 등 구체 타입을 직접 참조하지 않습니다(Goal 프롬프트 완료 기준 항목) — 카메라 좌표 계산에
    /// 필요한 Transform 참조는 이 클래스가 아니라 EventTargetTrackingCameraStrategy
    /// (및 그 아래 EventCameraTargetProvider) 계층에 격리되어 있습니다.
    ///
    /// 설계 정정(10번 기획서 10.1~10.9절 정정 완료): 3D 카메라는 항상 다크소울3식 3인칭 숄더뷰가 기본이며,
    /// 1단계 퍼즐 구간에서도 유저가 평소처럼 카메라를 직접 조작합니다 — 씬 진입 시 고정 쿼터뷰로 강제 전환하던
    /// 로직(과거 Initialize())은 제거되었습니다. 3단계 이벤트 추적 카메라(BindEventSequencer 이하)는 그대로 유지됩니다.
    ///
    /// 06(EventSequencerSystem)은 이 클래스가 등록되는 LocomotionLifetimeScope(부모: CharacterLifetimeScope)와
    /// 서로 다른 VContainer 스코프 트리(EventSequencerSystemLifetimeScope의 parentReference는
    /// InteractionSystemLifetimeScope)에 있어 생성자 주입이 불가능합니다 — 09번 작업에서 실측된 현상과 동일하며
    /// (CharacterLifetimeScope.cs/TimeSlowLogicSystem.cs의 eventSequencerScope 패턴 참고), 동일한 해법인
    /// 인스펙터 직접 참조 + 두 컨테이너가 모두 준비된 뒤 Start()에서 BindEventSequencer() 사후 배선을 그대로
    /// 재사용합니다(LocomotionLifetimeScope.cs 참고).
    /// </summary>
    public class EventCameraTriggerLogicSystem : IDisposable
    {
        private readonly ICameraFollowService m_cameraFollowService;
        private readonly IInputContextManager m_inputContextManager;

        private IEventSequencer m_eventSequencer;
        private IInputContext m_pushedEventContext;
        private CameraMode m_modeBeforeEventCamera = CameraMode.ThirdPersonShoulder;

        [Inject]
        public EventCameraTriggerLogicSystem(
            ICameraFollowService cameraFollowService,
            IInputContextManager inputContextManager)
        {
            m_cameraFollowService = cameraFollowService ?? throw new ArgumentNullException(nameof(cameraFollowService));
            m_inputContextManager = inputContextManager ?? throw new ArgumentNullException(nameof(inputContextManager));
        }

        /// <summary>
        /// 06(EventSequencerSystem) 연동 사후 배선. LifetimeScope가 두 컨테이너 Build 완료 후(Start())
        /// 호출한다. 재호출 시 기존 구독을 먼저 해제해 중복 구독을 막는다(TimeSlowLogicSystem.BindEventSequencer와
        /// 동일 패턴).
        /// </summary>
        public void BindEventSequencer(IEventSequencer sequencer)
        {
            UnbindEventSequencer();

            m_eventSequencer = sequencer;
            if (m_eventSequencer != null)
            {
                m_eventSequencer.OnBoss50Percent += HandleBoss50Percent;
                m_eventSequencer.OnCoreHitSuccess += HandleEventCameraEnd;
                m_eventSequencer.OnCoreHitFail += HandleEventCameraEnd;
            }
        }

        private void UnbindEventSequencer()
        {
            if (m_eventSequencer == null) return;

            m_eventSequencer.OnBoss50Percent -= HandleBoss50Percent;
            m_eventSequencer.OnCoreHitSuccess -= HandleEventCameraEnd;
            m_eventSequencer.OnCoreHitFail -= HandleEventCameraEnd;
            m_eventSequencer = null;
        }

        public void Dispose()
        {
            UnbindEventSequencer();
        }

        /// <summary>3단계 보스 50%: 이벤트 추적 카메라로 전환하고 Event 입력 컨텍스트를 Push한다(10.8절 데이터 흐름).</summary>
        private void HandleBoss50Percent()
        {
            m_modeBeforeEventCamera = m_cameraFollowService.CurrentMode;

            m_cameraFollowService.SetCameraMode(CameraMode.EventTargetTracking);

            m_pushedEventContext = new EventCameraInputContext();
            m_inputContextManager.PushContext(m_pushedEventContext);
        }

        /// <summary>결정타 성공/실패 신호 수신 시 Event 컨텍스트를 Pop하고 원래 모드로 복귀한다(10.5/10.8절).</summary>
        private void HandleEventCameraEnd()
        {
            if (m_pushedEventContext != null)
            {
                m_inputContextManager.PopContext(m_pushedEventContext);
                m_pushedEventContext = null;
            }

            m_cameraFollowService.SetCameraMode(m_modeBeforeEventCamera);
        }
    }
}
