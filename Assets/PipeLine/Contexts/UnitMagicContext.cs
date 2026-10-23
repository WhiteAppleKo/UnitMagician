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
        /// <summary>마법을 적용할 대상(적용 마법 목록 보유, 30번).</summary>
        public RuntimeDataUnitGroup TargetUnitGroup { get; set; }
        public PureDataUnit SelectedPureData { get; set; }

        /// <summary>
        /// 이번에 시전한 마법 한 번의 런타임 인스턴스(Applicator.CreateInstance로 생성, 캐싱 수치 포함 - Vector의 방향·속도도 여기 있음).
        /// ApplyUnitChangeStep이 TargetUnitGroup의 적용 목록 끝에 추가한다(30번). 즉시 마법이면 목록에 넣지 않고 한 번만 적용한다(IsInstantUnit, 32번).
        /// </summary>
        public RuntimeDataUnit UnitInstance { get; set; }

        /// <summary>UnitInstance가 즉시 마법인지(그 마법 Applicator의 IsInstant, UnitChangeService가 설정, 32번). true면 ApplyUnitChangeStep이 AddApplied 대신 ApplyInstant를 호출한다.</summary>
        public bool IsInstantUnit { get; set; }

        public int RequiredMana { get; set; }
        public int CurrentMana { get; set; }

        public bool IsSuccess { get; set; }
        public bool ManaCheckFailed { get; set; }
        public bool TargetInvalid { get; set; }
    }
}
