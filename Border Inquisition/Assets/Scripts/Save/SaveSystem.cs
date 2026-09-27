using System;
using System.IO;
using UnityEngine;

namespace Save
{
    // One match save on disk, in the player's data folder. Written in full every time, through a
    // temporary file so a crash mid-write never leaves half a save behind.
    public static class SaveSystem
    {
        public const int FormatVersion = 1;

        private const string FileName = "match.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => SavePath + ".tmp";

        public static void Write(MatchSave save)
        {
            save.FormatVersion = FormatVersion;
            save.GameVersion = Application.version;
            save.SavedAtUtc = DateTime.UtcNow.ToString("o");

            try
            {
                File.WriteAllText(TempPath, JsonUtility.ToJson(save));
                if (File.Exists(SavePath))
                    File.Delete(SavePath);
                File.Move(TempPath, SavePath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not save the match to {SavePath}: {exception.Message}");
            }
        }

        // False when there is no save, it cannot be read, or it is of another format version.
        public static bool TryRead(out MatchSave save)
        {
            save = null;
            if (!File.Exists(SavePath))
                return false;

            try
            {
                save = JsonUtility.FromJson<MatchSave>(File.ReadAllText(SavePath));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"The match save {SavePath} cannot be read: {exception.Message}");
                save = null;
                return false;
            }

            if (save == null || save.FormatVersion != FormatVersion)
            {
                Debug.LogWarning($"The match save {SavePath} is of another format version - ignored.");
                save = null;
                return false;
            }
            return true;
        }

        public static bool HasSave => TryRead(out _);

        public static void Delete()
        {
            try
            {
                if (File.Exists(SavePath))
                    File.Delete(SavePath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not delete the match save {SavePath}: {exception.Message}");
            }
        }
    }
}
