using UnityEngine;

namespace Game1.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class ExitZone : MonoBehaviour
    {
        [SerializeField] private LocalRunController run;
        [SerializeField, Min(0.1f)] private float requiredSeconds = 3f;
        private float progress;
        private LocalPlayerController occupant;

        public float Progress01 => Mathf.Clamp01(progress / requiredSeconds);

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void Update()
        {
            if (occupant == null || !run.ExitUnlocked)
            {
                progress = 0f;
                return;
            }

            progress += Time.deltaTime;
            if (progress >= requiredSeconds) run.CompleteEscape();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out LocalPlayerController player)) occupant = player;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out LocalPlayerController player) && player == occupant)
            {
                occupant = null;
                progress = 0f;
            }
        }
    }
}
