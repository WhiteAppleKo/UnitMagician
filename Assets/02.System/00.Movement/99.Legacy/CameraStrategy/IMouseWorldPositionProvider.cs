using UnityEngine;

namespace CameraMovement
{
    public interface IMouseWorldPositionProvider
    {
        Vector3 GetMouseWorldPosition(Camera camera = null);
    }
}
