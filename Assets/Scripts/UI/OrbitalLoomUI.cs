using System;
using System.Linq;
using ByteDance.PICO.SpatialAdapter.Component;
using OrbitalLoom.Core;
using OrbitalLoom.Interaction;
using OrbitalLoom.Rendering;
using OrbitalLoom.WindowSystem;
using UnityEngine;
namespace OrbitalLoom.UI
{
    public sealed partial class OrbitalLoomUI:MonoBehaviour
    {
        public SpatialButton[] buttons;public SpatialAdapterNativeText[] labels;public SpatialSlider[] sliders;
        public MeshRenderer[] panels;public SpatialAdapterNativeText toast;
        public SpatialInputRouter input;public SpatialWindowManager windows;public Transform menuSculpture;
        private GameManager game;private int buttonIndex,labelIndex,sliderIndex,panelIndex;
        private string overlay;private float toastUntil;private int settingsPage,lessonPage;
        private SpatialAdapterNativeText beatLabel;private int shownBeat=-1;
        private static readonly string[] ChapterNames={"初光 · 能量与印记","密语 · 条件与联动","棱镜 · 分配与倒序","回响 · 退印与节拍"};
        private void Start(){game=GameManager.Instance;game.Changed+=OnStateChanged;game.SessionChanged+=OnSessionChanged;game.Notification+=ShowToast;Build();}
        private void OnDestroy(){if(game==null)return;game.Changed-=OnStateChanged;game.SessionChanged-=OnSessionChanged;game.Notification-=ShowToast;}
        private void OnStateChanged(){overlay=game.State==GameState.LevelPlaying&&game.RelicIntroRequested&&game.BoxSession?.Observation?.Found!=true?"box-relic":null;Build();}
        private void OnSessionChanged(){if(game.State==GameState.LevelPlaying)Build();}
        private void Update()
        {
            if(toast.gameObject.activeSelf&&Time.unscaledTime>toastUntil)toast.gameObject.SetActive(false);
            if(beatLabel==null||game.BoxSession==null)return;
            int beat=Mathf.FloorToInt(game.BoxSession.Elapsed);
            if(beat==shownBeat)return;shownBeat=beat;
            beatLabel.Text="第 "+beat+" 拍   ·   84 BPM   ·   "+(beat%4==0?"● ○ ○ ○":beat%4==1?"○ ● ○ ○":beat%4==2?"○ ○ ● ○":"○ ○ ○ ●");
        }
        public void ShowToast(string message){toast.Text=BoxWrap(message,36);toast.TextColor=SpatialUITheme.Ink;toast.BackgroundColor=SpatialUITheme.Panel;toast.BackgroundCornerRadius=16;toastUntil=Time.unscaledTime+4;toast.gameObject.SetActive(true);}
        private void Build()
        {
            if(game==null)return;beatLabel=null;shownBeat=-1;game.UiOverlayOpen=overlay!=null;toast.gameObject.SetActive(false);input.CancelAll();
            foreach(var b in buttons)b.gameObject.SetActive(false);foreach(var l in labels)l.gameObject.SetActive(false);foreach(var s in sliders)s.gameObject.SetActive(false);
            foreach(var panel in panels)panel.gameObject.SetActive(false);buttonIndex=labelIndex=sliderIndex=panelIndex=0;
            bool menu=game.State==GameState.MainMenu&&overlay==null;menuSculpture.gameObject.SetActive(menu);input.boxView?.Refresh();
            input.boxView?.relic?.Bind(game.BoxSession,overlay=="box-relic",overlay==null&&game.State==GameState.LevelPlaying);
            if(!game.Ready){Panel(0,0,1.0f,.42f);Text("星轨织机",0,.1f,.9f,.08f,32);Text(game.LastError??"星仪正在苏醒",0,-.06f,1.0f,.1f,18);return;}
            if(!IsWorkspace&&!menu)Panel(0,0,1.50f,1.42f);
            if(overlay!=null){if(!BoxOverlay())GeneralOverlay();return;}
            if(game.State==GameState.MainMenu)MainMenu();else if(game.State==GameState.Settings)Settings();else if(game.State==GameState.LevelPlaying)BoxPlaying();
        }
        private void MainMenu()
        {
            // The ruin and crystal sculpture stays left of an opaque reading
            // surface. Decoration never crosses the native button hit regions.
            Panel(.26f,.02f,.80f,1.12f);
            Text("O R B I T A L   L O O M",.26f,.46f,.78f,.04f,13,SpatialUITheme.Secondary);
            Text("星轨织机",.26f,.32f,.78f,.09f,35);
            Text("唤醒遗迹，让水晶重述星光",.26f,.20f,.78f,.06f,15,SpatialUITheme.Secondary);
            Text("24 道手作谜题   /   4 个星域",.26f,.105f,.78f,.05f,14,SpatialUITheme.Secondary);
            Button(game.BoxProgress.cleared.Count==0?"开始旅程":"继续旅程",.26f,-.035f,.57f,game.ResumeBox,true,height:.12f);
            Button("星域选关",.26f,-.19f,.57f,()=>{boxPage=0;OpenOverlay("box-select");},height:.10f);
            Button("教学档案",.10f,-.34f,.25f,()=>OpenOverlay("lessons"),height:.09f);
            Button("设置",.42f,-.34f,.25f,game.OpenSettings,height:.09f);
            Text("已解开 "+game.BoxProgress.cleared.Count+" / 24",.26f,-.47f,.75f,.04f,13,SpatialUITheme.Secondary);
        }
        private void Settings()
        {
            Back(game.CloseSettings);Text("观测设置",0,.47f,1.0f,.08f,29);
            if(settingsPage==0)
            {
                Text("声音与舒适度",0,.34f,1.0f,.05f,16,SpatialUITheme.Secondary);
                Slider("总音量",-.32f,.17f,game.Settings.masterVolume,v=>game.Settings.masterVolume=v);
                Slider("操作音效",.32f,.17f,game.Settings.effectsVolume,v=>game.Settings.effectsVolume=v);
                Slider("环境声",-.32f,-.04f,game.Settings.ambienceVolume,v=>game.Settings.ambienceVolume=v);
                Slider("节拍音乐",.32f,-.04f,game.Settings.rhythmVolume,v=>game.Settings.rhythmVolume=v);
                Toggle("舒适锚定",-.25f,-.27f,game.Settings.comfortMode,()=>{game.Settings.comfortMode=!game.Settings.comfortMode;game.ApplySettings();Build();});
                Toggle("装饰演出",.25f,-.27f,game.Settings.particles,()=>{game.Settings.particles=!game.Settings.particles;game.ApplySettings();Build();});
                Button("重新居中",-.24f,-.46f,.39f,windows.ResetContent);
                Button("显示与关于 ›",.24f,-.46f,.39f,()=>{settingsPage=1;Build();});
            }
            else
            {
                Slider("星仪大小",0,.25f,Mathf.InverseLerp(.75f,1.1f,game.Settings.contentScale),v=>game.Settings.contentScale=Mathf.Lerp(.75f,1.1f,v));
                for(int i=0;i<3;i++){int q=i;Button(new[]{"轻量","平衡","精细"}[i],-.30f+i*.30f,.04f,.25f,()=>{game.Settings.quality=q;game.ApplySettings();Build();});buttons[buttonIndex-1].SetSelected(game.Settings.quality==i);}
                Text("0.9.8   /   独立 MR 空间\n24 道遗迹谜题 · 一步一引导\n无账号 · 无内购 · 进度保存在本机",0,-.18f,1.15f,.24f,18);
                Button("‹ 声音与舒适度",0,-.46f,.48f,()=>{settingsPage=0;Build();});
            }
        }
        private void GeneralOverlay()
        {
            Back(()=>{overlay=null;Build();});Text("教学档案",0,.47f,1.0f,.08f,29);
            Text("每段教学都需要你实际观察、装件和验证",0,.34f,1.25f,.05f,15,SpatialUITheme.Secondary);
            if(lessonPage==2)
            {
                int[] samples={14,17,22};string[] names={"双桥接合 · 第15关","镜面寻路 · 第18关","折返共鸣 · 第23关"};
                for(int i=0;i<samples.Length;i++){int index=samples[i];Button(names[i],0,.14f-i*.21f,.76f,()=>{overlay=null;game.StartBox(index,true);},height:.13f);}
                Text("机关方向与实际长度参与解题；支持坐姿观察",0,-.38f,1.3f,.08f,15,SpatialUITheme.Secondary);
                Button("‹ 新机关教学",0,-.55f,.48f,()=>{lessonPage=1;Build();});return;
            }
            int[] indices=lessonPage==0?new[]{0,1,2,6,8}:new[]{10,12,15,18,21};
            string[] titles=lessonPage==0?new[]{"基本操作","能量等分","符文顺序","条件门","末印替换"}:new[]{"联动插槽","比例棱镜","倒序镜","退印台","同拍抵达"};
            for(int i=0;i<indices.Length;i++){int index=indices[i];Button(titles[i],0,.19f-i*.135f,.68f,()=>{overlay=null;game.StartBox(index,true);},height:.095f);}
            Button(lessonPage==0?"新机关教学 ›":"‹ 基础教学",lessonPage==0?0:-.25f,-.55f,.43f,()=>{lessonPage=1-lessonPage;Build();});
            if(lessonPage==1)Button("空间观察教学 ›",.25f,-.55f,.43f,()=>{lessonPage=2;Build();});
        }
        private void OpenOverlay(string name) { overlay = name; Build(); }
        private void Back(Action action) { Button("‹ 返回", -.55f, .61f, .23f, action); }
        // Reserve the middle 1.1 m for the puzzle. HUD controls live outside it;
        // menus use a separate, less wide composition with more vertical rhythm.
        private bool IsWorkspace => (overlay == null||overlay=="box-relic") && game.State == GameState.LevelPlaying;
        // Keep gameplay tools within a seated player's comfortable lower field
        // of view. The former 1.18 vertical spread put the toolbar near the floor.
        private Vector3 UiPosition(float x, float y) => new Vector3(x * (IsWorkspace ? 1.85f : 1.5f),
            IsWorkspace ? y * .9f + .07f : y * 1.18f, -.54f);
        private SpatialAdapterNativeText Text(string text, float x, float y, float width, float height, int size, Color? color = null)
        {
            var label = labels[labelIndex++];
            label.transform.localPosition = UiPosition(x, y);
            // Native text uses SDK font units, independent of its meter-sized box.
            // Calibrated against rendered output so body copy remains readable.
            label.Text = text; label.TextSize = Mathf.RoundToInt(size * 2.5f); label.BackgroundSize = new Vector2(width * 1.25f, height * 1.12f);
            label.TextColor = color ?? SpatialUITheme.Ink; label.BackgroundColor = Color.clear; label.BackgroundCornerRadius=0;
            label.Justification = TextJustification.Center;
            label.gameObject.SetActive(true); return label;
        }
        private void Panel(float x,float y,float width,float height)
        {
            if(panels==null||panelIndex>=panels.Length)return;
            var panel=panels[panelIndex++];panel.transform.localPosition=UiPosition(x,y)+new Vector3(0,0,.04f);
            panel.transform.localScale=new Vector3(width*1.25f,height*1.12f,.008f);
            LoomPalette.SetColor(panel,SpatialUITheme.Panel);panel.gameObject.SetActive(true);
        }
        private void Button(string text, float x, float y, float width, Action action, bool primary = false, bool enabled = true, float height = .078f)
        {
            var button = buttons[buttonIndex++];
            var position = UiPosition(x, y); position.z = -.56f;
            button.Bind(text, position, new Vector2(width * 1.25f, height * 1.12f), action, primary, enabled);
            button.gameObject.SetActive(true);
        }
        private void Slider(string name, float x, float y, float value, Action<float> set)
        {
            var slider = sliders[sliderIndex++];
            var position = UiPosition(x, y); position.z = -.56f;
            slider.Bind(name, position, value, v => { set(v); game.ApplySettings(); });
            slider.gameObject.SetActive(true);
        }
        private SpatialButton Tool(string text, float x, float y, LoomIcon symbol, Action action, bool primary = false)
        {
            var button = buttons[buttonIndex++];
            var position = UiPosition(x, y); position.z = -.56f;
            button.Bind(text, position, new Vector2(.15f, .13f), action, primary, true, symbol);
            button.gameObject.SetActive(true);
            return button;
        }
        private void Toggle(string text, float x, float y, bool value, Action action)
        {
            Button(text, x, y, .39f, action);
            buttons[buttonIndex - 1].SetToggle(value);
        }
    }
}
