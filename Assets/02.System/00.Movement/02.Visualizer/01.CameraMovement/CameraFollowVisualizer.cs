using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;
using Unity.Cinemachine;
using Movement.RefactoredLocomotion;

namespace CameraMovement
{
    /// <summary>
    /// 카메라 비주얼라이저(22번 기획서). 평상시에는 3인칭 숄더뷰 가상 카메라 하나가 피벗(플레이어 머리 높이)을 따라간다.
    ///
    /// 3단계 이벤트 구간(22번 §10)에는 숄더 카메라를 건드리지 않고 이벤트 전용 1인칭 가상 카메라로 Cinemachine 블렌드 전환한다:
    ///  - 오버라이드 ON: 이벤트 vcam 우선순위를 숄더보다 높여 활성화(블렌드 시간 = PureDataCameraSetting.EventCameraBlendTime),
    ///    LateUpdate마다 서비스가 계산한 눈 위치/시선(눈→이벤트 투사체)을 이벤트 vcam 트랜스폼에 적용한다.
    ///    이 컴포넌트의 LateUpdate는 CinemachineBrain(실행 순서 100)보다 먼저 돌아 같은 프레임의 투사체/플레이어 위치가 바로 반영된다.
    ///  - 몸 숨김: 전환 블렌드가 끝나 이벤트 vcam이 화면을 완전히 쥔 뒤 플레이어 몸의 렌더러를 ShadowsOnly로 바꿔 화면에서는 안 보이고
    ///    그림자는 유지한다(렌더링만 숨김). 오버라이드 OFF가 되면 즉시 원래 그림자 모드로 복원한다.
    ///  - 오버라이드 OFF: 우선순위를 낮춰 같은 블렌드로 숄더 vcam에 복귀한다. 숄더 피벗 회전은 이벤트 중 변하지 않아 플레이어 시선 그대로 돌아온다.
    /// </summary>
    public class CameraFollowVisualizer : MonoBehaviour, ICameraFollowVisualizer
    {
        [Header("Pivot & Targets")]
        [SerializeField] private Transform pivotTarget;
        [SerializeField] private Transform playerTransform;

        [Header("Cinemachine Virtual Cameras")]
        [Tooltip("3인칭 숄더뷰 가상 카메라(평상시)")]
        [SerializeField] private CinemachineCamera shoulderVirtualCamera;

        [Tooltip("이벤트 전용 1인칭 가상 카메라. Follow/LookAt 없이 이 컴포넌트가 눈 위치·시선을 트랜스폼에 직접 적용한다. 평상시에는 낮은 우선순위.")]
        [SerializeField] private CinemachineCamera eventVirtualCamera;

        [Header("Cinemachine Brain (Optional)")]
        [SerializeField] private CinemachineBrain cinemachineBrain;

        private ICameraFollowService m_cameraFollowService;
        public ICameraFollowService CameraFollowService => m_cameraFollowService;
        public Transform PivotTarget => pivotTarget;
        public Transform PlayerTransform => playerTransform;
        public CinemachineCamera ShoulderVirtualCamera => shoulderVirtualCamera;
        public CinemachineCamera EventVirtualCamera => eventVirtualCamera;

        private ILocomotionVisualizer m_locomotionVisualizer;

        [Inject]
        public void Construct(
            ICameraFollowService cameraFollowService,
            ILocomotionVisualizer locomotionVisualizer = null)
        {
            m_cameraFollowService = cameraFollowService;

            if (locomotionVisualizer != null)
            {
                m_locomotionVisualizer = locomotionVisualizer;
            }

            // CinemachineBrain은 VContainer에 등록되지 않는 씬 로컬 리소스이므로 DI 대상에서 제외.
            // 인스펙터 직렬화(cinemachineBrain) 또는 Awake()의 Camera.main 탐색으로 확보한다.
        }

        private const string PIVOT_OBJECT_NAME = "CameraPivotTarget";
        private const int PRIORITY_ACTIVE = 100;
        private const int PRIORITY_EVENT_ACTIVE = 200; // 숄더(100)보다 높아 이벤트 중 활성
        private const int PRIORITY_EVENT_IDLE = 0;     // 평상시: 숄더보다 낮아 비활성 취급
        private Transform m_currentLockOnTarget;
        private bool m_isLockedOn;

        // 이벤트 몸 숨김: 숨기기 전 원래 그림자 모드를 기억했다가 복원한다.
        private readonly List<Renderer> m_hiddenRenderers = new List<Renderer>();
        private readonly List<ShadowCastingMode> m_originalShadowModes = new List<ShadowCastingMode>();
        private bool m_bodyHidden;

        private void Awake()
        {
            // 시간 정지(Time.timeScale = 0) 중에도 Cinemachine이 독립 시간으로 동작하도록 설정
            ApplyIgnoreTimeScaleToBrain();
        }

