using System.Collections.Generic;
using UnityEngine;
using Movement.RefactoredLocomotion;

namespace Movement.Visualizer
{
    /// <summary>
    /// 플레이어 자식 오브젝트 NormalLockOn에 부착되는 일반 전투 락온 컴포넌트 (단 1명의 최적 적 대상 전용)
    /// </summary>
    public class NormalCombatLockOnComponent : MonoBehaviour, ILockOnComponent
    {
        [Header("Settings")]
        [SerializeField] private float scanRadius = 25f;

        private ILocomotionVisualizer _visualizer;
        private GameObject _currentLockOnTarget;
        private bool _isLockedOn;

        private readonly Collider[] _scanColliderBuffer = new Collider[32];

        private void Awake()
        {
            if (_visualizer == null)
            {
                _visualizer = GetComponentInParent<ILocomotionVisualizer>();
            }
        }

        private void OnDisable()
        {
            // 비활성화 시 카메라 락온 및 적 하이라이트 즉시 완전 해제
            EnableLockOn(false);

            if (_currentLockOnTarget != null && _visualizer != null)
            {
                _visualizer.HighlightTarget(_currentLockOnTarget, false, false);
                _currentLockOnTarget = null;
            }
        }

        public void ToggleLockOn()
        {
            EnableLockOn(!_isLockedOn);
        }

        public void EnableLockOn(bool enable)
        {
            _isLockedOn = enable;

            Transform targetTransform = enable && _currentLockOnTarget != null 
                ? _currentLockOnTarget.transform 
                : null;

            if (_visualizer != null)
            {
                _visualizer.SetCameraLockOn(enable, targetTransform);

                if (enable && _currentLockOnTarget != null)
                {
                    _visualizer.HighlightTarget(_currentLockOnTarget, true, true);
                }
            }
        }

        private void Update()
        {
            if (_visualizer == null)
            {
                _visualizer = GetComponentInParent<ILocomotionVisualizer>();
                if (_visualizer == null) return;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(_visualizer.Position, scanRadius, _scanColliderBuffer);

            GameObject newBestTarget = null;
            float bestScore = float.MinValue;

            Vector3 camPos = _visualizer.GetCameraPosition();
            Vector3 camForward = _visualizer.GetCameraForward();

            for (int i = 0; i < hitCount; i++)
            {
                var col = _scanColliderBuffer[i];
                if (col == null) continue;

                if (_visualizer.Transform != null && col.transform.root == _visualizer.Transform.root) continue;

                // 오직 적 캐릭터만 검사
                var charStat = col.GetComponentInParent<CharacterSystem.CharacterStatComponent>();
                if (charStat == null || !charStat.IsEnemy()) continue;

                GameObject enemyObj = charStat.gameObject;

                // 기존 하이라이트 끄기 (확정 락온 대상이 아닌 경우)
                if (enemyObj != _currentLockOnTarget || !_isLockedOn)
                {
                    _visualizer.HighlightTarget(enemyObj, false, false);
                }

                Vector3 targetPos = enemyObj.transform.position + Vector3.up * 1.0f;
                Vector3 toTargetFromCam = targetPos - camPos;
                float distFromCam = toTargetFromCam.magnitude;
                if (distFromCam < 0.1f) continue;

                Vector3 dirFromCam = toTargetFromCam / distFromCam;
                float dot = Vector3.Dot(camForward, dirFromCam);

                // 카메라 전방 내적 0.25 이상
                if (dot < 0.25f) continue;

                float distFromPlayer = Vector3.Distance(_visualizer.Position, enemyObj.transform.position);

                // 가중치 점수 계산 (시야각 + 거리)
                float angleScore = dot * 60f;
                float distanceScore = (1f / Mathf.Max(distFromPlayer, 1.0f)) * 40f;
                float totalScore = angleScore + distanceScore;

                // 벽 차폐 검사
                if (Physics.Linecast(camPos, targetPos, out RaycastHit hit))
                {
                    if (hit.collider.transform.root != enemyObj.transform.root && !hit.collider.isTrigger)
                    {
                        continue;
                    }
                }

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    newBestTarget = enemyObj;
                }
            }

            if (!_isLockedOn)
            {
                _currentLockOnTarget = newBestTarget;
                if (_currentLockOnTarget != null)
                {
                    _visualizer.HighlightTarget(_currentLockOnTarget, true, false);
                }
            }
            else
            {
                // 락온 중 타겟 유효성 검증
                if (_currentLockOnTarget != null)
                {
                    _visualizer.HighlightTarget(_currentLockOnTarget, true, true);
                }
                else
                {
                    _currentLockOnTarget = newBestTarget;
                    EnableLockOn(false);
                }
            }
        }
    }
}
