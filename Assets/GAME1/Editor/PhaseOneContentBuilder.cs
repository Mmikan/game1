using System.Collections.Generic;
using Game1.Config;
using Game1.Gameplay;
using Game1.Items;
using Game1.Network;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;

namespace Game1.Editor
{
    public static class PhaseOneContentBuilder
    {
        private const string Root = "Assets/GAME1";
        private const string DataPath = Root + "/Data";
        private const string ScenePath = Root + "/Scenes/Phase1Prototype.unity";

        [MenuItem("GAME1/Build Phase 1 Prototype")]
        public static void Execute()
        {
            EnsureFolder(DataPath);
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Localization");
            // Localization creates its backing Addressables settings here. Creating the
            // folder through AssetDatabase first keeps batch mode from loading a null
            // settings asset immediately after Directory.CreateDirectory.
            EnsureFolder("Assets/AddressableAssetsData");
            EnsureRenderPipeline();
            RunConfig runConfig = CreateRunConfig();
            GraphicsPresetDefinition mediumPreset = CreateGraphicsPreset("Medium", GraphicsPresetId.Medium, 1920, 1080, 1f, 60, true);
            GraphicsPresetDefinition safePreset = CreateGraphicsPreset("SafeLow", GraphicsPresetId.Low, 1280, 720, 0.7f, 60, true);
            Dictionary<string, ItemDefinition> definitions = CreateItemDefinitions();
            CreateLocalization();
            CreateScene(runConfig, mediumPreset, safePreset, definitions);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"GAME1 Phase 1 content generated: {ScenePath}");
        }

        [MenuItem("GAME1/Build Windows Prototype")]
        public static void BuildWindows()
        {
            Execute();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/GAME1.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception($"Windows build failed: {report.summary.result}");
            Debug.Log($"GAME1 Windows build succeeded: {report.summary.outputPath} ({report.summary.totalSize} bytes)");
        }

