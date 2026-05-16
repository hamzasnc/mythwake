using System;
using System.Collections.Generic;

public enum MythwakeLanguage
{
    English,
    German
}

public static partial class MythwakeLocalization
{
    public static string Text(MythwakeLanguage language, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var table = GetLanguageTable(language);
        if (table != English && table.TryGetValue(key, out var localized))
        {
            return localized;
        }

        if (English.TryGetValue(key, out var english))
        {
            return english;
        }

        return key;
    }

    public static string Format(MythwakeLanguage language, string key, params object[] args)
    {
        var template = Text(language, key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    private static Dictionary<string, string> GetLanguageTable(MythwakeLanguage language)
    {
        switch (language)
        {
            case MythwakeLanguage.German:
                return German;
            default:
                return English;
        }
    }
}
