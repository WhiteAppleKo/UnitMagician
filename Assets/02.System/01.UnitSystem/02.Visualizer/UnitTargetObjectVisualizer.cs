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

        // 풀링 재사용(15-B) 시 마법(질량/온도/벡터)으로 바뀐 값을 되돌리기 위해 최초 값을 보관한다.
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

        /// <summary>재사용 직전: 마법으로 바뀐 스케일/질량/머티리얼/속도를 최초 값으로 되돌린다.</summary>
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

        public void ApplyMassScale(float mass, float scaleMultiplier)
        {
            EnsureComponents();
            CaptureInitialScale();

            if (rb != null)
            {
                rb.mass = Mathf.Max(0.1f, mass);
            }

            transform.localScale = InitialScale * scaleMultiplier;
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

        public GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            return Instantiate(prefab, position, rotation);
        }

        public void ApplyVisuals(RuntimeDataUnit runtimeData)
        {
            if (runtimeData == null) return;
            CaptureInitialScale();

            // 조건문 0% 전면 제거 -> C# 다형성 단 1줄 자동 수행
            if (runtimeData.CurrentUnitData != null && runtimeData.CurrentUnitData.Applicator != null)
            {
                runtimeData.CurrentUnitData.Applicator.Apply(gameObject, runtimeData);
            }
            else
            {
                Debug.LogWarning($"[UnitTargetObjectVisualizer] Applicator is missing for {runtimeData.CurrentUnit}");
            }
        }
    }
}
