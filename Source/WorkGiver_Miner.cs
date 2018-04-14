using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Harmony;
using RimWorld;
using System.Reflection;
using UnityEngine;

namespace Mining_Priority
{ 
	// ACTUALLY WorkGiver_Scanner
	[HarmonyPatch(typeof(WorkGiver_Miner), "GetPriority", new Type[] { typeof(Pawn), typeof(TargetInfo) })]
	//public virtual float GetPriority(Pawn pawn, TargetInfo t)
	public static class WorkGiver_Miner_GetPriority_Patch
	{
		public static void Postfix(WorkGiver_Miner __instance, ref float __result, Pawn pawn, TargetInfo t)
		{
			if (!(__instance is WorkGiver_Miner) || !t.HasThing)
				return;

			BuildingProperties building = t.Thing.def.building;
			if (building == null) return;

			Log.Message(pawn + " mining " + t.Thing + " commonality " + building.mineableScatterCommonality + " size = " + building.mineableScatterLumpSizeRange.Average);
			float p = building.mineableScatterCommonality + building.mineableScatterLumpSizeRange.Average / 10000f;
			__result = building.mineableScatterCommonality == 0 ? float.MinValue : -p;
			Log.Message("Miner priority for " + t.Thing + " is " + __result);
		}
	}

	// ACTUALLY WorkGiver_Scanner
	//public override bool Prioritized
	[HarmonyPatch(typeof(WorkGiver_Miner))]
	[HarmonyPatch("Prioritized", PropertyMethod.Getter)]
	public static class WorkGiver_Miner_Prioritized_Patch
	{
		public static void Postfix(WorkGiver_Miner __instance, ref bool __result)
		{
			if (__instance is WorkGiver_Miner)
			{
				__result |= Settings.Get().priorityMining;
			}
		}
	}


	//public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	[HarmonyPatch(typeof(WorkGiver_Miner), "PotentialWorkThingsGlobal")]
	public static class WorkGiver_Miner_Potential_Patch
	{
		public static void Postfix(WorkGiver_Miner __instance, Pawn pawn, ref IEnumerable<Thing> __result)
		{
			if (!Settings.Get().qualityMining) return;

			Func<Pawn, bool> validatePawn = p => p == pawn || (
			  p.workSettings.WorkIsActive(WorkTypeDefOf.Mining)
			  && (!Settings.Get().qualityMiningIgnoreBusy || p.CurJob.def == JobDefOf.Mine));

			float bestMiningYield = pawn.Map.mapPawns.FreeColonists.Where(validatePawn).Select(p => p.GetStatValue(StatDefOf.MiningYield)).Max();

			bestMiningYield *= Settings.Get().qualityGoodEnough;

			bool bestMiner = pawn.GetStatValue(StatDefOf.MiningYield) >= bestMiningYield;
			Log.Message(pawn + " is the " + (bestMiner ? "best miner!" : "worst"));
			if (!bestMiner)
			{
				Log.Message(" Results were " + __result.ToStringSafeEnumerable());
				__result = __result.Where(t => !t.def.building?.mineableYieldWasteable ?? true);
				Log.Message(" Results are " + __result.ToStringSafeEnumerable());
			}
		}
	}
}