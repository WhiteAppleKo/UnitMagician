using UnityEngine;

namespace CameraMovement
{
    public interface ICameraModeCalculationStrategy
    {
        CameraMode Mode { get; }
        Vector3 CalculateTargetPivot(Vector3 basePos, Vector2 mouseInput, float zoomRatio, PureDataCameraSetting setting);
        Vector2 CalculateLookAngles(Vector2 currentAngles, Vector2 mouseInput, PureDataCameraSetting setting);
    }
}
