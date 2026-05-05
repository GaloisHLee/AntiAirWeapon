using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using AntiAirWeapon.Settings;
using UnityEngine;
using Verse;

namespace AntiAirWeapon
{
    public class AntiAirWeaponSettings : ModSettings
    {
        public bool interceptHostilePods = true;
        public bool interceptHostileProjectiles = true;
        public bool interceptUnknownSkyfallers = false;
        public bool showAdvancedDefNameEditor = false;
        public string neverInterceptDefs = string.Empty;
        public string alwaysInterceptDefs = string.Empty;
        public string interceptHostileDefs = string.Empty;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.interceptHostilePods, "interceptHostilePods", true);
            Scribe_Values.Look(ref this.interceptHostileProjectiles, "interceptHostileProjectiles", true);
            Scribe_Values.Look(ref this.interceptUnknownSkyfallers, "interceptUnknownSkyfallers", false);
            Scribe_Values.Look(ref this.showAdvancedDefNameEditor, "showAdvancedDefNameEditor", false);
            Scribe_Values.Look(ref this.neverInterceptDefs, "neverInterceptDefs", string.Empty);
            Scribe_Values.Look(ref this.alwaysInterceptDefs, "alwaysInterceptDefs", string.Empty);
            Scribe_Values.Look(ref this.interceptHostileDefs, "interceptHostileDefs", string.Empty);
            base.ExposeData();
        }
    }

    public class AntiAirWeaponMod : Mod
    {
        private static readonly char[] DefNameSeparators = new[] { ',', ';', '\n', '\r', '\t', '，', '；' };
        private const float RuleRowHeightCollapsed = 66f;
        private const float RuleRowHeightExpanded = 96f;

        private static AntiAirWeaponMod instance;
        private AntiAirWeaponSettings settings;
        private Vector2 settingsScrollPosition;

        public static AntiAirWeaponMod Instance
        {
            get
            {
                if (instance == null)
                {
                    try
                    {
                        instance = LoadedModManager.GetMod<AntiAirWeaponMod>();
                    }
                    catch
                    {
                        instance = null;
                    }
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        public AllMapProjectileStorage _AllMapProjectileStorage
        {
            get { return Find.World != null ? Find.World.GetComponent<AllMapProjectileStorage>() : null; }
        }

        public bool InterceptHostilePods
        {
            get { return this.SettingsData == null || this.SettingsData.interceptHostilePods; }
        }

        public bool InterceptHostileProjectiles
        {
            get { return this.SettingsData == null || this.SettingsData.interceptHostileProjectiles; }
        }

        public bool InterceptUnknownSkyfallers
        {
            get { return this.SettingsData != null && this.SettingsData.interceptUnknownSkyfallers; }
        }

        public AntiAirWeaponMod(ModContentPack content) : base(content)
        {
            Instance = this;
            this.settings = this.GetSettings<AntiAirWeaponSettings>();
            this.RefreshCachedSettings();
        }

        public string ModIdentifier
        {
            get { return "AntiAirWeaponForked"; }
        }

        private AntiAirWeaponSettings SettingsData
        {
            get
            {
                if (this.settings == null)
                {
                    this.settings = this.GetSettings<AntiAirWeaponSettings>();
                }
                return this.settings;
            }
        }

        private float RuleRowHeight
        {
            get
            {
                return this.SettingsData != null && this.SettingsData.showAdvancedDefNameEditor
                    ? RuleRowHeightExpanded
                    : RuleRowHeightCollapsed;
            }
        }

        public override string SettingsCategory()
        {
            return "AntiAirWeapon[forked]";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            AntiAirWeaponSettings data = this.SettingsData;
            if (data == null)
            {
                Widgets.Label(inRect, "Anti-Air Weapon settings are not initialized.");
                return;
            }

            bool changed = false;
            float viewWidth = Mathf.Max(100f, inRect.width - 16f);
            Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(inRect.height, this.GetSettingsViewHeight()));
            Widgets.BeginScrollView(inRect, ref this.settingsScrollPosition, viewRect, true);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);
            this.DrawCheckbox(
                listing,
                "拦截敌对飞行舱",
                "启用后，防空炮会拦截识别为敌对派系的飞行舱或空投舱。",
                ref data.interceptHostilePods,
                ref changed);
            this.DrawCheckbox(
                listing,
                "拦截敌对炮弹",
                "启用后，防空炮会拦截能够识别为敌对派系的高空炮弹。",
                ref data.interceptHostileProjectiles,
                ref changed);
            this.DrawCheckbox(
                listing,
                "拦截未知 Skyfaller",
                "启用后，未实现飞行舱接口、也无法从内容物识别派系的 Skyfaller 会按默认可拦截目标处理。",
                ref data.interceptUnknownSkyfallers,
                ref changed);
            this.DrawCheckbox(
                listing,
                "高级：显示原始 defName",
                "显示底层 defName 文本编辑框。普通配置建议使用管理窗口选择目标。",
                ref data.showAdvancedDefNameEditor,
                ref changed);

            listing.Gap(8f);
            this.DrawRuleHandle(listing.GetRect(this.RuleRowHeight), InterceptRuleGroup.NeverIntercept, ref changed);
            listing.Gap(6f);
            this.DrawRuleHandle(listing.GetRect(this.RuleRowHeight), InterceptRuleGroup.AlwaysIntercept, ref changed);
            listing.Gap(6f);
            this.DrawRuleHandle(listing.GetRect(this.RuleRowHeight), InterceptRuleGroup.HostileIntercept, ref changed);
            listing.End();

            Widgets.EndScrollView();
            if (changed)
            {
                this.ApplySettingsChanged(true);
            }

            base.DoSettingsWindowContents(inRect);
        }

        public bool ShouldNeverIntercept(Thing thing)
        {
            return thing != null && thing.def != null && this.neverInterceptDefNames.Contains(thing.def.defName);
        }

        public bool ShouldAlwaysIntercept(Thing thing)
        {
            return thing != null && thing.def != null && this.alwaysInterceptDefNames.Contains(thing.def.defName);
        }

        public bool ShouldInterceptIfHostile(Thing thing)
        {
            return thing != null && thing.def != null && this.interceptHostileDefNames.Contains(thing.def.defName);
        }

        public List<string> GetRuleDefNames(InterceptRuleGroup group)
        {
            return ParseDefNameListOrdered(this.GetRuleDefNamesRaw(group));
        }

        public HashSet<string> GetRuleDefNameSet(InterceptRuleGroup group)
        {
            return new HashSet<string>(this.GetRuleDefNames(group), StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> GetAllRuleDefNames()
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string defName in this.GetRuleDefNames(InterceptRuleGroup.NeverIntercept))
            {
                result.Add(defName);
            }
            foreach (string defName in this.GetRuleDefNames(InterceptRuleGroup.AlwaysIntercept))
            {
                result.Add(defName);
            }
            foreach (string defName in this.GetRuleDefNames(InterceptRuleGroup.HostileIntercept))
            {
                result.Add(defName);
            }
            return result;
        }

        public InterceptRuleGroup? GetRuleForDefName(string defName)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return null;
            }

            if (this.GetRuleDefNameSet(InterceptRuleGroup.NeverIntercept).Contains(defName))
            {
                return InterceptRuleGroup.NeverIntercept;
            }
            if (this.GetRuleDefNameSet(InterceptRuleGroup.AlwaysIntercept).Contains(defName))
            {
                return InterceptRuleGroup.AlwaysIntercept;
            }
            if (this.GetRuleDefNameSet(InterceptRuleGroup.HostileIntercept).Contains(defName))
            {
                return InterceptRuleGroup.HostileIntercept;
            }
            return null;
        }

        public void AddOrMoveRuleDefName(InterceptRuleGroup group, string defName)
        {
            string normalizedDefName = NormalizeDefName(defName);
            if (string.IsNullOrEmpty(normalizedDefName))
            {
                return;
            }

            Dictionary<InterceptRuleGroup, List<string>> lists = new Dictionary<InterceptRuleGroup, List<string>>
            {
                { InterceptRuleGroup.NeverIntercept, this.GetRuleDefNames(InterceptRuleGroup.NeverIntercept) },
                { InterceptRuleGroup.AlwaysIntercept, this.GetRuleDefNames(InterceptRuleGroup.AlwaysIntercept) },
                { InterceptRuleGroup.HostileIntercept, this.GetRuleDefNames(InterceptRuleGroup.HostileIntercept) }
            };

            foreach (InterceptRuleGroup key in lists.Keys.ToList())
            {
                lists[key] = lists[key].Where(item => !string.Equals(item, normalizedDefName, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            lists[group].Add(normalizedDefName);

            this.SetRuleDefNames(InterceptRuleGroup.NeverIntercept, lists[InterceptRuleGroup.NeverIntercept]);
            this.SetRuleDefNames(InterceptRuleGroup.AlwaysIntercept, lists[InterceptRuleGroup.AlwaysIntercept]);
            this.SetRuleDefNames(InterceptRuleGroup.HostileIntercept, lists[InterceptRuleGroup.HostileIntercept]);
            this.ApplySettingsChanged(true);
        }

        public void RemoveRuleDefName(InterceptRuleGroup group, string defName)
        {
            string normalizedDefName = NormalizeDefName(defName);
            if (string.IsNullOrEmpty(normalizedDefName))
            {
                return;
            }

            List<string> names = this.GetRuleDefNames(group)
                .Where(item => !string.Equals(item, normalizedDefName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            this.SetRuleDefNames(group, names);
            this.ApplySettingsChanged(true);
        }

        public int CountMissingRuleDefNames(InterceptRuleGroup group)
        {
            int count = 0;
            List<string> names = this.GetRuleDefNames(group);
            for (int i = 0; i < names.Count; i++)
            {
                if (!InterceptTargetRegistry.DefExists(names[i]))
                {
                    count++;
                }
            }
            return count;
        }

        public string GetRuleTitle(InterceptRuleGroup group)
        {
            switch (group)
            {
                case InterceptRuleGroup.NeverIntercept:
                    return "永不拦截";
                case InterceptRuleGroup.AlwaysIntercept:
                    return "总是拦截";
                case InterceptRuleGroup.HostileIntercept:
                    return "按敌对关系拦截";
                default:
                    return "拦截规则";
            }
        }

        public IEnumerable<ObservedAirTarget> GetObservedAirTargets()
        {
            AllMapProjectileStorage storage = this._AllMapProjectileStorage;
            return storage != null ? storage.GetObservedAirTargets() : Enumerable.Empty<ObservedAirTarget>();
        }

        private void DrawCheckbox(Listing_Standard listing, string label, string tooltip, ref bool value, ref bool changed)
        {
            bool previous = value;
            listing.CheckboxLabeled(label, ref value, tooltip);
            if (previous != value)
            {
                changed = true;
            }
        }

        private void DrawRuleHandle(Rect rect, InterceptRuleGroup group, ref bool changed)
        {
            Rect topRect = new Rect(rect.x, rect.y + 2f, rect.width, 30f);
            int count = this.GetRuleDefNames(group).Count;
            int missing = this.CountMissingRuleDefNames(group);
            string summary = this.GetRuleTitle(group) + "：" + count + " 项";
            if (missing > 0)
            {
                summary += "，" + missing + " 个缺失";
            }

            Rect summaryRect = new Rect(topRect.x, topRect.y + 5f, topRect.width - 112f, 24f);
            Widgets.Label(summaryRect, summary);
            Rect buttonRect = new Rect(topRect.xMax - 104f, topRect.y, 104f, 28f);
            if (Widgets.ButtonText(buttonRect, "管理..."))
            {
                Find.WindowStack.Add(new Dialog_InterceptTargetSelector(this, group));
            }

            if (this.SettingsData != null && this.SettingsData.showAdvancedDefNameEditor)
            {
                Rect textRect = new Rect(rect.x, topRect.yMax + 6f, rect.width, 28f);
                string oldValue = this.GetRuleDefNamesRaw(group) ?? string.Empty;
                string newValue = Widgets.TextField(textRect, oldValue);
                if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                {
                    this.SetRuleDefNamesRaw(group, newValue);
                    changed = true;
                }
            }
            else
            {
                Rect hintRect = new Rect(rect.x, topRect.yMax + 8f, rect.width, 22f);
                Color oldColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(hintRect, "展开高级选项可直接编辑 defName。");
                GUI.color = oldColor;
            }
        }

        private void ApplySettingsChanged(bool writeSettings)
        {
            this.RefreshCachedSettings();
            if (writeSettings)
            {
                this.WriteSettings();
            }
        }

        private float GetSettingsViewHeight()
        {
            return 190f + ((this.RuleRowHeight + 6f) * 3f);
        }

        private void RefreshCachedSettings()
        {
            this.neverInterceptDefNames = ParseDefNameList(this.GetRuleDefNamesRaw(InterceptRuleGroup.NeverIntercept));
            this.alwaysInterceptDefNames = ParseDefNameList(this.GetRuleDefNamesRaw(InterceptRuleGroup.AlwaysIntercept));
            this.interceptHostileDefNames = ParseDefNameList(this.GetRuleDefNamesRaw(InterceptRuleGroup.HostileIntercept));
        }

        private string GetRuleDefNamesRaw(InterceptRuleGroup group)
        {
            AntiAirWeaponSettings data = this.SettingsData;
            if (data == null)
            {
                return string.Empty;
            }

            switch (group)
            {
                case InterceptRuleGroup.NeverIntercept:
                    return data.neverInterceptDefs;
                case InterceptRuleGroup.AlwaysIntercept:
                    return data.alwaysInterceptDefs;
                case InterceptRuleGroup.HostileIntercept:
                    return data.interceptHostileDefs;
                default:
                    return string.Empty;
            }
        }

        private void SetRuleDefNames(InterceptRuleGroup group, IEnumerable<string> defNames)
        {
            this.SetRuleDefNamesRaw(group, SerializeDefNameList(defNames));
        }

        private void SetRuleDefNamesRaw(InterceptRuleGroup group, string rawValue)
        {
            AntiAirWeaponSettings data = this.SettingsData;
            if (data == null)
            {
                return;
            }

            switch (group)
            {
                case InterceptRuleGroup.NeverIntercept:
                    data.neverInterceptDefs = rawValue ?? string.Empty;
                    break;
                case InterceptRuleGroup.AlwaysIntercept:
                    data.alwaysInterceptDefs = rawValue ?? string.Empty;
                    break;
                case InterceptRuleGroup.HostileIntercept:
                    data.interceptHostileDefs = rawValue ?? string.Empty;
                    break;
            }
        }

        private static HashSet<string> ParseDefNameList(string rawValue)
        {
            return new HashSet<string>(ParseDefNameListOrdered(rawValue), StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> ParseDefNameListOrdered(string rawValue)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return result;
            }

            string[] parts = rawValue.Split(DefNameSeparators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string defName = NormalizeDefName(parts[i]);
                if (defName.Length > 0 && seen.Add(defName))
                {
                    result.Add(defName);
                }
            }

            return result;
        }

        private static string SerializeDefNameList(IEnumerable<string> defNames)
        {
            if (defNames == null)
            {
                return string.Empty;
            }
            return string.Join(";", defNames.Select(NormalizeDefName).Where(defName => !string.IsNullOrEmpty(defName)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        }

        private static string NormalizeDefName(string defName)
        {
            return string.IsNullOrWhiteSpace(defName) ? string.Empty : defName.Trim();
        }

        private HashSet<string> neverInterceptDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> alwaysInterceptDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> interceptHostileDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
