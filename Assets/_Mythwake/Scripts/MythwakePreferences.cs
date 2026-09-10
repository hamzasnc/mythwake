using System;

/// <summary>
/// Normal players retain exactly the existing Unity PlayerPrefs keys.
/// Editor QA can opt into a separate process-wide namespace before any scene loads:
/// -mythwakeTestProfile kael-qa-20260909-a
/// </summary>
public static class MythwakePreferences
{
#if UNITY_EDITOR
    private static readonly string testProfileId = ParseEditorTestProfile(Environment.GetCommandLineArgs());
    public static string TestProfileId => testProfileId;
#else
    // Player builds never honor an Editor QA command-line switch.
    public static string TestProfileId => string.Empty;
#endif
    public static bool IsTestProfileActive => TestProfileId.Length > 0;

    private static string StorageKey(string key)
    {
        return IsTestProfileActive ? "Mythwake.TestProfiles." + TestProfileId + "." + key : key;
    }

    public static bool HasKey(string key) => UnityEngine.PlayerPrefs.HasKey(StorageKey(key));
    public static int GetInt(string key, int defaultValue = 0) => UnityEngine.PlayerPrefs.GetInt(StorageKey(key), defaultValue);
    public static float GetFloat(string key, float defaultValue = 0f) => UnityEngine.PlayerPrefs.GetFloat(StorageKey(key), defaultValue);
    public static string GetString(string key, string defaultValue = "") => UnityEngine.PlayerPrefs.GetString(StorageKey(key), defaultValue);
    public static void SetInt(string key, int value) => UnityEngine.PlayerPrefs.SetInt(StorageKey(key), value);
    public static void SetFloat(string key, float value) => UnityEngine.PlayerPrefs.SetFloat(StorageKey(key), value);
    public static void SetString(string key, string value) => UnityEngine.PlayerPrefs.SetString(StorageKey(key), value);
    public static void DeleteKey(string key) => UnityEngine.PlayerPrefs.DeleteKey(StorageKey(key));
    public static void Save() => UnityEngine.PlayerPrefs.Save();

#if UNITY_EDITOR
    private static string ParseEditorTestProfile(string[] arguments)
    {
        const string option = "-mythwakeTestProfile";
        var profile = string.Empty;
        for (var i = 0; i < arguments.Length; i++)
        {
            if (!string.Equals(arguments[i], option, StringComparison.Ordinal)) continue;
            if (profile.Length > 0) throw new ArgumentException("Specify -mythwakeTestProfile only once.");
            if (++i >= arguments.Length) throw new ArgumentException("-mythwakeTestProfile requires an explicit unique profile ID.");
            profile = arguments[i];
            if (string.IsNullOrEmpty(profile) || profile.Length > 64)
                throw new ArgumentException("Test profile ID must contain 1-64 ASCII letters, digits, hyphens or underscores.");
            foreach (var character in profile)
                if (!((character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z') ||
                    (character >= '0' && character <= '9') || character == '-' || character == '_'))
                    throw new ArgumentException("Test profile ID must contain only ASCII letters, digits, hyphens or underscores.");
        }
        return profile;
    }
#endif
}
