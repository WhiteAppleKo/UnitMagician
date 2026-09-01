using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;

namespace Movement.RefactoredLocomotion
{
    [RequireComponent(typeof(CharacterController))]
    public class LocomotionVisualizer : MonoBehaviour, ILocomotionVisualizer
    {
        [Header("Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [SerializeField] private Camera customCamera;

        [Header("Ground Ray Positions")]
        [SerializeField] private Transform rearRayPos;
        [SerializeField] private Transform frontRayPos;

        [Header("Lock On Transform")]
        [SerializeField] private Transform targetLockOnPos;

        #region Animation Variable Hashes

        private readonly int _movementInputTappedHash = Animator.StringToHash("MovementInputTapped");
        private readonly int _movementInputPressedHash = Animator.StringToHash("MovementInputPressed");
        private readonly int _movementInputHeldHash = Animator.StringToHash("MovementInputHeld");
        private readonly int _shuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
        private readonly int _shuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");

        private readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int _currentGaitHash = Animator.StringToHash("CurrentGait");

        private readonly int _isJumpingAnimHash = Animator.StringToHash("IsJumping");
        private readonly int _fallingDurationHash = Animator.StringToHash("FallingDuration");

        private readonly int _inclineAngleHash = Animator.StringToHash("InclineAngle");

        private readonly int _strafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private readonly int _strafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");

        private readonly int _forwardStrafeHash = Animator.StringToHash("ForwardStrafe");
        private readonly int _cameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
        private readonly int _isStrafingHash = Animator.StringToHash("IsStrafing");
        private readonly int _isTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");

        private readonly int _isCrouchingHash = Animator.StringToHash("IsCrouching");

        private readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int _isStoppedHash = Animator.StringToHash("IsStopped");
        private readonly int _isStartingHash = Animator.StringToHash("IsStarting");

        private readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");

        private readonly int _leanValueHash = Animator.StringToHash("LeanValue");
        private readonly int _headLookXHash = Animator.StringToHash("HeadLookX");
        private readonly int _headLookYHash = Animator.StringToHash("HeadLookY");

        private readonly int _bodyLookXHash = Animator.StringToHash("BodyLookX");
        private readonly int _bodyLookYHash = Animator.StringToHash("BodyLookY");

        private readonly int _locomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");

        #endregion

        public Transform Transform => transform;
        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;
        public Vector3 Right => transform.right;
        public bool IsControllerGrounded => characterController != null && characterController.isGrounded;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (targetLockOnPos == null)
            {
                targetLockOnPos = transform.Find("TargetLockOnPos");
            }
        }

        public void Move(Vector3 velocity)
        {
            if (characterController != null)
            {
                characterController.Move(velocity * Time.deltaTime);
            }
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetCapsuleSize(float height, float centerY)
        {
            if (characterController == null) return;

            characterController.height = height;
            characterController.center = new Vector3(0f, centerY, 0f);
        }

        public bool CheckGrounded(float offset, LayerMask layerMask)
        {
            if (characterController == null) return true;

            Vector3 spherePosition = new Vector3(
                characterController.transform.position.x,
                characterController.transform.position.y - offset,
                characterController.transform.position.z
            );

            return Physics.CheckSphere(spherePosition, characterController.radius, layerMask, QueryTriggerInteraction.Ignore);
        }

        public float CheckGroundIncline(LayerMask layerMask)
        {
            if (rearRayPos == null || frontRayPos == null) return 0f;

            float rayDistance = Mathf.Infinity;
            rearRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);
            frontRayPos.rotation = Quaternion.Euler(transform.rotation.x, 0, 0);

            Physics.Raycast(rearRayPos.position, rearRayPos.TransformDirection(-Vector3.up), out RaycastHit rearHit, rayDistance, layerMask);
            Physics.Raycast(frontRayPos.position, frontRayPos.TransformDirection(-Vector3.up), out RaycastHit frontHit, rayDistance, layerMask);

            Vector3 hitDifference = frontHit.point - rearHit.point;
            float xPlaneLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;

            return Mathf.Atan2(hitDifference.y, xPlaneLength) * Mathf.Rad2Deg;
        }

        public bool CheckCeilingHeight(float minimumStandingHeight, LayerMask layerMask)
        {
            if (frontRayPos == null) return false;

            float rayDistance = Mathf.Infinity;
            Vector3 midpoint = new Vector3(transform.position.x, transform.position.y + frontRayPos.localPosition.y, transform.position.z);

            if (Physics.Raycast(midpoint, transform.TransformDirection(Vector3.up), out RaycastHit ceilingHit, rayDistance, layerMask))
            {
                return ceilingHit.distance < minimumStandingHeight;
            }

            return false;
        }

        private Camera ActiveCamera => customCamera != null ? customCamera : Camera.main;

        public Vector3 GetCameraForwardZeroedY()
        {
            Camera cam = ActiveCamera;
            if (cam != null)
            {
                Vector3 f = cam.transform.forward;
                f.y = 0f;
                return f.normalized;
            }
            return Vector3.forward;
        }

        public Vector3 GetCameraRightZeroedY()
        {
            Camera cam = ActiveCamera;
            if (cam != null)
            {
                Vector3 r = cam.transform.right;
                r.y = 0f;
                return r.normalized;
            }
            return Vector3.right;
        }

        public Vector3 GetCameraPosition()
        {
            Camera cam = ActiveCamera;
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        public Vector3 GetCameraForward()
        {
            Camera cam = ActiveCamera;
            return cam != null ? cam.transform.forward : Vector3.forward;
        }

        public float GetCameraTiltX()
        {
            Camera cam = ActiveCamera;
            return cam != null ? cam.transform.eulerAngles.x : 0f;
        }

        public event System.Action<bool, Transform> OnCameraLockOnChanged;

        public void SetCameraLockOn(bool enable, Transform targetLockOn)
        {
            OnCameraLockOnChanged?.Invoke(enable, targetLockOn);
        }

        public void SetLockOnPos(Vector3 position)
        {
            if (targetLockOnPos != null)
            {
                targetLockOnPos.position = position;
            }
        }

        public event System.Action<GameObject> OnTargetCandidateAdded;
        public event System.Action<GameObject> OnTargetCandidateRemoved;

        public void HighlightTarget(GameObject target, bool enable, bool isLockedOn)
        {
            if (target != null && target.TryGetComponent<SampleObjectLockOn>(out var lockOnComponent))
            {
                lockOnComponent.Highlight(enable, isLockedOn);
            }
        }

        public void AddTargetCandidate(GameObject target)
        {
            OnTargetCandidateAdded?.Invoke(target);
        }

        public void RemoveTarget(GameObject target)
        {
            OnTargetCandidateRemoved?.Invoke(target);
        }

        public void UpdateAnimator(RuntimeDataLocomotion data)
        {
            if (animator == null || data == null) return;

            animator.SetFloat(_leanValueHash, data.LeanValue);
            animator.SetFloat(_headLookXHash, data.HeadLookX);
            animator.SetFloat(_headLookYHash, data.HeadLookY);
            animator.SetFloat(_bodyLookXHash, data.BodyLookX);
            animator.SetFloat(_bodyLookYHash, data.BodyLookY);

            animator.SetFloat(_isStrafingHash, data.IsStrafing ? 1.0f : 0.0f);
            animator.SetFloat(_inclineAngleHash, data.InclineAngle);

            animator.SetFloat(_moveSpeedHash, data.Speed2D);
            animator.SetInteger(_currentGaitHash, (int)data.CurrentGait);

            animator.SetFloat(_strafeDirectionXHash, data.StrafeDirectionX);
            animator.SetFloat(_strafeDirectionZHash, data.StrafeDirectionZ);
            animator.SetFloat(_forwardStrafeHash, data.ForwardStrafe);
            animator.SetFloat(_cameraRotationOffsetHash, data.CameraRotationOffset);

            animator.SetBool(_movementInputHeldHash, data.MovementInputHeld);
            animator.SetBool(_movementInputPressedHash, data.MovementInputPressed);
            animator.SetBool(_movementInputTappedHash, data.MovementInputTapped);
            animator.SetFloat(_shuffleDirectionXHash, data.ShuffleDirectionX);
            animator.SetFloat(_shuffleDirectionZHash, data.ShuffleDirectionZ);

            animator.SetBool(_isTurningInPlaceHash, data.IsTurningInPlace);
            animator.SetBool(_isCrouchingHash, data.IsCrouching);

            animator.SetFloat(_fallingDurationHash, data.FallingDuration);
            animator.SetBool(_isGroundedHash, data.IsGrounded);

            animator.SetBool(_isWalkingHash, data.IsWalking);
            animator.SetBool(_isStoppedHash, data.IsStopped);

            animator.SetFloat(_locomotionStartDirectionHash, data.LocomotionStartDirection);
        }

        public void SetJumpingAnim(bool isJumping)
        {
            if (animator != null)
            {
                animator.SetBool(_isJumpingAnimHash, isJumping);
            }
        }

        public void SetStartingAnim(bool isStarting)
        {
            if (animator != null)
            {
                animator.SetBool(_isStartingHash, isStarting);
            }
        }

        public void SetLocomotionStartDirectionAnim(float direction)
        {
            if (animator != null)
            {
                animator.SetFloat(_locomotionStartDirectionHash, direction);
            }
        }
    }
}
