using AntiAirWeapon.Buildings;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AntiAirWeapon
{
    [StaticConstructorOnStartup]
    public static class HarmonyStartUp
    {
        static HarmonyStartUp()
        {

            new Harmony("akreedz.rimworld.antiairweapon").PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("AntiAirWeapon Harmony Add");
        }
    }




    //物体生成提示测试
    [HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[]
{typeof(Thing),typeof(IntVec3),typeof(Map),typeof(Rot4),typeof(WipeMode),typeof(bool)
})]
    public static class Harmony_Gen
    {
        public static void Postfix(Thing newThing, IntVec3 loc, Map map, Rot4 rot, WipeMode wipeMode = WipeMode.Vanish, bool respawningAfterLoad = false)
        {
            if (newThing == null||newThing.def==null) { return; }
            
            ThingDef thingDef = newThing.def;
          
            if (  (thingDef.projectile!=null &&thingDef.projectile.flyOverhead)
                ||newThing is Skyfaller 
                ) {

                //Log.Message("enter"+ Find.World.GetComponent<AllMapProjectileStorage>());
                Find.World.GetComponent<AllMapProjectileStorage>().addThing(newThing);
            }
            

        }
    }



    //地图生成加上防空炮
    [HarmonyPatch(typeof(SymbolResolver_EdgeDefense), "Resolve", new Type[]
{typeof(ResolveParams)
})]
    public static class Harmony_Settlement
    {
        public static void Postfix(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            
            Faction faction = rp.faction ?? Find.FactionManager.RandomEnemyFaction(false, false, true, TechLevel.Undefined);
            int num = rp.edgeDefenseGuardsCount ?? 0;
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
                width = (Rand.Bool ? 2 : 4);
            }
            width = Mathf.Clamp(width, 1, Mathf.Min(rp.rect.Width, rp.rect.Height) / 2);
            int num2;
            int num3;
            bool flag;
            bool flag2;
            bool flag3;
            switch (width)
            {
                case 1:
                    num2 = (rp.edgeDefenseTurretsCount ?? 0);
                     
                    flag2 = true;
                   
                    break;
                case 2:
                    num2 = (rp.edgeDefenseTurretsCount ?? (rp.rect.EdgeCellsCount / 50));
                    
                    flag2 = false;
                    
                    break;
                case 3:
                    num2 = (rp.edgeDefenseTurretsCount ?? (rp.rect.EdgeCellsCount / 50));
                    
                     
                    flag2 = false;
                    
                    break;
                default:
                    num2 = (rp.edgeDefenseTurretsCount ?? (rp.rect.EdgeCellsCount / 50));
                    
                    flag2 = false;
                    
                    break;
            }

            CellRect rect3 = flag2 ? rp.rect : rp.rect.ContractedBy(1);
            num3 = (int)(num2 * 0.3);
            if (num2>0&&num3<1) {
                num3 = 1;
                 
            }
            Log.Message("num3:"+num3+",num2:"+num2);
            for (int l = 0; l < num2-num3; l++)
            {
                ResolveParams rp5 = rp;
                rp5.faction = faction;
                rp5.singleThingDef = ThingDef.Named("AntiAirWeapon_Simple");
                rp5.rect = rect3;
                rp5.edgeThingAvoidOtherEdgeThings = new bool?(rp.edgeThingAvoidOtherEdgeThings ?? true);
                BaseGen.symbolStack.Push("edgeThing", rp5, null);
            }
            CellRect rect4 = flag2 ? rp.rect : rp.rect.ContractedBy(3);
            for (int l = 0; l < num3; l++)
            {
                ResolveParams rp5 = rp;
                rp5.faction = faction;
                rp5.singleThingDef = ThingDef.Named("AntiAirWeapon_Advance");
                rp5.rect = rect4;
                rp5.edgeThingAvoidOtherEdgeThings = new bool?(rp.edgeThingAvoidOtherEdgeThings ?? true);
                BaseGen.symbolStack.Push("edgeThing", rp5, null);
            }

        }
    }





}
