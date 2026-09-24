using Game1.Config;
using Game1.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkStageState : NetworkBehaviour
    {
        [SerializeField] private RunConfig config;
        public NetworkVariable<int> SoldValue { get; } = new(0);
        public NetworkVariable<float> RemainingSeconds { get; } = new(0f);
        public NetworkVariable<RunState> State { get; } = new(RunState.Playing);
        public int TargetValue => config != null ? config.TargetValue : 0;
        public void Configure(RunConfig value) => config = value;

        public override void OnNetworkSpawn()
        {
            if (!IsServer || config == null) return;
            RemainingSeconds.Value = config.TimeLimitSeconds;
            SoldValue.Value = 0;
            State.Value = RunState.Playing;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null && IsServer) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void Update()
        {
            if (!IsServer || State.Value is RunState.Succeeded or RunState.Failed) return;
            RemainingSeconds.Value = Mathf.Max(0f, RemainingSeconds.Value - Time.deltaTime);
            if (RemainingSeconds.Value <= 0f) State.Value = RunState.Failed;
            bool anySurvivor = false;
            foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out NetworkPlayerAvatar player) &&
                    player.LifeState.Value != PlayerLifeState.Dead) anySurvivor = true;
            if (NetworkManager.ConnectedClients.Count > 0 && !anySurvivor) State.Value = RunState.Failed;
        }

        public bool TrySellOnServer(int value)
        {
            if (!IsServer || value < 0 || State.Value is RunState.Succeeded or RunState.Failed) return false;
            SoldValue.Value += value;
            if (SoldValue.Value >= config.TargetValue) State.Value = RunState.ObjectiveComplete;
            return true;
        }

        public void CompleteEscapeOnServer()
        {
            if (IsServer && State.Value == RunState.ObjectiveComplete) State.Value = RunState.Succeeded;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null &&
                client.PlayerObject.TryGetComponent(out NetworkPlayerAvatar avatar)) avatar.MarkDisconnected();
        }
    }
}
