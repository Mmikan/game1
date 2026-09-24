using UnityEngine;

namespace Game1.Enemies
{
    public static class EnemyPerception
    {
        public static bool InCone(Vector3 origin, Vector3 forward, Vector3 target, float range, float angle)
        {
            Vector3 offset = target - origin;
            return offset.sqrMagnitude <= range * range &&
                (offset.sqrMagnitude < 0.0001f || Vector3.Dot(forward.normalized, offset.normalized) >= Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad));
        }

        public static bool Hears(Vector3 listener, GameplayNoiseEvent noise, float maximum, bool hearsCurses) =>
            (hearsCurses || noise.Kind != NoiseKind.Curse) &&
            Vector3.Distance(listener, noise.Position) <= Mathf.Min(maximum, noise.Radius);

        public static bool ClearLine(Vector3 from, Vector3 to, Transform observer, Transform target)
        {
            Vector3 delta = to - from;
            foreach (RaycastHit hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(observer) && (target == null || !hit.transform.IsChildOf(target))) return false;
            return true;
        }
    }
}
