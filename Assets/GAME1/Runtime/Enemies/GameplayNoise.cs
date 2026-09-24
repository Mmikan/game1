using System;
using Unity.Netcode;
using UnityEngine;

namespace Game1.Enemies
{
    public enum NoiseKind { Footstep, Drop, Throw, HeavyBreath, Curse, Door, Enemy, Ping }

    public readonly struct GameplayNoiseEvent
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly NoiseKind Kind;
        public GameplayNoiseEvent(Vector3 position, float radius, NoiseKind kind)
        { Position = position; Radius = radius; Kind = kind; }
    }

    // AudioSource volume is deliberately absent: muting audio cannot change AI perception.
    public static class GameplayNoise
    {
        public static event Action<GameplayNoiseEvent> Emitted;
        public static bool HasAuthority => NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Emitted = null;

        public static void Emit(Vector3 position, float radius, NoiseKind kind)
        {
            if (!HasAuthority || float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f ||
                !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return;
            Emitted?.Invoke(new GameplayNoiseEvent(position, Mathf.Min(radius, 30f), kind));
        }
    }
}
