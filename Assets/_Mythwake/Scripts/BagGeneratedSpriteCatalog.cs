using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mythwake/Bag Generated Sprite Catalog")]
public sealed class BagGeneratedSpriteCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string key;
        public Sprite sprite;
    }

    public Entry[] entries;

    public Sprite GetSprite(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || entries == null)
        {
            return null;
        }

        for (var i = 0; i < entries.Length; i++)
        {
            if (string.Equals(entries[i].key, key, StringComparison.Ordinal))
            {
                return entries[i].sprite;
            }
        }

        return null;
    }
}
