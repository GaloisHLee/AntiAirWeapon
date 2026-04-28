using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiAirWeapon.Buildings
{
	public class Building_AirDefense : Building
	{
		// Token: 0x06000013 RID: 19 RVA: 0x00002A98 File Offset: 0x00000C98
		public Building_AirDefense()
		{
			this.top = new TurretTop_CustomSize(this);
			this.stunner = new StunHandler(this);
			this.nowTarget = null;
			this.restoreBullet = 0;
			//this.openTicks = 0;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002AF8 File Offset: 0x00000CF8
		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
			stringBuilder.Append(this.underRoof ? ("\n" + Translator.Translate("AntiAirWeaponUnderRoof").ToString() + "\n") : "\n");
			stringBuilder.Append(Translator.Translate("AntiAirWeaponKillCount") + ":" + this.killCount + "\n");
			stringBuilder.Append(Translator.Translate("AntiAirWeaponRestoreBullet") + ":" + this.restoreBullet);
			bool flag = this.restoreBullet < this.TopSizeComp.Props.maxStoreBullet;
			if (flag)
			{
				stringBuilder.Append("\n");
				stringBuilder.Append(string.Concat(new object[]
				{
					Translator.Translate("AntiAirWeaponRestoreProgress"),
					":",
					(float)(this.ticks * 100 / this.ticksAirInterval),
					"%"
				}));
			}
			bool flag2 = this.atkTicks > 0;
			if (flag2)
			{
				stringBuilder.Append(string.Concat(new string[]
				{
					"\n",
					Translator.Translate("AntiAirWeaponAtkSecondLabel"),
					":",
					((float)((double)((float)this.atkTicks * 1f) / 60.0)).ToString("#0.0"),
					Translator.Translate("AntiAirWeaponAtkSecond")
				}));
			}
			return stringBuilder.ToString();
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000015 RID: 21 RVA: 0x00002C60 File Offset: 0x00000E60
		private IEnumerable<IntVec3> AirCells
		{
			get
			{
				return this.AirCellsAround(base.Position, base.Map);
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000016 RID: 22 RVA: 0x00002C84 File Offset: 0x00000E84
		private bool ShouldAttackNow
		{
			get
			{
				bool flag = !base.Spawned;
				bool result;
				if (flag)
				{
					result = false;
				}
				else
				{
					bool flag2 = !FlickUtility.WantsToBeOn(this);
					if (flag2)
					{
						result = false;
					}
					else
					{
						bool underRoof = this.underRoof;
						if (underRoof)
						{
							result = false;
						}
						else
						{
							bool flag3 = this.atkTicks > 0;
							if (flag3)
							{
								result = false;
							}
							else
							{
								bool flag4 = false;//this.openTicks < 1;
								if (flag4)
								{
									result = false;
								}
								else
								{
									CompPowerTrader compPowerTrader = this.TryGetComp<CompPowerTrader>();
									bool flag5 = compPowerTrader != null && !compPowerTrader.PowerOn;
									if (flag5)
									{
										result = false;
									}
									else
									{
										bool stunned = this.stunner.Stunned;
										if (stunned)
										{
											result = false;
										}
										else
										{
											CompRefuelable compRefuelable = this.TryGetComp<CompRefuelable>();
											result = (compRefuelable == null || compRefuelable.HasFuel);
										}
									}
								}
							}
						}
					}
				}
				return result;
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002D48 File Offset: 0x00000F48
		protected override void Tick()
		{
			base.Tick();
			this.doTurretTick();
			bool flag = this.restoreBullet < this.TopSizeComp.Props.maxStoreBullet;
			if (flag)
			{
				CompPowerTrader compPowerTrader = this.TryGetComp<CompPowerTrader>();
				bool flag2 = compPowerTrader != null && compPowerTrader.PowerOn;
				if (flag2)
				{
					this.ticks++;
				}
			}
			bool flag3 = this.atkTicks > 0;
			if (flag3)
			{
				this.atkTicks--;
			}
			/*
			bool flag4 = this.openTicks > 0;
			if (flag4)
			{
				this.openTicks--;
			}
			*/
			bool flag5 = this.ticks >= this.ticksAirInterval;
			if (flag5)
			{
				this.restoreBullet++;
				this.ticks = 0;
			}
			bool flag6 = this.restoreBullet > 0;
			if (flag6)
			{
				bool shouldAttackNow = this.ShouldAttackNow;
				if (shouldAttackNow)
				{
					//Log.Message("我可以攻击！");
					//寻找可以打击的对象
					bool canShotProjectile = this.topSizeComp.Props.canShotProjectile;
					bool canShotPod = this.topSizeComp.Props.canShotPod;
					AllMapProjectileStorage aps = AntiAirWeaponModBase.Instance._AllMapProjectileStorage;
						Thing atkTarget = null;
						List<Thing> atkList = aps.mapAndThings;//.TryGetValue(this.Map.Index );
						if (atkList != null)
						{
							//Log.Message("正在寻找目标！"+canShotProjectile+"/"+canShotPod);
							//test
							for (int i = 0; i < atkList.Count; i++)
							{
								Thing a = atkList[i];
								string aRes = "";// "目标("+a+")结果:";

								if (aps.invalidThing(a))
								{
									atkList.RemoveAt(i);
									i--;
									continue;
								}
								if (!inMySight(a))
								{
									aRes += "超出|";
								}

								string targetReason;
								if (!this.shouldInterceptTarget(a, canShotProjectile, canShotPod, out targetReason))
								{
									aRes += targetReason;
								}

								if (aRes != "")
								{
									//Log.Message("目标(" + a + ")结果:" + aRes);
									continue;
								}

								atkTarget = a;
							}

							/*
							atkTarget = atkList.Find(a=> !aps.invalidThing(a) && inMySight(a)&&
							(
							
							 canShotProjectile ?a.def.projectile!=null:false ||
							 canShotPod ? a is Skyfaller : false 
							)

						);
						*/
						//Log.Message("目标"+atkTarget);
					}
					 
					
					

					 
					bool flag7 = atkTarget!=null&& atkTarget.Spawned;
					if (flag7)
					{
						 
						this.turnAndAttack(atkTarget);
					}
					else
					{
						this.nowTarget = null;
					}
				}
			}
		}

		//是否在可攻击范围内
		private bool inMySight(Thing t)
		{
			if (t.Map != this.Map) { return false; }
			IntVec3 tp = t.Position;
			IntVec3 mp = this.Position;

			//X Z
			float rangeSqrt = Mathf.Abs(Mathf.Pow( (tp.x - mp.x),2) + Mathf.Pow( (tp.z - mp.z),2));
			float mySqrt= Mathf.Pow(this.range,2);
			//Log.Message("当前物体(" + t + ")坐标！" +t.Position+"?"+ rangeSqrt + "/" + mp+"?"+mySqrt+"GG"+(tp.z - mp.z));
			//Log.Message("MP:"+mp);
			//Log.Message("TP:" + tp);
			return rangeSqrt <= mySqrt;
		}

		private bool shouldInterceptTarget(Thing target, bool canShotProjectile, bool canShotPod, out string reason)
		{
			reason = string.Empty;
			AntiAirWeaponModBase mod = AntiAirWeaponModBase.Instance;
			if (target == null || mod == null)
			{
				reason = "无效目标|";
				return false;
			}
			bool isProjectile = target is Projectile;
			bool isActiveDropPod = target is IActiveTransporter;
			bool isSkyfaller = target is Skyfaller;
			if (isProjectile && !canShotProjectile)
			{
				reason = "炮塔不可拦截炮弹|";
				return false;
			}
			if ((isActiveDropPod || isSkyfaller) && !canShotPod)
			{
				reason = "炮塔不可拦截飞行舱|";
				return false;
			}
			if (mod.ShouldNeverIntercept(target))
			{
				reason = "配置忽略|";
				return false;
			}
			if (mod.ShouldAlwaysIntercept(target))
			{
				return true;
			}

			Faction targetFaction = this.resolveTargetFaction(target);
			bool hostileToTurret = this.isHostileToTurret(targetFaction);
			if (mod.ShouldInterceptIfHostile(target))
			{
				reason = hostileToTurret ? string.Empty : "自定义目标非敌对|";
				return hostileToTurret;
			}

			if (isProjectile)
			{
				if (!mod.InterceptHostileProjectiles)
				{
					reason = "配置禁用炮弹拦截|";
					return false;
				}
				reason = hostileToTurret ? string.Empty : "炮弹非敌对或无法识别|";
				return hostileToTurret;
			}

			if (isActiveDropPod)
			{
				if (!mod.InterceptHostilePods)
				{
					reason = "配置禁用飞行舱拦截|";
					return false;
				}
				reason = hostileToTurret ? string.Empty : "飞行舱非敌对或无法识别|";
				return hostileToTurret;
			}

			if (isSkyfaller)
			{
				if (!mod.InterceptUnknownSkyfallers)
				{
					reason = "未知 Skyfaller 默认忽略|";
					return false;
				}
				return true;
			}

			reason = "不支持的飞行物|";
			return false;
		}

		private Faction resolveTargetFaction(Thing target)
		{
			if (target == null)
			{
				return null;
			}

			Faction targetFaction = target.Faction;
			Projectile projectile = target as Projectile;
			if (projectile != null)
			{
				return projectile.Launcher != null ? projectile.Launcher.Faction : targetFaction;
			}

			IActiveTransporter activeDropPod = target as IActiveTransporter;
			if (activeDropPod != null && targetFaction == null)
			{
				try
				{
					Thing innerThing = activeDropPod.Contents.innerContainer.FirstOrDefault(tt => tt != null && tt.Faction != null);
					if (innerThing != null)
					{
						targetFaction = innerThing.Faction;
					}
				}
				catch (Exception)
				{
				}
			}

			return targetFaction;
		}

		private bool isHostileToTurret(Faction targetFaction)
		{
			return this.Faction != null && targetFaction != null && this.Faction.HostileTo(targetFaction);
		}




		// Token: 0x06000018 RID: 24 RVA: 0x00002E80 File Offset: 0x00001080
 
		// Token: 0x06000019 RID: 25 RVA: 0x00002FB0 File Offset: 0x000011B0
		public List<IntVec3> AirCellsAround(IntVec3 pos, Map map)
		{
			bool flag = this.airCells.Count > 0;
			List<IntVec3> result;
			if (flag)
			{
				result = this.airCells;
			}
			else
			{
				int num = GenRadial.NumCellsInRadius((float)this.range);
				for (int i = 0; i < num; i++)
				{
					this.airCells.Add(pos + GenRadial.RadialPattern[i]);
				}
				result = this.airCells;
			}
			return result;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00003024 File Offset: 0x00001224
		public List<IntVec3> HugeGetCells(IntVec3 pos, Map map)
		{
			List<IntVec3> list = new List<IntVec3>();
			int num = GenRadial.NumCellsInRadius(10f);
			for (int i = 0; i < num; i++)
			{
				list.Add(pos + GenRadial.RadialPattern[i]);
			}
			return list;
		}

		 

		// Token: 0x0600001C RID: 28 RVA: 0x000031A0 File Offset: 0x000013A0
		private void turnAndAttack(Thing t)
		{
			if (t.Destroyed) {
				return;
			}
			this.nowTarget = t;
			this.doTurretTick();
			SoundDef named = DefDatabase<SoundDef>.GetNamed(this.TopSizeComp.Props.soundShoot, true);
			named.PlayOneShot(new TargetInfo(base.Position, base.Map, true));
			//MoteMaker.ThrowExplosionCell(base.Position, base.Map, ThingDefOf.Explosion, new Color(1f, 1f, 1f));
			FleckMaker.ThrowExplosionCell(base.Position,base.Map,FleckDefOf.ExplosionFlash, new Color(1f, 1f, 1f));
			MoteMaker.ThrowText(this.DrawPos, base.Map, Translator.Translate("AntiAirWeaponHitText"), new Color(0.66f, 0.66f, 0.12f), -1f);
			bool flag3 = t is Projectile;
			if (flag3)
			{
				//MoteMaker.ThrowSmoke(t.DrawPos, base.Map, 3f);
				FleckMaker.ThrowSmoke(t.DrawPos,base.Map,3f);
			}
			else
			{
				int newX = (int)(Math.Sin((double)this.top.CurRotation * 3.141592653589793 / 180.0) * 10.0);
				int newZ = (int)(Math.Cos((double)this.top.CurRotation * 3.141592653589793 / 180.0) * 10.0);
				IntVec3 intVec = base.Position + new IntVec3(newX, 0, newZ);
				//MoteMaker.MakeBombardmentMote(intVec, base.Map);
				//MoteMaker.MakeBombardmentMote(intVec,base.Map,0.5f);
				FleckMaker.ThrowSmoke(t.DrawPos, base.Map, 3f);
				SoundDefOf.Thunder_OnMap.PlayOneShot(new TargetInfo(base.Position, base.Map, true));
				float magnitude = (base.Position.ToVector3Shifted() - Find.Camera.transform.position).magnitude;
				Find.CameraDriver.shaker.DoShake(60f / magnitude);
			}
			bool flag4 = t is DropPodIncoming ;
			/*
			if (flag4)
			{
				foreach (Thing thing in this.HugeAirThings(this.HugeGetCells(t.Position, base.Map)))
				{
					bool flag5 = thing != null && thing == t;
					if (!flag5)
					{
						bool flag6 = thing != null && !thing.Destroyed;
						if (flag6)
						{
							this.destoryAir(thing);
						}
					}
				}
			}
			*/
			this.destoryAir(t);
			this.nowTarget = null;
			this.restoreBullet--;
			this.killCount++;
			this.top.aimCanATK = false;
			this.atkTicks = this.TopSizeComp.Props.atkTicks;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x000034C4 File Offset: 0x000016C4
		public void destoryAir(Thing t)
		{
			
			ActiveTransporterInfo adpi = null;
			if (t is IActiveTransporter)
			{
				adpi = (t as IActiveTransporter).Contents;
			}
			
			if (adpi!=null)
			{
				bool isPlayerInside = false;
				List<Thing> list = new List<Thing>();
				StringBuilder shotDownInfo = new StringBuilder();
				shotDownInfo.AppendLine(TranslatorFormattedStringExtensions.Translate("BeshotdownMsg", this.Faction.ToString()).ToString());

				for (int i = adpi.innerContainer.Count - 1; i >= 0; i--)
				{
					Thing thing = adpi.innerContainer[i];
					bool flag2 = thing == null;
					if (!flag2)
					{
						bool flag3 = thing is Pawn;
						if (flag3)
						{
							Pawn pawn = thing as Pawn;
							if (pawn.Faction == Faction.OfPlayer) {
								isPlayerInside = true;
								shotDownInfo.AppendLine("    -" + pawn.Name);
							}
							HealthUtility.DamageUntilDowned(pawn, true);
							list.Add(pawn);
						}
						else
						{
							list.Add(thing);
						}
					}
				}
				//Log.Message(adpi.innerContainer[0].ToString());
				//Log.Message(this.Faction.ToString() );
				if (isPlayerInside) {
					Find.LetterStack.ReceiveLetter(Translator.Translate("Beshotdown"), shotDownInfo.ToString(), LetterDefOf.Death, this, null, null);
				}

				

				System.Random random = new System.Random();
				foreach (Thing thing2 in list)
				{
					bool flag4 = !(thing2 is Pawn);
					if (flag4)
					{
						int num = random.Next(100);
						bool flag5 = false;//num >= Settings.droppodCargoDropPercentage;
						if (flag5)
						{
							continue;
						}
					}
					GenPlace.TryPlaceThing(thing2, t.Position, base.Map, ThingPlaceMode.Near, null, null);
						SoundDef dropPodOpenSound = DefDatabase<SoundDef>.GetNamed("DropPod_Open", false);
						if (dropPodOpenSound != null)
						{
							dropPodOpenSound.PlayOneShot(new TargetInfo(thing2));
						}
				}
				for (int j = 0; j < 3; j++)
				{
					Thing thing3 = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel, null);
					GenPlace.TryPlaceThing(thing3, t.Position, base.Map, ThingPlaceMode.Near, null, null);
				}
			}
			else
			{
				bool flag6 = t is Skyfaller && (t as Skyfaller).def.defName == "CrashedShipPartIncoming";
				if (flag6)
				{
						GenExplosion.DoExplosion(t.Position, t.Map, 2f, DamageDefOf.Bomb, this, 3, -1f, DefDatabase<SoundDef>.GetNamed("Explosion_Bomb", true), null, null, t);
					for (int k = 0; k < 15; k++)
					{
						Thing thing4 = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel, null);
					}

					/*
					ThingDef crashedPoisonShipPart =  ThingDefOf.CrashedPoisonShipPart;
					foreach (ThingDefCountClass thingDefCountClass in crashedPoisonShipPart.killedLeavings)
					{
						Thing thing5 = ThingMaker.MakeThing(thingDefCountClass.thingDef, null);
						thing5.stackCount = thingDefCountClass.count;
						GenPlace.TryPlaceThing(thing5, t.Position, base.Map, ThingPlaceMode.Near, null, null);
					}
					*/
				}
			}
			t.Destroy(DestroyMode.Vanish);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00003788 File Offset: 0x00001988
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref this.killCount, "killCount", 0, false);
			Scribe_Values.Look<int>(ref this.ticks, "ticks", 0, false);
			Scribe_Values.Look<int>(ref this.restoreBullet, "restoreBullet", 0, false);
			Scribe_Values.Look<int>(ref this.atkTicks, "atkTicks", 0, false);
			//Scribe_Values.Look<int>(ref this.openTicks, "openTicks", 0, false);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000037EC File Offset: 0x000019EC
 
		// Token: 0x06000020 RID: 32 RVA: 0x00003A10 File Offset: 0x00001C10
		 

		// Token: 0x06000021 RID: 33 RVA: 0x00003AE4 File Offset: 0x00001CE4
		private void doTurretTick()
		{
			CompPowerTrader compPowerTrader = this.TryGetComp<CompPowerTrader>();
			bool flag = (compPowerTrader == null || compPowerTrader.PowerOn) && base.Spawned;
			if (flag)
			{
				this.top.TurretTopTick();
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00003B1F File Offset: 0x00001D1F
		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			this.top.DrawTurret();
			base.Comps_PostDraw();
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00003B35 File Offset: 0x00001D35
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			this.topSizeComp = base.GetComp<CompTurretTopSize>();
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000024 RID: 36 RVA: 0x00003B4D File Offset: 0x00001D4D
		private int range
		{
			get
			{
				return this.TopSizeComp.Props.atkRange;
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000025 RID: 37 RVA: 0x00003B5F File Offset: 0x00001D5F
		private int ticksAirInterval
		{
			get
			{
				return this.TopSizeComp.Props.storeTick;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00002A7D File Offset: 0x00000C7D
		private bool underRoof
		{
			get
			{
				return base.Position.Roofed(base.Map);
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000027 RID: 39 RVA: 0x00003B74 File Offset: 0x00001D74
		// (set) Token: 0x06000028 RID: 40 RVA: 0x00003B8C File Offset: 0x00001D8C

		/*
		public int OpenTick
		{
			get
			{
				return this.openTicks;
			}
			set
			{
				this.openTicks = value;
			}
		}
		*/
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000029 RID: 41 RVA: 0x00003B96 File Offset: 0x00001D96
		public CompTurretTopSize TopSizeComp
		{
			get
			{
				return this.topSizeComp;
			}
		}

		// Token: 0x04000009 RID: 9
		protected TurretTop_CustomSize top;

		// Token: 0x0400000A RID: 10
		protected CompTurretTopSize topSizeComp;

		// Token: 0x0400000B RID: 11
		protected StunHandler stunner;

		// Token: 0x0400000C RID: 12
		public LocalTargetInfo nowTarget;

		// Token: 0x0400000D RID: 13
		public List<IntVec3> airCells = new List<IntVec3>();

		// Token: 0x0400000E RID: 14
	 

		// Token: 0x0400000F RID: 15
		private int ticks;

		// Token: 0x04000010 RID: 16
		private int atkTicks;

		// Token: 0x04000011 RID: 17
		//private int openTicks;

		// Token: 0x04000012 RID: 18
		private int restoreBullet;

		private int killCount;
	}
}
