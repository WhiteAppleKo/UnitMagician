using UnityEngine;
using UnityEngine.UIElements;

namespace UnitSystem
{
    /// <summary>
    /// 1인칭 및 3인칭 숄더뷰 다중 락온 조준선(크로스헤어) 및 락온 수량 표시를 담당하는 Visualizer입니다.
    /// </summary>
    public class MultiLockOnVisualizer : MonoBehaviour, IMultiLockOnVisualizer
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement crosshairContainer;
        private Label lockOnCountLabel;

        private void Awake()
        {
            EnsureUI();
        }

        private void EnsureUI()
        {
            if (this == null) return;
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;
                crosshairContainer = root.Q<VisualElement>("MultiLockOnCrosshair");
                lockOnCountLabel = root.Q<Label>("MultiLockOnCountLabel");
            }
        }

        public void SetCrosshairVisible(bool visible)
        {
            EnsureUI();
            if (crosshairContainer != null)
            {
                crosshairContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void UpdateLockOnCount(int count)
        {
            EnsureUI();
            if (lockOnCountLabel != null)
            {
                lockOnCountLabel.text = count > 0 ? $"Lock-On x{count}" : string.Empty;
                lockOnCountLabel.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void PlayBatchCastEffect()
        {
            Debug.Log("<color=cyan>[MultiLockOnVisualizer] Batch Cast Effect Triggered!</color>");
        }

        public IReadOnlyList<Collider> DetectAimTargets(float radius, float maxDistance)
        {
            return DetectAimTargets(radius, maxDistance, Physics.DefaultRaycastLayers);
        }

        public IReadOnlyList<Collider> DetectAimTargets(float radius, float maxDistance, LayerMask mask)
        {
            Camera cam = Camera.main;
            if (cam == null) return System.Array.Empty<Collider>();

            Ray aimRay = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit[] hits = Physics.SphereCastAll(aimRay, radius, maxDistance, mask);
            if (hits == null || hits.Length == 0)
            {
                return System.Array.Empty<Collider>();
            }

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            var colliders = new System.Collections.Generic.List<Collider>(hits.Length);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null)
                {
                    colliders.Add(hits[i].collider);
                }
            }

            return colliders;
        }
    }
}
