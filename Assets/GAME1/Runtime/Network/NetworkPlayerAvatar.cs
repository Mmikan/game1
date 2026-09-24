using System.Collections;
using Game1.Gameplay;
using Game1.Debuffs;
using Game1.Enemies;
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
        private float downedUntil, nextFootstep;
        private LocalPlayerController localView;
        private CharacterController motor;
        private float yaw, pitch;
        private bool wantsSprint;
        private float lastInputAt, nextPingAt;
        private float interactHeldUntil;
        private Coroutine rescueRoutine;
        public NetworkVariable<float> RescueProgress { get; } = new(0f);
        public NetworkVariable<ulong> RescueTarget { get; } = new(CoopAuthorityRules.NoClient);
        public NetworkVariable<float> DownedSecondsRemaining { get; } = new(0f);
        public NetworkVariable<float> Stamina { get; } = new(100f);
        public static DebuffKind ListenerDebuff
        {
            get
            {
                var manager = Unity.Netcode.NetworkManager.Singleton;
                if (manager != null && manager.IsConnectedClient && manager.LocalClient.PlayerObject != null)
                    return manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerAvatar>().Debuff.Value;
                var local = FindFirstObjectByType<PlayerDebuffController>();
                return local != null ? local.Kind : DebuffKind.None;
            }
        }
        public override void OnNetworkSpawn()
        {
            if (IsServer) transform.position = new Vector3((float)OwnerClientId * 1.2f, 0f, -4f);
            if (TryGetComponent(out CapsuleCollider capsule)) { capsule.center = Vector3.up * 0.875f; capsule.height = 1.75f; capsule.radius = 0.32f; capsule.isTrigger = true; }
            if (IsServer)
            {
                motor = gameObject.AddComponent<CharacterController>();
                motor.center = Vector3.up * 0.875f; motor.height = 1.75f; motor.radius = 0.32f;
            }
            if (!IsOwner) return;
            localView = FindFirstObjectByType<LocalPlayerController>();
            if (localView == null) return;
            localView.DropHeldItem(false);
            localView.enabled = false;
            localView.GetComponent<CharacterController>().enabled = false;
            localView.GetComponent<PlayerInteractor>().enabled = false;
            localView.GetComponent<PingController>().enabled = false;
            localView.GetComponent<PlayerDebuffController>().enabled = false;
        }

        public override void OnNetworkDespawn()
        {
            if (localView == null) return;
            localView.enabled = true;
            localView.GetComponent<CharacterController>().enabled = true;
            localView.GetComponent<PlayerInteractor>().enabled = true;
            localView.GetComponent<PingController>().enabled = true;
            localView.GetComponent<PlayerDebuffController>().enabled = true;
        }

        private void LateUpdate()
        {
            if (!IsOwner || localView == null) return;
            localView.ViewCamera.transform.SetPositionAndRotation(transform.position + Vector3.up * 1.62f, Quaternion.Euler(pitch, yaw, 0f));
            localView.ViewCamera.GetComponent<Light>().enabled = FlashlightOn.Value;
        }
        public NetworkVariable<Vector3> LookDirection { get; } = new(Vector3.forward);

        public void SetLightOnServer(bool value, Vector3 direction)
        {
            if (!IsServer || direction.sqrMagnitude < 0.01f) return;
            FlashlightOn.Value = value;
            LookDirection.Value = direction.normalized;
        }

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
            if (IsServer)
            {
                DownedSecondsRemaining.Value = LifeState.Value == PlayerLifeState.Downed ? Mathf.Max(0f, downedUntil - Time.time) : 0f;
                if (SupportingClientId.Value != CoopAuthorityRules.NoClient &&
                    (!TryGetPlayer(SupportingClientId.Value, out NetworkPlayerAvatar supported) || supported == this ||
                     supported.LifeState.Value != PlayerLifeState.Alive ||
                     !CoopAuthorityRules.CanInteract(LifeState.Value, transform.position, supported.transform.position, 1.8f)))
                    SupportingClientId.Value = CoopAuthorityRules.NoClient;
            }
            if (IsServer && LifeState.Value == PlayerLifeState.Downed && Time.time >= downedUntil) SetLifeStateOnServer(PlayerLifeState.Dead);
            if (IsOwner && !Application.isBatchMode && Keyboard.current != null && LifeState.Value == PlayerLifeState.Alive)
            {
                if (Mouse.current != null)
                {
                    Vector2 look = Mouse.current.delta.ReadValue() * 0.12f;
                    yaw += look.x; pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
                }
                if (Keyboard.current.fKey.wasPressedThisFrame) SetFlashlightServerRpc(!FlashlightOn.Value);
                if (Keyboard.current.eKey.wasPressedThisFrame) InteractServerRpc();
                if (Keyboard.current.qKey.wasPressedThisFrame) PingServerRpc();
                if (Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame) ThrowServerRpc();
            }
            if (IsOwner && !Application.isBatchMode && Keyboard.current != null && Time.unscaledTime >= nextInputTime)
            {
                nextInputTime = Time.unscaledTime + 1f / inputSendRate;
                Vector2 input = new(
                    (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                    (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));
                SubmitInputServerRpc(Vector2.ClampMagnitude(input, 1f), Keyboard.current.leftShiftKey.isPressed);
                SubmitLookServerRpc(Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);
                SetInteractHeldServerRpc(Keyboard.current.eKey.isPressed);
            }

            if (IsServer && LifeState.Value == PlayerLifeState.Alive)
            {
                if (Time.time - lastInputAt > 0.5f) serverInput = Vector2.zero;
                bool sprinting = wantsSprint && serverInput.y > 0f && Stamina.Value > 0f && !NetworkCarryItem.HasItem(OwnerClientId);
                Stamina.Value = Mathf.Clamp(Stamina.Value + (sprinting ? -(Debuff.Value == DebuffKind.HeavyBreath ? 35f : 20f) : Debuff.Value == DebuffKind.HeavyBreath ? 8f : 16f) * Time.deltaTime, 0f, 100f);
                Vector3 movement = transform.right * serverInput.x + transform.forward * serverInput.y;
                float effectiveSpeed = Debuff.Value == DebuffKind.HeavyBreath && IsSupportedByAlly() ? 4.6f : sprinting ? 6f : walkSpeed;
                if (movement.sqrMagnitude > 0.001f && motor != null) motor.Move((movement * effectiveSpeed + Vector3.down * 2f) * Time.deltaTime);
                if (movement.sqrMagnitude > 0.01f && Time.time >= nextFootstep)
                {
                    nextFootstep = Time.time + 0.5f;
                    GameplayNoise.Emit(transform.position, sprinting ? 4f : 1.5f, NoiseKind.Footstep);
                    if (Debuff.Value == DebuffKind.HeavyBreath && (sprinting || IsSupportedByAlly()))
                        GameplayNoise.Emit(transform.position, IsSupportedByAlly() ? 3f : 8f, NoiseKind.HeavyBreath);
                }
            }
        }

        [ServerRpc]
        private void InteractServerRpc()
        {
            interactHeldUntil = Time.time + 0.5f;
            if (LifeState.Value != PlayerLifeState.Alive) return;
            foreach (NetworkCarryItem item in FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None))
                if (item.PrimaryCarrier.Value == OwnerClientId || item.SecondaryCarrier.Value == OwnerClientId)
                { item.ReleaseOnServer(); return; }
            Ray ray = new(transform.position + Vector3.up * 1.62f, LookDirection.Value);
            RaycastHit[] hits = Physics.RaycastAll(ray, 3f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                if (hit.collider.GetComponentInParent<NetworkCarryItem>() is NetworkCarryItem item) item.TryRequestCarryOnServer(OwnerClientId);
                else if (hit.collider.GetComponentInParent<NetworkDoorState>() is NetworkDoorState door) door.TryToggleOnServer(OwnerClientId);
                else if (hit.collider.GetComponentInParent<NetworkPlayerAvatar>() is NetworkPlayerAvatar ally && ally.LifeState.Value == PlayerLifeState.Downed)
                    BeginRescue(ally);
                break;
            }
        }

        [ServerRpc]
        private void SubmitLookServerRpc(Vector3 direction)
        {
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y) || !float.IsFinite(direction.z) || direction.sqrMagnitude < 0.9f || direction.sqrMagnitude > 1.1f) return;
            LookDirection.Value = direction.normalized;
            Vector3 planar = new(direction.x, 0f, direction.z);
            if (planar.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(planar);
        }

        [ServerRpc]
        private void SubmitInputServerRpc(Vector2 input, bool sprint, ServerRpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != OwnerClientId || !float.IsFinite(input.x) || !float.IsFinite(input.y) || input.sqrMagnitude > 1.01f || LifeState.Value != PlayerLifeState.Alive) return;
            serverInput = Vector2.ClampMagnitude(input, 1f);
            wantsSprint = sprint;
            lastInputAt = Time.time;
        }

        [ServerRpc]
        private void PingServerRpc()
        {
            if (LifeState.Value != PlayerLifeState.Alive || Time.time < nextPingAt) return;
            nextPingAt = Time.time + (Debuff.Value == DebuffKind.LostVoice ? 4f : 2f);
            Ray ray = new(transform.position + Vector3.up * 1.62f, LookDirection.Value);
            Vector3 point = Physics.Raycast(ray, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : ray.GetPoint(30f);
            GameplayNoise.Emit(point, 4f, NoiseKind.Ping);
            ShowPingClientRpc(point);
        }

        [ClientRpc]
        private void ShowPingClientRpc(Vector3 point)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "NetworkPing";
            marker.transform.position = point;
            marker.transform.localScale = Vector3.one * 0.2f;
            Destroy(marker.GetComponent<Collider>());
            Destroy(marker, 6f);
        }

        [ServerRpc]
        private void ThrowServerRpc()
        {
            if (LifeState.Value != PlayerLifeState.Alive) return;
            foreach (NetworkCarryItem item in FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None))
                if (item.PrimaryCarrier.Value == OwnerClientId && !item.IsSharedCarry && item.Definition.Throwable &&
                    !(Debuff.Value == DebuffKind.BackPain && item.Definition.WeightKg > 8f))
                { item.ReleaseOnServer(); item.GetComponent<Rigidbody>().linearVelocity = LookDirection.Value * 7f; GameplayNoise.Emit(item.transform.position, item.Definition.NoiseRadius, NoiseKind.Throw); return; }
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
            rescuer.BeginRescue(target);
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
                !targetObject.TryGetComponent(out NetworkPlayerAvatar target) || target == supporter || target.LifeState.Value != PlayerLifeState.Alive ||
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
            if (!IsServer) return;
            LifeState.Value = state;
            if (state == PlayerLifeState.Downed) downedUntil = Time.time + 60f;
            if (state != PlayerLifeState.Alive)
            {
                serverInput = Vector2.zero;
                interactHeldUntil = 0f;
                SupportingClientId.Value = CoopAuthorityRules.NoClient;
                FlashlightOn.Value = false;
                foreach (NetworkCarryItem item in FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None))
                    if (item.PrimaryCarrier.Value == OwnerClientId || item.SecondaryCarrier.Value == OwnerClientId) item.ReleaseOnServer();
            }
        }

        [ServerRpc]
        public void SetInteractHeldServerRpc(bool held)
        {
            interactHeldUntil = held && LifeState.Value == PlayerLifeState.Alive ? Time.time + 0.5f : 0f;
        }

        private void BeginRescue(NetworkPlayerAvatar target)
        {
            if (rescueRoutine != null || target == this || Time.time >= interactHeldUntil) return;
            rescueRoutine = StartCoroutine(ReviveAfterDelay(target));
        }

        private IEnumerator ReviveAfterDelay(NetworkPlayerAvatar target)
        {
            // Yield first so the coroutine handle is assigned even when validation fails.
            yield return null;
            float started = Time.time;
            RescueTarget.Value = target.NetworkObjectId;
            while (target != null && target.IsSpawned && target.LifeState.Value == PlayerLifeState.Downed &&
                   Time.time < interactHeldUntil &&
                   CoopAuthorityRules.CanInteract(LifeState.Value, transform.position, target.transform.position, 1.5f))
            {
                RescueProgress.Value = Mathf.Clamp01((Time.time - started) / 2f);
                if (RescueProgress.Value >= 1f)
                {
                    target.SetLifeStateOnServer(PlayerLifeState.Alive);
                    break;
                }
                yield return null;
            }
            RescueProgress.Value = 0f;
            RescueTarget.Value = CoopAuthorityRules.NoClient;
            rescueRoutine = null;
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
