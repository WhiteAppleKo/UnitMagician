using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PickupSystem
{
    /// <summary>
    /// 13.PickupSystem(필드 아이템 습득)의 VContainer LifetimeScope입니다.
    /// UnitSystemLifetimeScope의 자식 스코프이므로(UnitSystem -> Locomotion -> Character 체인),
    /// IInventoryService(Character, 33번)와 IInputContextManager/InputReader(Character 계열)가
    /// 부모 컨테이너에서 직접 해석되어 별도의 크로스 스코프 사후 배선이 필요하지 않습니다.
    /// </summary>
    public class PickupSystemLifetimeScope : LifetimeScope
    {
        [SerializeField] private PickupDetectorComponent detector;
        [SerializeField] private PickupPromptUIComponent promptUI;

        protected override void Awake()
        {
            parentReference = ParentReference.Create<UnitSystem.UnitSystemLifetimeScope>();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<RuntimeDataPickupFocus>(Lifetime.Singleton);
            builder.RegisterEntryPoint<PickupLogicSystem>(Lifetime.Singleton);

            builder.RegisterComponent(detector);
            builder.RegisterComponent(promptUI);
        }
    }
}
