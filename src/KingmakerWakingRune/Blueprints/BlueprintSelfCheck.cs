using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;

namespace KingmakerWakingRune.Blueprints
{
    /// <summary>
    /// A save refers to blueprints by id: a brain's actions, a unit's facts, an item, a buff. A blueprint this mod builds and
    /// links from another, but never registers, makes every save that comes to refer to it fail to load ("Failed to load
    /// blueprint by guid"). After registration, every mod blueprint and its components are walked for references to such
    /// blueprints; each is logged, so a missing registration is found at start-up, not by a broken save.
    /// </summary>
    internal static class BlueprintSelfCheck
    {
        public static List<string> Run(Dictionary<string, BlueprintScriptableObject> library, IEnumerable<BlueprintScriptableObject> own)
        {
            var problems = new List<string>();
            var seen = new HashSet<object>();
            foreach (BlueprintScriptableObject bp in own.Where(b => b != null))
            {
                Walk(bp, bp, library, problems, seen, 0);
            }
            return problems.Distinct().ToList();
        }

        private static void Walk(object value, BlueprintScriptableObject owner, Dictionary<string, BlueprintScriptableObject> library, List<string> problems, HashSet<object> seen, int depth)
        {
            if (value == null || depth > 6 || !seen.Add(value))
            {
                return;
            }
            foreach (FieldInfo field in Fields(value.GetType()))
            {
                object v;
                try
                {
                    v = field.GetValue(value);
                }
                catch (Exception)
                {
                    continue;
                }
                foreach (object item in Items(v))
                {
                    if (item is BlueprintScriptableObject referenced)
                    {
                        if (!string.IsNullOrEmpty(referenced.AssetGuid) && !library.ContainsKey(referenced.AssetGuid))
                        {
                            problems.Add($"{owner.name}.{field.Name} -> {referenced.name} ({referenced.AssetGuid}) is not registered");
                        }
                    }
                    else if (item is BlueprintComponent component)
                    {
                        Walk(component, owner, library, problems, seen, depth + 1);
                    }
                    else if (item != null && !(item is UnityEngine.Object) && !item.GetType().IsPrimitive && !(item is string) && item.GetType().Namespace != null && item.GetType().Namespace.StartsWith("Kingmaker", StringComparison.Ordinal))
                    {
                        Walk(item, owner, library, problems, seen, depth + 1);
                    }
                }
            }
        }

        private static IEnumerable<object> Items(object v)
        {
            if (v == null || v is string)
            {
                yield break;
            }
            if (v is IEnumerable list && !(v is UnityEngine.Object))
            {
                foreach (object item in list)
                {
                    yield return item;
                }
                yield break;
            }
            yield return v;
        }

        private static IEnumerable<FieldInfo> Fields(Type type)
        {
            for (Type t = type; t != null && t != typeof(object) && t != typeof(UnityEngine.Object); t = t.BaseType)
            {
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    yield return f;
                }
            }
        }
    }
}
