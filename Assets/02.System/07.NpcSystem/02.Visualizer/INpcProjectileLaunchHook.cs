using System;
using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// 외부 시스템(예: 09.EventSequencerSystem의 3단계 비행 추적기)이 NPC가 쏘는 투사체의
    /// 발사 지점을 한 번만 바꾸고, 실제로 스폰된 투사체의 참조를 받을 수 있게 하는 얇은 후크입니다.
    /// NPC 공격 로직(NpcCombatActorLogicSystem/AllySupportTriggerLogicSystem)은 이 후크를 모릅니다 —
    /// 구현체(NpcCombatActorVisualizer)가 AttackOrigin/SpawnAndInject 안에서만 이 값을 사용합니다.
    /// </summary>
    public interface INpcProjectileLaunchHook
    {
        /// <summary>다음에 스폰되는 투사체 1개를 이 위치에서 발사하도록 예약합니다(일회성, 스폰 시 소모).</summary>
        void ArmLaunchOrigin(Vector3 origin);

        /// <summary>예약된 발사 지점을 취소합니다(이미 소모됐다면 아무 일도 없음).</summary>
        void DisarmLaunchOrigin();

        /// <summary>이 NPC가 투사체를 스폰(SpawnAndInject)한 직후 발행됩니다.</summary>
        event Action<GameObject> OnProjectileSpawned;
    }
}
