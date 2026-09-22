using Game1.Gameplay;
using Game1.Items;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Network
{
    [RequireComponent(typeof(NetworkObject), typeof(Rigidbody))]
    public sealed class NetworkCarryItem : NetworkBehaviour
    {
        [SerializeField] private ItemDefinition definition;
        private Rigidbody body;
        public NetworkVariable<ulong> PrimaryCarrier { get; } = new(CoopAuthorityRules.NoClient);
        public NetworkVariable<ulong> SecondaryCarrier { get; } = new(CoopAuthorityRules.NoClient);
        public ItemDefinition Definition => definition;
        public void Configure(ItemDefinition value) => definition = value;
        public bool IsSharedCarry => SecondaryCarrier.Value != CoopAuthorityRules.NoClient;

        private void Awake() => body = GetComponent<Rigidbody>();

        public override void OnNetworkSpawn()
        {
            if (IsServer) NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null && IsServer) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void FixedUpdate()
        {
            if (!IsServer || PrimaryCarrier.Value == CoopAuthorityRules.NoClient) return;
            if (!TryGetCarrier(PrimaryCarrier.Value, out NetworkPlayerAvatar first)) { Release(); return; }
            if (SecondaryCarrier.Value == CoopAuthorityRules.NoClient)
            {
                Vector3 tremor = first.Debuff.Value == Game1.Debuffs.DebuffKind.Tremor ? TremorOffset(0.18f) : Vector3.zero;
                transform.position = first.transform.position + first.transform.forward * 1.1f + Vector3.up + tremor;
                return;
            }
            if (!TryGetCarrier(SecondaryCarrier.Value, out NetworkPlayerAvatar second) || Vector3.Distance(first.transform.position, second.transform.position) > 3f)
            {
                Release();
                return;
            }
            bool tremorCarrier = first.Debuff.Value == Game1.Debuffs.DebuffKind.Tremor || second.Debuff.Value == Game1.Debuffs.DebuffKind.Tremor;
            transform.position = (first.transform.position + second.transform.position) * 0.5f + (first.transform.forward + second.transform.forward).normalized * 0.8f + Vector3.up +
                                 (tremorCarrier ? TremorOffset(0.04f) : Vector3.zero);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestCarryServerRpc(ServerRpcParams rpc = default)
        {
            TryRequestCarryOnServer(rpc.Receive.SenderClientId);
        }

        public bool TryRequestCarryOnServer(ulong sender)
        {
            if (!IsServer) return false;
            if (!TryGetCarrier(sender, out NetworkPlayerAvatar player) ||
                !CoopAuthorityRules.CanInteract(player.LifeState.Value, player.transform.position, transform.position, 2f)) return false;
            if (PrimaryCarrier.Value == CoopAuthorityRules.NoClient)
            {
                PrimaryCarrier.Value = sender;
                body.isKinematic = true;
                return true;
            }
            if (definition != null && definition.RequiredCarriers == 2 && PrimaryCarrier.Value != sender &&
                TryGetCarrier(PrimaryCarrier.Value, out NetworkPlayerAvatar first) &&
                CoopAuthorityRules.CanStartSharedCarry(first.transform.position, player.transform.position, transform.position))
            {
                SecondaryCarrier.Value = sender;
                Debug.Log($"GAME1_SHARED_CARRY_STARTED item={definition?.ItemId} first={PrimaryCarrier.Value} second={sender}");
                return true;
            }
            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReleaseServerRpc(ServerRpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (sender == PrimaryCarrier.Value || sender == SecondaryCarrier.Value) Release();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestTransferServerRpc(ulong targetClientId, ServerRpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != PrimaryCarrier.Value || SecondaryCarrier.Value != CoopAuthorityRules.NoClient ||
                !TryGetCarrier(PrimaryCarrier.Value, out NetworkPlayerAvatar source) || !TryGetCarrier(targetClientId, out NetworkPlayerAvatar target) ||
                !CoopAuthorityRules.CanInteract(target.LifeState.Value, target.transform.position, source.transform.position, 2f)) return;
            PrimaryCarrier.Value = targetClientId;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == PrimaryCarrier.Value || clientId == SecondaryCarrier.Value) Invoke(nameof(Release), 0.2f);
        }

        private void Release()
        {
            bool wasShared = IsSharedCarry;
            PrimaryCarrier.Value = CoopAuthorityRules.NoClient;
            SecondaryCarrier.Value = CoopAuthorityRules.NoClient;
            body.isKinematic = false;
            body.useGravity = true;
            if (wasShared) Debug.Log($"GAME1_SHARED_CARRY_RELEASED item={definition?.ItemId}");
        }

        private bool TryGetCarrier(ulong clientId, out NetworkPlayerAvatar player)
        {
            player = null;
            return NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null && client.PlayerObject.TryGetComponent(out player);
        }

        private static Vector3 TremorOffset(float magnitude) =>
            new(Mathf.Sin(Time.time * 17f) * magnitude, Mathf.Cos(Time.time * 19f) * magnitude, 0f);
    }
}
