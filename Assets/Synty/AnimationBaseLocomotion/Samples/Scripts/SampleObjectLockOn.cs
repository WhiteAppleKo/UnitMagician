// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// Sample scripts are included only as examples and are not intended as production-ready.

using UnityEngine;

namespace Synty.AnimationBaseLocomotion.Samples
{
    public class SampleObjectLockOn : MonoBehaviour
    {
        public Material _highlightMat;
        public Material _targetMat;
        private Transform _highlightOrb;

        private MeshRenderer _meshRenderer;

        /// <inheritdoc cref="Start" />
        protected virtual void Start()
        {
            EnsureHighlightTarget();
        }

        private void EnsureHighlightTarget()
        {
            if (_highlightOrb != null) return;

            // 직계 자식뿐만 아니라 하위 모든 계층에서 TargetHighlight를 안전하게 탐색
            var allChildren = GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child.name.Equals("TargetHighlight", System.StringComparison.OrdinalIgnoreCase))
                {
                    _highlightOrb = child;
                    _meshRenderer = child.GetComponent<MeshRenderer>();
                    break;
                }
            }
        }

        /// <summary>
        ///     Sets the highlight status of this object, and which highlight to use.
        /// </summary>
        /// <param name="enable">Whether the highlight is enabled on this object; or not.</param>
        /// <param name="targetLock">Whether this object is locked on to; or not.</param>
        public virtual void Highlight(bool enable, bool targetLock)
        {
            EnsureHighlightTarget();

            if (_highlightOrb != null)
            {
                _highlightOrb.gameObject.SetActive(enable);

                // 인스펙터에 머티리얼이 할당된 경우에만 교체하고, 비어있을 때는 원본 머티리얼 유지
                Material currentMaterial = targetLock ? _targetMat : _highlightMat;
                if (enable && _meshRenderer != null && currentMaterial != null)
                {
                    _meshRenderer.material = currentMaterial;
                }
            }
        }
    }
}
