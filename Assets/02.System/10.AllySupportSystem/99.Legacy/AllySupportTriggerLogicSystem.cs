using System;
using UnityEngine;
using NpcSystem;

namespace AllySupportSystem
{
    /// <summary>
    /// [Legacy] Goal 51에서 대체되어 어디에도 등록/참조되지 않는 격리 파일입니다(GUID 유지, 삭제 여부는 사용자 결정).
    /// 예전에는 06(이벤트 시퀀서)의 OnBoss50Percent 고정 신호를 구독해 보스 50%에서 아군 NPC가 보스를 1회 공격(3단계 지원 사격)했고, 실패 시 즉시 재발사하는 경로도 있었습니다.
    /// 지금은 보스 50% 이벤트가 이벤트 SO(PureDataEvent)로 정의되어 있고, 이벤트 투사체 발사는 EventProjectileLaunchActionHandler(01.Logic)가
    /// EventProjectileLaunchAction 동작으로 수행하며(HandleBoss50Percent와 실패 시 즉시 재발사는 제거됨), 돌문 사격은 Goal 50에서 이미 돌문 이벤트로 이전되었습니다.
    /// </summary>
    public class AllySupportTriggerLogicSystem
    {
        private readonly INpcCombatActor ally;
        private readonly GameObject bossTarget;

        public AllySupportTriggerLogicSystem(INpcCombatActor ally, GameObject bossTarget)
        {
            this.ally = ally ?? throw new ArgumentNullException(nameof(ally));
            this.bossTarget = bossTarget;
        }
    }
}
