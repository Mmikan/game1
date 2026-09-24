using System;
using System.Collections;
using System.Linq;
using Game1.Items;
using Game1.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Game1.Enemies
{
    public sealed class EnemyInteractionScenario : MonoBehaviour
    {
        private bool failed;
        private IEnumerator Start()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-game1-interaction-smoke") < 0) yield break;
            var manager = GetComponent<NetworkManager>();
            float deadline = Time.time + 25f;
            while ((!manager.IsHost || manager.ConnectedClients.Count < 2) && Time.time < deadline) yield return null;
            if (!manager.IsHost) yield break;
            if (manager.ConnectedClients.Count != 2) { Application.Quit(1); yield break; }
            var players = manager.ConnectedClients.Values.Select(c => c.PlayerObject.GetComponent<NetworkPlayerAvatar>()).ToArray();
            var enemies = FindObjectsByType<EnemyActor>(FindObjectsSortMode.None);
            var collector = enemies.First(e => e.Kind == EnemyKind.Collector);
            var mannequin = enemies.First(e => e.Kind == EnemyKind.Mannequin);
            mannequin.enabled = false;
            mannequin.GetComponent<NavMeshAgent>().enabled = false;
            var items = FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None);
            foreach (var item in items) { item.transform.position = new Vector3(-12f, 1f, -4f); item.GetComponent<Rigidbody>().isKinematic = true; }
            var stock = items.First(i => i.Definition.ItemId == "cursed_doll");
            var pickup = stock.GetComponent<PickupItem>();
            var agent = collector.GetComponent<NavMeshAgent>();
            agent.Warp(new Vector3(0f, 0f, 15f));
            stock.transform.position = collector.transform.position + Vector3.forward * 0.8f + Vector3.up * 0.3f;
            Physics.SyncTransforms();
            GameplayNoise.Emit(collector.transform.position, 4f, NoiseKind.Curse);
            deadline = Time.time + 3f;
            while (collector.State != EnemyState.InspectItem && Time.time < deadline) yield return null;
            Check(collector.State == EnemyState.InspectItem, "collector_inspects_nearby_stock");
            bool interrupted = false;
            void Track(EnemyState previous, EnemyState current) { if (previous == EnemyState.InspectItem && current == EnemyState.Investigate) interrupted = true; }
            collector.ReplicatedState.OnValueChanged += Track;
            players[0].transform.position = collector.transform.position + collector.transform.forward * 4f;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.35f);
            Check(interrupted && pickup.EnemyHolder == null && !stock.Stolen.Value, "inspection_sight_interrupts_before_claim");
            collector.ReplicatedState.OnValueChanged -= Track;
            players[0].transform.position = new Vector3(0f, 0f, -4f);
            deadline = Time.time + 25f;
            while (collector.State != EnemyState.Idle && Time.time < deadline) yield return null;
            Check(collector.State == EnemyState.Idle, "collector_returns_after_losing_sight");
            agent.Warp(new Vector3(0f, 0f, 15f));
            stock.transform.position = collector.transform.position + Vector3.forward * 0.8f + Vector3.up * 0.3f;
            GameplayNoise.Emit(collector.transform.position, 4f, NoiseKind.Curse);
            deadline = Time.time + 4f;
            while (collector.State != EnemyState.Steal && Time.time < deadline) yield return null;
            Check(collector.State == EnemyState.Steal && pickup.EnemyHolder == collector && stock.Stolen.Value, "theft_claim_acquired");
            Vector3 ordinary = collector.transform.position + Vector3.left * 2f;
            GameplayNoise.Emit(ordinary, 10f, NoiseKind.Drop);
            yield return null;
            Check(collector.State == EnemyState.Investigate && pickup.EnemyHolder == null && !stock.Stolen.Value, "theft_noise_interrupt_releases_claim");
            // Keep dropped stock still so its collision cannot introduce another ordinary noise.
            stock.GetComponent<Rigidbody>().isKinematic = true;
            GameplayNoise.Emit(collector.transform.position + Vector3.right * 2f, 10f, NoiseKind.Ping);
            yield return null;
            Check(Vector3.Distance(agent.destination, ordinary) < 0.4f, "ordinary_noise_outranks_ping");
            GameplayNoise.Emit(collector.transform.position + Vector3.forward * 2f, 10f, NoiseKind.Curse);
            yield return null;
            Check(Vector3.Distance(agent.destination, ordinary) < 0.4f, "ordinary_noise_outranks_curse");
            // A movable door-sized obstacle on the actual baked aisle tests carving updates.
            var doorFixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFixture.name = "DoorNavigationAcceptance";
            doorFixture.transform.position = new Vector3(0f, 1.5f, 10f);
            doorFixture.transform.localScale = new Vector3(3f, 3f, 0.25f);
            var obstacle = doorFixture.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
            yield return new WaitForSeconds(0.5f);
            Vector3 near = new(0f, 0f, 8f), far = new(0f, 0f, 12f);
            Check(NavMesh.Raycast(near, far, out _, NavMesh.AllAreas), "closed_door_blocks_direct_navigation");
            var path = new NavMeshPath();
            Check(NavMesh.CalculatePath(near, far, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete &&
                  path.corners.Any(c => Mathf.Abs(c.x) > 1.5f), "navigation_routes_around_closed_door");
            doorFixture.transform.position += Vector3.right * 6f;
            yield return new WaitForSeconds(0.5f);
            Check(!NavMesh.Raycast(near, far, out _, NavMesh.AllAreas), "moved_door_restores_direct_navigation");
            Destroy(doorFixture);
            var door = FindFirstObjectByType<NetworkDoorState>();
            Check(!door.TryToggleOnServer(players[0].OwnerClientId), "distant_door_toggle_rejected");
            players[0].transform.position = door.transform.position + Vector3.forward;
            Check(door.TryToggleOnServer(players[0].OwnerClientId) && door.OpenAngle.Value == 100f, "nearby_door_opens");
            door.SetHeldServerRpc(true);
            Check(!door.TryToggleOnServer(players[0].OwnerClientId) && door.OpenAngle.Value == 100f, "held_door_cannot_close");
            door.SetHeldServerRpc(false);
            Check(door.TryToggleOnServer(players[0].OwnerClientId) && door.OpenAngle.Value == 0f, "released_door_closes");
            Debug.Log($"GAME1_INTERACTION_COMPLETE success={!failed}");
            Application.Quit(failed ? 1 : 0);
        }
        private void Check(bool value, string label) { Debug.Log($"GAME1_INTERACTION_CHECK {label}={value}"); failed |= !value; }
    }
}
