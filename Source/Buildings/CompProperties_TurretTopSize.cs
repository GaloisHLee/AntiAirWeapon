using UnityEngine;
using Verse;

namespace AntiAirWeapon.Buildings
{
	public class CompProperties_TurretTopSize : CompProperties
	{
		// Token: 0x06000046 RID: 70 RVA: 0x00004640 File Offset: 0x00002840
		public CompProperties_TurretTopSize()
		{
			this.compClass = typeof(CompTurretTopSize);
		}

		// Token: 0x0400001A RID: 26
		public Vector3 topSize = Vector3.one;

		// Token: 0x0400001B RID: 27
		public string soundShoot = null;

		// Token: 0x0400001C RID: 28
		public int maxStoreBullet = 1;

		// Token: 0x0400001D RID: 29
		public int storeTick = 100;

		// Token: 0x0400001E RID: 30
		public bool canShotProjectile = false;

		// Token: 0x0400001F RID: 31
		public bool canShotPod = false;

		// Token: 0x04000020 RID: 32
		public int atkTicks = 60;

		// Token: 0x04000021 RID: 33
		public int atkRange = 10;

		// Token: 0x04000022 RID: 34
		public int aimRange = 5;
	}
}