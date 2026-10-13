using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CameraMovement
{
    [CreateAssetMenu(fileName = "PureDataCameraSetting", menuName = "Camera/Data/CameraSetting")]
    public class PureDataCameraSetting : ScriptableObject
    {
        [Header("Mode Whitelist & Default")]
        [Tooltip("게임 시작 시 기본 적용할 카메라 모드")]
        [SerializeField] private CameraMode defaultMode = CameraMode.ThirdPersonShoulder;

        [Tooltip("해당 프로젝트/씬에서 허용할 카메라 모드 목록 (숄더뷰 고정 — 3인칭 숄더만 유지)")]
        [SerializeField] private CameraMode[] allowedModes = new CameraMode[]
        {
            CameraMode.ThirdPersonShoulder
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

        [Header("Event Camera (First Person)")]
        [Tooltip("평상시 숄더 가상 카메라 <-> 이벤트 1인칭 가상 카메라 전환(Cinemachine 블렌드) 시간(초). 0 이하면 즉시 컷")]
        [FormerlySerializedAs("eventTargetBlendTime")]
        [SerializeField] private float eventCameraBlendTime = 0.5f;

        [Tooltip("이벤트 1인칭 가상 카메라의 세로 시야각(FOV)")]
        [SerializeField] private float eventCameraFieldOfView = 70f;

        [Tooltip("이벤트 1인칭 카메라(캐릭터 눈) 높이. 플레이어 루트 위치 기준 위쪽 오프셋(m)")]
        [SerializeField] private float eventCameraEyeHeight = 1.68f;

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
        public float EventCameraBlendTime => eventCameraBlendTime;
        public float EventCameraFieldOfView => eventCameraFieldOfView;
        public float EventCameraEyeHeight => eventCameraEyeHeight;

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

