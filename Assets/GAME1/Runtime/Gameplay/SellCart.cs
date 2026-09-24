using Game1.Items;
using Game1.Network;
using UnityEngine;

namespace Game1.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class SellCart : MonoBehaviour
    {
        [SerializeField] private LocalRunController run;
        public void Configure(LocalRunController value) => run = value;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            PickupItem item = other.GetComponentInParent<PickupItem>();
            if (item == null) return;
            if (item.TryGetComponent(out NetworkCarryItem network) && network.IsSpawned)
            {
                if (network.IsServer) network.TrySellOnServer(FindFirstObjectByType<NetworkStageState>());
            }
            else item.TrySell(run);
        }
    }
}
