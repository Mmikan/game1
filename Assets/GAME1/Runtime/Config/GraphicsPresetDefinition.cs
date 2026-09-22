using UnityEngine;

namespace Game1.Config
{
    public enum GraphicsPresetId { Laptop, Low, Medium, High, Custom }

    [CreateAssetMenu(fileName = "GraphicsPreset", menuName = "GAME1/Graphics Preset")]
    public sealed class GraphicsPresetDefinition : ScriptableObject
    {
        [SerializeField] private GraphicsPresetId presetId = GraphicsPresetId.Medium;
        [SerializeField, Min(640)] private int width = 1920;
        [SerializeField, Min(360)] private int height = 1080;
        [SerializeField, Range(0.5f, 1f)] private float renderScale = 1f;
        [SerializeField, Min(0)] private int fpsLimit = 60;
        [SerializeField] private bool verticalSync = true;
        [SerializeField] private FullScreenMode displayMode = FullScreenMode.FullScreenWindow;

        public GraphicsPresetId PresetId => presetId;
        public int Width => width;
        public int Height => height;
        public float RenderScale => renderScale;
        public int FpsLimit => fpsLimit;
        public bool VerticalSync => verticalSync;
        public FullScreenMode DisplayMode => displayMode;
    }
}
