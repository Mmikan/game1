using UnityEngine;
using UnityEngine.Localization.Settings;
using Game1.Config;

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

            string time = $"{Mathf.CeilToInt(run.RemainingSeconds / 60f):00}:{Mathf.CeilToInt(run.RemainingSeconds % 60f):00}";
            GUI.Label(new Rect(24, 20, 500, 36), $"{Text("hud.run.time")}: {time}", labelStyle);
            GUI.Label(new Rect(24, 54, 600, 36), $"{Text("hud.run.sold")}: ${run.SoldValue} / ${run.TargetValue}", labelStyle);
            GUI.Label(new Rect(Screen.width - 280, Screen.height - 64, 250, 36), $"{Text("hud.player.stamina")}: {Mathf.CeilToInt(player.Stamina)}", labelStyle);

            if (interactor.Current != null)
            {
                string progress = interactor.Progress01 > 0f ? $" {Mathf.RoundToInt(interactor.Progress01 * 100f)}%" : string.Empty;
                GUI.Label(new Rect(Screen.width * 0.5f - 260, Screen.height * 0.72f, 520, 40), $"[E] {Text(interactor.Current.PromptKey)}{progress}", centerStyle);
            }

            if (exitZone.Progress01 > 0f)
                GUI.Label(new Rect(Screen.width * 0.5f - 260, 80, 520, 40), $"{Text("hud.exit.progress")}: {Mathf.RoundToInt(exitZone.Progress01 * 100f)}%", centerStyle);

            if (run.State is RunState.Succeeded or RunState.Failed)
            {
                string key = run.State == RunState.Succeeded ? "result.success" : "result.failure";
                GUI.Label(new Rect(Screen.width * 0.5f - 300, Screen.height * 0.4f, 600, 80), Text(key), new GUIStyle(centerStyle) { fontSize = 42 });
            }

            if (graphics != null && GUI.Button(new Rect(24, Screen.height - 58, 190, 34), Text("settings.graphics.reset")))
                graphics.ResetGraphics();
        }

        private static string Text(string key)
        {
            string value = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
            return string.IsNullOrEmpty(value) ? $"[MISSING:{key}]" : value;
        }
    }
}
