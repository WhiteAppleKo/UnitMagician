using System;
using UnityEngine;

namespace CameraMovement
{
    public interface ICameraFollowService
    {
        Vector3 CurrentTargetPosition { get; }
        float CurrentZoomSize { get; }
        CameraMode CurrentMode { get; }

        event Action<Vector3> OnTargetPositionChanged;
        event Action<float> OnZoomSizeChanged;
        event Action<CameraMode> OnCameraModeChanged;

        void SetTarget(Transform target);
        void SetCameraMode(CameraMode mode);
        void UpdateCameraOffset(Vector2 mouseInput, float wheelDelta);
    }
}
