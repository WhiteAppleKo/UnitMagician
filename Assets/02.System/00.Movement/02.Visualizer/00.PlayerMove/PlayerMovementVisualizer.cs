using UnityEngine;
using Unity.Cinemachine;

namespace PlayerMovement
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovementVisualizer : MonoBehaviour, IPlayerMovementVisualizer
    {
        [Tooltip("1인칭 시점 상하 회전(Pitch)을 적용할 카메라 피벗 오브젝트 (자식 가상카메라)")]
        [SerializeField] private Transform firstPersonCameraPivot;

        private Rigidbody m_playerRigidbody;

        public Transform Transform => transform;
        public Vector3 Position => transform.position;

        private void Awake()
        {
            m_playerRigidbody = GetComponent<Rigidbody>();

            if (firstPersonCameraPivot == null)
            {
                var cam = GetComponentInChildren<CinemachineCamera>();
                if (cam != null)
                {
                    firstPersonCameraPivot = cam.transform;
                }
                else if (transform.childCount > 0)
                {
                    firstPersonCameraPivot = transform.GetChild(0);
                }
            }
        }

        public void SetFirstPersonCameraPitch(float pitchAngle)
        {
            if (firstPersonCameraPivot != null)
            {
                firstPersonCameraPivot.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            }
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
