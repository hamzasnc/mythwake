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

        /// <summary>Hub art and hotspot positions (Content/Town). Presentation only, not part of the content version.</summary>
        [System.Serializable]
        public sealed class TownAssets
        {
            public TextAsset layout;
            public Texture2D background;
            public Texture2D worldMap;
            /// <summary>Stand-in for every building that is not rebuilt yet.</summary>
            public Texture2D ruin;
            public Texture2D[] buildings = new Texture2D[0];

            public Texture2D Building(string id) => buildings.FirstOrDefault(b => b != null && b.name == id);
        }

        public TextAsset[] dataFiles = new TextAsset[0];
        public TextAsset[] strings = new TextAsset[0];
        public HeroAssets[] heroes = new HeroAssets[0];
        public HeroAssets[] monsters = new HeroAssets[0];
        public TownAssets town = new TownAssets();
        /// <summary>Item icons (Content/Icons), looked up by file name.</summary>
        public Texture2D[] icons = new Texture2D[0];
        /// <summary>Painted battle backgrounds (Content/Backgrounds), one per chapter and game mode, by file name.</summary>
        public Sprite[] backgrounds = new Sprite[0];
        /// <summary>Intro story pictures (Content/Story), by file name.</summary>
        public Texture2D[] story = new Texture2D[0];

        private Dictionary<string, HeroAssets> _byId;
        private TownLayout _layout;

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

        public Texture2D Icon(string name) => icons.FirstOrDefault(i => i != null && i.name == name);

        public Sprite Background(string name) => name == null ? null : backgrounds.FirstOrDefault(b => b != null && b.name == name);

        public Texture2D Story(string name) => story.FirstOrDefault(s => s != null && s.name == name);

        public TownLayout Layout()
        {
            if (_layout == null)
                _layout = town?.layout != null ? JsonUtility.FromJson<TownLayout>(town.layout.text) : new TownLayout();
            return _layout;
        }

        public HeroAssets Hero(string id)
        {
            if (_byId == null)
                _byId = heroes.Concat(monsters).ToDictionary(h => h.id);
            return _byId.TryGetValue(id, out var h) ? h : null;
        }

        /// <summary>True when the hero or monster has a Spine rig (lets assemblies without spine-unity ask).</summary>
        public bool HasRig(string id) => Hero(id)?.skeleton != null;
    }

    /// <summary>Where things sit on the hub pictures, as fractions of the picture (0..1, origin top left).</summary>
    [System.Serializable]
    public sealed class TownLayout
    {
        /// <summary>Buildings of Dawnhollow: x/y is the ground point under the building, size its width.</summary>
        public Spot[] buildings = new Spot[0];
        /// <summary>Pins on the world map: x/y is the pin centre.</summary>
        public Spot[] world = new Spot[0];

        public Spot Building(string id) => buildings.FirstOrDefault(s => s.id == id);
        public Spot Pin(string id) => world.FirstOrDefault(s => s.id == id);
    }

    [System.Serializable]
    public sealed class Spot
    {
        public string id;
        public float x;
        public float y;
        public float size = 0.3f;
    }
}
