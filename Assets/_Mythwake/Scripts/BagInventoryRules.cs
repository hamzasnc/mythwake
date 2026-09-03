using System;
using System.Collections.Generic;

public enum BagInventoryTab
{
    All,
    Gear,
    Armor,
    Consumables,
    Materials,
    Gems
}

public enum BagInventoryConsumable
{
    None,
    HeroShardChest
}

public static class BagInventoryRules
{
    public const int GridSlotCount = 10;

    public static List<T> Filter<T>(IEnumerable<T> items, BagInventoryTab selectedTab, int maxVisibleSlots, Func<T, BagInventoryTab> categorySelector)
    {
        if (items == null || categorySelector == null || maxVisibleSlots <= 0)
        {
            return new List<T>();
        }

        var visible = new List<T>();
        foreach (var item in items)
        {
            if (selectedTab != BagInventoryTab.All && categorySelector(item) != selectedTab)
            {
                continue;
            }

            visible.Add(item);
            if (visible.Count >= maxVisibleSlots)
            {
                break;
            }
        }

        return visible;
    }

    public static int NormalizeUseCount(int requestedAmount, int availableAmount)
    {
        return Math.Max(1, Math.Min(requestedAmount, Math.Max(1, availableAmount)));
    }

    public static int ParseUseAmount(string input, int availableAmount)
    {
        return int.TryParse(input == null ? string.Empty : input.Trim(), out var amount)
            ? NormalizeUseCount(amount, availableAmount)
            : 1;
    }
}
