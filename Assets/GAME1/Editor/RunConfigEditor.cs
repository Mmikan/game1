using System;
using System.IO;
using Game1.Config;
using Game1.Items;
using UnityEditor;
using UnityEngine;

namespace Game1.Editor
{
    [CustomEditor(typeof(RunConfig))]
    public sealed class RunConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RunConfig config = (RunConfig)target;
            if (GUILayout.Button("Validate Config"))
            {
                try
                {
                    config.Validate();
                    Debug.Log("Config is valid.");
                }
                catch (InvalidOperationException ex)
                {
                    Debug.LogError(ex.Message);
                }
            }
        }
    }

    public static class DataJsonExporter
    {
        [MenuItem("GAME1/Export Data JSON")]
        public static void Export()
        {
            const string outputRoot = "Exports/Data";
            Directory.CreateDirectory(outputRoot);
            string[] runGuids = AssetDatabase.FindAssets("t:RunConfig", new[] { "Assets/GAME1/Data" });
            if (runGuids.Length > 0)
            {
                RunConfig run = AssetDatabase.LoadAssetAtPath<RunConfig>(AssetDatabase.GUIDToAssetPath(runGuids[0]));
                File.WriteAllText(Path.Combine(outputRoot, "run_config.json"), JsonUtility.ToJson(run, true));
            }

            string[] itemGuids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/GAME1/Data" });
            foreach (string guid in itemGuids)
            {
                ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                File.WriteAllText(Path.Combine(outputRoot, item.ItemId + ".json"), JsonUtility.ToJson(item, true));
            }
            Debug.Log($"Exported GAME1 data JSON to {Path.GetFullPath(outputRoot)}");
        }
    }
}
