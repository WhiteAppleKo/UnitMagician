using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 마법(단위) 공통 메타데이터의 abstract 기반 PureData입니다(28번). 타입별 특화 필드와 연출 에셋은 서브클래스
    /// (PureDataMassUnit/PureDataTemperatureUnit/PureDataVectorUnit)에 있으며, 마법 하나는 서브클래스 에셋 하나입니다.
    /// UnitType은 직렬화하지 않고 서브클래스가 고정값을 반환합니다. 적용 로직(Applicator)은 참조하지 않습니다(Logic-Free).
    /// 33번: 인벤토리 항목(<see cref="CharacterSystem.IInventoryItem"/>)이며 종류는 항상 마법입니다(읽기 전용 고정값).
    /// </summary>
    public abstract class PureDataUnit : ScriptableObject, CharacterSystem.IInventoryItem
    {
        /// <summary>인벤토리 탭 분류 키. 마법 PureData는 항상 Magic입니다.</summary>
        public CharacterSystem.ItemType ItemType => CharacterSystem.ItemType.Magic;

        [Header("마법 공통 데이터")]
        [SerializeField] private string unitName;
        [SerializeField, TextArea(2, 6)] private string description = string.Empty; // 인벤토리 목록/상세에만 노출(비면 설명 요소 미생성)
        [SerializeField] private int baseCost = 10; // 규모에 따라이 코스트에 소모 배율이 곱해지거나 나눠짐
        [SerializeField] private Sprite icon;

        /// <summary>서브클래스가 고정값을 반환합니다(대상 쪽 카테고리 식별자 UnitType enum과의 연결).</summary>
        public abstract UnitType UnitType { get; }
        public string UnitName => unitName;
        public string Description => description;
        public int BaseCost => baseCost;
        public Sprite Icon => icon;
    }
}
