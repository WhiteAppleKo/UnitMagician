using System;
using System.Collections.Generic;
using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;
using TimeSlowFilterSystem;
using Pooling;

namespace UnitSystem
{
    /// <summary>
    /// 오브젝트에 부착되는 단위 데이터 그룹 및 락온 타겟 컴포넌트입니다.
    /// Mass, Volume, Vector 등 다중 단위 런타임 데이터를 관리하고 상태 변경 이벤트를 발행합니다.
    /// 풀링되는 오브젝트(15-B 아군 투사체)에 붙어 있으면 반환 시 "대상 소실"을 알리고(OnAnyTargetRetired),
    /// 재사용 직전에 단위 값/락온 마커를 초기값으로 되돌립니다(IPoolReusable).
    /// </summary>
    public class RuntimeDataUnitGroup : SampleObjectLockOn, IPoolReusable
    {
        [Header("Unit Group Setup")]
        [SerializeField] public UnitType supportedUnits = UnitType.None;
        [SerializeField] private bool isTargetable = true;
        [SerializeField, HideInInspector] public float bakedMassValue = 1.0f;

        public UnitType SupportedUnits => supportedUnits;
        public bool IsTargetable => isTargetable;
        public float BakedMassValue
        {
            get => bakedMassValue;
            set => bakedMassValue = value;
        }

        public event Action<RuntimeDataUnitGroup, RuntimeDataUnit> OnUnitGroupChanged;
        public event Action<bool> OnTargetableChanged;

        /// <summary>
        /// 풀로 반환되는 대상(IPoolReusable.OnReturnedToPool)이 발행하는 소실 통지(15-B). 락온 큐/지정 대기를 관리하는 쪽
        /// (LockOnTargetLossLogicSystem)이 구독해, 반환과 재사용이 같은 프레임에 일어나도 주기 점검을 기다리지 않고
        /// 해당 대상의 엔트리를 즉시 제거한다. 투사체(07.NpcSystem)가 UnitSystem의 락온 데이터를 직접 알지 않도록 하는 연결 고리다.
        /// </summary>
        public static event Action<RuntimeDataUnitGroup> OnAnyTargetRetired;

        private bool initialIsTargetable;

        public List<RuntimeDataUnit> UnitRuntimeDataList { get; private set; } = new();
        public IReadOnlyList<RuntimeDataUnit> Units => UnitRuntimeDataList;

        /// <summary>
        /// 마법 대상으로 유효한지의 단일 판정 기준(15-A): 파괴되지 않았고(Unity null 아님) 계층에서 활성 상태일 것.
        /// 파괴됨과 비활성/풀 반환됨을 모두 "소실"로 취급한다.
        /// </summary>
        public static bool IsValidTarget(RuntimeDataUnitGroup group)
        {
            return group != null && group.gameObject.activeInHierarchy;
        }

        private void Awake()
        {
            initialIsTargetable = isTargetable;

            if (UnitRuntimeDataList.Count == 0)
            {
                InitUnits();
            }
        }

        /// <summary>풀 반환 순간(아직 활성): 락온 마커를 끄고 락온 큐/지정 대기에 소실을 즉시 통지한다.</summary>
        public void OnReturnedToPool()
        {
            Highlight(false, false);
            OnAnyTargetRetired?.Invoke(this);
        }

        /// <summary>재사용 직전: 온도/질량/부피/방향 런타임 값을 초기값으로 되돌리고 타게팅 가능 여부/락온 마커도 초기화한다.</summary>
        public void ResetForReuse()
        {
            InitUnits();
            isTargetable = initialIsTargetable;
            Highlight(false, false);
        }

        private void OnEnable()
        {
            TargetStencilService.RegisterTargetStatic(this);
        }

        private void OnDisable()
        {
            TargetStencilService.UnregisterTargetStatic(this);
        }

        public void InitUnits()
        {
            UnbindUnitEvents();
            UnitRuntimeDataList.Clear();

            // 플래그 체크 후 개별 인스턴스 생성
            if (HasUnit(UnitType.Mass))
            {
                CreateAndAddRuntimeUnit(UnitType.Mass, bakedMassValue);
            }
            if (HasUnit(UnitType.Volume))
            {
                CreateAndAddRuntimeUnit(UnitType.Volume, 1.0f);
            }
            if (HasUnit(UnitType.Vector))
            {
                CreateAndAddRuntimeUnit(UnitType.Vector, 1.0f);
            }
            if (HasUnit(UnitType.Temperature))
            {
                CreateAndAddRuntimeUnit(UnitType.Temperature, 0f); // 0 = 중립 온도
            }
        }

        private void CreateAndAddRuntimeUnit(UnitType type, float value)
        {
            var runtimeData = new RuntimeDataUnit(type, value, null);
            runtimeData.OnUnitChanged += HandleUnitChanged;
            UnitRuntimeDataList.Add(runtimeData);
        }

        private void OnDestroy()
        {
            UnbindUnitEvents();
        }

        private void UnbindUnitEvents()
        {
            if (UnitRuntimeDataList == null) return;

            foreach (var data in UnitRuntimeDataList)
            {
                if (data != null) data.OnUnitChanged -= HandleUnitChanged;
            }
        }

        private void HandleUnitChanged(RuntimeDataUnit data)
        {
            OnUnitGroupChanged?.Invoke(this, data);
        }

        public bool HasUnit(UnitType targetType)
        {
            return (supportedUnits & targetType) == targetType && targetType != UnitType.None;
        }

        public RuntimeDataUnit GetMatchingUnitData(UnitType targetType)
        {
            if (!HasUnit(targetType)) return null;

            foreach (var unitData in UnitRuntimeDataList)
            {
                if (unitData != null && (unitData.CurrentUnit & targetType) == targetType)
                {
                    return unitData;
                }
            }
            return null;
        }

        public void SetTargetable(bool targetable)
        {
            if (isTargetable == targetable) return;

            isTargetable = targetable;
            if (!isTargetable)
            {
                Highlight(false, false);
            }

            OnTargetableChanged?.Invoke(isTargetable);
        }

        public override void Highlight(bool enable, bool targetLock)
        {
            if (!isTargetable)
            {
                base.Highlight(false, false);
                return;
            }

            base.Highlight(enable, targetLock);
        }
    }
}
