using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game1.Debuffs;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Network
{
    public sealed class NetworkDebuffDirector : MonoBehaviour
    {
        [SerializeField] private NetworkManager manager;
        [SerializeField] private DebuffDefinition[] definitions;
        private readonly List<DebuffKind> assigned = new();
        private readonly System.Random random = new();

        public void Configure(NetworkManager value, DebuffDefinition[] pool)
        {
            manager = value;
            definitions = pool;
        }

        private void Start()
        {
            if (manager == null) manager = GetComponent<NetworkManager>();
            manager.OnClientConnectedCallback += OnClientConnected;
            manager.OnServerStopped += _ => assigned.Clear();
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (manager.IsServer) StartCoroutine(AssignWhenSpawned(clientId));
        }

        private IEnumerator AssignWhenSpawned(ulong clientId)
        {
            yield return null;
            if (!manager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) || client.PlayerObject == null ||
                !client.PlayerObject.TryGetComponent(out NetworkPlayerAvatar avatar)) yield break;
            IReadOnlyList<DebuffDefinition> pool = definitions?.Where(value => value != null && (int)value.Kind is >= 1 and <= 4).ToArray();
            if (pool == null || !DebuffAllocator.TryAssign(pool, assigned, random, out DebuffDefinition selected)) yield break;
            assigned.Add(selected.Kind);
            avatar.SetDebuffOnServer(selected.Kind);
            Debug.Log($"GAME1_DEBUFF_ASSIGNED client={clientId} debuff={selected.Kind}");
        }
    }
}
