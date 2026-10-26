using System.IO;
using Mythwake.Content;
using Mythwake.Core.Meta;
using UnityEngine;

namespace Mythwake.Game
{
    /// <summary>Local save file with atomic writes and one backup generation.</summary>
    public sealed class SaveStore
    {
        private readonly string _path;

        public SaveStore(string directory, string fileName = "player.json")
        {
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, fileName);
        }

        public PlayerState Load()
        {
            foreach (var candidate in new[] { _path, _path + ".bak" })
            {
                if (!File.Exists(candidate))
                    continue;
                try
                {
                    return SaveJson.Read(File.ReadAllText(candidate));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Save] could not read {candidate}: {e.Message}");
                }
            }
            return null;
        }

        public void Save(PlayerState state)
        {
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, SaveJson.Write(state));
            if (File.Exists(_path))
                File.Copy(_path, _path + ".bak", true);
            File.Copy(tmp, _path, true);
            File.Delete(tmp);
        }

        public void Delete()
        {
            foreach (var f in new[] { _path, _path + ".bak", _path + ".tmp" })
                if (File.Exists(f))
                    File.Delete(f);
        }
    }
}
