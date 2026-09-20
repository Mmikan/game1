using System;
using UnityEngine;

namespace Game1.Config
{
    /// <summary>Data-driven values that define one local prototype run.</summary>
    [CreateAssetMenu(fileName = "RunConfig", menuName = "GAME1/Run Config")]
    public sealed class RunConfig : ScriptableObject
    {
        [SerializeField, Min(1f)] private float timeLimitSeconds = 720f;
        [SerializeField, Min(0)] private int targetValue = 1500;
        [SerializeField, Min(0)] private int normalItemCount = 16;
        [SerializeField, Min(0)] private int curseItemCount = 1;

        public float TimeLimitSeconds => timeLimitSeconds;
        public int TargetValue => targetValue;
        public int NormalItemCount => normalItemCount;
        public int CurseItemCount => curseItemCount;

        /// <summary>Throws when serialized values cannot produce a valid run.</summary>
        public void Validate()
        {
            if (timeLimitSeconds <= 0f) throw new InvalidOperationException("Run time must be positive.");
            if (targetValue < 0) throw new InvalidOperationException("Target value cannot be negative.");
            if (normalItemCount < 0 || curseItemCount < 0) throw new InvalidOperationException("Item counts cannot be negative.");
        }
    }
}
