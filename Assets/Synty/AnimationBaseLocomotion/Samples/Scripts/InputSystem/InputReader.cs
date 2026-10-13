// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// Sample scripts are included only as examples and are not intended as production-ready.

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Synty.AnimationBaseLocomotion.Samples.InputSystem
{
    public class InputReader : MonoBehaviour, Controls.IPlayerActions
    {
        public Vector2 _mouseDelta;
        public Vector2 _moveComposite;

        public float _movementInputDuration;
        public bool _movementInputDetected;

        private Controls _controls;

        public Action onAimActivated;
        public Action onAimDeactivated;

        public Action onCrouchActivated;
        public Action onCrouchDeactivated;

        public Action onJumpPerformed;

        /// <summary>구르기 입력(19-B). Space를 달리기 키(Shift) 없이 눌렀을 때만 발행된다. Shift+Space는 onJumpPerformed로만 발행된다.</summary>
        public Action onRollPressed;

        public Action onLockOnToggled;

        public Action onSprintActivated;
        public Action onSprintDeactivated;

        public Action onWalkToggled;

        public Action onTimeSlowToggled;
        public Action onOptionToggled;
        public Action onInventoryToggled;
        public Action onInteractPressed;
        public Action<float> onMouseWheelScrolled;

        /// <inheritdoc cref="OnEnable" />
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }

            _controls.Player.Enable();
        }

        /// <inheritdoc cref="OnDisable" />
        public void OnDisable()
        {
            _controls.Player.Disable();
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.tKey.wasPressedThisFrame)
                {
                    onTimeSlowToggled?.Invoke();
                }

                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    onOptionToggled?.Invoke();
                }

                if (Keyboard.current.iKey.wasPressedThisFrame)
                {
                    onInventoryToggled?.Invoke();
                }

                // 필드 아이템 줍기(임시 키 E). 컨텍스트 판정은 구독자(PickupLogicSystem)가 한다.
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    onInteractPressed?.Invoke();
                }
            }

            if (Mouse.current != null)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    onMouseWheelScrolled?.Invoke(scrollY);
                }
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnLook callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnLook(InputAction.CallbackContext context)
        {
            _mouseDelta = context.ReadValue<Vector2>();
        }

        /// <summary>
        ///     Defines the action to perform when the OnMove callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveComposite = context.ReadValue<Vector2>();
            _movementInputDetected = _moveComposite.magnitude > 0;
        }

        /// <summary>
        ///     Space(Jump 액션) 입력을 누른 그 순간의 달리기 키(Shift) 상태로 분기한다(19-B).
        ///     Shift 누름 -> 점프(onJumpPerformed), Shift 안 누름 -> 구르기(onRollPressed). 한 번의 입력은 둘 중 하나만 발행하므로
        ///     구르기와 점프가 동시에 발동하지 않는다. Shift 상태는 콜백 순서와 무관하게 장치의 현재 상태에서 직접 읽는다
        ///     (Shift와 Space를 같은 프레임에 눌러도 Sprint/Jump 콜백 처리 순서에 영향받지 않는다).
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnJump(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (IsSprintModifierHeld())
            {
                onJumpPerformed?.Invoke();
            }
            else
            {
                onRollPressed?.Invoke();
            }
        }

        /// <summary>
        ///     Sprint 액션에 바인딩된 컨트롤이 지금 눌려 있는지. 특정 키/버튼을 하드코딩하지 않고 Controls의 Sprint 바인딩을 그대로 따른다.
        ///     Sprint 액션 자신의 상태(IsPressed)는 같은 이벤트 안의 콜백 처리 순서에 영향받을 수 있으므로,
        ///     액션에 바인딩된 컨트롤의 현재 장치 상태도 함께 직접 읽는다.
        /// </summary>
        private bool IsSprintModifierHeld()
        {
            InputAction sprint = _controls.Player.Sprint;
            if (sprint.IsPressed())
            {
                return true;
            }

            var controls = sprint.controls;
            for (int i = 0; i < controls.Count; i++)
            {
                if (controls[i].IsPressed())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Defines the action to perform when the OnToggleWalk callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnToggleWalk(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onWalkToggled?.Invoke();
        }

        /// <summary>
        ///     Defines the action to perform when the OnSprint callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onSprintActivated?.Invoke();
            }
            else if (context.canceled)
            {
                onSprintDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnCrouch callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onCrouchActivated?.Invoke();
            }
            else if (context.canceled)
            {
                onCrouchDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnAim callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnAim(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                onAimActivated?.Invoke();
            }

            if (context.canceled)
            {
                onAimDeactivated?.Invoke();
            }
        }

        /// <summary>
        ///     Defines the action to perform when the OnLockOn callback is called.
        /// </summary>
        /// <param name="context">The context of the callback.</param>
        public void OnLockOn(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            onLockOnToggled?.Invoke();
            onSprintDeactivated?.Invoke();
        }
    }
}
