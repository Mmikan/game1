using UnityEngine;
using UnityEngine.InputSystem;
using Game1.Debuffs;

namespace Game1.Gameplay
{
    [RequireComponent(typeof(LocalPlayerController))]
    public sealed class PingController : MonoBehaviour
    {
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private float range = 30f;
        [SerializeField] private float duration = 6f;
        [SerializeField] private float cooldown = 2f;
        private LocalPlayerController player;
        private float nextAllowedTime;
        private PlayerDebuffController debuff;

        private void Awake()
        {
            player = GetComponent<LocalPlayerController>();
            debuff = GetComponent<PlayerDebuffController>();
        }

        private void Update()
        {
            if (Game1.Network.NetworkSessionMenu.MenuOpen || Keyboard.current == null || !Keyboard.current.qKey.wasPressedThisFrame || Time.time < nextAllowedTime) return;
            nextAllowedTime = Time.time + cooldown;
            float effectiveRange = debuff != null ? debuff.PingRange : range;
            Ray ray = new(player.ViewCamera.transform.position, player.ViewCamera.transform.forward);
            Vector3 position = Physics.Raycast(ray, out RaycastHit hit, effectiveRange) ? hit.point : ray.GetPoint(effectiveRange);
            Game1.Enemies.GameplayNoise.Emit(position, 4f, Game1.Enemies.NoiseKind.Ping);
            GameObject marker = markerPrefab != null
                ? Instantiate(markerPrefab, position + Vector3.up * 0.05f, Quaternion.identity)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (markerPrefab == null)
            {
                marker.name = "PingMarker";
                marker.transform.SetPositionAndRotation(position + Vector3.up * 0.25f, Quaternion.identity);
                marker.transform.localScale = Vector3.one * 0.22f;
                Destroy(marker.GetComponent<Collider>());
                marker.GetComponent<Renderer>().material.color = Color.cyan;
            }
            Destroy(marker, duration);
        }
    }
}
