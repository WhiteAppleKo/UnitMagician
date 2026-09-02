using UnityEngine;
using VContainer;
using Unity.Cinemachine;
using Movement.RefactoredLocomotion;

namespace CameraMovement
{
    public class CameraFollowVisualizer : MonoBehaviour
    {
        [Header("Pivot & Targets")]
        [SerializeField] private Transform pivotTarget;
        [SerializeField] private Transform playerTransform;

        [Header("Cinemachine Virtual Cameras")]
        [Tooltip("탑뷰 / 쿼터뷰 가상 카메라")]
        [SerializeField] private CinemachineCamera topViewVirtualCamera;

        [Tooltip("1인칭 가상 카메라")]
        [SerializeField] private CinemachineCamera firstPersonVirtualCamera;

        [Tooltip("3인칭 숄더뷰 가상 카메라")]
        [SerializeField] private CinemachineCamera shoulderVirtualCamera;

        private ICameraFollowService m_cameraFollowService;
        private ILocomotionVisualizer m_locomotionVisualizer;
        private PureDataCameraSetting m_cameraSetting;

        [Inject]
        public void Construct(
            ICameraFollowService cameraFollowService,
            IObjectResolver resolver)
        {
            m_cameraFollowService = cameraFollowService;

            if (resolver != null)
            {
                if (resolver.TryResolve<ILocomotionVisualizer>(out var locomotionVis))
                {
                    m_locomotionVisualizer = locomotionVis;
                }
            }

            m_cameraSetting = cameraFollowService?.Setting;
        }

        private const string PIVOT_OBJECT_NAME = "CameraPivotTarget";
        private const int PRIORITY_ACTIVE = 100;
        private const int PRIORITY_INACTIVE = 0;
        private static readonly Vector3 TOPVIEW_OFFSET_DIRECTION = new Vector3(0f, 1f, -1f).normalized;
        private Transform m_currentLockOnTarget;
        private bool m_isLockedOn;

        private void Awake()
        {
            // 시간 정지(Time.timeScale = 0) 중에도 Cinemachine이 독립 시간(unscaledDeltaTime)으로 동작하도록 강제
            CinemachineCore.UniformDeltaTimeGetter = () => Time.unscaledDeltaTime;
            ApplyIgnoreTimeScaleToBrain();
        }

