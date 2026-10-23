using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 마법 대상의 원본 PureData입니다(31번, 불변). 이 대상이 받을 수 있는 마법 속성과, 대상 RuntimeData(RuntimeDataUnitTarget)가
    /// 초기화·재계산 때마다 되돌아가는 원본 값(온도)을 가집니다.
    /// 원본 값은 컴포넌트 필드나 코드 상수에 두지 않고 이 에셋에만 둡니다.
    /// 질량은 원본 값이 아니라 스케일 배율(초기 1)로 다루므로 이 에셋에 두지 않습니다. 배율의 베이스는 프리팹 원본 크기이며 Visualizer가 보관합니다(32번).
    /// 적용 로직은 참조하지 않습니다(Logic-Free).
    /// </summary>
    [CreateAssetMenu(fileName = "PureDataUnitTarget_", menuName = "UnitSystem/PureData/UnitTarget")]
    public class PureDataUnitTarget : ScriptableObject
    {
        [Header("마법 대상 원본 데이터")]
        [Tooltip("이 대상이 받을 수 있는 마법 속성(Flags).")]
        [SerializeField] private UnitType supportedUnits = UnitType.None;

        [Tooltip("원본 온도(0 = 중립).")]
        [SerializeField] private float baseTemperature = 0f;

        public UnitType SupportedUnits => supportedUnits;
        public float BaseTemperature => baseTemperature;

        /// <summary>이 대상이 그 마법 속성을 받을 수 있는지(None은 항상 false).</summary>
        public bool HasUnit(UnitType unitType)
        {
            return unitType != UnitType.None && (supportedUnits & unitType) == unitType;
        }
    }
}
