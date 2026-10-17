using System;
using VContainer.Unity;

namespace UnitSystem
{
    /// <summary>
    /// 락온 큐/지정 대기에 들어 있는 대상이 소실(파괴 또는 계층 비활성 = 풀 반환 포함)되면 정리하는
    /// 유니티 생명주기 연동 로직입니다(15-A §3-C, 15-B, [L]). 순수 C#이며 소실 정리는 두 경로로 이뤄집니다.
    ///
    /// 1) 직접 통지(주 경로, 15-B): 풀링되는 대상은 풀에 반환되는 순간 RuntimeDataUnitGroup.OnAnyTargetRetired를 발행하고,
    ///    이 시스템이 즉시 RuntimeDataMultiLockOn.RemoveTarget(대상)을 호출한다. 반환과 재사용(SetActive(false) → SetActive(true))이
    ///    같은 프레임/두 점검 사이에 일어나면 주기 점검으로는 "이미 다시 활성"이라 소실을 못 잡아 이전 큐 엔트리가 재사용된 인스턴스에
    ///    남던 경합(15-A QA 실측)을 없앤다. 투사체(NpcSystem)는 UnitSystem의 락온 데이터를 알지 못하고 대상 컴포넌트의 이벤트만 발행한다.
    /// 2) 주기 점검(안전망, 15-A): 매 프레임 RuntimeDataMultiLockOn.RemoveLostTargets()를 호출한다. Destroy / SetActive(false) /
    ///    부모 비활성 등 풀 통지를 거치지 않는 소실도 같은 규칙(RuntimeDataUnitGroup.IsValidTarget 한 곳의 판정)으로 정리한다.
    ///    큐가 비면 비용 0, 보통 수 개 항목 순회.
    /// </summary>
    public class LockOnTargetLossLogicSystem : IInitializable, ITickable, IDisposable
    {
        private readonly RuntimeDataMultiLockOn multiLockOnData;

        public LockOnTargetLossLogicSystem(RuntimeDataMultiLockOn multiLockOnData)
        {
            this.multiLockOnData = multiLockOnData;
        }

        public void Initialize()
        {
            RuntimeDataUnitGroup.OnAnyTargetRetired += HandleTargetRetired;
        }

        public void Dispose()
        {
            RuntimeDataUnitGroup.OnAnyTargetRetired -= HandleTargetRetired;
        }

        private void HandleTargetRetired(RuntimeDataUnitGroup target)
        {
            multiLockOnData?.RemoveTarget(target);
        }

        public void Tick()
        {
            if (multiLockOnData == null) return;
            if (!multiLockOnData.HasPending && multiLockOnData.EntryCount == 0) return;

            multiLockOnData.RemoveLostTargets();
        }
    }
}
