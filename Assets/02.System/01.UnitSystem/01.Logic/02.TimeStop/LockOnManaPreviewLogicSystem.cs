using System;
using CharacterSystem;
using VContainer.Unity;

namespace UnitSystem
{
    /// <summary>
    /// 락온 큐의 "예상 총비용"을 마나 UI(CharacterSystem HUD)에 전달하는 유니티 생명주기 연동 로직입니다(13-C, [L]).
    ///
    /// 크로스 스코프 방식: CharacterSystem(부모 스코프)은 UnitSystem의 RuntimeDataMultiLockOn을 볼 수 없지만,
    /// UnitSystem(자식 스코프)은 부모의 ICharacterStatService를 볼 수 있다. 그래서 자식인 이 로직이 큐 변화를 구독해
    /// 부모의 RuntimeStatData(ICharacterStatService.SetPreviewManaCost)에 값을 푸시하고,
    /// HUD(CharacterHUDUIView)는 이미 구독 중인 RuntimeStatData의 OnPreviewManaCostChanged만 받아 그린다.
    ///
    /// 비용 규칙은 실제 차감과 같은 IUnitBatchCastingService.CalculateTotalCost 한 곳이 정본이다(15-A).
    /// 3단계 마나 무제한 상태의 표시 여부는 HUD가 결정한다.
    /// </summary>
    public class LockOnManaPreviewLogicSystem : IInitializable, IDisposable
    {
        private readonly RuntimeDataMultiLockOn multiLockOnData;
        private readonly IUnitBatchCastingService batchCastingService;
        private readonly IUnitCatalogService catalogService;
        private readonly ICharacterStatService statService;

        public LockOnManaPreviewLogicSystem(
            RuntimeDataMultiLockOn multiLockOnData,
            IUnitBatchCastingService batchCastingService,
            IUnitCatalogService catalogService,
            ICharacterStatService statService)
        {
            this.multiLockOnData = multiLockOnData;
            this.batchCastingService = batchCastingService;
            this.catalogService = catalogService;
            this.statService = statService;
        }

        public void Initialize()
        {
            if (multiLockOnData != null)
            {
                multiLockOnData.OnEntryAdded += HandleEntryChanged;
                multiLockOnData.OnEntryRemoved += HandleEntryChanged;
                multiLockOnData.OnEntriesCleared += Refresh;
            }

            if (catalogService != null)
            {
                catalogService.OnUnitEquipped += HandleCatalogChanged;
                catalogService.OnUnitUnequipped += HandleCatalogChanged;
            }

            Refresh();
        }

        public void Dispose()
        {
            if (multiLockOnData != null)
            {
                multiLockOnData.OnEntryAdded -= HandleEntryChanged;
                multiLockOnData.OnEntryRemoved -= HandleEntryChanged;
                multiLockOnData.OnEntriesCleared -= Refresh;
            }

            if (catalogService != null)
            {
                catalogService.OnUnitEquipped -= HandleCatalogChanged;
                catalogService.OnUnitUnequipped -= HandleCatalogChanged;
            }

            statService?.SetPreviewManaCost(0);
        }

        private void HandleEntryChanged(LockOnCastEntry entry) => Refresh();
        private void HandleCatalogChanged(PureDataUnit unit) => Refresh();

        private void Refresh()
        {
            if (multiLockOnData == null || batchCastingService == null || statService == null) return;

            statService.SetPreviewManaCost(batchCastingService.CalculateTotalCost(multiLockOnData.Entries));
        }
    }
}
