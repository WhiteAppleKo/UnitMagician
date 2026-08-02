using UnityEngine;

namespace PlayerMovement
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovementVisualizer : MonoBehaviour, IPlayerMovementVisualizer
    {
        private Rigidbody m_playerRigidbody;

        private void Awake()
        {
            m_playerRigidbody = GetComponent<Rigidbody>();
        }

        public void Move(Vector3 direction, float speed)
        {
            Vector3 targetVelocity = direction.normalized * speed;
            targetVelocity.y = m_playerRigidbody.linearVelocity.y;
            m_playerRigidbody.linearVelocity = targetVelocity;
        }

        public void Rotate(Vector3 direction, float rotateSpeed)
        {
            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime);
        }
    }
}
