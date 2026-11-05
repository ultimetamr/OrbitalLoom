using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace OrbitalLoom.Core
{
    public static class LocalStore
    {
        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.None, MaxDepth = 32
        };
        public static string Root => Application.persistentDataPath;

        public static T Load<T>(string name, Func<T> fallback) where T : class
        {
            string path = Path.Combine(Root, name);
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                try
                {
                    if (!File.Exists(candidate)) continue;
                    if (new FileInfo(candidate).Length > 1024 * 1024) throw new IOException("Save file too large.");
                    var value = JsonConvert.DeserializeObject<T>(File.ReadAllText(candidate), Json);
                    if (value != null) return value;
                }
                catch (Exception e) when (e is IOException || e is JsonException || e is UnauthorizedAccessException)
                { Debug.LogWarning("[Orbital Loom] Save recovery: " + e.Message); }
            }
            return fallback();
        }

        public static void Save<T>(string name, T data)
        {
            Directory.CreateDirectory(Root);
            WriteAtomic(Path.Combine(Root, name), JsonConvert.SerializeObject(data, Formatting.Indented, Json));
        }

        public static void WriteAtomic(string path, string content)
        {
            string temp = path + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
            {
                writer.Write(content); writer.Flush();
                // Android IL2CPP does not implement Flush(flushToDisk: true) on
                // every device. Closing the stream still flushes managed buffers.
                FlushPortable(stream, () => stream.Flush(true));
            }
            if (!File.Exists(path)) { File.Move(temp, path); return; }
            // Keep the last complete generation if a crash interrupts a save.
            try { File.Replace(temp, path, path + ".bak"); }
            catch (PlatformNotSupportedException) { ReplacePortable(temp, path); }
            catch (IOException) { ReplacePortable(temp, path); }
        }

        public static void FlushPortable(Stream stream, Action flushToDisk)
        {
            try { flushToDisk(); }
            catch (PlatformNotSupportedException) { stream.Flush(); }
        }

        private static void ReplacePortable(string temp, string path)
        {
            File.Copy(path, path + ".bak", true);
            File.Copy(temp, path, true);
            File.Delete(temp);
        }
    }
}
