namespace Vheos.Mods.DungeonOfTheEndless
{
    using System;
    using HarmonyLib;
    using UnityEngine;
    using Mods.Core;
    using Tools.Extensions.Math;
    using Tools.Extensions.Collections;

    public class CameraAndUI : AMod
    {
        // Settings
        static private ModSetting<bool> _zoomWithMouseWheel;
        static private ModSetting<float> _zoomValue;
        static private ModSetting<float> _zoomSpeed;
        static private ModSetting<bool> _followHeroAfterFocus;
        static private ModSetting<bool> _multiplayerChat;
        override protected void Initialize()
        {
            _zoomValue = CreateSetting(nameof(_zoomValue), 1f, FloatRange(0.1f, 2f));
            _zoomWithMouseWheel = CreateSetting(nameof(_zoomWithMouseWheel), false);
            _zoomSpeed = CreateSetting(nameof(_zoomSpeed), 0.2f, FloatRange(0.1f, 1f));
            _followHeroAfterFocus = CreateSetting(nameof(_followHeroAfterFocus), false);
            _multiplayerChat = CreateSetting(nameof(_multiplayerChat), true);

            _currentZoom = 1f;
        }
        override protected void SetFormatting()
        {
            _zoomValue.Format("Zoom value");
            _zoomWithMouseWheel.Format("Zoom with mouse wheel");
            using (Indent)
                _zoomSpeed.Format("Zoom speed", _zoomWithMouseWheel);
            _followHeroAfterFocus.Format("Follow hero after focus");
            _multiplayerChat.Format("Multiplayer chat");
        }
        protected override void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case nameof(SettingsPreset.Vheos_Rebalance):
                    ForceApply();
                    _zoomWithMouseWheel.Value = true;
                    _zoomValue.Value = 0.75f;
                    _zoomSpeed.Value = 0.2f;
                    _followHeroAfterFocus.Value = false;
                    _multiplayerChat.Value = false;
                    break;
            }
        }

        // Privates
        static private float _currentZoom;

        // Hooks
#pragma warning disable IDE0051, IDE0060, IDE1006

        // Zoom
        [HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.SetGameCameraScale)), HarmonyPrefix]
        static private void GameCameraManager_SetGameCameraScale_Pre(GameCameraManager __instance, ref float scale)
        => scale *= _currentZoom;

        // Zoom with mouse wheel
        [HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.Update)), HarmonyPrefix]
        static private void GameCameraManager_Update_Pre(GameCameraManager __instance)
        {
            if (_zoomWithMouseWheel)
                _zoomValue.Value *= _zoomSpeed.Value.Add(1f).Pow(Input.GetAxis("MouseWheel"));

            _currentZoom.SetLerp(_zoomValue, 0.1f);
        }

        [HarmonyPatch(typeof(GameCameraManager), nameof(GameCameraManager.SwitchToTacticalMapCamera)), HarmonyPrefix]
        static private bool GameCameraManager_SwitchToTacticalMapCamera_Pre(GameCameraManager __instance)
        => !_zoomWithMouseWheel || Input.GetAxis("MouseWheel") >= 0f;

        // Follow hero after focus
        [HarmonyPatch(typeof(Hero), nameof(Hero.Focus)), HarmonyPostfix]
        static private void Hero_Focus_Post(Hero __instance)
        {
            if (_followHeroAfterFocus)
                __instance.dungeon.gameCameraManager.StickTo(__instance.transform);
        }

        // Multiplayer chat
        [HarmonyPatch(typeof(ChatPanel), nameof(ChatPanel.IsChatSystemAvailable)), HarmonyPrefix]
        static private bool ChatPanel_IsChatSystemAvailable_Pre(ChatPanel __instance)
        => _multiplayerChat;

        [HarmonyPatch(typeof(ChatPanelLobby), nameof(ChatPanelLobby.IsChatSystemAvailable)), HarmonyPrefix]
        static private bool ChatPanelLobby_IsChatSystemAvailable_Pre(ChatPanelLobby __instance)
        => _multiplayerChat;
    }
}