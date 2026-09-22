using UnityEngine;
using UnityEngine.InputSystem;

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

        private void Awake() => player = GetComponent<LocalPlayerController>();

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.qKey.wasPressedThisFrame || Time.time < nextAllowedTime) return;
            nextAllowedTime = Time.time + cooldown;
            Ray ray = new(player.ViewCamera.transform.position, player.ViewCamera.transform.forward);
            Vector3 position = Physics.Raycast(ray, out RaycastHit hit, range) ? hit.point : ray.GetPoint(range);
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
