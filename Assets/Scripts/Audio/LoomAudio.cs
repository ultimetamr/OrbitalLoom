using System;
using System.Collections.Generic;
using OrbitalLoom.Core;
using OrbitalLoom.Gameplay;
using UnityEngine;
namespace OrbitalLoom.Audio
{
    /// <summary>Imported original PCM timbres. The DSP clock drives rhythmic
    /// playback; pause/menu/stop cancels scheduled cues instead of catching up.</summary>
    public sealed class LoomAudio:MonoBehaviour
    {
        public static LoomAudio Instance{get;private set;}
        public AudioSource[] voices,scoreVoices;public AudioSource ambience;
        private readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        private GameManager game;private int next,cursor;
        private EnergyBoxSession session;private BoxEvaluation evaluation;
        private List<LoomCue> cues=new List<LoomCue>();
        private double origin;private bool clockRunning,ringing;private double[] busy;private float[] gains;
        public int ScheduledCueCount{get;private set;}
        public int DroppedCueCount{get;private set;}
        private void Awake(){Instance=this;}
        private void Start()
        {
            game=GameManager.Instance;
            foreach(string name in new[]{"select","connect","undo","error","win","mirror","splitter","ambient","tick","downbeat","flow","wait","goal-low","goal-mid","goal-high"})
            {var clip=Resources.Load<AudioClip>("LoomAudio/"+name);if(clip!=null)clips[name]=clip;else Debug.LogWarning("[Orbital Loom] Missing optional audio: "+name);}
            busy=new double[scoreVoices?.Length??0];gains=new float[busy.Length];
            game.SettingsChanged+=ApplyVolumes;game.Changed+=Observe;game.SessionChanged+=Observe;
            if(clips.TryGetValue("ambient",out var ambient)){ambience.clip=ambient;ambience.loop=true;ambience.spatialize=false;ambience.spatialBlend=0;ambience.Play();}
            ApplyVolumes();Observe();
            Debug.Log("[Orbital Loom] Score ready: clips="+clips.Count+", bpm="+LoomScore.Bpm+", voices="+busy.Length);
        }
        private void ApplyVolumes()
        {
            foreach(var source in voices)source.volume=game.Settings.effectsVolume;
            ambience.volume=game.Settings.ambienceVolume*.38f;
            if(scoreVoices!=null)for(int i=0;i<scoreVoices.Length;i++)scoreVoices[i].volume=game.Settings.rhythmVolume*(gains==null?0:gains[i]);
            Observe();
        }
        private bool CanRun=>game!=null&&!game.Paused&&!game.UiOverlayOpen&&game.State==GameState.LevelPlaying&&
            session!=null&&session.Level.syncWindow>0&&session.Running&&!session.Paused;
        private void Observe()
        {
            if(game==null)return;
            if(game.Paused){if(ambience.isPlaying)ambience.Pause();}
            else if(!ambience.isPlaying&&ambience.clip!=null)ambience.UnPause();
            if(session!=game.BoxSession||evaluation!=game.BoxSession?.Result)
            {
                Cancel();session=game.BoxSession;evaluation=session?.Result;
                cues=session==null?new List<LoomCue>():LoomScore.Build(session.Level,evaluation);
                cursor=0;ScheduledCueCount=DroppedCueCount=0;
            }
            if(!CanRun)
            {
                bool naturalFinish=session!=null&&session.Solved&&!game.Paused&&!game.UiOverlayOpen&&game.State==GameState.LevelPlaying;
                if(naturalFinish)clockRunning=false; // Let the final chord decay.
                else if(clockRunning||ringing)Cancel();
                return;
            }
            if(clockRunning)return;
            // Start in the future so Android has time to fill its audio buffer.
            origin=AudioSettings.dspTime+.12-session.Elapsed*LoomScore.SecondsPerBeat;
            cursor=0;while(cursor<cues.Count&&cues[cursor].beat<session.Elapsed-.001f)cursor++;
            clockRunning=true;
        }
        private void Update()
        {
            if(game==null)return;Observe();
            if(!clockRunning)return;
            double now=AudioSettings.dspTime;
            while(cursor<cues.Count&&origin+cues[cursor].beat*LoomScore.SecondsPerBeat<=now+.18)
            {
                var cue=cues[cursor++];double at=origin+cue.beat*LoomScore.SecondsPerBeat;
                // Avoid catch-up bursts after stalls or fast stepping. A missed
                // cue must never become a cluster of old notes in the next frame.
                if(at<now-.04){DroppedCueCount++;continue;}
                Schedule(cue,Math.Max(now+.005,at));
            }
        }
        public bool TryClockDelta(EnergyBoxSession current,out float delta)
        {
            delta=0;if(game==null||current==null||current.Level.syncWindow<=0)return false;
            Observe();if(!clockRunning)return true;
            double beat=Math.Max(0,(AudioSettings.dspTime-origin)/LoomScore.SecondsPerBeat);
            delta=Math.Max(0,(float)beat-current.Elapsed);return true;
        }
        private void Schedule(LoomCue cue,double at)
        {
            if(!clips.TryGetValue(cue.clip,out var clip))return;
            for(int i=0;i<busy.Length;i++)if(busy[i]<=AudioSettings.dspTime)
            {
                var source=scoreVoices[i];source.clip=clip;source.pitch=1;source.spatialize=false;source.spatialBlend=0;
                source.panStereo=Mathf.Clamp(cue.pan,-.35f,.35f);gains[i]=cue.gain;source.volume=game.Settings.rhythmVolume*cue.gain;
                source.PlayScheduled(at);busy[i]=at+clip.length;ScheduledCueCount++;ringing=true;return;
            }
            DroppedCueCount++;
        }
        private void Cancel()
        {
            if(scoreVoices!=null)foreach(var source in scoreVoices)if(source!=null)source.Stop();
            if(busy!=null)Array.Clear(busy,0,busy.Length);clockRunning=ringing=false;
        }
        public void Play(string key,Vector3 position)
        {
            if(game==null||!clips.TryGetValue(key,out var clip)||voices.Length==0)return;
            var source=voices[next++%voices.Length];source.transform.localPosition=position;source.clip=clip;
            source.volume=game.Settings.effectsVolume;source.spatialize=false;source.spatialBlend=0;source.panStereo=Mathf.Clamp(position.x,-.4f,.4f);source.Play();
        }
        private void OnApplicationPause(bool paused){if(paused)Cancel();}
        private void OnDestroy(){Cancel();if(game!=null){game.SettingsChanged-=ApplyVolumes;game.Changed-=Observe;game.SessionChanged-=Observe;}if(Instance==this)Instance=null;}
    }
}
