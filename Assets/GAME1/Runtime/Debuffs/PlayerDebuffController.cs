using System;
using Game1.Gameplay;
using Game1.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game1.Debuffs
{
    [RequireComponent(typeof(LocalPlayerController))]
    public sealed class PlayerDebuffController : MonoBehaviour
    {
        [SerializeField] private DebuffDefinition definition;
        [SerializeField] private DebuffDefinition[] availableDefinitions;
        private LocalPlayerController player;
        private Vector3 carryOrigin;
        private float floorStartedAt = -1f, stillCrouchTime, tapeProgress, tapeUntil, dropWarningUntil;
        private float mapOpenedAt = -1f, beaconUntil, balancePenaltyUntil, nextCurseNoise, impactCooldownUntil;
        private bool tapeUsed, floorStabilized, allyAssisting, sharedCarry, allyLight;

        public event Action<Vector3, float, string> NoiseEmitted;
        public DebuffKind Kind => definition != null ? definition.Kind : DebuffKind.None;
        public DebuffDefinition Definition => definition;
        public bool MovementBlocked => Kind == DebuffKind.TunnelVision && mapOpenedAt >= 0f;
        public bool MapOpen => MovementBlocked;
        public float TunnelVisionAlpha => Kind == DebuffKind.TunnelVision ? definition.Tuning.duration : 0f;
        public bool DropWarning => Time.time < dropWarningUntil;
        public bool AllyAssisting => allyAssisting;
        public bool DangerVibrationHudEnabled => Kind == DebuffKind.HearingLoss;
        public float DangerVibrationRange => Kind == DebuffKind.HearingLoss ? definition.Tuning.solo : 0f;
        public float AudibleVolumeMultiplier => Kind == DebuffKind.HearingLoss ? Mathf.Pow(10f, definition.Tuning.primary / 20f) : 1f;
        public bool StereoLocalizationEnabled => Kind != DebuffKind.HearingLoss;
        public float PingRange => Kind == DebuffKind.LostVoice ? definition.Tuning.primary :
            Kind == DebuffKind.TunnelVision ? (allyLight || allyAssisting ? definition.Tuning.assistedRange : definition.Tuning.range) : 30f;
        public float RescuePingCooldown => Kind == DebuffKind.LostVoice ? definition.Tuning.range : 2f;
        public float HeavyBreathNoiseRadius => Kind == DebuffKind.HeavyBreath ? (allyAssisting ? definition.Tuning.assistedRange : definition.Tuning.range) : 0f;
        public bool BeaconActive => Time.time < beaconUntil;
        public float TapeProgress01 => Kind == DebuffKind.FragileGrip ? Mathf.Clamp01(tapeProgress / definition.Tuning.solo) : 0f;
        public float SprintCostPerSecond => Kind == DebuffKind.HeavyBreath ? definition.Tuning.primary : 20f;
        public float StaminaRecoveryPerSecond => Kind == DebuffKind.HeavyBreath ? definition.Tuning.interval : 16f;
        public float AssistedMoveSpeed => Kind == DebuffKind.HeavyBreath && allyAssisting ? definition.Tuning.assisted : -1f;
        public float HeldItemDurabilityMultiplier => Kind == DebuffKind.Tremor ? definition.Tuning.range : 1f;
        public bool CanThrowHeldItem => Kind != DebuffKind.BackPain || player == null || player.HeldItem == null || player.HeldItem.Definition.WeightKg <= definition.Tuning.range;

        public void Configure(DebuffDefinition value) => definition = value;
        public void Configure(DebuffDefinition value, DebuffDefinition[] pool)
        {
            definition = value;
            availableDefinitions = pool;
        }
        public void SetAllyAssisting(bool value) => allyAssisting = value;
        public void SetSharedCarry(bool value) => sharedCarry = value;
        public void SetAllyLight(bool value) => allyLight = value;

        private void Awake()
        {
            player = GetComponent<LocalPlayerController>();
            carryOrigin = player.CarryAnchor != null ? player.CarryAnchor.localPosition : Vector3.zero;
        }

        private void Update()
        {
            UpdateFallbackInput();
            UpdateTremor();
            UpdateHeavyBreathFallback();
            UpdateCurseNoise();
        }

        private void UpdateFallbackInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (Debug.isDebugBuild && keyboard.f6Key.wasPressedThisFrame) SelectNextDebugDebuff();
            if (Kind == DebuffKind.TunnelVision)
            {
                if (keyboard.mKey.wasPressedThisFrame) mapOpenedAt = Time.time;
                if (mapOpenedAt >= 0f && Time.time - mapOpenedAt >= definition.Tuning.solo) mapOpenedAt = -1f;
            }
            if (Kind == DebuffKind.FragileGrip && !tapeUsed)
            {
                tapeProgress = keyboard.tKey.isPressed ? tapeProgress + Time.deltaTime : 0f;
                if (tapeProgress >= definition.Tuning.solo) ActivateTape();
            }
            if (Kind == DebuffKind.LostVoice && !BeaconActive && keyboard.bKey.wasPressedThisFrame)
                beaconUntil = Time.time + definition.Tuning.duration;
        }

        private void SelectNextDebugDebuff()
        {
            if (availableDefinitions == null || availableDefinitions.Length == 0) return;
            int index = Array.IndexOf(availableDefinitions, definition);
            definition = availableDefinitions[(index + 1 + availableDefinitions.Length) % availableDefinitions.Length];
            mapOpenedAt = -1f;
            floorStabilized = tapeUsed = allyAssisting = allyLight = false;
            tapeProgress = tapeUntil = beaconUntil = balancePenaltyUntil = 0f;
            Debug.Log($"GAME1_DEBUG_DEBUFF={definition.Kind}");
        }

        private void UpdateTremor()
        {
            if (player.CarryAnchor == null) return;
            if (Kind != DebuffKind.Tremor || player.HeldItem == null) { player.CarryAnchor.localPosition = carryOrigin; return; }
            float magnitude = allyAssisting || floorStabilized ? definition.Tuning.assisted : definition.Tuning.primary;
            player.CarryAnchor.localPosition = carryOrigin + new Vector3(Mathf.PerlinNoise(Time.time * 17f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.time * 19f) - 0.5f, 0f) * (magnitude * 2f);
        }

        private void UpdateHeavyBreathFallback()
        {
            if (Kind != DebuffKind.HeavyBreath || allyAssisting) { stillCrouchTime = 0f; return; }
            if (player.IsCrouched && !player.IsMoving)
            {
                stillCrouchTime += Time.deltaTime;
                if (stillCrouchTime >= definition.Tuning.duration) { player.RestoreStamina(definition.Tuning.solo); stillCrouchTime = 0f; }
            }
            else stillCrouchTime = 0f;
        }

        private void UpdateCurseNoise()
        {
            if (Kind != DebuffKind.StaticFear || player.HeldItem == null || player.HeldItem.Definition.Rarity != ItemRarity.Curse || Time.time < nextCurseNoise) return;
            nextCurseNoise = Time.time + definition.Tuning.interval;
            NoiseEmitted?.Invoke(transform.position, allyAssisting ? definition.Tuning.assisted : definition.Tuning.primary, "Curse");
            Game1.Enemies.GameplayNoise.Emit(transform.position, allyAssisting ? definition.Tuning.assisted : definition.Tuning.primary, Game1.Enemies.NoiseKind.Curse);
        }

        public float ResolveMoveSpeed(float baseSpeed, Vector2 input, bool turning)
        {
            if (Kind == DebuffKind.BackPain && player.HeldItem != null && player.HeldItem.Definition.WeightKg > definition.Tuning.range)
                return sharedCarry ? definition.Tuning.assisted : allyAssisting ? definition.Tuning.assistedRange :
                    (Keyboard.current != null && Keyboard.current.gKey.isPressed ? definition.Tuning.solo : definition.Tuning.primary);
            if (Kind == DebuffKind.Balance && !allyAssisting)
            {
                if (Mathf.Abs(input.x) > 0.05f || turning) balancePenaltyUntil = Time.time + definition.Tuning.duration;
                bool nearWall = Physics.Raycast(transform.position + Vector3.up, transform.right, definition.Tuning.solo) || Physics.Raycast(transform.position + Vector3.up, -transform.right, definition.Tuning.solo);
                if (!nearWall && Time.time < balancePenaltyUntil) return baseSpeed * definition.Tuning.primary;
            }
            return AssistedMoveSpeed > 0f ? AssistedMoveSpeed : baseSpeed;
        }

        public float ResolveInteractionRange(float normalRange)
        {
            if (Kind != DebuffKind.LowLight || allyLight || (player != null && player.FlashlightOn)) return normalRange;
            return definition.Tuning.primary;
        }

        public void NotifyPickedUp() => floorStartedAt = -1f;

        public void NotifyDropped()
        {
            if (Kind != DebuffKind.Tremor) return;
            floorStabilized = false;
            floorStartedAt = Time.time;
            Invoke(nameof(CompleteFloorStabilize), definition.Tuning.solo);
        }

        private void CompleteFloorStabilize()
        {
            if (player.HeldItem == null && floorStartedAt >= 0f && Time.time - floorStartedAt >= definition.Tuning.solo) floorStabilized = true;
        }

        public void NotifyImpactOrJump()
        {
            if (Kind != DebuffKind.FragileGrip || player.HeldItem == null || allyAssisting || Time.time < tapeUntil || Time.time < impactCooldownUntil) return;
            impactCooldownUntil = Time.time + 0.5f;
            if (UnityEngine.Random.value <= definition.Tuning.primary)
            {
                dropWarningUntil = Time.time + definition.Tuning.interval;
                Invoke(nameof(DropAfterWarning), definition.Tuning.interval);
            }
        }

        private void ActivateTape()
        {
            tapeUsed = true;
            tapeProgress = 0f;
            tapeUntil = Time.time + definition.Tuning.duration;
        }

        private void DropAfterWarning()
        {
            if (player.HeldItem != null && !allyAssisting && Time.time >= tapeUntil) player.DropHeldItem(false);
        }
    }
}
