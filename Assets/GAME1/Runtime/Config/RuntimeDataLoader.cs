using System;
using System.IO;
using UnityEngine;

namespace Game1.Config
{
    public static class RuntimeDataLoader
    {
        public static RunConfig LoadRunConfig(RunConfig source)
        {
            return LoadRunConfig(source, Path.Combine(Application.streamingAssetsPath, "run_config.json"));
        }

        public static RunConfig LoadRunConfig(RunConfig source, string overridePath)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            source.Validate();
            if (string.IsNullOrWhiteSpace(overridePath) || !File.Exists(overridePath)) return source;

            RunConfig runtime = UnityEngine.Object.Instantiate(source);
            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(overridePath), runtime);
                runtime.Validate();
                return runtime;
            }
            catch (Exception exception)
            {
                UnityEngine.Object.Destroy(runtime);
                Debug.LogWarning($"RunConfig override was ignored: {exception.Message}");
                return source;
            }
        }
    }
}
