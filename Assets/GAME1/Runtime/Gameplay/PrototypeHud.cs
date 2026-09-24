using UnityEngine;
using UnityEngine.Localization.Settings;
using Game1.Config;
using Game1.Debuffs;
using Game1.Enemies;
using Game1.Network;

namespace Game1.Gameplay
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private LocalRunController run;
        [SerializeField] private LocalPlayerController player;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private ExitZone exitZone;
        [SerializeField] private GraphicsSettingsService graphics;
        private GUIStyle labelStyle;
        private GUIStyle centerStyle;
        public void Configure(LocalRunController runValue, LocalPlayerController playerValue, PlayerInteractor interactorValue, ExitZone exitValue, GraphicsSettingsService graphicsValue)
        {
            run = runValue;
            player = playerValue;
            interactor = interactorValue;
            exitZone = exitValue;
            graphics = graphicsValue;
        }

        private void OnGUI()
        {
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 22, normal = { textColor = Color.white } };
            centerStyle ??= new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
            var manager = Unity.Netcode.NetworkManager.Singleton;
            bool online = manager != null && manager.IsListening;
            NetworkPlayerAvatar avatar = online && manager.IsConnectedClient && manager.LocalClient.PlayerObject != null
                ? manager.LocalClient.PlayerObject.GetComponent<NetworkPlayerAvatar>() : null;
            NetworkStageState stage = online ? FindFirstObjectByType<NetworkStageState>() : null;
            bool hasStage = stage != null && stage.IsSpawned;
            float seconds = hasStage ? stage.RemainingSeconds.Value : run.RemainingSeconds;
            int sold = hasStage ? stage.SoldValue.Value : run.SoldValue;
            int targetValue = hasStage ? stage.TargetValue : run.TargetValue;
            RunState runState = hasStage ? stage.State.Value : run.State;
            PlayerLifeState life = avatar != null ? avatar.LifeState.Value : player.LifeState;
            if (life is PlayerLifeState.Downed or PlayerLifeState.Dead)
            {
                float remaining = avatar != null ? avatar.DownedSecondsRemaining.Value : player.DownedSecondsRemaining;
                string status = life == PlayerLifeState.Dead ? Text("hud.player.dead") : $"{Text("hud.player.downed")} {Mathf.CeilToInt(remaining)}s";
                GUI.Box(new Rect(Screen.width * 0.5f - 340, Screen.height * 0.56f, 680, 80), status, centerStyle);
            }
            int rescueRow = 0;
            if (avatar != null && avatar.SupportingClientId.Value != CoopAuthorityRules.NoClient)
                GUI.Label(new Rect(Screen.width * 0.5f - 260, Screen.height * 0.72f, 520, 40), Text("hud.player.supporting"), centerStyle);
            if (online)
                foreach (NetworkPlayerAvatar rescuer in FindObjectsByType<NetworkPlayerAvatar>(FindObjectsSortMode.None))
                    if (rescuer.IsSpawned && rescuer.RescueTarget.Value != CoopAuthorityRules.NoClient)
                    {
                        GUI.Label(new Rect(Screen.width * 0.5f - 260, 120 + rescueRow++ * 35, 520, 34),
                            $"{Text("hud.player.rescue")} {Mathf.RoundToInt(rescuer.RescueProgress.Value * 100f)}%", centerStyle);
                    }
            foreach (EnemyActor enemy in FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
            {
                if (NetworkPlayerAvatar.ListenerDebuff == DebuffKind.HearingLoss && Vector3.Distance(enemy.transform.position, player.ViewCamera.transform.position) <= 8f)
                    GUI.Label(new Rect(Screen.width * 0.5f - 250f, Screen.height * 0.2f, 500f, 40f), Text("hud.enemy.vibration"), centerStyle);
                if (enemy.IsRevealed)
                {
                    Vector3 point = player.ViewCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 2f);
                    if (point.z > 0f) GUI.Label(new Rect(point.x - 100f, Screen.height - point.y, 200f, 40f), Text("hud.enemy.revealed"), centerStyle);
                }
            }

            int wholeSeconds = Mathf.CeilToInt(seconds);
            string time = $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
            GUI.Label(new Rect(24, 20, 500, 36), $"{Text("hud.run.time")}: {time}", labelStyle);
            GUI.Label(new Rect(24, 54, 600, 36), $"{Text("hud.run.sold")}: ${sold} / ${targetValue}", labelStyle);
            GUI.Label(new Rect(Screen.width - 280, Screen.height - 64, 250, 36), $"{Text("hud.player.stamina")}: {Mathf.CeilToInt(avatar != null ? avatar.Stamina.Value : player.Stamina)}", labelStyle);

            if (!online && life == PlayerLifeState.Alive && interactor.Current != null)
            {
                string progress = interactor.Progress01 > 0f ? $" {Mathf.RoundToInt(interactor.Progress01 * 100f)}%" : string.Empty;
                GUI.Label(new Rect(Screen.width * 0.5f - 260, Screen.height * 0.72f, 520, 40), $"[E] {Text(interactor.Current.PromptKey)}{progress}", centerStyle);
            }

            if (!online && exitZone.Progress01 > 0f)
                GUI.Label(new Rect(Screen.width * 0.5f - 260, 80, 520, 40), $"{Text("hud.exit.progress")}: {Mathf.RoundToInt(exitZone.Progress01 * 100f)}%", centerStyle);

            if (runState is RunState.Succeeded or RunState.Failed)
            {
                string key = runState == RunState.Succeeded ? "result.success" : "result.failure";
                GUI.Label(new Rect(Screen.width * 0.5f - 300, Screen.height * 0.4f, 600, 80), Text(key), new GUIStyle(centerStyle) { fontSize = 42 });
            }

            if (graphics != null && GUI.Button(new Rect(24, Screen.height - 58, 190, 34), Text("settings.graphics.reset")))
                graphics.ResetGraphics();

            PlayerDebuffController debuff = player.GetComponent<PlayerDebuffController>();
            if (debuff != null && debuff.Kind != DebuffKind.None)
            {
                GUI.Label(new Rect(24, 88, 560, 36), $"{Text("hud.debuff")}: {Text(debuff.Definition.LocalizationKey)}", labelStyle);
                if (Debug.isDebugBuild) GUI.Label(new Rect(24, 120, 700, 30), Text("hud.debuff.debug_cycle"), labelStyle);
                if (debuff.TapeProgress01 > 0f) GUI.Label(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.64f, 300, 40), $"Tape {Mathf.RoundToInt(debuff.TapeProgress01 * 100f)}%", centerStyle);
                if (debuff.DropWarning) GUI.Label(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.58f, 300, 48), Text("hud.debuff.drop_warning"), centerStyle);
                if (debuff.TunnelVisionAlpha > 0f) DrawTunnelVision(debuff.TunnelVisionAlpha);
            }
        }

        private static void DrawTunnelVision(float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            float edge = Screen.width * 0.2f;
            GUI.DrawTexture(new Rect(0, 0, edge, Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - edge, 0, edge, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static string Text(string key)
        {
            string value = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
            return string.IsNullOrEmpty(value) ? $"[MISSING:{key}]" : value;
        }
    }
}
