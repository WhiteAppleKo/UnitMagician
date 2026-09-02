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
            builder.Register<UnitChangeService>(Lifetime.Singleton).As<IUnitChangeService>().AsSelf();
            builder.Register<UnitBatchCastingService>(Lifetime.Singleton).As<IUnitBatchCastingService>();
            builder.Register<RuntimeDataMultiLockOn>(Lifetime.Singleton);
            builder.Register<RuntimeDataUnitQuickSlot>(Lifetime.Singleton);

            // Logic Systems Register
            builder.RegisterEntryPoint<UnitQuickSlotLogicSystem>(Lifetime.Singleton).AsSelf();

            // UI Toolkit Component Register
            var qsUI = quickSlotUI != null ? quickSlotUI : FindAnyObjectByType<UnitQuickSlotUIComponent>();
            if (qsUI != null) builder.RegisterComponent(qsUI);

            var invUI = inventoryUI != null ? inventoryUI : FindAnyObjectByType<UnitInventoryUIComponent>();
            if (invUI != null) builder.RegisterComponent(invUI);

            var popUI = unlockPopupUI != null ? unlockPopupUI : FindAnyObjectByType<UnitUnlockPopupUIComponent>();
            if (popUI != null) builder.RegisterComponent(popUI);

            var testUI = unlockTestUI != null ? unlockTestUI : FindAnyObjectByType<UnitUnlockTestUIComponent>();
            if (testUI != null) builder.RegisterComponent(testUI);

            var gizmoUI = vectorGizmoUI != null ? vectorGizmoUI : FindAnyObjectByType<UnitVectorGizmoUIComponent>();
            if (gizmoUI != null) builder.RegisterComponent(gizmoUI);

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
