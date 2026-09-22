using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Network
{
    public sealed class NetworkCoopSmokeDriver : MonoBehaviour
    {
        [SerializeField] private NetworkManager manager;
        private bool enabledByCommandLine;
        public void Configure(NetworkManager value) => manager = value;

        private void Awake()
        {
            enabledByCommandLine = Array.IndexOf(Environment.GetCommandLineArgs(), "-game1-coop-smoke") >= 0;
            if (!enabledByCommandLine) return;
            if (manager == null) manager = GetComponent<NetworkManager>();
            manager.OnClientConnectedCallback += OnConnected;
        }

        private void Start()
        {
            if (enabledByCommandLine) StartCoroutine(RequestCarryWhenReady());
        }

        private void OnConnected(ulong _)
        {
            if (!manager.IsServer || manager.ConnectedClients.Count < 2) return;
            StartCoroutine(StartServerCarrySmoke());
        }

        private IEnumerator StartServerCarrySmoke()
        {
            yield return new WaitForSeconds(1f);
            NetworkCarryItem item = FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.Definition != null && candidate.Definition.ItemId == "giant_block");
            if (item == null)
            {
                Debug.LogError("GAME1_COOP_SMOKE_ITEM_MISSING");
                yield break;
            }
            int index = 0;
            foreach (NetworkClient client in manager.ConnectedClients.Values)
            {
                if (client.PlayerObject != null) client.PlayerObject.transform.position = item.transform.position + Vector3.right * (index++ == 0 ? -0.75f : 0.75f);
            }
            yield return null;
            foreach (ulong clientId in manager.ConnectedClientsIds) item.TryRequestCarryOnServer(clientId);
            Debug.Log($"GAME1_COOP_SMOKE_SHARED={item.IsSharedCarry}");

            NetworkPlayerAvatar rescuer = manager.ConnectedClients[NetworkManager.ServerClientId].PlayerObject.GetComponent<NetworkPlayerAvatar>();
            NetworkPlayerAvatar target = manager.ConnectedClients.Values
                .Where(client => client.ClientId != NetworkManager.ServerClientId)
                .Select(client => client.PlayerObject.GetComponent<NetworkPlayerAvatar>())
                .First();
            target.SetLifeStateOnServer(Game1.Gameplay.PlayerLifeState.Downed);
            target.transform.position = rescuer.transform.position + Vector3.right;
            rescuer.RequestReviveServerRpc(target.NetworkObjectId);
            yield return new WaitForSeconds(2.2f);
            Debug.Log($"GAME1_COOP_SMOKE_RESCUED={target.LifeState.Value == Game1.Gameplay.PlayerLifeState.Alive}");
        }

        private IEnumerator RequestCarryWhenReady()
        {
            yield return new WaitForSeconds(3f);
            NetworkCarryItem item = FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.Definition != null && candidate.Definition.ItemId == "giant_block");
            if (manager.IsConnectedClient && item != null) item.RequestCarryServerRpc();
            if (!manager.IsHost)
            {
                yield return new WaitForSeconds(4f);
                manager.Shutdown();
                Debug.Log("GAME1_COOP_SMOKE_CLIENT_SHUTDOWN");
            }
        }
    }
}
