using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace TacticalCommandSystem
{
    /// <summary>
    /// 전술 명령 및 연쇄 발동 시스템(TacticalCommand)의 VContainer LifetimeScope입니다.
    /// DLV 아키텍처 및 VContainer 가이드라인에 따라 기능 루트 바로 아래에 위치하며,
    /// 모든 시각 컴포넌트는 인터페이스 단위(As<I...>)로 바인딩됩니다.
    /// </summary>
    public class TacticalCommandLifetimeScope : LifetimeScope
    {
        [Header("Pure Data")]
        [SerializeField] private PureDataTacticalCommand pureDataTacticalCommand;

        [Header("Visual Components")]
        [SerializeField] private TacticalCommandVisualizer visualizer;
        [SerializeField] private TacticalCommandUIView uiView;

        protected override void Configure(IContainerBuilder builder)
        {
            // 1. [D] Pure Data 바인딩
            PureDataTacticalCommand pureData = pureDataTacticalCommand != null
                ? pureDataTacticalCommand
                : ScriptableObject.CreateInstance<PureDataTacticalCommand>();
            builder.RegisterInstance(pureData);

            // 2. [D] Runtime Data 바인딩 (Scoped)
            builder.Register<RuntimeDataTacticalQueue>(Lifetime.Scoped);

            // 3. [V] Visualizer 인터페이스 단위 등록 (구체 클래스 노출 금지)
            if (visualizer != null)
            {
                builder.RegisterComponent(visualizer).As<ITacticalCommandVisualizer>();
            }
            else
            {
                builder.RegisterComponentInHierarchy<TacticalCommandVisualizer>().As<ITacticalCommandVisualizer>();
            }

            // 4. [V] UI View 등록
            if (uiView != null)
            {
                builder.RegisterComponent(uiView);
            }
            else
            {
                builder.RegisterComponentInHierarchy<TacticalCommandUIView>();
            }

            // 5. 외부 의존성 Hierarchy 자동 바인딩 (InputReader, Stat, QuickSlot UI)
            builder.RegisterComponentInHierarchy<Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader>();
            builder.RegisterComponentInHierarchy<CharacterSystem.CharacterStatComponent>();
            builder.RegisterComponentInHierarchy<UnitSystem.UnitQuickSlotUIComponent>();

            // 6. [L] Logic System 생명주기 등록
            builder.RegisterEntryPoint<TacticalCommandLogicSystem>(Lifetime.Scoped);
        }
    }
}
