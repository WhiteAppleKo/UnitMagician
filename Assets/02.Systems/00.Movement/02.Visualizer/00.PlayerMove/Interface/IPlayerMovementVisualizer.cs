using UnityEngine;

namespace PlayerMovement
{
    public interface IPlayerMovementVisualizer
    {
        void Move(Vector3 direction, float speed);
        void Rotate(Vector3 direction, float rotateSpeed);
    }
}
