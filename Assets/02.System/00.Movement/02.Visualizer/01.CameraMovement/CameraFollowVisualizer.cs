using UnityEngine;
using VContainer;
using Unity.Cinemachine;

namespace CameraMovement
{
    public class CameraFollowVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform pivotTarget;
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private CinemachineCamera firstPersonVirtualCamera;
        [SerializeField] private Transform playerTransform;

        [SerializeField] private CameraSettingSO cameraSetting;

        private ICameraFollowService m_cameraFollowService;

        [Inject]
        public void Construct(ICameraFollowService cameraFollowService, CameraSettingSO cameraSetting = null)
        {
            m_cameraFollowService = cameraFollowService;
            if (cameraSetting != null)
            {
                this.cameraSetting = cameraSetting;
            }
        }

        private void Start()
        {
            if (pivotTarget == null)
            {
                GameObject pivotObj = new GameObject("CameraPivotTarget");
                pivotTarget = pivotObj.transform;
            }

            if (pivotTarget != null)
            {
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = pivotTarget;
                    virtualCamera.LookAt = pivotTarget;
                    LensSettings lens = virtualCamera.Lens;
                    lens.ModeOverride = LensSettings.OverrideModes.Perspective;
                    virtualCamera.Lens = lens;
                }
            }

            if (m_cameraFollowService != null)
            {
                if (playerTransform != null)
                {
                    m_cameraFollowService.SetTarget(playerTransform);
                }

                m_cameraFollowService.OnTargetPositionChanged += HandleTargetPositionChanged;
                m_cameraFollowService.OnZoomSizeChanged += HandleZoomSizeChanged;
                m_cameraFollowService.OnCameraModeChanged += HandleCameraModeChanged;

                HandleCameraModeChanged(m_cameraFollowService.CurrentMode);
            }
        }

        private void OnDestroy()
        {
            if (m_cameraFollowService != null)
            {
                m_cameraFollowService.OnTargetPositionChanged -= HandleTargetPositionChanged;
                m_cameraFollowService.OnZoomSizeChanged -= HandleZoomSizeChanged;
                m_cameraFollowService.OnCameraModeChanged -= HandleCameraModeChanged;
            }
        }

        private void HandleCameraModeChanged(CameraMode mode)
        {
            bool isFirstPerson = mode == CameraMode.FirstPerson;

            if (firstPersonVirtualCamera != null && virtualCamera != null)
            {
                firstPersonVirtualCamera.gameObject.SetActive(isFirstPerson);
                virtualCamera.gameObject.SetActive(!isFirstPerson);

                firstPersonVirtualCamera.Priority.Value = isFirstPerson ? 100 : 0;
                virtualCamera.Priority.Value = isFirstPerson ? 0 : 100;
            }

            if (m_cameraFollowService != null && !isFirstPerson)
            {
                HandleZoomSizeChanged(m_cameraFollowService.CurrentZoomSize);
            }
        }

        private void HandleTargetPositionChanged(Vector3 position)
        {
            if (pivotTarget != null)
            {
                pivotTarget.position = position;
            }
        }

        private void HandleZoomSizeChanged(float zoomRatio)
        {
            // 3인칭 쿼터뷰 모드 전용 동적 줌 (FOV & FollowOffset) 연산
            if (m_cameraFollowService != null && m_cameraFollowService.CurrentMode == CameraMode.FirstPerson)
            {
                return;
            }

            if (virtualCamera != null)
            {
                float minFOV = cameraSetting != null ? cameraSetting.MinZoomFOV : 30f;
                float maxFOV = cameraSetting != null ? cameraSetting.MaxZoomFOV : 60f;
                float minDistance = cameraSetting != null ? cameraSetting.MinCameraDistance : 8f;
                float maxDistance = cameraSetting != null ? cameraSetting.MaxCameraDistance : 18f;

                // zoomRatio: 0.0 (ZoomOut) -> maxFOV, maxDistance / 1.0 (ZoomIn) -> minFOV, minDistance
                float fov = Mathf.Lerp(maxFOV, minFOV, zoomRatio);
                float distance = Mathf.Lerp(maxDistance, minDistance, zoomRatio);

                LensSettings lens = virtualCamera.Lens;
                lens.ModeOverride = LensSettings.OverrideModes.Perspective;
                lens.FieldOfView = fov;
                virtualCamera.Lens = lens;

                if (virtualCamera.TryGetComponent<CinemachineFollow>(out var follow))
                {
                    Vector3 baseDirection = new Vector3(0f, 1f, -1f).normalized;
                    follow.FollowOffset = baseDirection * distance;
                }
            }
        }
    }
}
