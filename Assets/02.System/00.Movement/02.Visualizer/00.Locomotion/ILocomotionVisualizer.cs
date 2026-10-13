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

        // Roll invincibility (19-B)
        /// <summary>
        /// true이면 플레이어 루트와 모든 자식 콜라이더를 IgnoreAttack 레이어로 옮겨 모든 공격 판정체를 통과시키고,
        /// false이면 그 시점 진영(CurrentFaction)에 맞는 몸체 레이어로 되돌린다.
        /// </summary>
        void SetAttackIgnored(bool ignored);

        /// <summary>이 비주얼라이저가 비활성/파괴될 때 발행된다. 구르기 같은 진행 중 상태를 로직이 중단할 수 있게 한다.</summary>
        event System.Action OnVisualizerDisabled;

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
