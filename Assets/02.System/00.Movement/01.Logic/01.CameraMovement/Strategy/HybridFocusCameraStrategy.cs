using UnityEngine;
using VContainer;

namespace CameraMovement
{
    public class HybridFocusCameraStrategy : ICameraModeCalculationStrategy
    {
        private readonly IMouseWorldPositionProvider m_mousePositionProvider;

        public CameraMode Mode => CameraMode.HybridFocus;

        [Inject]
        public HybridFocusCameraStrategy(IMouseWorldPositionProvider mousePositionProvider = null)
        {
            m_mousePositionProvider = mousePositionProvider ?? new MouseWorldPositionProvider();
        }

        public Vector3 CalculateTargetPivot(Vector3 basePos, Vector2 mouseInput, float zoomRatio, PureDataCameraSetting setting)
        {
            if (setting == null) return basePos;

            Vector3 hybridWorldPos = m_mousePositionProvider.GetMouseWorldPosition();
            if (hybridWorldPos == Vector3.zero) hybridWorldPos = basePos;

            float effectiveMouseWeight = zoomRatio * setting.MouseWeight;
            return Vector3.Lerp(basePos, hybridWorldPos, effectiveMouseWeight);
        }

        public Vector2 CalculateLookAngles(Vector2 currentAngles, Vector2 mouseInput, PureDataCameraSetting setting)
        {
            return currentAngles;
        }
    }
}
