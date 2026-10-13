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

            _multiLockOnData?.ClearEntries();
            _currentHoverTarget = null;
            _isRightClickDown = false;
            _maxRightDragDist = 0f;

            SubscribeQueueEvents();
        }

        private void SubscribeQueueEvents()
        {
            if (_multiLockOnData == null) return;

            // 팝업 확정(CommitPending)처럼 이 컴포넌트 밖에서 큐가 바뀌어도 락온 카운트 표시를 갱신한다.
            _multiLockOnData.OnEntryAdded -= HandleEntryAdded;
            _multiLockOnData.OnEntryAdded += HandleEntryAdded;
            _multiLockOnData.OnEntryRemoved -= HandleEntryRemoved;
            _multiLockOnData.OnEntryRemoved += HandleEntryRemoved;
            _multiLockOnData.OnEntriesCleared -= RefreshLockOnCount;
            _multiLockOnData.OnEntriesCleared += RefreshLockOnCount;
        }

        private void UnsubscribeQueueEvents()
        {
            if (_multiLockOnData == null) return;

            _multiLockOnData.OnEntryAdded -= HandleEntryAdded;
            _multiLockOnData.OnEntryRemoved -= HandleEntryRemoved;
            _multiLockOnData.OnEntriesCleared -= RefreshLockOnCount;
        }

        private void HandleEntryAdded(LockOnCastEntry entry) => RefreshLockOnCount();
        private void HandleEntryRemoved(LockOnCastEntry entry) => RefreshLockOnCount();

        private void RefreshLockOnCount()
        {
            if (_multiLockOnData != null)
            {
                _visualizer?.UpdateLockOnCount(_multiLockOnData.EntryCount);
            }
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

            UnsubscribeQueueEvents();

            if (_visualizer != null)
            {
                _visualizer.SetCrosshairVisible(false);
            }
        }

        private void Update()
        {
            // UI 메뉴 등 다른 컨텍스트 활성화 시 마법 락온 조작 완전 차단.
            // 3단계 이벤트 중에는 스택 최상단이 Event(Tactical은 그 아래)이므로 판정을 MagicLockOnInputGate 한 곳에 모아
            // 이벤트 지정 창이 열려 있는 동안은 허용하고, 이탈 구간(지정 창 닫힘)/UI 컨텍스트에서는 차단한다.
            var currentContext = _inputContextManager?.CurrentContext;
            if (!MagicLockOnInputGate.IsInputAllowed(currentContext, _multiLockOnData))
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

            // 2. 마우스 좌클릭 락온 → 마법 지정 대기(팝업 오픈). 이미 큐에 있는 대상이어도 다시 락온해 마법을 더 지정할 수 있다.
            if (mouse.leftButton.wasPressedThisFrame)
            {
                BeginPendingOnHoverTarget();
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

                // 드래그 없이 제자리(5픽셀 미만)에서 뗐을 때: 팝업(지정 대기) 중이면 지정 취소, 아니면 최근 지정된 쌍 1개 취소
                if (finalDist < DRAG_CANCEL_THRESHOLD && _maxRightDragDist < DRAG_CANCEL_THRESHOLD)
                {
                    if (_multiLockOnData != null && _multiLockOnData.HasPending)
                    {
                        CancelPendingLockOn();
                    }
                    else
                    {
                        CancelLastLockOnEntry();
                    }
                }

                _isRightClickDown = false;
                _maxRightDragDist = 0f;
            }
        }

        /// <summary>
        /// 현재 예비 락온(호버) 대상을 락온해 마법 지정 대기(PendingTarget)로 만든다 → 팝업이 열린다.
        /// 팝업이 열려 있는(지정 대기 중인) 동안에는 새 락온을 시작하지 않는다 — 팝업 UI 클릭이 뒤쪽 3D 대상의 락온으로 새지 않게 한다.
        /// </summary>
        private bool BeginPendingOnHoverTarget()
        {
            if (_currentHoverTarget == null || _multiLockOnData == null) return false;
            if (_multiLockOnData.IsDesignationClosed) return false;
            if (_multiLockOnData.HasPending) return false;

            // 이미 큐에 있는 대상이어도 허용(한 대상에 여러 마법). 빨간(확정 락온) 마커를 켠다.
            _multiLockOnData.BeginPending(_currentHoverTarget);
            _currentHoverTarget.Highlight(true, true);
            return true;
        }

        /// <summary>팝업이 열린 상태의 취소 입력: 지정 대기를 취소하고, 큐에 없는 대상이면 마커를 예비 락온 상태로 되돌린다.</summary>
        private void CancelPendingLockOn()
        {
            if (_multiLockOnData == null) return;

            var pending = _multiLockOnData.PendingTarget;
            _multiLockOnData.CancelPending();
            RestoreHighlightAfterRelease(pending);
        }

        /// <summary>팝업이 닫힌 상태의 취소 입력: 가장 최근에 쌓인 (대상, 마법) 쌍 1개를 취소한다.</summary>
        private void CancelLastLockOnEntry()
        {
            if (_multiLockOnData == null || _multiLockOnData.EntryCount == 0) return;

            var entries = _multiLockOnData.Entries;
            var lastTarget = entries[entries.Count - 1].Target;
            _multiLockOnData.RemoveLastEntry();
            RestoreHighlightAfterRelease(lastTarget);
        }

        /// <summary>대상이 큐/대기 어디에도 남아 있지 않으면 마커를 호버 여부에 맞춰 되돌린다(남아 있으면 확정 락온 마커 유지).</summary>
        private void RestoreHighlightAfterRelease(RuntimeDataUnitGroup target)
        {
            if (target == null || _multiLockOnData == null) return;
            if (_multiLockOnData.Contains(target)) return;

            target.Highlight(target == _currentHoverTarget, false);
        }

        /// <summary>
        /// 마우스 휠 클릭: 호버 대상 락온(= 좌클릭과 동일하게 마법 지정 대기 + 팝업 오픈).
        /// 그 대상이 이미 지정 대기 중이면 지정을 취소한다(토글).
        /// </summary>
        public void ToggleLockOn()
        {
            if (_currentHoverTarget == null || _multiLockOnData == null) return;

            if (_multiLockOnData.PendingTarget == _currentHoverTarget)
            {
                CancelPendingLockOn();
                return;
            }

            BeginPendingOnHoverTarget();
        }
    }
}
