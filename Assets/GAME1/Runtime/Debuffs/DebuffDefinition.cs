using System;
using UnityEngine;

namespace Game1.Debuffs
{
    public enum DebuffKind { None, Tremor, TunnelVision, HeavyBreath, FragileGrip, HearingLoss, LostVoice, LowLight, BackPain, StaticFear, Balance }
    public enum SoloFallback { None, FloorStabilize, HandMap, TapeBox, CrouchRecovery }

    [Serializable]
    public struct DebuffTuning
    {
        public float primary;
        public float assisted;
        public float solo;
        public float range;
        public float assistedRange;
        public float duration;
        public float interval;

        public DebuffTuning(float primary, float assisted = 0f, float solo = 0f, float range = 0f, float assistedRange = 0f, float duration = 0f, float interval = 0f)
        {
            this.primary = primary;
            this.assisted = assisted;
            this.solo = solo;
            this.range = range;
            this.assistedRange = assistedRange;
            this.duration = duration;
            this.interval = interval;
        }
    }

    [CreateAssetMenu(fileName = "DebuffDefinition", menuName = "GAME1/Debuff Definition")]
    public sealed class DebuffDefinition : ScriptableObject
    {
        [SerializeField] private DebuffKind kind;
        [SerializeField] private string localizationKey;
        [SerializeField] private DebuffKind[] incompatibleWith = Array.Empty<DebuffKind>();
        [SerializeField] private SoloFallback soloFallback;
        [SerializeField] private DebuffTuning tuning;
        public DebuffKind Kind => kind;
        public string LocalizationKey => localizationKey;
        public DebuffKind[] IncompatibleWith => incompatibleWith;
        public SoloFallback Fallback => soloFallback;
        public DebuffTuning Tuning => tuning;

        public void Configure(DebuffKind value, SoloFallback fallback, params DebuffKind[] incompatible)
        {
            kind = value;
            localizationKey = KeyFor(value);
            soloFallback = fallback;
            incompatibleWith = incompatible ?? Array.Empty<DebuffKind>();
            tuning = DefaultTuning(value);
        }

        public bool IsCompatibleWith(DebuffKind other) => Array.IndexOf(incompatibleWith, other) < 0;
        public static string KeyFor(DebuffKind value) => $"debuff.{ToSnakeCase(value.ToString())}.name";

        private static DebuffTuning DefaultTuning(DebuffKind value) => value switch
        {
            DebuffKind.Tremor => new DebuffTuning(0.18f, 0.04f, 0.8f, 1.8f),
            DebuffKind.TunnelVision => new DebuffTuning(0.2f, 0f, 3f, 12f, 9999f, 0.65f),
            DebuffKind.HeavyBreath => new DebuffTuning(35f, 4.6f, 50f, 8f, 3f, 2f, 8f),
            DebuffKind.FragileGrip => new DebuffTuning(0.25f, 0f, 2f, 0f, 0f, 45f, 0.75f),
            DebuffKind.HearingLoss => new DebuffTuning(-18f, 0f, 8f),
            DebuffKind.LostVoice => new DebuffTuning(10f, 0f, 20f, 4f, 0f, 60f),
            DebuffKind.LowLight => new DebuffTuning(1.5f, 3f),
            DebuffKind.BackPain => new DebuffTuning(2.5f, 3.4f, 1.8f, 8f, 3f, 0f, 8f),
            DebuffKind.StaticFear => new DebuffTuning(4f, 1f, 0f, 1.5f, 0f, 0f, 0.7f),
            DebuffKind.Balance => new DebuffTuning(0.75f, 1f, 0.5f, 0f, 0f, 2f),
            _ => default
        };

        private static string ToSnakeCase(string value)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (i > 0 && char.IsUpper(value[i])) result.Append('_');
                result.Append(char.ToLowerInvariant(value[i]));
            }
            return result.ToString();
        }
    }
}
