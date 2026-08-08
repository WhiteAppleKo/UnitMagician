using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace CameraMovement
{
    public class CameraFollowService : ICameraFollowService, ILateTickable
    {
        private readonly CameraSettingSO m_cameraSetting;
        private readonly MouseWorldPositionProvider m_mousePositionProvider;

        private Transform m_targetTransform;
        private Vector3 m_currentTargetPosition;
        private Vector3 m_smoothVelocity;

        private float m_targetZoomRatio = 0.5f;
        private float m_currentZoomRatio = 0.5f;
        private float m_zoomVelocity;
        private CameraMode m_currentMode = CameraMode.HybridFocus;

        public Vector3 CurrentTargetPosition => m_currentTargetPosition;
        public float CurrentZoomSize => m_currentZoomRatio;
        public CameraMode CurrentMode => m_currentMode;

        public event Action<Vector3> OnTargetPositionChanged;
        public event Action<float> OnZoomSizeChanged;
        public event Action<CameraMode> OnCameraModeChanged;

        [Inject]
        public CameraFollowService(CameraSettingSO cameraSetting)
        {
            m_cameraSetting = cameraSetting;
            m_mousePositionProvider = new MouseWorldPositionProvider();
            
            float defaultZoomRatio = cameraSetting != null ? cameraSetting.DefaultZoomRatio : 0.5f;
            m_targetZoomRatio = defaultZoomRatio;
            m_currentZoomRatio = defaultZoomRatio;
        }

        public void SetTarget(Transform target)
        {
            m_targetTransform = target;
            if (target != null)
            {
                m_currentTargetPosition = target.position;
                OnTargetPositionChanged?.Invoke(m_currentTargetPosition);
                OnZoomSizeChanged?.Invoke(m_currentZoomRatio);
                OnCameraModeChanged?.Invoke(m_currentMode);
            }
        }

        public void SetCameraMode(CameraMode mode)
        {
            m_currentMode = mode;
            OnCameraModeChanged?.Invoke(m_currentMode);
        }

        public void LateTick()
        {
            if (m_targetTransform == null) return;

            float wheelDelta = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            UpdateCameraOffset(Vector2.zero, wheelDelta);
        }

        public void UpdateCameraOffset(Vector2 mouseInput, float wheelDelta)
        {
            if (m_targetTransform == null) return;

            float mouseWeight = m_cameraSetting != null ? m_cameraSetting.MouseWeight : 0.25f;
            float followSmoothTime = m_cameraSetting != null ? m_cameraSetting.FollowSmoothTime : 0.15f;
            float zoomSpeed = m_cameraSetting != null ? m_cameraSetting.ZoomSpeed : 0.1f;

            Camera mainCam = Camera.main;
            Vector3 playerPos = m_targetTransform.position;
            Vector3 mouseWorldPos = m_mousePositionProvider.GetMouseWorldPosition(mainCam);

            if (mouseWorldPos == Vector3.zero)
            {
                mouseWorldPos = playerPos;
            }

            Vector3 targetPivotPos;

            // Section 5-4: 3가지 카메라 모드 시스템 분기 연산
            if (m_currentMode == CameraMode.PlayerOnly)
            {
                targetPivotPos = playerPos;
            }
            else if (m_currentMode == CameraMode.MouseFocus)
            {
                // 선제적 Pre-Clamp: SmoothDamp 전 목표 지점 한계 강제 설정으로 튕김 현상 원천 차단
                float maxDistance = m_cameraSetting != null ? m_cameraSetting.MaxMouseFocusDistance : 5.5f;
                Vector3 rawOffset = mouseWorldPos - playerPos;
                targetPivotPos = playerPos + Vector3.ClampMagnitude(rawOffset, maxDistance);
            }
            else // HybridFocus (디폴트)
            {
                float effectiveMouseWeight = m_currentZoomRatio * mouseWeight;
                targetPivotPos = Vector3.Lerp(playerPos, mouseWorldPos, effectiveMouseWeight);
            }

            // 2. SmoothDamp 위치 보간
            m_currentTargetPosition = Vector3.SmoothDamp(
                m_currentTargetPosition,
                targetPivotPos,
                ref m_smoothVelocity,
                followSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );

            OnTargetPositionChanged?.Invoke(m_currentTargetPosition);

            // 3. 마우스 휠 줌 연산 및 SmoothDamp 줌 보간 (zoomRatio: 0.0 = ZoomOut ~ 1.0 = ZoomIn)
            if (Mathf.Abs(wheelDelta) > 0.01f)
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
