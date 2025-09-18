using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using SkillManager;

namespace DualWielder
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class DualWielderPlugin : BaseUnityPlugin
    {
        internal const string ModName = "DualWielder";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private const string ConfigFileName = ModGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource DualWielderLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public enum Toggle { On = 1, Off = 0 }

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        private static ConfigEntry<Toggle> _combineDamages = null!;
        private static ConfigEntry<float> _damageModifier = null!;
        public static bool CombineDamages => _combineDamages.Value is Toggle.On;
        public static float DamageModifier => _damageModifier.Value;
        public void Awake()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            _combineDamages = config(
                "2 - Settings", 
                "Combine Weapon Damages", 
                Toggle.On, 
                "When enabled, dual-wielding will add the left-hand weapon’s damage to the right-hand weapon’s attacks."
            );

            _damageModifier = config(
                "2 - Settings", 
                "Total Damage Modifier", 
                0.5f, 
                new ConfigDescription(
                    "Adjusts the total damage output when combining weapon damages. "
                    + "Acts as a balancing factor to prevent dual wield from being overpowered. "
                    + "For example: 0.5 = 50% of combined damage, 1.0 = 100% of combined damage.",
                    new AcceptableValueRange<float>(0f, 1f)
                )
            );
            
            Skill dualSkill = new Skill("DualWielder", "dualwielder_icon.png");
            dualSkill.Name.English("Dual Wield");
            dualSkill.Description.English("Reduces damage reduction from using two weapons");
            dualSkill.Configurable = true;

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }
        private void OnDestroy()
        {
            Config.Save();
        }
        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }
        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                DualWielderLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                DualWielderLogger.LogError($"There was an issue loading your {ConfigFileName}");
                DualWielderLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }
    }
}