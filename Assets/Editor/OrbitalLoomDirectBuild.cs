using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrbitalLoom.Editor
{
    public static class OrbitalLoomDirectBuild
    {
        public static void Export()
        {
            string destination = Environment.GetEnvironmentVariable("ORBITAL_LOOM_EXPORT_DIR");
            if (string.IsNullOrWhiteSpace(destination)) destination = Path.GetFullPath("Builds/AndroidExport-1.0.0");
            if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
                throw new InvalidOperationException("Export destination must be empty: " + destination);
            EditorSceneManager.OpenScene(OrbitalLoomProjectTools.ScenePath);
            OrbitalLoomProjectTools.Validate();
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 28;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
            AssetDatabase.SaveAssets();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes, locationPathName = destination, target = BuildTarget.Android, options = BuildOptions.None
            });
            File.WriteAllText("Builds/Direct-Export.txt", report.summary.result + "\nErrors=" + report.summary.totalErrors
                + "\nWarnings=" + report.summary.totalWarnings + "\nDuration=" + report.summary.totalTime);
            Debug.Log("[Orbital Loom] DIRECT_EXPORT " + report.summary.result);
            int code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
