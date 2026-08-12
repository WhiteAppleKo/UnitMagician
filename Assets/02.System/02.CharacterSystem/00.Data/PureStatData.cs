using System.Collections.Generic;
using UnityEngine;
using UnitSystem;

namespace CharacterSystem
{
    [CreateAssetMenu(fileName = "PureStatData_", menuName = "CharacterSystem/PureStatData")]
    public class PureStatData : ScriptableObject
    {
        [Header("Character Base Stats")]
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int maxMP = 100;
        [SerializeField] private float baseMoveSpeed = 5.0f;
        [SerializeField] private List<PureDataUnit> defaultUnitMagics = new List<PureDataUnit>();

        public int MaxHP => maxHP;
        public int MaxMP => maxMP;
        public float BaseMoveSpeed => baseMoveSpeed;
        public IReadOnlyList<PureDataUnit> DefaultUnitMagics => defaultUnitMagics;
    }
}
