using Game1.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Game1.Tests
{
    public sealed class EnemyPerceptionTests
    {
        [Test]
        public void SightRejectsBehindAndOutOfRange()
        {
            Assert.IsTrue(EnemyPerception.InCone(Vector3.zero, Vector3.forward, Vector3.forward * 7f, 7f, 90f));
            Assert.IsFalse(EnemyPerception.InCone(Vector3.zero, Vector3.forward, Vector3.forward * 7.01f, 7f, 90f));
            Assert.IsFalse(EnemyPerception.InCone(Vector3.zero, Vector3.forward, Vector3.back, 12f, 90f));
        }

        [Test]
        public void NoiseRangeUsesBothEmissionAndListenerCaps()
        {
            var loud = new GameplayNoiseEvent(Vector3.forward * 11f, 20f, NoiseKind.Drop);
            Assert.IsFalse(EnemyPerception.Hears(Vector3.zero, loud, 10f, false));
            Assert.IsTrue(EnemyPerception.Hears(Vector3.zero, loud, 14f, true));
            Assert.IsFalse(EnemyPerception.Hears(Vector3.zero, new GameplayNoiseEvent(Vector3.forward * 4f, 3f, NoiseKind.HeavyBreath), 14f, true));
        }

        [Test]
        public void MannequinIgnoresCurseCollectorHearsIt()
        {
            var curse = new GameplayNoiseEvent(Vector3.right, 4f, NoiseKind.Curse);
            Assert.IsFalse(EnemyPerception.Hears(Vector3.zero, curse, 10f, false));
            Assert.IsTrue(EnemyPerception.Hears(Vector3.zero, curse, 14f, true));
        }

        [Test]
        public void MutingAudioDoesNotMuteGameplayNoise()
        {
            float previous = AudioListener.volume;
            int count = 0;
            System.Action<GameplayNoiseEvent> listener = _ => count++;
            GameplayNoise.Emitted += listener;
            try { AudioListener.volume = 0f; GameplayNoise.Emit(Vector3.zero, 4f, NoiseKind.Drop); Assert.AreEqual(1, count); }
            finally { AudioListener.volume = previous; GameplayNoise.Emitted -= listener; }
        }

        [Test]
        public void WallOccludesSightAndLight()
        {
            GameObject observer = new("observer"), target = new("target");
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                target.transform.position = Vector3.forward * 4f;
                wall.transform.position = Vector3.forward * 2f;
                Physics.SyncTransforms();
                Assert.IsFalse(EnemyPerception.ClearLine(Vector3.zero, target.transform.position, observer.transform, target.transform));
                wall.transform.position = Vector3.right * 5f;
                Physics.SyncTransforms();
                Assert.IsTrue(EnemyPerception.ClearLine(Vector3.zero, target.transform.position, observer.transform, target.transform));
            }
            finally { Object.DestroyImmediate(observer); Object.DestroyImmediate(target); Object.DestroyImmediate(wall); }
        }
    }
}
