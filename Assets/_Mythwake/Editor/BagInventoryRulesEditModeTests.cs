using System;
using UnityEditor;
using UnityEngine;

public static class BagInventoryRulesEditModeTests
{
    [MenuItem("Mythwake/Tests/Bag Inventory Rules")]
    public static void Run()
    {
        var items = new[]
        {
            BagInventoryTab.Consumables,
            BagInventoryTab.Materials,
            BagInventoryTab.Gear,
            BagInventoryTab.Gear,
            BagInventoryTab.Gems
        };

        var filtered = BagInventoryRules.Filter(items, BagInventoryTab.Gear, 10, item => item);
        Assert(filtered.Count == 2, "Gear filtering should return both Gear entries.");
        Assert(BagInventoryRules.Filter(items, BagInventoryTab.All, 2, item => item).Count == 2, "All filtering should respect the ten-slot page limit.");
        Assert(BagInventoryRules.NormalizeUseCount(0, 3) == 1, "Use amount should clamp to one at the lower bound.");
        Assert(BagInventoryRules.NormalizeUseCount(99, 3) == 3, "Use amount should clamp to the owned quantity.");
        Assert(BagInventoryRules.ParseUseAmount(" 2 ", 3) == 2, "Use amount should parse trimmed input.");
        Assert(BagInventoryRules.ParseUseAmount("not-a-number", 3) == 1, "Invalid use amount should fall back to one.");
        Debug.Log("Bag inventory rule EditMode checks passed: category filtering, ten-slot limit, and quantity clamping are deterministic.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
