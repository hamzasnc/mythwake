using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class CurrentSliceValidation
{
    [MenuItem("Mythwake/Validate Current Slice")]
    public static void RunCurrentSliceValidation()
    {
        try
        {
            RunPrivateValidator(typeof(VillageUiValidation), "ValidateVillageUi", "Village UI");
            RunPrivateValidator(typeof(FastRewardsUiValidation), "ValidateFastRewardsUi", "Fast Rewards UI");
            RunPrivateValidator(typeof(SummonUiValidation), "ValidateSummonUi", "Summon UI");
            RunValidator("Paladin Integration", PaladinSpineValidation.RunPaladinIntegrationValidation);
            RunValidator("Paladin Spine Handoff", PaladinSpineValidation.RunPaladinSpineValidation);

            Debug.Log("Current Mythwake slice validated: Village, Fast Rewards, Summon, Paladin integration, and Paladin Spine handoff.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    private static void RunPrivateValidator(Type type, string methodName, string label)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new InvalidOperationException($"Missing validator method: {type.Name}.{methodName}");
        }

        RunValidator(label, () => method.Invoke(null, null));
    }

    private static void RunValidator(string label, Action action)
    {
        Debug.Log($"Running {label} validation...");
        try
        {
            action();
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw new InvalidOperationException($"{label} validation failed.", ex.InnerException);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{label} validation failed.", ex);
        }

        Debug.Log($"{label} validation passed.");
    }
}
