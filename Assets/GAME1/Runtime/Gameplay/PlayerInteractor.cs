using UnityEngine;
using UnityEngine.InputSystem;

namespace Game1.Gameplay
{
    [RequireComponent(typeof(LocalPlayerController))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float range = 3f;
        [SerializeField] private LayerMask mask = ~0;
        [SerializeField, Min(0.05f)] private float holdSeconds = 0.25f;
        private LocalPlayerController player;
        private IInteractable heldTarget;
        private float heldTime;

        public IInteractable Current { get; private set; }
        public float Progress01 => heldTarget == null ? 0f : Mathf.Clamp01(heldTime / holdSeconds);

        private void Awake() => player = GetComponent<LocalPlayerController>();

        private void Update()
        {
            Current = FindTarget();
            if (Keyboard.current == null || Current?.CanInteract(player) != true || !Keyboard.current.eKey.isPressed)
            {
                heldTarget = null;
                heldTime = 0f;
                return;
            }

            if (!ReferenceEquals(heldTarget, Current))
            {
                heldTarget = Current;
                heldTime = 0f;
            }
            heldTime += Time.deltaTime;
            if (heldTime < holdSeconds) return;
            heldTarget.Interact(player);
            heldTarget = null;
            heldTime = 0f;
        }

        private IInteractable FindTarget()
        {
            Ray ray = new(player.ViewCamera.transform.position, player.ViewCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore)) return null;
            return hit.collider.GetComponentInParent<IInteractable>();
        }
    }
}
