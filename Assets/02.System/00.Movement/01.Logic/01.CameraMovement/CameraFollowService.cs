using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using Common.InputSystem;

namespace CameraMovement
{
    /// <summary>
    /// 카메라 추적 서비스(22번 기획서). 평상시 시점은 3인칭 숄더뷰 하나로 고정이며, 플레이어 시선각(m_lookAngles)은 마우스 입력으로만 변한다.
    ///
    /// 3단계 이벤트 구간은 숄더 카메라의 시선을 고치지 않고 이벤트 전용 1인칭 가상 카메라로 Cinemachine 블렌드 전환한다(22번 §10).
    /// 숄더 카메라는 투사체가 캐릭터 몸에 가려지기 때문이다. 이 서비스는
    ///  - 이벤트 오버라이드 ON/OFF 상태(SetEventTargetOverride, OnEventTargetOverrideChanged)와
    ///  - 이벤트 1인칭 카메라의 눈 위치/시선 계산(TryGetEventCameraPose: 눈 = 플레이어 위치 + 눈 높이, 시선 = 눈→이벤트 투사체)
    /// 만 맡고, 가상 카메라 활성화/블렌드/몸 렌더 숨김은 CameraFollowVisualizer가 수행한다.
    /// 플레이어 시선각은 이벤트 중에도 변경하지 않는다(스냅샷 저장/복원 없음) — 오버라이드 중에는 마우스 입력이 무시되므로 종료 후
    /// 숄더 카메라는 이벤트 전 시선 그대로 복귀한다.
    /// </summary>
    public class CameraFollowService : ICameraFollowService, ILateTickable, IDisposable
    {
        private readonly PureDataCameraSetting m_cameraSetting;
        private readonly ICameraModeCalculationStrategy m_shoulderStrategy;
        private readonly IEventCameraLookTarget m_eventLookTarget;
        private readonly InputReader m_inputReader;
        private readonly IInputContextManager m_inputContextManager;

        private Transform m_targetTransform;
        private Vector3 m_currentTargetPosition;

        private Vector2 m_lookAngles; // x: pitch (상하), y: yaw (좌우) — 플레이어 시선각(이벤트 중 불변)

        private bool m_eventTargetOverrideActive;
        private bool m_requireRightClickToRotate = false;

        public Vector3 CurrentTargetPosition => m_currentTargetPosition;
        public Vector2 CurrentLookAngles => m_lookAngles;
        public bool IsEventTargetOverrideActive => m_eventTargetOverrideActive;
        public PureDataCameraSetting Setting => m_cameraSetting;

        public event Action<Vector3> OnTargetPositionChanged;
        public event Action<Vector2> OnLookAnglesChanged;
        public event Action<bool> OnEventTargetOverrideChanged;

        [Inject]
        public CameraFollowService(
            PureDataCameraSetting cameraSetting,
            ICameraModeCalculationStrategy shoulderStrategy,
            IEventCameraLookTarget eventLookTarget,
            InputReader inputReader = null,
            IInputContextManager contextManager = null)
        {
            m_cameraSetting = cameraSetting;
            m_shoulderStrategy = shoulderStrategy;
            m_eventLookTarget = eventLookTarget;
            m_inputReader = inputReader;
            m_inputContextManager = contextManager;
            // 카메라 줌 기능 폐지(§9) — 휠 입력은 더 이상 구독하지 않는다(휠은 퀵슬롯 순환 전용).
        }

        public void SetTarget(Transform target)
        {
            m_targetTransform = target;
            if (target != null)
            {
                m_currentTargetPosition = target.position;
                m_lookAngles = new Vector2(0f, target.eulerAngles.y);
                OnTargetPositionChanged?.Invoke(m_currentTargetPosition);
                OnLookAnglesChanged?.Invoke(m_lookAngles);
            }
        }

        public void SetEventTargetOverride(bool active)
        {
            if (m_eventTargetOverrideActive == active) return;

            m_eventTargetOverrideActive = active;
            OnEventTargetOverrideChanged?.Invoke(active);
        }

