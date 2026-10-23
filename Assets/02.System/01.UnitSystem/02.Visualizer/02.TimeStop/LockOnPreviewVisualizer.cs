using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnitSystem
{
    /// <summary>
    /// 락온 큐의 "적용 전 변화 미리보기"를 월드에 그리는 Visualizer입니다(13-C). ILockOnPreviewVisualizer 구현체.
    /// 대상별로 홀로그램(Ghost Shader) 고스트 1개를 만들어 이 오브젝트 아래에 두고(씬 계층에서 확인 가능),
    /// 예측 결과(LockOnTargetPreview)를 그대로 표현한다.
    ///  - Mass:        고스트의 크기를 예상 스케일(초기 스케일 x 예상 질량 배율)로 표시 — 실제 MassUnitApplicator와 같은 규칙(32번).
    ///  - Temperature: 예상 온도 상태를 색조로 표시 — 얼음(음수)은 얼음 청백색, 가열(양수)은 주황.
    ///  - Vector:      폴백 — 크기 변화는 없고 "변화 예정" 색조(노랑) 마커 수준으로 표시.
    /// 색조 우선순위: Temperature > Vector > Mass(단독).
    /// 판단/계산은 하지 않는다(값은 Logic이 이미 결정). 대상 추적/자가 정리는 LockOnPreviewGhost가 수행한다.
    /// </summary>
    public class LockOnPreviewVisualizer : MonoBehaviour, ILockOnPreviewVisualizer
    {
        [Header("Ghost Material (Assets/04.VFX/01.Ghost Object/Ghost Object.mat)")]
        [SerializeField] private Material ghostMaterial;

        [Header("Preview Tints")]
        [SerializeField] private Color massColor = new Color(0.3f, 1f, 0.4f, 1f);
        [SerializeField] private Color iceColor = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private Color heatColor = new Color(1f, 0.45f, 0.15f, 1f);
        [SerializeField] private Color vectorColor = new Color(1f, 0.9f, 0.2f, 1f);

        [Header("Ghost Settings")]
        [Tooltip("대상 표면과 같은 깊이에서 깜빡이는(z-fighting) 것을 막기 위해 고스트를 살짝 키우는 배율")]
        [SerializeField] private float surfaceOffsetScale = 1.03f;

        // 홀로그램(Ghost Shader) 색상 프로퍼티는 ShaderGraph 레퍼런스 이름이 "_" 이다.
        private static readonly int HologramColorId = Shader.PropertyToID("_");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Dictionary<RuntimeDataUnitGroup, LockOnPreviewGhost> ghosts = new();
        private MaterialPropertyBlock propertyBlock;

        /// <summary>현재 살아 있는(표시 중인) 미리보기 수. 검증/디버그용.</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var pair in ghosts)
                {
                    if (pair.Value != null) count++;
                }
                return count;
            }
        }

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        public void ShowPreview(LockOnTargetPreview preview)
        {
            if (preview == null || preview.Target == null) return;

            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            if (!ghosts.TryGetValue(preview.Target, out var ghost) || ghost == null)
            {
                ghost = CreateGhost(preview.Target);
                if (ghost == null) return;
                ghosts[preview.Target] = ghost;
            }

            ApplyPreview(ghost, preview);
        }

        public void HidePreview(RuntimeDataUnitGroup target)
        {
            if (target == null) return;
            if (!ghosts.TryGetValue(target, out var ghost)) return;

            ghosts.Remove(target);
            DestroyGhost(ghost);
        }

        public void HideAll()
        {
            if (ghosts.Count == 0) return;

            var list = new List<LockOnPreviewGhost>(ghosts.Values);
            ghosts.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                DestroyGhost(list[i]);
            }
        }

        private void OnDestroy()
        {
            ghosts.Clear();
        }

        private static void DestroyGhost(LockOnPreviewGhost ghost)
        {
            if (ghost == null) return;

            // Destroy는 프레임 끝에 실행되므로, 같은 프레임에 잔상이 보이거나 계층에서 헷갈리지 않도록 먼저 꺼 둔다.
            ghost.gameObject.SetActive(false);
            Destroy(ghost.gameObject);
        }

        private LockOnPreviewGhost CreateGhost(RuntimeDataUnitGroup target)
        {
            var go = new GameObject($"{target.gameObject.name}_LockOnPreview");
            go.transform.SetParent(transform, false);

            // 고스트 형태는 대상 자신의 메시를 그대로 쓴다(대상 루트의 MeshFilter. 없으면 첫 자식 메시).
            // 대상 전체를 복제하지 않는 이유: 자식의 하이라이트 마커/파티클/콜라이더/스크립트까지 복제되기 때문.
            var sourceFilter = FindSourceMesh(target);
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                Debug.LogWarning($"[LockOnPreviewVisualizer] {target.gameObject.name}에 미리보기로 쓸 MeshFilter가 없어 미리보기를 생략합니다.");
                Destroy(go);
                return null;
            }

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = sourceFilter.sharedMesh;

            var meshRenderer = go.AddComponent<MeshRenderer>();
            if (ghostMaterial != null)
            {
                var mats = new Material[Mathf.Max(1, sourceFilter.sharedMesh.subMeshCount)];
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
                meshRenderer.sharedMaterials = mats;
            }
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var ghost = go.AddComponent<LockOnPreviewGhost>();
            ghost.Initialize(target, HandleGhostDestroyed);
            return ghost;
        }

        private static MeshFilter FindSourceMesh(RuntimeDataUnitGroup target)
        {
            var root = target.GetComponent<MeshFilter>();
            if (root != null && root.sharedMesh != null) return root;

            var children = target.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].sharedMesh != null) return children[i];
            }

            return null;
        }

        private void ApplyPreview(LockOnPreviewGhost ghost, LockOnTargetPreview preview)
        {
            var targetTransform = preview.Target.transform;
            var go = ghost.gameObject;
            go.name = $"{preview.Target.gameObject.name}_LockOnPreview [{preview.Description}]";

            // 1) 크기: Mass 변화가 있으면 실제 적용기(MassUnitApplicator)와 같은 절대 규칙 —
            //    localScale = (IUnitTargetVisualizer.InitialScale 또는 Vector3.one) x 스케일 배율. 없으면 현재 크기 그대로.
            Vector3 worldScale = targetTransform.lossyScale;
            if (preview.HasScaleChange)
            {
                var targetVisualizer = preview.Target.GetComponent<IUnitTargetVisualizer>();
                Vector3 baseScale = targetVisualizer != null ? targetVisualizer.InitialScale : Vector3.one;
                Vector3 predictedLocal = baseScale * preview.ScaleMultiplier;
                worldScale = Vector3.Scale(worldScale, DivideSafe(predictedLocal, targetTransform.localScale));
            }
            go.transform.localScale = worldScale * surfaceOffsetScale;
            go.transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);

            // 2) 색조: 예상 온도 상태 > 폴백(Vector) > Mass 단독
            Color tint = ResolveTint(preview);
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(HologramColorId, tint);
                propertyBlock.SetColor(BaseColorId, tint);
                propertyBlock.SetColor(ColorId, tint);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private Color ResolveTint(LockOnTargetPreview preview)
        {
            if (preview.HasTemperature) return preview.IsIce ? iceColor : heatColor;
            if (preview.HasVector) return vectorColor;
            return massColor;
        }

        private static Vector3 DivideSafe(Vector3 a, Vector3 b)
        {
            return new Vector3(
                Mathf.Abs(b.x) > 1e-6f ? a.x / b.x : 1f,
                Mathf.Abs(b.y) > 1e-6f ? a.y / b.y : 1f,
                Mathf.Abs(b.z) > 1e-6f ? a.z / b.z : 1f);
        }

        private void HandleGhostDestroyed(LockOnPreviewGhost ghost)
        {
            // 대상 파괴/비활성화로 고스트가 스스로 사라졌을 때 사전에서 제거한다.
            if (ReferenceEquals(ghost, null)) return;

            var target = ghost.Target;
            if (target != null && ghosts.TryGetValue(target, out var current) && current == ghost)
            {
                ghosts.Remove(target);
                return;
            }

            // 대상 자체가 파괴된 경우(키가 Unity null) — 값으로 찾아 제거
            RuntimeDataUnitGroup staleKey = null;
            foreach (var pair in ghosts)
            {
                if (pair.Value == ghost) { staleKey = pair.Key; break; }
            }
            if (!ReferenceEquals(staleKey, null)) ghosts.Remove(staleKey);
        }
    }
}
