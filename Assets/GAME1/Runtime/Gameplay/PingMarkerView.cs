using System.Collections.Generic;
using UnityEngine;

namespace Game1.Gameplay
{
    // One visible Ping on this peer. Network IDs are assigned by the Host, so late RPCs
    // cannot replace one of the three newer Pings already on screen.
    public sealed class PingMarkerView : MonoBehaviour
    {
        private static readonly List<PingMarkerView> active = new();
        private static ulong nextSoloId;
        private float expiresAtLocal;

        public static IReadOnlyList<PingMarkerView> Active => active;
        public ulong Id { get; private set; }
        public Vector3 WorldPoint { get; private set; }
        public double ServerExpiresAt { get; private set; }
        public bool IsNetworkPing { get; private set; }

        public static void ShowSolo(GameObject marker, Vector3 point, float duration)
        {
            Register(marker, ++nextSoloId, point, Mathf.Max(0f, duration), 0d, false);
        }

        public static void ShowNetwork(GameObject marker, ulong id, Vector3 point, double serverExpiresAt, float remaining)
        {
            Register(marker, id, point, Mathf.Clamp(remaining, 0f, 6f), serverExpiresAt, true);
        }

        private static void Register(GameObject marker, ulong id, Vector3 point, float remaining, double serverExpiresAt, bool network)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                PingMarkerView old = active[i];
                if (old == null) { active.RemoveAt(i); continue; }
                if (old.IsNetworkPing == network && Time.time < old.expiresAtLocal) continue;
                active.RemoveAt(i);
                Destroy(old.gameObject);
            }
            if (remaining <= 0f || active.Exists(old => old.Id == id)) { Destroy(marker); return; }

            PingMarkerView view = marker.AddComponent<PingMarkerView>();
            view.Id = id;
            view.WorldPoint = point;
            view.ServerExpiresAt = serverExpiresAt;
            view.IsNetworkPing = network;
            view.expiresAtLocal = Time.time + remaining;
            active.Add(view);
            active.Sort((a, b) => a.Id.CompareTo(b.Id));
            while (active.Count > 3)
            {
                PingMarkerView oldest = active[0];
                active.RemoveAt(0);
                Destroy(oldest.gameObject);
            }
        }

        private void Update()
        {
            if (Time.time >= expiresAtLocal) Destroy(gameObject);
        }

        private void OnDestroy() => active.Remove(this);
    }
}
