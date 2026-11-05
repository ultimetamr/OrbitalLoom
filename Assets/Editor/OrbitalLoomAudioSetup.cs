using System;
using System.Linq;
using OrbitalLoom.Audio;
using UnityEditor;
using UnityEngine;
namespace OrbitalLoom.Editor
{
    public static class OrbitalLoomAudioSetup
    {
        public static void Apply()
        {
            var audio=UnityEngine.Object.FindObjectOfType<LoomAudio>(true);if(audio==null)throw new InvalidOperationException("Audio root missing");
            Undo.RecordObject(audio,"Author DSP score voice pool");
            audio.scoreVoices=Enumerable.Range(0,24).Select(i=>
            {
                var child=audio.transform.Find("Score voice "+i);
                if(child==null){var item=new GameObject("Score voice "+i);Undo.RegisterCreatedObjectUndo(item,"Create score voice");item.transform.SetParent(audio.transform,false);child=item.transform;}
                var source=child.GetComponent<AudioSource>();if(source==null)source=Undo.AddComponent<AudioSource>(child.gameObject);
                Undo.RecordObject(source,"Score voice defaults");source.playOnAwake=false;source.loop=false;source.spatialize=false;source.spatialBlend=0;source.volume=.4f;return source;
            }).ToArray();EditorUtility.SetDirty(audio);
            AssetDatabase.Refresh();
            foreach(string path in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/LoomAudio"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.forceToMono=false;
                settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
        }
    }
}
