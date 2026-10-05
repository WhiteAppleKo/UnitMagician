using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using Common.InputSystem;

namespace CameraMovement
{
    public class CameraFollowService : ICameraFollowService, ILateTickable, IDisposable
    {
        private readonly PureDataCameraSetting m_cameraSetting;
        private readonly IMouseWorldPositionProvider m_mousePositionProvider;
        private readonly Dictionary<CameraMode, ICameraModeCalculationStrategy> m_strategies;
        private readonly InputReader m_inputReader;

        private Transform m_targetTransform;
        private Vector3 m_currentTargetPosition;
        private Vector3 m_smoothVelocity;

        private Vector2 m_currentLookAngles; // x: pitch (상하), y: yaw (좌우)
        private float m_targetZoomRatio = 0.5f;
        private float m_currentZoomRatio = 0.5f;
        private float m_zoomVelocity;
        private CameraMode m_currentMode = CameraMode.ThirdPersonShoulder;
        private bool m_requireRightClickToRotate = false;

        public Vector3 CurrentTargetPosition => m_currentTargetPosition;
        public Vector2 CurrentLookAngles => m_currentLookAngles;
        public float CurrentZoomSize => m_currentZoomRatio;
        public CameraMode CurrentMode => m_currentMode;
        public PureDataCameraSetting Setting => m_cameraSetting;

        public event Action<Vector3> OnTargetPositionChanged;
        public event Action<Vector2> OnLookAnglesChanged;
        public event Action<float> OnZoomSizeChanged;
        public event Action<CameraMode> OnCameraModeChanged;

        private readonly IInputContextManager m_inputContextManager;

        [Inject]
        public CameraFollowService(
            PureDataCameraSetting cameraSetting,
            IMouseWorldPositionProvider mousePositionProvider = null,
            IReadOnlyList<ICameraModeCalculationStrategy> strategies = null,
            InputReader inputReader = null,
            IInputContextManager contextManager = null)
        {
            m_cameraSetting = cameraSetting;
            m_mousePositionProvider = mousePositionProvider ?? new MouseWorldPositionProvider();
            m_inputReader = inputReader;
            m_inputContextManager = contextManager;
            // 카메라 줌 기능 폐지(§9) — 휠 입력은 더 이상 구독하지 않는다(휠은 퀵슬롯 순환 전용).

            m_strategies = new Dictionary<CameraMode, ICameraModeCalculationStrategy>();
            if (strategies != null)
            {
                for (int i = 0; i < strategies.Count; i++)
                {
                    var strat = strategies[i];
                    if (strat != null)
                    {
                        m_strategies[strat.Mode] = strat;
                    }
                }
            }

            // 누락된 기본 전략 fallback 등록
            EnsureDefaultStrategies();

            if (cameraSetting != null)
            {
                m_currentMode = cameraSetting.DefaultMode;
                float defaultZoomRatio = cameraSetting.DefaultZoomRatio;
                m_targetZoomRatio = defaultZoomRatio;
                m_currentZoomRatio = defaultZoomRatio;
            }
        }

        private void EnsureDefaultStrategies()
        {
            if (!m_strategies.ContainsKey(CameraMode.FirstPerson))
                m_strategies[CameraMode.FirstPerson] = new FirstPersonCameraStrategy();
            if (!m_strategies.ContainsKey(CameraMode.ThirdPersonShoulder))
                m_strategies[CameraMode.ThirdPersonShoulder] = new ThirdPersonShoulderCameraStrategy();
            if (!m_strategies.ContainsKey(CameraMode.MouseFocus))
                m_strategies[CameraMode.MouseFocus] = new TopViewMouseFocusStrategy(m_mousePositionProvider);
            if (!m_strategies.ContainsKey(CameraMode.HybridFocus))
                m_strategies[CameraMode.HybridFocus] = new HybridFocusCameraStrategy(m_mousePositionProvider);
            if (!m_strategies.ContainsKey(CameraMode.PlayerOnly))
                m_strategies[CameraMode.PlayerOnly] = new PlayerOnlyCameraStrategy();
            if (!m_strategies.ContainsKey(CameraMode.EventTargetTracking))
                m_strategies[CameraMode.EventTargetTracking] = new EventTargetTrackingCameraStrategy();
        }

        public void SetTarget(Transform target)
        {
            m_targetTransform = target;
            if (target != null)
            {
                m_currentTargetPosition = target.position;
                m_currentLookAngles = new Vector2(0f, target.eulerAngles.y);
                OnTargetPositionChanged?.Invoke(m_currentTargetPosition);
                OnLookAnglesChanged?.Invoke(m_currentLookAngles);
                OnZoomSizeChanged?.Invoke(m_currentZoomRatio);
                OnCameraModeChanged?.Invoke(m_currentMode);
            }
        }

