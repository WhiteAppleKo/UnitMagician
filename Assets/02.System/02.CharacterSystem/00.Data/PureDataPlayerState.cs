using UnityEngine;

namespace CharacterSystem
{
    public enum PlayerStateType
    {
        Normal,
        Casting,
        TimeSlow
    }

    [CreateAssetMenu(fileName = "PureDataPlayerState_", menuName = "CharacterSystem/PureDataPlayerState")]
    public class PureDataPlayerState : ScriptableObject
    {
        [Header("State Settings")]
        [SerializeField] private PlayerStateType defaultState = PlayerStateType.Normal;
        [SerializeField] private float castingMoveSpeedMultiplier = 0.5f;
        [SerializeField] private float castingDuration = 0.5f;
        [SerializeField] private bool canMoveDuringCasting = true;
        [SerializeField] private bool canMoveDuringSlow = true;

        public PlayerStateType DefaultState => defaultState;
        public float CastingMoveSpeedMultiplier => castingMoveSpeedMultiplier;
        public float CastingDuration => castingDuration;
        public bool CanMoveDuringCasting => canMoveDuringCasting;
        public bool CanMoveDuringSlow => canMoveDuringSlow;
    }
}
