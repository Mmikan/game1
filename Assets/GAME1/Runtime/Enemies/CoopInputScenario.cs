using System;
using System.Collections;
using System.Linq;
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
            int flag = Array.IndexOf(args, "-game1-coop-input-smoke");
            if (flag < 0 || flag + 1 >= args.Length) yield break;
            string capturePrefix = args[flag + 1];
            var manager = GetComponent<NetworkManager>();
            float deadline = Time.time + 25f;
            while ((!manager.IsHost || manager.ConnectedClients.Count < 2) && Time.time < deadline) yield return null;
            if (!manager.IsHost || manager.ConnectedClients.Count != 2) { Application.Quit(1); yield break; }
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            var host = manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerAvatar>();
            var client = manager.ConnectedClients.Values.First(c => c.ClientId != NetworkManager.ServerClientId).PlayerObject.GetComponent<NetworkPlayerAvatar>();
            host.transform.position = new Vector3(0f, 0f, -5f);
            client.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);
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
            Vector3 lastPosition = host.transform.position;
            manager.Shutdown();
            yield return new WaitForSeconds(0.5f);
            var local = FindFirstObjectByType<LocalPlayerController>();
            Check(local.enabled && local.GetComponent<CharacterController>().enabled &&
                  Vector3.Distance(local.transform.position, lastPosition) < 0.1f &&
                  Vector3.Distance(local.ViewCamera.transform.position, local.transform.position + Vector3.up * 1.62f) < 0.1f,
                  "disconnect_restores_local_controller_camera");
            Debug.Log($"GAME1_COOP_INPUT_COMPLETE success={!failed}");
            Application.Quit(failed ? 1 : 0);
        }
        private void Check(bool value, string label) { Debug.Log($"GAME1_COOP_INPUT_CHECK {label}={value}"); failed |= !value; }
    }
}
