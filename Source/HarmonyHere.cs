using AntiAirWeapon.Buildings;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AntiAirWeapon
{
    [StaticConstructorOnStartup]
    public static class HarmonyStartUp
    {
        private const string HarmonyId = "credmao.antiairweapon.forked";

        static HarmonyStartUp()
        {
            Harmony harmony = new Harmony(HarmonyId);
            int applied = 0;

            applied += PatchPostfix(
                harmony,
                AccessTools.Method(
                    typeof(GenSpawn),
                    nameof(GenSpawn.Spawn),
                    new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(WipeMode) }),
                AccessTools.Method(typeof(Harmony_Gen), nameof(Harmony_Gen.PostfixSpawnThing)),
                "GenSpawn.Spawn(Thing, IntVec3, Map, WipeMode)");

            applied += PatchPostfix(
                harmony,
                AccessTools.Method(
                    typeof(GenSpawn),
                    nameof(GenSpawn.Spawn),
                    new[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) }),
                AccessTools.Method(typeof(Harmony_Gen), nameof(Harmony_Gen.PostfixSpawnThing)),
                "GenSpawn.Spawn(Thing, IntVec3, Map, Rot4, WipeMode, bool, bool)");

            applied += PatchPostfix(
                harmony,
                AccessTools.Method(typeof(SymbolResolver_EdgeDefense), "Resolve", new[] { typeof(ResolveParams) }),
                AccessTools.Method(typeof(Harmony_Settlement), nameof(Harmony_Settlement.Postfix)),
                "SymbolResolver_EdgeDefense.Resolve(ResolveParams)");

            Log.Message("[AntiAirWeaponForked] Harmony patches applied: " + applied);
        }

        private static int PatchPostfix(Harmony harmony, MethodBase original, MethodInfo postfix, string label)
        {
            if (original == null)
            {
                Log.Warning("[AntiAirWeaponForked][WARN] Harmony target not found, skipping patch: " + label);
                return 0;
            }
            if (postfix == null)
            {
                Log.Error("[AntiAirWeaponForked][ERR] Harmony postfix not found, skipping patch: " + label);
                return 0;
            }

            try
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                return 1;
            }
            catch (Exception ex)
            {
                Log.Error("[AntiAirWeaponForked][ERR] Failed to apply Harmony patch: " + label + "\n" + ex);
                return 0;
            }
        }
    }

    public static class Harmony_Gen
    {
        public static void PostfixSpawnThing(Thing newThing)
        {
            try
            {
                if (newThing == null || newThing.def == null)
                {
                    return;
                }

                ThingDef thingDef = newThing.def;
                bool isTrackedAirThing = (thingDef.projectile != null && thingDef.projectile.flyOverhead) || newThing is Skyfaller;
                if (!isTrackedAirThing || Find.World == null)
                {
                    return;
                }

                AllMapProjectileStorage storage = Find.World.GetComponent<AllMapProjectileStorage>();
                if (storage != null)
                {
                    storage.addThing(newThing);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[AntiAirWeaponForked][WARN] Failed to record spawned air target: " + ex.Message);
            }
        }
    }

    public static class Harmony_Settlement
    {
        public static void Postfix(ResolveParams rp)
        {
            Faction faction = rp.faction ?? Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined);
            int width;
            if (rp.edgeDefenseWidth != null)
            {
                width = rp.edgeDefenseWidth.Value;
            }
            else if (rp.edgeDefenseMortarsCount != null && rp.edgeDefenseMortarsCount.Value > 0)
            {
                width = 4;
            }
            else
            {
                width = Rand.Bool ? 2 : 4;
            }

            width = Mathf.Clamp(width, 1, Mathf.Min(rp.rect.Width, rp.rect.Height) / 2);
            int turretCount;
            bool singleCellEdge;
            switch (width)
            {
                case 1:
                    turretCount = rp.edgeDefenseTurretsCount ?? 0;
                    singleCellEdge = true;
                    break;
                default:
                    turretCount = rp.edgeDefenseTurretsCount ?? (rp.rect.EdgeCellsCount / 50);
                    singleCellEdge = false;
                    break;
            }

            CellRect simpleRect = singleCellEdge ? rp.rect : rp.rect.ContractedBy(1);
            int advancedCount = (int)(turretCount * 0.3f);
            if (turretCount > 0 && advancedCount < 1)
            {
                advancedCount = 1;
            }

            for (int i = 0; i < turretCount - advancedCount; i++)
            {
                ResolveParams turretParams = rp;
                turretParams.faction = faction;
                turretParams.singleThingDef = ThingDef.Named("AntiAirWeapon_Simple");
                turretParams.rect = simpleRect;
                turretParams.edgeThingAvoidOtherEdgeThings = rp.edgeThingAvoidOtherEdgeThings ?? true;
                BaseGen.symbolStack.Push("edgeThing", turretParams, null);
            }

            CellRect advancedRect = singleCellEdge ? rp.rect : rp.rect.ContractedBy(3);
            for (int i = 0; i < advancedCount; i++)
            {
                ResolveParams turretParams = rp;
                turretParams.faction = faction;
                turretParams.singleThingDef = ThingDef.Named("AntiAirWeapon_Advance");
                turretParams.rect = advancedRect;
                turretParams.edgeThingAvoidOtherEdgeThings = rp.edgeThingAvoidOtherEdgeThings ?? true;
                BaseGen.symbolStack.Push("edgeThing", turretParams, null);
            }
        }
    }
}
