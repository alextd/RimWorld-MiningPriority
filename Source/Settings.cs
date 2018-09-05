using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using TD.Utilities;

namespace Mining_Priority
{
	class Settings : ModSettings
	{
		public bool qualityMining = true;
		public bool qualityMiningIgnoreBusy = false;

		public bool priorityMining = true;
		public bool continueWork = true;
		public float qualityGoodEnough = 1.0f;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<Mining_Priority.Mod>().GetSettings<Settings>();
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.CheckboxLabeled("TD.MineValue".Translate(), ref priorityMining);
			options.CheckboxLabeled("Prefer partially mined targets", ref continueWork);
			options.Gap();

			options.CheckboxLabeled("TD.RestrictBest".Translate(), ref qualityMining, "TD.RestrictBestDesc".Translate());
			if (qualityMining)
			{
				options.CheckboxLabeled("TD.SettingIgnoreBusy".Translate(), ref qualityMiningIgnoreBusy, "TD.SettingIgnoreBusyDesc".Translate());
				options.SliderLabeled("TD.SettingMinerGoodEnough".Translate(), ref qualityGoodEnough, "{0:P0}", 0, 1, "TD.SettingMinerGoodEnoughDesc".Translate());
			}
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref qualityMining, "qualityMining", true);
			Scribe_Values.Look(ref qualityMiningIgnoreBusy, "qualityMiningIgnoreBusy", false);
			Scribe_Values.Look(ref priorityMining, "priorityMining", true);
			Scribe_Values.Look(ref continueWork, "continueWork", true);
			Scribe_Values.Look(ref qualityGoodEnough, "priorityGoodEnough", 1.0f);
		}
	}
}