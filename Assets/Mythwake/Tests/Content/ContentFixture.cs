using System.IO;
using System.Linq;
using Mythwake.Core.Meta;
using NUnit.Framework;

namespace Mythwake.Content.Tests
{
    /// <summary>Finds and loads the real content of this checkout (works from Unity and from dotnet).</summary>
    internal static class ContentFixture
    {
        private static GameContent _content;

        public static string ContentDir()
        {
            // Unity runs tests from the project root; dotnet runs them from bin/. Walk up until Assets/Mythwake is found.
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "Mythwake")))
                dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "project root not found");
            return Path.Combine(dir.FullName, "Assets", "Mythwake", "Content");
        }

        public static string HeroesDir() => Path.Combine(ContentDir(), "Heroes");

        public static string[] MonsterFiles() =>
            Directory.GetFiles(Path.Combine(ContentDir(), "Monsters"), "*.monster.json", SearchOption.AllDirectories)
                .OrderBy(p => p, System.StringComparer.Ordinal).ToArray();

        public static string[] HeroFiles() =>
            Directory.GetFiles(HeroesDir(), "*.hero.json", SearchOption.AllDirectories).OrderBy(p => p, System.StringComparer.Ordinal).ToArray();

        public static GameContent Content()
        {
            if (_content != null)
                return _content;
            var data = Path.Combine(ContentDir(), "Data");
            _content = GameContentLoader.Load(
                name => File.ReadAllText(Path.Combine(data, name + ".json")),
                HeroFiles().Select(File.ReadAllText),
                MonsterFiles().Select(File.ReadAllText));
            return _content;
        }
    }
}
