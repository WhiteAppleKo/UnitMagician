using UnityEngine;

namespace CameraMovement
{
    [CreateAssetMenu(fileName = "CameraSettingSO", menuName = "UnitMagician/Data/CameraSetting")]
    public class CameraSettingSO : ScriptableObject
    {
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

        public float MouseWeight => mouseWeight;
        public float FollowSmoothTime => followSmoothTime;

        public float MinZoomFOV => minZoomFOV;
        public float MaxZoomFOV => maxZoomFOV;
        public float MinCameraDistance => minCameraDistance;
        public float MaxCameraDistance => maxCameraDistance;
        public float ZoomSpeed => zoomSpeed;
        public float DefaultZoomRatio => defaultZoomRatio;
        public float MaxMouseFocusDistance => maxMouseFocusDistance;
    }
}
