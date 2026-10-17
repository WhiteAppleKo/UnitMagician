using System;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 락온 대상 하나에 대한 미리보기(고스트) 오브젝트입니다. LockOnPreviewVisualizer가 생성/관리하는 수동적 수행자입니다.
    /// 대상은 부모/자식으로 묶지 않고(자식 Renderer를 순회하는 다른 시스템 — 아웃라인/머티리얼 교체 — 에 섞이지 않도록)
    /// 매 LateUpdate마다 대상의 위치/회전을 그대로 복사해 따라다닙니다. 시간(deltaTime)을 쓰지 않으므로 timeScale이 0이거나
    /// 매우 낮아도(이벤트 접근 구간 0.03 등) 대상과 어긋나지 않습니다. 대상이 파괴/비활성화되면 스스로 사라집니다.
    /// </summary>
    public class LockOnPreviewGhost : MonoBehaviour
    {
        private RuntimeDataUnitGroup target;
        private Transform targetTransform;
        private Action<LockOnPreviewGhost> onDestroyed;

        public RuntimeDataUnitGroup Target => target;

        public void Initialize(RuntimeDataUnitGroup targetGroup, Action<LockOnPreviewGhost> destroyedCallback)
        {
            target = targetGroup;
            targetTransform = targetGroup != null ? targetGroup.transform : null;
            onDestroyed = destroyedCallback;
            SyncToTarget();
        }

        private void LateUpdate()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                // 대상이 파괴/비활성화됨 → 잔상 없이 정리
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            SyncToTarget();
        }

        private void SyncToTarget()
        {
            if (targetTransform == null) return;

            transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
        }

        private void OnDestroy()
        {
            var callback = onDestroyed;
            onDestroyed = null;
            callback?.Invoke(this);
        }
    }
}