        public bool TryGetEventCameraPose(Vector3 playerPosition, out Vector3 eyePosition, out Quaternion lookRotation)
        {
            eyePosition = playerPosition + Vector3.up * m_cameraSetting.EventCameraEyeHeight;
            lookRotation = Quaternion.identity;

            if (m_eventLookTarget == null || !m_eventLookTarget.TryGetPosition(out Vector3 targetPosition)) return false;

            Vector3 toTarget = targetPosition - eyePosition;
            if (toTarget.sqrMagnitude < 0.0001f) return false;

            lookRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            return true;
        }

        public void SetRequireRightClickToRotate(bool require)
        {
            m_requireRightClickToRotate = require;
        }

        public void Dispose()
        {
            // 휠 입력을 구독하지 않으므로 해제할 것도 없다(§9).
        }

        public void LateTick()
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            // UI 메뉴 / 이벤트 카메라 활성화 중에는 마우스 시점 회전 조작 완전 차단
            // (Event 추가: 10번 기획서 10.2/10.8절 - 06의 OnBoss50Percent로 진입한 이벤트 카메라 동안 회귀 없이 차단)
            var currentContext = m_inputContextManager?.CurrentContext;
            if (currentContext != null &&
                (currentContext.ContextType == InputContextType.UI || currentContext.ContextType == InputContextType.Event))
            {
                UpdateCameraOffset(Vector2.zero);
                return;
            }

            var reader = m_inputReader ?? UnityEngine.Object.FindAnyObjectByType<InputReader>();
            Vector2 mouseDelta = Vector2.zero;

            if (reader != null)
            {
                if (!m_requireRightClickToRotate || (Mouse.current != null && Mouse.current.rightButton.isPressed))
                {
                    mouseDelta = reader._mouseDelta;
                }
            }
            else if (Mouse.current != null)
            {
                if (!m_requireRightClickToRotate || Mouse.current.rightButton.isPressed)
                {
                    mouseDelta = Mouse.current.delta.ReadValue();
                }
            }

            UpdateCameraOffset(mouseDelta);
        }

        public void UpdateCameraOffset(Vector2 mouseInput)
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            // 이벤트 카메라 활성 중에는 이 메서드가 어떤 경로로 호출되어도(LateTick 경유든 외부에서 직접 호출하든)
            // 마우스 회전 입력이 플레이어 시선각에 반영되지 않아야 한다(10.6/10.8절 DoD, 22번 기획서 §3-1).
            // LateTick()의 조기 반환(위 UI/Event 체크)만으로는 UpdateCameraOffset을 직접 호출하는 경로까지
            // 막을 수 없으므로, 이 메서드 최상단에서도 동일하게 입력을 무효화한다.
            var eventGateContext = m_inputContextManager?.CurrentContext;
            if (m_eventTargetOverrideActive ||
                (eventGateContext != null && eventGateContext.ContextType == InputContextType.Event))
            {
                mouseInput = Vector2.zero;
            }

            Vector3 basePos = m_targetTransform.position + Vector3.up * 1.4f;

            // 1. 플레이어 시선각 연산(숄더 전략) — 이벤트 오버라이드 중에는 입력이 0이라 변하지 않는다.
            Vector2 lookAngles = m_shoulderStrategy.CalculateLookAngles(m_lookAngles, mouseInput, m_cameraSetting);
            Vector3 pivotPos = m_shoulderStrategy.CalculateTargetPivot(basePos, mouseInput, m_cameraSetting.DefaultZoomRatio, m_cameraSetting);

            // 2. 값 갱신 및 통지
            if (lookAngles != m_lookAngles)
            {
                m_lookAngles = lookAngles;
                OnLookAnglesChanged?.Invoke(m_lookAngles);
            }

            m_currentTargetPosition = pivotPos;
            OnTargetPositionChanged?.Invoke(m_currentTargetPosition);
        }
    }
}
