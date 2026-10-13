using System;
using System.Collections.Generic;
using CharacterSystem;
using Movement.Visualizer;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Movement.RefactoredLocomotion
{
    public class LocomotionLogicSystem : ITickable, IDisposable
    {
        private const float ANIMATION_DAMP_TIME = 5f;
        private const float STRAFE_DIRECTION_DAMP_TIME = 20f;

        private readonly PureDataLocomotion _pureData;
        private readonly RuntimeDataLocomotion _runtimeData;
        private readonly ILocomotionVisualizer _visualizer;
        private readonly InputReader _inputReader;
        private readonly ILockOnController _lockOnController;
        private readonly ICharacterStatService _statService;
        private readonly Common.InputSystem.IInputContextManager _inputContextManager;

        // 구르기 입력은 콜백에서 요청만 올리고 Tick에서 처리한다(19-B). 같은 입력 업데이트 안의 Move/Jump 콜백 순서와 무관하게
        // Tick 시점에는 WASD 입력이 최종 상태라 "Space를 누른 시점의 WASD 방향"이 순서에 흔들리지 않는다.
        private bool _rollRequested;

        [Inject]
        public LocomotionLogicSystem(
            PureDataLocomotion pureData,
            RuntimeDataLocomotion runtimeData,
            ILocomotionVisualizer visualizer,
            InputReader inputReader,
            ICharacterStatService statService,
            Common.InputSystem.IInputContextManager inputContextManager,
            ILockOnController lockOnController = null)
        {
            _pureData = pureData;
            _runtimeData = runtimeData;
            _visualizer = visualizer;
            _inputReader = inputReader;
            _statService = statService;
            _inputContextManager = inputContextManager;
            _lockOnController = lockOnController;

            RegisterInputEvents();
            RegisterRollInterruptEvents();
            SwitchState(LocomotionAnimationState.Locomotion);
        }

        #region Input Registration

        private void RegisterInputEvents()
        {
            if (_inputReader == null) return;

            _inputReader.onLockOnToggled += ToggleLockOn;
            _inputReader.onWalkToggled += ToggleWalk;
            _inputReader.onSprintActivated += ActivateSprint;
            _inputReader.onSprintDeactivated += DeactivateSprint;
            _inputReader.onCrouchActivated += ActivateCrouch;
            _inputReader.onCrouchDeactivated += DeactivateCrouch;
            _inputReader.onAimActivated += ActivateAim;
            _inputReader.onAimDeactivated += DeactivateAim;
            _inputReader.onRollPressed += RequestRoll;
        }

        /// <summary>구르기가 중간에 끊기는 경로(사망, 비주얼라이저 비활성/파괴)를 구독한다(19-B). 피격 경직은 CancelRoll()을 호출하면 된다.</summary>
        private void RegisterRollInterruptEvents()
        {
            _visualizer.OnVisualizerDisabled += CancelRoll;

            var statData = _statService?.RuntimeData;
            if (statData != null)
            {
                statData.OnDeath += CancelRoll;
            }
        }

        public void Dispose()
        {
            _visualizer.OnVisualizerDisabled -= CancelRoll;

            var statData = _statService?.RuntimeData;
            if (statData != null)
            {
                statData.OnDeath -= CancelRoll;
            }

            _rollRequested = false;

            if (_inputReader == null) return;

            _inputReader.onLockOnToggled -= ToggleLockOn;
            _inputReader.onWalkToggled -= ToggleWalk;
            _inputReader.onSprintActivated -= ActivateSprint;
            _inputReader.onSprintDeactivated -= DeactivateSprint;
            _inputReader.onCrouchActivated -= ActivateCrouch;
            _inputReader.onCrouchDeactivated -= DeactivateCrouch;
            _inputReader.onAimActivated -= ActivateAim;
            _inputReader.onAimDeactivated -= DeactivateAim;
            _inputReader.onRollPressed -= RequestRoll;
            _inputReader.onJumpPerformed -= LocomotionToJumpState;
            _inputReader.onJumpPerformed -= CrouchToJumpState;
        }

        #endregion

        #region State Machine

        public void SwitchState(LocomotionAnimationState newState)
        {
            ExitCurrentState();
            EnterState(newState);
        }

        private void EnterState(LocomotionAnimationState stateToEnter)
        {
            _runtimeData.SetState(stateToEnter);

            switch (stateToEnter)
            {
                case LocomotionAnimationState.Base:
                    _runtimeData.PreviousRotation = _visualizer.Forward;
                    break;
                case LocomotionAnimationState.Locomotion:
                    if (_inputReader != null) _inputReader.onJumpPerformed += LocomotionToJumpState;
                    break;
                case LocomotionAnimationState.Jump:
                    _visualizer.SetJumpingAnim(true);
                    _runtimeData.IsSliding = false;
                    float jumpForce = _pureData != null ? _pureData.JumpForce : 10f;
                    _runtimeData.Velocity = new Vector3(_runtimeData.Velocity.x, jumpForce, _runtimeData.Velocity.z);
                    break;
                case LocomotionAnimationState.Fall:
                    ResetFallingDuration();
                    _runtimeData.Velocity = new Vector3(_runtimeData.Velocity.x, 0f, _runtimeData.Velocity.z);
                    DeactivateCrouch();
                    _runtimeData.IsSliding = false;
                    break;
                case LocomotionAnimationState.Crouch:
                    if (_inputReader != null) _inputReader.onJumpPerformed += CrouchToJumpState;
                    break;
                case LocomotionAnimationState.Roll:
                    // 구르기 중에는 점프 구독이 없고(점프 차단), 수평 속도는 매 프레임 구르기 방향/속도로 정해진다.
                    _runtimeData.IsSliding = false;
                    _runtimeData.Velocity = new Vector3(0f, _runtimeData.Velocity.y, 0f);
                    break;
            }
        }

        private void ExitCurrentState()
        {
            switch (_runtimeData.CurrentState)
            {
                case LocomotionAnimationState.Locomotion:
                    if (_inputReader != null) _inputReader.onJumpPerformed -= LocomotionToJumpState;
                    break;
                case LocomotionAnimationState.Jump:
                    _visualizer.SetJumpingAnim(false);
                    break;
                case LocomotionAnimationState.Crouch:
                    if (_inputReader != null) _inputReader.onJumpPerformed -= CrouchToJumpState;
                    break;
                case LocomotionAnimationState.Roll:
                    // 구르기를 벗어나는 모든 경로(정상 종료/CancelRoll/사망/비활성)의 공통 정리 지점.
                    // 무적 구간 중이었다면 레이어를 그 시점 진영의 몸체 레이어로 복원한다.
                    if (_runtimeData.IsRollInvincible) _visualizer.SetAttackIgnored(false);
                    _runtimeData.EndRoll();
                    _runtimeData.Velocity = new Vector3(0f, _runtimeData.Velocity.y, 0f);
                    break;
            }
        }

        public void Tick()
        {
            UpdateSprintLock();
            ProcessRollRequest();

            switch (_runtimeData.CurrentState)
            {
                case LocomotionAnimationState.Locomotion:
                    UpdateLocomotionState();
                    break;
                case LocomotionAnimationState.Jump:
                    UpdateJumpState();
                    break;
                case LocomotionAnimationState.Fall:
                    UpdateFallState();
                    break;
                case LocomotionAnimationState.Crouch:
                    UpdateCrouchState();
                    break;
                case LocomotionAnimationState.Roll:
                    UpdateRollState();
                    break;
            }
        }

        #endregion

        #region State Updates

        private void UpdateLocomotionState()
        {
            GroundedCheck();

            if (!_runtimeData.IsGrounded && !_visualizer.IsControllerGrounded)
            {
                SwitchState(LocomotionAnimationState.Fall);
            }

            if (_runtimeData.IsCrouching)
            {
                SwitchState(LocomotionAnimationState.Crouch);
            }

            CheckEnableTurns();
            CheckEnableLean();
            CalculateRotationalAdditives(_runtimeData.EnableLean, _runtimeData.EnableHeadTurn, _runtimeData.EnableBodyTurn);

            ConsumeSprintStamina();
            CalculateMoveDirection();

            // 접지 중력 유지
            Vector3 vel = _runtimeData.Velocity;
            vel.y = -2f;
            _runtimeData.Velocity = vel;

            CheckIfStarting();
            CheckIfStopped();
            FaceMoveDirection();
            Move();
            _visualizer.UpdateAnimator(_runtimeData);
        }

        private void UpdateJumpState()
        {
            ApplyGravity();

            if (_runtimeData.Velocity.y <= 0f)
            {
                _visualizer.SetJumpingAnim(false);
                SwitchState(LocomotionAnimationState.Fall);
            }

            GroundedCheck();
            CalculateRotationalAdditives(false, _runtimeData.EnableHeadTurn, _runtimeData.EnableBodyTurn);
            CalculateMoveDirection();
            FaceMoveDirection();
            Move();
            _visualizer.UpdateAnimator(_runtimeData);
        }

        private void UpdateFallState()
        {
            GroundedCheck();

            CalculateRotationalAdditives(false, _runtimeData.EnableHeadTurn, _runtimeData.EnableBodyTurn);
            CalculateMoveDirection();
            FaceMoveDirection();

            ApplyGravity();
            Move();
            _visualizer.UpdateAnimator(_runtimeData);

            if (_visualizer.IsControllerGrounded || _runtimeData.IsGrounded)
            {
                SwitchState(LocomotionAnimationState.Locomotion);
            }

            UpdateFallingDuration();
        }

        /// <summary>
        /// 구르기 진행(19-B). rollDuration 동안 rollDirection으로 rollDistance만큼 등속 이동하고, 무적 구간(rollInvincibleStart~End)에는
        /// IgnoreAttack 레이어를 적용한다. 게임 시간(Time.deltaTime) 기준이라 시간정지/일시정지(dt=0) 중에는 멈춘다.
        /// 구르기 중에는 이동/점프/달리기 입력을 읽지 않는다(애니메이션은 클립 확보 후 연결).
        /// </summary>
        private void UpdateRollState()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float duration = _pureData.RollDuration;
            float remaining = duration - _runtimeData.RollElapsed;
            bool finishing = dt >= remaining;
            float stepDt = finishing ? remaining : dt;

            // 마지막 프레임은 남은 시간만큼만 이동해 총 이동 거리가 정확히 rollDistance가 되게 한다.
            float horizontalSpeed = _pureData.RollDistance / duration * (stepDt / dt);

            if (_visualizer.IsControllerGrounded)
            {
                _runtimeData.Velocity = new Vector3(_runtimeData.Velocity.x, -2f, _runtimeData.Velocity.z);
            }
            else
            {
                ApplyGravity();
            }

            Vector3 rollVelocity = _runtimeData.RollDirection * horizontalSpeed;
            rollVelocity.y = _runtimeData.Velocity.y;
            _runtimeData.Velocity = rollVelocity;
            Move();

            _runtimeData.SetRollElapsed(finishing ? duration : _runtimeData.RollElapsed + dt);

            if (finishing)
            {
                SwitchState(LocomotionAnimationState.Locomotion);
                return;
            }

            UpdateRollInvincibility();
        }

        private void UpdateCrouchState()
        {
            GroundedCheck();

            if (!_runtimeData.IsGrounded)
            {
                DeactivateCrouch();
                ApplyCapsuleSize(false);
                SwitchState(LocomotionAnimationState.Fall);
            }

            CeilingHeightCheck();

            if (!_runtimeData.CrouchKeyPressed && !_runtimeData.CannotStandUp)
            {
                DeactivateCrouch();
                SwitchState(LocomotionAnimationState.Locomotion);
            }

            if (!_runtimeData.IsCrouching)
            {
                ApplyCapsuleSize(false);
                SwitchState(LocomotionAnimationState.Locomotion);
            }

            CheckEnableTurns();
            CheckEnableLean();
            CalculateRotationalAdditives(false, _runtimeData.EnableHeadTurn, false);

            CalculateMoveDirection();
            CheckIfStarting();
            CheckIfStopped();

            FaceMoveDirection();
            Move();
            _visualizer.UpdateAnimator(_runtimeData);
        }

        #endregion

        #region Actions & Calculations

        #region Roll (19-B)

        private void RequestRoll()
        {
            _rollRequested = true;
        }

        private void ProcessRollRequest()
        {
            if (!_rollRequested) return;

            _rollRequested = false;
            TryStartRoll();
        }

        /// <summary>
        /// 구르기 시작 조건(전부 만족): 지면 접촉의 로코모션 상태(공중/앉기/점프/낙하 불가), 구르기 중 아님, 입력 컨텍스트 Player(Tactical/UI/Event/시간정지 불가),
        /// 스태미나 TryUseStamina(rollStaminaCost) 성공. 스태미나 소모는 가장 마지막에 시도하므로 다른 조건 실패나 스태미나 부족 시에는 소모가 없다.
        /// </summary>
        private bool TryStartRoll()
        {
            if (_pureData == null || _statService == null) return false;
            if (_runtimeData.IsRolling) return false;
            if (_runtimeData.CurrentState != LocomotionAnimationState.Locomotion || !_runtimeData.IsGrounded) return false;
            if (!IsPlayerInputContext()) return false;
            if (!_statService.TryUseStamina(_pureData.RollStaminaCost)) return false;

            _runtimeData.BeginRoll(CalculateRollDirection());
            SwitchState(LocomotionAnimationState.Roll);
            UpdateRollInvincibility();
            return true;
        }

        private bool IsPlayerInputContext()
        {
            var current = _inputContextManager?.CurrentContext;
            return current != null && current.ContextType == Common.InputSystem.InputContextType.Player;
        }

        /// <summary>
        /// WASD 입력을 카메라 기준 월드 방향으로 변환한다. 입력이 없으면 캐릭터가 현재 바라보는 방향(transform.forward의 수평 성분).
        /// </summary>
        private Vector3 CalculateRollDirection()
        {
            Vector2 input = _inputReader != null ? _inputReader._moveComposite : Vector2.zero;
            Vector3 direction = (_visualizer.GetCameraForwardZeroedY() * input.y) + (_visualizer.GetCameraRightZeroedY() * input.x);
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }

            Vector3 facing = _visualizer.Forward;
            facing.y = 0f;
            return facing.normalized;
        }

        /// <summary>무적 구간(rollInvincibleStart 이상, rollInvincibleEnd 미만)에 들어가거나 나올 때만 IgnoreAttack 레이어를 켜고 끈다.</summary>
        private void UpdateRollInvincibility()
        {
            float elapsed = _runtimeData.RollElapsed;
            bool inWindow = elapsed >= _pureData.RollInvincibleStart && elapsed < _pureData.RollInvincibleEnd;
            if (_runtimeData.IsRollInvincible == inWindow) return;

            _runtimeData.SetRollInvincible(inWindow);
            _visualizer.SetAttackIgnored(inWindow);
        }

        /// <summary>
        /// 구르기를 중단한다(피격 경직/사망/비주얼라이저 비활성 등 외부 요인). 구르기 중이 아니면 아무것도 하지 않는다.
        /// 로코모션 상태로 돌아가면서 상태 이탈 정리(ExitCurrentState)가 IgnoreAttack 레이어를 그 시점 진영의 몸체 레이어로 복원하고 IsRolling을 내린다.
        /// 지면이 없으면 다음 Tick의 로코모션 상태가 낙하로 넘긴다.
        /// </summary>
        public void CancelRoll()
        {
            if (!_runtimeData.IsRolling) return;

            SwitchState(LocomotionAnimationState.Locomotion);
        }

        #endregion

        private void LocomotionToJumpState() => SwitchState(LocomotionAnimationState.Jump);

        private void CrouchToJumpState()
        {
            if (!_runtimeData.CannotStandUp)
            {
                DeactivateCrouch();
                SwitchState(LocomotionAnimationState.Jump);
            }
        }

        private void ActivateAim()
        {
            _runtimeData.IsAiming = true;
            _runtimeData.IsStrafing = !_runtimeData.IsSprinting;
        }

        private void DeactivateAim()
        {
            _runtimeData.IsAiming = false;
            _runtimeData.IsStrafing = !_runtimeData.IsSprinting && ((_pureData != null && _pureData.AlwaysStrafe) || _runtimeData.IsLockedOn);
        }

        private void ToggleLockOn()
        {
            _lockOnController?.ToggleLockOn();
        }

        private void ToggleWalk() => EnableWalk(!_runtimeData.IsWalking);

        private void EnableWalk(bool enable)
        {
            _runtimeData.IsWalking = enable && _runtimeData.IsGrounded && !_runtimeData.IsSprinting;
        }

        /// <summary>
        /// 달리기 연속 소모(19-A). 이동 입력이 있는 상태로 달리는 동안(접지 로코모션 상태) 매 프레임 게임 시간 기준으로 소모하고,
        /// 스태미나가 0이 되면 달리기를 해제하고 잠근다. 시간정지/일시정지 중(dt=0)에는 소모되지 않는다.
        /// </summary>
        private void ConsumeSprintStamina()
        {
            if (_statService == null || !_runtimeData.IsSprinting) return;
            if (_runtimeData.MoveDirection.magnitude <= 0.01f) return;

            float costPerSecond = _pureData != null ? _pureData.SprintStaminaCostPerSecond : 0f;
            _statService.DrainStamina(costPerSecond * Time.deltaTime);

            if (_statService.Stamina <= 0f)
            {
                DeactivateSprint();
                _runtimeData.SetSprintLocked(true);
            }
        }

        /// <summary>
        /// 달리기 잠금 해제 판정(19-A). 스태미나가 최대치 x sprintRestartThresholdRatio(PureData) 이상으로 회복되면 풀린다.
        /// 풀려도 달리기가 자동 재개되지는 않고, 다시 달리기 입력(Shift 누름)이 있어야 시작된다.
        /// </summary>
        private void UpdateSprintLock()
        {
            if (_statService == null || !_runtimeData.IsSprintLocked) return;

            float ratio = _pureData != null ? _pureData.SprintRestartThresholdRatio : 0f;
            float stamina = _statService.Stamina;
            if (stamina > 0f && stamina >= _statService.MaxStamina * ratio)
            {
                _runtimeData.SetSprintLocked(false);
            }
        }

        private void ActivateSprint()
        {
            // 구르기 중에는 달리기 입력을 차단한다(19-B).
            if (_runtimeData.IsRolling) return;
            if (_runtimeData.IsSprintLocked) return;

            // 스태미나가 이미 0이면(구르기/공격 소모 등) 달리기를 시작하지 않고 같은 잠금 규칙으로 회복을 기다린다.
            if (_statService != null && _statService.Stamina <= 0f)
            {
                _runtimeData.SetSprintLocked(true);
                return;
            }

            if (!_runtimeData.IsCrouching)
            {
                EnableWalk(false);
                _runtimeData.SetSprinting(true);
                _runtimeData.IsStrafing = false;
            }
        }

        private void DeactivateSprint()
        {
            _runtimeData.SetSprinting(false);
            if ((_pureData != null && _pureData.AlwaysStrafe) || _runtimeData.IsAiming || _runtimeData.IsLockedOn)
            {
                _runtimeData.IsStrafing = true;
            }
        }

        private void ActivateCrouch()
        {
            // 구르기 중에는 앉기 입력도 차단한다(19-B) - 캡슐 크기가 구르기 도중 바뀌지 않게.
            if (_runtimeData.IsRolling) return;

            _runtimeData.CrouchKeyPressed = true;
            if (_runtimeData.IsGrounded)
            {
                ApplyCapsuleSize(true);
                DeactivateSprint();
                _runtimeData.SetCrouching(true);
            }
        }

        private void DeactivateCrouch()
        {
            _runtimeData.CrouchKeyPressed = false;
            if (!_runtimeData.CannotStandUp && !_runtimeData.IsSliding)
            {
                ApplyCapsuleSize(false);
                _runtimeData.SetCrouching(false);
            }
        }

        private void ApplyCapsuleSize(bool crouching)
        {
            if (_pureData == null) return;
            float height = crouching ? _pureData.CapsuleCrouchingHeight : _pureData.CapsuleStandingHeight;
            float centerY = crouching ? _pureData.CapsuleCrouchingCentre : _pureData.CapsuleStandingCentre;
            _visualizer.SetCapsuleSize(height, centerY);
        }

        private void CalculateInput()
        {
            if (_inputReader == null) return;

            float buttonHoldThreshold = _pureData != null ? _pureData.ButtonHoldThreshold : 0.15f;

            if (_inputReader._movementInputDetected)
            {
                if (_inputReader._movementInputDuration == 0)
                {
                    _runtimeData.MovementInputTapped = true;
                }
                else if (_inputReader._movementInputDuration > 0 && _inputReader._movementInputDuration < buttonHoldThreshold)
                {
                    _runtimeData.MovementInputTapped = false;
                    _runtimeData.MovementInputPressed = true;
                    _runtimeData.MovementInputHeld = false;
                }
                else
                {
                    _runtimeData.MovementInputTapped = false;
                    _runtimeData.MovementInputPressed = false;
                    _runtimeData.MovementInputHeld = true;
                }

                _inputReader._movementInputDuration += Time.deltaTime;
            }
            else
            {
                _inputReader._movementInputDuration = 0;
                _runtimeData.MovementInputTapped = false;
                _runtimeData.MovementInputPressed = false;
                _runtimeData.MovementInputHeld = false;
            }

            _runtimeData.MoveDirection = (_visualizer.GetCameraForwardZeroedY() * _inputReader._moveComposite.y)
                + (_visualizer.GetCameraRightZeroedY() * _inputReader._moveComposite.x);
        }

        private void Move()
        {
            _visualizer.Move(_runtimeData.Velocity);

            if (_runtimeData.IsLockedOn && _runtimeData.CurrentLockOnTarget != null)
            {
                _visualizer.SetLockOnPos(_runtimeData.CurrentLockOnTarget.transform.position);
            }
        }

        private void ApplyGravity()
        {
            float gravityMultiplier = _pureData != null ? _pureData.GravityMultiplier : 2f;
            if (_runtimeData.Velocity.y > Physics.gravity.y)
            {
                Vector3 vel = _runtimeData.Velocity;
                vel.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
                _runtimeData.Velocity = vel;
            }
        }

        private void CalculateMoveDirection()
        {
            CalculateInput();

            float walkSpeed = _pureData != null ? _pureData.WalkSpeed : 1.4f;
            float runSpeed = _pureData != null ? _pureData.RunSpeed : 2.5f;
            float sprintSpeed = _pureData != null ? _pureData.SprintSpeed : 7f;
            float speedChangeDamping = _pureData != null ? _pureData.SpeedChangeDamping : 10f;

            if (!_runtimeData.IsGrounded)
            {
                _runtimeData.TargetMaxSpeed = _runtimeData.CurrentMaxSpeed;
            }
            else if (_runtimeData.IsCrouching)
            {
                _runtimeData.TargetMaxSpeed = walkSpeed;
            }
            else if (_runtimeData.IsSprinting)
            {
                _runtimeData.TargetMaxSpeed = sprintSpeed;
            }
            else if (_runtimeData.IsWalking)
            {
                _runtimeData.TargetMaxSpeed = walkSpeed;
            }
            else
            {
                _runtimeData.TargetMaxSpeed = runSpeed;
            }

            _runtimeData.CurrentMaxSpeed = Mathf.Lerp(_runtimeData.CurrentMaxSpeed, _runtimeData.TargetMaxSpeed, ANIMATION_DAMP_TIME * Time.deltaTime);

            Vector3 targetVel = _runtimeData.TargetVelocity;
            targetVel.x = _runtimeData.MoveDirection.x * _runtimeData.CurrentMaxSpeed;
            targetVel.z = _runtimeData.MoveDirection.z * _runtimeData.CurrentMaxSpeed;
            _runtimeData.TargetVelocity = targetVel;

            Vector3 vel = _runtimeData.Velocity;
            vel.z = Mathf.Lerp(vel.z, targetVel.z, speedChangeDamping * Time.deltaTime);
            vel.x = Mathf.Lerp(vel.x, targetVel.x, speedChangeDamping * Time.deltaTime);
            _runtimeData.Velocity = vel;

            float speed2D = new Vector3(vel.x, 0f, vel.z).magnitude;
            _runtimeData.Speed2D = Mathf.Round(speed2D * 1000f) / 1000f;

            Vector3 playerForward = _visualizer.Forward;
            _runtimeData.NewDirectionDifferenceAngle = playerForward != _runtimeData.MoveDirection
                ? Vector3.SignedAngle(playerForward, _runtimeData.MoveDirection, Vector3.up)
                : 0f;

            CalculateGait();
        }

        private void CalculateGait()
        {
            float walkSpeed = _pureData != null ? _pureData.WalkSpeed : 1.4f;
            float runSpeed = _pureData != null ? _pureData.RunSpeed : 2.5f;
            float sprintSpeed = _pureData != null ? _pureData.SprintSpeed : 7f;

            float runThreshold = (walkSpeed + runSpeed) / 2;
            float sprintThreshold = (runSpeed + sprintSpeed) / 2;

            if (_runtimeData.Speed2D < 0.01f)
            {
                _runtimeData.CurrentGait = LocomotionGaitState.Idle;
            }
            else if (_runtimeData.Speed2D < runThreshold)
            {
                _runtimeData.CurrentGait = LocomotionGaitState.Walk;
            }
            else if (_runtimeData.Speed2D < sprintThreshold)
            {
                _runtimeData.CurrentGait = LocomotionGaitState.Run;
            }
            else
            {
                _runtimeData.CurrentGait = LocomotionGaitState.Sprint;
            }
        }

        private void FaceMoveDirection()
        {
            Vector3 charForward = new Vector3(_visualizer.Forward.x, 0f, _visualizer.Forward.z).normalized;
            Vector3 charRight = new Vector3(_visualizer.Right.x, 0f, _visualizer.Right.z).normalized;
            Vector3 dirForward = new Vector3(_runtimeData.MoveDirection.x, 0f, _runtimeData.MoveDirection.z).normalized;

            Vector3 camForward = _visualizer.GetCameraForwardZeroedY();
            Vector3 targetFacingDir = camForward;

            if (_runtimeData.IsLockedOn && _runtimeData.CurrentLockOnTarget != null)
            {
                Vector3 lockDir = _runtimeData.CurrentLockOnTarget.transform.position - _visualizer.Position;
                lockDir.y = 0f;
                if (lockDir != Vector3.zero)
                {
                    targetFacingDir = lockDir.normalized;
                }
            }

            Quaternion strafingTargetRot = Quaternion.LookRotation(targetFacingDir);

            _runtimeData.StrafeAngle = charForward != dirForward ? Vector3.SignedAngle(charForward, dirForward, Vector3.up) : 0f;
            _runtimeData.IsTurningInPlace = false;

            float rotationSmoothing = _pureData != null ? _pureData.RotationSmoothing : 10f;
            float forwardStrafeMin = _pureData != null ? _pureData.ForwardStrafeMinThreshold : -55.0f;
            float forwardStrafeMax = _pureData != null ? _pureData.ForwardStrafeMaxThreshold : 125.0f;

            if (_runtimeData.IsStrafing)
            {
                if (_runtimeData.MoveDirection.magnitude > 0.01f)
                {
                    if (camForward != Vector3.zero)
                    {
                        _runtimeData.ShuffleDirectionZ = Vector3.Dot(charForward, dirForward);
                        _runtimeData.ShuffleDirectionX = Vector3.Dot(charRight, dirForward);

                        UpdateStrafeDirection(_runtimeData.ShuffleDirectionZ, _runtimeData.ShuffleDirectionX);
                        _runtimeData.CameraRotationOffset = Mathf.Lerp(_runtimeData.CameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);

                        float targetValue = _runtimeData.StrafeAngle > forwardStrafeMin && _runtimeData.StrafeAngle < forwardStrafeMax ? 1f : 0f;

                        if (Mathf.Abs(_runtimeData.ForwardStrafe - targetValue) <= 0.001f)
                        {
                            _runtimeData.ForwardStrafe = targetValue;
                        }
                        else
                        {
                            float t = Mathf.Clamp01(STRAFE_DIRECTION_DAMP_TIME * Time.deltaTime);
                            _runtimeData.ForwardStrafe = Mathf.SmoothStep(_runtimeData.ForwardStrafe, targetValue, t);
                        }
                    }

                    _visualizer.SetRotation(Quaternion.Slerp(_visualizer.Transform.rotation, strafingTargetRot, rotationSmoothing * Time.deltaTime));
                }
                else
                {
                    UpdateStrafeDirection(1f, 0f);

                    float t = 20 * Time.deltaTime;
                    float newOffset = charForward != camForward ? Vector3.SignedAngle(charForward, camForward, Vector3.up) : 0f;

                    _runtimeData.CameraRotationOffset = Mathf.Lerp(_runtimeData.CameraRotationOffset, newOffset, t);

                    if (Mathf.Abs(_runtimeData.CameraRotationOffset) > 10)
                    {
                        _runtimeData.IsTurningInPlace = true;
                    }
                }
            }
            else
            {
                UpdateStrafeDirection(1f, 0f);
                _runtimeData.CameraRotationOffset = Mathf.Lerp(_runtimeData.CameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);

                _runtimeData.ShuffleDirectionZ = 1;
                _runtimeData.ShuffleDirectionX = 0;

                Vector3 faceDir = new Vector3(_runtimeData.Velocity.x, 0f, _runtimeData.Velocity.z);
                if (faceDir != Vector3.zero)
                {
                    _visualizer.SetRotation(Quaternion.Slerp(_visualizer.Transform.rotation, Quaternion.LookRotation(faceDir), rotationSmoothing * Time.deltaTime));
                }
            }
        }

        private void CheckIfStopped()
        {
            _runtimeData.IsStopped = _runtimeData.MoveDirection.magnitude == 0 && _runtimeData.Speed2D < 0.5f;
        }

        private void CheckIfStarting()
        {
            _runtimeData.LocomotionStartTimer = VariableOverrideDelayTimer(_runtimeData.LocomotionStartTimer);

            bool isStartingCheck = false;

            if (_runtimeData.LocomotionStartTimer <= 0.0f)
            {
                if (_runtimeData.MoveDirection.magnitude > 0.01f && _runtimeData.Speed2D < 1f && !_runtimeData.IsStrafing)
                {
                    isStartingCheck = true;
                }

                if (isStartingCheck)
                {
                    if (!_runtimeData.IsStarting)
                    {
                        _runtimeData.LocomotionStartDirection = _runtimeData.NewDirectionDifferenceAngle;
                        _visualizer.SetLocomotionStartDirectionAnim(_runtimeData.LocomotionStartDirection);
                    }

                    float delayTime = 0.2f;
                    _runtimeData.LeanDelay = delayTime;
                    _runtimeData.HeadLookDelay = delayTime;
                    _runtimeData.BodyLookDelay = delayTime;
                    _runtimeData.LocomotionStartTimer = delayTime;
                }
            }
            else
            {
                isStartingCheck = true;
            }

            _runtimeData.IsStarting = isStartingCheck;
            _visualizer.SetStartingAnim(_runtimeData.IsStarting);
        }

        private void UpdateStrafeDirection(float targetZ, float targetX)
        {
            _runtimeData.StrafeDirectionZ = Mathf.Lerp(_runtimeData.StrafeDirectionZ, targetZ, ANIMATION_DAMP_TIME * Time.deltaTime);
            _runtimeData.StrafeDirectionX = Mathf.Lerp(_runtimeData.StrafeDirectionX, targetX, ANIMATION_DAMP_TIME * Time.deltaTime);
            _runtimeData.StrafeDirectionZ = Mathf.Round(_runtimeData.StrafeDirectionZ * 1000f) / 1000f;
            _runtimeData.StrafeDirectionX = Mathf.Round(_runtimeData.StrafeDirectionX * 1000f) / 1000f;
        }

        private void GroundedCheck()
        {
            float offset = _pureData != null ? _pureData.GroundedOffset : 0.14f;
            LayerMask mask = _pureData != null ? _pureData.GroundLayerMask : (LayerMask)1;

            _runtimeData.SetGrounded(_visualizer.CheckGrounded(offset, mask));

            if (_runtimeData.IsGrounded)
            {
                GroundInclineCheck();
            }
        }

        private void GroundInclineCheck()
        {
            LayerMask mask = _pureData != null ? _pureData.GroundLayerMask : (LayerMask)1;
            float rawAngle = _visualizer.CheckGroundIncline(mask);
            _runtimeData.InclineAngle = Mathf.Lerp(_runtimeData.InclineAngle, rawAngle, 20f * Time.deltaTime);
        }

        private void CeilingHeightCheck()
        {
            float capsuleHeight = _pureData != null ? _pureData.CapsuleStandingHeight : 1.8f;
            LayerMask mask = _pureData != null ? _pureData.GroundLayerMask : (LayerMask)1;
            _runtimeData.CannotStandUp = _visualizer.CheckCeilingHeight(capsuleHeight, mask);
        }

        private void ResetFallingDuration()
        {
            _runtimeData.FallStartTime = Time.time;
            _runtimeData.FallingDuration = 0f;
        }

        private void UpdateFallingDuration()
        {
            _runtimeData.FallingDuration = Time.time - _runtimeData.FallStartTime;
        }

        private void CheckEnableTurns()
        {
            _runtimeData.HeadLookDelay = VariableOverrideDelayTimer(_runtimeData.HeadLookDelay);
            _runtimeData.EnableHeadTurn = _runtimeData.HeadLookDelay == 0.0f && !_runtimeData.IsStarting;

            _runtimeData.BodyLookDelay = VariableOverrideDelayTimer(_runtimeData.BodyLookDelay);
            _runtimeData.EnableBodyTurn = _runtimeData.BodyLookDelay == 0.0f && !(_runtimeData.IsStarting || _runtimeData.IsTurningInPlace);
        }

        private void CheckEnableLean()
        {
            _runtimeData.LeanDelay = VariableOverrideDelayTimer(_runtimeData.LeanDelay);
            _runtimeData.EnableLean = _runtimeData.LeanDelay == 0.0f && !(_runtimeData.IsStarting || _runtimeData.IsTurningInPlace);
        }

        private void CalculateRotationalAdditives(bool leansActivated, bool headLookActivated, bool bodyLookActivated)
        {
            if (headLookActivated || leansActivated || bodyLookActivated)
            {
                _runtimeData.CurrentRotation = _visualizer.Forward;

                _runtimeData.RotationRate = _runtimeData.CurrentRotation != _runtimeData.PreviousRotation
                    ? Vector3.SignedAngle(_runtimeData.CurrentRotation, _runtimeData.PreviousRotation, Vector3.up) / Time.deltaTime * -1f
                    : 0f;
            }

            _runtimeData.InitialLeanValue = leansActivated ? _runtimeData.RotationRate : 0f;

            float leanSmoothness = 5f;
            float maxLeanRate = 275.0f;
            float sprintSpeed = _pureData != null ? _pureData.SprintSpeed : 7f;
            AnimationCurve leanCurve = _pureData != null ? _pureData.LeanCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);
            AnimationCurve headCurve = _pureData != null ? _pureData.HeadLookXCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);
            AnimationCurve bodyCurve = _pureData != null ? _pureData.BodyLookXCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f);

            float refValue = _runtimeData.Speed2D / sprintSpeed;
            _runtimeData.LeanValue = CalculateSmoothedValue(_runtimeData.LeanValue, _runtimeData.InitialLeanValue, maxLeanRate, leanSmoothness, leanCurve, refValue, true);

            float headSmoothness = 5f;
            if (headLookActivated && _runtimeData.IsTurningInPlace)
            {
                _runtimeData.InitialTurnValue = _runtimeData.CameraRotationOffset;
                _runtimeData.HeadLookX = Mathf.Lerp(_runtimeData.HeadLookX, _runtimeData.InitialTurnValue / 200f, 5f * Time.deltaTime);
            }
            else
            {
                _runtimeData.InitialTurnValue = headLookActivated ? _runtimeData.RotationRate : 0f;
                _runtimeData.HeadLookX = CalculateSmoothedValue(_runtimeData.HeadLookX, _runtimeData.InitialTurnValue, maxLeanRate, headSmoothness, headCurve, _runtimeData.HeadLookX, false);
            }

            float bodySmoothness = 5f;
            _runtimeData.InitialTurnValue = bodyLookActivated ? _runtimeData.RotationRate : 0f;
            _runtimeData.BodyLookX = CalculateSmoothedValue(_runtimeData.BodyLookX, _runtimeData.InitialTurnValue, maxLeanRate, bodySmoothness, bodyCurve, _runtimeData.BodyLookX, false);

            float cameraTilt = _visualizer.GetCameraTiltX();
            cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180f;
            cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);
            _runtimeData.HeadLookY = cameraTilt;
            _runtimeData.BodyLookY = cameraTilt;

            _runtimeData.PreviousRotation = _runtimeData.CurrentRotation;
        }

        private float CalculateSmoothedValue(
            float mainVal,
            float newVal,
            float maxRate,
            float smoothness,
            AnimationCurve curve,
            float refVal,
            bool isMultiplier)
        {
            float changeVal = Mathf.Clamp(newVal / maxRate, -1.0f, 1.0f);

            if (isMultiplier)
            {
                changeVal *= curve.Evaluate(refVal);
            }
            else
            {
                changeVal = curve.Evaluate(changeVal);
            }

            if (!changeVal.Equals(mainVal))
            {
                changeVal = Mathf.Lerp(mainVal, changeVal, smoothness * Time.deltaTime);
            }

            return changeVal;
        }

        private float VariableOverrideDelayTimer(float timeVar)
        {
            if (timeVar > 0.0f)
            {
                timeVar -= Time.deltaTime;
                return Mathf.Clamp(timeVar, 0.0f, 1.0f);
            }
            return 0.0f;
        }

        #endregion
    }
}
