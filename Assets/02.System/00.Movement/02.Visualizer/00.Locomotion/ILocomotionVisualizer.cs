using UnityEngine;

namespace Movement.RefactoredLocomotion
{
    public interface ILocomotionVisualizer
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        Vector3 Forward { get; }
        Vector3 Right { get; }

        // Physical actions
        void Move(Vector3 velocity);
        void SetRotation(Quaternion rotation);
        void SetCapsuleSize(float height, float centerY);

        // Ground & Physics queries
        bool CheckGrounded(float offset, LayerMask layerMask);
        float CheckGroundIncline(LayerMask layerMask);
        bool CheckCeilingHeight(float minimumStandingHeight, LayerMask layerMask);
        bool IsControllerGrounded { get; }

        // Camera queries & commands
        Vector3 GetCameraForwardZeroedY();
        Vector3 GetCameraRightZeroedY();
        Vector3 GetCameraPosition();
        Vector3 GetCameraForward();
        float GetCameraTiltX();
        void SetCameraLockOn(bool enable, Transform targetLockOnPos);
        void SetLockOnPos(Vector3 position);
        event System.Action<bool, Transform> OnCameraLockOnChanged;

        // Target highlight & registration commands
        void HighlightTarget(GameObject target, bool enable, bool isLockedOn);
        void AddTargetCandidate(GameObject target);
        void RemoveTarget(GameObject target);
        event System.Action<GameObject> OnTargetCandidateAdded;
        event System.Action<GameObject> OnTargetCandidateRemoved;

        // Animator sync
        void UpdateAnimator(RuntimeDataLocomotion data);
        void SetJumpingAnim(bool isJumping);
        void SetStartingAnim(bool isStarting);
        void SetLocomotionStartDirectionAnim(float direction);
    }
}
