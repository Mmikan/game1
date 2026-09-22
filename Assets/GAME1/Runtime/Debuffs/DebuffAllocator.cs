using System;
using System.Collections.Generic;

namespace Game1.Debuffs
{
    public static class DebuffAllocator
    {
        public static bool TryAssign(IReadOnlyList<DebuffDefinition> pool, IReadOnlyList<DebuffKind> assigned, Random random, out DebuffDefinition result)
        {
            var candidates = new List<DebuffDefinition>();
            foreach (DebuffDefinition definition in pool)
            {
                if (definition == null || definition.Kind == DebuffKind.None) continue;
                bool compatible = true;
                foreach (DebuffKind current in assigned)
                    if (!definition.IsCompatibleWith(current)) { compatible = false; break; }
                if (compatible) candidates.Add(definition);
            }
            if (candidates.Count == 0) { result = null; return false; }
            result = candidates[random.Next(candidates.Count)];
            return true;
        }
    }
}
