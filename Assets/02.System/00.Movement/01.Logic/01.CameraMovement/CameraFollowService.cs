using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace CameraMovement
{
    public class CameraFollowService : ICameraFollowService, ILateTickable
    {
        private readonly PureDataCameraSetting m_cameraSetting;
        private readonly IMouseWorldPositionProvider m_mousePositionProvider;
        private readonly Dictionary<CameraMode, ICameraModeCalculationStrategy> m_strategies;

        private Transform m_targetTransform;
        private Vector3 m_currentTargetPosition;
        private Vector3 m_smoothVelocity;

        private Vector2 m_currentLookAngles; // x: pitch (상하), y: yaw (좌우)
        private float m_targetZoomRatio = 0.5f;
        private float m_currentZoomRatio = 0.5f;
        private float m_zoomVelocity;
        private CameraMode m_currentMode = CameraMode.ThirdPersonShoulder;

        private const float WHEEL_SCROLL_THRESHOLD = 0.01f;

        public Vector3 CurrentTargetPosition => m_currentTargetPosition;
        public Vector2 CurrentLookAngles => m_currentLookAngles;
        public float CurrentZoomSize => m_currentZoomRatio;
        public CameraMode CurrentMode => m_currentMode;
        public PureDataCameraSetting Setting => m_cameraSetting;

        // 호환성 편의 프로퍼티
        public Vector3 TargetPosition => m_currentTargetPosition;
        public Vector2 LookAngles => m_currentLookAngles;

        public event Action<Vector3> OnTargetPositionChanged;
        public event Action<Vector2> OnLookAnglesChanged;
        public event Action<float> OnZoomSizeChanged;
        public event Action<CameraMode> OnCameraModeChanged;

        [Inject]
        public CameraFollowService(
            PureDataCameraSetting cameraSetting,
            IMouseWorldPositionProvider mousePositionProvider = null,
            IReadOnlyList<ICameraModeCalculationStrategy> strategies = null)
        {
            m_cameraSetting = cameraSetting;
            m_mousePositionProvider = mousePositionProvider ?? new MouseWorldPositionProvider();

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

        public void LateTick()
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            float wheelDelta = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
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