        private void ApplyIgnoreTimeScaleToBrain()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.TryGetComponent<CinemachineBrain>(out var brain))
            {
                brain.IgnoreTimeScale = true;
            }
            else
            {
                var anyBrain = FindAnyObjectByType<CinemachineBrain>();
                if (anyBrain != null)
                {
                    anyBrain.IgnoreTimeScale = true;
                }
            }
        }

        private void LateUpdate()
        {
            EnsurePlayerTransform();

            if (playerTransform == null || pivotTarget == null) return;

            if (m_cameraFollowService != null && 
               (m_cameraFollowService.CurrentMode == CameraMode.HybridFocus || 
                m_cameraFollowService.CurrentMode == CameraMode.MouseFocus))
            {
                pivotTarget.position = m_cameraFollowService.CurrentTargetPosition;
            }
            else
            {
                pivotTarget.position = playerTransform.position + Vector3.up * 1.4f;
            }

            // 락온 시 피벗 회전축을 타겟 방향으로 정렬 (독립 시간 적용)
            if (m_isLockedOn && m_currentLockOnTarget != null)
            {
                Vector3 toTarget = (m_currentLockOnTarget.position + Vector3.up * 1.0f) - pivotTarget.position;
                if (toTarget != Vector3.zero)
                {
                    Quaternion lockRot = Quaternion.LookRotation(toTarget);
                    pivotTarget.rotation = Quaternion.Slerp(pivotTarget.rotation, lockRot, 15f * Time.unscaledDeltaTime);
                }
            }

            // 1인칭 가상 카메라가 활성화되어 있을 때 피벗의 회전각 일치
            if (firstPersonVirtualCamera != null && firstPersonVirtualCamera.Priority.Value == PRIORITY_ACTIVE)
            {
                firstPersonVirtualCamera.transform.position = pivotTarget.position;
                firstPersonVirtualCamera.transform.rotation = pivotTarget.rotation;
            }
        }

        private void EnsurePlayerTransform()
        {
            if (playerTransform != null) return;

            if (m_locomotionVisualizer != null)
            {
                playerTransform = m_locomotionVisualizer.Transform;
            }
            else
            {
                var locVis = FindAnyObjectByType<LocomotionVisualizer>();
                if (locVis != null) playerTransform = locVis.transform;
            }

            if (playerTransform != null && m_cameraFollowService != null)
            {
                m_cameraFollowService.SetTarget(playerTransform);
            }
        }

        private void SetupVirtualCamera(CinemachineCamera vcam, bool isFirstPerson = false)
        {
            if (vcam == null || pivotTarget == null) return;
            vcam.Follow = pivotTarget;
            vcam.LookAt = isFirstPerson ? null : pivotTarget;
        }

        private void Start()
        {
            ApplyIgnoreTimeScaleToBrain();
            EnsurePlayerTransform();

            if (pivotTarget == null)
            {
                GameObject pivotObj = new GameObject(PIVOT_OBJECT_NAME);
                pivotTarget = pivotObj.transform;
                if (playerTransform != null)
                {
                    pivotTarget.position = playerTransform.position + Vector3.up * 1.4f;
                    pivotTarget.rotation = playerTransform.rotation;
                }
            }

            SetupVirtualCamera(topViewVirtualCamera, false);
            SetupVirtualCamera(firstPersonVirtualCamera, true);
            SetupVirtualCamera(shoulderVirtualCamera, false);

            if (m_cameraFollowService != null)
            {
                if (playerTransform != null)
                {
                    m_cameraFollowService.SetTarget(playerTransform);
                }

                m_cameraFollowService.OnTargetPositionChanged += HandleTargetPositionChanged;
                m_cameraFollowService.OnLookAnglesChanged += HandleLookAnglesChanged;
                m_cameraFollowService.OnZoomSizeChanged += HandleZoomSizeChanged;
                m_cameraFollowService.OnCameraModeChanged += HandleCameraModeChanged;

                HandleCameraModeChanged(m_cameraFollowService.CurrentMode);
            }

            if (m_locomotionVisualizer != null)
            {
                m_locomotionVisualizer.OnCameraLockOnChanged += HandleLockOnChanged;
            }
        }

        private void OnDestroy()
        {
            if (m_cameraFollowService != null)
            {
                m_cameraFollowService.OnTargetPositionChanged -= HandleTargetPositionChanged;
                m_cameraFollowService.OnLookAnglesChanged -= HandleLookAnglesChanged;
                m_cameraFollowService.OnZoomSizeChanged -= HandleZoomSizeChanged;
                m_cameraFollowService.OnCameraModeChanged -= HandleCameraModeChanged;
            }

            if (m_locomotionVisualizer != null)
            {
                m_locomotionVisualizer.OnCameraLockOnChanged -= HandleLockOnChanged;
            }
        }

        private void HandleLockOnChanged(bool enable, Transform targetLockOn)
        {
            m_isLockedOn = enable;
            m_currentLockOnTarget = (enable && targetLockOn != null) ? targetLockOn : null;
        }

        private void HandleCameraModeChanged(CameraMode mode)
        {
            CinemachineCamera activeCamera = mode switch
            {
                CameraMode.FirstPerson => firstPersonVirtualCamera,
                CameraMode.ThirdPersonShoulder => shoulderVirtualCamera,
                _ => topViewVirtualCamera
            };

            SetCameraPriority(topViewVirtualCamera, topViewVirtualCamera == activeCamera);
            SetCameraPriority(firstPersonVirtualCamera, firstPersonVirtualCamera == activeCamera);
            SetCameraPriority(shoulderVirtualCamera, shoulderVirtualCamera == activeCamera);

            if (m_cameraFollowService != null && activeCamera == topViewVirtualCamera)
            {
                HandleZoomSizeChanged(m_cameraFollowService.CurrentZoomSize);
            }
        }

        private void SetCameraPriority(CinemachineCamera vcam, bool isActive)
        {
            if (vcam == null) return;
            vcam.gameObject.SetActive(true);
            vcam.Priority.Value = isActive ? PRIORITY_ACTIVE : PRIORITY_INACTIVE;
        }

        private void HandleTargetPositionChanged(Vector3 position)
        {
            if (pivotTarget != null)
            {
                pivotTarget.position = position;
            }
        }

        private void HandleLookAnglesChanged(Vector2 lookAngles)
        {
            if (pivotTarget == null || m_isLockedOn) return;

            CameraMode currentMode = m_cameraFollowService != null 
                ? m_cameraFollowService.CurrentMode 
                : CameraMode.ThirdPersonShoulder;

            switch (currentMode)
            {
                case CameraMode.FirstPerson:
                case CameraMode.ThirdPersonShoulder:
                    pivotTarget.rotation = Quaternion.Euler(lookAngles.x, lookAngles.y, 0f);
                    break;

                case CameraMode.HybridFocus:
                case CameraMode.PlayerOnly:
                case CameraMode.MouseFocus:
                default:
                    break;
            }
        }

        private void HandleZoomSizeChanged(float zoomRatio)
        {
            if (m_cameraSetting == null) return;

            // 탑뷰 줌 처리
            if (topViewVirtualCamera != null && topViewVirtualCamera.Priority.Value == PRIORITY_ACTIVE)
            {
                float minFOV = m_cameraSetting.MinZoomFOV;
                float maxFOV = m_cameraSetting.MaxZoomFOV;
                float minDistance = m_cameraSetting.MinCameraDistance;
                float maxDistance = m_cameraSetting.MaxCameraDistance;

                float fov = Mathf.Lerp(maxFOV, minFOV, zoomRatio);
                float distance = Mathf.Lerp(maxDistance, minDistance, zoomRatio);

                LensSettings lens = topViewVirtualCamera.Lens;
                lens.ModeOverride = LensSettings.OverrideModes.Perspective;
                lens.FieldOfView = fov;
                topViewVirtualCamera.Lens = lens;

                if (topViewVirtualCamera.TryGetComponent<CinemachineFollow>(out var follow))
                {
                    follow.FollowOffset = TOPVIEW_OFFSET_DIRECTION * distance;
                }
            }
        }
    }
}

