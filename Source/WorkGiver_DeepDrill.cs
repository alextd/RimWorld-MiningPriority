using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Harmony;
using RimWorld;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Mining_Priority
{
	// Would like to patch WorkGiver_DeepDrill but there is no override so:
	// ACTUALLY WorkGiver_Scanner
	[HarmonyPatch(typeof(WorkGiver_Scanner), "GetPriority", new Type[] { typeof(Pawn), typeof(TargetInfo) })]
	//public virtual float GetPriority(Pawn pawn, TargetInfo t)
	public static class WorkGiver_DeepDrill_GetPriority_Patch
	{
		public static void Postfix(WorkGiver_Scanner __instance, ref float __result, Pawn pawn, TargetInfo t)
		{
			if (!(__instance is WorkGiver_DeepDrill) || !t.HasThing)
				return;

			IntVec3 drillPos = t.Thing.Position;
			Map map = pawn.Map;
			ThingDef def = DeepDrillUtility.GetNextResource(drillPos, map);
			if (def == null) return;
			
			float p = WorkGiver_Miner_GetPriority_Patch.Priority(def.deepCommonality, def.deepLumpSizeRange);
			
			if (Settings.Get().finishUpDrills)
			{
				int count = 0;
				for (int i = 0; i < 9; i++)
				{
					IntVec3 deepPos = drillPos + GenRadial.RadialPattern[i];
					if (deepPos.InBounds(map))
					{
						ThingDef thingDef = map.deepResourceGrid.ThingDefAt(deepPos);
						if (thingDef != null)
							count += map.deepResourceGrid.CountAt(deepPos);
					}
				}
				p -= count / 1000000f;//Less is more!
			}

			__result = p;
			Log.Message($"DeepDrill priority for {t.Thing} is {__result}");
		}
	}

	[HarmonyPatch(typeof(WorkGiver_DeepDrill), "HasJobOnThing")]
	public static class WorkGiver_DeepDrill_JobOnThing_Patch
	{ 
		//public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)	{
		public static bool Prefix(ref bool __result, Pawn pawn, Thing t, bool forced = false)
		{
			if (!Settings.Get().qualityMining || forced) return true;

			CompDeepDrill comp = t.TryGetComp<CompDeepDrill>();
			if (!comp?.ValuableResourcesPresent() ?? false) return true;

			if (!WorkGiver_Miner_JobOnThing_Patch.IsGoodMiner(pawn, typeof(WorkGiver_DeepDrill)))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
}