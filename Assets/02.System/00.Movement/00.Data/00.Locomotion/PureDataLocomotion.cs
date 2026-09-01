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
