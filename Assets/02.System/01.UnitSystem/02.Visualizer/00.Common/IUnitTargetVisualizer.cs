using UnityEngine;

namespace UnitSystem
{
    public interface IUnitTargetVisualizer
    {
        /// <summary>
        /// 질량 배율(32번)을 적용합니다: 크기 = 프리팹 기본 Scale(InitialScale) × multiplier, Rigidbody 질량 = 원래 질량 × multiplier.
        /// multiplier 1이면 기본 크기와 원래 질량으로 돌아옵니다.
        /// </summary>
        void ApplyMassScale(float multiplier);
        void ApplyVelocity(Vector3 direction, float speed);
        void ApplyMaterial(Material material);
        Vector3 InitialScale { get; }

        /// <summary>
        /// 연출용 이펙트 프리팹을 이 대상의 위치·회전에 스폰합니다(28번 §6-7). Applicator(Logic)/전략 클래스는 직접 Instantiate하지 말고
        /// 반드시 이 헬퍼를 통해서만 이펙트를 스폰해야 합니다(대상 자신의 Transform/Material을 건드리지 않는
        /// 독립된 연출 오브젝트 스폰이므로, DI 주입이 필요해지면 이 한 곳에서만 대응하면 됩니다).
        /// scaleMultiplier는 스폰한 이펙트의 크기 배율입니다(호출자는 배율 값만 계산해 넘기고 transform을 만지지 않음, §6-11).
        /// </summary>
        GameObject SpawnEffect(GameObject prefab, float scaleMultiplier = 1f);
    }
}
