using UnityEngine;

namespace UnitSystem
{
    public class UnitTargetObjectVisualizer : MonoBehaviour, IUnitTargetVisualizer
    {
        private Rigidbody rb;
        private MeshRenderer meshRenderer;
        private Vector3 initialTransformScale;
        private bool isScaleCaptured = false;

        public Vector3 InitialScale => isScaleCaptured ? initialTransformScale : Vector3.one;

        private void Awake()
        {
            EnsureComponents();
            CaptureInitialScale();
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
