namespace Vheos.Mods.DungeonOfTheEndless
{
    using System;
    using System.Linq;
    using HarmonyLib;
    using UnityEngine;
    using Mods.Core;

    public class RoomsAndModules : AMod
    {
        // Settings
        static private ModSetting<BoolOverride> _canUnpowerRooms;
        static private ModSetting<bool> _unpowerStartRoomOnCrystalUnplug;
        static private ModSetting<float> _autoUnpowerDelayMin;
        static private ModSetting<float> _autoUnpowerDelayMax;
        static private ModSetting<int> _maxSameModulesPerRoom;
        static private ModSetting<bool> _twoLevelResearchUpgrades;
        static private ModSetting<bool> _canResetResearch;
        override protected void Initialize()
        {
            _canUnpowerRooms = CreateSetting(nameof(_canUnpowerRooms), BoolOverride.Original);
            _unpowerStartRoomOnCrystalUnplug = CreateSetting(nameof(_unpowerStartRoomOnCrystalUnplug), false);
            _autoUnpowerDelayMin = CreateSetting(nameof(_autoUnpowerDelayMin), 0.5f, FloatRange(0f, 10f));
            _autoUnpowerDelayMax = CreateSetting(nameof(_autoUnpowerDelayMax), 1.5f, FloatRange(0f, 10f));

            _maxSameModulesPerRoom = CreateSetting(nameof(_maxSameModulesPerRoom), 10, IntRange(1, 10));
            _twoLevelResearchUpgrades = CreateSetting(nameof(_twoLevelResearchUpgrades), true);
            _canResetResearch = CreateSetting(nameof(_canResetResearch), true);
        }
        override protected void SetFormatting()
        {
            _canUnpowerRooms.Format("Can unpower rooms");
            _unpowerStartRoomOnCrystalUnplug.Format("Unpower all rooms in crystal phase");
            using (Indent)
            {
                _autoUnpowerDelayMin.Format("min. delay", _unpowerStartRoomOnCrystalUnplug);
                _autoUnpowerDelayMax.Format("max. delay", _unpowerStartRoomOnCrystalUnplug);
            }

            _maxSameModulesPerRoom.Format("Max same modules per room");
            _twoLevelResearchUpgrades.Format("2-level research upgrades");
            _canResetResearch.Format("Can reset research");
        }
        protected override void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_Rebalance):
                    ForceApply();
                    _canUnpowerRooms.Value = BoolOverride.Original;
                    _unpowerStartRoomOnCrystalUnplug.Value = true;
                    _autoUnpowerDelayMin.Value = 3f;
                    _autoUnpowerDelayMax.Value = 6f;

                    _maxSameModulesPerRoom.Value = 1;
                    _twoLevelResearchUpgrades.Value = false;
                    _canResetResearch.Value = false;
                    break;
            }
        }

        // Privates

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Can unpower
        [HarmonyPatch(typeof(ShipConfig), nameof(ShipConfig.ForbidUnpower), MethodType.Getter), HarmonyPostfix]
        static private void ShipConfig_ForbidUnpower_Getter_Post(ShipConfig __instance, ref bool __result)
        => __result.ApplyBoolOverride(_canUnpowerRooms, true);

        // Unpower whole dungeon on crystal unplug
        [HarmonyPatch(typeof(Dungeon), nameof(Dungeon.OnCrystalUnplugged)), HarmonyPostfix]
        static private void Dungeon_OnCrystalUnplugged_Post(Dungeon __instance)
        {
            if (!_unpowerStartRoomOnCrystalUnplug)
                return;

            foreach (var room in __instance.OpenedRooms)
                if (room.IsAutoPowered)
                {
                    room.IsAutoPowered = false;
                    if (room.autoPowerVFXPfb != null)
                        foreach (ParticleSystem particleSystem in room.autoPowerVFXs)
                            particleSystem.Stop(true);
                }

            __instance.StartRoom.netSyncElement.SendRPCToServer(UniqueIDRPC.Room_RequestUnpower, new object[]
            {
                 __instance.StartRoom.gameNetManager.GetLocalPlayerID()
            });
        }

        [HarmonyPatch(typeof(Room), nameof(Room.Awake)), HarmonyPostfix]
        static private void Room_Awake_Post(Room __instance)
        {
            if (!_unpowerStartRoomOnCrystalUnplug)
                return;

            __instance.unpowerMinDelay = _autoUnpowerDelayMin;
            __instance.unpowerMaxDelay = _autoUnpowerDelayMax;
        }

        // Max same modules per room
        [HarmonyPatch(typeof(Room), nameof(Room.CanBuildMinorModule)), HarmonyPostfix]
        static private void Room_CanBuildMinorModule_Post(Room __instance, ref bool __result, BluePrintConfig bpConfig)
        {
            if (__instance.MinorModules.Count(t => t.BPConfig.ModuleName == bpConfig.ModuleName) >= _maxSameModulesPerRoom)
            {
                __instance.dungeon.EnqueueErrorNotification($"Cannot build any more modules of this type here!");
                __result = false;
            }
        }

        // 2-LV research upgrades
        [HarmonyPatch(typeof(GameConfig), nameof(GameConfig.LvlPlus2BluePrintWeight), MethodType.Getter), HarmonyPostfix]
        static private void GameConfig_LvlPlus2BluePrintWeight_Post(GameConfig __instance, ref int __result)
        {
            if (_twoLevelResearchUpgrades)
                return;

            __result = 0;
        }

        // Can reset research
        [HarmonyPatch(typeof(ResearchPanel), nameof(ResearchPanel.RefreshContent)), HarmonyPostfix]
        static private void ResearchPanel_RefreshContent_Post(ResearchPanel __instance)
        {
            if (_canResetResearch)
                return;

            __instance.resetButton.Visible = false;
        }
    }
}