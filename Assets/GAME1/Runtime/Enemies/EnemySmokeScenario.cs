using System;
using System.Collections;
using System.Linq;
using Game1.Gameplay;
using Game1.Network;
using Game1.Items;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Enemies
{
    public sealed class EnemySmokeScenario : MonoBehaviour
    {
        private bool failed;
        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-game1-enemy-smoke") < 0) yield break;
            NetworkManager manager = GetComponent<NetworkManager>();
            float deadline = Time.realtimeSinceStartup + 25f;
            while (!manager.IsConnectedClient && Time.realtimeSinceStartup < deadline) yield return null;
            if (!manager.IsHost)
            {
                Check(FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None).All(item => item.GetComponent<Rigidbody>().isKinematic), "client_items_kinematic");
                foreach (NetworkCarryItem item in FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None))
                {
                    item.Stolen.OnValueChanged += (_, current) => Debug.Log($"GAME1_ITEM_CLIENT_STOLEN item={item.name} value={current}");
                    item.Sold.OnValueChanged += (_, current) => Debug.Log($"GAME1_ITEM_CLIENT_SOLD item={item.name} value={current}");
                }
                foreach (EnemyActor enemy in FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
                    enemy.ReplicatedState.OnValueChanged += (_, current) => Debug.Log($"GAME1_ENEMY_CLIENT_STATE kind={enemy.Kind} state={current}");
                yield break;
            }
            while (manager.ConnectedClients.Count < 2 && Time.realtimeSinceStartup < deadline) yield return null;
            if (!Check(manager.ConnectedClients.Count == 2, "two_clients")) yield break;
            NetworkPlayerAvatar[] players = manager.ConnectedClients.Values.Select(c => c.PlayerObject.GetComponent<NetworkPlayerAvatar>()).ToArray();
            EnemyActor mannequin = FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).First(e => e.Kind == EnemyKind.Mannequin);
            EnemyActor collector = FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).First(e => e.Kind == EnemyKind.Collector);
            players[0].transform.position = new Vector3(0f, 0f, 26f);
            players[1].transform.position = new Vector3(-3f, 0f, -4f);
            // Continuous server-authored light poses exercise actual perception, not forced AI states.
            deadline = Time.realtimeSinceStartup + 5f;
            while (mannequin.State != EnemyState.Freeze && Time.realtimeSinceStartup < deadline)
            {
                players[0].SetLightOnServer(true, (mannequin.transform.position - players[0].transform.position).normalized);
                yield return null;
            }
            Check(mannequin.State == EnemyState.Freeze, "mannequin_freeze");
            Vector3 frozen = mannequin.transform.position;
            yield return new WaitForSeconds(0.6f);
            Check(Vector3.Distance(frozen, mannequin.transform.position) < 0.05f, "freeze_stops_navigation");
            yield return new WaitForSeconds(5.6f);
            Check(mannequin.State != EnemyState.Freeze, "freeze_six_second_cap");
            bool immune = true;
            deadline = Time.time + 3.5f;
            while (Time.time < deadline)
            {
                players[0].transform.position = mannequin.transform.position + Vector3.forward * 12f;
                players[0].SetLightOnServer(true, Vector3.back);
                immune &= mannequin.State != EnemyState.Freeze;
                yield return null;
            }
            Check(immune, "freeze_immunity");
            players[0].SetLightOnServer(false, Vector3.forward);
            players[0].transform.position = new Vector3(0f, 0f, -4f);
            GameplayNoise.Emit(collector.transform.position, 4f, NoiseKind.Curse);
            deadline = Time.realtimeSinceStartup + 20f;
            bool stolen = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (collector.State == EnemyState.Steal) { stolen = true; break; }
                yield return null;
            }
            Check(stolen, "collector_steals_item");
            PickupItem stolenItem = FindObjectsByType<PickupItem>(FindObjectsSortMode.None).FirstOrDefault(item => item.EnemyHolder == collector);
            Check(stolenItem != null && stolenItem.GetComponent<NetworkCarryItem>().Stolen.Value, "stolen_item_claim_replicated");
            deadline = Time.time + 20f;
            while (stolenItem != null && stolenItem.EnemyHolder != null && Time.time < deadline) yield return null;
            Check(stolenItem != null && stolenItem.EnemyHolder == null && !stolenItem.GetComponent<NetworkCarryItem>().Stolen.Value, "stolen_item_released_at_shelf");
            players[0].transform.position = collector.transform.position + Vector3.back * 6f;
            players[1].transform.position = collector.transform.position + Vector3.back * 6f + Vector3.left * 2f;
            foreach (NetworkPlayerAvatar player in players) player.SetLightOnServer(true, (collector.transform.position - player.transform.position).normalized);
            yield return new WaitForSeconds(0.3f);
            Check(collector.State == EnemyState.Stagger, "collector_two_lights_stagger");
            foreach (NetworkPlayerAvatar player in players) { player.SetLightOnServer(false, Vector3.forward); player.transform.position = new Vector3(0f, 0f, -4f); }
            yield return new WaitForSeconds(2.2f);
            players[1].transform.position = mannequin.transform.position + mannequin.transform.forward * 0.9f;
            deadline = Time.realtimeSinceStartup + 5f;
            while (players[1].LifeState.Value == PlayerLifeState.Alive && Time.realtimeSinceStartup < deadline) yield return null;
            Check(players[1].LifeState.Value == PlayerLifeState.Downed, "enemy_attack_downs_player");
            players[1].transform.position = new Vector3(0f, 0f, -4f);
            players[0].transform.position = players[1].transform.position + Vector3.right;
            players[0].SetInteractHeldServerRpc(true);
            players[0].RequestReviveServerRpc(players[1].NetworkObjectId);
            yield return new WaitForSeconds(0.25f);
            Check(players[0].RescueProgress.Value > 0f, "rescue_progress_started");
            players[0].SetInteractHeldServerRpc(false);
            yield return new WaitForSeconds(2.1f);
            Check(players[1].LifeState.Value == PlayerLifeState.Downed && players[0].RescueProgress.Value == 0f, "rescue_release_cancels");
            players[0].SetInteractHeldServerRpc(true);
            players[0].RequestReviveServerRpc(players[1].NetworkObjectId);
            yield return new WaitForSeconds(0.7f);
            Check(players[0].RescueProgress.Value == 0f && players[1].LifeState.Value == PlayerLifeState.Downed, "rescue_missing_heartbeat_cancels");
            players[0].SetInteractHeldServerRpc(true);
            players[0].RequestReviveServerRpc(players[1].NetworkObjectId);
            yield return new WaitForSeconds(0.2f);
            players[0].transform.position += Vector3.right * 3f;
            yield return null;
            yield return null;
            Check(players[0].RescueProgress.Value == 0f, "rescue_distance_cancels");
            players[0].transform.position = players[1].transform.position + Vector3.right;
            players[0].SetInteractHeldServerRpc(true);
            players[0].RequestReviveServerRpc(players[1].NetworkObjectId);
            for (int pulse = 0; pulse < 23; pulse++)
            {
                players[0].SetInteractHeldServerRpc(true);
                yield return new WaitForSeconds(0.1f);
            }
            players[0].SetInteractHeldServerRpc(false);
            Check(players[1].LifeState.Value == PlayerLifeState.Alive, "revive_after_enemy_attack");
            NetworkCarryItem sellable = FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None).First(item => !item.Sold.Value && !item.Stolen.Value);
            NetworkStageState stage = FindFirstObjectByType<NetworkStageState>();
            int before = stage.SoldValue.Value;
            Check(sellable.TrySellOnServer(stage) && stage.SoldValue.Value > before && !sellable.TrySellOnServer(stage), "sale_once_on_host");
            // Isolate the lifetime timer using the same server entry point as an attack.
            // Actual damage was verified above; this check does not claim a second attack.
            players[1].SetLifeStateOnServer(PlayerLifeState.Downed);
            Check(players[1].LifeState.Value == PlayerLifeState.Downed, "downed_timer_started");
            players[1].transform.position = new Vector3(0f, 0f, -4f);
            yield return new WaitForSeconds(59f);
            Check(players[1].LifeState.Value == PlayerLifeState.Downed, "downed_not_dead_early");
            yield return new WaitForSeconds(1.3f);
            Check(players[1].LifeState.Value == PlayerLifeState.Dead, "downed_expires_after_sixty_seconds");
            players[0].SetLifeStateOnServer(PlayerLifeState.Dead);
            yield return null;
            Check(stage.State.Value == RunState.Failed, "no_survivors_fails_run");
            Debug.Log($"GAME1_ENEMY_SMOKE_COMPLETE success={!failed}");
            Application.Quit(failed ? 1 : 0);
        }
        private bool Check(bool success, string label)
        {
            Debug.Log($"GAME1_ENEMY_CHECK {label}={success}");
            if (!success) failed = true;
            return success;
        }
    }
}
