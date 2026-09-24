using System;
using System.Collections;
using System.Linq;
using Game1.Gameplay;
using Game1.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Game1.Enemies
{
    // Opt-in integration checks using real perception, navigation and authoritative player state.
    public sealed class EnemyBoundaryScenario : MonoBehaviour
    {
        private bool failed;
        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-game1-boundary-smoke") < 0) yield break;
            NetworkManager manager = GetComponent<NetworkManager>();
            float deadline = Time.time + 25f;
            while ((!manager.IsHost || manager.ConnectedClients.Count < 2) && Time.time < deadline) yield return null;
            if (!manager.IsHost) yield break;
            if (manager.ConnectedClients.Count != 2) { Application.Quit(1); yield break; }
            var players = manager.ConnectedClients.Values.Select(c => c.PlayerObject.GetComponent<NetworkPlayerAvatar>()).ToArray();
            var mannequin = FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).First(e => e.Kind == EnemyKind.Mannequin);
            var collector = FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).First(e => e.Kind == EnemyKind.Collector);
            yield return new WaitForSeconds(0.3f);
            GameplayNoise.Emit(mannequin.transform.position, 4f, NoiseKind.Drop);
            Check(mannequin.State == EnemyState.Detect, "idle_noise_detect");
            yield return new WaitForSeconds(0.5f);
            Check(mannequin.State == EnemyState.Investigate, "detect_without_sight_investigate");
            mannequin.RevealFromBlocks(3f);
            yield return null;
            Check(Mathf.Abs(mannequin.GetComponent<NavMeshAgent>().speed - 3.52f) < 0.01f, "blocks_speed_plus_ten_percent");
            mannequin.RevealFromBlocks(0f);
            yield return null;
            Check(Mathf.Abs(mannequin.GetComponent<NavMeshAgent>().speed - 3.2f) < 0.01f, "blocks_speed_restored");

            // Mirror gives an immediate Freeze, making the four-second immunity boundary observable.
            var mirror = FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None).First(i => i.Definition.ItemId == "mirror_box");
            players[0].transform.position = mannequin.transform.position + Vector3.back * 5f;
            mirror.transform.position = players[0].transform.position + Vector3.up;
            Check(mirror.TryRequestCarryOnServer(players[0].OwnerClientId), "mirror_pickup");
            players[0].SetLightOnServer(true, Vector3.forward);
            deadline = Time.time + 1f;
            while (mannequin.State != EnemyState.Freeze && Time.time < deadline) yield return null;
            Check(mannequin.State == EnemyState.Freeze, "mirror_freeze");
            players[0].SetLightOnServer(false, Vector3.forward);
            while (mannequin.State == EnemyState.Freeze) yield return null;
            float released = Time.time;
            players[0].SetLightOnServer(true, Vector3.forward);
            bool immune = true;
            while (Time.time - released < 3.95f)
            {
                players[0].transform.position = mannequin.transform.position + Vector3.back * 5f;
                immune &= mannequin.State != EnemyState.Freeze;
                yield return null;
            }
            Check(immune, "freeze_immunity_near_four_seconds");
            deadline = Time.time + 0.4f;
            while (mannequin.State != EnemyState.Freeze && Time.time < deadline) yield return null;
            Check(mannequin.State == EnemyState.Freeze, "freeze_immunity_expires");
            players[0].SetLightOnServer(false, Vector3.forward);
            mirror.ReleaseOnServer();
            foreach (var player in players) player.transform.position = new Vector3(0f, 0f, -4f);
            mannequin.enabled = false;
            mannequin.GetComponent<NavMeshAgent>().isStopped = true;

            // Facing away from both players isolates light Stagger from pursuit/attacks.
            collector.GetComponent<NavMeshAgent>().enabled = false;
            foreach (var player in players)
            {
                player.transform.position = collector.transform.position + Vector3.back * 6f + Vector3.left * (player == players[0] ? 0f : 2f);
                player.SetLightOnServer(true, (collector.transform.position - player.transform.position).normalized);
            }
            deadline = Time.time + 1f;
            while (collector.State != EnemyState.Stagger && Time.time < deadline) yield return null;
            Check(collector.State == EnemyState.Stagger, "stagger_started");
            float staggerStarted = Time.time;
            bool ended = false, repeatedEarly = false;
            while (Time.time - staggerStarted < 11.8f)
            {
                foreach (var player in players)
                {
                    player.transform.position = collector.transform.position + Vector3.back * 6f + Vector3.left * (player == players[0] ? 0f : 2f);
                    player.SetLightOnServer(true, (collector.transform.position - player.transform.position).normalized);
                }
                if (collector.State != EnemyState.Stagger) ended = true;
                else if (ended) repeatedEarly = true;
                yield return null;
            }
            Check(ended && !repeatedEarly, "stagger_cooldown_near_twelve_seconds");
            deadline = Time.time + 0.5f;
            while (collector.State != EnemyState.Stagger && Time.time < deadline) yield return null;
            Check(collector.State == EnemyState.Stagger, "stagger_cooldown_expires");
            collector.GetComponent<NavMeshAgent>().enabled = true;
            foreach (var player in players) { player.SetLightOnServer(false, Vector3.forward); player.transform.position = new Vector3(0f, 0f, -4f); }
            mannequin.enabled = true;
            players[1].transform.position = mannequin.transform.position + mannequin.transform.forward * 0.9f;
            deadline = Time.time + 8f;
            while (players[1].LifeState.Value != PlayerLifeState.Dead && Time.time < deadline) yield return null;
            Check(players[1].LifeState.Value == PlayerLifeState.Dead, "repeat_attack_kills_downed_player");
            Debug.Log($"GAME1_BOUNDARY_COMPLETE success={!failed}");
            Application.Quit(failed ? 1 : 0);
        }
        private void Check(bool value, string label) { Debug.Log($"GAME1_BOUNDARY_CHECK {label}={value}"); failed |= !value; }
    }
}
