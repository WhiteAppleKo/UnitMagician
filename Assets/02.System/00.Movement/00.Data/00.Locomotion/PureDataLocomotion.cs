using UnityEngine;

namespace Movement.RefactoredLocomotion
{
    [CreateAssetMenu(fileName = "PureDataLocomotion", menuName = "Movement/PureDataLocomotion")]
    public class PureDataLocomotion : ScriptableObject
    {
        [Header("Locomotion Settings")]
        [SerializeField] private bool alwaysStrafe = true;
        [SerializeField] private float walkSpeed = 1.4f;
        [SerializeField] private float runSpeed = 2.5f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float speedChangeDamping = 10f;
        [SerializeField] private float rotationSmoothing = 10f;
        [SerializeField] private float cameraRotationOffset;

        [Header("Sprint Stamina (19-A)")]
        [Tooltip("달리는 동안(이동 중 스프린트) 초당 소모되는 스태미나.")]
        [SerializeField] private float sprintStaminaCostPerSecond = 15f;
        [Tooltip("스태미나가 0이 되어 달리기가 끊긴 뒤, 다시 달릴 수 있게 되는 회복 비율(최대 스태미나 대비). 0.3 = 30%.")]
        [Range(0f, 1f)]
        [SerializeField] private float sprintRestartThresholdRatio = 0.3f;

        [Header("Roll (19-B)")]
        [Tooltip("구르기 시작 시 한 번 소모되는 스태미나. 현재 스태미나가 이 값 이상이어야 시작된다.")]
        [SerializeField] private float rollStaminaCost = 25f;
        [Tooltip("구르기 동안 이동하는 수평 거리(m). rollDuration 동안 등속으로 이동한다.")]
        [SerializeField] private float rollDistance = 3f;
        [Tooltip("구르기 전체 시간(초).")]
        [SerializeField] private float rollDuration = 0.6f;
        [Tooltip("구르기 시작 후 무적(IgnoreAttack 레이어)이 시작되는 시각(초).")]
        [SerializeField] private float rollInvincibleStart = 0.1f;
        [Tooltip("구르기 시작 후 무적(IgnoreAttack 레이어)이 끝나는 시각(초).")]
        [SerializeField] private float rollInvincibleEnd = 0.4f;

        [Header("Shuffle Settings")]
        [SerializeField] private float buttonHoldThreshold = 0.15f;

        [Header("Capsule Settings")]
        [SerializeField] private float capsuleStandingHeight = 1.8f;
        [SerializeField] private float capsuleStandingCentre = 0.93f;
        [SerializeField] private float capsuleCrouchingHeight = 1.2f;
        [SerializeField] private float capsuleCrouchingCentre = 0.6f;

        [Header("Strafing Settings")]
        [SerializeField] private float forwardStrafeMinThreshold = -55.0f;
        [SerializeField] private float forwardStrafeMaxThreshold = 125.0f;

        [Header("Grounded & Air Settings")]
        [SerializeField] private LayerMask groundLayerMask = 1;
        [SerializeField] private float groundedOffset = 0.14f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float gravityMultiplier = 2f;

        [Header("Head Look Settings")]
        [SerializeField] private bool enableHeadTurn = true;
        [SerializeField] private float headLookDelay;
        [SerializeField] private AnimationCurve headLookXCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Body Look Settings")]
        [SerializeField] private bool enableBodyTurn = true;
        [SerializeField] private float bodyLookDelay;
        [SerializeField] private AnimationCurve bodyLookXCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Lean Settings")]
        [SerializeField] private bool enableLean = true;
        [SerializeField] private float leanDelay;
        [SerializeField] private AnimationCurve leanCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private float leansHeadLooksDelay;

        // Public Readonly Properties (Immutable)
        public bool AlwaysStrafe => alwaysStrafe;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float SprintSpeed => sprintSpeed;
        public float SpeedChangeDamping => speedChangeDamping;
        public float RotationSmoothing => rotationSmoothing;
        public float CameraRotationOffset => cameraRotationOffset;

        public float SprintStaminaCostPerSecond => sprintStaminaCostPerSecond;
        public float SprintRestartThresholdRatio => sprintRestartThresholdRatio;

        public float RollStaminaCost => rollStaminaCost;
        public float RollDistance => rollDistance;
        public float RollDuration => rollDuration;
        public float RollInvincibleStart => rollInvincibleStart;
        public float RollInvincibleEnd => rollInvincibleEnd;

        public float ButtonHoldThreshold => buttonHoldThreshold;

        public float CapsuleStandingHeight => capsuleStandingHeight;
        public float CapsuleStandingCentre => capsuleStandingCentre;
        public float CapsuleCrouchingHeight => capsuleCrouchingHeight;
        public float CapsuleCrouchingCentre => capsuleCrouchingCentre;

        public float ForwardStrafeMinThreshold => forwardStrafeMinThreshold;
        public float ForwardStrafeMaxThreshold => forwardStrafeMaxThreshold;

        public LayerMask GroundLayerMask => groundLayerMask;
        public float GroundedOffset => groundedOffset;
        public float JumpForce => jumpForce;
        public float GravityMultiplier => gravityMultiplier;

        public bool EnableHeadTurn => enableHeadTurn;
        public float HeadLookDelay => headLookDelay;
        public AnimationCurve HeadLookXCurve => headLookXCurve;

        public bool EnableBodyTurn => enableBodyTurn;
        public float BodyLookDelay => bodyLookDelay;
        public AnimationCurve BodyLookXCurve => bodyLookXCurve;

        public bool EnableLean => enableLean;
        public float LeanDelay => leanDelay;
        public AnimationCurve LeanCurve => leanCurve;
        public float LeansHeadLooksDelay => leansHeadLooksDelay;
    }
}
