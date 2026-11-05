using System;
using System.Collections;
using System.IO;
using System.Linq;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Core
{
    public enum GameState { MainMenu, ChapterSelect, LevelPlaying, LevelResult, Sandbox, Settings }
    [DefaultExecutionOrder(-200)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = GameState.MainMenu;
        public AppSettings Settings { get; private set; }
        public EnergyBoxCampaign BoxCampaign { get; private set; }
        public EnergyBoxSession BoxSession { get; private set; }
        public EnergyBoxProgress BoxProgress { get; private set; }
        public int BoxLevelIndex { get; private set; } = -1;
        public bool RelicIntroRequested { get; private set; }
        public bool Ready { get; private set; }
        public bool Paused { get; private set; }
        public bool UiOverlayOpen { get; set; }
        public bool VerificationMode { get; set; }
        public string LastError { get; private set; }
        public int AvailableSkins => 1+Math.Min(3,BoxProgress.cleared.Count/6);
        public float EnergyElapsed=>BoxSession?.Elapsed??0;
        public event Action Changed,SessionChanged,SettingsChanged;
        public event Action<string> Notification;
        private GameState returnState;
        private bool resultSaved;
        private float idle;
        private bool prompted;
        private void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;
            Settings=LocalStore.Load("settings-v1.json",()=>new AppSettings());Settings.Sanitize();
            BoxProgress=LocalStore.Load("energy-box-v2.json",()=>new EnergyBoxProgress());
            if(BoxProgress.cleared==null)BoxProgress.cleared=new System.Collections.Generic.List<string>();
            ApplySettings(false);
        }
        private IEnumerator Start()
        {
            yield return null;
            try
            {
                var source=Resources.Load<TextAsset>("EnergyBoxes");
                if(source==null)throw new FormatException("关卡资源缺失");
                BoxCampaign=EnergyBoxCampaign.Parse(source.text);Ready=true;
            }
            catch(Exception error)when(error is FormatException||error is ArgumentException)
            {LastError=error.Message;Debug.LogException(error);}
            // Always enter the menu, including returning players. Continue is an
            // explicit choice and never hides first-run instructions or settings.
            Transition(GameState.MainMenu);
            Debug.Log($"[Orbital Loom] Startup: ready={Ready}, puzzles={BoxCampaign?.levels.Length}, state={State}, error={LastError??"none"}");
        }
        private void Update()
        {
            if(Paused||UiOverlayOpen||State!=GameState.LevelPlaying||BoxSession==null)return;
            var audio=OrbitalLoom.Audio.LoomAudio.Instance;
            float delta=Time.unscaledDeltaTime;
            if(audio!=null&&audio.TryClockDelta(BoxSession,out var clockDelta))delta=clockDelta;
            // Without audio (or when muted) the same 84-BPM logical clock is
            // used. Hearing is never required to solve the puzzle.
            else if(BoxSession.Level.syncWindow>0)delta/=(float)OrbitalLoom.Audio.LoomScore.SecondsPerBeat;
            BoxSession.Tick(delta);idle+=Time.unscaledDeltaTime;
            if(idle>60&&!prompted&&!BoxSession.Solved&&!BoxSession.Running){prompted=true;Notify("可以先读终点条件，再用提示寻找下一步；开始后可暂停并逐段观察。");}
        }
        public void ResumeBox()
        {
            if(!Ready)return;
            int index=Array.FindIndex(BoxCampaign.levels,l=>!BoxProgress.cleared.Contains(l.id));StartBox(index<0?0:index);
        }
        public void StartBox(int index,bool forceTutorial=false)
        {
            if(!Ready||index<0||index>=BoxCampaign.levels.Length)return;
            if(BoxSession!=null)BoxSession.Changed-=OnBoxChanged;
            var level=BoxCampaign.levels[index];bool teaching=index<3||level.tutorial.Length>0;
            BoxLevelIndex=index;BoxSession=new EnergyBoxSession(level,teaching&&(forceTutorial||!Settings.seenTutorials.Contains("box-guide-095-"+index)));
            RelicIntroRequested=level.relic!=null&&(forceTutorial||!Settings.seenTutorials.Contains("relic-guide-094-"+level.id));
            BoxSession.Changed+=OnBoxChanged;resultSaved=false;Transition(GameState.LevelPlaying);
            Debug.Log($"[Orbital Loom] Energy box ready: {level.id}, sockets={BoxSession.Sockets.Length}");
        }
        private void OnBoxChanged()
        {
            Activity();var guide=BoxSession.Tutorial;string key="box-guide-095-"+BoxLevelIndex;
            string relicKey="relic-guide-094-"+BoxSession.Level.id;
            if(BoxSession.Observation?.Found==true&&(BoxSession.Level.spatial==null||BoxSession.SpatialLessonCompleted)&&!Settings.seenTutorials.Contains(relicKey))
            {Settings.seenTutorials=Settings.seenTutorials.Concat(new[]{relicKey}).ToArray();ApplySettings();}
            if((guide.Completed||guide.Dismissed)&&!Settings.seenTutorials.Contains(key)){Settings.seenTutorials=Settings.seenTutorials.Concat(new[]{key}).ToArray();ApplySettings();}
            if(!BoxSession.Solved)resultSaved=false;
            else if(!resultSaved)
            {
                resultSaved=true;if(!BoxProgress.cleared.Contains(BoxSession.Level.id))BoxProgress.cleared.Add(BoxSession.Level.id);
                TrySave(()=>LocalStore.Save("energy-box-v2.json",BoxProgress));
                OrbitalLoom.Audio.LoomAudio.Instance?.Play("win",Vector3.zero);
            }
            SessionChanged?.Invoke();
        }
        public void NextLevel(){if(BoxLevelIndex+1<BoxCampaign.levels.Length)StartBox(BoxLevelIndex+1);else MainMenu();}
        public void MainMenu(){Transition(GameState.MainMenu);}
        public void ReplayBoxTutorial(){StartBox(0,true);}
        public void OpenSettings(){returnState=State;Transition(GameState.Settings);}
        public void CloseSettings(){ApplySettings();Transition(returnState);}
        public void ResetLevel(){if(BoxSession!=null){resultSaved=false;BoxSession.Reset();}}
        public void Undo(){BoxSession?.Undo();}
        public void Hint(){BoxSession?.Hint();}
        public void Notify(string text){Notification?.Invoke(text);}
        public void Activity(){idle=0;prompted=false;}
        public void ApplySettings(bool persist=true)
        {
            Settings.Sanitize();Settings.skin=Math.Min(Settings.skin,AvailableSkins-1);AudioListener.volume=Settings.masterVolume;
            QualitySettings.SetQualityLevel(Math.Min(Settings.quality,Math.Max(0,QualitySettings.names.Length-1)),false);
            if(persist)TrySave(()=>LocalStore.Save("settings-v1.json",Settings));SettingsChanged?.Invoke();
        }
        private void Transition(GameState value){State=value;Activity();Changed?.Invoke();Debug.Log("[Orbital Loom] State="+State);}
        private void TrySave(Action write)
        {
            if(VerificationMode)return;
            try{write();}catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is PlatformNotSupportedException){Debug.LogException(e);Notify("本次进度暂未写入，请检查存储空间。");}
        }
        private void OnApplicationPause(bool value){Paused=value;if(value&&Settings!=null)ApplySettings();Changed?.Invoke();}
        private void OnApplicationQuit(){if(Settings!=null)ApplySettings();}
        private void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
