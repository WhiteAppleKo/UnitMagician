using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UnitSystem
{
    public class UnitSystemLifetimeScope : LifetimeScope
    {
        [Header("Unit Catalog Settings")]
        [SerializeField] private List<PureDataUnit> initialUnits = new();

        [Header("UI Toolkit Components")]
        [SerializeField] private UnitQuickSlotUIComponent quickSlotUI;
        [SerializeField] private UnitInventoryUIComponent inventoryUI;
        [SerializeField] private UnitUnlockPopupUIComponent unlockPopupUI;
        [SerializeField] private UnitUnlockTestUIComponent unlockTestUI;
        [SerializeField] private UnitVectorGizmoUIComponent vectorGizmoUI;

        [Header("Gameplay Systems")]
        [SerializeField] private UnitCasterSystem unitCaster;
        [SerializeField] private UnitGhostPreviewComponent ghostPreview;
        [SerializeField] private MultiLockOnVisualizer multiLockOnVisualizer;

        protected override void Configure(IContainerBuilder builder)
        {
            // Data & Service Register
            builder.RegisterInstance<IReadOnlyList<PureDataUnit>>(initialUnits);
            builder.Register<UnitCatalogService>(Lifetime.Singleton).As<IUnitCatalogService>();
            builder.Register<UnitChangeService>(Lifetime.Singleton);
            builder.Register<UnitBatchCastingService>(Lifetime.Singleton).As<IUnitBatchCastingService>();
            builder.Register<RuntimeDataMultiLockOn>(Lifetime.Singleton);

            // UI Toolkit Component Register
            if (quickSlotUI != null) builder.RegisterComponent(quickSlotUI);
            if (inventoryUI != null) builder.RegisterComponent(inventoryUI);
            if (unlockPopupUI != null) builder.RegisterComponent(unlockPopupUI);
            if (unlockTestUI != null) builder.RegisterComponent(unlockTestUI);
            if (vectorGizmoUI != null) builder.RegisterComponent(vectorGizmoUI);

            // Gameplay Component Register
            var caster = unitCaster;
            if (caster == null) caster = FindAnyObjectByType<UnitCasterSystem>();
            if (caster != null) builder.RegisterComponent(caster);

            if (ghostPreview != null) builder.RegisterComponent(ghostPreview);

            var multiLockOnVis = multiLockOnVisualizer;
            if (multiLockOnVis == null) multiLockOnVis = FindAnyObjectByType<MultiLockOnVisualizer>();
            if (multiLockOnVis != null)
            {
                builder.RegisterComponent(multiLockOnVis).As<IMultiLockOnVisualizer>();
            }

            // 단위 마법 조작/락온 로직은 UnitCasterSystem(전략 패턴 Context)으로 일원화됨
        }
    }
}
