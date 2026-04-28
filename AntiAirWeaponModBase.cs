using HugsLib;
using HugsLib.Utils;
using System;
using System.Collections.Generic;
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
            get { return Find.World.GetComponent<AllMapProjectileStorage>(); }
        }

        public bool InterceptHostilePods => this.interceptHostilePods == null || this.interceptHostilePods.Value;
        public bool InterceptHostileProjectiles => this.interceptHostileProjectiles == null || this.interceptHostileProjectiles.Value;
        public bool InterceptUnknownSkyfallers => this.interceptUnknownSkyfallers != null && this.interceptUnknownSkyfallers.Value;

        public AntiAirWeaponModBase()
        {
            Instance = this;
        }

        public override string ModIdentifier => "AntiAirWeapon";

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
            this.neverInterceptDefs = this.Settings.GetHandle<string>(
                "neverInterceptDefs",
                "永不拦截 defName",
                "逗号或分号分隔。命中列表的飞行物永远不会被防空炮拦截。",
                string.Empty);
            this.alwaysInterceptDefs = this.Settings.GetHandle<string>(
                "alwaysInterceptDefs",
                "总是拦截 defName",
                "逗号或分号分隔。命中列表的飞行物会被防空炮强制拦截，不再检查派系。",
                string.Empty);
            this.interceptHostileDefs = this.Settings.GetHandle<string>(
                "interceptHostileDefs",
                "按敌对关系拦截 defName",
                "逗号或分号分隔。命中列表的飞行物会尝试解析派系，只拦截敌对目标。",
                string.Empty);
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

        private void RefreshCachedSettings()
        {
            this.neverInterceptDefNames = ParseDefNameList(this.neverInterceptDefs?.Value);
            this.alwaysInterceptDefNames = ParseDefNameList(this.alwaysInterceptDefs?.Value);
            this.interceptHostileDefNames = ParseDefNameList(this.interceptHostileDefs?.Value);
        }

        private static HashSet<string> ParseDefNameList(string rawValue)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return result;
            }

            string[] parts = rawValue.Split(DefNameSeparators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string defName = parts[i].Trim();
                if (defName.Length > 0)
                {
                    result.Add(defName);
                }
            }

            return result;
        }

        private HugsLib.Settings.SettingHandle<bool> interceptHostilePods;
        private HugsLib.Settings.SettingHandle<bool> interceptHostileProjectiles;
        private HugsLib.Settings.SettingHandle<bool> interceptUnknownSkyfallers;
        private HugsLib.Settings.SettingHandle<string> neverInterceptDefs;
        private HugsLib.Settings.SettingHandle<string> alwaysInterceptDefs;
        private HugsLib.Settings.SettingHandle<string> interceptHostileDefs;
        private HashSet<string> neverInterceptDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> alwaysInterceptDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> interceptHostileDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
