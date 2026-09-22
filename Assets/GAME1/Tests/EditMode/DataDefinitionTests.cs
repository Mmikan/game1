using Game1.Config;
using Game1.Items;
using Game1.Gameplay;
using Game1.Network;
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

        [Test]
        public void AuthorityRules_RejectDeadAndOutOfRangePlayers()
        {
            Assert.IsFalse(CoopAuthorityRules.CanInteract(PlayerLifeState.Dead, Vector3.zero, Vector3.zero, 3f));
            Assert.IsFalse(CoopAuthorityRules.CanInteract(PlayerLifeState.Alive, Vector3.zero, Vector3.forward * 3.1f, 3f));
            Assert.IsTrue(CoopAuthorityRules.CanInteract(PlayerLifeState.Alive, Vector3.zero, Vector3.forward * 3f, 3f));
        }

        [Test]
        public void AuthorityRules_SharedCarryRequiresBothPlayersWithinTwoMeters()
        {
            Assert.IsTrue(CoopAuthorityRules.CanStartSharedCarry(Vector3.left * 0.5f, Vector3.right * 0.5f, Vector3.zero));
            Assert.IsFalse(CoopAuthorityRules.CanStartSharedCarry(Vector3.left * 2f, Vector3.right * 2f, Vector3.zero));
        }
    }
}
