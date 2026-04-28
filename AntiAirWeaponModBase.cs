using HugsLib;
using HugsLib.Utils;
using HugsLib.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using AntiAirWeapon.Settings;
using UnityEngine;
using Verse;

namespace AntiAirWeapon
{
    public class AntiAirWeaponModBase : ModBase
    {
        private static readonly char[] DefNameSeparators = new[] { ',', ';', '\n', '\r', '\t', '，', '；' };
        private static List<Action> TickActions = new List<Action>();

        public static AntiAirWeaponModBase Instance { get; private set; }

        public AllMapProjectileStorage _AllMapProjectileStorage
        {
            get { return Find.World != null ? Find.World.GetComponent<AllMapProjectileStorage>() : null; }
        }

        public bool InterceptHostilePods => this.interceptHostilePods == null || this.interceptHostilePods.Value;
        public bool InterceptHostileProjectiles => this.interceptHostileProjectiles == null || this.interceptHostileProjectiles.Value;
        public bool InterceptUnknownSkyfallers => this.interceptUnknownSkyfallers != null && this.interceptUnknownSkyfallers.Value;

        public AntiAirWeaponModBase()
        {
            Instance = this;
        }

        public override string ModIdentifier => "AntiAirWeaponForked";

        public static void RegisterTickAction(Action action)
        {
            TickActions.Add(action);
        }

        public override void Tick(int currentTick)
        {
            foreach (Action action in TickActions)
            {
                action();
            }
            TickActions.Clear();
        }

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            this.interceptHostilePods = this.Settings.GetHandle<bool>(
                "interceptHostilePods",
                "拦截敌对飞行舱",
                "启用后，防空炮会拦截识别为敌对派系的飞行舱或空投舱。",
                true);
            this.interceptHostileProjectiles = this.Settings.GetHandle<bool>(
                "interceptHostileProjectiles",
                "拦截敌对炮弹",
                "启用后，防空炮会拦截能够识别为敌对派系的高空炮弹。",
                true);
            this.interceptUnknownSkyfallers = this.Settings.GetHandle<bool>(
                "interceptUnknownSkyfallers",
                "拦截未知 Skyfaller",
                "启用后，未实现飞行舱接口、也无法从内容物识别派系的 Skyfaller 会按默认可拦截目标处理。",
                false);
            this.showAdvancedDefNameEditor = this.Settings.GetHandle<bool>(
                "showAdvancedDefNameEditor",
                "高级：显示原始 defName",
                "显示底层 defName 文本编辑框。普通配置建议使用管理窗口选择目标。",
                false);
            this.neverInterceptDefs = this.Settings.GetHandle<string>(
                "neverInterceptDefs",
                "永不拦截",
                "命中列表的飞行物永远不会被防空炮拦截。",
                string.Empty);
            this.alwaysInterceptDefs = this.Settings.GetHandle<string>(
                "alwaysInterceptDefs",
                "总是拦截",
                "命中列表的飞行物会被防空炮强制拦截，不再检查派系。",
                string.Empty);
            this.interceptHostileDefs = this.Settings.GetHandle<string>(
                "interceptHostileDefs",
                "按敌对关系拦截",
                "命中列表的飞行物会尝试解析派系，只拦截敌对目标。",
                string.Empty);
            this.ConfigureRuleHandle(this.neverInterceptDefs, InterceptRuleGroup.NeverIntercept);
            this.ConfigureRuleHandle(this.alwaysInterceptDefs, InterceptRuleGroup.AlwaysIntercept);
            this.ConfigureRuleHandle(this.interceptHostileDefs, InterceptRuleGroup.HostileIntercept);
            this.RefreshCachedSettings();
        }

        public override void SettingsChanged()
        {
            base.SettingsChanged();
            this.RefreshCachedSettings();
        }

        public override void WorldLoaded()
        {
            base.WorldLoaded();
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
            SettingHandle<string> handle = this.GetRuleHandle(group);
            return ParseDefNameListOrdered(handle?.Value);
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
            this.RefreshCachedSettings();
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
            this.RefreshCachedSettings();
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

        private void RefreshCachedSettings()
        {
            this.neverInterceptDefNames = ParseDefNameList(this.neverInterceptDefs?.Value);
            this.alwaysInterceptDefNames = ParseDefNameList(this.alwaysInterceptDefs?.Value);
            this.interceptHostileDefNames = ParseDefNameList(this.interceptHostileDefs?.Value);
        }

        private void ConfigureRuleHandle(SettingHandle<string> handle, InterceptRuleGroup group)
        {
            if (handle == null)
            {
                return;
            }

            handle.CustomDrawerHeight = 70f;
            handle.CustomDrawer = rect => this.DrawRuleHandle(rect, group, handle);
        }

        private bool DrawRuleHandle(Rect rect, InterceptRuleGroup group, SettingHandle<string> handle)
        {
            bool changed = false;
            Rect topRect = new Rect(rect.x, rect.y + 2f, rect.width, 30f);
            int count = this.GetRuleDefNames(group).Count;
            int missing = this.CountMissingRuleDefNames(group);
            string summary = count + " 项";
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

            if (this.showAdvancedDefNameEditor != null && this.showAdvancedDefNameEditor.Value)
            {
                Rect textRect = new Rect(rect.x, topRect.yMax + 6f, rect.width, 28f);
                string oldValue = handle.Value ?? string.Empty;
                string newValue = Widgets.TextField(textRect, oldValue);
                if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                {
                    handle.Value = newValue;
                    this.RefreshCachedSettings();
                    changed = true;
                }
            }
            else
            {
                Rect hintRect = new Rect(rect.x, topRect.yMax + 8f, rect.width, 22f);
                GUI.color = Color.gray;
                Widgets.Label(hintRect, "展开高级选项可直接编辑 defName。");
                GUI.color = Color.white;
            }

            return changed;
        }

        private SettingHandle<string> GetRuleHandle(InterceptRuleGroup group)
        {
            switch (group)
            {
                case InterceptRuleGroup.NeverIntercept:
                    return this.neverInterceptDefs;
                case InterceptRuleGroup.AlwaysIntercept:
                    return this.alwaysInterceptDefs;
                case InterceptRuleGroup.HostileIntercept:
                    return this.interceptHostileDefs;
                default:
                    return null;
            }
        }

        private void SetRuleDefNames(InterceptRuleGroup group, IEnumerable<string> defNames)
        {
            SettingHandle<string> handle = this.GetRuleHandle(group);
            if (handle != null)
            {
                handle.Value = SerializeDefNameList(defNames);
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

        private HugsLib.Settings.SettingHandle<bool> interceptHostilePods;
        private HugsLib.Settings.SettingHandle<bool> interceptHostileProjectiles;
        private HugsLib.Settings.SettingHandle<bool> interceptUnknownSkyfallers;
        private HugsLib.Settings.SettingHandle<bool> showAdvancedDefNameEditor;
        private HugsLib.Settings.SettingHandle<string> neverInterceptDefs;
        private HugsLib.Settings.SettingHandle<string> alwaysInterceptDefs;
        private HugsLib.Settings.SettingHandle<string> interceptHostileDefs;
        private HashSet<string> neverInterceptDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> alwaysInterceptDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> interceptHostileDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
