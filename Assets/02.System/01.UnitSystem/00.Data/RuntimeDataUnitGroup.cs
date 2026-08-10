using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace UnitSystem
{
    public class RuntimeDataUnitGroup : MonoBehaviour
    {
        [Header("Unit Group Setup")]
        [SerializeField] public UnitType supportedUnits = UnitType.None;
        [SerializeField, HideInInspector] public float bakedMassValue = 1.0f;

        public event Action<RuntimeDataUnitGroup, RuntimeDataUnit> OnUnitGroupChanged;

        public List<RuntimeDataUnit> UnitRuntimeDataList { get; private set; } = new();

        private IUnitCatalogService catalogService;

        [Inject]
        public void Construct(IUnitCatalogService catalogService)
        {
            this.catalogService = catalogService;
            InitUnits();
        }

        private void Awake()
        {
            // VContainer에서 직접 의존성 해결 시도 (미등록된 씬 오브젝트 대응)
            if (catalogService == null)
            {
                var scope = FindObjectOfType<UnitSystemLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    catalogService = scope.Container.Resolve<IUnitCatalogService>();
                }
            }
        }

        private void Start()
        {
            // Start 시점에 아직도 null이라면 다시 시도 (Awake 순서 문제 방지)
            if (catalogService == null)
            {
                var scope = FindObjectOfType<UnitSystemLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    catalogService = scope.Container.Resolve<IUnitCatalogService>();
                }
            }

            if (UnitRuntimeDataList.Count == 0)
            {
                InitUnits();
            }
        }

        public void InitUnits()
        {
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
        }

        private void CreateAndAddRuntimeUnit(UnitType type, float value)
        {
            var runtimeData = new RuntimeDataUnit(type, value, null);
            runtimeData.OnUnitChanged += HandleUnitChanged;
            UnitRuntimeDataList.Add(runtimeData);
        }

        private void OnDestroy()
        {
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
                if ((unitData.CurrentUnit & targetType) == targetType)
                {
                    return unitData;
                }
            }
            return null;
        }


    }
}
