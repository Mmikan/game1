using System;
using System.Collections;
using System.Linq;
using Game1.Gameplay;
using Game1.Network;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Enemies
{
    public sealed class ClientRescueScenario : MonoBehaviour
    {
        private bool failed;
        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-game1-client-rescue-smoke") < 0) yield break;
            var manager = GetComponent<NetworkManager>();
            float deadline = Time.time + 25f;
            while (!manager.IsConnectedClient && Time.time < deadline) yield return null;
            if (!manager.IsConnectedClient) { Application.Quit(1); yield break; }
            if (!manager.IsHost)
            {
                while (manager.IsConnectedClient)
                {
                    var self = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerAvatar>();
                    var target = FindObjectsByType<NetworkPlayerAvatar>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsSpawned && p.OwnerClientId == NetworkManager.ServerClientId);
                    if (target != null && target.LifeState.Value == PlayerLifeState.Downed && self.LifeState.Value == PlayerLifeState.Alive)
                    {
                        self.SetInteractHeldServerRpc(true);
                        self.RequestReviveServerRpc(target.NetworkObjectId);
                        if (self.RescueProgress.Value > 0f) Debug.Log("GAME1_RESCUE_CLIENT_PROGRESS_RECEIVED");
                    }
                    else self.SetInteractHeldServerRpc(false);
                    yield return new WaitForSeconds(0.1f);
                }
                yield break;
            }
            while (manager.ConnectedClients.Count < 2 && Time.time < deadline) yield return null;
            if (manager.ConnectedClients.Count != 2) { Application.Quit(1); yield break; }
            var host = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerAvatar>();
            var client = manager.ConnectedClients.Values.First(c => c.ClientId != NetworkManager.ServerClientId).PlayerObject.GetComponent<NetworkPlayerAvatar>();
            host.transform.position = new Vector3(0f, 0f, -4f);
            client.transform.position = new Vector3(1f, 0f, -4f);
            host.SetSupportServerRpc(client.NetworkObjectId, true);
            yield return null;
            Check(client.IsSupportedByAlly(), "support_started");
            client.transform.position += Vector3.right * 3f;
            yield return null; yield return null;
            Check(!client.IsSupportedByAlly(), "support_distance_expires");
            host.SetSupportServerRpc(host.NetworkObjectId, true);
            Check(host.SupportingClientId.Value == CoopAuthorityRules.NoClient, "self_support_rejected");
            client.transform.position = new Vector3(1f, 0f, -4f);
            host.SetLifeStateOnServer(PlayerLifeState.Downed);
            deadline = Time.time + 5f;
            while (client.RescueProgress.Value <= 0.1f && Time.time < deadline) yield return null;
            Check(client.RescueProgress.Value > 0.1f, "client_rpc_starts_rescue");
            client.SetLifeStateOnServer(PlayerLifeState.Downed);
            yield return new WaitForSeconds(0.3f);
            Check(client.RescueProgress.Value == 0f && host.LifeState.Value == PlayerLifeState.Downed, "rescuer_damage_cancels");
            client.SetLifeStateOnServer(PlayerLifeState.Alive);
            deadline = Time.time + 5f;
            while (host.LifeState.Value != PlayerLifeState.Alive && Time.time < deadline) yield return null;
            Check(host.LifeState.Value == PlayerLifeState.Alive, "client_hold_rescues_host");
            yield return new WaitForSeconds(0.4f);
            Check(client.RescueTarget.Value == CoopAuthorityRules.NoClient && client.RescueProgress.Value == 0f, "completed_rescue_clears_progress");
            Debug.Log($"GAME1_CLIENT_RESCUE_COMPLETE success={!failed}");
            Application.Quit(failed ? 1 : 0);
        }
        private void Check(bool value, string label) { Debug.Log($"GAME1_CLIENT_RESCUE_CHECK {label}={value}"); failed |= !value; }
    }
}
