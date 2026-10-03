using System;
using System.Collections;
using System.Linq;
using Game1.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game1.Enemies
{
    public sealed class SoloEnemySmokeScenario : MonoBehaviour
    {
        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int capture = Array.IndexOf(args, "-game1-hud-capture");
            if (capture >= 0 && capture + 1 < args.Length)
            {
                yield return new WaitForSeconds(3f);
                FindFirstObjectByType<LocalPlayerController>().Down();
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(args[capture + 1]);
                yield return new WaitForSeconds(1f);
                Application.Quit(0);
                yield break;
            }
            int pingCapture = Array.IndexOf(args, "-game1-solo-ping-smoke");
            if (pingCapture >= 0 && pingCapture + 1 < args.Length)
            {
                yield return CheckSoloPing(args[pingCapture + 1]);
                yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-game1-enemy-solo-smoke") < 0) yield break;
            LocalPlayerController player = FindFirstObjectByType<LocalPlayerController>();
            EnemyActor[] enemies = FindObjectsByType<EnemyActor>(FindObjectsSortMode.None);
            EnemyActor mannequin = enemies.First(e => e.Kind == EnemyKind.Mannequin);
            EnemyActor collector = enemies.First(e => e.Kind == EnemyKind.Collector);
            player.transform.position = new Vector3(0f, 0f, 26f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            player.ViewCamera.GetComponent<Light>().enabled = true;
            float deadline = Time.time + 5f;
            while (mannequin.State != EnemyState.Freeze && Time.time < deadline) yield return null;
            bool frozen = mannequin.State == EnemyState.Freeze;
            Debug.Log($"GAME1_SOLO_CHECK freeze={frozen}");
            player.ViewCamera.GetComponent<Light>().enabled = false;
            player.transform.position = new Vector3(0f, 0f, -4f);
            FindFirstObjectByType<LocalRunController>().Sell(500);
            deadline = Time.time + 20f;
            while (collector.State != EnemyState.Steal && Time.time < deadline) yield return null;
            bool steal = collector.State == EnemyState.Steal;
            Debug.Log($"GAME1_SOLO_CHECK steal={steal}");
            Debug.Log($"GAME1_SOLO_SMOKE_COMPLETE success={frozen && steal}");
            Application.Quit(frozen && steal ? 0 : 1);
        }

        private static IEnumerator CheckSoloPing(string screenshotPath)
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices.ToArray())
                if (device is Keyboard or Mouse) InputSystem.DisableDevice(device);
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            LocalPlayerController player = FindFirstObjectByType<LocalPlayerController>();
            player.transform.position = new Vector3(0f, 0f, -5f);
            var pitchField = typeof(LocalPlayerController).GetField("pitch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pitchField.SetValue(player, 45f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
            yield return new WaitForSeconds(0.2f);
            GameObject marker = GameObject.Find("PingMarker");
            Vector3 screen = marker != null ? player.ViewCamera.WorldToScreenPoint(marker.transform.position) : Vector3.zero;
            bool visible = marker != null && screen.z > 0.1f && screen.x > 0f && screen.x < Screen.width &&
                           screen.y > 0f && screen.y < Screen.height &&
                           Vector3.Distance(marker.transform.position, player.ViewCamera.transform.position) is > 1f and < 10f &&
                           marker.GetComponent<Renderer>().sharedMaterial.shader.name == "Universal Render Pipeline/Unlit";
            Debug.Log($"GAME1_SOLO_PING_CHECK visible={visible} marker={(marker != null ? marker.transform.position.ToString() : "missing")} screen={screen}");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(screenshotPath);
            yield return new WaitForSeconds(1f);
            Application.Quit(visible ? 0 : 1);
        }
    }
}
