using System;
using System.IO;
using BepInEx.Configuration;

namespace OC2HudCustomization
{
    internal static class ConfigMigration
    {
        // Run before Bind: BaseUnityPlugin has opened, but not created, the new ConfigFile.
        // A completed new config always wins. Keep the old file for rollback.
        internal static bool ImportLegacy(ConfigFile config)
        {
            string target = config.ConfigFilePath;
            if (File.Exists(target)) return false;
            string source = Path.Combine(Path.GetDirectoryName(target), PluginInfo.LegacyGuid + ".cfg");
            if (!File.Exists(source)) return false;
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".migration";
            try
            {
                File.Copy(source, temporary, false);
                try { File.Move(temporary, target); }
                catch (IOException)
                {
                    // A concurrent creator owns its config; never replace it with the old one.
                    if (!File.Exists(target)) throw;
                    config.Reload();
                    return false;
                }
                config.Reload();
                return true;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
