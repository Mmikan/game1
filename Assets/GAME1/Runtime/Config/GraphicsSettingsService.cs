using System;
using System.IO;
using UnityEngine;

namespace Game1.Config
{
    public sealed class GraphicsSettingsService : MonoBehaviour
    {
        private const int CurrentSchemaVersion = 1;
        [SerializeField] private GraphicsPresetDefinition defaultPreset;
        [SerializeField] private GraphicsPresetDefinition safePreset;
        [SerializeField, Min(1f)] private float confirmationSeconds = 15f;

        private Snapshot current;
        private Snapshot beforePending;
        private float pendingDeadline;

        public bool IsAwaitingConfirmation => pendingDeadline > 0f;
        public float ConfirmationSecondsRemaining => IsAwaitingConfirmation ? Mathf.Max(0f, pendingDeadline - Time.unscaledTime) : 0f;
        public void Configure(GraphicsPresetDefinition defaultValue, GraphicsPresetDefinition safeValue)
        {
            defaultPreset = defaultValue;
            safePreset = safeValue;
        }

        private string SavePath => Path.Combine(Application.persistentDataPath, "graphics.json");

        private void Awake()
        {
            try
            {
                current = Load() ?? Snapshot.From(defaultPreset);
                current.Validate();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Graphics settings were invalid and safe settings were applied: {exception.Message}");
                current = Snapshot.From(safePreset != null ? safePreset : defaultPreset);
            }
            ApplyNow(current);
        }

        private void Update()
        {
            if (IsAwaitingConfirmation && Time.unscaledTime >= pendingDeadline) Revert();
        }

        public void ApplyTemporary(GraphicsPresetDefinition preset)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));
            beforePending = current;
            current = Snapshot.From(preset);
            ApplyNow(current);
            pendingDeadline = Time.unscaledTime + confirmationSeconds;
        }

        public void Confirm()
        {
            if (!IsAwaitingConfirmation) return;
            pendingDeadline = 0f;
            Save(current);
        }

        public void Revert()
        {
            if (!IsAwaitingConfirmation) return;
            current = beforePending;
            pendingDeadline = 0f;
            ApplyNow(current);
        }

        public void ResetGraphics()
        {
            pendingDeadline = 0f;
            if (File.Exists(SavePath)) File.Delete(SavePath);
            current = Snapshot.From(defaultPreset);
            ApplyNow(current);
            Save(current);
        }

        private Snapshot Load()
        {
            if (!File.Exists(SavePath)) return null;
            return JsonUtility.FromJson<Snapshot>(File.ReadAllText(SavePath));
        }

        private void Save(Snapshot snapshot)
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath, JsonUtility.ToJson(snapshot, true));
        }

        private static void ApplyNow(Snapshot snapshot)
        {
            QualitySettings.vSyncCount = snapshot.verticalSync ? 1 : 0;
            Application.targetFrameRate = snapshot.fpsLimit;
            ScalableBufferManager.ResizeBuffers(snapshot.renderScale, snapshot.renderScale);
            Screen.SetResolution(snapshot.width, snapshot.height, (FullScreenMode)snapshot.displayMode);
        }

        [Serializable]
        private sealed class Snapshot
        {
            public int schemaVersion;
            public int presetId;
            public int width;
            public int height;
            public float renderScale;
            public int fpsLimit;
            public bool verticalSync;
            public int displayMode;

            public static Snapshot From(GraphicsPresetDefinition preset)
            {
                if (preset == null) throw new InvalidOperationException("A graphics preset is required.");
                return new Snapshot
                {
                    schemaVersion = CurrentSchemaVersion,
                    presetId = (int)preset.PresetId,
                    width = preset.Width,
                    height = preset.Height,
                    renderScale = preset.RenderScale,
                    fpsLimit = preset.FpsLimit,
                    verticalSync = preset.VerticalSync,
                    displayMode = (int)preset.DisplayMode
                };
            }

            public void Validate()
            {
                if (schemaVersion != CurrentSchemaVersion) throw new InvalidDataException("Unsupported schema version.");
                if (width < 640 || height < 360) throw new InvalidDataException("Resolution is too small.");
                if (renderScale is < 0.5f or > 1f) throw new InvalidDataException("Render scale is out of range.");
                if (fpsLimit < 0) throw new InvalidDataException("FPS limit is invalid.");
                if (!Enum.IsDefined(typeof(FullScreenMode), displayMode)) throw new InvalidDataException("Display mode is invalid.");
            }
        }
    }
}
