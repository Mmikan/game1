using Game1.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkDoorState : NetworkBehaviour
    {
        [SerializeField] private Transform pivot;
        private Vector3 closedForward;
        private void Awake() => closedForward = transform.forward;
        public NetworkVariable<float> OpenAngle { get; } = new(0f);
        public NetworkVariable<ulong> HeldByClient { get; } = new(CoopAuthorityRules.NoClient);
        public void Configure(Transform value) => pivot = value;
        // The negative-Z side of the closed doorway is its holding side.
        public bool IsHoldingSide(Vector3 position) => Vector3.Dot(position - transform.position, closedForward) <= 0f;
        public string InteractionPromptKey(Vector3 position, ulong viewer)
        {
            if (HeldByClient.Value != CoopAuthorityRules.NoClient)
                return HeldByClient.Value == viewer ? "hud.door.holding" : "hud.door.busy";
            if (IsHoldingSide(position)) return OpenAngle.Value > 1f ? "hud.door.hold_hint" : "hud.door.open_hold_hint";
            return OpenAngle.Value > 1f ? "hud.door.close_hint" : "hud.door.open_hint";
        }
        public override void OnNetworkDespawn()
        {
            if (TryGetComponent(out InteractableDoor localDoor)) localDoor.SetOpenState(OpenAngle.Value > 1f);
        }

        private void Update()
        {
            if (!IsSpawned) return;
            if (IsServer && HeldByClient.Value != CoopAuthorityRules.NoClient && !CanHold(HeldByClient.Value))
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
            TrySetHeldOnServer(rpc.Receive.SenderClientId, held);
        }

        public bool TrySetHeldOnServer(ulong sender, bool held)
        {
            if (!IsServer) return false;
            if (!held && HeldByClient.Value == sender) { HeldByClient.Value = CoopAuthorityRules.NoClient; return true; }
            else if (held && (HeldByClient.Value == CoopAuthorityRules.NoClient || HeldByClient.Value == sender) &&
                     OpenAngle.Value > 90f && CanHold(sender)) { HeldByClient.Value = sender; return true; }
            return false;
        }

        private bool TryValidate(ulong clientId, float range)
        {
            return NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null &&
                   client.PlayerObject.TryGetComponent(out NetworkPlayerAvatar player) &&
                   CoopAuthorityRules.CanInteract(player.LifeState.Value, player.transform.position, transform.position, range);
        }

        private bool CanHold(ulong clientId) => TryValidate(clientId, 3f) &&
            IsHoldingSide(NetworkManager.ConnectedClients[clientId].PlayerObject.transform.position);
    }
}
