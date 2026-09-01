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
            _highlightOrb = transform.Find("TargetHighlight");
            if (_highlightOrb != null)
            {
                _meshRenderer = _highlightOrb.GetComponent<MeshRenderer>();
                if (_meshRenderer == null)
                {
                    Debug.LogWarning($"[SampleObjectLockOn] TargetHighlight on {gameObject.name} requires a MeshRenderer component.");
                }
            }
        }

        /// <summary>
        ///     Adds this object as a potential lock on target if the player is within range of the target.
        /// </summary>
        /// <param name="otherCollider">The collider to check.</param>
        protected virtual void OnTriggerEnter(Collider otherCollider)
        {
            SamplePlayerAnimationController playerAnimationController = otherCollider.GetComponent<SamplePlayerAnimationController>();
            if (playerAnimationController != null)
            {
                playerAnimationController.AddTargetCandidate(gameObject);
                return;
            }

            Movement.RefactoredLocomotion.ILocomotionVisualizer visualizer = otherCollider.GetComponent<Movement.RefactoredLocomotion.ILocomotionVisualizer>();
            if (visualizer != null)
            {
                visualizer.AddTargetCandidate(gameObject);
            }
        }

        /// <summary>
        ///     Removes this object as a potential lock on target if the player is within range of the target.
        /// </summary>
        /// <param name="otherCollider">The collider to check.</param>
        protected virtual void OnTriggerExit(Collider otherCollider)
        {
            SamplePlayerAnimationController playerAnimationController = otherCollider.GetComponent<SamplePlayerAnimationController>();
            if (playerAnimationController != null)
            {
                playerAnimationController.RemoveTarget(gameObject);
                Highlight(false, false);
                return;
            }

            Movement.RefactoredLocomotion.ILocomotionVisualizer visualizer = otherCollider.GetComponent<Movement.RefactoredLocomotion.ILocomotionVisualizer>();
            if (visualizer != null)
            {
                visualizer.RemoveTarget(gameObject);
                Highlight(false, false);
            }
        }

        /// <summary>
        ///     Sets the highlight status of this object, and which highlight to use.
        /// </summary>
        /// <param name="enable">Whether the highlight is enabled on this object; or not.</param>
        /// <param name="targetLock">Whether this object is locked on to; or not.</param>
        public virtual void Highlight(bool enable, bool targetLock)
        {
            Material currentMaterial = targetLock ? _targetMat : _highlightMat;

            if (_highlightOrb != null)
            {
                _highlightOrb.gameObject.SetActive(enable);
                if (enable && _meshRenderer != null && currentMaterial != null)
                {
                    _meshRenderer.material = currentMaterial;
                }
            }
        }
    }
}
