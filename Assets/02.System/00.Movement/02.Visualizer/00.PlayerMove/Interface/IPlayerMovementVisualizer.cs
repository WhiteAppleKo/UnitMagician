using UnityEngine;

namespace PlayerMovement
{
    public interface IPlayerMovementVisualizer
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        void Move(Vector3 direction, float speed);
        void Rotate(Vector3 direction, float rotateSpeed);
        void SetFirstPersonCameraPitch(float pitchAngle);
    }
}
