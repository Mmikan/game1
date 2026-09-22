using System.Collections;
using Game1.Gameplay;
using Game1.Debuffs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game1.Network
{
    public sealed class NetworkPlayerAvatar : NetworkBehaviour
    {
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float inputSendRate = 20f;
        private float nextInputTime;
        private Vector2 serverInput;

        public NetworkVariable<PlayerLifeState> LifeState { get; } = new(PlayerLifeState.Alive);
        public NetworkVariable<bool> FlashlightOn { get; } = new(false);
        public NetworkVariable<ulong> SupportingClientId { get; } = new(CoopAuthorityRules.NoClient);
        public NetworkVariable<DebuffKind> Debuff { get; } = new(DebuffKind.None);

        public void SetDebuffOnServer(DebuffKind value)
        {
            if (IsServer) Debuff.Value = value;
        }

        private void Update()
        {
            if (IsOwner && Keyboard.current != null && Time.unscaledTime >= nextInputTime)
            {
                nextInputTime = Time.unscaledTime + 1f / inputSendRate;
                Vector2 input = new(
                    (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                    (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));
                SubmitInputServerRpc(Vector2.ClampMagnitude(input, 1f));
                if (Keyboard.current.fKey.wasPressedThisFrame) SetFlashlightServerRpc(!FlashlightOn.Value);
            }

            if (IsServer && LifeState.Value == PlayerLifeState.Alive)
            {
                Vector3 movement = transform.right * serverInput.x + transform.forward * serverInput.y;
                float effectiveSpeed = Debuff.Value == DebuffKind.HeavyBreath && IsSupportedByAlly() ? 4.6f : walkSpeed;
                transform.position += movement * (effectiveSpeed * Time.deltaTime);
            }
        }

        [ServerRpc]
        private void SubmitInputServerRpc(Vector2 input, ServerRpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != OwnerClientId || input.sqrMagnitude > 1.01f || LifeState.Value != PlayerLifeState.Alive) return;
            serverInput = Vector2.ClampMagnitude(input, 1f);
        }

        [ServerRpc]
        private void SetFlashlightServerRpc(bool enabled, ServerRpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId == OwnerClientId && LifeState.Value == PlayerLifeState.Alive)
                FlashlightOn.Value = enabled;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestReviveServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpc = default)
        {
            if (!TryGetPlayer(rpc.Receive.SenderClientId, out NetworkPlayerAvatar rescuer) ||
                !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject) ||
                !targetObject.TryGetComponent(out NetworkPlayerAvatar target) || target.LifeState.Value != PlayerLifeState.Downed ||
                !CoopAuthorityRules.CanInteract(rescuer.LifeState.Value, rescuer.transform.position, target.transform.position, 1.5f)) return;
            StartCoroutine(ReviveAfterDelay(rescuer, target));
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetSupportServerRpc(ulong targetNetworkObjectId, bool active, ServerRpcParams rpc = default)
        {
            if (!TryGetPlayer(rpc.Receive.SenderClientId, out NetworkPlayerAvatar supporter)) return;
            if (!active)
            {
                supporter.SupportingClientId.Value = CoopAuthorityRules.NoClient;
                return;
            }
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject) ||
                !targetObject.TryGetComponent(out NetworkPlayerAvatar target) || target.LifeState.Value != PlayerLifeState.Alive ||
                !CoopAuthorityRules.CanInteract(supporter.LifeState.Value, supporter.transform.position, target.transform.position, 1.2f)) return;
            supporter.SupportingClientId.Value = target.OwnerClientId;
        }

        public void MarkDisconnected()
        {
            if (!IsServer) return;
            LifeState.Value = PlayerLifeState.Dead;
            serverInput = Vector2.zero;
            SupportingClientId.Value = CoopAuthorityRules.NoClient;
        }

        public void SetLifeStateOnServer(PlayerLifeState state)
        {
            if (IsServer) LifeState.Value = state;
        }

        private IEnumerator ReviveAfterDelay(NetworkPlayerAvatar rescuer, NetworkPlayerAvatar target)
        {
            float deadline = Time.time + 2f;
            while (Time.time < deadline)
            {
                if (!CoopAuthorityRules.CanInteract(rescuer.LifeState.Value, rescuer.transform.position, target.transform.position, 1.5f)) yield break;
                yield return null;
            }
            if (target.LifeState.Value == PlayerLifeState.Downed) target.LifeState.Value = PlayerLifeState.Alive;
        }

        private bool TryGetPlayer(ulong clientId, out NetworkPlayerAvatar player)
        {
            player = null;
            return NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) &&
                   client.PlayerObject != null && client.PlayerObject.TryGetComponent(out player);
        }

        public bool IsSupportedByAlly()
        {
            if (!IsServer) return false;
            foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out NetworkPlayerAvatar candidate) &&
                    candidate.SupportingClientId.Value == OwnerClientId) return true;
            return false;
        }
    }
}
