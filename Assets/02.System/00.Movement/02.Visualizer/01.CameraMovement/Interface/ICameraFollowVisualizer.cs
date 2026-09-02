using UnityEngine;

namespace CameraMovement
{
    public interface ICameraFollowVisualizer
    {
        Transform PivotTarget { get; }
        Transform PlayerTransform { get; }
        ICameraFollowService CameraFollowService { get; }
    }
}
