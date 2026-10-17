using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace UnitSystem
{
    /// <summary>
    /// 락온 큐(RuntimeDataMultiLockOn.Entries)가 바뀔 때마다 대상별 "적용 전 변화 미리보기"를 갱신하는
    /// 유니티 생명주기 연동 로직입니다(13-C, [L]). 순수 C# — Transform/Renderer는 만지지 않고
    /// ILockOnPreviewVisualizer에 "무엇을 보일지"(LockOnTargetPreview)만 지시합니다.
    ///
    ///  - 구독: OnEntryAdded / OnEntryRemoved / OnEntriesCleared, 카탈로그 장착/해제(장착 해제된 쌍은 시전에서 제외되므로 미리보기에서도 제외).
    ///  - 갱신: 큐 전체를 ILockOnPreviewCalculator로 대상별 누적 예측(큐 순서, 같은 종류는 나중 것이 덮어씀)한 뒤,
    ///          예측이 있는 대상은 Show, 더 이상 없는 대상은 Hide.
    ///  - 큐 비움(발동/취소/시간정지 종료)은 UnitCasterSystem.ExecuteBatchCast가 시전 직전에 ClearEntries 하므로 미리보기가 먼저 사라진 뒤 실제 변환이 일어난다.
    /// </summary>
    public class LockOnPreviewLogicSystem : IInitializable, IDisposable
    {
        private readonly RuntimeDataMultiLockOn multiLockOnData;
        private readonly ILockOnPreviewCalculator calculator;
        private readonly IUnitCatalogService catalogService;
        private readonly ILockOnPreviewVisualizer visualizer;

        private readonly HashSet<RuntimeDataUnitGroup> shownTargets = new();

        public LockOnPreviewLogicSystem(
            RuntimeDataMultiLockOn multiLockOnData,
            ILockOnPreviewCalculator calculator,
            IUnitCatalogService catalogService,
            ILockOnPreviewVisualizer visualizer)
        {
            this.multiLockOnData = multiLockOnData;
            this.calculator = calculator;
            this.catalogService = catalogService;
            this.visualizer = visualizer;
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

            shownTargets.Clear();
            visualizer?.HideAll();
        }

        private void HandleEntryChanged(LockOnCastEntry entry) => Refresh();
        private void HandleCatalogChanged(PureDataUnit unit) => Refresh();

        private void Refresh()
        {
            if (multiLockOnData == null || calculator == null || visualizer == null) return;

            var previews = calculator.BuildTargetPreviews(multiLockOnData.Entries);

            var stillShown = new HashSet<RuntimeDataUnitGroup>();
            for (int i = 0; i < previews.Count; i++)
            {
                var preview = previews[i];
                stillShown.Add(preview.Target);
                visualizer.ShowPreview(preview);
            }

            foreach (var target in shownTargets)
            {
                if (!stillShown.Contains(target))
                {
                    visualizer.HidePreview(target);
                }
            }

            shownTargets.Clear();
            foreach (var target in stillShown)
            {
                shownTargets.Add(target);
            }
        }
    }
}
