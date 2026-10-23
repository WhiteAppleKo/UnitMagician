using System;

namespace UnitSystem
{
    /// <summary>
    /// 마법 타입별 Applicator 조회([L] 호출형 순수 로직, 30번)입니다. 시전(인스턴스 생성)·적용(재계산)·미리보기가 같은 조회를 씁니다.
    /// </summary>
    public interface IUnitApplicatorRegistry
    {
        /// <summary>PureData 서브클래스 타입으로 조회(인스턴스 생성용). 없으면 null.</summary>
        IUnitApplicator GetByDataType(Type dataType);

        /// <summary>대상 속성으로 조회(목록 재계산용). 없으면 null.</summary>
        IUnitApplicator GetByUnitType(UnitType unitType);
    }
}
