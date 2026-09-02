using UnityEngine;
using VContainer;

namespace CameraMovement
{
    public class TopViewMouseFocusStrategy : ICameraModeCalculationStrategy
    {
        private readonly IMouseWorldPositionProvider m_mousePositionProvider;

        public CameraMode Mode => CameraMode.MouseFocus;

        [Inject]
        public TopViewMouseFocusStrategy(IMouseWorldPositionProvider mousePositionProvider = null)
        {
            m_mousePositionProvider = mousePositionProvider ?? new MouseWorldPositionProvider();
        }

        public Vector3 CalculateTargetPivot(Vector3 basePos, Vector2 mouseInput, float zoomRatio, PureDataCameraSetting setting)
        {
            if (setting == null) return basePos;

            Vector3 mouseFocusWorldPos = m_mousePositionProvider.GetMouseWorldPosition();
            if (mouseFocusWorldPos == Vector3.zero) mouseFocusWorldPos = basePos;

            Vector3 rawOffset = mouseFocusWorldPos - basePos;
            return basePos + Vector3.ClampMagnitude(rawOffset, setting.MaxMouseFocusDistance);
        }

        public Vector2 CalculateLookAngles(Vector2 currentAngles, Vector2 mouseInput, PureDataCameraSetting setting)
        {
            return currentAngles;
        }
    }

    // 하위 호환 별칭
    public class MouseFocusCameraStrategy : TopViewMouseFocusStrategy
    {
        public MouseFocusCameraStrategy(IMouseWorldPositionProvider mousePositionProvider = null) : base(mousePositionProvider)
        {
        }
    }
}
