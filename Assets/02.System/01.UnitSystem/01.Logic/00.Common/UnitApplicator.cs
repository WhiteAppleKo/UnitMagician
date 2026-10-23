using System;
using System.Collections.Generic;
using PipeLine.Combat;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// PureData 서브클래스(TData) 하나를 담당하는 Applicator의 제네릭 기반입니다.
    /// - CreateInstance: data가 TData인지 확인한 뒤 타입별 인스턴스를 만든다. 타입별 구현은 캐싱 값을 시전자 영향 계산 자리(ApplyCasterInfluence)에 통과시킨다(30번).
    /// - Accumulate: 재계산 순회 중 이 항목을 누적기에 적용한다(타입별 AccumulateInstance, 31번).
    /// - ApplyVisual: 완성된 대상 상태로 타입별 시각 적용(ApplyVisualToTarget).
    /// - ApplyDamage: 데미지 계산 자리(CalculateDamage). 현재까지의 데미지를 받아 새 값을 반환하며 저장하지 않는다. 기본 구현은 입력값 그대로이고,
    ///   계산식을 넣은 Applicator만 값을 바꾼다(28번 §6-9). 최종값 저장은 UnitApplyLogicSystem이 한 번만 한다.
    /// - ReadValue: 대상 상태에서 이 속성의 현재 값 읽기(타입별 ReadStateValue).
    /// - IsInstant: 즉시 마법 여부(32번, 기본 false = 지속 마법). 즉시 마법 Applicator만 재정의한다.
    /// </summary>
    public abstract class UnitApplicator<TData> : IUnitApplicator where TData : PureDataUnit
    {
        public Type DataType => typeof(TData);

        public abstract UnitType UnitType { get; }

        public virtual bool IsInstant => false;

        public RuntimeDataUnit CreateInstance(PureDataUnit data, UnitCastSource source)
        {
            if (data is not TData typed)
            {
                Debug.LogWarning($"[{GetType().Name}] Unit data type mismatch: expected {typeof(TData).Name}, got {(data != null ? data.GetType().Name : "null")}. Instance not created.");
                return null;
            }

            return CreateInstance(typed, source ?? UnitCastSource.None);
        }

        public void Accumulate(UnitStateAccumulator acc, RuntimeDataUnit instance)
        {
            if (acc == null || instance == null) return;

            AccumulateInstance(acc, instance);
        }

        public void ApplyVisual(UnitApplyTarget target, RuntimeDataUnitTarget state, RuntimeDataUnit added)
        {
            if (target == null || state == null) return;

            ApplyVisualToTarget(target, state, added);
        }

        public int ApplyDamage(int currentDamage, RuntimeDataAttackDamage damage, RuntimeDataUnitTarget state)
        {
            if (damage == null || state == null) return currentDamage;

            return CalculateDamage(currentDamage, damage, state);
        }

        public float ReadValue(RuntimeDataUnitTarget state)
        {
            return state != null ? ReadStateValue(state) : 0f;
        }

        /// <summary>타입별 인스턴스 생성. 캐싱할 값은 반드시 ApplyCasterInfluence를 거쳐 넣는다.</summary>
        protected abstract RuntimeDataUnit CreateInstance(TData data, UnitCastSource source);

        /// <summary>
        /// 시전자 영향 계산 자리(30번 §6-6). PureData 값(pureValue)에 시전자 스탯·장착 아이템 영향을 반영한 최종 캐싱 값을 돌려준다.
        /// 현재는 영향을 주는 스탯·장비가 없으므로 기본 구현은 PureData 값을 그대로 돌려주며, 어떤 Applicator도 재정의하지 않는다.
        /// </summary>
        protected virtual float ApplyCasterInfluence(TData data, float pureValue, UnitCastSource source)
        {
            return pureValue;
        }

        /// <summary>순회 중 항목 하나를 누적기에 적용(타입별). 이 타입 인스턴스가 아니면 무시한다.</summary>
        protected abstract void AccumulateInstance(UnitStateAccumulator acc, RuntimeDataUnit instance);

        /// <summary>타입별 시각 적용(대상 Visualizer가 없으면 해당 부분은 건너뜀). 이펙트는 added가 이 타입일 때(추가)만 스폰한다(30번 §6-5).</summary>
        protected abstract void ApplyVisualToTarget(UnitApplyTarget target, RuntimeDataUnitTarget state, RuntimeDataUnit added);

        /// <summary>데미지 계산 자리(비어 있음 - 입력값 그대로 반환). 데미지를 바꾸도록 설계된 마법만 재정의해 새 값을 반환합니다(저장은 호출부).</summary>
        protected virtual int CalculateDamage(int currentDamage, RuntimeDataAttackDamage damage, RuntimeDataUnitTarget state) => currentDamage;

        /// <summary>이 속성의 현재 값을 대상 상태에서 읽는다(타입별).</summary>
        protected abstract float ReadStateValue(RuntimeDataUnitTarget state);

        /// <summary>목록에서 가장 나중 항목(T 타입)을 찾는다. 없으면 null.</summary>
        protected static T FindLatest<T>(IReadOnlyList<RuntimeDataUnit> applied) where T : RuntimeDataUnit
        {
            for (int i = applied.Count - 1; i >= 0; i--)
            {
                if (applied[i] is T typed) return typed;
            }
            return null;
        }

        /// <summary>목록에서 T 타입 항목 수(로그용).</summary>
        protected static int CountOf<T>(IReadOnlyList<RuntimeDataUnit> applied) where T : RuntimeDataUnit
        {
            int count = 0;
            for (int i = 0; i < applied.Count; i++)
            {
                if (applied[i] is T) count++;
            }
            return count;
        }
    }
}
