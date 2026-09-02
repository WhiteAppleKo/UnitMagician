using UnityEngine;

namespace CameraMovement
{
    public class PlayerOnlyCameraStrategy : ICameraModeCalculationStrategy
    {
        public CameraMode Mode => CameraMode.PlayerOnly;

        public Vector3 CalculateTargetPivot(Vector3 basePos, Vector2 mouseInput, float zoomRatio, PureDataCameraSetting setting)
        {
            return basePos;
        }

        public Vector2 CalculateLookAngles(Vector2 currentAngles, Vector2 mouseInput, PureDataCameraSetting setting)
        {
            return currentAngles;
        }
    }
}
