using System.Collections;
using System.Globalization;
using System.Reflection;
using Game1.Config;
using Game1.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game1.Tests
{
    public sealed class LocalRunFlowTests
    {
        [UnityTest]
        public IEnumerator SellTargetThenEscape_CompletesRun()
        {
            (GameObject root, LocalRunController run, RunConfig config) = CreateRun();
            yield return null;

            run.Sell(config.TargetValue);
            Assert.IsTrue(run.ExitUnlocked);
            run.CompleteEscape();
            Assert.AreEqual(RunState.Succeeded, run.State);

            Object.Destroy(root);
            Object.Destroy(config);
        }

        [UnityTest]
        public IEnumerator Timeout_FailsRun()
        {
            (GameObject root, LocalRunController run, RunConfig config) = CreateRun(0.01f);
            yield return new WaitForSeconds(0.05f);
            Assert.AreEqual(RunState.Failed, run.State);

            Object.Destroy(root);
            Object.Destroy(config);
        }

        private static (GameObject root, LocalRunController run, RunConfig config) CreateRun(float? timeLimit = null)
        {
            RunConfig config = ScriptableObject.CreateInstance<RunConfig>();
            if (timeLimit.HasValue)
            {
                string seconds = timeLimit.Value.ToString(CultureInfo.InvariantCulture);
                JsonUtility.FromJsonOverwrite($"{{\"timeLimitSeconds\":{seconds}}}", config);
            }

            GameObject root = new("LocalRunFlowTest");
            root.SetActive(false);
            LocalRunController run = root.AddComponent<LocalRunController>();
            FieldInfo field = typeof(LocalRunController).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(run, config);
            root.SetActive(true);
            return (root, run, config);
        }
    }
}
