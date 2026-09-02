using System;
using System.Collections.Generic;
using UnityEngine;

namespace Movement.RefactoredLocomotion
{
    public class NormalCombatLockOnStrategy : ILockOnStrategy
    {
        private readonly RuntimeDataLocomotion _runtimeData;
        private readonly ILocomotionVisualizer _visualizer;
        private readonly PureDataLocomotion _pureData;

        private const float LOCK_ON_SCAN_RADIUS = 25f;
        private readonly Collider[] _scanColliderBuffer = new Collider[32];

        public NormalCombatLockOnStrategy(
            RuntimeDataLocomotion runtimeData,
            ILocomotionVisualizer visualizer,
            PureDataLocomotion pureData)
        {
            _runtimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
            _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
            _pureData = pureData;
        }

        public void Enter()
        {
        }

        public void Exit()
        {
            EnableLockOn(false);
            if (_runtimeData.CurrentLockOnTarget != null)
            {
                _visualizer.HighlightTarget(_runtimeData.CurrentLockOnTarget, false, false);
                _runtimeData.CurrentLockOnTarget = null;
            }
            foreach (var cand in _runtimeData.TargetCandidates)
            {
                if (cand != null) _visualizer.HighlightTarget(cand, false, false);
            }
            _runtimeData.TargetCandidates.Clear();
        }

        public void ToggleLockOn()
        {
            EnableLockOn(!_runtimeData.IsLockedOn);
        }

        public void EnableLockOn(bool enable)
        {
            _runtimeData.IsLockedOn = enable;
            _runtimeData.IsStrafing = !_runtimeData.IsSprinting && (enable || _runtimeData.IsAiming || (_pureData != null && _pureData.AlwaysStrafe));

            Transform targetTransform = enable && _runtimeData.CurrentLockOnTarget != null 
                ? _runtimeData.CurrentLockOnTarget.transform 
                : null;

            _visualizer.SetCameraLockOn(enable, targetTransform);

            if (enable && _runtimeData.CurrentLockOnTarget != null)
            {
                _visualizer.HighlightTarget(_runtimeData.CurrentLockOnTarget, true, true);
            }
        }

        public void Update()
        {
            // 매 프레임 시간 정지 검사 0개! 순수 적 캐릭터 타겟팅만 수행
            int hitCount = Physics.OverlapSphereNonAlloc(_visualizer.Position, LOCK_ON_SCAN_RADIUS, _scanColliderBuffer);

            _runtimeData.TargetCandidates.Clear();
            var candidateSet = new HashSet<GameObject>();

            for (int i = 0; i < hitCount; i++)
            {
                var col = _scanColliderBuffer[i];
                if (col == null) continue;

                if (_visualizer.Transform != null && col.transform.root == _visualizer.Transform.root) continue;

                var charStat = col.GetComponentInParent<CharacterSystem.CharacterStatComponent>();
                if (charStat != null && charStat.IsEnemy())
                {
                    if (candidateSet.Add(charStat.gameObject))
                    {
                        _runtimeData.TargetCandidates.Add(charStat.gameObject);
                    }
                }
            }

            var candidates = _runtimeData.TargetCandidates;
            GameObject newBestTarget = null;
            float bestScore = float.MinValue;

            Vector3 camPos = _visualizer.GetCameraPosition();
            Vector3 camForward = _visualizer.GetCameraForward();

            foreach (var target in candidates)
            {
                if (target == null) continue;

                if (target != _runtimeData.CurrentLockOnTarget || !_runtimeData.IsLockedOn)
                {
                    _visualizer.HighlightTarget(target, false, false);
                }

                Vector3 targetPos = target.transform.position + Vector3.up * 1.0f;
                Vector3 toTargetFromCam = targetPos - camPos;
                float distFromCam = toTargetFromCam.magnitude;
                if (distFromCam < 0.1f) continue;

                Vector3 dirFromCam = toTargetFromCam / distFromCam;
                float dot = Vector3.Dot(camForward, dirFromCam);

                if (dot < 0.25f) continue;

                float distFromPlayer = Vector3.Distance(_visualizer.Position, target.transform.position);

                float angleScore = dot * 60f;
                float distanceScore = (1f / Mathf.Max(distFromPlayer, 1.0f)) * 40f;
                float totalScore = angleScore + distanceScore;

                if (Physics.Linecast(camPos, targetPos, out RaycastHit hit))
                {
                    if (hit.collider.transform.root != target.transform.root && !hit.collider.isTrigger)
                    {
                        continue;
                    }
                }

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    newBestTarget = target;
                }
            }

            if (!_runtimeData.IsLockedOn)
            {
                _runtimeData.CurrentLockOnTarget = newBestTarget;
                if (_runtimeData.CurrentLockOnTarget != null)
                {
                    _visualizer.HighlightTarget(_runtimeData.CurrentLockOnTarget, true, false);
                }
            }
            else
            {
                if (_runtimeData.CurrentLockOnTarget != null && candidates.Contains(_runtimeData.CurrentLockOnTarget))
                {
                    _visualizer.HighlightTarget(_runtimeData.CurrentLockOnTarget, true, true);
                }
                else
                {
                    _runtimeData.CurrentLockOnTarget = newBestTarget;
                    EnableLockOn(false);
                }
            }
        }

        public void AddTargetCandidate(GameObject target)
        {
            if (target != null && !_runtimeData.TargetCandidates.Contains(target))
            {
                _runtimeData.TargetCandidates.Add(target);
            }
        }

        public void RemoveTarget(GameObject target)
        {
            if (target != null && _runtimeData.TargetCandidates.Contains(target))
            {
                _runtimeData.TargetCandidates.Remove(target);
            }
        }

        public void Dispose()
        {
            Exit();
        }
    }
}
