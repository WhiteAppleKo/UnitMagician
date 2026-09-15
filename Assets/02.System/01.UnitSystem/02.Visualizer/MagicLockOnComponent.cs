using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitSystem;
using Movement.Visualizer;
using Movement.RefactoredLocomotion;
using VContainer;
using Common.InputSystem;

namespace UnitSystem
{
    /// <summary>
    /// 플레이어 자식 오브젝트 MagicLockOn에 부착되는 시간 정지 마법 다중 락온 컴포넌트
    /// 일반 락온과 100% 동일한 탐색 및 가중치 알고리즘으로 예비 락온을 표시하며, 휠 클릭 시 목록에 누적합니다.
    /// </summary>
    public class MagicLockOnComponent : MonoBehaviour, ILockOnComponent
    {
        [Header("Settings")]
        [SerializeField] private float scanRadius = 25f;

        private RuntimeDataMultiLockOn _multiLockOnData;
        private IMultiLockOnVisualizer _visualizer;
        private ILocomotionVisualizer _locomotionVisualizer;

        private RuntimeDataUnitGroup _currentHoverTarget;
        private readonly Collider[] _scanColliderBuffer = new Collider[32];

        private UnitCasterSystem _unitCasterSystem;
        private IInputContextManager _inputContextManager;
        private Camera _mainCamera;

        private Vector2 _rightClickDownPos;
        private bool _isRightClickDown;
        private float _maxRightDragDist;
        private const float DRAG_CANCEL_THRESHOLD = 5f;

        [Inject]
        public void Construct(
            RuntimeDataMultiLockOn multiLockOnData,
            IMultiLockOnVisualizer visualizer = null,
            UnitCasterSystem unitCasterSystem = null,
            IInputContextManager inputContextManager = null)
        {
            _multiLockOnData = multiLockOnData;
            _visualizer = visualizer;
            _unitCasterSystem = unitCasterSystem;
            _inputContextManager = inputContextManager;
        }

        public void Initialize(
            RuntimeDataMultiLockOn multiLockOnData,
            IMultiLockOnVisualizer visualizer = null,
            ILocomotionVisualizer locomotionVisualizer = null,
            UnitCasterSystem unitCasterSystem = null)
        {
            _multiLockOnData = multiLockOnData;
            _visualizer = visualizer;
            _locomotionVisualizer = locomotionVisualizer;
            _unitCasterSystem = unitCasterSystem;
        }

        private void Awake()
        {
            if (_visualizer == null) _visualizer = GetComponentInParent<IMultiLockOnVisualizer>();
            if (_locomotionVisualizer == null) _locomotionVisualizer = GetComponentInParent<ILocomotionVisualizer>();
            if (_unitCasterSystem == null) _unitCasterSystem = GetComponentInParent<UnitCasterSystem>();
        }

        private void OnEnable()
        {
            if (_visualizer == null) _visualizer = GetComponentInParent<IMultiLockOnVisualizer>();
            if (_locomotionVisualizer == null) _locomotionVisualizer = GetComponentInParent<ILocomotionVisualizer>();
            if (_unitCasterSystem == null) _unitCasterSystem = GetComponentInParent<UnitCasterSystem>();

            if (_visualizer != null)
            {
                _visualizer.SetCrosshairVisible(true);
                _visualizer.UpdateLockOnCount(0);
            }

            _multiLockOnData?.ClearTargets();
            _currentHoverTarget = null;
            _isRightClickDown = false;
            _maxRightDragDist = 0f;
        }

        private void OnDisable()
        {
            _isRightClickDown = false;
            _maxRightDragDist = 0f;

            // 예비 타겟 마커 끄기
            if (_currentHoverTarget != null && (_multiLockOnData == null || !_multiLockOnData.Contains(_currentHoverTarget)))
            {
                _currentHoverTarget.Highlight(false, false);
                _currentHoverTarget = null;
            }

            // 시간 정지 해제 순간 UnitCasterSystem.ExecuteBatchCast() 호출하여 예약된 마법 일괄 실행
            if (_unitCasterSystem == null) _unitCasterSystem = GetComponentInParent<UnitCasterSystem>();
            if (_unitCasterSystem == null) _unitCasterSystem = UnityEngine.Object.FindAnyObjectByType<UnitCasterSystem>();

            if (_unitCasterSystem != null)
            {
                _unitCasterSystem.ExecuteBatchCast();
            }
            else
            {
                _multiLockOnData?.RequestBatchCast();
            }

            if (_visualizer != null)
            {
                _visualizer.SetCrosshairVisible(false);
            }
        }

        private void Update()
        {
            // UI 메뉴 등 다른 컨텍스트 활성화 시 마법 락온 조작 완전 차단
            var currentContext = _inputContextManager?.CurrentContext;
            if (currentContext != null && currentContext.ContextType != InputContextType.Tactical)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // 1. 마우스 커서 기반 레이캐스트 탐색
            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mousePos);

