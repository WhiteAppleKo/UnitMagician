using CharacterSystem;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 마법 인스턴스 생성 입력인 시전자 정보입니다(30번). Applicator.CreateInstance가 시전자 영향 계산 자리(ApplyCasterInfluence)에 넘깁니다.
    /// 둘 다 없을 수 있습니다(예: 시전자 스탯을 모르는 경로, 미리보기). 마나 차감 대상은 이 값과 별개입니다(IUnitChangeService.ChangeUnit의 manaPayer).
    /// </summary>
    public sealed class UnitCastSource
    {
        /// <summary>시전자 정보가 없는 경우.</summary>
        public static readonly UnitCastSource None = new UnitCastSource(null, null);

        /// <summary>시전자(없을 수 있음).</summary>
        public GameObject Caster { get; }

        /// <summary>시전자 스탯(없을 수 있음).</summary>
        public RuntimeStatData CasterStat { get; }

        public UnitCastSource(GameObject caster, RuntimeStatData casterStat)
        {
            Caster = caster;
            CasterStat = casterStat;
        }
    }
}
