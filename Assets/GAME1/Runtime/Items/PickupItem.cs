using Game1.Gameplay;
using UnityEngine;

namespace Game1.Items
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PickupItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition definition;
        private Rigidbody body;
        private bool sold;
        private int durability;

        public ItemDefinition Definition => definition;
        public string PromptKey => "hud.interact.pick_up";
        public bool IsHeld { get; private set; }
        public bool IsSold => sold;
        public bool IsBroken => durability <= 0;
        public int CurrentDurability => durability;
        public void Configure(ItemDefinition value) => definition = value;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (definition == null)
            {
                enabled = false;
                Debug.LogError("ItemDefinition is required.", this);
                return;
            }
            durability = definition.Durability;
        }

        public bool CanInteract(LocalPlayerController player) => !sold && !IsHeld && player.HeldItem == null && definition.RequiredCarriers == 1;

        public void Interact(LocalPlayerController player) => player.TryHold(this);

        public void BeginHold(Transform anchor)
        {
            IsHeld = true;
            body.isKinematic = true;
            body.useGravity = false;
            transform.SetParent(anchor, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        public void EndHold(Vector3 velocity)
        {
            transform.SetParent(null, true);
            IsHeld = false;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = velocity;
        }

        public bool TrySell(LocalRunController run)
        {
            if (sold || IsHeld || definition == null) return false;
            sold = true;
            int sellValue = IsBroken ? Mathf.RoundToInt(definition.Value * definition.BrokenValueMultiplier) : definition.Value;
            run.Sell(sellValue);
            gameObject.SetActive(false);
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsHeld || IsBroken || definition == null) return;
            float impact = collision.relativeVelocity.magnitude;
            if (impact <= 2f) return;
            int damage = Mathf.CeilToInt((impact - 2f) * definition.Fragility * 0.1f);
            durability = Mathf.Max(0, durability - damage);
        }
    }
}
