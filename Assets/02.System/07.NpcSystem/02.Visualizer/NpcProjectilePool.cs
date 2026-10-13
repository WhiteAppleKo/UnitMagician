using UnityEngine;
using VContainer;
using VContainer.Unity;
using Pooling;

namespace NpcSystem
{
    /// <summary>
    /// 아군 투사체(NpcProjectile)를 담는 풀의 씬 컴포넌트입니다(15-B). 99.Common/Pooling의 PoolingManager&lt;T, TData&gt;를 그대로 쓰며,
    /// Unity가 직렬화할 수 있도록 구체 타입을 정하고(MonoBehaviour는 제네릭 상태로 부착 불가) 아래 두 가지만 덧붙입니다.
    ///  - 풀 크기(초기 용량/상한)를 PureDataNpcProjectile에서 읽는다(DefaultSize/MaxSize 재정의).
    ///  - 런타임에 생성된 투사체는 씬의 자동 주입 대상이 아니므로, 생성 직후 IObjectResolver로 직접 주입한다
    ///    (예전 NpcCombatActorVisualizer.SpawnAndInject가 하던 주입을 풀 생성 시점 한 곳으로 옮김).
    /// NpcSystemLifetimeScope가 IPoolService&lt;NpcProjectile, NpcProjectileLaunchData&gt; 인터페이스로만 등록합니다.
    /// </summary>
    public class NpcProjectilePool : PoolingManager<NpcProjectile, NpcProjectileLaunchData>
    {
        private IObjectResolver resolver;
        private PureDataNpcProjectile settings;

        protected override int DefaultSize => settings != null ? settings.DefaultPoolSize : base.DefaultSize;
        protected override int MaxSize => settings != null ? settings.MaxPoolSize : base.MaxSize;

        [Inject]
        public void Construct(IObjectResolver resolver, PureDataNpcProjectile settings)
        {
            this.resolver = resolver;
            this.settings = settings;
        }

        protected override NpcProjectile CreatePoolItem()
        {
            var projectile = base.CreatePoolItem();
            resolver?.InjectGameObject(projectile.gameObject);
            return projectile;
        }
    }
}
