using UnityEngine;

namespace Game1.Enemies
{
    // Prototype SFX only. GameplayNoise is emitted separately by the Ping action.
    public static class AudiblePingCue
    {
        private const int SampleRate = 24000;
        private const int SampleCount = 4320;
        private static AudioClip clip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => clip = null;

        public static void Play(Vector3 position, bool hearingImpaired)
        {
            if (clip == null) clip = CreateClip();
            GameObject sourceObject = new("Audible Ping");
            sourceObject.transform.position = position;
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.clip = clip;
            source.spatialBlend = hearingImpaired ? 0f : 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = 30f;
            float attenuation = hearingImpaired && Camera.main != null
                ? Mathf.Clamp01(1f - Vector3.Distance(Camera.main.transform.position, position) / source.maxDistance)
                : 1f;
            source.volume = hearingImpaired ? Mathf.Pow(10f, -18f / 20f) * attenuation : 1f;
            source.Play();
            Object.Destroy(sourceObject, clip.length + 0.5f);
        }

        private static AudioClip CreateClip()
        {
            AudioClip result = AudioClip.Create("Prototype Ping", SampleCount, 1, SampleRate, false);
            float[] samples = new float[SampleCount];
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)SampleRate;
                float fade = 1f - i / (float)SampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * (880f * time - 610f * time * time)) * fade * fade * Mathf.Min(1f, time * 80f) * 0.25f;
            }
            result.SetData(samples, 0);
            return result;
        }
    }
}
