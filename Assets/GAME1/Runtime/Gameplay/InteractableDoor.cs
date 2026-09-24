using UnityEngine;

namespace Game1.Gameplay
{
    public sealed class InteractableDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float turnSpeed = 180f;
        private bool open;
        public void Configure(Transform value) => pivot = value;

        public string PromptKey => open ? "hud.interact.close_door" : "hud.interact.open_door";
        public bool CanInteract(LocalPlayerController player) => pivot != null;
        public void Interact(LocalPlayerController player)
        {
            open = !open;
            Game1.Enemies.GameplayNoise.Emit(transform.position, 4f, Game1.Enemies.NoiseKind.Door);
        }

        private void Update()
        {
            if (TryGetComponent(out Game1.Network.NetworkDoorState network) && network.IsSpawned) return;
            if (pivot == null) return;
            Quaternion target = Quaternion.Euler(0f, open ? openAngle : 0f, 0f);
            pivot.localRotation = Quaternion.RotateTowards(pivot.localRotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
