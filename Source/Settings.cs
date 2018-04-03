using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Mining_Priority
{
	class Settings : ModSettings
	{
		public bool qualityMining = true;
		public bool priorityMining = true;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<Mining_Priority.Mod>().GetSettings<Settings>();
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.CheckboxLabeled("RestrictBest".Translate(), ref qualityMining, "RestrictBestDesc".Translate());
			options.CheckboxLabeled("MineValue".Translate(), ref priorityMining);
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref qualityMining, "qualityMining", true);
			Scribe_Values.Look(ref priorityMining, "priorityMining", true);
		}
	}
}