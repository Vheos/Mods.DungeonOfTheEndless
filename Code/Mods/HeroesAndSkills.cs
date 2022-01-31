namespace Vheos.Mods.DungeonOfTheEndless
{
    using System;
    using System.Collections.Generic;
    using HarmonyLib;
    using UnityEngine;
    using Mods.Core;
    using Tools.Extensions.Collections;
    using Tools.Extensions.General;
    using Amplitude.Unity.Framework;

    public class HeroesAndSkills : AMod
    {
        // Settings
        static private ModSetting<int> _maxHeroes;
        static private ModSetting<BoolOverride> _canRecruitHeroes;
        static private ModSetting<BoolOverride> _canUseHeal;
        static private ModSetting<BoolOverride> _autoHealAfterCombat;
        static private ModSetting<bool> _activateAllSelectedHeroesSkills;
        static private ModSetting<bool> _fixedActiveSkillSlots;
        static private ModSetting<PlayerCountModes> _dungeonDialogues;
        static private ModSetting<int> _dungeonDialoguesFrequencyMultiplier;
        static private ModSetting<PlayerCountModes> _liftDialogues;
        override protected void Initialize()
        {
            _maxHeroes = CreateSetting(nameof(_maxHeroes), 4, IntRange(1, 8));
            _canRecruitHeroes = CreateSetting(nameof(_canRecruitHeroes), BoolOverride.Original);
            _canUseHeal = CreateSetting(nameof(_canUseHeal), BoolOverride.Original);
            _autoHealAfterCombat = CreateSetting(nameof(_autoHealAfterCombat), BoolOverride.Original);

            _activateAllSelectedHeroesSkills = CreateSetting(nameof(_activateAllSelectedHeroesSkills), false);
            _fixedActiveSkillSlots = CreateSetting(nameof(_fixedActiveSkillSlots), false);

            _dungeonDialogues = CreateSetting(nameof(_dungeonDialogues), PlayerCountModes.Singleplayer);
            _liftDialogues = CreateSetting(nameof(_liftDialogues), PlayerCountModes.Singleplayer);
            _dungeonDialoguesFrequencyMultiplier = CreateSetting(nameof(_dungeonDialoguesFrequencyMultiplier), 100, IntRange(0, 500));
        }
        override protected void SetFormatting()
        {
            _maxHeroes.Format("Max heroes");
            _canRecruitHeroes.Format("Can recruit heroes");
            _canUseHeal.Format("Can use heal");
            _autoHealAfterCombat.Format("Auto heal after combat");

            _activateAllSelectedHeroesSkills.Format("Active all selected hereoes' skills");
            _fixedActiveSkillSlots.Format("Fixed active skill slots");

            _dungeonDialogues.Format("Dungeon dialogues");
            using (Indent)
                _dungeonDialoguesFrequencyMultiplier.Format("frequency multiplier", _dungeonDialogues, t => t != 0);
            _liftDialogues.Format("Lift dialogues");
        }
        protected override void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_Rebalance):
                    ForceApply();
                    _maxHeroes.Value = 4;
                    _canRecruitHeroes.Value = BoolOverride.Original;
                    _canUseHeal.Value = BoolOverride.Original;
                    _autoHealAfterCombat.Value = BoolOverride.False;

                    _activateAllSelectedHeroesSkills.Value = true;
                    _fixedActiveSkillSlots.Value = true;

                    _dungeonDialogues.Value = (PlayerCountModes)~0;
                    _dungeonDialoguesFrequencyMultiplier.Value = 200;
                    _liftDialogues.Value = (PlayerCountModes)~0;
                    break;
            }
        }

        // Privates
        static private bool _oneShotFalse_IsMultiplayerSession;

        // Defines
        [Flags]
        private enum PlayerCountModes
        {
            Singleplayer = 1 << 0,
            Multiplayer = 1 << 1,
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Max heroes
        [HarmonyPatch(typeof(GameConfig), nameof(GameConfig.MaxHeroCount), MethodType.Getter), HarmonyPostfix]
        static private void GameConfig_MaxHeroCount_Post(GameConfig __instance, ref int __result)
        => __result = _maxHeroes;

        [HarmonyPatch(typeof(Hero), nameof(Hero.GetLevelWinningHeroes)), HarmonyPostfix]
        static private void Hero_GetLevelWinningHeroes_Post(Hero __instance, ref List<Hero> __result)
        {
            if (_maxHeroes > 4
            && __result != null
            && __result.Count > 4)
                __result.RemoveRange(4, __result.Count - 4);
        }

        // Can recruit heroes
        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.SpawnHero)), HarmonyPrefix]
        static private void Dungeon_SpawnHero_Pre(Dungeon __instance, ref bool displayRecruitmentDialog, ref bool recruitable)
        {
            displayRecruitmentDialog.ApplyBoolOverride(_canRecruitHeroes);
            recruitable.ApplyBoolOverride(_canRecruitHeroes);
        }

        // Can use heal
        [HarmonyPatch(typeof(ShipConfig), nameof(ShipConfig.ForbidHeal), MethodType.Getter), HarmonyPostfix]
        static private void ShipConfig_ForbidHeal_Getter_Post(ShipConfig __instance, ref bool __result)
        => __result.ApplyBoolOverride(_canUseHeal, true);

        // Heal after combat
        [HarmonyPatch(typeof(ShipConfig), nameof(ShipConfig.ForbidStrategyHealthRegen), MethodType.Getter), HarmonyPostfix]
        static private void ShipConfig_ForbidStrategyHealthRegen_Getter_Post(ShipConfig __instance, ref bool __result)
        => __result.ApplyBoolOverride(_autoHealAfterCombat, true);

        [HarmonyPatch(typeof(Health), nameof(Health.ShouldInstantRegen)), HarmonyPostfix]
        static private void Health_ShouldInstantRegen_Post(Health __instance, ref bool __result)
        {
            if (__instance.module != null)
                return;

            __result.ApplyBoolOverride(_autoHealAfterCombat);
        }

        [HarmonyPatch(typeof(Health), nameof(Health.HealthRegenUpdate)), HarmonyPrefix]
        static private bool Health_HealthRegenUpdate_Pre(Health __instance)
        => __instance.module != null
        || _autoHealAfterCombat != BoolOverride.False
        || __instance.dungeon.CurrentGamePhase != GamePhase.Strategy;

        // Activate all selected heroes' skills
        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.CheckInputs)), HarmonyPostfix]
        static private void Dungeon_CheckInputs_Post(Dungeon __instance)
        {
            if (!_activateAllSelectedHeroesSkills
            || !Hero.SelectedHeroes.TryNonNull(out var selectedHeroes)
            || selectedHeroes.Count <= 1
            || !Amplitude.Unity.Framework.Application.HasFocus
            || __instance.IsLevelOver
            || __instance.gameMenuPanel.IsVisible
            || !__instance.selectableManager.GetCurrentCategoryConfig().EnableNonContextualControl
            || !__instance.IsDisplayed
            || __instance.inputManager.CurrentControlScheme == ControlScheme.MouseAndKeyboard
                && !__instance.inputManager.KeyboardShortcutEnabled)
                return;

            if (__instance.inputManager.GetControlDown(Control.ActiveSkill1))
                foreach (var hero in Hero.SelectedHeroes)
                    hero.ActivateActiveSkill(0);

            if (__instance.inputManager.GetControlDown(Control.ActiveSkill2))
                foreach (var hero in Hero.SelectedHeroes)
                    hero.ActivateActiveSkill(1);
        }

        // Fixed active skill slots
        [HarmonyPatch(typeof(Hero), nameof(Hero.FilterActiveSkills)), HarmonyPrefix]
        static private bool Hero_FilterActiveSkills_Pre(Hero __instance)
        {
            if (!_fixedActiveSkillSlots)
                return true;

            __instance.FilteredActiveSkills = new List<ActiveSkill>();
            foreach (var skill in __instance.activeSkills)
            {
                for (int j = 0; j < __instance.FilteredActiveSkills.Count; j++)
                {
                    ActiveSkill filteredSkill = __instance.FilteredActiveSkills[j];
                    if (filteredSkill.Config.BaseName == skill.Config.BaseName
                    && skill.Config.Level > filteredSkill.Config.Level)
                    {
                        __instance.FilteredActiveSkills.Insert(j, skill);
                        __instance.FilteredActiveSkills.Remove(filteredSkill);
                        break;
                    }
                }
                __instance.FilteredActiveSkills.TryAddUnique(skill);
            }
            return false;
        }

        // Dungeon dialogues
        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.CheckSituationDialog)), HarmonyPrefix]
        static private bool Dungeon_CheckSituationDialog_Pre(Dungeon __instance, SituationDialogType situation, Hero speaker)
        {
            var playerCountMode = __instance.gameNetManager != null && __instance.gameNetManager.IsMultiplayerSession()
                                ? PlayerCountModes.Multiplayer : PlayerCountModes.Singleplayer;
            if (!_dungeonDialogues.Value.HasFlag(playerCountMode)
            || !Databases.GetDatabase<DialogueSituationConfig>(false).GetValue(situation.ToString())
                .TryNonNull(out var situationDialogue))
                return false;

            var previousProbability = situationDialogue.Probabilty;
            situationDialogue.Probabilty *= _dungeonDialoguesFrequencyMultiplier / 100f;
            situationDialogue.CheckSituationDialog(speaker, __instance.heroDialogueDuration);
            situationDialogue.Probabilty = previousProbability;
            return false;
        }

        // Lift dialogues
        [HarmonyPatch(typeof(StoryDialogManager), nameof(StoryDialogManager.OnEnable)), HarmonyPostfix]
        static private void StoryDialogManager_OnEnable_Post(StoryDialogManager __instance)
        {
            var sessionMode = SingletonManager.Get<GameNetworkManager>(true).sessionManager.Session.SessionMode;
            if (_liftDialogues.Value.HasFlag(PlayerCountModes.Multiplayer)
            && sessionMode != SessionMode.Single)
                __instance.heroesInLift = Hero.GetLevelWinningHeroes();
        }

        [HarmonyPatch(typeof(Lift), nameof(Lift.Show)), HarmonyPrefix]
        static private void Lift_Show_Pre(Lift __instance)
        {
            var sessionMode = SingletonManager.Get<GameNetworkManager>(true).sessionManager.Session.SessionMode;
            _oneShotFalse_IsMultiplayerSession = _liftDialogues.Value.HasFlag(PlayerCountModes.Singleplayer)
                                              && sessionMode == SessionMode.Single
                                              || _liftDialogues.Value.HasFlag(PlayerCountModes.Multiplayer)
                                              && sessionMode != SessionMode.Single;
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.IsMultiplayerSession)), HarmonyPostfix]
        static private void GameNetworkManager_IsMultiplayerSession_Post(GameNetworkManager __instance, ref bool __result)
        {
            if (!_oneShotFalse_IsMultiplayerSession)
                return;

            _oneShotFalse_IsMultiplayerSession = false;
            __result = false;
        }
    }
}