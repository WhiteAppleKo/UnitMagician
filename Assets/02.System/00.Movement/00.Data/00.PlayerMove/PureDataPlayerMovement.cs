using UnityEngine;

namespace PlayerMovement
{
    [CreateAssetMenu(fileName = "PureDataPlayerMovement", menuName = "PlayerMovement/Data/PlayerMovement")]
    public class PureDataPlayerMovement : ScriptableObject
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float rotateSpeed = 10.0f;

        [Header("1st Person Pitch Limits")]
        [Tooltip("1인칭 고개 숙이기 최대 각도 (아래쪽 한계)")]
        [SerializeField] private float minPitchAngle = -80f;

        [Tooltip("1인칭 고개 들기 최대 각도 (위쪽 한계)")]
        [SerializeField] private float maxPitchAngle = 80f;

        public float MoveSpeed => moveSpeed;
        public float RotateSpeed => rotateSpeed;
        public float MinPitchAngle => minPitchAngle;
        public float MaxPitchAngle => maxPitchAngle;
    }
}
