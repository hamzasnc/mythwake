using System.Collections.Generic;
using System.Linq;
using Mythwake.Content;
using Mythwake.Core.Meta;
using Spine.Unity;
using UnityEngine;

namespace Mythwake.Game
{
    /// <summary>Unity-side index of all content: data files and, per hero, its definition, skeleton and images.
    /// Built by the editor (Mythwake ▸ Rebuild Content Catalog), never edited by hand.</summary>
    [CreateAssetMenu(menuName = "Mythwake/Content Catalog")]
    public sealed class ContentCatalog : ScriptableObject
    {
        [System.Serializable]
        public sealed class HeroAssets
        {
            public string id;
            public TextAsset definition;
            public SkeletonDataAsset skeleton;
            public Texture2D portrait;
            /// <summary>Full-body render of the rig (hero detail, banners).</summary>
            public Texture2D full;
            public Texture2D cutIn;
        }

        public TextAsset[] dataFiles = new TextAsset[0];
        public TextAsset[] strings = new TextAsset[0];
        public HeroAssets[] heroes = new HeroAssets[0];

        private Dictionary<string, HeroAssets> _byId;

        public GameContent LoadContent()
        {
            string Read(string name)
            {
                var file = dataFiles.FirstOrDefault(f => f != null && f.name == name);
                if (file == null)
                    throw new ContentException($"content catalog has no data file '{name}'");
                return file.text;
            }
            return GameContentLoader.Load(Read, heroes.Where(h => h.definition != null).Select(h => h.definition.text));
        }

        public HeroAssets Hero(string id)
        {
            if (_byId == null)
                _byId = heroes.ToDictionary(h => h.id);
            return _byId.TryGetValue(id, out var h) ? h : null;
        }
    }
}
