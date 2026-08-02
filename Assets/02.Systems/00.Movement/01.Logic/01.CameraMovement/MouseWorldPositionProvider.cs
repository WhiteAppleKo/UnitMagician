using UnityEngine;
using UnityEngine.InputSystem;

namespace CameraMovement
{
    public class MouseWorldPositionProvider
    {
        private readonly Plane m_groundPlane = new Plane(Vector3.up, Vector3.zero);

        public Vector3 GetMouseWorldPosition(Camera camera)
        {
            if (camera == null) return Vector3.zero;

            Vector2 mouseScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            mouseScreenPos.x = Mathf.Clamp(mouseScreenPos.x, 0f, Screen.width);
            mouseScreenPos.y = Mathf.Clamp(mouseScreenPos.y, 0f, Screen.height);
            Ray ray = camera.ScreenPointToRay(mouseScreenPos);

            if (m_groundPlane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return Vector3.zero;
        }
    }
}
