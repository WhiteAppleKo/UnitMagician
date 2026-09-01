using System;
using UnityEngine;

namespace CameraMovement
{
    public interface ICameraFollowService
    {
        Vector3 CurrentTargetPosition { get; }
        Vector2 CurrentLookAngles { get; }
        float CurrentZoomSize { get; }
        CameraMode CurrentMode { get; }
        PureDataCameraSetting Setting { get; }

        event Action<Vector3> OnTargetPositionChanged;
        event Action<Vector2> OnLookAnglesChanged;
        event Action<float> OnZoomSizeChanged;
        event Action<CameraMode> OnCameraModeChanged;

        void SetTarget(Transform target);
        void SetCameraMode(CameraMode mode);
        void UpdateCameraOffset(Vector2 mouseInput, float wheelDelta);
    }
}

