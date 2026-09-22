using Game1.Gameplay;
using UnityEngine;

namespace Game1.Network
{
    public static class CoopAuthorityRules
    {
        public const ulong NoClient = ulong.MaxValue;

        public static bool CanInteract(PlayerLifeState state, Vector3 playerPosition, Vector3 targetPosition, float range)
        {
            return state == PlayerLifeState.Alive && Vector3.SqrMagnitude(playerPosition - targetPosition) <= range * range;
        }

        public static bool CanStartSharedCarry(Vector3 firstPlayer, Vector3 secondPlayer, Vector3 itemPosition)
        {
            return Vector3.Distance(firstPlayer, secondPlayer) <= 2f &&
                   Vector3.Distance(firstPlayer, itemPosition) <= 2f &&
                   Vector3.Distance(secondPlayer, itemPosition) <= 2f;
        }
    }
}
