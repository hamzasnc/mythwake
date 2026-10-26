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
            /// <summary>Monsters without a Spine rig: the cut-out master drawn as one sprite.</summary>
            public Sprite sprite;
        }

        public TextAsset[] dataFiles = new TextAsset[0];
        public TextAsset[] strings = new TextAsset[0];
        public HeroAssets[] heroes = new HeroAssets[0];
        public HeroAssets[] monsters = new HeroAssets[0];

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
            return GameContentLoader.Load(Read, heroes.Where(h => h.definition != null).Select(h => h.definition.text),
                monsters.Where(m => m.definition != null).Select(m => m.definition.text));
        }

        /// <summary>Same value the rules service reports (see <see cref="Mythwake.Content.ContentVersion"/>).</summary>
        public string ContentVersion()
        {
            var files = dataFiles.Concat(strings).Where(f => f != null)
                .Select(f => new KeyValuePair<string, string>("Data/" + f.name + ".json", f.text))
                .Concat(heroes.Where(h => h.definition != null)
                    .Select(h => new KeyValuePair<string, string>($"Heroes/{h.id}/{h.id}.hero.json", h.definition.text)))
                .Concat(monsters.Where(m => m.definition != null)
                    .Select(m => new KeyValuePair<string, string>($"Monsters/{m.id}/{m.id}.monster.json", m.definition.text)));
            return Mythwake.Content.ContentVersion.Compute(files);
        }

        public HeroAssets Hero(string id)
        {
            if (_byId == null)
                _byId = heroes.Concat(monsters).ToDictionary(h => h.id);
            return _byId.TryGetValue(id, out var h) ? h : null;
        }
    }
}
