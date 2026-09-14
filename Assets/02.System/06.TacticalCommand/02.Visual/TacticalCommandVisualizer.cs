using System.Collections.Generic;
using UnityEngine;

namespace TacticalCommandSystem
{
    /// <summary>
    /// 전술 명령의 인게임 3D 시각 연출을 전담하는 수동적 Visualizer 컴포넌트입니다.
    /// ITacticalCommandVisualizer 인터페이스를 구현하며 로직 시스템의 지시만을 받아
    /// 조준 궤적선(LineRenderer), 타깃 머리 위 순서 인디케이터([1st], [2nd] 배지), 타격 이펙트를 연출합니다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TacticalCommandVisualizer : MonoBehaviour, ITacticalCommandVisualizer
    {
        [Header("Line Settings")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private float lineWidth = 0.12f;
        [SerializeField] private Color defaultLineColor = new Color(0.3f, 0.8f, 1f, 0.8f);

        [Header("Indicator Settings")]
        [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Color indicatorColor = new Color(0.2f, 0.9f, 1f, 1f);

        [Header("Effects (Optional)")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip enqueueSound;
        [SerializeField] private AudioClip executeSound;
        [SerializeField] private AudioClip errorSound;

        private readonly Dictionary<TacticalCommandEntry, GameObject> activeIndicators = new();
        private GameObject currentHoverObject;
        private Camera targetCamera;

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            InitLineRenderer();
            targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            // 빌보드 효과: 모든 인디케이터가 카메라를 바라보도록 유지
            if (targetCamera != null && activeIndicators.Count > 0)
            {
                var camTransform = targetCamera.transform;
                foreach (var kvp in activeIndicators)
                {
                    if (kvp.Value != null)
                    {
                        var entry = kvp.Key;
                        if (entry.TargetObject != null)
                        {
                            kvp.Value.transform.position = entry.TargetObject.transform.position + indicatorOffset;
                        }
                        kvp.Value.transform.rotation = Quaternion.LookRotation(kvp.Value.transform.position - camTransform.position);
                    }
                }
            }
        }

        private void InitLineRenderer()
        {
            if (lineRenderer == null) return;

            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth * 0.7f;
            lineRenderer.useWorldSpace = true;

            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }
            else
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    lineRenderer.material = new Material(shader);
                }
            }

            lineRenderer.startColor = defaultLineColor;
            lineRenderer.endColor = defaultLineColor;
        }

        public void ShowTacticalVisuals(bool show)
        {
            if (!show)
            {
                ClearAllVisuals();
            }
        }

        public void SetHoverTarget(GameObject target)
        {
            if (currentHoverObject == target) return;

            if (currentHoverObject != null)
            {
                var prevGroup = currentHoverObject.GetComponentInParent<UnitSystem.RuntimeDataUnitGroup>();
                if (prevGroup != null) prevGroup.Highlight(false, false);
            }

            currentHoverObject = target;

            if (currentHoverObject != null)
            {
                var newGroup = currentHoverObject.GetComponentInParent<UnitSystem.RuntimeDataUnitGroup>();
                if (newGroup != null) newGroup.Highlight(true, false);
            }
        }

        public void AddTargetIndicator(TacticalCommandEntry entry)
        {
            if (entry == null || entry.TargetObject == null) return;

            // 이미 존재하는 인디케이터가 있다면 제거 후 재생성
            if (activeIndicators.TryGetValue(entry, out var existingObj))
            {
                if (existingObj != null) Destroy(existingObj);
                activeIndicators.Remove(entry);
            }

            // 인디케이터 게임오브젝트 생성
            var indicatorObj = new GameObject($"Indicator_#{entry.OrderIndex}_{entry.TargetObject.name}");
            indicatorObj.transform.position = entry.TargetObject.transform.position + indicatorOffset;

            // 3D 텍스트 메시 구성
            var textMesh = indicatorObj.AddComponent<TextMesh>();
            string suffix = GetOrderSuffix(entry.OrderIndex);
            textMesh.text = $"[{entry.OrderIndex}{suffix}]\n{entry.MagicData?.UnitName}";
            textMesh.fontSize = 24;
            textMesh.characterSize = 0.12f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = indicatorColor;
            textMesh.fontStyle = FontStyle.Bold;

            activeIndicators[entry] = indicatorObj;

            // 프로젝트 표준 락온 하이라이트 활성화
            var unitGroup = entry.TargetObject.GetComponentInParent<UnitSystem.RuntimeDataUnitGroup>();
            if (unitGroup != null) unitGroup.Highlight(true, true);

            PlaySound(enqueueSound);
        }

        public void RemoveTargetIndicator(TacticalCommandEntry entry)
        {
            if (entry == null) return;

            if (entry.TargetObject != null)
            {
                var unitGroup = entry.TargetObject.GetComponentInParent<UnitSystem.RuntimeDataUnitGroup>();
                if (unitGroup != null) unitGroup.Highlight(false, false);
            }

            if (activeIndicators.TryGetValue(entry, out var indicatorObj))
            {
                if (indicatorObj != null)
                {
                    Destroy(indicatorObj);
                }
                activeIndicators.Remove(entry);
            }
        }

        public void UpdateTrajectoryLines(IReadOnlyList<TacticalCommandEntry> entries, Vector3 originPosition)
        {
            if (lineRenderer == null) return;

            if (entries == null || entries.Count == 0)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            // 포인트 구성: Origin -> Target 1 -> Target 2 -> ...
            var points = new List<Vector3>();
            points.Add(originPosition);

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].TargetObject != null)
                {
                    points.Add(entries[i].TargetObject.transform.position + Vector3.up * 0.5f);
                }
            }

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }

        public void PlayCommandExecutionEffect(TacticalCommandEntry entry)
        {
            if (entry == null || entry.TargetObject == null) return;

            Vector3 hitPos = entry.TargetObject.transform.position + Vector3.up * 1f;

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hitPos, Quaternion.identity);
            }
            else
            {
                // Fallback: 간이 플래시 연출을 위한 파티클 또는 디버그
                Debug.Log($"<color=yellow>[TacticalCommandVisualizer] 연쇄 타격 시각 이펙트 발동 -> {entry.TargetObject.name}</color>");
            }

            PlaySound(executeSound);
        }

        public void PlayManaInsufficientFeedback()
        {
            PlaySound(errorSound);
            Debug.LogWarning("<color=red>[TacticalCommandVisualizer] 마나 부족 경고 피드백 연출!</color>");
        }

        public void ClearAllVisuals()
        {
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }

            if (currentHoverObject != null)
            {
                var prevGroup = currentHoverObject.GetComponentInParent<UnitSystem.RuntimeDataUnitGroup>();
                if (prevGroup != null) prevGroup.Highlight(false, false);
                currentHoverObject = null;
            }

            foreach (var kvp in activeIndicators)
            {
                if (kvp.Key?.TargetObject != null)
                {
                    var group = kvp.Key.TargetObject.GetComponentInParent<UnitSystem.RuntimeDataUnitGroup>();
                    if (group != null) group.Highlight(false, false);
                }
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            activeIndicators.Clear();
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private string GetOrderSuffix(int order)
        {
            switch (order)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }
    }
}
