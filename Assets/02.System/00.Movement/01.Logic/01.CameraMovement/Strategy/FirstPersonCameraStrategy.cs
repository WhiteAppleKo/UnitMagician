using UnityEngine;

namespace CameraMovement
{
    public class FirstPersonCameraStrategy : ICameraModeCalculationStrategy
    {
        public CameraMode Mode => CameraMode.FirstPerson;

        public Vector3 CalculateTargetPivot(Vector3 basePos, Vector2 mouseInput, float zoomRatio, PureDataCameraSetting setting)
        {
            return basePos;
        }

        public Vector2 CalculateLookAngles(Vector2 currentAngles, Vector2 mouseInput, PureDataCameraSetting setting)
        {
            if (setting == null) return currentAngles;

            float sensitivity = setting.MouseSensitivity;
            float invertMultiplier = setting.InvertY ? 1f : -1f;

            currentAngles.y += mouseInput.x * sensitivity * 0.1f;
            currentAngles.x += mouseInput.y * sensitivity * 0.1f * invertMultiplier;

            Vector2 limits = setting.VerticalAngleLimits;
            currentAngles.x = Mathf.Clamp(currentAngles.x, limits.x, limits.y);

            return currentAngles;
        }
    }
}
