using System.Reflection;

[assembly: AssemblyTitle(OC2HudCustomization.PluginInfo.Name)]
[assembly: AssemblyDescription("Local recipe and ingredient HUD presentation settings for Overcooked! 2.")]
[assembly: AssemblyVersion(OC2HudCustomization.PluginInfo.Version)]
[assembly: AssemblyFileVersion(OC2HudCustomization.PluginInfo.Version)]

namespace OC2HudCustomization
{
    internal static class PluginInfo
    {
        internal const string Guid = "com.ch3ngyz.plugin.overcooked2.hudcustomization";
        internal const string LegacyGuid = "com.oc2.hudcustomization";
        internal const string Name = "OC2 HUD 自定义 / OC2 HUD Customization";
        internal const string Version = "0.2.1";
    }
}