        public void SetCameraMode(CameraMode mode)
        {
            if (m_cameraSetting != null && !m_cameraSetting.IsModeAllowed(mode))
            {
                return;
            }

            m_currentMode = mode;
            OnCameraModeChanged?.Invoke(m_currentMode);
        }

        public void SwitchCameraMode(CameraMode mode) => SetCameraMode(mode);

        public void SetRequireRightClickToRotate(bool require)
        {
            m_requireRightClickToRotate = require;
        }

        public void Dispose()
        {
            // 휠 입력 구독을 하지 않으므로 해제할 것도 없다(§9).
        }

        public void LateTick()
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            // UI 메뉴 / 이벤트 카메라(EventTargetTracking 등) 활성화 중에는 마우스 시점 회전 및 줌 조작 완전 차단
            // (Event 추가: 10번 기획서 10.2/10.8절 - 06의 OnBoss50Percent로 진입한 이벤트 카메라 동안 회귀 없이 차단)
            var currentContext = m_inputContextManager?.CurrentContext;
            if (currentContext != null &&
                (currentContext.ContextType == InputContextType.UI || currentContext.ContextType == InputContextType.Event))
            {
                UpdateCameraOffset(Vector2.zero, 0f);
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

            // 카메라 줌 기능 폐지(§9) — 휠 입력은 더 이상 읽지 않으며 항상 0으로 고정한다(휠은 퀵슬롯 순환 전용).
            UpdateCameraOffset(mouseDelta, 0f);
        }

        public void UpdateCameraOffset(Vector2 mouseInput, float wheelDelta)
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            // 이벤트 카메라(EventTargetTracking) 활성 중에는 이 메서드가 어떤 경로로 호출되어도(LateTick 경유든
            // 외부에서 직접 호출하든) 마우스 회전/줌 입력이 결과에 반영되지 않아야 한다(10.6/10.8절 DoD).
            // LateTick()의 조기 반환(위 UI/Event 체크)만으로는 UpdateCameraOffset을 직접 호출하는 경로까지
            // 막을 수 없으므로, 이 메서드 최상단에서도 동일하게 입력을 무효화한다. TargetPivot 계산 자체는
            // (EventTargetTrackingCameraStrategy가 아군/보스 위치를 계속 추적할 수 있도록) 계속 수행한다.
            var eventGateContext = m_inputContextManager?.CurrentContext;
            if (eventGateContext != null && eventGateContext.ContextType == InputContextType.Event)
            {
                mouseInput = Vector2.zero;
                wheelDelta = 0f;
            }

            float followSmoothTime = m_cameraSetting.FollowSmoothTime;
            Vector3 basePos = m_targetTransform.position + Vector3.up * 1.4f;

            if (!m_strategies.TryGetValue(m_currentMode, out var strategy) || strategy == null)
            {
                strategy = m_strategies[CameraMode.PlayerOnly];
            }

            // 1. 시선 각도 연산
            Vector2 previousAngles = m_currentLookAngles;
            m_currentLookAngles = strategy.CalculateLookAngles(m_currentLookAngles, mouseInput, m_cameraSetting);
            if (m_currentLookAngles != previousAngles)
            {
                OnLookAnglesChanged?.Invoke(m_currentLookAngles);
            }

            // 2. 타깃 피벗 위치 연산
            Vector3 targetPivotPos = strategy.CalculateTargetPivot(basePos, mouseInput, m_currentZoomRatio, m_cameraSetting);
            if (m_requireRightClickToRotate && (m_currentMode == CameraMode.MouseFocus || m_currentMode == CameraMode.HybridFocus))
            {
                if (Mouse.current == null || !Mouse.current.rightButton.isPressed)
                {
                    targetPivotPos = basePos;
                }
            }

            m_currentTargetPosition = Vector3.SmoothDamp(
                m_currentTargetPosition,
                targetPivotPos,
                ref m_smoothVelocity,
                followSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

            OnTargetPositionChanged?.Invoke(m_currentTargetPosition);

            // 3. 줌 연산 — 카메라 줌 기능 자체가 폐지되어(§9) wheelDelta는 더 이상 m_targetZoomRatio에 반영하지
            // 않는다. m_targetZoomRatio는 생성자에서 설정된 defaultZoomRatio로 고정된 채 유지된다.
            m_currentZoomRatio = Mathf.SmoothDamp(
                m_currentZoomRatio,
                m_targetZoomRatio,
                ref m_zoomVelocity,
                followSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

            OnZoomSizeChanged?.Invoke(m_currentZoomRatio);
        }
    }
}

