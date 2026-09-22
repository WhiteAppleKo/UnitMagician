using UnityEngine;

namespace UnitSystem
{
    public interface IUnitTargetVisualizer
    {
        void ApplyMassScale(float mass, float scaleMultiplier);
        void ApplyVelocity(Vector3 direction, float speed);
        void ApplyMaterial(Material material);
        Vector3 InitialScale { get; }
        void ApplyVisuals(RuntimeDataUnit runtimeData);

        /// <summary>
        /// 연출용 이펙트 프리팹을 월드에 스폰합니다. Applicator(SO)/전략 클래스는 직접 Instantiate하지 말고
        /// 반드시 이 헬퍼를 통해서만 이펙트를 스폰해야 합니다(대상 자신의 Transform/Material을 건드리지 않는
        /// 독립된 연출 오브젝트 스폰이므로, DI 주입이 필요해지면 이 한 곳에서만 대응하면 됩니다).
        /// </summary>
        GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation);
    }
}
