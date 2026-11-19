using System;
using System.IO;
using System.Linq;
using OrbitalLoom.Core;
using OrbitalLoom.Interaction;
using OrbitalLoom.Rendering;
using OrbitalLoom.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrbitalLoom.Editor
{
    /// <summary>Exercise the same grab/move/release entry points used by SpatialInputDevice.
    /// These are Editor checks, never substitutes for real hand-tracking validation.</summary>
    [InitializeOnLoad] public static class OrbitalLoomDirectSmoke
    {
        private const string Key="OrbitalLoom.DirectSmoke";
        private static int stage;
        private static double next;
        static OrbitalLoomDirectSmoke(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            EditorSceneManager.OpenScene(OrbitalLoomProjectTools.ScenePath);
            SessionState.SetBool(Key,true);stage=0;next=EditorApplication.timeSinceStartup+3;
            File.WriteAllText("Builds/Direct-Smoke.txt","WAIT");EditorApplication.isPlaying=true;
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
            next=EditorApplication.timeSinceStartup+.4;
            try
            {
                var game=GameManager.Instance;if(game==null||!game.Ready)return;game.VerificationMode=true;
                var direct=UnityEngine.Object.FindObjectOfType<DirectPuzzleInteraction>();var view=direct.view;
                if(stage==0)
                {
                    OrbitalLoomSmokeTest.Capture("Builds/Direct-Menu.png",1600,1200);
                    game.StartBox(0,true);stage++;return;
                }
                if(stage==1)
                {
                    var s=game.BoxSession;s.BeginTutorial();s.Select(0);direct.Refresh();
                    Install(direct,0,"turn");Require(s.Choice(0)==1&&s.Moves==1,"Physical crystal placement");
                    var node=view.nodes.First(n=>n.Id==s.Sockets[0].id);int before=s.Moves;
                    Require(direct.Begin(90,node.target,node.transform.position,Quaternion.identity),"Rotation capture");
                    direct.Move(90,node.transform.position,Quaternion.AngleAxis(32,node.transform.forward));
                    Require(s.Moves==before,"Preview must not commit");direct.Cancel();
                    Require(s.Choice(0)==1&&s.Moves==before,"Cancel must retain committed state");
                    Rotate(direct,0);Require(s.Choice(0)==2&&s.Moves==2,"Physical rotation and release");
                    Require(s.Tutorial.Button=="开始","Teaching reaches start without extra buttons");
                    OrbitalLoomSmokeTest.Capture("Builds/Direct-Teaching.png",1600,1200);
                    s.Test();s.Tick(100);Require(s.Solved,"First teaching solution");stage++;return;
                }
                int index=stage-2;
                if(index<24)
                {
                    game.StartBox(index);var s=game.BoxSession;s.SkipTutorial();
                    // Observation overlays are deliberately closed through the public UI back path.
                    var ui=UnityEngine.Object.FindObjectOfType<OrbitalLoomUI>();
                    var returnButton=ui.buttons.FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.Caption=="返回机关");
                    returnButton?.target.Click();direct.Refresh();
                    for(int i=0;i<s.Sockets.Length;i++)
                    {
                        int wanted=s.Level.reference[i];if(wanted==0||s.Choice(i)==wanted)continue;
                        Install(direct,i,s.Sockets[i].choices[wanted-1].kind);
                        int guard=0;while(s.Choice(i)!=wanted&&guard++<12)Rotate(direct,i);
                        Require(s.Choice(i)==wanted,"Reach reference choice "+index+"/"+i);
                    }
                    if(index==0||index==11||index==22)OrbitalLoomSmokeTest.Capture("Builds/Direct-Level-"+(index+1)+".png",1600,1200);
                    s.Test();s.Tick(100);Require(s.Solved,"Direct solution "+s.Level.id+": "+s.Message);
                    stage++;return;
                }
                File.WriteAllText("Builds/Direct-Smoke.txt","PASS\n24 levels solved via grab/move/release\nCancel, preview, commit and teaching verified\n");
                Finish(0);
            }
            catch(Exception error){Debug.LogException(error);File.WriteAllText("Builds/Direct-Smoke.txt","FAIL\n"+error);Finish(1);}
        }
        private static void Install(DirectPuzzleInteraction direct,int socket,string kind)
        {
            var piece=(kind=="turn"?direct.turnCrystal:direct.splitCrystal).GetComponent<LoomTarget>();
            Require(piece.gameObject.activeInHierarchy,"Inventory visible");
            var from=piece.transform.position;var to=direct.view.assembly.TransformPoint(GameManager.Instance.BoxSession.Sockets[socket].Position);
            Require(direct.Begin(90,piece,from,Quaternion.identity),"Grab crystal");
            direct.Move(90,to,Quaternion.identity);direct.End(90,false,to,Quaternion.identity);
        }
        private static void Rotate(DirectPuzzleInteraction direct,int socket)
        {
            var s=GameManager.Instance.BoxSession;var node=direct.view.nodes.First(n=>n.Id==s.Sockets[socket].id);var point=node.transform.position;
            Require(direct.Begin(90,node.target,point,Quaternion.identity),"Grab installed mechanism");
            var rotation=Quaternion.AngleAxis(32,node.transform.forward);direct.Move(90,point,rotation);direct.End(90,false,point,rotation);
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
