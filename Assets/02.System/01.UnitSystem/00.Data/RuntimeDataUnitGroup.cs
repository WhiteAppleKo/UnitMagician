using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace UnitSystem
{
    public class RuntimeDataUnitGroup : MonoBehaviour
    {
        [Header("Unit Group Setup")]
        [SerializeField] private List<UnitSetupData> initialUnitSetups = new()
        {
            new UnitSetupData { unitType = UnitType.Mass, initialValue = 10.0f },
            new UnitSetupData { unitType = UnitType.Volume, initialValue = 1.0f }
        };

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

            foreach (var setup in initialUnitSetups)
            {
                var runtimeData = new RuntimeDataUnit(setup.unitType, setup.initialValue, null);
                runtimeData.OnUnitChanged += HandleUnitChanged;
                UnitRuntimeDataList.Add(runtimeData);
            }
            // 게임 시작 시 후보군(풀) 리스트만 준비하고, 어플리케이터 자동 실행 로직은 폐기합니다.
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

        public RuntimeDataUnit GetMatchingUnitData(UnitType targetType)
        {
            foreach (var unitData in UnitRuntimeDataList)
            {
                if (unitData.CurrentUnit == targetType)
                {
                    return unitData;
                }
            }
            return null;
        }


    }
}
