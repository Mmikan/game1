using System;
using Game1.Config;
using UnityEngine;

namespace Game1.Gameplay
{
    /// <summary>Owns the authoritative state for the Phase 1 local run.</summary>
    public sealed class LocalRunController : MonoBehaviour
    {
        [SerializeField] private RunConfig config;

        public event Action StateChanged;
        public RunState State { get; private set; } = RunState.Playing;
        public float RemainingSeconds { get; private set; }
        public int SoldValue { get; private set; }
        public int TargetValue => config != null ? config.TargetValue : 0;
        public bool ExitUnlocked => State is RunState.ObjectiveComplete or RunState.Succeeded;

        private void Awake()
        {
            if (config == null)
            {
                enabled = false;
                Debug.LogError("RunConfig is required.", this);
                return;
            }

            config = RuntimeDataLoader.LoadRunConfig(config);
            RemainingSeconds = config.TimeLimitSeconds;
        }

        private void Update()
        {
            if (State is RunState.Succeeded or RunState.Failed) return;
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            if (RemainingSeconds <= 0f) SetState(RunState.Failed);
        }

        /// <summary>Adds value only when an item enters the sold cart.</summary>
        public void Sell(int value)
        {
            if (State is RunState.Succeeded or RunState.Failed || value < 0) return;
            SoldValue += value;
            if (SoldValue >= config.TargetValue && State == RunState.Playing)
                SetState(RunState.ObjectiveComplete);
            else
                StateChanged?.Invoke();
        }

        public void CompleteEscape()
        {
            if (ExitUnlocked) SetState(RunState.Succeeded);
        }

        public void FailNoSurvivors()
        {
            if (State != RunState.Succeeded) SetState(RunState.Failed);
        }

        private void SetState(RunState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke();
        }
    }
}
