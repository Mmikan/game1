using System;
using System.Collections;
using System.Linq;
using Game1.Gameplay;
using UnityEngine;

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
    }
}
