using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 벡터(Vector) 마법 한 번의 런타임 인스턴스입니다(30번, 범위 밖 보존용). 즉시 마법이라 대상의 적용 목록에 남지 않고, 시전 시 이 값으로 속도를 한 번 바꿉니다(32번).
    /// Direction·Speed는 시전 시 정해지며(지금은 입력 경로가 없어 공용 경로의 forward·0), Speed는 시전자 영향(계산 자리, VectorUnitApplicator.CreateInstance)을 거칩니다.
    /// 생성 후 바뀌지 않습니다.
    /// </summary>
    public sealed class RuntimeDataVectorUnit : RuntimeDataUnit
    {
        /// <summary>원본 마법의 Vector 데이터. Source와 같은 에셋입니다.</summary>
        public PureDataVectorUnit Data { get; }

        /// <summary>정규화된 적용 방향.</summary>
        public Vector3 Direction { get; }

        public float Speed { get; }

        public RuntimeDataVectorUnit(PureDataVectorUnit data, Vector3 direction, float speed) : base(data)
        {
            Data = data;
            Direction = direction.normalized;
            Speed = speed;
        }
    }
}
