using UnityEngine;

namespace DestructibleSystem
{
    /// <summary>
    /// DestructibleDoorLogicSystem이 참조하는 불변 설정값([D] Pure Data)입니다.
    /// 공격자 쪽 기본 데미지(baseDamage)는 이 SO의 책임이 아니므로(공격자 PureData/CollisionDamageTrigger 쪽에 있음),
    /// 여기서는 파괴 판정에 필요한 임계값(Hp)만 보관합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataDestructibleDoor_", menuName = "DestructibleSystem/PureDataDestructibleDoor")]
    public class PureDataDestructibleDoor : ScriptableObject
    {
        [Header("Destruction Threshold")]
        [SerializeField] private int hp = 100;

        /// <summary>파괴 판정 임계값입니다. 매 충돌마다 FinalDamage가 이 값 이상이면 파괴됩니다(누적 없음).</summary>
        public int Hp => hp;
    }
}
