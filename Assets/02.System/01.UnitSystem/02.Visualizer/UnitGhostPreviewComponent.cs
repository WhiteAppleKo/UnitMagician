using System;
using UnityEngine;

namespace UnitSystem
{
    /// <summary>
    /// 단위 변환 결과 사전 확인용 3D 고스트 프리뷰(Ghost Preview) 컴포넌트.
    /// 원본 오브젝트를 복제(Instantiate)하여 물리 컴포넌트 제거 후 프리뷰 마테리얼 적용.
    /// </summary>
    public class UnitGhostPreviewComponent : MonoBehaviour
    {
        [Header("Preview Colors")]
        [SerializeField] private Color validColor = new Color(0f, 1f, 0.3f, 0.5f);   // 변환 가능 색상 (Green)
        [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.5f); // 변환 불가 색상 (Red)

        [Header("Default Preview Material")]
        [SerializeField] private Material defaultPreviewMaterial;

        private GameObject ghostObject;
        private Renderer[] ghostRenderers;
        private MaterialPropertyBlock propertyBlock;
        private bool isShowing = false;

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        public bool IsShowing => isShowing;
        public GameObject GhostObject => ghostObject;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 원본 타깃 오브젝트 복제하여 3D 고스트 프리뷰 생성 및 표출.
        /// </summary>
        /// <param name="target">원본 대상 오브젝트</param>
        /// <param name="overrideMaterial">프리뷰 전용 마테리얼 (null일 경우 기본 마테리얼 사용)</param>
        public void ShowPreview(GameObject target, Material overrideMaterial = null)
        {
            if (target == null) return;

            // 기존 프리뷰 존재 시 먼저 정리
            HidePreview();

            // 1. 원본 GameObject 복제 (Ghost 클론 생성)
            ghostObject = Instantiate(target, target.transform.position, target.transform.rotation);
            ghostObject.name = $"{target.name}_GhostPreview";

            // 2. 물리/충돌 컴포넌트 제거 (자식 오브젝트 포함)
            StripPhysicsComponents(ghostObject);

            // 3. Renderer 마테리얼 프리뷰 전용 마테리얼로 일괄 교체
            Material matToUse = overrideMaterial != null ? overrideMaterial : defaultPreviewMaterial;
            ghostRenderers = ghostObject.GetComponentsInChildren<Renderer>();

            if (matToUse != null)
            {
                foreach (var rend in ghostRenderers)
                {
                    Material[] newMats = new Material[rend.sharedMaterials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        newMats[i] = matToUse;
                    }
                    rend.materials = newMats;
                }
            }

            // 4. 초기 상태 설정 (기본 가능 색상 지정)
            SetPreviewStatus(true);

            ghostObject.SetActive(true);
            isShowing = true;
        }

        /// <summary>
        /// 실시간으로 고스트 프리뷰 클론의 Transform 수치(스케일, 방향 등) 갱신.
        /// </summary>
        /// <param name="targetScale">목표 스케일 수치</param>
        /// <param name="targetDirection">목표 회전 방향 수치</param>
        public void UpdatePreview(Vector3 targetScale, Vector3 targetDirection)
        {
            if (!isShowing || ghostObject == null) return;

            // 스케일 갱신
            if (targetScale != Vector3.zero)
            {
                ghostObject.transform.localScale = targetScale;
            }

            // 방향 갱신
            if (targetDirection != Vector3.zero)
            {
                ghostObject.transform.forward = targetDirection.normalized;
            }
        }

        /// <summary>
        /// 변환 가능 여부에 따라 프리뷰 마테리얼 색상(Green/Red) 변경.
        /// </summary>
        /// <param name="isValid">true: 변환 가능(Green), false: 변환 불가(Red)</param>
        public void SetPreviewStatus(bool isValid)
        {
            if (!isShowing || ghostRenderers == null) return;

            Color targetColor = isValid ? validColor : invalidColor;

            foreach (var rend in ghostRenderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorID, targetColor);
                propertyBlock.SetColor(ColorID, targetColor);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        /// <summary>
        /// 고스트 프리뷰 숨기기 (비활성화).
        /// </summary>
        public void HidePreview()
        {
            if (ghostObject != null)
            {
                Destroy(ghostObject);
                ghostObject = null;
            }
            ghostRenderers = null;
            isShowing = false;
        }

        /// <summary>
        /// 원본에 최종 수치 반영 완료 후 프리뷰 클론 완전 파괴.
        /// </summary>
        /// <param name="applyAction">원본 수치 변환 실행 콜백 함수</param>
        public void ApplyAndDestroy(Action applyAction)
        {
            // 1. 원본 변환 실행
            applyAction?.Invoke();

            // 2. 프리뷰 클론 제거
            HidePreview();
        }

        /// <summary>
        /// 복제 오브젝트 내부 충돌체, 물리 및 게임플레이 스크립트 의존성 역순 안전 자동 제거.
        /// </summary>
        private void StripPhysicsComponents(GameObject obj)
        {
            // 1. Colliders 제거
            var colliders = obj.GetComponentsInChildren<Collider>(true);
            for (int i = colliders.Length - 1; i >= 0; i--)
            {
                if (colliders[i] != null) DestroyImmediate(colliders[i]);
            }

            // 2. Rigidbodies 제거
            var rigidbodies = obj.GetComponentsInChildren<Rigidbody>(true);
            for (int i = rigidbodies.Length - 1; i >= 0; i--)
            {
                if (rigidbodies[i] != null) DestroyImmediate(rigidbodies[i]);
            }

            // 3. Joints 제거
            var joints = obj.GetComponentsInChildren<Joint>(true);
            for (int i = joints.Length - 1; i >= 0; i--)
            {
                if (joints[i] != null) DestroyImmediate(joints[i]);
            }

            // 4. RequireComponent 의존성 고려하여 커스텀 MonoBehaviour 스크립트 역순 안전 제거
            var scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = scripts.Length - 1; i >= 0; i--)
            {
                if (scripts[i] != null && scripts[i] != this)
                {
                    DestroyImmediate(scripts[i]);
                }
            }
        }

        private void OnDestroy()
        {
            HidePreview();
        }
    }
}
