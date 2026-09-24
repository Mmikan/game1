using System;
using System.Collections;
using System.Linq;
using Game1.Network;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Enemies
{
    public sealed class CurseSmokeScenario : MonoBehaviour
    {
        private bool failed;
        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-game1-curse-smoke") < 0) yield break;
            NetworkManager manager = GetComponent<NetworkManager>();
            float deadline = Time.time + 25f;
            while (!manager.IsHost && Time.time < deadline) yield return null;
            if (!manager.IsHost) yield break;
            while (manager.ConnectedClients.Count < 2 && Time.time < deadline) yield return null;
            if (manager.ConnectedClients.Count < 2) { Debug.LogError("GAME1_CURSE_MISSING_CLIENT"); Application.Quit(1); yield break; }
            NetworkPlayerAvatar[] players = manager.ConnectedClients.Values.Select(c => c.PlayerObject.GetComponent<NetworkPlayerAvatar>()).ToArray();
            NetworkCarryItem[] items = FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None);
            EnemyActor mannequin = FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).First(e => e.Kind == EnemyKind.Mannequin);
            NetworkCarryItem mirror = items.First(item => item.Definition.ItemId == "mirror_box");
            Place(mirror, players);
            Check(mirror.TryRequestCarryOnServer(players[0].OwnerClientId), "mirror_pickup");
            players[0].SetLightOnServer(true, (mannequin.transform.position - players[0].transform.position).normalized);
            yield return new WaitForSeconds(0.3f);
            Check(mannequin.State == EnemyState.Freeze, "real_mirror_freezes_without_two_second_wait");
            yield return new WaitForSeconds(3.9f);
            Check(mannequin.State != EnemyState.Freeze, "mirror_four_second_cap");
            players[0].SetLightOnServer(false, Vector3.forward);
            mirror.ReleaseOnServer();
            NetworkCarryItem blocks = items.First(item => item.Definition.ItemId == "black_blocks");
            Place(blocks, players, -4f);
            Check(blocks.TryRequestCarryOnServer(players[0].OwnerClientId) && blocks.TryRequestCarryOnServer(players[1].OwnerClientId), "blocks_shared_carry");
            yield return new WaitForSeconds(3.3f);
            Check(FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).Any(e => e.IsRevealed), "blocks_reveals_enemy");
            blocks.ReleaseOnServer();
            yield return null;
            Check(!FindObjectsByType<EnemyActor>(FindObjectsSortMode.None).Any(e => e.IsRevealed), "blocks_release_clears_effect");
            // Keep players in the safe area while checking real held-item emission intervals.
            NetworkCarryItem clown = items.First(item => item.Definition.ItemId == "windup_clown");
            Place(clown, players, -4f);
            Check(clown.TryRequestCarryOnServer(players[0].OwnerClientId), "clown_pickup");
            int clownEvents = 0;
            Action<GameplayNoiseEvent> listener = noise => { if (noise.Kind == NoiseKind.Curse && Mathf.Abs(noise.Radius - 10f) < 0.01f) clownEvents++; };
            GameplayNoise.Emitted += listener;
            yield return new WaitForSeconds(19f);
            Check(clownEvents == 0, "clown_not_early");
            yield return new WaitForSeconds(1.3f);
            Check(clownEvents == 1, "clown_twenty_seconds_ten_meters");
            GameplayNoise.Emitted -= listener;
            Check(clown.TryRequestCarryOnServer(players[1].OwnerClientId), "clown_optional_shared_carry");
            Check(!clown.TryRequestCarryOnServer(players[1].OwnerClientId), "shared_slot_cannot_be_reclaimed");
            int sharedClownEvents = 0;
            Action<GameplayNoiseEvent> sharedListener = noise => { if (noise.Kind == NoiseKind.Curse && Mathf.Abs(noise.Radius - 5f) < 0.01f) sharedClownEvents++; };
            GameplayNoise.Emitted += sharedListener;
            yield return new WaitForSeconds(20.3f);
            Check(sharedClownEvents == 1, "shared_clown_twenty_seconds_five_meters");
            GameplayNoise.Emitted -= sharedListener;
            clown.ReleaseOnServer();
            Place(blocks, players, -4f);
            Check(blocks.TryRequestCarryOnServer(players[0].OwnerClientId), "heavy_item_reservation");
            players[0].transform.position += Vector3.left * 4f;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Check(blocks.PrimaryCarrier.Value == CoopAuthorityRules.NoClient, "distant_reservation_released");
            NetworkCarryItem doll = items.First(item => item.Definition.ItemId == "cursed_doll");
            Place(doll, players, -4f);
            Check(doll.TryRequestCarryOnServer(players[0].OwnerClientId), "doll_pickup");
            int dollEvents = 0;
            Action<GameplayNoiseEvent> dollListener = noise => { if (noise.Kind == NoiseKind.Curse && Mathf.Abs(noise.Radius - 14f) < 0.01f) dollEvents++; };
            GameplayNoise.Emitted += dollListener;
            yield return new WaitForSeconds(12.3f);
            Check(dollEvents == 1, "doll_twelve_seconds");
            doll.ReleaseOnServer();
            yield return new WaitForSeconds(12.3f);
            Check(dollEvents == 1, "doll_release_stops_noise");
            GameplayNoise.Emitted -= dollListener;
            Debug.Log($"GAME1_CURSE_SMOKE_COMPLETE success={!failed}");
            Application.Quit(failed ? 1 : 0);
        }

        private static void Place(NetworkCarryItem item, NetworkPlayerAvatar[] players, float z = 5f)
        {
            item.transform.position = new Vector3(0f, 1f, z);
            players[0].transform.position = new Vector3(-0.5f, 0f, z);
            players[1].transform.position = new Vector3(0.5f, 0f, z);
            Physics.SyncTransforms();
        }
        private void Check(bool value, string label) { Debug.Log($"GAME1_CURSE_CHECK {label}={value}"); failed |= !value; }
    }
}

