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
	// Would like to patch WorkGiver_Miner but there is no override so:
	// ACTUALLY WorkGiver_Scanner
	[HarmonyPatch(typeof(WorkGiver_Scanner), "GetPriority", new Type[] { typeof(Pawn), typeof(TargetInfo) })]
	//public virtual float GetPriority(Pawn pawn, TargetInfo t)
	public static class WorkGiver_Miner_GetPriority_Patch
	{
		public static float Priority(float commonality, IntRange sizeRange)
		{
			if (Settings.Get().priorityMining)
				return (commonality == 0) ? -5 : -commonality + sizeRange.Average / 10000f;
			else
				return 0f;
		}

		public static void Postfix(WorkGiver_Scanner __instance, ref float __result, Pawn pawn, TargetInfo t)
		{
			if (!(__instance is WorkGiver_Miner) || !t.HasThing)
				return;

			BuildingProperties building = t.Thing.def.building;
			if (building == null) return;

			float p = Priority(building.mineableScatterCommonality, building.mineableScatterLumpSizeRange);

			if (Settings.Get().continueWork)
			{
				float damage = t.Thing.MaxHitPoints - t.Thing.HitPoints;
				p += damage / 1000000f;
			}

			__result = p;
			Log.Message($"Miner priority for {t.Thing} is {__result}");
		}
	}

	/*
	// ACTUALLY WorkGiver_Scanner
	//public override bool Prioritized
	[HarmonyPatch(typeof(WorkGiver_Scanner))]
	[HarmonyPatch("Prioritized", PropertyMethod.Getter)]
	public static class WorkGiver_Miner_Prioritized_Patch
	{
		public static void Postfix(WorkGiver_Scanner __instance, ref bool __result)
		{
			if (__instance is WorkGiver_Miner)
			{
				__result |= Settings.Get().priorityMining || Settings.Get().continueWork;
			}
		}
	}
	*/

	//Mac has a problem with above patch AND I HAVE NO IDEA WHY. Even a transpiler instead.
	//just __result = true fails, but other Property patches e.g. Pawn.Downed aren't a problem.
	//So instead I'm transpiling the call to Prioritized ~ just two exist in the same function 
	// ~ and this will be a problem if there are more calls to Prioritized added.
	//public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
	[HarmonyPatch(typeof(JobGiver_Work), "TryIssueJobPackage")]
	public static class Prioritized_Patch
	{
		//IL_0206: ldloc.s packageCAnonStorey1
		//IL_0208: ldfld class RimWorld.WorkGiver_Scanner RimWorld.JobGiver_Work/'<TryIssueJobPackage>c__AnonStorey1'::scanner
		//IL_020d: callvirt instance bool RimWorld.WorkGiver_Scanner::get_Prioritized()
		//IL_0212: brfalse IL_030a
		//But the WorkGiver_Scanner there is actually just the second local var WorkGiver

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase mb)
		{
			MethodInfo PrioritizedInfo = AccessTools.Property(typeof(WorkGiver_Scanner), nameof(WorkGiver_Scanner.Prioritized)).GetGetMethod();

			MethodInfo PostfixInfo = AccessTools.Method(typeof(Prioritized_Patch), "TranspilerPostfix");

			int scannerIndex = mb.GetMethodBody().LocalVariables.Last(lv => lv.LocalType == typeof(WorkGiver)).LocalIndex;

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (i.opcode == OpCodes.Callvirt && i.operand == PrioritizedInfo)
				{
					yield return new CodeInstruction(OpCodes.Ldloc_S, scannerIndex);//WorkGiver
					yield return new CodeInstruction(OpCodes.Call, PostfixInfo);
				}
			}
		}

		public static bool TranspilerPostfix(bool result, WorkGiver instance)
		{
			if (instance is WorkGiver_Miner)
			{
				result |= Settings.Get().priorityMining;
			}
			if (instance is WorkGiver_DeepDrill)
			{
				result |= Settings.Get().continueWork;
			}
			return result;
		}
	}

	//public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	[HarmonyPatch(typeof(WorkGiver_Miner), "JobOnThing")]
	public static class WorkGiver_Miner_JobOnThing_Patch
	{
		public static bool IsGoodMiner(Pawn pawn)
		{
			Func<Pawn, bool> validatePawn = p => p == pawn || (
				(p.workSettings?.WorkIsActive(WorkTypeDefOf.Mining) ?? false)
				&& (!Settings.Get().qualityMiningIgnoreBusy || p.CurJob?.def == JobDefOf.Mine || p.CurJob?.def == JobDefOf.OperateDeepDrill));

			//TODO: save value instead of computing each JobOnThing
			float bestMiningYield = pawn.Map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(validatePawn).Select(p => p.GetStatValue(StatDefOf.MiningYield)).Max();

			bestMiningYield *= Settings.Get().qualityGoodEnough;

			bool bestMiner = pawn.GetStatValue(StatDefOf.MiningYield) >= bestMiningYield;
			Log.Message($"{pawn} is the best : {bestMiner}");
			if (!bestMiner)
			{
				JobFailReason.Is("TD.JobFailReasonNotBestMiner".Translate());
				return false;
			}
			return true;
		}

		public static bool Prefix(ref Job __result, Pawn pawn, Thing t, bool forced = false)
		{
			if (!Settings.Get().qualityMining || forced) return true;

			BuildingProperties building = t.def.building;
			if (building == null || !building.mineableYieldWasteable) return true;

			if (!IsGoodMiner(pawn))
			{
				__result = null;
				return false;
			}
			return true;
		}
	}
}