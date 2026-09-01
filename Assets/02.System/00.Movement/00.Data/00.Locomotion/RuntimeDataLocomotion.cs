using System;
using System.Collections.Generic;
using UnityEngine;

namespace Movement.RefactoredLocomotion
{
    public enum LocomotionAnimationState
    {
        Base,
        Locomotion,
        Jump,
        Fall,
        Crouch
    }

    public enum LocomotionGaitState
    {
        Idle,
        Walk,
        Run,
        Sprint
    }

    public class RuntimeDataLocomotion
    {
        private readonly PureDataLocomotion _pureData;

        public PureDataLocomotion PureData => _pureData;

        // Current States
        public LocomotionAnimationState CurrentState { get; set; } = LocomotionAnimationState.Base;
        public LocomotionGaitState CurrentGait { get; set; } = LocomotionGaitState.Idle;

        // Dynamic State Properties
        public float ShuffleDirectionX { get; set; }
        public float ShuffleDirectionZ { get; set; }

        public float ForwardStrafe { get; set; } = 1f;
        public float InclineAngle { get; set; }
        public float FallingDuration { get; set; }
        public float CameraRotationOffset { get; set; }

        public float HeadLookX { get; set; }
        public float HeadLookY { get; set; }
        public float HeadLookDelay { get; set; }
        public bool EnableHeadTurn { get; set; } = true;

        public float BodyLookX { get; set; }
        public float BodyLookY { get; set; }
        public float BodyLookDelay { get; set; }
        public bool EnableBodyTurn { get; set; } = true;

        public float LeanValue { get; set; }
        public float LeanDelay { get; set; }
        public bool EnableLean { get; set; } = true;
        public bool AnimationClipEnd { get; set; }

        public bool IsGrounded { get; set; } = true;
        public bool IsAiming { get; set; }
        public bool IsCrouching { get; set; }
        public bool IsSprinting { get; set; }
        public bool IsWalking { get; set; }
        public bool IsStopped { get; set; } = true;
        public bool IsStarting { get; set; }
        public bool IsStrafing { get; set; }
        public bool IsTurningInPlace { get; set; }
        public bool IsSliding { get; set; }
        public bool IsLockedOn { get; set; }
        public bool CannotStandUp { get; set; }
        public bool CrouchKeyPressed { get; set; }

        public bool MovementInputTapped { get; set; }
        public bool MovementInputPressed { get; set; }
        public bool MovementInputHeld { get; set; }

        public float CurrentMaxSpeed { get; set; }
        public float TargetMaxSpeed { get; set; }
        public float Speed2D { get; set; }
        public float FallStartTime { get; set; }
        public float RotationRate { get; set; }
        public float InitialLeanValue { get; set; }
        public float InitialTurnValue { get; set; }
        public float LocomotionStartDirection { get; set; }
        public float LocomotionStartTimer { get; set; }
        public float LookingAngle { get; set; }
        public float StrafeAngle { get; set; }
        public float StrafeDirectionX { get; set; }
        public float StrafeDirectionZ { get; set; }
        public float NewDirectionDifferenceAngle { get; set; }

        public Vector3 MoveDirection { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 TargetVelocity { get; set; }
        public Vector3 CurrentRotation { get; set; }
        public Vector3 PreviousRotation { get; set; }
        public Vector3 CameraForward { get; set; }

        public GameObject CurrentLockOnTarget { get; set; }
        public List<GameObject> TargetCandidates { get; } = new List<GameObject>();

        // Events
        public event Action<bool> OnGroundedChanged;
        public event Action<bool> OnSprintingChanged;
        public event Action<bool> OnCrouchingChanged;
        public event Action<LocomotionAnimationState> OnStateChanged;

        public RuntimeDataLocomotion(PureDataLocomotion pureData)
        {
            _pureData = pureData;
            if (_pureData != null)
            {
                IsStrafing = _pureData.AlwaysStrafe;
            }
        }

        public void SetGrounded(bool isGrounded)
        {
            if (IsGrounded == isGrounded) return;
            IsGrounded = isGrounded;
            OnGroundedChanged?.Invoke(isGrounded);
        }

        public void SetSprinting(bool isSprinting)
        {
            if (IsSprinting == isSprinting) return;
            IsSprinting = isSprinting;
            OnSprintingChanged?.Invoke(isSprinting);
        }

        public void SetCrouching(bool isCrouching)
        {
            if (IsCrouching == isCrouching) return;
            IsCrouching = isCrouching;
            OnCrouchingChanged?.Invoke(isCrouching);
        }

        public void SetState(LocomotionAnimationState state)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            OnStateChanged?.Invoke(state);
        }
    }
}
