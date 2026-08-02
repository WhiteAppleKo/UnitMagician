using UnityEngine;

namespace PlayerMovement
{
    [CreateAssetMenu(fileName = "PureDataPlayerMovement", menuName = "PlayerMovement/Data/PlayerMovement")]
    public class PureDataPlayerMovement : ScriptableObject
    {
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float rotateSpeed = 10.0f;

        public float MoveSpeed => moveSpeed;
        public float RotateSpeed => rotateSpeed;
    }
}
