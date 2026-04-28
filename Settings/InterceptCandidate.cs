using RimWorld;
using Verse;

namespace AntiAirWeapon.Settings
{
    public enum InterceptCandidateKind
    {
        Projectile,
        OverheadProjectile,
        Skyfaller,
        DropPodTransporter,
        Unknown
    }

    public class InterceptCandidate
    {
        public string DefName;
        public string Label;
        public string Description;
        public InterceptCandidateKind Kind;
        public string SourceModName;
        public string SourcePackageId;
        public ThingDef ThingDef;
        public string SearchText;
        public bool IsObservedRecently;
        public bool IsMissing;

        public string KindLabel
        {
            get
            {
                switch (this.Kind)
                {
                    case InterceptCandidateKind.OverheadProjectile:
                        return "Overhead Projectile";
                    case InterceptCandidateKind.Projectile:
                        return "Projectile";
                    case InterceptCandidateKind.Skyfaller:
                        return "Skyfaller";
                    case InterceptCandidateKind.DropPodTransporter:
                        return "DropPod/Transporter";
                    default:
                        return "Unknown";
                }
            }
        }

        public static InterceptCandidate FromThingDef(ThingDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.defName))
            {
                return null;
            }

            string label = def.defName;
            try
            {
                label = !def.LabelCap.NullOrEmpty() ? def.LabelCap.ToString() : def.defName;
            }
            catch
            {
                label = def.label.NullOrEmpty() ? def.defName : def.label;
            }

            string description = string.Empty;
            try
            {
                description = def.description ?? string.Empty;
            }
            catch
            {
                description = string.Empty;
            }

            string sourceName = "Unknown";
            string sourcePackageId = string.Empty;
            try
            {
                if (def.modContentPack != null)
                {
                    sourceName = def.modContentPack.Name ?? "Unknown";
                    sourcePackageId = def.modContentPack.PackageIdPlayerFacing ?? def.modContentPack.PackageId ?? string.Empty;
                }
            }
            catch
            {
                sourceName = "Unknown";
                sourcePackageId = string.Empty;
            }

            InterceptCandidate candidate = new InterceptCandidate
            {
                DefName = def.defName,
                Label = label,
                Description = description,
                Kind = Classify(def),
                SourceModName = sourceName,
                SourcePackageId = sourcePackageId,
                ThingDef = def,
                IsMissing = false
            };
            candidate.RebuildSearchText();
            return candidate;
        }

        public static InterceptCandidate Missing(string defName, bool observedRecently, string observedKind)
        {
            InterceptCandidate candidate = new InterceptCandidate
            {
                DefName = defName,
                Label = defName,
                Description = "This defName is saved in settings but was not found in the currently loaded defs.",
                Kind = ParseKind(observedKind),
                SourceModName = "Missing",
                SourcePackageId = string.Empty,
                ThingDef = null,
                IsObservedRecently = observedRecently,
                IsMissing = true
            };
            candidate.RebuildSearchText();
            return candidate;
        }

        public bool MatchesTokens(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string[] tokens = searchText.ToLowerInvariant().Split(new[] { ' ', '\t', ',', ';', '，', '；' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                if (this.SearchText == null || !this.SearchText.Contains(tokens[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public void RebuildSearchText()
        {
            this.SearchText = string.Join(" ", new[]
            {
                this.DefName ?? string.Empty,
                this.Label ?? string.Empty,
                this.Description ?? string.Empty,
                this.SourceModName ?? string.Empty,
                this.SourcePackageId ?? string.Empty,
                this.KindLabel ?? string.Empty
            }).ToLowerInvariant();
        }

        public static InterceptCandidateKind Classify(ThingDef def)
        {
            if (def == null)
            {
                return InterceptCandidateKind.Unknown;
            }

            try
            {
                if (def.projectile != null && def.projectile.flyOverhead)
                {
                    return InterceptCandidateKind.OverheadProjectile;
                }
            }
            catch
            {
            }

            try
            {
                if (def.thingClass != null && typeof(IActiveTransporter).IsAssignableFrom(def.thingClass))
                {
                    return InterceptCandidateKind.DropPodTransporter;
                }
            }
            catch
            {
            }

            try
            {
                if (def.skyfaller != null || (def.thingClass != null && typeof(Skyfaller).IsAssignableFrom(def.thingClass)))
                {
                    string defName = def.defName ?? string.Empty;
                    if (defName.IndexOf("drop", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("pod", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        defName.IndexOf("transport", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return InterceptCandidateKind.DropPodTransporter;
                    }
                    return InterceptCandidateKind.Skyfaller;
                }
            }
            catch
            {
            }

            try
            {
                if (def.projectile != null ||
                    def.category == ThingCategory.Projectile ||
                    (def.thingClass != null && typeof(Projectile).IsAssignableFrom(def.thingClass)))
                {
                    return InterceptCandidateKind.Projectile;
                }
            }
            catch
            {
            }

            return InterceptCandidateKind.Unknown;
        }

        public static InterceptCandidateKind ParseKind(string rawKind)
        {
            InterceptCandidateKind kind;
            if (!string.IsNullOrEmpty(rawKind) && System.Enum.TryParse(rawKind, out kind))
            {
                return kind;
            }
            return InterceptCandidateKind.Unknown;
        }
    }
}
