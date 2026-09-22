using Game1.Items;
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

        public PlayerLifeState LifeState { get; private set; } = PlayerLifeState.Alive;
        public PickupItem HeldItem { get; private set; }
        public Camera ViewCamera => viewCamera;
        public float Stamina => stamina;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            stamina = staminaMax;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
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

            bool sprinting = !crouched && HeldItem == null && keyboard.leftShiftKey.isPressed && input.y > 0f && stamina > 0f;
            float speed = crouched ? crouchSpeed : sprinting ? sprintSpeed : HeldItem != null && HeldItem.Definition.TwoHanded ? 3f : walkSpeed;
            if (sprinting) stamina = Mathf.Max(0f, stamina - sprintCostPerSecond * Time.deltaTime);
            else stamina = Mathf.Min(staminaMax, stamina + staminaRecoveryPerSecond * Time.deltaTime);

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (!crouched && keyboard.spaceKey.wasPressedThisFrame)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            }
            else verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 planar = transform.right * input.x + transform.forward * input.y;
            controller.Move((planar * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
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
            item.BeginHold(carryAnchor);
            return true;
        }

        public void DropHeldItem(bool throwItem)
        {
            if (HeldItem == null) return;
            PickupItem item = HeldItem;
            HeldItem = null;
            float throwSpeed = Mathf.Lerp(5f, 10f, Mathf.Clamp01(throwCharge / 0.7f));
            Vector3 velocity = throwItem && item.Definition.Throwable ? viewCamera.transform.forward * throwSpeed : Vector3.zero;
            throwCharge = 0f;
            item.EndHold(velocity);
        }

        private void ToggleCrouch()
        {
            crouched = !crouched;
            controller.height = crouched ? 1.1f : 1.75f;
            controller.center = Vector3.up * controller.height * 0.5f;
        }
    }
}
