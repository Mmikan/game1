using Game1.Config;
using Game1.Items;
using Game1.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game1.Tests
{
    public sealed class DataDefinitionTests
    {
        [Test]
        public void RunConfig_DefaultValuesAreValid()
        {
            RunConfig config = ScriptableObject.CreateInstance<RunConfig>();
            Assert.DoesNotThrow(config.Validate);
            Assert.AreEqual(720f, config.TimeLimitSeconds);
            Assert.AreEqual(1500, config.TargetValue);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ItemDefinition_RejectsNonSnakeCaseId()
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            SerializedObject serialized = new(item);
            serialized.FindProperty("itemId").stringValue = "Not Valid";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<System.InvalidOperationException>(item.Validate);
            Object.DestroyImmediate(item);
        }

        [Test]
        public void ItemDefinition_DefaultDefinitionIsValid()
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            Assert.DoesNotThrow(item.Validate);
            Object.DestroyImmediate(item);
        }

        [Test]
        public void RunController_UnlocksExitAtConfiguredSoldValue()
        {
            RunConfig config = ScriptableObject.CreateInstance<RunConfig>();
            GameObject root = new("RunControllerTest");
            root.SetActive(false);
            LocalRunController run = root.AddComponent<LocalRunController>();
            SerializedObject serialized = new(run);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);

            run.Sell(1499);
            Assert.IsFalse(run.ExitUnlocked);
            run.Sell(1);
            Assert.IsTrue(run.ExitUnlocked);
            Assert.AreEqual(RunState.ObjectiveComplete, run.State);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(config);
        }
    }
}
