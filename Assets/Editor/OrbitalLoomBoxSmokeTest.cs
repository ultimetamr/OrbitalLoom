using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ByteDance.PICO.SpatialAdapter;
using OrbitalLoom.Core;
using OrbitalLoom.Interaction;
using OrbitalLoom.Rendering;
using OrbitalLoom.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Phase=UnityEngine.InputSystem.TouchPhase;

namespace OrbitalLoom.Editor
{
    [InitializeOnLoad]
    public static class OrbitalLoomBoxSmokeTest
    {
        private const string Active="OrbitalLoom.BoxSmoke";
        private static double deadline,next;
        static OrbitalLoomBoxSmokeTest(){EditorApplication.update+=Tick;}
        public static void Begin(){SessionState.SetInt(Active,1);EditorApplication.isPlaying=true;}
        private static void Tick()
        {
            int step=SessionState.GetInt(Active,0);if(step==0||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            var game=GameManager.Instance;if(game==null||!game.Ready)return;game.VerificationMode=true;
            try
            {
                if(step==1)
                {
                    Require(game.State==GameState.MainMenu&&game.BoxSession==null,"Default startup is menu without active puzzle");
                    Require(!UnityEngine.Object.FindObjectsOfType<Transform>(true).Any(t=>t.name=="Ring HUD surfaces"),"Legacy interface plates removed from scene");
                    CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-090-MainMenu.png");Click(game.BoxProgress.cleared.Count==0?"开始旅程":"继续旅程");game.StartBox(0,true);
                    deadline=EditorApplication.timeSinceStartup+240;next=EditorApplication.timeSinceStartup+1;SessionState.SetInt(Active,2);
                }
                else if(step==2&&EditorApplication.timeSinceStartup>next)
                {
                    var view=UnityEngine.Object.FindObjectOfType<EnergyBoxView>();
                    Require(view.assembly.gameObject.activeInHierarchy&&view.transform.position.y>1.2f,"Apparatus visible at comfortable height");
                    CheckAccess(view);CheckLayout();CheckTestEmphasis(false);CheckArt(view);OrbitalLoomSmokeTest.Capture("Builds/Preview-Box-Start.png",1600,1200);
                    CheckSingleTutorialFocus();
                    OrbitalLoomSmokeTest.Capture("Builds/Preview-096-Tutorial-Start.png",1600,1200);
                    Click("开始教学");Require(game.BoxSession.Tutorial.Stage==1,"Teaching waits for real selection");CheckSingleTutorialFocus();
                    OrbitalLoomSmokeTest.Capture("Builds/Preview-096-Tutorial-Socket.png",1600,1200);
                    // Real official device -> InputAction -> router -> socket selection.
                    CheckOfficialPress(view);
                    CheckSingleTutorialFocus();OrbitalLoomSmokeTest.Capture("Builds/Preview-096-Tutorial-Install.png",1600,1200);
                    Click("装转向件");Require(game.BoxSession.Choice(0)==1,"Native install button");
                    CheckSingleTutorialFocus();OrbitalLoomSmokeTest.Capture("Builds/Preview-096-Tutorial-Exit.png",1600,1200);
                    CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-UI-Selected.png",1600,1200);
                    Click("出口 ›");Require(game.BoxSession.Choice(0)==2,"Discrete authored exit choice");
                    Require(game.BoxSession.Tutorial.Stage==4,"Committed turn advances teaching");
                    CheckTestEmphasis(true);
                    CheckSingleTutorialFocus();OrbitalLoomSmokeTest.Capture("Builds/Preview-096-Tutorial-Test.png",1600,1200);
                    OrbitalLoomSmokeTest.Capture("Builds/Preview-Box-Tutorial.png",1600,1200);
                    game.BoxSession.SkipTutorial();CheckTestEmphasis(false);Click("撤销");Require(game.BoxSession.Choice(0)==1,"Undo route change");
                    Click("开始");Require(game.BoxSession.Running,"Explicit test button");Click("暂停");
                    CheckTestEmphasis(false);
                    Require(game.BoxSession.Paused,"Pause timeline");Click("下一段");Require(game.BoxSession.Elapsed>0,"Step reaches next junction");
                    Click("停止");Require(!game.BoxSession.Running&&!game.BoxSession.Solved,"Stop does not solve");
                    game.BoxSession.Test();game.BoxSession.Tick(100);Require(!game.BoxSession.Solved,"Wrong route fails");
                    next=EditorApplication.timeSinceStartup+.3;SessionState.SetInt(Active,3);
                }
                else if(step==3&&EditorApplication.timeSinceStartup>next)
                {
                    OrbitalLoomSmokeTest.Capture("Builds/Preview-Box-Failure.png",1600,1200);
                    int attempts=game.BoxSession.Attempts;Click("目标");CheckLayout();
                    OrbitalLoomSmokeTest.Capture("Builds/Preview-095-Diagnosis.png",1600,1200);
                    Click("首错复盘");Require(game.BoxSession.ReviewingFailure&&game.BoxSession.Paused,"Actual native review callback freezes causal event");
                    Require(game.BoxSession.Attempts==attempts,"Review is not another attempt");CheckLayout();CheckAccess(UnityEngine.Object.FindObjectOfType<EnergyBoxView>());
                    OrbitalLoomSmokeTest.Capture("Builds/Preview-095-Review.png",1600,1200);Click("停止");
                    CheckGuidedTutorials();
                    game.ResetLevel();SessionState.SetInt(Active,4);SessionState.SetInt("OrbitalLoom.BoxIndex",0);next=0;
                }
                else if(step==4)
                {
                    int index=SessionState.GetInt("OrbitalLoom.BoxIndex",0);game.StartBox(index,index==2||game.BoxCampaign.levels[index].tutorial.Length>0);
                    if(index==2){Click("开始教学");var clues=UnityEngine.Object.FindObjectOfType<EnergyBoxView>();Press(clues.nodes.First(n=>n.Id=="g1").target);Require(game.BoxSession.Tutorial.Stage==9,"Read goal lock");OrbitalLoomSmokeTest.Capture("Builds/Preview-Box-RuneLesson.png",1600,1200);Press(clues.nodes.First(n=>n.Id=="rs").target);Require(!game.BoxSession.Tutorial.Completed&&game.BoxSession.Tutorial.Stage==12,"Rune teaching now requires practical completion");}
                    else if(game.BoxCampaign.levels[index].tutorial.Length>0)
                    {
                        Click("开始教学");var clues=UnityEngine.Object.FindObjectOfType<EnergyBoxView>();
                        Press(clues.nodes.First(n=>n.Id==game.BoxSession.Level.tutorialNode).target);
                        Require(game.BoxSession.Tutorial.Stage!=11,"Advanced mechanic inspected "+index);
                        OrbitalLoomSmokeTest.Capture("Builds/Preview-Advanced-Teaching-"+(index+1)+".png",1600,1200);
                    }
                    else game.BoxSession.SkipTutorial();
                    var view=UnityEngine.Object.FindObjectOfType<EnergyBoxView>();
                    if(game.BoxSession.Observation!=null)CheckRelic(view,index);
                    CheckAccess(view);
                    var box=game.BoxSession;
                    if(index==6){Press(view.nodes.First(n=>n.Id=="a").target);Click("装转向件");CheckSingleTutorialFocus();box.SkipTutorial();Click("开始");box.Tick(100);Click("目标");OrbitalLoomSmokeTest.Capture("Builds/Preview-Advanced-Diagnosis.png",1600,1200);Click("定位错误");Require(box.HintTarget=="door","Failure localized");game.StartBox(index,true);box=game.BoxSession;Click("开始教学");Press(view.nodes.First(n=>n.Id==box.Level.tutorialNode).target);}
                    for(int i=0;i<box.Sockets.Length;i++)
                    {
                        if(box.Choice(i)==box.Level.reference[i])continue;
                        var node=view.nodes.First(n=>n.Id==box.Sockets[i].id);Press(node.target);
                        Require(box.Selected==i,"Socket callback selection "+i);
                        var wanted=box.Sockets[i].choices[box.Level.reference[i]-1];Click(wanted.kind=="split"?(box.Sockets[i].choices.Any(c=>c.parts.Length>0)?"装棱镜":"装等分件"):"装转向件");
                        CheckSingleTutorialFocus();
                        if(box.Tutorial.Active&&box.Tutorial.Stage==14){Click("出口 ›");CheckSingleTutorialFocus();}
                        int guard=0;while(box.Choice(i)!=box.Level.reference[i]&&guard++<10)Click("出口 ›");
                        Require(box.Choice(i)==box.Level.reference[i],"UI reaches reference direction "+i);
                        CheckSingleTutorialFocus();
                    }
                    CheckLayout();CheckAccess(view);
                    view.RotateView(1);CheckAccess(view);if(index==11)OrbitalLoomSmokeTest.Capture("Builds/Preview-Art080-Depth.png",1600,1200);view.RotateView(-1);Require(Math.Abs(view.Yaw)<.1f,"Inspection restores front");
                    Require(!box.Solved,"Layout alone cannot clear level");
                    if(index==14||index==22||index==23)
                    {
                        Click("目标");Click("联动表");CheckLayout();
                        OrbitalLoomSmokeTest.Capture("Builds/Preview-092-Links-"+(index+1)+".png",1600,1200);
                        Click("‹ 返回");Click("回到机关");
                    }
                    Click("开始");next=EditorApplication.timeSinceStartup+.25;SessionState.SetInt(Active,5);
                }
                else if(step==5&&EditorApplication.timeSinceStartup>next)
                {
                    int index=SessionState.GetInt("OrbitalLoom.BoxIndex",0);
                    OrbitalLoomSmokeTest.Capture("Builds/Preview-Box-Level-"+(index+1)+".png",1600,1200);
                    if(index==11)OrbitalLoomSmokeTest.Capture("Builds/Preview-Box-Complex.png",1600,1200);
                    game.BoxSession.Tick(100);Require(game.BoxSession.Solved,"Reference completion "+index);
                    if(game.BoxCampaign.levels[index].tutorial.Length>0)Require(game.BoxSession.Tutorial.Completed,"Advanced teaching completed through UI "+index);
                    CheckLayout();CheckTestEmphasis(false);SessionState.SetInt(Active,6);next=EditorApplication.timeSinceStartup+(game.BoxSession.Observation!=null?2.6:.2);
                }
                else if(step==6&&EditorApplication.timeSinceStartup>next)
                {
                    int index=SessionState.GetInt("OrbitalLoom.BoxIndex",0);
                    if(game.BoxSession.Observation!=null)
                    {
                        var relic=UnityEngine.Object.FindObjectOfType<EnergyBoxView>().relic;
                        Require(relic.heart.gameObject.activeInHierarchy&&relic.RestorationAge>2,"Crystal rises after completion");
                        OrbitalLoomSmokeTest.Capture("Builds/Preview-093-Restored-"+(index+1)+".png",1600,1200);
                        bool effects=game.Settings.particles;game.Settings.particles=false;
                        typeof(RelicView).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(relic,null);
                        Require(Mathf.Abs(relic.heart.localPosition.y+.16f)<.001f,"Reduced motion retains a static completed altar");game.Settings.particles=effects;
                    }
                    if(index==game.BoxCampaign.levels.Length-1)
                    {
                        OrbitalLoomSmokeTest.Capture("Builds/Preview-Box-Solved.png",1600,1200);
                        var art=UnityEngine.Object.FindObjectOfType<EnergyBoxView>().art;
                        Require(art.winShards.Any(s=>s.gameObject.activeSelf),"Solved constellation plays");
                        bool effects=game.Settings.particles;game.Settings.particles=false;
                        typeof(EnergyBoxArt).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(art,null);
                        Require(art.winShards.Concat(art.tails).Concat(art.echoes).All(s=>!s.gameObject.activeSelf),"Reduced-effects preference removes decorative motion");game.Settings.particles=effects;
                        Click("完成挑战");Require(game.State==GameState.MainMenu,"Completion returns to menu");CheckLayout();
                        Click("星域选关");
                        for(int page=0;page<4;page++){CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-090-Selection-"+(page+1)+".png",1600,1200);if(page<3)Click("下一星域 ›");}
                        var levelCard=UnityEngine.Object.FindObjectOfType<OrbitalLoomUI>().buttons.First(b=>b.gameObject.activeInHierarchy&&b.Caption.StartsWith("20 "));
                        Click(levelCard.Caption);CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-095-Curriculum.png",1600,1200);Click("‹ 返回");
                        Click("‹ 返回");Click("教学档案");CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-090-Lessons-1.png",1600,1200);Click("新机关教学 ›");CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-090-Lessons-2.png",1600,1200);
                        Click("空间观察教学 ›");CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-093-Lessons.png",1600,1200);Click("‹ 返回");
                        OrbitalLoomSmokeTest.Capture("Builds/Preview-UI-Menu.png",1600,1200);
                        Click(game.BoxProgress.cleared.Count==0?"开始旅程":"继续旅程");Require(game.BoxSession!=null,"Menu opens new game");
                        game.OpenSettings();CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-UI-Settings.png",1600,1200);Click("显示与关于 ›");CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-092-DisplaySettings.png",1600,1200);game.CloseSettings();Require(game.BoxSession!=null,"Settings retains mechanism");
                        Click("更多");CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-UI-More.png",1600,1200);Click("机关规则");OrbitalLoomSmokeTest.Capture("Builds/Preview-UI-Help.png",1600,1200);Click("进阶机关 ›");CheckLayout();Click("新机关 ›");CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-090-NewRules.png",1600,1200);Click("‹ 返回");Click("回到机关");
                        Click("更多");Click("重新开始");Click("继续思考");Require(game.BoxSession!=null,"Reset cancel keeps layout");
                        Click("更多");Click("重新开始");Click("清空布局");Require(game.BoxSession.Moves==0,"Confirmed reset");
                        CheckReusedButtonStates();
                        File.WriteAllText("Builds/EnergyBox-Smoke.txt","PASS\nTwenty-four levels solved through actual socket/install/direction/test UI callbacks.\nAdvanced gate/converter/link teaching, goal report and failure localization.\nOfficial SpatialInputDevice input and duplicate event suppression.\nWrong route, undo, pause, step, stop, view rotation, selection pages and reset.\nCamera rays reach active sockets/clues/buttons at front and 20 degrees.\nButton bounds and native text contrast checked.\nEditor evidence only; physical rendering, comfort and performance require headset testing.\n"+DateTimeOffset.Now.ToString("o"));
                        SessionState.SetInt(Active,7);next=0;
                    }
                    else{Click("下一关");Require(game.BoxLevelIndex==index+1,"Next level");SessionState.SetInt("OrbitalLoom.BoxIndex",index+1);SessionState.SetInt(Active,4);}
                }
                else if(step==7)
                {
                    game.StartBox(22);game.BoxSession.SkipTutorial();
                    for(int i=0;i<game.BoxSession.Sockets.Length;i++)game.BoxSession.Set(i,game.BoxSession.Level.reference[i]);
                    game.BoxSession.Test();SessionState.SetInt(Active,8);next=EditorApplication.timeSinceStartup+.65;
                }
                else if(step==8&&EditorApplication.timeSinceStartup>next)
                {
                    var audio=OrbitalLoom.Audio.LoomAudio.Instance;
                    Require(audio!=null&&audio.scoreVoices.Length==24&&audio.ScheduledCueCount>0,"DSP score is scheduled on authored voices");
                    Click("暂停");SessionState.SetFloat("OrbitalLoom.ScorePaused",game.BoxSession.Elapsed);
                    SessionState.SetInt("OrbitalLoom.ScoreCues",audio.ScheduledCueCount);
                    SessionState.SetInt(Active,9);next=EditorApplication.timeSinceStartup+.35;
                }
                else if(step==9&&EditorApplication.timeSinceStartup>next)
                {
                    var audio=OrbitalLoom.Audio.LoomAudio.Instance;
                    Require(Mathf.Abs(game.BoxSession.Elapsed-SessionState.GetFloat("OrbitalLoom.ScorePaused",-1))<.001f,"Paused DSP clock cannot advance gameplay");
                    Require(audio.ScheduledCueCount==SessionState.GetInt("OrbitalLoom.ScoreCues",-1)&&audio.scoreVoices.All(v=>!v.isPlaying),"Pause cancels scheduled notes without replay");
                    Click("下一段");Require(game.BoxSession.Elapsed>SessionState.GetFloat("OrbitalLoom.ScorePaused",-1),"Manual step advances while score is paused");
                    Click("继续");SessionState.SetInt(Active,10);next=EditorApplication.timeSinceStartup+.45;
                }
                else if(step==10&&EditorApplication.timeSinceStartup>next)
                {
                    Click("停止");Require(OrbitalLoom.Audio.LoomAudio.Instance.scoreVoices.All(v=>!v.isPlaying),"Stop cancels score");
                    Click("开始");SessionState.SetInt(Active,11);next=EditorApplication.timeSinceStartup+.3;
                }
                else if(step==11&&EditorApplication.timeSinceStartup>next)
                {
                    if(game.BoxSession.Solved)
                    {
                        var audio=OrbitalLoom.Audio.LoomAudio.Instance;
                        Require(audio.ScheduledCueCount>=15&&audio.DroppedCueCount==0,"Real-time three-goal score plays without dropped notes");
                        OrbitalLoomSmokeTest.Capture("Builds/Preview-092-ThreeVoices.png",1600,1200);
                        game.MainMenu();Require(audio.scoreVoices.All(v=>!v.isPlaying),"Leaving gameplay cancels chord tails");
                        File.AppendAllText("Builds/EnergyBox-Smoke.txt","\nPASS DSP scheduling, pause, step, resume, stop/retest, real-time three-goal completion and menu cleanup.\n");
                        File.AppendAllText("Builds/EnergyBox-Smoke.txt","PASS geometry-bearing bridge, reflection and folded-path controls; inspection invariance, metric readout, hint UI, restored crystal and reduced motion.\n");
                        File.AppendAllText("Builds/EnergyBox-Smoke.txt","PASS all ten practical tutorials solved on first run using ONLY their visible highlighted node/button; no answer-based correction.\n");
                        Finish("PASS box-smoke");
                    }
                }
                if(deadline>0&&EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Box smoke timeout");
            }
            catch(Exception e){Debug.LogException(e);Finish("FAIL box-smoke\n"+e);}
        }
        private static void CheckRelic(EnergyBoxView view,int index)
        {
            var box=GameManager.Instance.BoxSession;
            if(box.Level.spatial!=null){CheckSpatial(view,index);return;}
            Require(view.relic!=null&&box.Observation!=null,"Authored observation component");
            if(!view.relic.Displaying)Click("遗迹线索");
            Require(!view.assembly.gameObject.activeInHierarchy&&view.relic.Displaying,"Exhibit replaces graph during observation");
            Require(view.relic.rotor.GetComponentsInChildren<Collider>(true).Length==0,"Exhibit cannot block UI rays");
            CheckLayout();CheckAccess(view);Click("记录刻印");Require(!box.Observation.Found,"Wrong face cannot record");
            OrbitalLoomSmokeTest.Capture("Builds/Preview-093-Observe-Before-"+(index+1)+".png",1600,1200);
            int turns=index==14?3:index==17?12:-3;
            for(int t=0;t<Math.Abs(turns);t++)Click(turns>0?"转动 ›":"‹ 转动");
            Require(box.Observation.Aligned,"Hand controller rotation presents clue face");
            CheckLayout();CheckAccess(view);
            OrbitalLoomSmokeTest.Capture("Builds/Preview-093-Observe-Aligned-"+(index+1)+".png",1600,1200);
            Click("记录刻印");Require(box.Observation.Found&&box.Moves==0,"Record clue independently of moves");
            OrbitalLoomSmokeTest.Capture("Builds/Preview-093-Observe-Found-"+(index+1)+".png",1600,1200);
            Click("返回机关");Require(view.assembly.gameObject.activeInHierarchy&&!view.relic.Displaying,"Return retains playable graph");
            Click("遗迹线索");Require(box.Observation.Found,"Recorded clue can be read again");Click("返回机关");
            Click("提示 3");CheckLayout();OrbitalLoomSmokeTest.Capture("Builds/Preview-093-Hint-"+(index+1)+".png",1600,1200);Click("查看遗迹");Click("返回机关");
            // A fresh session has no recorded evidence; the text alternative
            // is exercised through exactly the same native button callback.
            GameManager.Instance.StartBox(index,true);box=GameManager.Instance.BoxSession;
            Click("文字线索");Require(box.Observation.Found&&box.Observation.UsedTextAccess&&box.Hints==0,"Equivalent text clue is always accessible");
            Click("返回机关");CheckLayout();CheckAccess(view);
        }
        private static void CheckSpatial(EnergyBoxView view,int index)
        {
            var box=GameManager.Instance.BoxSession;var rig=box.Level.spatial;
            if(!view.relic.Displaying)Click("空间机关");
            Require(view.relic.mechanism.gameObject.activeInHierarchy,"Playable spatial apparatus authored");
            Require(view.relic.mechanism.GetComponentsInChildren<Collider>(true).Length==0,"Apparatus cannot block native controls");
            CheckLayout();CheckAccess(view);OrbitalLoomSmokeTest.Capture("Builds/Preview-094-Before-"+(index+1)+".png");
            for(int i=0;i<rig.controls.Length;i++)
            {
                string name=rig.kind=="bridge"?"桥臂 "+(i==0?"H":"I"):rig.kind=="mirror"?"反射镜":new[]{"月路 B","星路 D","日路 E"}[i];
                Click(name);int socket=Array.FindIndex(box.Sockets,n=>n.id==rig.controls[i]);int guard=0;
                while(box.Choice(socket)!=box.Level.reference[socket]&&guard++<8)Click(rig.kind=="path"?"展开一段":"机关 ›");
                Require(box.Choice(socket)==box.Level.reference[socket],"Native physical control reaches pose "+i);
            }
            var before=box.Snapshot();Click("转视角 ›");Click("转视角 ›");
            Require(before.SequenceEqual(box.Snapshot()),"View rotation never edits geometry state");
            CheckLayout();CheckAccess(view);Click("规则与读数");
            OrbitalLoomSmokeTest.Capture("Builds/Preview-094-Geometry-"+(index+1)+".png");Click("规则与读数");
            Require(box.SpatialEdits>0&&!box.Solved,"Manipulation is real but still requires the energy graph");
            Click("返回机关");Require(view.assembly.gameObject.activeInHierarchy,"Return preserves graph");
            Require(before.SequenceEqual(box.Snapshot()),"Returning preserves physical choices");
            Click("提示 3");CheckLayout();Click("查看遗迹");Click("返回机关");
        }
        private static void CheckOfficialPress(EnergyBoxView view)
        {
            var mapping=typeof(SpatialInputSupport).GetField("targetObjects",BindingFlags.Static|BindingFlags.NonPublic);
            var previous=mapping.GetValue(null);var target=view.nodes.First(n=>n.target.controlIndex==0);
            mapping.SetValue(null,new[]{target.core.gameObject});var device=InputSystem.AddDevice<SpatialInputDevice>();
            try
            {
                int changes=0;Action count=()=>changes++;GameManager.Instance.BoxSession.Changed+=count;
                Queue(Phase.Began);InputSystem.Update();Require(GameManager.Instance.BoxSession.Selected==0,"Official controller selects socket on down");
                Queue(Phase.Began);Queue(Phase.Ended);InputSystem.Update();Require(changes==1,"Duplicate Began selects once");
                GameManager.Instance.BoxSession.Changed-=count;
            }
            finally{InputSystem.RemoveDevice(device);mapping.SetValue(null,previous);}
            void Queue(Phase phase)
            {
                InputSystem.QueueStateEvent(device,new SpatialInputDeviceState{
                    primary=new SpatialInputState{interactionId=0,phase=phase,currentPosition=target.transform.position,deviceRotation=Quaternion.identity},
                    secondary=new SpatialInputState{interactionId=-1,phase=Phase.None}});
            }
        }
        private static void CheckTestEmphasis(bool guided)
        {
            var button=UnityEngine.Object.FindObjectsOfType<SpatialButton>().First(b=>b.Caption=="开始");
            button.target.Hover(false);
            float expected=guided?.014f:.008f;
            Require(Mathf.Abs(button.outline.transform.localScale.x-button.surface.localScale.x-expected)<.0001f,
                "Test has emphasis only on its active tutorial step");
            CheckInk(button);
        }
        private static void CheckSingleTutorialFocus()
        {
            var box=GameManager.Instance.BoxSession;if(box==null||!box.Tutorial.Active)return;
            var buttons=UnityEngine.Object.FindObjectsOfType<SpatialButton>();
            var nodes=UnityEngine.Object.FindObjectsOfType<NodeView>();
            int focus=buttons.Count(b=>b.GuidanceVisible)+nodes.Count(n=>n.GuidanceVisible);
            int expected=box.Tutorial.Target!=null||box.Tutorial.Button!=null?1:0;
            Require(focus==expected,"Exactly one current tutorial focus, stage="+box.Tutorial.Stage+", count="+focus);
            foreach(var button in buttons)
            {
                button.target.Hover(true);
                float border=button.outline.transform.localScale.x-button.surface.localScale.x;
                Require(Mathf.Abs(border-(button.GuidanceVisible?.014f:.008f))<.0001f,"Old selection or hover cannot compete with tutorial: "+button.Caption);
                button.target.Hover(false);
            }
        }
        /// <summary>Reproduce a beginner: follow only the visible focus, with
        /// no reference-answer lookup or corrective clicks in the test driver.</summary>
        private static void CheckGuidedTutorials()
        {
            var game=GameManager.Instance;
            foreach(int index in new[]{0,1,2,6,8,10,12,15,18,21})
            {
                game.StartBox(index,true);var box=game.BoxSession;
                for(int step=0;step<48&&box.Tutorial.Active;step++)
                {
                    CheckSingleTutorialFocus();
                    if(box.Running){box.Tick(100);continue;}
                    var guide=box.Tutorial;
                    if(guide.Target!=null)
                    {
                        var view=UnityEngine.Object.FindObjectOfType<EnergyBoxView>();
                        Press(view.nodes.First(n=>n.gameObject.activeInHierarchy&&n.Id==guide.Target).target);
                    }
                    else
                    {
                        Require(guide.Button!=null&&guide.Button!="目标","Visible tutorial path cannot require failure recovery: "+index);
                        Click(guide.Button);
                    }
                    if(index==2&&step<10)
                        OrbitalLoomSmokeTest.Capture("Builds/Preview-098-MoonLesson-"+step+".png",1600,1200);
                }
                Require(box.Solved&&box.Tutorial.Completed&&box.Attempts==1,"Only highlighted tutorial actions solve first run: "+index);
                if(index==2)OrbitalLoomSmokeTest.Capture("Builds/Preview-098-MoonLesson-Complete.png",1600,1200);
            }
        }
        private static void CheckArt(EnergyBoxView view)
        {
            Require(view.art!=null&&view.art.echoes.Length==18&&view.art.tails.Length==24&&view.art.winShards.Length==12,"Bounded exported art pools");
            Require(view.art.socketBody!=view.art.sourceBody&&view.art.goalBody!=view.art.gateBody,"Distinct physical apparatus meshes");
            Require(view.nodes[0].core.sharedMaterial.GetTexture("_BaseMap")!=null,"Persistent facet texture survives asset import and play mode: "+view.nodes[0].core.sharedMaterial.name);
            foreach(var renderer in view.art.echoes.Concat(view.art.tails).Concat(view.art.winShards))
                Require(renderer.GetComponentsInChildren<Collider>(true).Length==0,"Decoration cannot intercept controller ray");
        }
        private static void Click(string text)
        {
            var ui=UnityEngine.Object.FindObjectOfType<OrbitalLoomUI>();
            var button=ui.buttons.FirstOrDefault(b=>b.gameObject.activeInHierarchy&&new SerializedObject(b.label).FindProperty("_text").stringValue==text);
            Require(button!=null,"Button present: "+text);Press(button.target);
        }
        private static void Press(LoomTarget target)
        {
            var input=UnityEngine.Object.FindObjectOfType<SpatialInputRouter>();
            var dispatch=typeof(SpatialInputRouter).GetMethod("Dispatch",BindingFlags.Instance|BindingFlags.NonPublic);
            dispatch.Invoke(input,new object[]{-701,Phase.Began,target,target.transform.position});
            dispatch.Invoke(input,new object[]{-701,Phase.Ended,null,target.transform.position+Vector3.right*.02f});
        }
        private static void CheckAccess(EnergyBoxView view)
        {
            Physics.SyncTransforms();var input=UnityEngine.Object.FindObjectOfType<SpatialInputRouter>();
            var targets=view.nodes.Where(n=>n.gameObject.activeInHierarchy&&n.target.kind==TargetKind.BoxSocket||n.gameObject.activeInHierarchy&&n.target.kind==TargetKind.BoxClue&&n.GetComponentsInChildren<Collider>().Any(c=>c.enabled)).Select(n=>n.target)
                .Concat(UnityEngine.Object.FindObjectsOfType<SpatialButton>().Select(b=>b.target));
            foreach(var t in targets)
            {
                var ray=new Ray(input.editorCamera.transform.position,t.transform.position-input.editorCamera.transform.position);
                Require(Physics.Raycast(ray,out var hit,10),"Ray reaches "+t.name);
                Require(hit.collider.GetComponentInParent<LoomTarget>()==t,"Ray obstruction: "+t.name+" by "+hit.collider.name);
            }
        }
        private static void CheckLayout()
        {
            var buttons=UnityEngine.Object.FindObjectsOfType<SpatialButton>();
            foreach(var button in buttons)
            {
                CheckInk(button);button.target.Hover(true);CheckInk(button);button.target.Hover(false);CheckInk(button);
            }
            for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)
            {var d=buttons[i].transform.localPosition-buttons[j].transform.localPosition;var size=(buttons[i].surface.localScale+buttons[j].surface.localScale)*.5f;
                Require(Mathf.Abs(d.x)>=size.x||Mathf.Abs(d.y)>=size.y,"Overlapping buttons "+i+" / "+j);}
        }
        private static void CheckInk(SpatialButton button)
        {
            var label=new SerializedObject(button.label);
            var ink=label.FindProperty("_textColor").colorValue;var fill=label.FindProperty("_backgroundColor").colorValue;
            Require(fill.a>=.99f&&ink.a>=.99f,"Opaque native text payload: "+button.Caption);
            Require(SpatialUITheme.Contrast(ink,fill)>=7,"Readable native interaction state: "+button.Caption);
        }
        private static void CheckReusedButtonStates()
        {
            var button=UnityEngine.Object.FindObjectOfType<OrbitalLoomUI>().buttons.First(b=>b.gameObject.activeInHierarchy);
            var position=button.transform.localPosition;var size=(Vector2)button.surface.localScale;
            button.Bind("状态检查",position,size,()=>{});button.SetSelected(true);button.target.Hover(true);CheckInk(button);
            button.target.Click();CheckInk(button);button.SetAvailable(false);CheckInk(button);
            button.Bind("复用检查",position,size,()=>{});CheckInk(button);
            var data=new SerializedObject(button.label);
            Require(data.FindProperty("_backgroundColor").colorValue==SpatialUITheme.Surface,"Rebinding clears press, hover and selection");
            GameManager.Instance.MainMenu();
        }
        private static void Require(bool value,string text){if(!value)throw new InvalidOperationException(text);}
        private static void Finish(string result)
        {File.WriteAllText("Library/OrbitalLoom.Task.result",result);SessionState.SetInt(Active,0);EditorApplication.isPlaying=false;}
    }
}
