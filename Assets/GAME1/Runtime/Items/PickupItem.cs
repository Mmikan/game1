using Game1.Gameplay;
using Game1.Enemies;
using Game1.Network;
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
        private float nextImpactDamageMultiplier = 1f;
        private NetworkCarryItem networkItem;
        private bool Online => networkItem != null && networkItem.IsSpawned;

        public ItemDefinition Definition => definition;
        public string PromptKey => "hud.interact.pick_up";
        public bool IsHeld { get; private set; }
        public EnemyActor EnemyHolder { get; private set; }

        public bool TryClaimByEnemy(EnemyActor enemy)
        {
            if (!GameplayNoise.HasAuthority || IsSold || IsHeld || EnemyHolder != null || enemy == null ||
                (Online && (networkItem.PrimaryCarrier.Value != CoopAuthorityRules.NoClient || networkItem.Stolen.Value))) return false;
            EnemyHolder = enemy;
            if (Online) networkItem.Stolen.Value = true;
            body.isKinematic = true;
            return true;
        }

        public void ReleaseFromEnemy(Vector3 position)
        {
            if (!GameplayNoise.HasAuthority || EnemyHolder == null) return;
            EnemyHolder = null;
            if (Online) networkItem.Stolen.Value = false;
            transform.position = position;
            body.isKinematic = false;
            body.useGravity = true;
        }
        public bool IsSold => Online ? networkItem.Sold.Value : sold;
        public bool IsBroken => durability <= 0;
        public int CurrentDurability => durability;
        public void Configure(ItemDefinition value) => definition = value;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            networkItem = GetComponent<NetworkCarryItem>();
            if (definition == null)
            {
                enabled = false;
                Debug.LogError("ItemDefinition is required.", this);
                return;
            }
            durability = definition.Durability;
        }

        public bool CanInteract(LocalPlayerController player) => !Online && EnemyHolder == null && !sold && !IsHeld && player.HeldItem == null && definition.RequiredCarriers == 1;

        public void Interact(LocalPlayerController player) => player.TryHold(this);

        public void BeginHold(Transform anchor, float impactDamageMultiplier = 1f)
        {
            if (Online) return;
            IsHeld = true;
            body.isKinematic = true;
            body.useGravity = false;
            transform.SetParent(anchor, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            nextImpactDamageMultiplier = Mathf.Max(1f, impactDamageMultiplier);
        }

        public void EndHold(Vector3 velocity)
        {
            if (Online) return;
            transform.SetParent(null, true);
            IsHeld = false;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = velocity;
        }

        public bool TrySell(LocalRunController run)
        {
            if (Online || sold || IsHeld || EnemyHolder != null || definition == null) return false;
            sold = true;
            int sellValue = IsBroken ? Mathf.RoundToInt(definition.Value * definition.BrokenValueMultiplier) : definition.Value;
            run.Sell(sellValue);
            gameObject.SetActive(false);
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!GameplayNoise.HasAuthority || IsSold || IsHeld || IsBroken || definition == null) return;
            float impact = collision.relativeVelocity.magnitude;
            if (impact <= 2f) return;
            GameplayNoise.Emit(transform.position, definition.NoiseRadius, NoiseKind.Drop);
            int damage = Mathf.CeilToInt((impact - 2f) * definition.Fragility * 0.1f * nextImpactDamageMultiplier);
            durability = Mathf.Max(0, durability - damage);
            nextImpactDamageMultiplier = 1f;
        }
    }
}
