using Game1.Items;
using Game1.Debuffs;
using Game1.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game1.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class LocalPlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform carryAnchor;
        [SerializeField] private Light flashlight;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float crouchSpeed = 2.4f;
        [SerializeField] private float jumpHeight = 1f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float staminaMax = 100f;
        [SerializeField] private float sprintCostPerSecond = 20f;
        [SerializeField] private float staminaRecoveryPerSecond = 16f;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;
        private float stamina;
        private bool crouched;
        private float throwCharge;
        private PlayerDebuffController debuff;
        private Vector2 movementInput;
        private bool turningThisFrame;
        private float downedUntil;
        private float nextFootstep;

        public void Down()
        {
            if (!GameplayNoise.HasAuthority || LifeState is PlayerLifeState.Dead or PlayerLifeState.Escaped) return;
            if (LifeState == PlayerLifeState.Downed)
            {
                LifeState = PlayerLifeState.Dead;
                FindFirstObjectByType<LocalRunController>()?.FailNoSurvivors();
                return;
            }
            DropHeldItem(false);
            LifeState = PlayerLifeState.Downed;
            downedUntil = Time.time + 60f;
        }

        public PlayerLifeState LifeState { get; private set; } = PlayerLifeState.Alive;
        public float DownedSecondsRemaining => LifeState == PlayerLifeState.Downed ? Mathf.Max(0f, downedUntil - Time.time) : 0f;
        public PickupItem HeldItem { get; private set; }
        public Camera ViewCamera => viewCamera;
        public float Stamina => stamina;
        public bool IsCrouched => crouched;
        public bool IsMoving => movementInput.sqrMagnitude > 0.01f;
        public Transform CarryAnchor => carryAnchor;
        public bool FlashlightOn => flashlight != null && flashlight.enabled;
        public void Configure(Camera cameraValue, Transform anchorValue, Light lightValue)
        {
            viewCamera = cameraValue;
            carryAnchor = anchorValue;
            flashlight = lightValue;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            debuff = GetComponent<PlayerDebuffController>();
            stamina = staminaMax;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (LifeState == PlayerLifeState.Downed && Time.time >= downedUntil)
            {
                LifeState = PlayerLifeState.Dead;
                FindFirstObjectByType<LocalRunController>()?.FailNoSurvivors();
            }
            if (LifeState != PlayerLifeState.Alive) return;
            UpdateLook();
            UpdateMovement();
            UpdateActions();
        }

        private void UpdateLook()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
            turningThisFrame = Mathf.Abs(delta.x) > 0.01f;
            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.Rotate(0f, delta.x, 0f);
        }

        private void UpdateMovement()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);
            if (debuff != null && debuff.MovementBlocked) input = Vector2.zero;
            movementInput = input;

            bool sprinting = !crouched && HeldItem == null && keyboard.leftShiftKey.isPressed && input.y > 0f && stamina > 0f;
            float speed = crouched ? crouchSpeed : sprinting ? sprintSpeed : HeldItem != null && HeldItem.Definition.TwoHanded ? 3f : walkSpeed;
            if (debuff != null) speed = debuff.ResolveMoveSpeed(speed, input, turningThisFrame);
            float sprintCost = debuff != null ? debuff.SprintCostPerSecond : sprintCostPerSecond;
            float recovery = debuff != null ? debuff.StaminaRecoveryPerSecond : staminaRecoveryPerSecond;
            if (sprinting) stamina = Mathf.Max(0f, stamina - sprintCost * Time.deltaTime);
            else stamina = Mathf.Min(staminaMax, stamina + recovery * Time.deltaTime);

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (!crouched && keyboard.spaceKey.wasPressedThisFrame)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
                    debuff?.NotifyImpactOrJump();
                }
            }
            else verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 planar = transform.right * input.x + transform.forward * input.y;
            controller.Move((planar * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
            if (planar.sqrMagnitude > 0.01f && Time.time >= nextFootstep)
            {
                nextFootstep = Time.time + 0.5f;
                GameplayNoise.Emit(transform.position, sprinting ? 4f : 1.5f, NoiseKind.Footstep);
                if (sprinting && debuff != null) GameplayNoise.Emit(transform.position, debuff.HeavyBreathNoiseRadius, NoiseKind.HeavyBreath);
            }
        }

        private void UpdateActions()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.cKey.wasPressedThisFrame) ToggleCrouch();
            if (keyboard.fKey.wasPressedThisFrame && flashlight != null) flashlight.enabled = !flashlight.enabled;
            if (HeldItem != null && Mouse.current != null)
            {
                if (Mouse.current.rightButton.isPressed)
                    throwCharge = Mathf.Min(0.7f, throwCharge + Time.deltaTime);
                if (Mouse.current.rightButton.wasReleasedThisFrame)
                    DropHeldItem(true);
            }
        }

        public bool TryHold(PickupItem item)
        {
            if (HeldItem != null || item == null || item.Definition.RequiredCarriers > 1) return false;
            HeldItem = item;
            item.BeginHold(carryAnchor, debuff != null ? debuff.HeldItemDurabilityMultiplier : 1f);
            debuff?.NotifyPickedUp();
            return true;
        }

        public void DropHeldItem(bool throwItem)
        {
            if (HeldItem == null) return;
            PickupItem item = HeldItem;
            HeldItem = null;
            float throwSpeed = Mathf.Lerp(5f, 10f, Mathf.Clamp01(throwCharge / 0.7f));
            bool canThrow = debuff == null || debuff.CanThrowHeldItem;
            Vector3 velocity = throwItem && canThrow && item.Definition.Throwable ? viewCamera.transform.forward * throwSpeed : Vector3.zero;
            throwCharge = 0f;
            item.EndHold(velocity);
            debuff?.NotifyDropped();
        }

        private void ToggleCrouch()
        {
            crouched = !crouched;
            controller.height = crouched ? 1.1f : 1.75f;
            controller.center = Vector3.up * controller.height * 0.5f;
        }

        public void RestoreStamina(float amount) => stamina = Mathf.Min(staminaMax, stamina + Mathf.Max(0f, amount));

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.moveDirection.y < -0.2f || hit.moveLength > 0.05f) debuff?.NotifyImpactOrJump();
        }
    }
}
