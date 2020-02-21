using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using HarmonyLib;
using Harmony.ILCopying;
using RimWorld;

namespace Mining_Priority
{
	public class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			GetSettings<Settings>();
#if DEBUG
			Harmony.DEBUG = true;
#endif
			Harmony harmony = new Harmony("Uuugggg.rimworld.Mining_Priority.main");
			
			//Turn off DefOf warning since harmony patches trigger it.
			MethodInfo DefOfHelperInfo = AccessTools.Method(typeof(DefOfHelper), "EnsureInitializedInCtor");
			if (!harmony.GetPatchedMethods().Contains(DefOfHelperInfo))
				harmony.Patch(DefOfHelperInfo, new HarmonyMethod(typeof(Mod), "EnsureInitializedInCtorPrefix"), null);
			
			harmony.PatchAll();
		}

		public static bool EnsureInitializedInCtorPrefix()
		{
			//No need to display this warning.
			return false;
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			GetSettings<Settings>().DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "TD.MiningPriority".Translate();
		}
	}
}