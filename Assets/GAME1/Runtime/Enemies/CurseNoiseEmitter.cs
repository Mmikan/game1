using Game1.Items;
using Game1.Network;
using UnityEngine;

namespace Game1.Enemies
{
    [RequireComponent(typeof(PickupItem))]
    public sealed class CurseNoiseEmitter : MonoBehaviour
    {
        private PickupItem item;
        private NetworkCarryItem carry;
        private float nextNoise;
        private EnemyActor affected;
        private void Awake() { item = GetComponent<PickupItem>(); carry = GetComponent<NetworkCarryItem>(); }
        private void Update()
        {
            if (!GameplayNoise.HasAuthority || item.IsSold) return;
            bool held = carry != null && carry.IsSpawned ? carry.PrimaryCarrier.Value != CoopAuthorityRules.NoClient && (item.Definition.RequiredCarriers == 1 || carry.IsSharedCarry) : item.IsHeld;
            if (!held) { nextNoise = 0f; ClearBlocks(); return; }
            float interval = item.Definition.ItemId switch { "cursed_doll" => 12f, "windup_clown" => 20f, "black_blocks" => 3f, _ => 0f };
            if (interval <= 0f) return;
            if (nextNoise == 0f) nextNoise = Time.time + interval;
            if (Time.time < nextNoise) return;
            nextNoise = Time.time + interval;
            if (item.Definition.ItemId == "black_blocks")
            {
                ClearBlocks();
                float nearest = float.PositiveInfinity;
                foreach (EnemyActor enemy in FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < nearest) { nearest = distance; affected = enemy; }
                }
                if (affected != null) affected.RevealFromBlocks(3.1f);
                return;
            }
            float radius = item.Definition.ItemId == "windup_clown" ? (carry != null && carry.IsSharedCarry ? 5f : 10f) : 14f;
            GameplayNoise.Emit(transform.position, radius, NoiseKind.Curse);
        }
        private void ClearBlocks() { if (affected != null) affected.RevealFromBlocks(0f); affected = null; }
        private void OnDisable() => ClearBlocks();
    }
}
