using System.Collections.Generic;
using Game1.Gameplay;
using Game1.Items;
using Game1.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Game1.Enemies
{
    public enum EnemyKind { Mannequin, Collector }
    public enum EnemyState { Idle, Patrol, Detect, Freeze, Investigate, Chase, Attack, Return, Hunt, InspectItem, Steal, Stagger }

    [RequireComponent(typeof(NetworkObject), typeof(NavMeshAgent))]
    public sealed class EnemyActor : NetworkBehaviour
    {
        [SerializeField] private EnemyKind kind;
        [SerializeField] private Vector3[] waypoints;
        private NavMeshAgent agent;
        private Vector3 home, lastSeen;
        private float enteredAt, lastSeenAt = float.NegativeInfinity, nextSense, nextAttack, immuneUntil, staggerReady;
        private bool networkSession;
        private int waypoint;
        private Transform target;
        private PickupItem desiredItem;
        private EnemyState localState;
        private readonly HashSet<Transform> lights = new();
        private readonly Dictionary<Transform, float> exposure = new();
        private Transform freezeSource;
        private float freezeSeconds = 6f;
        private float heardUntil;
        private int heardPriority;
        public NetworkVariable<EnemyState> ReplicatedState { get; } = new(EnemyState.Idle);
        public NetworkVariable<bool> Revealed { get; } = new(false);
        private float curseUntil;
        public bool IsRevealed => IsSpawned ? Revealed.Value : Time.time < curseUntil;
        public void RevealFromBlocks(float seconds)
        {
            if (!Authority) return;
            curseUntil = Time.time + Mathf.Max(0f, seconds);
            if (IsSpawned) Revealed.Value = seconds > 0f;
        }
        public EnemyState State => IsSpawned ? ReplicatedState.Value : localState;
        public EnemyKind Kind => kind;
        public bool Authority => networkSession ? IsSpawned && IsServer : GameplayNoise.HasAuthority;

        public void Configure(EnemyKind value, Vector3[] route) { kind = value; waypoints = route; }
        private void Awake() { agent = GetComponent<NavMeshAgent>(); home = transform.position; enteredAt = Time.time; }
        private void Start() { if (Authority) agent.enabled = true; }
        private void OnEnable() => GameplayNoise.Emitted += Hear;
        private void OnDisable()
        {
            GameplayNoise.Emitted -= Hear;
            if (desiredItem != null && desiredItem.EnemyHolder == this) desiredItem.ReleaseFromEnemy(transform.position);
        }
        public override void OnNetworkSpawn() { networkSession = true; agent.enabled = IsServer; if (IsServer) Enter(EnemyState.Idle); }
        public override void OnNetworkDespawn() => agent.enabled = false;

        private bool Ready => agent.enabled && agent.isOnNavMesh;
        private void Enter(EnemyState state)
        {
            if (State == EnemyState.Steal && state != EnemyState.Steal && desiredItem != null && desiredItem.EnemyHolder == this)
                desiredItem.ReleaseFromEnemy(transform.position + Vector3.up);
            localState = state;
            if (IsSpawned && IsServer) ReplicatedState.Value = state;
            enteredAt = Time.time;
            if (Ready)
            {
                agent.ResetPath();
                agent.isStopped = state is EnemyState.Freeze or EnemyState.Stagger or EnemyState.Attack or EnemyState.Idle or EnemyState.InspectItem;
                agent.velocity = Vector3.zero;
            }
            Debug.Log($"GAME1_ENEMY_STATE kind={kind} state={state}");
        }

        private void Update()
        {
            if (!Authority) return;
            if (IsSpawned && Revealed.Value && Time.time >= curseUntil) Revealed.Value = false;
            if (Time.time >= nextSense)
            {
                nextSense = Time.time + 0.1f;
                Sense();
            }
            float elapsed = Time.time - enteredAt;
            if (State == EnemyState.Freeze)
            {
                if (elapsed >= freezeSeconds || !lights.Contains(freezeSource))
                { immuneUntil = Time.time + 4f; exposure.Clear(); Enter(EnemyState.Investigate); }
                return;
            }
            if (State == EnemyState.Stagger) { if (elapsed >= 2f) Enter(EnemyState.Investigate); return; }
            if (kind == EnemyKind.Collector && State == EnemyState.Idle)
            {
                NetworkStageState stage = FindFirstObjectByType<NetworkStageState>();
                LocalRunController local = FindFirstObjectByType<LocalRunController>();
                if ((IsSpawned ? stage != null && stage.SoldValue.Value >= 500 : local != null && local.SoldValue >= 500)) Enter(EnemyState.Hunt);
                return;
            }
            switch (State)
            {
                case EnemyState.Idle: if (elapsed >= 20f) Enter(EnemyState.Patrol); break;
                case EnemyState.Patrol:
                    if (waypoints == null || waypoints.Length == 0) break;
                    if (elapsed % 12f >= 10f) { if (Ready) agent.ResetPath(); break; }
                    Move(waypoints[waypoint], 1.8f);
                    if (Vector3.Distance(transform.position, waypoints[waypoint]) < 0.6f) waypoint = (waypoint + 1) % waypoints.Length;
                    break;
                case EnemyState.Detect:
                    if (elapsed >= 0.4f) Enter(target != null && Time.time - lastSeenAt < 0.2f ? EnemyState.Chase : EnemyState.Investigate);
                    break;
                case EnemyState.Investigate:
                    Move(lastSeen, kind == EnemyKind.Mannequin ? 3.2f : 4f);
                    if (elapsed >= (kind == EnemyKind.Mannequin ? 6f : 7f)) Enter(EnemyState.Return);
                    break;
                case EnemyState.Chase:
                    Move(lastSeen, kind == EnemyKind.Mannequin ? 4.6f : 5f);
                    if (target != null && Attackable(target) && target.position.z >= 0f && Vector3.Distance(transform.position, target.position) <= 1.3f && Time.time >= nextAttack) Enter(EnemyState.Attack);
                    else if (Time.time - lastSeenAt >= 8f) Enter(EnemyState.Return);
                    break;
                case EnemyState.Attack:
                    if (elapsed >= 0.4f && nextAttack <= Time.time)
                    {
                        nextAttack = Time.time + (kind == EnemyKind.Mannequin ? 2f : 2.5f);
                        if (target != null && Attackable(target) && target.position.z >= 0f && Vector3.Distance(transform.position, target.position) <= 1.3f && EnemyPerception.ClearLine(transform.position + Vector3.up, target.position + Vector3.up, transform, target)) Down(target);
                    }
                    if (elapsed >= 1.4f) Enter(EnemyState.Chase);
                    break;
                case EnemyState.Hunt:
                    desiredItem = NearestItem();
                    if (desiredItem == null) { Enter(EnemyState.Return); break; }
                    Move(desiredItem.transform.position, 3.4f);
                    if (Vector3.Distance(transform.position, desiredItem.transform.position) <= 1.4f) Enter(EnemyState.InspectItem);
                    break;
                case EnemyState.InspectItem:
                    if (!Available(desiredItem)) { Enter(EnemyState.Hunt); break; }
                    if (elapsed >= 1.2f && desiredItem.TryClaimByEnemy(this)) Enter(EnemyState.Steal);
                    break;
                case EnemyState.Steal:
                    if (desiredItem == null || desiredItem.EnemyHolder != this) { Enter(EnemyState.Return); break; }
                    desiredItem.transform.position = transform.position + transform.forward * 0.8f + Vector3.up;
                    Move(home, 3.4f);
                    if (Vector3.Distance(transform.position, home) < 0.6f)
                    { desiredItem.ReleaseFromEnemy(home + Vector3.up); desiredItem = null; Enter(EnemyState.Idle); }
                    break;
                case EnemyState.Return:
                    Vector3 destination = home;
                    if (kind == EnemyKind.Mannequin && waypoints != null)
                        foreach (Vector3 point in waypoints) if (Vector3.Distance(transform.position, point) < Vector3.Distance(transform.position, destination)) destination = point;
                    Move(destination, kind == EnemyKind.Mannequin ? 2.2f : 3f);
                    if (Vector3.Distance(transform.position, destination) < 0.6f) Enter(kind == EnemyKind.Mannequin ? EnemyState.Patrol : EnemyState.Idle);
                    break;
            }
        }

        private void Move(Vector3 destination, float speed)
        {
            if (!Ready || !NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, agent.areaMask)) return;
            agent.speed = speed * (Time.time < curseUntil ? 1.1f : 1f);
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }

        private void Sense()
        {
            lights.Clear();
            Transform visible = null;
            float best = float.PositiveInfinity;
            if (IsSpawned)
            {
                foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
                    if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out NetworkPlayerAvatar avatar) && Attackable(avatar.transform))
                        Consider(avatar.transform, avatar.FlashlightOn.Value, avatar.LookDirection.Value, ref visible, ref best);
            }
            else foreach (LocalPlayerController player in FindObjectsByType<LocalPlayerController>(FindObjectsSortMode.None))
                if (Attackable(player.transform)) Consider(player.transform, player.FlashlightOn, player.ViewCamera.transform.forward, ref visible, ref best);

            if (kind == EnemyKind.Mannequin && Time.time >= immuneUntil && State != EnemyState.Freeze)
            {
                foreach (Transform source in lights)
                {
                    if (!exposure.TryGetValue(source, out float start)) exposure[source] = start = Time.time;
                    bool mirror = CarriesMirror(source);
                    if (mirror || Time.time - start >= 2f) { freezeSource = source; freezeSeconds = mirror ? 4f : 6f; Enter(EnemyState.Freeze); break; }
                }
                foreach (Transform source in new List<Transform>(exposure.Keys)) if (!lights.Contains(source)) exposure.Remove(source);
            }
            if (kind == EnemyKind.Collector && lights.Count >= 2 && Time.time >= staggerReady)
            { staggerReady = Time.time + 12f; Enter(EnemyState.Stagger); }
            if (visible == null || State is EnemyState.Freeze or EnemyState.Stagger or EnemyState.Attack) return;
            if (kind == EnemyKind.Collector && State == EnemyState.Idle) return;
            target = visible; lastSeen = visible.position; lastSeenAt = Time.time;
            if (State == EnemyState.Steal && desiredItem != null) desiredItem.ReleaseFromEnemy(transform.position + Vector3.up);
            if (State is not EnemyState.Chase and not EnemyState.Detect)
                Enter(State == EnemyState.InspectItem ? EnemyState.Investigate :
                    kind == EnemyKind.Mannequin && State != EnemyState.Investigate ? EnemyState.Detect : EnemyState.Chase);
        }

        private void Consider(Transform candidate, bool lit, Vector3 direction, ref Transform visible, ref float best)
        {
            // The entrance/backyard is a safe zone in the current greybox.
            if (candidate.position.z < 0f) return;
            Vector3 eye = candidate.position + Vector3.up * 1.62f;
            Vector3 enemyEye = transform.position + Vector3.up * 1.5f;
            if (lit && EnemyPerception.InCone(eye, direction, enemyEye, 15f, 35f) && EnemyPerception.ClearLine(eye, enemyEye, candidate, transform)) lights.Add(candidate);
            float range = kind == EnemyKind.Mannequin ? (lit ? 12f : 7f) : 10f;
            float score = Vector3.Distance(candidate.position, transform.position);
            if (Life(candidate) == PlayerLifeState.Downed)
            {
                if (score > 1.3f) return;
                score -= 200f;
            }
            if (kind == EnemyKind.Collector && IsCarrying(candidate)) score -= 100f;
            if (score >= best || !EnemyPerception.InCone(enemyEye, transform.forward, eye, range, kind == EnemyKind.Mannequin ? 90f : 100f) || !EnemyPerception.ClearLine(enemyEye, eye, transform, candidate)) return;
            best = score; visible = candidate;
        }

        private void Hear(GameplayNoiseEvent noise)
        {
            if (!Authority || noise.Position.z < 0f || !EnemyPerception.Hears(transform.position, noise, kind == EnemyKind.Mannequin ? 10f : 14f, kind == EnemyKind.Collector)) return;
            if (kind == EnemyKind.Collector && State == EnemyState.Idle)
            { if (noise.Kind == NoiseKind.Curse) Enter(EnemyState.Hunt); return; }
            if (State is EnemyState.Freeze or EnemyState.Stagger or EnemyState.Attack || Time.time - lastSeenAt < 0.2f) return;
            int priority = noise.Kind == NoiseKind.Ping ? 0 : noise.Kind == NoiseKind.Curse ? 1 : 2;
            if (Time.time < heardUntil && priority < heardPriority) return;
            heardPriority = priority;
            heardUntil = Time.time + (kind == EnemyKind.Mannequin ? 6f : 7f);
            if (State == EnemyState.Steal && desiredItem != null) desiredItem.ReleaseFromEnemy(transform.position + Vector3.up);
            lastSeen = noise.Position; target = null;
            Enter(kind == EnemyKind.Mannequin && State == EnemyState.Idle ? EnemyState.Detect : EnemyState.Investigate);
        }
        private PickupItem NearestItem()
        {
            PickupItem result = null; float distance = float.PositiveInfinity;
            foreach (PickupItem item in FindObjectsByType<PickupItem>(FindObjectsSortMode.None))
                if (Available(item) && (item.Definition.Value >= 300 || item.Definition.Rarity == ItemRarity.Curse) && item.transform.position.z >= 0f)
                { float current = Vector3.Distance(transform.position, item.transform.position); if (current < distance) { distance = current; result = item; } }
            return result;
        }
        private static bool Available(PickupItem item) => item != null && !item.IsSold && !item.IsHeld && item.EnemyHolder == null &&
            (!item.TryGetComponent(out NetworkCarryItem carry) || !carry.IsSpawned || carry.PrimaryCarrier.Value == CoopAuthorityRules.NoClient);
        private static bool CarriesMirror(Transform player)
        {
            if (player.TryGetComponent(out LocalPlayerController local)) return local.HeldItem != null && local.HeldItem.Definition.ItemId == "mirror_box";
            if (!player.TryGetComponent(out NetworkPlayerAvatar avatar)) return false;
            foreach (NetworkCarryItem item in FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None))
                if (item.Definition.ItemId == "mirror_box" && item.PrimaryCarrier.Value == avatar.OwnerClientId) return true;
            return false;
        }
        private static bool IsCarrying(Transform player)
        {
            if (player.TryGetComponent(out LocalPlayerController local)) return local.HeldItem != null;
            return player.TryGetComponent(out NetworkPlayerAvatar avatar) && NetworkCarryItem.HasItem(avatar.OwnerClientId);
        }
        private static PlayerLifeState Life(Transform value) => value.TryGetComponent(out NetworkPlayerAvatar avatar) ? avatar.LifeState.Value : value.GetComponent<LocalPlayerController>().LifeState;
        private static bool Attackable(Transform value) => Life(value) is PlayerLifeState.Alive or PlayerLifeState.Downed;
        private static void Down(Transform value)
        {
            if (value.TryGetComponent(out NetworkPlayerAvatar avatar)) avatar.SetLifeStateOnServer(avatar.LifeState.Value == PlayerLifeState.Downed ? PlayerLifeState.Dead : PlayerLifeState.Downed);
            else value.GetComponent<LocalPlayerController>().Down();
        }
    }
}
