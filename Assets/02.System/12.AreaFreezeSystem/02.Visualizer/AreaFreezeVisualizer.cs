using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace AreaFreezeSystem
{
    /// <summary>
    /// IAreaFreezeVisualizer의 기본 구현체([V] World Visualizer)입니다. "수동적 수행자"로서 실제
    /// Physics.OverlapSphere 물리 조회, transform.position 접근, 코루틴 기반 순차 Freeze() 호출을 전부
    /// 수행합니다(05번 기획서 5.9절 - Logic이 아닌 이 계층이 담당하는 이유).
    ///
    /// 명중 좌표 대신 bossGameObject.transform.position을 폭발 중심으로 사용합니다 - DamageContext(기존 코드,
    /// Assets/PipeLine/Contexts/DamageContext.cs)에 명중 좌표 필드가 없고, "핵은 순수 연출일 뿐 보스 몸통 어디에
    /// 맞아도 된다"(5.1절)는 설계상 정확한 명중 좌표가 필요하지 않기 때문입니다(5.8절에서 이미 확정).
    ///
    /// 실제 얼음 텍스처/파티클/폭발음 아트 리소스는 이번 범위 밖이므로(Goal 프롬프트 "제외" 절),
    /// BossPhaseVisualizer와 동일하게 로그 기반 플레이스홀더로 대체합니다.
    /// </summary>
    public class AreaFreezeVisualizer : MonoBehaviour, IAreaFreezeVisualizer
    {
        private PureDataAreaFreeze pureData;
        private Coroutine sequenceCoroutine;

        [Inject]
        public void Construct(PureDataAreaFreeze pureData)
        {
            this.pureData = pureData;
        }

        /// <summary>
        /// 5.8절 데이터 흐름: bossGameObject.transform.position을 중심으로 Physics.OverlapSphere(center,
        /// freezeRadius)로 콜라이더를 조회 → 각 결과에서 TryGetComponent&lt;IFreezable&gt;로 추출 →
        /// freezeSequenceDelay 간격으로 순차 Freeze() 호출. 재호출(예: 반복 트리거) 시 진행 중이던 시퀀스를
        /// 멈추고 새로 시작합니다.
        /// </summary>
        public void TriggerAreaFreeze(GameObject bossGameObject)
        {
            if (bossGameObject == null)
            {
                Debug.LogError("[AreaFreezeVisualizer] bossGameObject가 null이라 광역 빙결을 트리거할 수 없습니다.");
                return;
            }

            if (pureData == null)
            {
                Debug.LogError("[AreaFreezeVisualizer] PureDataAreaFreeze가 주입되지 않았습니다.");
                return;
            }

            if (sequenceCoroutine != null)
            {
                StopCoroutine(sequenceCoroutine);
            }

            Vector3 center = bossGameObject.transform.position;

            // 5.4절 명중 순간 연출: 화면 전체 임팩트 프레임(화이트아웃/슬로우) + 방사형 얼음 폭발 파티클 +
            // 저음 폭발음 (플레이스홀더).
            Debug.Log($"<color=cyan>[AreaFreezeVisualizer] 광역 빙결 트리거 - 화이트아웃 + 얼음 폭발 파티클 + " +
                      $"저음 폭발음 재생 (플레이스홀더). center={center}, radius={pureData.FreezeRadius}</color>");

            List<IFreezable> targets = CollectFreezableTargets(center, pureData.FreezeRadius);
            sequenceCoroutine = StartCoroutine(PlayFreezeSequence(targets));
        }

        /// <summary>
        /// Physics.OverlapSphere + TryGetComponent&lt;IFreezable&gt;로 반경 내 대상을 수집합니다. 코드_가이드라인상
        /// Logic이 아닌 Visualizer 계층에서 이 물리 접근을 수행합니다(05번 기획서 5.9절).
        /// </summary>
        private List<IFreezable> CollectFreezableTargets(Vector3 center, float radius)
        {
            var results = new List<IFreezable>();
            Collider[] hits = Physics.OverlapSphere(center, radius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IFreezable>(out var freezable) && !results.Contains(freezable))
                {
                    results.Add(freezable);
                }
            }
            return results;
        }

        /// <summary>
        /// freezeSequenceDelay 간격으로 하나씩 Freeze()를 호출합니다(5.4절 "순차적으로 덮이는" 프리징 웨이브
        /// 연출, 5.6절 체크리스트 근거) - 동시 호출이 아니라 순차 호출임을 보장합니다.
        /// </summary>
        private IEnumerator PlayFreezeSequence(List<IFreezable> targets)
        {
            foreach (var target in targets)
            {
                target?.Freeze();
                if (pureData.FreezeSequenceDelay > 0f)
                {
                    yield return new WaitForSeconds(pureData.FreezeSequenceDelay);
                }
            }

            // 5.4절 종료 연출: 정적인 얼음 조각상이 된 보스 위로 카메라가 서서히 줌아웃 (플레이스홀더).
            Debug.Log("<color=cyan>[AreaFreezeVisualizer] 광역 빙결 시퀀스 종료 - 카메라 줌아웃 재생 (플레이스홀더)</color>");
            sequenceCoroutine = null;
        }
    }
}
