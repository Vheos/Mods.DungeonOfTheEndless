namespace Vheos.Mods.DungeonOfTheEndless
{
    using System;
    using HarmonyLib;
    using UnityEngine;
    using Mods.Core;
    using Tools.Extensions.Math;
    using Tools.Extensions.General;

    public class Cheats : AMod
    {
        // Settings
        static private ModSetting<bool> _antiTamper;
        static private ModSetting<string> _heroPoolSwitchLock;
        static private ModSetting<bool> _heroPoolShowSecret;
        static private ModSetting<bool> _heroPoolSave;
        static private ModSetting<bool> _unlockAllShips;
        static private ModSetting<bool> _timedEventsOverride;
        static private ModSetting<TimedEvents> _timedEvents;
        static private ModSetting<RoomEvent> _roomEventOverride;
        static private ModSetting<int> _industry;
        static private ModSetting<int> _science;
        static private ModSetting<int> _food;
        static private ModSetting<int> _dust;
        override protected void Initialize()
        {
            _antiTamper = CreateSetting(nameof(_antiTamper), true);
            _heroPoolSwitchLock = CreateSetting(nameof(_heroPoolSwitchLock), "");
            _heroPoolShowSecret = CreateSetting(nameof(_heroPoolShowSecret), false);
            _heroPoolSave = CreateSetting(nameof(_heroPoolSave), false);
            _unlockAllShips = CreateSetting(nameof(_unlockAllShips), false);

            _timedEventsOverride = CreateSetting(nameof(_timedEventsOverride), false);
            _timedEvents = CreateSetting(nameof(_timedEvents), (TimedEvents)0);

            _roomEventOverride = CreateSetting(nameof(_roomEventOverride), RoomEvent.None);

            _industry = CreateSetting(nameof(_industry), 0, IntRange(0, 500));
            _science = CreateSetting(nameof(_science), 0, IntRange(0, 500));
            _food = CreateSetting(nameof(_food), 0, IntRange(0, 500));
            _dust = CreateSetting(nameof(_dust), 0, IntRange(0, 500));

            // Events
            AddEventOnConfigOpened(TryReadResources);
            _industry.AddEventSilently(TrySetIndustry);
            _science.AddEventSilently(TrySetScience);
            _food.AddEventSilently(TrySetFood);
            _dust.AddEventSilently(TrySetDust);
        }
        override protected void SetFormatting()
        {
            _antiTamper.Format("Anti-tamper");

            _heroPoolSwitchLock.Format("Switch hero lock");
            using (Indent)
            {
                _heroPoolShowSecret.Format("Show secret heroes");
                _heroPoolSave.Format("save hero pool", _heroPoolSwitchLock, t => t.IsValidKeyCode());
            }

            _unlockAllShips.Format("Unlock all ships");

            _timedEventsOverride.Format("Override timed events");
            using (Indent)
                _timedEvents.Format("", _timedEventsOverride);

            _roomEventOverride.Format("Room event override");

            CreateHeader("Resources");
            using (Indent)
            {
                _industry.Format("Industry");
                _science.Format("Science");
                _food.Format("Food");
                _dust.Format("Dust");
            }
        }
        protected override void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_Rebalance):
                    ForceApply();
                    _antiTamper.Value = false;
                    _heroPoolSwitchLock.Value = "LeftShift";
                    _heroPoolShowSecret.Value = true;
                    _heroPoolSave.Value = true;
                    _unlockAllShips.Value = true;
                    _timedEventsOverride.Value = true;
                    _timedEvents.Value = 0;
                    _roomEventOverride.Value = RoomEvent.None;
                    break;
            }
        }

        // Privates
        static private void TryReadResources()
        {
            if (!Player.LocalPlayer.TryNonNull(out var localPlayer)
            || !localPlayer.dungeon.TryNonNull(out var dungeon))
                return;

            _industry.SetSilently(localPlayer.IndustryStock.Round());
            _science.SetSilently(localPlayer.ScienceStock.Round());
            _food.SetSilently(localPlayer.FoodStock.Round());
            _dust.SetSilently(dungeon.DustStock.Round());
        }
        static private void TrySetIndustry()
        {
            if (Player.LocalPlayer.TryNonNull(out var localPlayer))
                localPlayer.IndustryStock = _industry;
        }
        static private void TrySetScience()
        {
            if (Player.LocalPlayer.TryNonNull(out var localPlayer))
                localPlayer.ScienceStock = _science;
        }
        static private void TrySetFood()
        {
            if (Player.LocalPlayer.TryNonNull(out var localPlayer))
                localPlayer.FoodStock = _food;
        }
        static private void TrySetDust()
        {
            if (Player.LocalPlayer.TryNonNull(out var localPlayer)
            && localPlayer.dungeon.TryNonNull(out var dungeon))
                dungeon.DustStock = _dust;
        }

        // Defines
        [Flags]
        private enum TimedEvents
        {
            EndlessDay = 1 << 0,
            FreeWeekend = 1 << 1,
            Halloween = 1 << 2,
        }

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Anti-tamper
        [HarmonyPatch(typeof(HashManager), nameof(HashManager.RuntimeService_RuntimeChange)), HarmonyPostfix]
        static private void HashManager_RuntimeService_RuntimeChange_Post(HashManager __instance)
        {
            if (_antiTamper)
                return;

            HashManager.IsHashDifferent = false;
        }

        // Hero switch lock
        [HarmonyPatch(typeof(HeroSelectionItem), nameof(HeroSelectionItem.OnLeftClick)), HarmonyPrefix]
        static private bool HeroSelectionItem_OnLeftClick_Pre(HeroSelectionItem __instance)
        {
            if (!_heroPoolSwitchLock.IsValidKeyCode()
            || !Input.GetKey(_heroPoolSwitchLock.ToKeyCode()))
                return true;

            var heroStats = __instance.HeroStats;
            heroStats.Status = heroStats.Status switch
            {
                HeroStatus.Unknown => HeroStatus.Discovered,
                HeroStatus.Discovered => HeroStatus.Unlocked,
                HeroStatus.Unlocked => HeroStatus.Unknown,
                _ => HeroStatus.Unknown,
            };
            __instance.HeroStats = heroStats;
            __instance.RefreshPortrait();
            __instance.RefreshTooltip();

            int heroSaveDataIndex = UserProfile.Data.HeroesGameStats.FindIndex(t => t.ConfigName == __instance.heroConfig.Name);
            UserProfile.Data.HeroesGameStats[heroSaveDataIndex] = heroStats;
            return false;
        }

        [HarmonyPatch(typeof(HeroConfig), nameof(HeroConfig.IsHidden)), HarmonyPostfix]
        static private void HeroConfig_IsHidden_Post(HeroConfig __instance, ref bool __result)
        {
            if (!_heroPoolSwitchLock.IsValidKeyCode()
            || !_heroPoolShowSecret)
                return;

            __result = false;
        }

        [HarmonyPatch(typeof(HeroConfig), nameof(HeroConfig.IsCommunityEventHero)), HarmonyPostfix]
        static private void HeroConfig_IsCommunityEventHero_Post(HeroConfig __instance, ref bool __result)
        {
            if (!_heroPoolSwitchLock.IsValidKeyCode()
            || !_heroPoolShowSecret)
                return;

            __result = false;
        }

        [HarmonyPatch(typeof(GameSelectionPanel), nameof(GameSelectionPanel.QuitLobby)), HarmonyPrefix]
        static private void GameSelectionPanel_QuitLobby_Pre(GameSelectionPanel __instance)
        {
            if (!_heroPoolSwitchLock.IsValidKeyCode()
            || !_heroPoolSave)
                return;

            UserProfile.SaveToFile();
        }

        [HarmonyPatch(typeof(GameSelectionPanel), nameof(GameSelectionPanel.StartGame)), HarmonyPrefix]
        static private void GameSelectionPanel_StartGame_Pre(GameSelectionPanel __instance)
        {
            if (!_heroPoolSwitchLock.IsValidKeyCode()
            || !_heroPoolSave)
                return;

            UserProfile.SaveToFile();
        }

        // Ships
        [HarmonyPatch(typeof(GameSelectionPanel), nameof(GameSelectionPanel.IsSelectedShipLocked)), HarmonyPostfix]
        static private void GameSelectionPanel_IsSelectedShipLocked_Post(GameSelectionPanel __instance, ref bool __result)
        {
            if (!_unlockAllShips)
                return;

            __result = false;
        }

        // Timed events
        [HarmonyPatch(typeof(PrivateGameConfigManager), nameof(PrivateGameConfigManager.IsCommunityEventActive)), HarmonyPrefix]
        static private bool PrivateGameConfigManager_IsCommunityEventActive_Pre(PrivateGameConfigManager __instance, ref bool __result, CommunityEvent commEvt)
        {
            if (!_timedEventsOverride)
                return true;

            switch (commEvt)
            {
                case CommunityEvent.EndlessDay: __result = _timedEvents.Value.HasFlag(TimedEvents.EndlessDay); break;
                case CommunityEvent.FreeWeekend: __result = _timedEvents.Value.HasFlag(TimedEvents.FreeWeekend); break;
                case CommunityEvent.Halloween: __result = _timedEvents.Value.HasFlag(TimedEvents.Halloween); break;
            }
            return false;
        }

        // Room events
        [HarmonyPatch(typeof(DynamicRoomEventConfig), nameof(DynamicRoomEventConfig.GetProbWeightValue)), HarmonyPostfix]
        static private void DynamicRoomEventConfig_GetProbWeightValue_Post(DynamicRoomEventConfig __instance, ref float __result)
        {
            if (_roomEventOverride == RoomEvent.None
            || __instance.Name != _roomEventOverride.Value.ToString())
                return;

            __result = float.PositiveInfinity;
        }
    }
}





