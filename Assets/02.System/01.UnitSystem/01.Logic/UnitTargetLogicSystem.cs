using UnityEngine;

namespace UnitSystem
{
    [RequireComponent(typeof(RuntimeDataUnitGroup))]
    public class UnitTargetLogicSystem : MonoBehaviour
    {
        private RuntimeDataUnitGroup dataGroup;
        private IUnitTargetVisualizer visualizer;

        private void Awake()
        {
            dataGroup = GetComponent<RuntimeDataUnitGroup>();
            visualizer = GetComponent<IUnitTargetVisualizer>();
        }

        private void OnEnable()
        {
            if (dataGroup != null)
            {
                dataGroup.OnUnitGroupChanged += HandleUnitGroupChanged;
            }
        }

        private void OnDisable()
        {
            if (dataGroup != null)
            {
                dataGroup.OnUnitGroupChanged -= HandleUnitGroupChanged;
            }
        }

        private void HandleUnitGroupChanged(RuntimeDataUnitGroup group, RuntimeDataUnit data)
        {
            EvaluateAndApplyLogic(data);
        }

        public void EvaluateAndApplyLogic(RuntimeDataUnit data = null)
        {
            if (dataGroup == null || visualizer == null) return;

            if (data != null)
            {
                // 변경된 특정 단위 딱 1개만 단독 시각 갱신 (핀포인트 갱신)
                visualizer.ApplyVisuals(data);
            }
            else
            {
                // 초기화 시 등 불가피한 경우 전체 갱신
                foreach (var runtimeData in dataGroup.UnitRuntimeDataList)
                {
                    if (runtimeData == null) continue;
                    visualizer.ApplyVisuals(runtimeData);
                }
            }
        }
    }
}
