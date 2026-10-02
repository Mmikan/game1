using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Game1.Enemies
{
    // World-space noise radii are a development aid; AI never reads these renderers.
    public sealed class GameplayNoiseDebugView : MonoBehaviour
    {
        private readonly Dictionary<NoiseKind, Material> materials = new();
        private Material sourceMaterial;
        private bool showing = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if ((!Application.isEditor && !Debug.isDebugBuild) ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                FindFirstObjectByType<GameplayNoiseDebugView>() != null) return;
            new GameObject("Gameplay Noise Debug").AddComponent<GameplayNoiseDebugView>();
            Debug.Log("GAME1_NOISE_DEBUG installed");
        }

        private void Awake()
        {
            sourceMaterial = Resources.Load<Material>("GameplayNoiseDebugUnlit");
            if (sourceMaterial == null)
            {
                Debug.LogError("GAME1_NOISE_DEBUG missing unlit material");
                enabled = false;
            }
        }

        private void OnEnable() => GameplayNoise.Emitted += Show;
        private void OnDisable() => GameplayNoise.Emitted -= Show;

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.f8Key.wasPressedThisFrame) return;
            showing = !showing;
            if (!showing)
                foreach (Transform child in transform) Destroy(child.gameObject);
            Debug.Log($"GAME1_NOISE_DEBUG visible={showing}");
        }

        private void Show(GameplayNoiseEvent noise)
        {
            if (!showing || !GameplayNoise.HasAuthority) return;
            if (!materials.TryGetValue(noise.Kind, out Material material))
            {
                material = new Material(sourceMaterial) { color = ColorFor(noise.Kind) };
                materials.Add(noise.Kind, material);
                Debug.Log($"GAME1_NOISE_DEBUG kind={noise.Kind} shader={material.shader.name}");
            }

            GameObject circle = new($"Noise {noise.Kind}");
            circle.transform.SetParent(transform, false);
            LineRenderer line = circle.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 64;
            line.widthMultiplier = 0.07f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, noise.Position + new Vector3(Mathf.Cos(angle) * noise.Radius, 0.1f, Mathf.Sin(angle) * noise.Radius));
            }
            Destroy(circle, 1.5f);
        }

        private static Color ColorFor(NoiseKind kind) => kind switch
        {
            NoiseKind.Ping => Color.cyan,
            NoiseKind.Curse => Color.magenta,
            NoiseKind.Throw or NoiseKind.Drop => Color.yellow,
            NoiseKind.Door => Color.green,
            _ => Color.white
        };

        private void OnDestroy()
        {
            foreach (Material material in materials.Values) Destroy(material);
        }
    }
}
