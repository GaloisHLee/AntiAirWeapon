using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AntiAirWeapon.Settings
{
    public static class InterceptTargetRegistry
    {
        public const int MaxDisplayedCandidates = 200;

        public static List<InterceptCandidate> BuildCandidates(IEnumerable<string> configuredDefNames, IEnumerable<ObservedAirTarget> observedTargets)
        {
            Dictionary<string, InterceptCandidate> byDefName = new Dictionary<string, InterceptCandidate>(StringComparer.OrdinalIgnoreCase);

            try
            {
                List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int i = 0; i < defs.Count; i++)
                {
                    ThingDef def = defs[i];
                    if (!ShouldInclude(def))
                    {
                        continue;
                    }

                    InterceptCandidate candidate = InterceptCandidate.FromThingDef(def);
                    if (candidate != null && !byDefName.ContainsKey(candidate.DefName))
                    {
                        byDefName.Add(candidate.DefName, candidate);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[AntiAirWeapon] Failed to build intercept target candidates: " + ex.Message);
            }

            HashSet<string> observedDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ObservedAirTarget> observedByDefName = new Dictionary<string, ObservedAirTarget>(StringComparer.OrdinalIgnoreCase);
            if (observedTargets != null)
            {
                foreach (ObservedAirTarget observed in observedTargets)
                {
                    if (observed == null || string.IsNullOrWhiteSpace(observed.DefName))
                    {
                        continue;
                    }

                    observedDefNames.Add(observed.DefName);
                    observedByDefName[observed.DefName] = observed;
                    InterceptCandidate candidate;
                    if (byDefName.TryGetValue(observed.DefName, out candidate))
                    {
                        candidate.IsObservedRecently = true;
                        candidate.RebuildSearchText();
                    }
                    else
                    {
                        byDefName[observed.DefName] = InterceptCandidate.Missing(observed.DefName, true, observed.Kind);
                    }
                }
            }

            if (configuredDefNames != null)
            {
                foreach (string defName in configuredDefNames)
                {
                    if (string.IsNullOrWhiteSpace(defName) || byDefName.ContainsKey(defName))
                    {
                        continue;
                    }

                    ObservedAirTarget observed;
                    observedByDefName.TryGetValue(defName, out observed);
                    byDefName[defName] = InterceptCandidate.Missing(
                        defName,
                        observedDefNames.Contains(defName),
                        observed != null ? observed.Kind : string.Empty);
                }
            }

            return byDefName.Values
                .OrderByDescending(candidate => candidate.IsObservedRecently)
                .ThenBy(candidate => candidate.IsMissing)
                .ThenBy(candidate => candidate.Kind)
                .ThenBy(candidate => candidate.Label)
                .ThenBy(candidate => candidate.DefName)
                .ToList();
        }

        public static bool DefExists(string defName)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return false;
            }

            try
            {
                return DefDatabase<ThingDef>.GetNamedSilentFail(defName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldInclude(ThingDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.defName))
            {
                return false;
            }

            try
            {
                if (def.projectile != null)
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                if (def.category == ThingCategory.Projectile)
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                if (def.thingClass != null &&
                    (typeof(Projectile).IsAssignableFrom(def.thingClass) ||
                     typeof(Skyfaller).IsAssignableFrom(def.thingClass) ||
                     typeof(IActiveTransporter).IsAssignableFrom(def.thingClass)))
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                return def.skyfaller != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
