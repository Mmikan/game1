using System;
using System.Collections;
using System.Linq;
using Game1.Debuffs;
using Game1.Gameplay;
using Game1.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game1.Enemies
{
    // Opt-in game-side input acceptance. Never sends keyboard events to the desktop.
    public sealed class CoopInputScenario : MonoBehaviour
    {
        private bool failed;
        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int clientCapture = Array.IndexOf(args, "-game1-coop-client-capture");
            if (clientCapture >= 0 && clientCapture + 1 < args.Length)
            {
                yield return CaptureClientRescue(args[clientCapture + 1]);
                yield break;
            }
            if (Array.IndexOf(args, "-game1-ping-audio-client-smoke") >= 0)
            {
                yield return CheckClientPingAudio();
                yield break;
            }
            int flag = Array.IndexOf(args, "-game1-coop-input-smoke");
            if (flag < 0 || flag + 1 >= args.Length) yield break;
            string capturePrefix = args[flag + 1];
            var manager = GetComponent<NetworkManager>();
            float deadline = Time.time + 25f;
            while ((!manager.IsHost || manager.ConnectedClients.Count < 2) && Time.time < deadline) yield return null;
            if (!manager.IsHost || manager.ConnectedClients.Count != 2) { Application.Quit(1); yield break; }
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices.ToArray())
                if (device is Keyboard || device is Mouse) InputSystem.DisableDevice(device);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            InputSystem.AddDevice<Mouse>().MakeCurrent();
            var host = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerAvatar>();
            var client = manager.ConnectedClients.Values.First(c => c.ClientId != NetworkManager.ServerClientId).PlayerObject.GetComponent<NetworkPlayerAvatar>();
            Check(host.GetComponent<Renderer>() is { enabled: false } && client.GetComponent<Renderer>() is { enabled: true },
                "first_person_hides_own_avatar_only");
            host.transform.position = new Vector3(0f, 0f, -5f);
            client.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);
            Physics.SyncTransforms();
            float assignmentDeadline = Time.time + 5f;
            while (client.Debuff.Value == DebuffKind.None && Time.time < assignmentDeadline) yield return null;
            Check(client.Debuff.Value != DebuffKind.None, "client_initial_debuff_assigned_before_fixture");
            client.SetDebuffOnServer(DebuffKind.HearingLoss);
            yield return new WaitForSeconds(0.5f);
            client.transform.position = new Vector3(3f, 0f, -4f);
            Physics.SyncTransforms();
            var pitchField = typeof(NetworkPlayerAvatar).GetField("pitch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pitchField.SetValue(host, 45f);
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
            yield return new WaitForSeconds(0.2f);
            var pingMarker = GameObject.Find("NetworkPing");
            Check(pingMarker != null && pingMarker.GetComponent<Renderer>().sharedMaterial.shader.name.StartsWith("Universal Render Pipeline/"),
                "q_ping_uses_urp_material");
            PingMarkerView networkPing = pingMarker != null ? pingMarker.GetComponent<PingMarkerView>() : null;
            Check(networkPing != null && networkPing.IsNetworkPing && networkPing.Id > 0 &&
                  networkPing.ServerExpiresAt > manager.ServerTime.Time && PingMarkerView.Active.Count == 1,
                "q_ping_replicates_id_expiry_and_visual_entry");
            if (pingMarker != null)
            {
                Camera view = FindFirstObjectByType<LocalPlayerController>().ViewCamera;
                Vector3 screenPoint = view.WorldToScreenPoint(pingMarker.transform.position);
                Debug.Log($"GAME1_PING_VISUAL_DIAGNOSTIC marker={pingMarker.transform.position} camera={view.transform.position} screen={screenPoint} screen_size={Screen.width}x{Screen.height} color={pingMarker.GetComponent<Renderer>().sharedMaterial.color}");
                Check(screenPoint.z > 0.1f && screenPoint.x > 0f && screenPoint.x < Screen.width &&
                      screenPoint.y > 0f && screenPoint.y < Screen.height &&
                      Vector3.Distance(pingMarker.transform.position, view.transform.position) > 1f,
                    "q_ping_targets_visible_point_beyond_own_collider");
            }
            Check(GameObject.Find("Audible Ping")?.GetComponent<AudioSource>()?.clip != null, "q_ping_creates_audible_cue");
            AudioSource ownPingAudio = GameObject.Find("Audible Ping")?.GetComponent<AudioSource>();
            Check(ownPingAudio != null && ownPingAudio.spatialBlend > 0.99f && Mathf.Abs(ownPingAudio.volume - 1f) < 0.01f,
                "own_ping_keeps_spatial_audio");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-ping.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            var pingYawField = typeof(NetworkPlayerAvatar).GetField("yaw", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pingYawField.SetValue(host, 180f);
            pitchField.SetValue(host, 0f);
            yield return new WaitForSeconds(0.2f);
            Check(pingMarker != null && FindFirstObjectByType<LocalPlayerController>().ViewCamera.WorldToScreenPoint(pingMarker.transform.position).z < 0f,
                "q_ping_behind_camera_for_direction_capture");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-ping-behind.png");
            pingYawField.SetValue(host, 0f);
            client.transform.position = new Vector3(0f, 0f, -4f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.3f);
            Check(host.SupportingClientId.Value == client.OwnerClientId, "e_ray_starts_support");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-support.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.2f);
            Check(host.SupportingClientId.Value == CoopAuthorityRules.NoClient, "e_cancels_support");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            client.SetLifeStateOnServer(PlayerLifeState.Downed);
            host.transform.position = client.transform.position + Vector3.back;
            Physics.SyncTransforms();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(1f);
            Check(host.RescueProgress.Value > 0.2f && host.RescueProgress.Value < 1f, "e_ray_starts_rescue_progress");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-rescue.png");
            yield return new WaitForSeconds(1.3f);
            Check(client.LifeState.Value == PlayerLifeState.Alive, "e_hold_completes_rescue");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            var door = FindFirstObjectByType<NetworkDoorState>();
            door.transform.position = new Vector3(-1.5f, 0f, -2f);
            host.transform.position = new Vector3(0f, 0f, -3.5f);
            client.transform.position = new Vector3(4f, 0f, -4f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            Debug.Log($"GAME1_DOOR_INPUT_POSE host={host.transform.position} look={host.LookDirection.Value} door={door.transform.position}");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-door-open-hint.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.08f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.3f);
            Check(door.OpenAngle.Value == 0f && door.HeldByClient.Value == CoopAuthorityRules.NoClient, "short_press_does_not_open");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.12f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-door-progress.png");
            yield return new WaitForSeconds(0.38f);
            Check(door.OpenAngle.Value == 100f && door.HeldByClient.Value == host.OwnerClientId, "e_hold_opens_and_holds_door");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-door-held.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            Check(door.HeldByClient.Value == CoopAuthorityRules.NoClient, "e_release_frees_door");
            door.TryToggleOnServer(host.OwnerClientId);
            yield return new WaitForSeconds(0.7f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.5f);
            Check(door.HeldByClient.Value == host.OwnerClientId, "side_crossing_fixture_acquires_hold");
            host.transform.position = new Vector3(0f, 0f, -1f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            Check(door.HeldByClient.Value == CoopAuthorityRules.NoClient && door.OpenAngle.Value == 100f,
                "crossing_side_releases_without_closing");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.5f);
            host.transform.position = new Vector3(-1.75f, 0f, -1f);
            var yawField = typeof(NetworkPlayerAvatar).GetField("yaw", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            yawField.SetValue(host, 180f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            Check(!door.TrySetHeldOnServer(host.OwnerClientId, true), "opposite_side_cannot_hold");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-door-close-hint.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.5f);
            Check(door.OpenAngle.Value == 0f && door.HeldByClient.Value == CoopAuthorityRules.NoClient, "opposite_side_e_hold_closes");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.7f);
            host.transform.position = new Vector3(0f, 0f, -1f);
            Physics.SyncTransforms();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.5f);
            Check(door.OpenAngle.Value == 100f && door.HeldByClient.Value == CoopAuthorityRules.NoClient, "opposite_side_e_hold_opens_without_claim");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            host.transform.position = new Vector3(0f, 0f, -3.5f);
            yawField.SetValue(host, 0f);
            Physics.SyncTransforms();
            door.TryToggleOnServer(host.OwnerClientId);
            yield return new WaitForSeconds(0.7f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.5f);
            Check(door.HeldByClient.Value == host.OwnerClientId, "door_rehold_started");
            host.SetLifeStateOnServer(PlayerLifeState.Downed);
            yield return null;
            Check(door.HeldByClient.Value == CoopAuthorityRules.NoClient, "downed_releases_door");
            host.SetLifeStateOnServer(PlayerLifeState.Alive);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            door.TryToggleOnServer(host.OwnerClientId);
            yield return new WaitForSeconds(0.7f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.08f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E, Key.Escape));
            yield return new WaitForSeconds(0.3f);
            Check(NetworkSessionMenu.MenuOpen && door.OpenAngle.Value == 0f && door.HeldByClient.Value == CoopAuthorityRules.NoClient, "menu_cancels_pending_door");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.1f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.5f);
            Check(door.HeldByClient.Value == host.OwnerClientId, "lease_fixture_acquires_door");
            // Suppress only outbound input refresh, leaving server Update active.
            // This exercises lease expiry, not an actual transport outage.
            var nextInput = typeof(NetworkPlayerAvatar).GetField("nextInputTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            nextInput.SetValue(host, Time.unscaledTime + 2f);
            yield return new WaitForSeconds(0.7f);
            Check(door.HeldByClient.Value == CoopAuthorityRules.NoClient, "missing_input_refresh_releases_door");
            nextInput.SetValue(host, 0f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            door.TryToggleOnServer(host.OwnerClientId);
            yield return new WaitForSeconds(0.7f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.5f);
            Check(door.HeldByClient.Value == host.OwnerClientId, "distance_fixture_acquires_door");
            host.transform.position = new Vector3(0f, 0f, -7f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            Check(door.HeldByClient.Value == CoopAuthorityRules.NoClient, "distance_releases_door");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            client.transform.position = host.transform.position + Vector3.forward * 1.5f;
            var transferItem = FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None)
                .First(i => i.Definition.RequiredCarriers == 1 && !i.Definition.TwoHanded && i.Definition.Rarity != Game1.Items.ItemRarity.Curse);
            transferItem.transform.position = host.transform.position + Vector3.up;
            Check(transferItem.TryRequestCarryOnServer(host.OwnerClientId), "transfer_fixture_picks_up");
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(capturePrefix + "-transfer-prompt.png");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.2f);
            Check(transferItem.PrimaryCarrier.Value == client.OwnerClientId && transferItem.GetComponent<Rigidbody>().isKinematic,
                "e_ray_transfers_without_dropping");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSeconds(0.2f);
            Check(!transferItem.TryTransferOnServer(host.OwnerClientId, client.OwnerClientId), "non_owner_transfer_rejected");
            Check(transferItem.TryTransferOnServer(client.OwnerClientId, host.OwnerClientId), "transfer_back_to_empty_recipient");
            var occupiedItem = FindObjectsByType<NetworkCarryItem>(FindObjectsSortMode.None)
                .First(i => i != transferItem && i.Definition.RequiredCarriers == 1 && i.Definition.Rarity != Game1.Items.ItemRarity.Curse);
            occupiedItem.transform.position = client.transform.position + Vector3.up;
            Check(occupiedItem.TryRequestCarryOnServer(client.OwnerClientId), "recipient_occupied_fixture");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.2f);
            Check(transferItem.PrimaryCarrier.Value == host.OwnerClientId && occupiedItem.PrimaryCarrier.Value == client.OwnerClientId,
                "e_on_full_recipient_keeps_both_items");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            occupiedItem.ReleaseOnServer();
            client.SetLifeStateOnServer(PlayerLifeState.Downed);
            Check(!transferItem.TryTransferOnServer(host.OwnerClientId, client.OwnerClientId), "downed_recipient_transfer_rejected");
            client.SetLifeStateOnServer(PlayerLifeState.Alive);
            client.transform.position += Vector3.forward * 3f;
            Check(!transferItem.TryTransferOnServer(host.OwnerClientId, client.OwnerClientId), "distant_recipient_transfer_rejected");
            client.transform.position = host.transform.position + Vector3.forward * 1.5f;
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "TransferOcclusionFixture";
            barrier.transform.position = host.transform.position + Vector3.forward * 0.75f + Vector3.up * 1.5f;
            barrier.transform.localScale = new Vector3(2f, 2f, 0.1f);
            Physics.SyncTransforms();
            Check(!transferItem.TryTransferOnServer(host.OwnerClientId, client.OwnerClientId), "occluded_recipient_transfer_rejected");
            Destroy(barrier);
            yield return null;
            Check(transferItem.TryRequestCarryOnServer(client.OwnerClientId) && transferItem.IsSharedCarry, "shared_release_fixture_acquires_partner");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
            yield return new WaitForSeconds(0.2f);
            Check(transferItem.PrimaryCarrier.Value == CoopAuthorityRules.NoClient && !transferItem.IsSharedCarry,
                "e_still_releases_shared_item");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            transferItem.ReleaseOnServer();
            yield return new WaitForSeconds(0.2f);
            Vector3 lastPosition = host.transform.position;
            manager.Shutdown();
            yield return new WaitForSeconds(0.5f);
            var local = FindFirstObjectByType<LocalPlayerController>();
            Debug.Log($"GAME1_DISCONNECT_POSE last={lastPosition} local={local.transform.position} camera={local.ViewCamera.transform.position}");
            Check(local.enabled && local.GetComponent<CharacterController>().enabled &&
                  Vector3.Distance(local.transform.position, lastPosition) < 0.1f &&
                  Vector3.Distance(local.ViewCamera.transform.position, local.transform.position + Vector3.up * 1.62f) < 0.1f,
                  "disconnect_restores_local_controller_camera");
            Debug.Log($"GAME1_COOP_INPUT_COMPLETE success={!failed}");
            Application.Quit(failed ? 1 : 0);
        }
        private IEnumerator CaptureClientRescue(string path)
        {
            var manager = GetComponent<NetworkManager>();
            float deadline = Time.time + 30f;
            while (Time.time < deadline)
            {
                if (manager.IsConnectedClient && !manager.IsHost && manager.LocalClient.PlayerObject != null)
                {
                    var self = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerAvatar>();
                    var rescuer = FindObjectsByType<NetworkPlayerAvatar>(FindObjectsSortMode.None)
                        .FirstOrDefault(p => p.IsSpawned && p.RescueTarget.Value == self.NetworkObjectId && p.RescueProgress.Value > 0.25f);
                    if (rescuer != null && self.LifeState.Value == PlayerLifeState.Downed)
                    {
                        yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(path);
                        Debug.Log($"GAME1_CLIENT_HUD_CAPTURE progress={rescuer.RescueProgress.Value} debuff={self.Debuff.Value} path={path}");
                        yield break;
                    }
                }
                yield return null;
            }
            Debug.LogError("GAME1_CLIENT_HUD_CAPTURE timed out before replicated rescue progress");
        }
        private void Check(bool value, string label) { Debug.Log($"GAME1_COOP_INPUT_CHECK {label}={value}"); failed |= !value; }

        private IEnumerator CheckClientPingAudio()
        {
            var manager = GetComponent<NetworkManager>();
            float deadline = Time.time + 20f;
            while ((!manager.IsClient || manager.LocalClient?.PlayerObject == null) && Time.time < deadline) yield return null;
            if (!manager.IsClient || manager.LocalClient?.PlayerObject == null)
            {
                Debug.Log("GAME1_PING_AUDIO_CLIENT_CHECK connected=False");
                yield break;
            }
            var self = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerAvatar>();
            while (self.Debuff.Value != DebuffKind.HearingLoss && Time.time < deadline) yield return null;
            deadline = Time.time + 10f;
            while (GameObject.Find("Audible Ping") == null && Time.time < deadline) yield return null;
            AudioSource cue = GameObject.Find("Audible Ping")?.GetComponent<AudioSource>();
            bool valid = self.Debuff.Value == DebuffKind.HearingLoss && cue != null && cue.clip != null &&
                         cue.spatialBlend < 0.01f && cue.volume > 0f && cue.volume <= Mathf.Pow(10f, -18f / 20f);
            Debug.Log($"GAME1_PING_AUDIO_CLIENT_CHECK hearing_loss_teammate_ping={valid} debuff={self.Debuff.Value} spatial={(cue != null ? cue.spatialBlend : -1f)} volume={(cue != null ? cue.volume : -1f)}");
            PingMarkerView ping = PingMarkerView.Active.Count > 0 ? PingMarkerView.Active[0] : null;
            Debug.Log($"GAME1_PING_REPLICATION_CLIENT_CHECK id_and_expiry={ping != null && ping.IsNetworkPing && ping.Id > 0 && ping.ServerExpiresAt > manager.ServerTime.Time}");
        }
    }
}
