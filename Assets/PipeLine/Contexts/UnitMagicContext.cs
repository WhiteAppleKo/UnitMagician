using CharacterSystem;
using UnitSystem;
using UnityEngine;

namespace PipeLine.Contexts
{
    /// <summary>
    /// 단위 마법 파이프라인 연산에 활용되는 Pure Data Context 객체입니다.
    /// </summary>
    public class UnitMagicContext
    {
        public GameObject Caster { get; set; }
        public RuntimeStatData CasterStatData { get; set; }
        public GameObject TargetObject { get; set; }
        public RuntimeDataUnit TargetRuntimeData { get; set; }
        public PureDataUnit SelectedPureData { get; set; }
        
        public int RequiredMana { get; set; }
        public int CurrentMana { get; set; }

        public Vector3 VectorDirection { get; set; }
        public float VectorSpeed { get; set; }
        public float NewValue { get; set; }

        public bool IsSuccess { get; set; }
        public bool ManaCheckFailed { get; set; }
        public bool TargetInvalid { get; set; }
    }
}
