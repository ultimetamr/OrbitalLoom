using System;
using System.IO;
using System.Linq;
using OrbitalLoom.Rendering;
using OrbitalLoom.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace OrbitalLoom.Editor
{
    public static class OrbitalLoomBoxSetup
    {
        public static void Apply()
        {
            var view=UnityEngine.Object.FindObjectOfType<EnergyBoxView>(true);
            var ui=UnityEngine.Object.FindObjectOfType<OrbitalLoomUI>(true);
            if(view==null||ui==null||view.assembly==null)throw new InvalidOperationException("Open authored scene with energy apparatus");
            // Remove the old modes from the delivery scene, retaining only the
            // established energy-box hierarchy and native UI/input pools.
            var puzzle=ui.windows.puzzleRoot;
            foreach(Transform child in puzzle.Cast<Transform>().ToArray())
                if(child!=view.transform)Undo.DestroyObjectImmediate(child.gameObject);
            // Legacy ring chrome lived under Interface, outside puzzleRoot.
            // Its opaque plates otherwise cover the new menus and tool dock.
            var legacyChrome=ui.buttons[0].transform.parent.Find("Ring HUD surfaces");
            if(legacyChrome!=null)Undo.DestroyObjectImmediate(legacyChrome.gameObject);
            foreach(var root in view.gameObject.scene.GetRootGameObjects())
                foreach(var t in root.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            OrbitalLoomUIRefreshSetup.Apply();OrbitalLoomBoxArtSetup.Apply(view);
            OrbitalLoomObservatorySetup.Apply(ui,view);
            OrbitalLoomRelicSetup.Apply(view);
            OrbitalLoomAudioSetup.Apply();
            PlayerSettings.bundleVersion="0.9.8";PlayerSettings.Android.bundleVersionCode=25;
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            if(!EditorSceneManager.SaveScene(view.gameObject.scene))throw new IOException("Scene save failed");
            AssetDatabase.SaveAssets();OrbitalLoomProjectTools.Validate();
        }
    }
}
