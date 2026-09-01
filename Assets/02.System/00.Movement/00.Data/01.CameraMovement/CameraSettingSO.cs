using System.Collections.Generic;
using UnityEngine;

namespace CameraMovement
{
    [CreateAssetMenu(fileName = "PureDataCameraSetting", menuName = "Camera/Data/CameraSetting")]
    public class PureDataCameraSetting : ScriptableObject
    {
        [Header("Mode Whitelist & Default")]
        [Tooltip("게임 시작 시 기본 적용할 카메라 모드")]
        [SerializeField] private CameraMode defaultMode = CameraMode.ThirdPersonOrbit;

        [Tooltip("해당 프로젝트/씬에서 허용할 카메라 모드 목록 (UI 노출 필터링)")]
        [SerializeField] private CameraMode[] allowedModes = new CameraMode[]
        {
            CameraMode.HybridFocus,
            CameraMode.PlayerOnly,
            CameraMode.MouseFocus,
            CameraMode.FirstPerson,
            CameraMode.ThirdPersonShoulder,
            CameraMode.ThirdPersonOrbit
        };

        [Header("Mouse Target Offset")]
        [Tooltip("마우스 방향 시야 조준 비율")]
        [SerializeField] private float mouseWeight = 0.25f;

        [Tooltip("카메라 따라가는 속도")]
        [SerializeField] private float followSmoothTime = 0.15f;

        [Header("3D Perspective Zoom Settings")]
        [Tooltip("가장 가까울 때 카메라 화면 각도")]
        [SerializeField] private float minZoomFOV = 30f;

        [Tooltip("가장 멀 때 카메라 화면 각도")]
        [SerializeField] private float maxZoomFOV = 60f;

        [Tooltip("가장 가까울 때 카메라 거리")]
        [SerializeField] private float minCameraDistance = 8f;

        [Tooltip("가장 멀 때 카메라 거리")]
        [SerializeField] private float maxCameraDistance = 18f;

        [Tooltip("마우스 휠 줌 조절 속도")]
        [SerializeField] private float zoomSpeed = 0.1f;

        [Tooltip("시작할 때 기본 줌 비율 (0 = 가장 멀 때, 1 = 가장 가까울 때)")]
        [SerializeField] private float defaultZoomRatio = 0.5f;

        [Tooltip("마우스 모드일 때 멀어질 수 있는 최대 거리")]
        [SerializeField] private float maxMouseFocusDistance = 5.5f;

        [Header("Third Person & Orbit Settings")]
        [Tooltip("3인칭 시선 마우스 감도")]
        [SerializeField] private float mouseSensitivity = 2f;

        [Tooltip("3인칭 숄더뷰 오프셋")]
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.5f, 0.2f, -2.5f);

        [Tooltip("3인칭 궤도 기본 거리")]
        [SerializeField] private float orbitDistance = 5f;

        [Tooltip("상하 시선 각도 제한 (최소, 최대)")]
        [SerializeField] private Vector2 verticalAngleLimits = new Vector2(-30f, 60f);

        [Tooltip("Y축 마우스 반전")]
        [SerializeField] private bool invertY = false;

        public CameraMode DefaultMode => defaultMode;
        public IReadOnlyList<CameraMode> AllowedModes => allowedModes;

        public float MouseWeight => mouseWeight;
        public float FollowSmoothTime => followSmoothTime;

        public float MinZoomFOV => minZoomFOV;
        public float MaxZoomFOV => maxZoomFOV;
        public float MinCameraDistance => minCameraDistance;
        public float MaxCameraDistance => maxCameraDistance;
        public float ZoomSpeed => zoomSpeed;
        public float DefaultZoomRatio => defaultZoomRatio;
        public float MaxMouseFocusDistance => maxMouseFocusDistance;

        public float MouseSensitivity => mouseSensitivity;
        public Vector3 ShoulderOffset => shoulderOffset;
        public float OrbitDistance => orbitDistance;
        public Vector2 VerticalAngleLimits => verticalAngleLimits;
        public bool InvertY => invertY;

        public bool IsModeAllowed(CameraMode mode)
        {
            if (allowedModes == null || allowedModes.Length == 0) return true;
            for (int i = 0; i < allowedModes.Length; i++)
            {
                if (allowedModes[i] == mode) return true;
            }
            return false;
        }
    }

    // 하위 호환 별칭
    public class CameraSettingSO : PureDataCameraSetting
    {
    }
}

