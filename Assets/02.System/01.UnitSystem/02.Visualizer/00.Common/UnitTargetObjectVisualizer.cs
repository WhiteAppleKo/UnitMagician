using UnityEngine;
using Pooling;

namespace UnitSystem
{
    public class UnitTargetObjectVisualizer : MonoBehaviour, IUnitTargetVisualizer, IPoolReusable
    {
        private Rigidbody rb;
        private MeshRenderer meshRenderer;
        private Vector3 initialTransformScale;
        private bool isScaleCaptured = false;

        // 풀링 재사용(15-B) 시 지속 마법(질량/온도)으로 바뀐 값을 되돌리기 위해 최초 값을 보관한다.
        // initialMass는 질량 배율의 베이스이기도 하다(rb.mass = initialMass × 배율, 32번).
        private float initialMass = 1f;
        private Material initialSharedMaterial;

        public Vector3 InitialScale => isScaleCaptured ? initialTransformScale : Vector3.one;

        private void Awake()
        {
            EnsureComponents();
            CaptureInitialScale();
            if (rb != null) initialMass = rb.mass;
            if (meshRenderer != null) initialSharedMaterial = meshRenderer.sharedMaterial;
        }

        public void OnReturnedToPool()
        {
        }

        /// <summary>재사용 직전: 지속 마법으로 바뀐 스케일/질량/머티리얼을 최초 값으로 되돌리고, 재발사를 위해 물리 속도를 0으로 초기화한다(Vector 즉시 마법 결과를 되돌리는 동작이 아님).</summary>
        public void ResetForReuse()
        {
            EnsureComponents();
            CaptureInitialScale();

            transform.localScale = InitialScale;

            if (rb != null)
            {
                rb.mass = initialMass;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (meshRenderer != null && initialSharedMaterial != null)
            {
                meshRenderer.sharedMaterial = initialSharedMaterial;
            }
        }

        private void EnsureComponents()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        }

        private void CaptureInitialScale()
        {
            if (!isScaleCaptured)
            {
                initialTransformScale = transform.localScale != Vector3.zero ? transform.localScale : Vector3.one;
                isScaleCaptured = true;
            }
        }

        public void ApplyMassScale(float multiplier)
        {
            EnsureComponents();
            CaptureInitialScale();

            if (rb != null)
            {
                rb.mass = initialMass * multiplier;
            }

            transform.localScale = InitialScale * multiplier;
        }

        public void ApplyVelocity(Vector3 direction, float speed)
        {
            EnsureComponents();

            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }

            if (direction != Vector3.zero)
            {
                transform.forward = direction;
            }
        }

        public void ApplyMaterial(Material material)
        {
            EnsureComponents();

            if (meshRenderer != null && material != null)
            {
                meshRenderer.material = material;
            }
        }

        /// <summary>이 대상의 위치·회전에 이펙트를 스폰하고 크기에 배율을 곱한다(28번 §6-7, §6-11).</summary>
        public GameObject SpawnEffect(GameObject prefab, float scaleMultiplier = 1f)
        {
            if (prefab == null) return null;

            var effect = Instantiate(prefab, transform.position, transform.rotation);
            effect.transform.localScale *= scaleMultiplier;
            return effect;
        }
    }
}
