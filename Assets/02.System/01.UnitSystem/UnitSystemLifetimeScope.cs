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
            if (quickSlotUI != null) builder.RegisterComponent(quickSlotUI);
            else builder.RegisterComponentInHierarchy<UnitQuickSlotUIComponent>();

            if (inventoryUI != null) builder.RegisterComponent(inventoryUI);
            else builder.RegisterComponentInHierarchy<UnitInventoryUIComponent>();

            if (unlockPopupUI != null) builder.RegisterComponent(unlockPopupUI);
            else builder.RegisterComponentInHierarchy<UnitUnlockPopupUIComponent>();

            if (unlockTestUI != null) builder.RegisterComponent(unlockTestUI);
            else builder.RegisterComponentInHierarchy<UnitUnlockTestUIComponent>();

            if (vectorGizmoUI != null) builder.RegisterComponent(vectorGizmoUI);
            else builder.RegisterComponentInHierarchy<UnitVectorGizmoUIComponent>();

            // Gameplay Component Register
            if (unitCaster != null) builder.RegisterComponent(unitCaster);
            else builder.RegisterComponentInHierarchy<UnitCasterSystem>();

            if (ghostPreview != null) builder.RegisterComponent(ghostPreview);
            else builder.RegisterComponentInHierarchy<UnitGhostPreviewComponent>();

            if (multiLockOnVisualizer != null) builder.RegisterComponent(multiLockOnVisualizer).As<IMultiLockOnVisualizer>();
            else builder.RegisterComponentInHierarchy<MultiLockOnVisualizer>().As<IMultiLockOnVisualizer>();

            // Player Magic LockOn Component Register
            builder.RegisterComponentInHierarchy<MagicLockOnComponent>();
        }
    }
}
