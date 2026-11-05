using System;
using System.IO;
using System.Linq;
using OrbitalLoom.Core;
using OrbitalLoom.Gameplay;
using OrbitalLoom.Rendering;
using OrbitalLoom.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace OrbitalLoom.Editor
{
    [InitializeOnLoad]
    public static class OrbitalLoomProjectTools
    {
        public const string ScenePath="Assets/Scenes/OrbitalLoom.unity";
        static OrbitalLoomProjectTools(){EditorApplication.update+=Poll;}
        private static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            string reload="Library/OrbitalLoom.VerifiedScene.pending",request="Library/OrbitalLoom.Task.request";
            if(File.Exists(reload))
            {
                try{var current=SceneManager.GetActiveScene();if(current.IsValid()&&current.isDirty){Directory.CreateDirectory("Builds/Backups");EditorSceneManager.SaveScene(current,"Builds/Backups/Editor-Before-Verified-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity",true);}AssetDatabase.Refresh();EditorSceneManager.OpenScene(ScenePath);File.Delete(reload);File.WriteAllText("Library/OrbitalLoom.Task.result","PASS reload-verified\n"+DateTimeOffset.Now.ToString("o"));}
                catch(Exception e){File.Move(reload,reload+".failed");Debug.LogException(e);}return;
            }
            if(!File.Exists(request))return;string cmd=File.ReadAllText(request).Trim();File.Delete(request);
            try{if(cmd=="build")BuildAndroid();else if(cmd=="validate")Validate();else if(cmd=="refresh")AssetDatabase.Refresh();else throw new ArgumentException(cmd);File.WriteAllText("Library/OrbitalLoom.Task.result","PASS "+cmd+"\n"+DateTimeOffset.Now.ToString("o"));}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Library/OrbitalLoom.Task.result","FAIL "+cmd+"\n"+e);}
        }
        public static void Validate()
        {
            var campaign=EnergyBoxCampaign.Parse(Resources.Load<TextAsset>("EnergyBoxes").text);
            if(campaign.levels.Length!=24)throw new InvalidOperationException("Expected24puzzles");
            foreach(var level in campaign.levels)if(!EnergyBoxSolver.Evaluate(level,level.reference).Complete)throw new InvalidOperationException("Invalid reference "+level.id+": "+EnergyBoxSolver.Evaluate(level,level.reference).Message);
            var ui=UnityEngine.Object.FindObjectOfType<OrbitalLoomUI>(true);var view=UnityEngine.Object.FindObjectOfType<EnergyBoxView>(true);
            if(ui==null||view==null||view.art==null||ui.menuSculpture==null)throw new InvalidOperationException("Missing delivery references");
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing component "+t.name);
            File.WriteAllText("Builds/Validation.txt","PASS24puzzles; references valid; no missing scripts\n"+DateTimeOffset.Now.ToString("o"));
        }
        public static void BuildAndroid()
        {
            Validate();var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/OrbitalLoom-MR.apk",target=BuildTarget.Android,options=BuildOptions.Development});
            File.WriteAllText("Builds/BuildReport.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+"\nWarnings="+report.summary.totalWarnings+"\nBytes="+report.summary.totalSize+"\nDuration="+report.summary.totalTime);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Android build failed");
        }
    }
}
