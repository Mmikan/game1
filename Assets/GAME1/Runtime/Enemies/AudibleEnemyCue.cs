using Game1.Debuffs;
using UnityEngine;

namespace Game1.Enemies
{
    // Temporary audible cues. The AI never reads this AudioSource or its volume.
    [RequireComponent(typeof(EnemyActor), typeof(AudioSource))]
    public sealed class AudibleEnemyCue : MonoBehaviour
    {
        private AudioSource source;
        private EnemyActor enemy;
        private AudioClip step;
        private float nextStep;
        private Vector3 previous;
        private void Awake()
        {
            enemy = GetComponent<EnemyActor>(); source = GetComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 1f; source.maxDistance = 15f;
            source.rolloffMode = AudioRolloffMode.Linear;
            step = AudioClip.Create("Prototype footstep", 2400, 1, 24000, false);
            float[] samples = new float[2400];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * 0.034f) * Mathf.Exp(-i / 400f) * 0.2f;
            step.SetData(samples, 0); previous = transform.position;
        }
        private void Update()
        {
            bool moving = Vector3.Distance(previous, transform.position) > 0.001f;
            previous = transform.position;
            if (!moving || Time.time < nextStep || enemy.State is EnemyState.Freeze or EnemyState.Stagger) return;
            nextStep = Time.time + 0.5f;
            bool impaired = Game1.Network.NetworkPlayerAvatar.ListenerDebuff == DebuffKind.HearingLoss;
            source.spatialBlend = impaired ? 0f : 1f;
            float attenuation = impaired && Camera.main != null ? Mathf.Clamp01(1f - Vector3.Distance(Camera.main.transform.position, transform.position) / 15f) : 1f;
            source.PlayOneShot(step, impaired ? Mathf.Pow(10f, -18f / 20f) * attenuation : 1f);
        }
        private void OnDestroy() { if (step != null) Destroy(step); }
    }
}