            RaycastHit[] hits = Physics.SphereCastAll(ray, 0.4f, scanRadius);
            RuntimeDataUnitGroup newHoverTarget = null;

            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    var hit = hits[i];
                    if (hit.collider == null) continue;

                    if (_locomotionVisualizer != null && _locomotionVisualizer.Transform != null && hit.collider.transform.root == _locomotionVisualizer.Transform.root)
                    {
                        continue;
                    }

                    var unitGroup = hit.collider.GetComponentInParent<RuntimeDataUnitGroup>();
                    if (unitGroup != null && unitGroup.IsTargetable)
                    {
                        newHoverTarget = unitGroup;
                        break;
                    }
                }
            }

            // 호버 하이라이트 갱신
            if (_currentHoverTarget != newHoverTarget)
            {
                if (_currentHoverTarget != null && (_multiLockOnData == null || !_multiLockOnData.Contains(_currentHoverTarget)))
                {
                    _currentHoverTarget.Highlight(false, false);
                }

                _currentHoverTarget = newHoverTarget;

                if (_currentHoverTarget != null && (_multiLockOnData == null || !_multiLockOnData.Contains(_currentHoverTarget)))
                {
                    _currentHoverTarget.Highlight(true, false);
                }
            }

            // 2. 마우스 좌클릭 마법 타깃 지정
            if (mouse.leftButton.wasPressedThisFrame && _currentHoverTarget != null)
            {
                if (_multiLockOnData != null && !_multiLockOnData.Contains(_currentHoverTarget))
                {
                    _multiLockOnData.AddTarget(_currentHoverTarget);
                    _currentHoverTarget.Highlight(true, true);
                    _visualizer?.UpdateLockOnCount(_multiLockOnData.TargetCount);
                }
            }

            // 3. 마우스 우클릭 드래그 vs 탭 취소 구분
            ProcessRightClickCancel(mouse);
        }

        private void ProcessRightClickCancel(Mouse mouse)
        {
            if (mouse.rightButton.wasPressedThisFrame)
            {
                _isRightClickDown = true;
                _rightClickDownPos = mouse.position.ReadValue();
                _maxRightDragDist = 0f;
            }

            if (_isRightClickDown && mouse.rightButton.isPressed)
            {
                Vector2 curPos = mouse.position.ReadValue();
                float dist = Vector2.Distance(curPos, _rightClickDownPos);
                if (dist > _maxRightDragDist)
                {
                    _maxRightDragDist = dist;
                }
            }

            if (mouse.rightButton.wasReleasedThisFrame && _isRightClickDown)
            {
                Vector2 releasePos = mouse.position.ReadValue();
                float finalDist = Vector2.Distance(releasePos, _rightClickDownPos);

                // 드래그 없이 제자리(5픽셀 미만)에서 뗐을 때: 최근 락온 대상 1개 취소
                if (finalDist < DRAG_CANCEL_THRESHOLD && _maxRightDragDist < DRAG_CANCEL_THRESHOLD)
                {
                    CancelLastLockOnTarget();
                }

                _isRightClickDown = false;
                _maxRightDragDist = 0f;
            }
        }

        private void CancelLastLockOnTarget()
        {
            if (_multiLockOnData != null && _multiLockOnData.TargetCount > 0)
            {
                var targets = _multiLockOnData.LockedTargets;
                var lastTarget = targets[targets.Count - 1];
                _multiLockOnData.RemoveTarget(lastTarget);

                if (lastTarget != null)
                {
                    if (lastTarget == _currentHoverTarget)
                    {
                        lastTarget.Highlight(true, false);
                    }
                    else
                    {
                        lastTarget.Highlight(false, false);
                    }
                }

                _visualizer?.UpdateLockOnCount(_multiLockOnData.TargetCount);
            }
        }

        /// <summary>
        /// 마우스 휠 클릭 시 현재 예비 락온 대상을 확정 락온 목록에 누적/토글합니다.
        /// </summary>
        public void ToggleLockOn()
        {
            if (_currentHoverTarget == null || _multiLockOnData == null) return;

            if (_multiLockOnData.Contains(_currentHoverTarget))
            {
                // 이미 확정 락온된 대상을 다시 클릭하면 락온 해제 -> 예비 락온(노란색)으로 복귀
                _multiLockOnData.RemoveTarget(_currentHoverTarget);
                _currentHoverTarget.Highlight(true, false);
            }
            else
            {
                // 새로운 대상 확정 락온 추가 -> 빨간색 마커 켜기 및 리스트 누적
                _multiLockOnData.AddTarget(_currentHoverTarget);
                _currentHoverTarget.Highlight(true, true);
            }

            _visualizer?.UpdateLockOnCount(_multiLockOnData.TargetCount);
        }
    }
}
