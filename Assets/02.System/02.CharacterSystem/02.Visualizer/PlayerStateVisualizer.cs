using UnityEngine;

namespace CharacterSystem
{
    public class PlayerStateVisualizer : MonoBehaviour, IPlayerStateVisualizer
    {
        [Header("Components")]
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem castingParticle;
        [SerializeField] private AudioSource stateAudioSource;
        [SerializeField] private AudioClip castingSound;

        private static readonly int StateHash = Animator.StringToHash("PlayerState");
        private static readonly int IsCastingHash = Animator.StringToHash("IsCasting");
        private static readonly int CastTriggerHash = Animator.StringToHash("TriggerCast");

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            if (stateAudioSource == null)
            {
                stateAudioSource = GetComponentInChildren<AudioSource>();
            }
        }

        public void OnStateChanged(PlayerStateType previousState, PlayerStateType newState)
        {
            if (animator != null)
            {
                animator.SetInteger(StateHash, (int)newState);
            }

            Debug.Log($"<color=cyan>[PlayerStateVisualizer] State Transition: {previousState} -> {newState}</color>");
        }

        public void PlayCastingMotion(bool isCasting)
        {
            if (animator != null)
            {
                animator.SetBool(IsCastingHash, isCasting);
            }
        }

        public void TriggerCastEffect()
        {
            if (animator != null)
            {
                animator.SetTrigger(CastTriggerHash);
            }

            if (castingParticle != null)
            {
                castingParticle.Play();
            }

            if (stateAudioSource != null && castingSound != null)
            {
                stateAudioSource.PlayOneShot(castingSound);
            }
        }
    }
}