        private static void EnsureRenderPipeline()
        {
            const string rendererPath = DataPath + "/PrototypeRenderer.asset";
            const string pipelinePath = DataPath + "/PrototypeURP.asset";
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        private static GraphicsPresetDefinition CreateGraphicsPreset(string name, GraphicsPresetId id, int width, int height, float scale, int fps, bool vsync)
        {
            string path = $"{DataPath}/Graphics{name}.asset";
            GraphicsPresetDefinition asset = AssetDatabase.LoadAssetAtPath<GraphicsPresetDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<GraphicsPresetDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            SerializedObject serialized = new(asset);
            serialized.FindProperty("presetId").enumValueIndex = (int)id;
            serialized.FindProperty("width").intValue = width;
            serialized.FindProperty("height").intValue = height;
            serialized.FindProperty("renderScale").floatValue = scale;
            serialized.FindProperty("fpsLimit").intValue = fps;
            serialized.FindProperty("verticalSync").boolValue = vsync;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static RunConfig CreateRunConfig()
        {
            string path = DataPath + "/RunConfig.asset";
            RunConfig asset = AssetDatabase.LoadAssetAtPath<RunConfig>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RunConfig>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static Dictionary<string, ItemDefinition> CreateItemDefinitions()
        {
            var data = new (string id, int value, float weight, ItemSize size, int fragility, float noise, bool twoHanded, int carriers, int spawnWeight, float brokenMultiplier)[]
            {
                ("plush_bear", 80, 0.7f, ItemSize.S, 5, 0f, false, 1, 18, 0.5f),
                ("toy_car", 110, 1.2f, ItemSize.S, 35, 3f, false, 1, 16, 0.5f),
                ("music_box", 180, 1.5f, ItemSize.S, 75, 5f, false, 1, 12, 0.5f),
                ("dollhouse", 260, 5f, ItemSize.M, 55, 4f, true, 1, 10, 0.5f),
                ("robot", 330, 9f, ItemSize.L, 20, 5f, true, 1, 8, 0.5f),
                ("glass_unicorn", 420, 2f, ItemSize.M, 95, 7f, false, 1, 6, 0.1f),
                ("giant_block", 620, 22f, ItemSize.XL, 10, 7f, true, 2, 4, 0.5f)
            };
            var result = new Dictionary<string, ItemDefinition>();
            foreach (var entry in data)
            {
                string path = $"{DataPath}/{entry.id}.asset";
                ItemDefinition asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<ItemDefinition>();
                    AssetDatabase.CreateAsset(asset, path);
                }
                SerializedObject serialized = new(asset);
                serialized.FindProperty("itemId").stringValue = entry.id;
                serialized.FindProperty("localizationKey").stringValue = $"item.{entry.id}.name";
                serialized.FindProperty("value").intValue = entry.value;
                serialized.FindProperty("weightKg").floatValue = entry.weight;
                serialized.FindProperty("sizeClass").enumValueIndex = (int)entry.size;
                serialized.FindProperty("fragility").intValue = entry.fragility;
                serialized.FindProperty("noiseRadius").floatValue = entry.noise;
                serialized.FindProperty("twoHanded").boolValue = entry.twoHanded;
                serialized.FindProperty("spawnWeight").intValue = entry.spawnWeight;
                serialized.FindProperty("durability").intValue = 100;
                serialized.FindProperty("brokenValueMultiplier").floatValue = entry.brokenMultiplier;
                serialized.FindProperty("requiredCarriers").intValue = entry.carriers;
                serialized.FindProperty("throwable").boolValue = !entry.twoHanded;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                asset.Validate();
                result.Add(entry.id, asset);
            }
            return result;
        }

        private static void CreateLocalization()
        {
            var values = new Dictionary<string, string[]>
            {
                ["hud.run.time"] = new[] { "残り時間", "Time", "剩余时间", "남은 시간" },
                ["hud.run.sold"] = new[] { "売却額", "Sold", "已售金额", "판매 금액" },
                ["hud.player.stamina"] = new[] { "スタミナ", "Stamina", "耐力", "스태미나" },
                ["hud.interact.pick_up"] = new[] { "拾う", "Pick up", "拾取", "줍기" },
                ["hud.interact.open_door"] = new[] { "ドアを開ける", "Open door", "开门", "문 열기" },
                ["hud.interact.close_door"] = new[] { "ドアを閉める", "Close door", "关门", "문 닫기" },
                ["hud.exit.progress"] = new[] { "脱出", "Escaping", "逃脱", "탈출" },
                ["result.success"] = new[] { "脱出成功", "ESCAPED", "逃脱成功", "탈출 성공" },
                ["result.failure"] = new[] { "任務失敗", "RUN FAILED", "任务失败", "임무 실패" }
                , ["settings.graphics.reset"] = new[] { "画面設定をリセット", "Reset Graphics", "重置画面设置", "그래픽 초기화" }
            };
            var locales = new[] { ("ja-JP", "Japanese (Japan)"), ("en-US", "English (US)"), ("zh-Hans", "Chinese (Simplified)"), ("ko-KR", "Korean") };
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("UI");
            if (collection != null) return;
            collection = LocalizationEditorSettings.CreateStringTableCollection("UI", Root + "/Localization");
            for (int localeIndex = 0; localeIndex < locales.Length; localeIndex++)
            {
                Locale locale = FindOrCreateLocale(locales[localeIndex].Item1, locales[localeIndex].Item2);
                StringTable table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null) table = collection.AddNewTable(locale.Identifier) as StringTable;
                foreach (KeyValuePair<string, string[]> pair in values)
                    table.AddEntry(pair.Key, pair.Value[localeIndex]).Value = pair.Value[localeIndex];
                EditorUtility.SetDirty(table);
            }
        }

        private static Locale FindOrCreateLocale(string code, string displayName)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(code);
            if (locale != null) return locale;
            locale = Locale.CreateLocale(code);
            locale.name = displayName;
            AssetDatabase.CreateAsset(locale, $"{Root}/Localization/{code}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void CreateScene(RunConfig config, GraphicsPresetDefinition defaultPreset, GraphicsPresetDefinition safePreset, IReadOnlyDictionary<string, ItemDefinition> definitions)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject systems = new("Systems");
            LocalRunController run = systems.AddComponent<LocalRunController>();
            run.Configure(config);
            GraphicsSettingsService graphics = systems.AddComponent<GraphicsSettingsService>();
            graphics.Configure(defaultPreset, safePreset);
            CreateNetworkFoundation(systems, config);

            LocalPlayerController player = CreatePlayer();
            PlayerInteractor interactor = player.gameObject.AddComponent<PlayerInteractor>();
            player.gameObject.AddComponent<PingController>();

            CreateBox("Floor", new Vector3(0f, -0.5f, 12f), new Vector3(34f, 1f, 42f), new Color(0.12f, 0.13f, 0.16f));
            CreateBox("BackWall", new Vector3(0f, 2.5f, 32f), new Vector3(34f, 6f, 1f), Color.gray);
            CreateBox("LeftWall", new Vector3(-17f, 2.5f, 12f), new Vector3(1f, 6f, 42f), Color.gray);
            CreateBox("RightWall", new Vector3(17f, 2.5f, 12f), new Vector3(1f, 6f, 42f), Color.gray);
            CreateBox("EntranceWallLeft", new Vector3(-10f, 2.5f, -9f), new Vector3(14f, 6f, 1f), Color.gray);
            CreateBox("EntranceWallRight", new Vector3(10f, 2.5f, -9f), new Vector3(14f, 6f, 1f), Color.gray);
            CreateDoor();
            for (int row = 0; row < 3; row++)
            {
                CreateBox($"ShelfL{row}", new Vector3(-8f, 1.25f, 4f + row * 8f), new Vector3(5f, 2.5f, 1.2f), new Color(0.23f, 0.18f, 0.14f));
                CreateBox($"ShelfR{row}", new Vector3(8f, 1.25f, 4f + row * 8f), new Vector3(5f, 2.5f, 1.2f), new Color(0.23f, 0.18f, 0.14f));
            }

            SellCart cart = CreateTrigger<SellCart>("SellCart", new Vector3(-5f, 0.75f, -5f), new Vector3(3f, 1.5f, 3f), new Color(0.12f, 0.55f, 0.18f, 0.5f));
            cart.Configure(run);
            ExitZone exit = CreateTrigger<ExitZone>("ExitZone", new Vector3(0f, 1.5f, -8f), new Vector3(5f, 3f, 2f), new Color(0.1f, 0.4f, 0.8f, 0.35f));
            exit.Configure(run);

            int index = 0;
            foreach (ItemDefinition definition in definitions.Values)
            {
                CreateItem(definition, new Vector3(-12f + (index % 3) * 12f, 1f, 5f + (index / 3) * 12f));
                index++;
            }
            CreateItem(definitions["glass_unicorn"], new Vector3(12f, 1f, 25f));
            CreateItem(definitions["robot"], new Vector3(-12f, 1f, 25f));
            CreateItem(definitions["giant_block"], new Vector3(0f, 1f, 28f));

            GameObject hudObject = new("HUD");
            PrototypeHud hud = hudObject.AddComponent<PrototypeHud>();
            hud.Configure(run, player, interactor, exit, graphics);

            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.12f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static LocalPlayerController CreatePlayer()
        {
            GameObject root = new("LocalPlayer");
            root.transform.position = new Vector3(0f, 0f, -4f);
            CharacterController character = root.AddComponent<CharacterController>();
            character.height = 1.75f;
            character.radius = 0.32f;
            character.center = Vector3.up * 0.875f;
            LocalPlayerController controller = root.AddComponent<LocalPlayerController>();

            GameObject cameraObject = new("ViewCamera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = Vector3.up * 1.62f;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 85f;
            cameraObject.AddComponent<AudioListener>();
            GameObject anchor = new("CarryAnchor");
            anchor.transform.SetParent(cameraObject.transform, false);
            anchor.transform.localPosition = new Vector3(0f, -0.2f, 0.9f);
            Light light = cameraObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 15f;
            light.spotAngle = 35f;
            light.intensity = 2f;
            light.enabled = false;
            controller.Configure(camera, anchor.transform, light);
            return controller;
        }

        private static void CreateItem(ItemDefinition definition, Vector3 position)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = definition.ItemId;
            item.transform.position = position;
            item.transform.localScale = definition.TwoHanded ? new Vector3(1.2f, 0.9f, 0.8f) : Vector3.one * 0.55f;
            Rigidbody body = item.AddComponent<Rigidbody>();
            body.mass = definition.WeightKg;
            PickupItem pickup = item.AddComponent<PickupItem>();
            pickup.Configure(definition);
            item.AddComponent<NetworkObject>();
            item.AddComponent<NetworkTransform>();
            NetworkCarryItem networkItem = item.AddComponent<NetworkCarryItem>();
            networkItem.Configure(definition);
        }

        private static void CreateDoor()
        {
            GameObject pivot = new("EntranceDoor");
            pivot.transform.position = new Vector3(-1.5f, 0f, -8.5f);
            InteractableDoor door = pivot.AddComponent<InteractableDoor>();
            GameObject panel = CreateBox("DoorPanel", new Vector3(0f, 1.5f, 0f), new Vector3(3f, 3f, 0.25f), new Color(0.28f, 0.2f, 0.12f));
            panel.transform.SetParent(pivot.transform, true);
            panel.transform.localPosition = new Vector3(1.5f, 1.5f, 0f);
            door.Configure(pivot.transform);
            pivot.AddComponent<NetworkObject>();
            NetworkDoorState networkDoor = pivot.AddComponent<NetworkDoorState>();
            networkDoor.Configure(pivot.transform);
        }

        private static void CreateNetworkFoundation(GameObject systems, RunConfig config)
        {
            NetworkManager manager = systems.AddComponent<NetworkManager>();
            UnityTransport transport = systems.AddComponent<UnityTransport>();
            NetworkSessionMenu menu = systems.AddComponent<NetworkSessionMenu>();
            menu.Configure(manager, transport);
            NetworkCoopSmokeDriver smoke = systems.AddComponent<NetworkCoopSmokeDriver>();
            smoke.Configure(manager);
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = CreateNetworkPlayerPrefab();

            NetworkObject stateObject = systems.AddComponent<NetworkObject>();
            NetworkStageState stage = systems.AddComponent<NetworkStageState>();
            stage.Configure(config);
        }

        private static GameObject CreateNetworkPlayerPrefab()
        {
            const string path = DataPath + "/NetworkPlayer.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            avatar.name = "NetworkPlayer";
            avatar.AddComponent<NetworkObject>();
            avatar.AddComponent<NetworkTransform>();
            avatar.AddComponent<NetworkPlayerAvatar>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(avatar, path);
            Object.DestroyImmediate(avatar);
            return prefab;
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetPositionAndRotation(position, Quaternion.identity);
            box.transform.localScale = scale;
            SetColor(box, color);
            return box;
        }

        private static T CreateTrigger<T>(string name, Vector3 position, Vector3 scale, Color color) where T : Component
        {
            GameObject box = CreateBox(name, position, scale, color);
            box.GetComponent<BoxCollider>().isTrigger = true;
            return box.AddComponent<T>();
        }

        private static void SetColor(GameObject target, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { color = color };
            target.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.GetType().Name}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
