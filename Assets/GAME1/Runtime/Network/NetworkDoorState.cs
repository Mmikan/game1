using Game1.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkDoorState : NetworkBehaviour
    {
        [SerializeField] private Transform pivot;
        public NetworkVariable<float> OpenAngle { get; } = new(0f);
        public NetworkVariable<ulong> HeldByClient { get; } = new(CoopAuthorityRules.NoClient);
        public void Configure(Transform value) => pivot = value;

        private void Update()
        {
            if (pivot != null) pivot.localRotation = Quaternion.RotateTowards(pivot.localRotation, Quaternion.Euler(0f, OpenAngle.Value, 0f), 180f * Time.deltaTime);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestToggleServerRpc(ServerRpcParams rpc = default)
        {
            if (!TryValidate(rpc.Receive.SenderClientId, 3f)) return;
            OpenAngle.Value = OpenAngle.Value > 1f ? 0f : 100f;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetHeldServerRpc(bool held, ServerRpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!held && HeldByClient.Value == sender) HeldByClient.Value = CoopAuthorityRules.NoClient;
            else if (held && OpenAngle.Value > 90f && TryValidate(sender, 3f)) HeldByClient.Value = sender;
        }

        private bool TryValidate(ulong clientId, float range)
        {
            return NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null &&
                   client.PlayerObject.TryGetComponent(out NetworkPlayerAvatar player) &&
                   CoopAuthorityRules.CanInteract(player.LifeState.Value, player.transform.position, transform.position, range);
        }
    }
}
