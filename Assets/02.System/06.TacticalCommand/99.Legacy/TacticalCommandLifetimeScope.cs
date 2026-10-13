// [99.Legacy / 15-A] 은퇴한 옛 전술 명령(시간정지 예약/시전) 시스템. 시간정지 마법 지정/발동은 01.UnitSystem의 RuntimeDataMultiLockOn 큐 시스템으로 일원화되었다.
// 컴파일만 유지되며 어떤 LifetimeScope/씬에도 등록되지 않는다. 새 코드에서 이 폴더의 타입을 참조하지 말 것(호버/순서 인디케이터/궤적선 연출 참고용 보관).
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

        protected override void Awake()
        {
            parentReference = ParentReference.Create<UnitSystem.UnitSystemLifetimeScope>();
            base.Awake();
        }

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

            // 5. 외부 의존성 Hierarchy 자동 바인딩 (InputReader, Stat)
            builder.RegisterComponentInHierarchy<Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader>();
            builder.RegisterComponentInHierarchy<CharacterSystem.CharacterStatComponent>();

            // 6. [L] Logic System 생명주기 등록
            builder.RegisterEntryPoint<TacticalCommandLogicSystem>(Lifetime.Scoped);
        }
    }
}
