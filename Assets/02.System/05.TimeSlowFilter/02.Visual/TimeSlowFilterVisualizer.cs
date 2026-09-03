using System.Collections;
using UnityEngine;
using VContainer;

namespace TimeSlowFilterSystem
{
    public class TimeSlowFilterVisualizer : MonoBehaviour, ITimeSlowFilterVisualizer
    {
        [Header("Data Reference")]
        [SerializeField] private PureDataTimeSlowFilter pureData;

        [Header("Material Reference (Optional)")]
        [Tooltip("전역 셰이더 변수 외에 특정 머티리얼에 직접 적용이 필요한 경우 연결")]
        [SerializeField] private Material filterMaterial;

        private Material runtimeMaterial;
        private int shaderPropId;
        private int contrastPropId;
        private Coroutine transitionCoroutine;
        private float currentIntensity = 0f;

        [Inject]
        public void Construct(PureDataTimeSlowFilter injectedPureData)
        {
            if (injectedPureData != null)
            {
                pureData = injectedPureData;
            }
        }

        private void Awake()
        {
            InitializePropertyIds();

            if (filterMaterial != null)
            {
                runtimeMaterial = new Material(filterMaterial);
            }

            SetFilterIntensity(0f);
        }

        private void OnDisable()
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            SetFilterIntensity(0f);
        }

        private void OnDestroy()
        {
            SetFilterIntensity(0f);

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
        }

        private void InitializePropertyIds()
        {
            string shaderName = pureData != null ? pureData.ShaderPropertyName : "_DesaturateAmount";
            string contrastName = pureData != null ? pureData.ContrastPropertyName : "_Contrast";

            shaderPropId = Shader.PropertyToID(shaderName);
            contrastPropId = Shader.PropertyToID(contrastName);
        }

        public void SetFilterIntensity(float intensity)
        {
            currentIntensity = Mathf.Clamp01(intensity);
            float contrast = pureData != null ? pureData.Contrast : 1.15f;

            if (shaderPropId == 0)
            {
                InitializePropertyIds();
            }

            Shader.SetGlobalFloat(shaderPropId, currentIntensity);
            Shader.SetGlobalFloat(contrastPropId, contrast);

            if (runtimeMaterial != null)
            {
                runtimeMaterial.SetFloat(shaderPropId, currentIntensity);
                runtimeMaterial.SetFloat(contrastPropId, contrast);
            }
        }

        public void PlayFilterTransition(bool isEnter)
        {
            float duration = pureData != null ? pureData.TransitionDuration : 0.3f;
            PlayFilterTransition(isEnter, duration);
        }

        public void PlayFilterTransition(bool isEnter, float duration)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
                transitionCoroutine = null;
            }

            float target = isEnter ? 1f : 0f;
            transitionCoroutine = StartCoroutine(TransitionRoutine(target, duration));
        }

        private IEnumerator TransitionRoutine(float targetIntensity, float duration)
        {
            float startIntensity = currentIntensity;

            if (duration <= 0f)
            {
                SetFilterIntensity(targetIntensity);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float val = Mathf.Lerp(startIntensity, targetIntensity, t);
                SetFilterIntensity(val);
                yield return null;
            }

            SetFilterIntensity(targetIntensity);
            transitionCoroutine = null;
        }
    }
}
