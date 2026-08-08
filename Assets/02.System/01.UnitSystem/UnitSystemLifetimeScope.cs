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

        [Header("Gameplay Systems")]
        [SerializeField] private UnitCasterSystem unitCaster;

        protected override void Configure(IContainerBuilder builder)
        {
            // Data & Service Register
            builder.RegisterInstance<IReadOnlyList<PureDataUnit>>(initialUnits);
            builder.Register<UnitCatalogService>(Lifetime.Singleton).As<IUnitCatalogService>();
            builder.Register<UnitChangeService>(Lifetime.Singleton);

            // UI Toolkit Component Register
            if (quickSlotUI != null) builder.RegisterComponent(quickSlotUI);
            if (inventoryUI != null) builder.RegisterComponent(inventoryUI);
            if (unlockPopupUI != null) builder.RegisterComponent(unlockPopupUI);
            if (unlockTestUI != null) builder.RegisterComponent(unlockTestUI);

            // Gameplay Component Register
            if (unitCaster != null) builder.RegisterComponent(unitCaster);
        }
    }
}
