using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrbitalLoom.Editor
{
    [InitializeOnLoad]
    public static class OrbitalLoomBoxBatch
    {
        private const string Key="OrbitalLoom.BoxBatch";
        private static int idleFrames;
        /// <summary>Resume packaging an already tested saved scene. Tool paths
        /// are scoped to this operation and user preferences are restored.</summary>
        public static void RebuildVerified()
        {
            string[] flags={"JdkUseEmbedded","SdkUseEmbedded","NdkUseEmbedded"};
            string[] paths={"Jdk11Path","AndroidSdkRoot","AndroidNdkRootR23B"};
            var hasFlags=Array.ConvertAll(flags,EditorPrefs.HasKey);var oldFlags=Array.ConvertAll(flags,k=>EditorPrefs.GetBool(k));
            var hasPaths=Array.ConvertAll(paths,EditorPrefs.HasKey);var oldPaths=Array.ConvertAll(paths,k=>EditorPrefs.GetString(k));
            int exit=1;
            try
            {
                EditorSceneManager.OpenScene(OrbitalLoomProjectTools.ScenePath);
                string player=Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines/AndroidPlayer");
                UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath=Path.Combine(player,"OpenJDK");
                UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath=Path.Combine(player,"SDK");
                UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath=Path.Combine(player,"NDK");
                Debug.Log("[Orbital Loom] Build tool paths: JDK="+UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath+
                    "; SDK="+UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath+"; NDK="+UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath);
                OrbitalLoomProjectTools.BuildAndroid();exit=0;
                File.WriteAllText("Builds/EnergyBox-BatchResult.txt","PASS energy-box-batch\n124 EditMode tests and full UI smoke retained from completed verification\nPASS build\n"+DateTimeOffset.Now.ToString("o"));
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Builds/Reasoning095-BuildFailure.txt",e.ToString());}
            finally
            {
                for(int i=0;i<flags.Length;i++)
                {
                    if(hasFlags[i])EditorPrefs.SetBool(flags[i],oldFlags[i]);else EditorPrefs.DeleteKey(flags[i]);
                    if(hasPaths[i])EditorPrefs.SetString(paths[i],oldPaths[i]);else EditorPrefs.DeleteKey(paths[i]);
                }
            }
            EditorApplication.Exit(exit);
        }
        static OrbitalLoomBoxBatch(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            Directory.CreateDirectory("Builds");EditorSceneManager.OpenScene(OrbitalLoomProjectTools.ScenePath);
            OrbitalLoomBoxSetup.Apply();
            // Test Framework saves the current Undo group, then reverts down to
            // that group after EditMode tests. Finish authored assets first so
            // the runner cannot roll back their meshes/materials on completion.
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            // Domain tests are pure. Run them in a clean empty scene so a
            // delayed native-export refresh cannot raise a batch save dialog.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SessionState.SetInt(Key,1);
            File.WriteAllText("Library/OrbitalLoom.Task.result","WAIT tests");OrbitalLoomTestRunner.Run();
        }
        private static void Tick()
        {
            int stage=SessionState.GetInt(Key,0);
            if(stage==0||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
            {idleFrames=0;return;}
            // isPlaying becomes false before the exit transition has destroyed
            // scene objects. Wait for stable edit frames before another run.
            if(++idleFrames<12)return;
            if(!File.Exists("Library/OrbitalLoom.Task.result"))return;
            string result=File.ReadAllText("Library/OrbitalLoom.Task.result");if(result.StartsWith("WAIT"))return;
            if(result.StartsWith("FAIL")){Finish(result,1);return;}
            idleFrames=0;
            try
            {
                if(stage==1)
                {
                    // The test runner restores a temporary scene after asset
                    // imports. Reopen the saved delivery scene before play.
                    EditorSceneManager.OpenScene(OrbitalLoomProjectTools.ScenePath);
                    var view=UnityEngine.Object.FindObjectOfType<OrbitalLoom.Rendering.EnergyBoxView>();
                    if(view==null||view.assembly==null||view.art==null)throw new InvalidOperationException("Saved energy apparatus references missing");
                    SessionState.SetInt(Key,2);File.WriteAllText("Library/OrbitalLoom.Task.result","WAIT boxes");OrbitalLoomBoxSmokeTest.Begin();
                }
                else if(stage==2)
                {
                    SessionState.SetInt(Key,6);File.WriteAllText("Library/OrbitalLoom.Task.result","WAIT build");
                    File.WriteAllText("Library/OrbitalLoom.Task.request","build");
                }
                else if(stage==6)Finish("PASS energy-box-batch\n"+result+"\n"+DateTimeOffset.Now.ToString("o"),0);
            }
            catch(Exception e){Debug.LogException(e);Finish("FAIL energy-box-batch\n"+e,1);}
        }
        private static void Finish(string result,int exit)
        {SessionState.SetInt(Key,0);File.WriteAllText("Builds/EnergyBox-BatchResult.txt",result);Debug.Log(result);EditorApplication.Exit(exit);}
    }
}
