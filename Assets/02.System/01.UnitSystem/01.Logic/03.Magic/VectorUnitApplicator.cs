using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 벡터(Vector) 마법 적용 로직입니다(범위 밖 보존용). 즉시 마법(IsInstant, 32번)이라 적용 목록에 남지 않습니다.
    /// - 인스턴스: 공용 CreateInstance(data, source)는 방향 forward, 속도 0(시전자 영향 계산 자리를 거침)으로 만든다.
    ///   방향·속도를 정하는 입력 경로는 지금 없다(나중에 다시 추가).
    /// - 적용: 시전 시 RuntimeDataUnitTarget.ApplyInstant → UnitApplyLogicSystem이 이번 인스턴스(added)를 실어 ApplyVisual을 한 번 호출하면
    ///   그 인스턴스의 방향·속도로 ApplyVelocity를 한 번 호출하고 끝난다. 해제·재계산·풀 재사용 때 되돌리지 않는다.
    /// - 순회 적용(Accumulate)·값 읽기(ReadValue)는 즉시 마법이라 의미가 없어 아무것도 하지 않거나 0을 반환한다. 데미지 계산 자리는 비어 있다.
    /// 기존의 "방향이 zero면 대상 정면 방향" 대체는 Logic에서 할 수 없어 zero 방향을 그대로 넘깁니다(28번 §6-12 참고).
    /// </summary>
    public class VectorUnitApplicator : UnitApplicator<PureDataVectorUnit>
    {
        public override UnitType UnitType => UnitType.Vector;

        public override bool IsInstant => true;

        protected override RuntimeDataUnit CreateInstance(PureDataVectorUnit data, UnitCastSource source)
        {
            return new RuntimeDataVectorUnit(data, Vector3.forward, ApplyCasterInfluence(data, 0f, source));
        }

        protected override void AccumulateInstance(UnitStateAccumulator acc, RuntimeDataUnit instance)
        {
            // 즉시 마법: 목록 순회 대상이 아니다.
        }

        protected override void ApplyVisualToTarget(UnitApplyTarget target, RuntimeDataUnitTarget state, RuntimeDataUnit added)
        {
            if (added is not RuntimeDataVectorUnit vector) return;

            if (target.Visualizer != null)
            {
                target.Visualizer.ApplyVelocity(vector.Direction, vector.Speed);
            }

            Debug.Log($"[VectorUnitApplicator] Applied instant Vector ({vector.Source.name}). Direction: {vector.Direction}, Speed: {vector.Speed}");
        }

        protected override float ReadStateValue(RuntimeDataUnitTarget state) => 0f;
    }
}
