namespace OC2HudCustomization
{
    internal static class GameLanguage
    {
        internal static HudLanguage? Read()
        {
            // GetLanguage falls back to the OS before Steam is ready. Keep our requested
            // English startup instead, then use the game's authoritative language method.
            if (!SteamPlayerManager.Initialized) return null;
            return FromGame(global::Localization.GetLanguage());
        }

        internal static HudLanguage FromGame(SupportedLanguages language)
        {
            switch (language)
            {
                case SupportedLanguages.French: return HudLanguage.French;
                case SupportedLanguages.Italian: return HudLanguage.Italian;
                case SupportedLanguages.German: return HudLanguage.German;
                case SupportedLanguages.Spanish: return HudLanguage.Spanish;
                case SupportedLanguages.Russian: return HudLanguage.Russian;
                case SupportedLanguages.Brazilian: return HudLanguage.Brazilian;
                case SupportedLanguages.Polish: return HudLanguage.Polish;
                case SupportedLanguages.Chinese: return HudLanguage.Chinese;
                case SupportedLanguages.Japanese: return HudLanguage.Japanese;
                case SupportedLanguages.Korean: return HudLanguage.Korean;
                case SupportedLanguages.ChineseTraditional: return HudLanguage.ChineseTraditional;
                default: return HudLanguage.English;
            }
        }
    }
}
