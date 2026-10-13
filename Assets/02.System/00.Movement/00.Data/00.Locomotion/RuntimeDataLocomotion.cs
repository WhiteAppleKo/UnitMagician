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
        Crouch,
        Roll
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

        /// <summary>
        /// 스태미나가 0이 되어 달리기가 끊긴 뒤, 최대치의 sprintRestartThresholdRatio 이상으로 회복되기 전까지 true(19-A).
        /// true인 동안에는 달리기를 시작할 수 없다. 변경은 SetSprintLocked로만 한다.
        /// </summary>
        public bool IsSprintLocked { get; private set; }

        /// <summary>구르기 진행 중(19-B). 변경은 BeginRoll/EndRoll로만 한다.</summary>
        public bool IsRolling { get; private set; }

        /// <summary>구르기의 무적 구간(rollInvincibleStart~End) 안에 있는지(19-B). 이 구간에 플레이어는 IgnoreAttack 레이어에 있다. 변경은 SetRollInvincible로만 한다.</summary>
        public bool IsRollInvincible { get; private set; }

        /// <summary>구르기 방향(수평 월드 단위벡터, 19-B). 구르기 시작 시 확정된다.</summary>
        public Vector3 RollDirection { get; private set; }

        /// <summary>구르기 시작 후 경과 시간(초, 19-B). 게임 시간 기준이며 시간정지 중에는 늘지 않는다.</summary>
        public float RollElapsed { get; private set; }
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
        public event Action<bool> OnSprintLockedChanged;
        public event Action<bool> OnRollingChanged;
        public event Action<bool> OnRollInvincibleChanged;
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

        public void SetSprintLocked(bool isLocked)
        {
            if (IsSprintLocked == isLocked) return;
            IsSprintLocked = isLocked;
            OnSprintLockedChanged?.Invoke(isLocked);
        }

        /// <summary>구르기를 시작한다(19-B). 방향은 수평 월드 단위벡터, 경과 시간은 0으로 초기화된다.</summary>
        public void BeginRoll(Vector3 direction)
        {
            RollDirection = direction;
            RollElapsed = 0f;
            if (IsRolling) return;
            IsRolling = true;
            OnRollingChanged?.Invoke(true);
        }

        /// <summary>구르기 경과 시간을 갱신한다(19-B).</summary>
        public void SetRollElapsed(float elapsed)
        {
            RollElapsed = elapsed;
        }

        /// <summary>구르기를 끝낸다(정상 종료/중단 공통, 19-B). 무적 구간 표시도 함께 내린다.</summary>
        public void EndRoll()
        {
            SetRollInvincible(false);
            RollElapsed = 0f;
            if (!IsRolling) return;
            IsRolling = false;
            OnRollingChanged?.Invoke(false);
        }

        public void SetRollInvincible(bool isInvincible)
        {
            if (IsRollInvincible == isInvincible) return;
            IsRollInvincible = isInvincible;
            OnRollInvincibleChanged?.Invoke(isInvincible);
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
