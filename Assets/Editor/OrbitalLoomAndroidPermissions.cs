using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

namespace OrbitalLoom.Editor
{
    // The SDK's generic manifest requests shared storage. This app uses only
    // its own files directory, so remove those declarations from generated files.
    public sealed class OrbitalLoomAndroidPermissions : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => int.MaxValue;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var root = Directory.GetParent(path)?.FullName ?? path;
            XNamespace android = "http://schemas.android.com/apk/res/android";
            string[] permissions = { "android.permission.READ_EXTERNAL_STORAGE", "android.permission.WRITE_EXTERNAL_STORAGE",
                "android.permission.MANAGE_EXTERNAL_STORAGE" };
            foreach (string file in Directory.EnumerateFiles(root, "AndroidManifest.xml", SearchOption.AllDirectories)
                .Where(f => f.Replace('\\', '/').Contains("/src/main/") || f.EndsWith("xrmanifest.androidlib/AndroidManifest.xml")))
            {
                var document = XDocument.Load(file);
                var matches = document.Root.Elements("uses-permission")
                    .Where(e => permissions.Contains((string)e.Attribute(android + "name"))).ToArray();
                if (matches.Length == 0) continue;
                foreach (var element in matches) element.Remove();
                document.Save(file);
            }
        }
    }
}
