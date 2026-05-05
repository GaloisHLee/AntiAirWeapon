using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AntiAirWeapon.Buildings
{
	public class TurretTop_CustomSize
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00004B8C File Offset: 0x00002D8C
		// (set) Token: 0x0600005A RID: 90 RVA: 0x00004BA4 File Offset: 0x00002DA4
		public bool aimCanATK
		{
			get
			{
				return this.aimATK;
			}
			set
			{
				this.aimATK = value;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600005B RID: 91 RVA: 0x00004BB0 File Offset: 0x00002DB0
		// (set) Token: 0x0600005C RID: 92 RVA: 0x00004BC8 File Offset: 0x00002DC8
		public float CurRotation
		{
			get
			{
				return this.curRotationInt;
			}
			set
			{
				this.curRotationInt = value;
				bool flag = (double)this.curRotationInt > 360.0;
				if (flag)
				{
					this.curRotationInt -= 360f;
				}
				bool flag2 = (double)this.curRotationInt < 0.0;
				if (flag2)
				{
					this.curRotationInt += 360f;
				}
			}
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00004C31 File Offset: 0x00002E31
		public TurretTop_CustomSize(Building_AirDefense ParentTurret)
		{
			this.parentDefense = ParentTurret;
		}

		 

		// Token: 0x0600005F RID: 95 RVA: 0x00004C64 File Offset: 0x00002E64
		private void defense1Tick()
		{
			LocalTargetInfo nowTarget = this.parentDefense.nowTarget;
			bool flag = nowTarget != null;
			if (flag)
			{
				float num = this.CurRotation = Vector3Utility.AngleFlat(nowTarget.Cell.ToVector3Shifted() - this.parentDefense.DrawPos);
				this.ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);

			}
			else
			{
				bool flag6 = this.ticksUntilIdleTurn > 0;
				if (flag6)
				{
					this.ticksUntilIdleTurn--;
					bool flag7 = this.ticksUntilIdleTurn == 0;
					if (flag7)
					{
						bool flag8 = (double)Rand.Value < 0.5;
						if (flag8)
						{
							this.idleTurnClockwise = true;
						}
						else
						{
							this.idleTurnClockwise = false;
						}
						this.idleTurnTicksLeft = 140;
					}
				}
				else
				{
					bool flag9 = this.idleTurnClockwise;
					if (flag9)
					{
						this.CurRotation += 0.26f;
					}
					else
					{
						this.CurRotation -= 0.26f;
					}
					this.idleTurnTicksLeft--;
					bool flag10 = this.idleTurnTicksLeft <= 0;
					if (flag10)
					{
						this.ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
					}
				}
			}
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00004F10 File Offset: 0x00003110
	 
		// Token: 0x06000061 RID: 97 RVA: 0x00005060 File Offset: 0x00003260
		public void TurretTopTick()
		{
			bool flag = this.parentDefense != null  ;
			if (flag)
			{
				this.defense1Tick();
			}
			 
		}

		// Token: 0x06000062 RID: 98 RVA: 0x0000509C File Offset: 0x0000329C
		public void DrawTurret()
		{
			bool flag = this.parentDefense != null  ;
			Matrix4x4 matrix4x = default(Matrix4x4);
			matrix4x.SetTRS((  this.parentDefense.DrawPos  ) + Altitudes.AltIncVect, GenMath.ToQuat(this.CurRotation), (( this.parentDefense.TopSizeComp ) == null) ? Vector3.one : ( this.parentDefense.TopSizeComp.Props.topSize ));
			Graphics.DrawMesh(MeshPool.plane20, matrix4x, this.parentDefense.def.building.turretTopMat , 0);
		}

		// Token: 0x0400002A RID: 42
		private Building_AirDefense parentDefense;

		// Token: 0x0400002B RID: 43
		 

		// Token: 0x0400002C RID: 44
		private bool aimATK = false;

		// Token: 0x0400002D RID: 45
		private float curRotationInt;

		// Token: 0x0400002E RID: 46
		private int ticksUntilIdleTurn;

		// Token: 0x0400002F RID: 47
		private int idleTurnTicksLeft;

		// Token: 0x04000030 RID: 48
		private bool idleTurnClockwise;

		// Token: 0x04000031 RID: 49
		private const float IdleTurnDegreesPerTick = 0.26f;

		// Token: 0x04000032 RID: 50
		private const int IdleTurnDuration = 140;

		// Token: 0x04000033 RID: 51
		private const int IdleTurnIntervalMin = 150;

		// Token: 0x04000034 RID: 52
		private const int IdleTurnIntervalMax = 350;
	}
}