/* ZOOM



[HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.ZoomIn)), HarmonyPrefix]
static private void GameCameraManager_ZoomIn_Pre(GameCameraManager __instance)
{
    if (!_zoomWithMouseWheel)
        return;

    __instance.zoomInScale = _zoomIn;
    __instance.zoomInSpeed = _zoomSpeed;
}

[HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.Unzoom)), HarmonyPrefix]
static private void GameCameraManager_Unzoom_Pre(GameCameraManager __instance)
{
    if (!_zoomWithMouseWheel)
        return;

    __instance.defaultScale = _defaultZoom;
    __instance.zoomSpeed = _zoomSpeed;
}

[HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.ZoomOut)), HarmonyPrefix]
static private void GameCameraManager_ZoomOut_Pre(GameCameraManager __instance)
{
    if (!_zoomWithMouseWheel)
        return;

    __instance.zoomOutScale = _zoomOut;
}

[HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.ZoomOut)), HarmonyPostfix]
static private void GameCameraManager_ZoomOut_Post(GameCameraManager __instance)
{
    if (!_zoomWithMouseWheel)
        return;

    __instance.tacticalMapCamera.enabled = false;
    __instance.zoomSpeed = _zoomSpeed;
}

[HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.Update)), HarmonyPrefix]
static private void GameCameraManager_Update_Pre(GameCameraManager __instance)
{
    if (!_zoomWithMouseWheel
    || __instance.IsTacticalMapActive())
        return;

    float mouseWheel = Input.GetAxis("MouseWheel");
    switch (__instance.zoom)
    {
        case GameCameraZoom.Default when mouseWheel > 0: __instance.ZoomIn(); break;
        case GameCameraZoom.ZoomedOut when mouseWheel > 0: __instance.Unzoom(); break;
        case GameCameraZoom.ZoomedIn when mouseWheel < 0: __instance.Unzoom(); break;
        case GameCameraZoom.Default when mouseWheel < 0: __instance.ZoomOut(); break;
        //case GameCameraZoom.ZoomedOut when mouseWheel < 0: InvokeOriginalSwitchToTacticalMapCamera(__instance); break;
    }
}

[HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.OnMove)), HarmonyPrefix]
static private bool GameCameraManager_OnMove_Pre(GameCameraManager __instance)
=> !_zoomWithMouseWheel;
*/