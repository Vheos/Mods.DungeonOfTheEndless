namespace Vheos.Mods.DungeonOfTheEndless
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using HarmonyLib;
    using UnityEngine;
    using Amplitude.Unity.Framework;
    using Mods.Core;
    using Tools.Extensions.Math;
    using Tools.Extensions.Collections;
    using Tools.Extensions.General;

    public class Various : AMod
    {
        // Settings
        static private ModSetting<int> _strategyPhaseSpeed;
        static private ModSetting<int> _actionPhaseSpeed;
        static private ModSetting<bool> _heroPoolRandomizeSlots;
        static private ModSetting<int> _heroPoolRandomizeSlotsSeed;

        override protected void Initialize()
        {
            _strategyPhaseSpeed = CreateSetting(nameof(_strategyPhaseSpeed), 100, IntRange(50, 200));
            _actionPhaseSpeed = CreateSetting(nameof(_actionPhaseSpeed), 100, IntRange(50, 200));

            _heroPoolRandomizeSlots = CreateSetting(nameof(_heroPoolRandomizeSlots), false);
            _heroPoolRandomizeSlotsSeed = CreateSetting(nameof(_heroPoolRandomizeSlotsSeed), 0, IntRange(0, int.MaxValue));
        }
        override protected void SetFormatting()
        {
            CreateHeader("Engine speed");
            using (Indent)
            {
                _strategyPhaseSpeed.Format("strategy phase");
                _actionPhaseSpeed.Format("action phase");
            }

            _heroPoolRandomizeSlots.Format("Randomize hero slots");
            using (Indent)
                _heroPoolRandomizeSlotsSeed.Format("seed", _heroPoolRandomizeSlots);
        }
        protected override void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_Rebalance):
                    ForceApply();
                    _strategyPhaseSpeed.Value = 150;
                    _actionPhaseSpeed.Value = 50;

                    _heroPoolRandomizeSlots.Value = true;
                    _heroPoolRandomizeSlotsSeed.Value = 15;
                    break;
            }
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Speed
        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.UpdateGamePhase)), HarmonyPostfix]
        static private void Dungeon_UpdateGamePhase_Post(Dungeon __instance)
        {
            int phaseSpeed = Mob.ActiveMobs.Count <= 0 || __instance.CurrentCrystalState == CrystalState.PluggedOnExitSlot
                           ? _strategyPhaseSpeed : _actionPhaseSpeed;
            Time.timeScale = phaseSpeed / 100f;
        }

        // Randomize hero pool
        [HarmonyPatch(typeof(UserProfile), nameof(UserProfile.GetSelectableHeroes)), HarmonyPostfix]
        static private void UserProfile_GetSelectableHeroes_Post(UserProfile __instance, ref HeroGameStatsData[] __result)
        {
            if (!_heroPoolRandomizeSlots)
                return;

            if (_heroPoolRandomizeSlotsSeed > 0)
                __result.Shuffle(_heroPoolRandomizeSlotsSeed);
            else
                __result.Shuffle();
        }
    }
}