﻿using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld;

namespace Mining_Priority
{
	public class Mod : Verse.Mod
	{
		public static Settings settings;
		public Mod(ModContentPack content) : base(content)
		{
			// initialize settings
			settings = GetSettings<Settings>();
#if DEBUG
			Harmony.DEBUG = true;
#endif
			Harmony harmony = new Harmony("Uuugggg.rimworld.Mining_Priority.main");
			
			harmony.PatchAll();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "TD.MiningPriority".Translate();
		}
	}
}