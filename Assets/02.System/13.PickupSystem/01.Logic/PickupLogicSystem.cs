using System;
using VContainer;
using VContainer.Unity;
using Common.InputSystem;
using CharacterSystem;

namespace PickupSystem
{
    /// <summary>
    /// [L] 줍기 입력 처리 로직(순수 C#). InputReader.onInteractPressed를 받아
    /// (1) 입력 컨텍스트가 Player일 때만(Tactical/UI/Event에서는 무시),
    /// (2) RuntimeDataPickupFocus.CurrentTarget이 있고 아직 소유하지 않은 마법이면
    /// 플레이어 인벤토리에 그 PureData를 넣고(IInventoryService.TryAcquire, 33번) 성공하면 target.Consume()을 호출합니다.
    /// 줍기 가능 여부 판정(IsPickupAllowed: Player 컨텍스트일 때만)은 이 클래스 한 곳에만 있고, 입력 처리와
    /// 포커스 결정(ResolveFocus: DetectedTarget을 컨텍스트로 걸러 CurrentTarget에 반영)이 같은 판정을 공유합니다.
    /// 감지 대상 변경(OnDetectedChanged)과 컨텍스트 변경(OnContextChanged) 때마다 포커스를 다시 결정하므로
    /// 프롬프트는 줍기 불가 컨텍스트에서 숨겨지고 Player 복귀 시 즉시 다시 나타납니다.
    /// 습득 팝업("Unit Acquired: ...")과 인벤토리 마법 탭 갱신은 RuntimeDataInventory.OnItemAdded 구독자가 처리하며,
    /// 자동 장착(TryEquipUnit)은 하지 않습니다(습득=소유, 장착은 인벤토리에서 별개).
    /// </summary>
    public class PickupLogicSystem : IInitializable, IDisposable
    {
        private readonly RuntimeDataPickupFocus focus;
        private readonly IInventoryService inventoryService;
        private readonly IInputContextManager contextManager;
        private readonly Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader;

        [Inject]
        public PickupLogicSystem(
            RuntimeDataPickupFocus focus,
            IInventoryService inventoryService,
            IInputContextManager contextManager,
            Synty.AnimationBaseLocomotion.Samples.InputSystem.InputReader inputReader)
        {
            this.focus = focus;
            this.inventoryService = inventoryService;
            this.contextManager = contextManager;
            this.inputReader = inputReader;
        }

        public void Initialize()
        {
            inputReader.onInteractPressed -= HandleInteractPressed;
            inputReader.onInteractPressed += HandleInteractPressed;

            focus.OnDetectedChanged -= HandleDetectedChanged;
            focus.OnDetectedChanged += HandleDetectedChanged;
            contextManager.OnContextChanged -= HandleContextChanged;
            contextManager.OnContextChanged += HandleContextChanged;

            ResolveFocus();
        }

        public void Dispose()
        {
            if (inputReader != null)
            {
                inputReader.onInteractPressed -= HandleInteractPressed;
            }

            focus.OnDetectedChanged -= HandleDetectedChanged;
            contextManager.OnContextChanged -= HandleContextChanged;
        }

        private void HandleDetectedChanged(IPickupTarget detected)
        {
            ResolveFocus();
        }

        private void HandleContextChanged(IInputContext context)
        {
            ResolveFocus();
        }

        // 줍기 가능 여부의 유일한 정의: Player 컨텍스트일 때만(Tactical/UI/Event에서는 불가).
        // 입력 처리(HandleInteractPressed)와 포커스/프롬프트 결정(ResolveFocus)이 모두 이 판정을 공유한다.
        private bool IsPickupAllowed()
        {
            var currentContext = contextManager.CurrentContext;
            return currentContext != null && currentContext.ContextType == InputContextType.Player;
        }

        private void ResolveFocus()
        {
            focus.SetTarget(IsPickupAllowed() ? focus.DetectedTarget : null);
        }

        private void HandleInteractPressed()
        {
            if (!IsPickupAllowed()) return;

            var target = focus.CurrentTarget;
            if (target == null) return;

            var unit = target.GrantedUnit;
            if (unit == null) return;

            // 이미 보유한 마법(같은 PureData 에셋)이면 TryAcquire가 false — 줍지 않는다(기존 규칙).
            if (!inventoryService.TryAcquire(unit)) return;

            target.Consume();
            focus.SetDetectedTarget(null);
        }
    }
}
