namespace Vheos.Mods.DungeonOfTheEndless
{
    using System;
    using System.Linq;
    using System.Reflection;
    using BepInEx;
    using Mods.Core;
    using Tools.Utilities;
    using Utility = Tools.Utilities.Utility;

    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Main : BepInExEntryPoint
    {
        // Metadata
        public const string GUID = "Vheos.Mods.DungeonOfTheEndless";
        public const string NAME = "DotE Mods";
        public const string VERSION = "1.0.0";

        // User logic
        override protected Assembly CurrentAssembly
        => Assembly.GetExecutingAssembly();
        override protected Type[] ModsOrderingList
        => new[]
        {
            typeof(HeroesAndSkills),
            typeof(RoomsAndModules),
            typeof(CameraAndUI),
            typeof(Various),
            typeof(Cheats),
        };
        override protected string[] PresetNames
        => Utility.GetEnumValuesAsStrings<SettingsPreset>().ToArray();
    }
}