        private void ApplyIgnoreTimeScaleToBrain()
        {
            if (cinemachineBrain != null)
            {
                cinemachineBrain.IgnoreTimeScale = true;
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.TryGetComponent<CinemachineBrain>(out var brain))
            {
                cinemachineBrain = brain;
                cinemachineBrain.IgnoreTimeScale = true;
                return;
            }
        }

        private void LateUpdate()
        {
            EnsurePlayerTransform();

            if (playerTransform == null || pivotTarget == null) return;

            // 숄더 피벗은 항상 플레이어 머리 위치(이벤트 구간에는 숄더 카메라를 쓰지 않지만 복귀를 위해 계속 따라간다)
            pivotTarget.position = playerTransform.position + Vector3.up * 1.4f;

            // 락온 시 피벗 회전축을 타겟 방향으로 정렬 (독립 시간 적용)
            if (m_isLockedOn && m_currentLockOnTarget != null)
            {
                Vector3 toTarget = (m_currentLockOnTarget.position + Vector3.up * 1.0f) - pivotTarget.position;
                if (toTarget != Vector3.zero)
                {
                    Quaternion lockRot = Quaternion.LookRotation(toTarget);
                    pivotTarget.rotation = Quaternion.Slerp(pivotTarget.rotation, lockRot, 15f * Time.unscaledDeltaTime);
                }
            }

            if (m_cameraFollowService != null && m_cameraFollowService.IsEventTargetOverrideActive)
            {
                // 이 LateUpdate는 CinemachineBrain(실행 순서 100)보다 먼저 실행되므로 같은 프레임 위치가 Brain 갱신에 반영된다.
                ApplyEventCameraPose();
                SetBodyHidden(IsEventCameraSettled());
            }
        }

        private void EnsurePlayerTransform()
        {
            if (playerTransform != null) return;

            if (m_locomotionVisualizer != null)
            {
                playerTransform = m_locomotionVisualizer.Transform;
            }

            if (playerTransform != null && m_cameraFollowService != null)
            {
                m_cameraFollowService.SetTarget(playerTransform);
            }
        }

        private void SetupVirtualCamera(CinemachineCamera vcam)
        {
            if (vcam == null || pivotTarget == null) return;
            vcam.Follow = pivotTarget;
            vcam.LookAt = pivotTarget;
        }

        private void Start()
        {
            ApplyIgnoreTimeScaleToBrain();
            EnsurePlayerTransform();

            if (pivotTarget == null)
            {
                GameObject pivotObj = new GameObject(PIVOT_OBJECT_NAME);
                pivotTarget = pivotObj.transform;
                if (playerTransform != null)
                {
                    pivotTarget.position = playerTransform.position + Vector3.up * 1.4f;
                    pivotTarget.rotation = playerTransform.rotation;
                }
            }

            SetupVirtualCamera(shoulderVirtualCamera);
            ActivateShoulderCamera();
            SetupEventCamera();

            // 이벤트 vcam이 낀 블렌드는 회전을 연속으로 보간한다(180도 근처에서 최단 호가 뒤집혀 시선이 튀는 것을 방지).
            CinemachineCore.GetCustomBlender = HandleGetCustomBlender;

            if (m_cameraFollowService != null)
            {
                if (playerTransform != null)
                {
                    m_cameraFollowService.SetTarget(playerTransform);
                }

                m_cameraFollowService.OnLookAnglesChanged += HandleLookAnglesChanged;
                m_cameraFollowService.OnEventTargetOverrideChanged += HandleEventOverrideChanged;
            }

            if (m_locomotionVisualizer != null)
            {
                m_locomotionVisualizer.OnCameraLockOnChanged += HandleLockOnChanged;
            }
        }

        private void OnDestroy()
        {
            SetBodyHidden(false);

            if (CinemachineCore.GetCustomBlender == HandleGetCustomBlender)
            {
                CinemachineCore.GetCustomBlender = null;
            }

            if (m_cameraFollowService != null)
            {
                m_cameraFollowService.OnLookAnglesChanged -= HandleLookAnglesChanged;
                m_cameraFollowService.OnEventTargetOverrideChanged -= HandleEventOverrideChanged;
            }

            if (m_locomotionVisualizer != null)
            {
                m_locomotionVisualizer.OnCameraLockOnChanged -= HandleLockOnChanged;
            }
        }

        private void HandleEventOverrideChanged(bool active)
        {
            if (eventVirtualCamera == null) return;

            // 블렌드 시간은 우선순위를 바꾸기 전에 정한다(Brain은 활성 카메라가 바뀌는 순간 DefaultBlend를 읽는다).
            ApplyEventBlendDefinition();

            if (active)
            {
                // 첫 프레임부터 올바른 눈 위치/시선에서 블렌드가 시작되도록 우선순위를 올리기 전에 포즈를 한 번 적용한다.
                ApplyEventCameraPose();
                eventVirtualCamera.Priority.Value = PRIORITY_EVENT_ACTIVE;
            }
            else
            {
                eventVirtualCamera.Priority.Value = PRIORITY_EVENT_IDLE;
                SetBodyHidden(false);
            }
        }

