using UnityEngine;

namespace InteractionSystem.Data
{
    /// <summary>
    /// 충돌 데미지 오브젝트(CollisionDamageTrigger)의 기본 데미지 원본([PureData], 28번 §6-10)입니다.
    /// 발사 값이 없는 오브젝트(씬의 Sphere 등)는 이 값으로, 풀링 투사체는 발사 값으로 RuntimeDataAttackDamage를 초기화합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataAttackDamage_", menuName = "InteractionSystem/PureData/AttackDamage")]
    public class PureDataAttackDamage : ScriptableObject
    {
        [SerializeField] private int baseDamage = 10;

        public int BaseDamage => baseDamage;
    }
}
