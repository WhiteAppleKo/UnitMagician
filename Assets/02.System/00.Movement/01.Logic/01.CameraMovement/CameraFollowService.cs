using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace CameraMovement
{
    public class CameraFollowService : ICameraFollowService, ILateTickable
    {
        private readonly PureDataCameraSetting m_cameraSetting;
        private readonly MouseWorldPositionProvider m_mousePositionProvider;

        private Transform m_targetTransform;
        private Vector3 m_currentTargetPosition;
        private Vector3 m_smoothVelocity;

        private Vector2 m_currentLookAngles; // x: pitch (상하), y: yaw (좌우)
        private float m_targetZoomRatio = 0.5f;
        private float m_currentZoomRatio = 0.5f;
        private float m_zoomVelocity;
        private CameraMode m_currentMode = CameraMode.ThirdPersonOrbit;

        public Vector3 CurrentTargetPosition => m_currentTargetPosition;
        public Vector2 CurrentLookAngles => m_currentLookAngles;
        public float CurrentZoomSize => m_currentZoomRatio;
        public CameraMode CurrentMode => m_currentMode;
        public PureDataCameraSetting Setting => m_cameraSetting;

        public event Action<Vector3> OnTargetPositionChanged;
        public event Action<Vector2> OnLookAnglesChanged;
        public event Action<float> OnZoomSizeChanged;
        public event Action<CameraMode> OnCameraModeChanged;

        [Inject]
        public CameraFollowService(PureDataCameraSetting cameraSetting)
        {
            m_cameraSetting = cameraSetting;
            m_mousePositionProvider = new MouseWorldPositionProvider();

            if (cameraSetting != null)
            {
                m_currentMode = cameraSetting.DefaultMode;
                float defaultZoomRatio = cameraSetting.DefaultZoomRatio;
                m_targetZoomRatio = defaultZoomRatio;
                m_currentZoomRatio = defaultZoomRatio;
            }
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

        public void LateTick()
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            float wheelDelta = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            UpdateCameraOffset(mouseDelta, wheelDelta);
        }

        private const float WHEEL_SCROLL_THRESHOLD = 0.01f;

        public void UpdateCameraOffset(Vector2 mouseInput, float wheelDelta)
        {
            if (m_targetTransform == null || m_cameraSetting == null) return;

            float followSmoothTime = m_cameraSetting.FollowSmoothTime;
            float zoomSpeed = m_cameraSetting.ZoomSpeed;
            Vector3 basePos = m_targetTransform.position + Vector3.up * 1.4f;

            Vector3 targetPivotPos;

            switch (m_currentMode)
            {
                case CameraMode.FirstPerson:
                case CameraMode.ThirdPersonShoulder:
                case CameraMode.ThirdPersonOrbit:
                    float sensitivity = m_cameraSetting.MouseSensitivity;
                    float invertMultiplier = m_cameraSetting.InvertY ? 1f : -1f;

                    m_currentLookAngles.y += mouseInput.x * sensitivity * 0.1f;
                    m_currentLookAngles.x += mouseInput.y * sensitivity * 0.1f * invertMultiplier;

                    Vector2 limits = m_cameraSetting.VerticalAngleLimits;
                    m_currentLookAngles.x = Mathf.Clamp(m_currentLookAngles.x, limits.x, limits.y);

                    OnLookAnglesChanged?.Invoke(m_currentLookAngles);
                    targetPivotPos = basePos;
                    break;

                case CameraMode.MouseFocus:
                    Camera mouseCam = Camera.main;
                    Vector3 mouseFocusWorldPos = m_mousePositionProvider.GetMouseWorldPosition(mouseCam);
                    if (mouseFocusWorldPos == Vector3.zero) mouseFocusWorldPos = basePos;

                    Vector3 rawOffset = mouseFocusWorldPos - basePos;
                    targetPivotPos = basePos + Vector3.ClampMagnitude(rawOffset, m_cameraSetting.MaxMouseFocusDistance);
                    break;

                case CameraMode.HybridFocus:
                    Camera hybridCam = Camera.main;
                    Vector3 hybridWorldPos = m_mousePositionProvider.GetMouseWorldPosition(hybridCam);
                    if (hybridWorldPos == Vector3.zero) hybridWorldPos = basePos;

                    float effectiveMouseWeight = m_currentZoomRatio * m_cameraSetting.MouseWeight;
                    targetPivotPos = Vector3.Lerp(basePos, hybridWorldPos, effectiveMouseWeight);
                    break;

                case CameraMode.PlayerOnly:
                default:
                    targetPivotPos = basePos;
                    break;
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