        private void SetupEventCamera()
        {
            if (eventVirtualCamera == null)
            {
                Debug.LogWarning("[CameraFollowVisualizer] eventVirtualCamera가 연결되지 않아 이벤트 구간에도 숄더 카메라가 유지됩니다.");
                return;
            }

            eventVirtualCamera.Follow = null;
            eventVirtualCamera.LookAt = null;
            eventVirtualCamera.Priority.Value = PRIORITY_EVENT_IDLE;

            // 이벤트 vcam에서 숄더로 복귀하는 블렌드 중에는 이벤트 vcam의 마지막 상태를 스냅샷으로 얼려 쓴다.
            // 복귀 블렌드가 끝나기 전에 이벤트가 다시 시작(재도전)돼 이벤트 vcam을 새 투사체 쪽으로 다시 돌려도, 복귀 중인 화면이 튀지 않는다.
            eventVirtualCamera.BlendHint |= CinemachineCore.BlendHints.FreezeWhenBlendingOut;

            if (m_cameraFollowService != null && m_cameraFollowService.Setting != null)
            {
                LensSettings lens = eventVirtualCamera.Lens;
                lens.FieldOfView = m_cameraFollowService.Setting.EventCameraFieldOfView;
                eventVirtualCamera.Lens = lens;
            }
        }

        private CinemachineBlend.IBlender HandleGetCustomBlender(ICinemachineCamera fromCam, ICinemachineCamera toCam)
        {
            if (eventVirtualCamera == null) return null;

            bool involvesEventCamera = ReferenceEquals(fromCam, eventVirtualCamera) || ReferenceEquals(toCam, eventVirtualCamera);
            return involvesEventCamera ? new ContinuousOrientationBlender() : null;
        }

        private void ApplyEventBlendDefinition()
        {
            if (cinemachineBrain == null || m_cameraFollowService == null || m_cameraFollowService.Setting == null) return;

            float blendTime = m_cameraFollowService.Setting.EventCameraBlendTime;
            cinemachineBrain.DefaultBlend = blendTime > 0f
                ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime)
                : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        }

        /// <summary>서비스가 계산한 눈 위치/시선을 이벤트 vcam 트랜스폼에 적용한다(시선 대상이 없으면 위치만 갱신하고 회전은 유지).</summary>
        private void ApplyEventCameraPose()
        {
            if (eventVirtualCamera == null || playerTransform == null || m_cameraFollowService == null) return;

            bool hasLook = m_cameraFollowService.TryGetEventCameraPose(playerTransform.position, out Vector3 eyePosition, out Quaternion lookRotation);
            Transform vcamTransform = eventVirtualCamera.transform;
            vcamTransform.position = eyePosition;
            if (hasLook)
            {
                vcamTransform.rotation = lookRotation;
            }
        }

        /// <summary>이벤트 vcam이 활성 카메라이고 전환 블렌드가 끝났는지(몸을 숨겨도 되는 시점). Brain이 없으면 항상 true.</summary>
        private bool IsEventCameraSettled()
        {
            if (cinemachineBrain == null) return true;

            return ReferenceEquals(cinemachineBrain.ActiveVirtualCamera, eventVirtualCamera) && !cinemachineBrain.IsBlending;
        }

        /// <summary>플레이어 몸 렌더러를 그림자만 남기고 숨기거나(true) 원래 그림자 모드로 복원한다(false).</summary>
        private void SetBodyHidden(bool hidden)
        {
            if (m_bodyHidden == hidden) return;
            m_bodyHidden = hidden;

            if (hidden)
            {
                if (playerTransform == null)
                {
                    m_bodyHidden = false;
                    return;
                }

                foreach (Renderer r in playerTransform.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;

                    m_hiddenRenderers.Add(r);
                    m_originalShadowModes.Add(r.shadowCastingMode);
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
            else
            {
                for (int i = 0; i < m_hiddenRenderers.Count; i++)
                {
                    if (m_hiddenRenderers[i] != null)
                    {
                        m_hiddenRenderers[i].shadowCastingMode = m_originalShadowModes[i];
                    }
                }

                m_hiddenRenderers.Clear();
                m_originalShadowModes.Clear();
            }
        }

        private void HandleLockOnChanged(bool enable, Transform targetLockOn)
        {
            m_isLockedOn = enable;
            m_currentLockOnTarget = (enable && targetLockOn != null) ? targetLockOn : null;
        }

        private void ActivateShoulderCamera()
        {
            if (shoulderVirtualCamera == null) return;
            shoulderVirtualCamera.gameObject.SetActive(true);
            shoulderVirtualCamera.Priority.Value = PRIORITY_ACTIVE;
        }

        private void HandleLookAnglesChanged(Vector2 lookAngles)
        {
            if (pivotTarget == null) return;

            // 일반 락온 정렬 중에는 마우스 시선각을 적용하지 않는다.
            if (m_isLockedOn) return;

            pivotTarget.rotation = Quaternion.Euler(lookAngles.x, lookAngles.y, 0f);
        }
    }
}
