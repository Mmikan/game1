using Game1.Items;
using UnityEngine;

namespace Game1.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class SellCart : MonoBehaviour
    {
        [SerializeField] private LocalRunController run;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            PickupItem item = other.GetComponentInParent<PickupItem>();
            if (item != null) item.TrySell(run);
        }
    }
}
