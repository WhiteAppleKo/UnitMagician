using Unity.Cinemachine;
using UnityEngine;

namespace CameraMovement
{
    /// <summary>
    /// 이벤트 1인칭 가상 카메라가 낀 Cinemachine 블렌드의 회전 보간을 "연속"으로 만드는 커스텀 블렌더입니다([V] 연출 도우미).
    ///
    /// 기본 블렌더(CameraState.Lerp)는 회전을 Quaternion.Slerp(최단 호)로 보간합니다. 플레이어가 보스를 보고 있을 때 이벤트 시선(발사 지점 L 방향)은
    /// 거의 정확히 뒤쪽, 즉 숄더 시선과 거의 180도 반대라 두 회전 사이 각도가 블렌드 도중 180도를 가로지르면 최단 호의 방향이 한 프레임에
    /// 뒤집혀 시선이 크게 튑니다(44번 QA 결함 A/B와 같은 원인). 이 블렌더는 위치/FOV 등은 기본 보간 그대로 쓰고, 회전만 "최단 호"와
    /// "반대쪽 호" 두 후보 중 직전 프레임 출력에 더 가까운 쪽을 고릅니다. 블렌드 인스턴스마다 하나씩 만들어지므로(블렌드 시작마다 새로 생성)
    /// 시작 프레임엔 최단 호이고, 이후엔 그 방향을 유지합니다. 같은 프레임에 State가 여러 번 읽혀도 같은 후보가 선택돼 결과가 같습니다.
    /// 블렌드 끝(t=1)에서는 두 후보가 같은 회전이라 도착 자세가 달라지지 않습니다.
    /// (Quaternion.Slerp는 항상 최단 호만 돌려주므로 반대쪽 호는 상대 회전의 각도에서 360도를 빼서 직접 만든다.)
    /// </summary>
    public class ContinuousOrientationBlender : CinemachineBlend.IBlender
    {
        private Quaternion m_lastOrientation;
        private bool m_hasLast;

        public CameraState GetIntermediateState(ICinemachineCamera camA, ICinemachineCamera camB, float t)
        {
            CameraState stateA = camA.State;
            CameraState stateB = camB.State;
            CameraState state = CameraState.Lerp(stateA, stateB, t);

            Quaternion qa = stateA.GetCorrectedOrientation();
            Quaternion qb = stateB.GetCorrectedOrientation();

            // qa → qb 상대 회전을 (축, [-180, 180] 각도)로 분해한다. 최단 호 = 이 각도, 반대쪽 호 = 각도 ∓ 360도.
            (qb * Quaternion.Inverse(qa)).ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;

            Quaternion shortArc = Quaternion.AngleAxis(angle * t, axis) * qa;
            Quaternion chosen = shortArc;
            if (m_hasLast && axis.sqrMagnitude > 0.5f)
            {
                float otherAngle = angle >= 0f ? angle - 360f : angle + 360f;
                Quaternion longArc = Quaternion.AngleAxis(otherAngle * t, axis) * qa;
                if (Quaternion.Angle(m_lastOrientation, longArc) < Quaternion.Angle(m_lastOrientation, shortArc))
                {
                    chosen = longArc;
                }
            }

            m_lastOrientation = chosen;
            m_hasLast = true;

            state.RawOrientation = chosen;
            state.OrientationCorrection = Quaternion.identity;
            return state;
        }
    }
}
