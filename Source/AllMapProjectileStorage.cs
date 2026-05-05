using AntiAirWeapon.Settings;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AntiAirWeapon
{
    public class AllMapProjectileStorage : WorldComponent, IExposable
    {
        public List<Thing> mapAndThings = new List<Thing>();
        private const int MaxObservedAirTargets = 100;
        private List<string> observedDefNames = new List<string>();
        private List<string> observedKinds = new List<string>();
        private List<int> observedTicks = new List<int>();
        //public Dictionary<int, AirTargets> mapAndThings = new Dictionary<int, AirTargets>();
        //List<int> maps;
        //List<AirTargets> things;

        public AllMapProjectileStorage(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref this.mapAndThings, "mapAndThings",   LookMode.Reference);//,ref maps,ref things);
            Scribe_Collections.Look(ref this.observedDefNames, "observedDefNames", LookMode.Value);
            Scribe_Collections.Look(ref this.observedKinds, "observedKinds", LookMode.Value);
            Scribe_Collections.Look(ref this.observedTicks, "observedTicks", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.observedDefNames = this.observedDefNames ?? new List<string>();
                this.observedKinds = this.observedKinds ?? new List<string>();
                this.observedTicks = this.observedTicks ?? new List<int>();
                this.TrimObservedAirTargets();
            }
        }

        public bool invalidThing(Thing thing) {
            if (thing == null || thing.Map == null || !thing.Spawned||thing.Destroyed)
            {
                return true;
            }
            return false;
        }

 



        public Thing mapHasThing(Thing thing ) {
            //Log.Message("Gtep1");
            if (invalidThing(thing)) {
                //Log.Message("Gtep1-1");
                //Log.Message("----这玩意儿无了");
                return null;
            }
            //Log.Message("Gtep2");
            bool hasList = mapAndThings != null;//mapAndThings.TryGetValue(thing.Map.Index , out AirTargets thingList);
            //Log.Message("Gtep3");
            if (hasList&& mapAndThings != null&& mapAndThings.Count>0) {
                //Log.Message("Gtep3-1");
                //Log.Message("----有物品列表");
                mapAndThings.RemoveAll(x => invalidThing(x));
                
                //Log.Message("Gtep3-2");
                Thing result=
                 mapAndThings.Find(a => !invalidThing(a) &&

                    a.thingIDNumber == thing.thingIDNumber



                ) ;
                //Log.Message("Gtep3-3");
                //Log.Message("----has结果:"+result);
                return result;
            }
            //Log.Message("Gtep4");

            return null;
        }

        public void addThing(Thing thing)
        {
            //Log.Message("===========Step1");
            if (invalidThing(thing))
            {
                //Log.Message("Step1-end");
                return  ;
            }
            this.RecordObservedAirTarget(thing);
            //Log.Message("Step2");
            bool hasList = mapAndThings != null;//.TryGetValue(thing.Map.Index , out AirTargets thingList);
            //Log.Message("Step3");
            if (!hasList)
            {

                mapAndThings = new List<Thing>();
                 
            }
            //Log.Message("Step4");
            if (mapHasThing(thing) == null)
            {
                //Log.Message("Step4-1");
                mapAndThings.Add(thing);
                //Log.Message("Step4-2");
                //Log.Message("----添加进入全局map物品" + thing.def.defName + "!");
            }
            else {
                //Log.Message("----已经有该物品" + thing.def.defName + "!");
            }

            //Log.Message("==========StepEnd===================");

            return ;
        }

        public void RecordObservedAirTarget(Thing thing)
        {
            if (thing == null || thing.def == null || string.IsNullOrEmpty(thing.def.defName))
            {
                return;
            }

            if (this.observedDefNames == null)
            {
                this.observedDefNames = new List<string>();
            }
            if (this.observedKinds == null)
            {
                this.observedKinds = new List<string>();
            }
            if (this.observedTicks == null)
            {
                this.observedTicks = new List<int>();
            }

            string defName = thing.def.defName;
            int existingIndex = this.observedDefNames.FindIndex(item => string.Equals(item, defName, System.StringComparison.OrdinalIgnoreCase));
            string kind = InterceptCandidate.Classify(thing.def).ToString();
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (existingIndex >= 0)
            {
                this.observedDefNames[existingIndex] = defName;
                this.EnsureObservedParallelLists(existingIndex);
                this.observedKinds[existingIndex] = kind;
                this.observedTicks[existingIndex] = tick;
            }
            else
            {
                this.observedDefNames.Add(defName);
                this.observedKinds.Add(kind);
                this.observedTicks.Add(tick);
            }
            this.TrimObservedAirTargets();
        }

        public IEnumerable<ObservedAirTarget> GetObservedAirTargets()
        {
            if (this.observedDefNames == null)
            {
                return Enumerable.Empty<ObservedAirTarget>();
            }

            List<ObservedAirTarget> result = new List<ObservedAirTarget>();
            for (int i = 0; i < this.observedDefNames.Count; i++)
            {
                string defName = this.observedDefNames[i];
                if (string.IsNullOrEmpty(defName))
                {
                    continue;
                }

                result.Add(new ObservedAirTarget
                {
                    DefName = defName,
                    Kind = this.observedKinds != null && i < this.observedKinds.Count ? this.observedKinds[i] : string.Empty,
                    LastSeenTick = this.observedTicks != null && i < this.observedTicks.Count ? this.observedTicks[i] : 0
                });
            }

            return result.OrderByDescending(item => item.LastSeenTick).ToList();
        }

        private void EnsureObservedParallelLists(int index)
        {
            while (this.observedKinds.Count <= index)
            {
                this.observedKinds.Add(string.Empty);
            }
            while (this.observedTicks.Count <= index)
            {
                this.observedTicks.Add(0);
            }
        }

        private void TrimObservedAirTargets()
        {
            List<ObservedAirTarget> ordered = this.GetObservedAirTargets()
                .Take(MaxObservedAirTargets)
                .ToList();
            this.observedDefNames = ordered.Select(item => item.DefName).ToList();
            this.observedKinds = ordered.Select(item => item.Kind).ToList();
            this.observedTicks = ordered.Select(item => item.LastSeenTick).ToList();
        }

        //清除 
        public bool removeThingsFromGlobal(int mapIndex, Thing thing)
        {

            bool hasList = mapAndThings != null;//.TryGetValue(mapIndex, out AirTargets thingList);
            if (hasList)
            {
                //Log.Message("----移除物品" + thing.def.defName + "!移除前剩余物品:"+thingList.Count);
                                 
                return mapAndThings.Remove(thing);
            }
            return false;
             
        }
    }
}
