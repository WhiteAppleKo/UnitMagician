using UnityEngine;

namespace NpcSystem
{
    /// <summary>
    /// NpcCombatActorLogicSystem(순수 C# 로직)이 실제 이동/모션/스폰을 수행시키기 위해 참조하는
    /// "수동적 수행자" 인터페이스입니다. 로직은 이 인터페이스만 호출하며, Transform/Animator/Rigidbody 등
    /// 구체 컴포넌트를 직접 참조·수정하지 않습니다(DLV Rule 2 - Blind Logic).
    /// </summary>
    public interface INpcCombatActorVisualizer
    {
        /// <summary>로직이 이동 도착 판정에 사용하는 현재 위치(값 읽기 전용).</summary>
        Vector3 CurrentPosition { get; }

        /// <summary>
        /// 공격 전략(SO)이 투사체를 스폰할 위치/방향 기준점. 기본은 CurrentPosition이며,
        /// INpcProjectileLaunchHook으로 발사 지점이 예약되어 있으면 그 지점을 돌려줍니다.
        /// </summary>
        Vector3 AttackOrigin { get; }

        /// <summary>
        /// 외부 시스템이 이 공격 1회의 발사 지점을 예약해 둔 상태인지 여부. 예약된 공격은 명시적인 이벤트 사격이므로
        /// 공격 쿨타임에 막혀 조용히 무시되지 않도록 로직이 쿨타임 검사를 건너뜁니다.
        /// </summary>
        bool HasArmedLaunchOrigin { get; }

        /// <summary>공격 전략(SO)이 발사자(attacker)와 그 provider(진영/공격 플래그)를 읽을 수 있도록 노출하는 소유자 GameObject 참조.</summary>
        GameObject Owner { get; }

        /// <summary>목적지 방향으로 speed*Time.deltaTime만큼(오버슈트 방지 clamp 포함) 실제 위치를 이동시킵니다.</summary>
        void MoveTowards(Vector3 destination, float speed);

        /// <summary>Animator가 있으면 해당 트리거를 재생합니다. 없으면 안전하게 무시됩니다.</summary>
        void PlayMotion(string motionKey);

        /// <summary>
        /// 런타임에 Instantiate한 오브젝트는 씬의 autoInjectGameObjects 대상이 아니므로
        /// 공격 전략(SO) 구현체들은 직접 Instantiate하지 말고 이 헬퍼를 통해 스폰해야 합니다.
        /// DI 주입에 대한 지식을 이 한 곳에만 두기 위한 헬퍼입니다.
        /// </summary>
        GameObject SpawnAndInject(GameObject prefab, Vector3 position, Quaternion rotation);

        /// <summary>
        /// 아군 투사체를 풀(IPoolService)에서 꺼내 발사 설정대로 발사합니다(15-B). 매 발사마다 Instantiate/Destroy하지 않고 풀링된 인스턴스를 재사용하며,
        /// 발사 지점 예약 소모와 OnProjectileSpawned 발행은 SpawnAndInject와 동일하게 처리됩니다. 발사된 투사체 GameObject를 돌려줍니다.
        /// </summary>
        GameObject SpawnProjectile(NpcProjectileLaunchData launchData);
    }
}
