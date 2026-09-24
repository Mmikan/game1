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
            if (!IsSpawned) return;
            if (IsServer && HeldByClient.Value != CoopAuthorityRules.NoClient && !TryValidate(HeldByClient.Value, 3f))
                HeldByClient.Value = CoopAuthorityRules.NoClient;
            if (pivot != null) pivot.localRotation = Quaternion.RotateTowards(pivot.localRotation, Quaternion.Euler(0f, OpenAngle.Value, 0f), 180f * Time.deltaTime);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestToggleServerRpc(ServerRpcParams rpc = default)
        {
            TryToggleOnServer(rpc.Receive.SenderClientId);
        }

        public bool TryToggleOnServer(ulong clientId)
        {
            if (!IsServer || !TryValidate(clientId, 3f) || HeldByClient.Value != CoopAuthorityRules.NoClient) return false;
            OpenAngle.Value = OpenAngle.Value > 1f ? 0f : 100f;
            Game1.Enemies.GameplayNoise.Emit(transform.position, 4f, Game1.Enemies.NoiseKind.Door);
            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetHeldServerRpc(bool held, ServerRpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!held && HeldByClient.Value == sender) HeldByClient.Value = CoopAuthorityRules.NoClient;
            else if (held && (HeldByClient.Value == CoopAuthorityRules.NoClient || HeldByClient.Value == sender) &&
                     OpenAngle.Value > 90f && TryValidate(sender, 3f)) HeldByClient.Value = sender;
        }

        private bool TryValidate(ulong clientId, float range)
        {
            return NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null &&
                   client.PlayerObject.TryGetComponent(out NetworkPlayerAvatar player) &&
                   CoopAuthorityRules.CanInteract(player.LifeState.Value, player.transform.position, transform.position, range);
        }
    }
}
