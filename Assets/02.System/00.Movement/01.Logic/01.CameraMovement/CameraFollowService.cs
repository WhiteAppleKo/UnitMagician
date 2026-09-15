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
        private float m_accumulatedWheelDelta;

        private Transform m_targetTransform;
        private Vector3 m_currentTargetPosition;
        private Vector3 m_smoothVelocity;

        private Vector2 m_currentLookAngles; // x: pitch (상하), y: yaw (좌우)
        private float m_targetZoomRatio = 0.5f;
        private float m_currentZoomRatio = 0.5f;
        private float m_zoomVelocity;
        private CameraMode m_currentMode = CameraMode.ThirdPersonShoulder;
        private bool m_requireRightClickToRotate = false;

        private const float WHEEL_SCROLL_THRESHOLD = 0.01f;

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
            if (m_inputReader != null)
            {
                m_inputReader.onMouseWheelScrolled += HandleMouseWheelScrolled;
            }

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
            if (m_inputReader != null)
            {
                m_inputReader.onMouseWheelScrolled -= HandleMouseWheelScrolled;
            }
        }

        private void HandleMouseWheelScrolled(float scrollY)
        {
            m_accumulatedWheelDelta += scrollY;
        }

        public void LateTick()
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            // UI 메뉴 활성화 중에는 마우스 시점 회전 및 줌 조작 완전 차단
            var currentContext = m_inputContextManager?.CurrentContext;
            if (currentContext != null && currentContext.ContextType == InputContextType.UI)
            {
                m_accumulatedWheelDelta = 0f;
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

            float wheelDelta = m_accumulatedWheelDelta;
            m_accumulatedWheelDelta = 0f;

            if (Mathf.Abs(wheelDelta) <= WHEEL_SCROLL_THRESHOLD && Mouse.current != null)
            {
                wheelDelta = Mouse.current.scroll.ReadValue().y;
            }

            // 전술(시간 정지) 컨텍스트에서는 휠 입력이 퀵슬롯 순환 전용으로 사용되므로 카메라 줌에 반영하지 않음
            if (currentContext != null && currentContext.ContextType == InputContextType.Tactical)
            {
                wheelDelta = 0f;
            }

            UpdateCameraOffset(mouseDelta, wheelDelta);
        }

        public void UpdateCameraOffset(Vector2 mouseInput, float wheelDelta)
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            float followSmoothTime = m_cameraSetting.FollowSmoothTime;
            float zoomSpeed = m_cameraSetting.ZoomSpeed;
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

            // 3. 줌 연산
            if (Mathf.Abs(wheelDelta) > WHEEL_SCROLL_THRESHOLD)
            {
                float scrollDir = Mathf.Sign(wheelDelta);
                m_targetZoomRatio = Mathf.Clamp01(m_targetZoomRatio + scrollDir * zoomSpeed);
            }

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

