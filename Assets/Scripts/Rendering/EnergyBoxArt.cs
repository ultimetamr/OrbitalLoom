using System;
using OrbitalLoom.Core;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Rendering
{
    /// <summary>Editor-exported star instruments. Effects never move the puzzle,
    /// change its timing, add colliders, or create objects on the headset.</summary>
    public sealed class EnergyBoxArt : MonoBehaviour
    {
        public Mesh socketBody,sourceBody,goalBody,runeBody,gateBody,converterBody,blockedBody,casing,orbit;
        public Mesh turnMark,splitMark,moonMark,starMark,sunMark,goalMark,reverseMark,eraseMark,clockMark;
        public MeshRenderer[] echoes,tails,winShards;
        private EnergyBoxView view;
        private EnergyBoxSession session;
        private BoxEvaluation evaluation;
        private readonly bool[] sounded=new bool[48];
        private bool completed;
        private float solvedAge;
        public float CelebrationAge=>solvedAge;
        private static readonly Color Case=LoomPalette.Hex("334F53"),Face=LoomPalette.Hex("172F38"),
            Rim=LoomPalette.Hex("BFA875");

        public void Bind(EnergyBoxView owner,EnergyBoxSession current)
        {
            view=owner;
            if(session!=current){session=current;evaluation=null;completed=false;solvedAge=0;Array.Clear(sounded,0,sounded.Length);}
            for(int i=0;i<current.Level.nodes.Length;i++)
            {
                var n=current.Level.nodes[i];var node=owner.nodes[i];
                Mesh body=n.kind=="source"?sourceBody:n.kind=="goal"?goalBody:n.kind=="socket"?socketBody:
                    n.gate.Length>0?gateBody:n.swapFrom.Length>0||n.operation.Length>0?converterBody:n.kind=="blocked"?blockedBody:runeBody;
                node.SetBody(body,casing,orbit);
                // Actual depth, bevelled housing and a front inlay replace the
                // identical translucent balls. All stay inside the old hit area.
                node.core.transform.localScale=n.kind=="source"?new Vector3(.068f,.085f,.068f):
                    n.rune.Length>0?new Vector3(.063f,.088f,.063f):n.kind=="socket"?new Vector3(.112f,.112f,.060f):Vector3.one*(n.gate.Length>0?.11f:.10f);
                var hit=node.core.GetComponent<SphereCollider>();
                if(hit!=null)hit.radius=.064f/Mathf.Max(node.core.transform.localScale.x,Mathf.Max(node.core.transform.localScale.y,node.core.transform.localScale.z));
                node.shell.transform.localScale=new Vector3(.132f,.132f,.070f);
                node.shell.transform.localPosition=new Vector3(0,0,.028f);
                LoomPalette.SetColor(node.shell,Case);
                node.ring.localPosition=new Vector3(0,0,-.020f);
                node.ring.localScale=Vector3.one*(n.kind=="source"?.069f:.066f);
                node.ring.localRotation=Quaternion.Euler(n.kind=="source"?38:n.rune.Length>0?22:0,n.kind=="source"?-20:0,0);
                LoomPalette.SetColor(node.ring.GetComponent<Renderer>(),Rim);
                bool gate=n.gate.Length>0;
                node.glyph.gameObject.SetActive(!gate&&n.kind!="blocked");
                if(!gate)
                {
                    var piece=n.kind=="socket"?current.Installed(Array.IndexOf(current.Sockets,n)):null;
                    var mark=n.operation=="reverse"?reverseMark:n.operation=="erase"?eraseMark:n.kind=="socket"&&Array.Exists(n.choices,c=>c.delay>0)?clockMark:n.kind=="socket"?(piece?.kind=="split"?splitMark:turnMark):n.kind=="goal"?goalMark:
                        n.rune=="月"?moonMark:n.rune=="日"?sunMark:n.swapFrom.Length>0?owner.swapGlyph:starMark;
                    node.SetSymbol(mark);node.glyph.localPosition=new Vector3(0,0,-.055f);
                    node.glyph.localScale=Vector3.one*(n.kind=="source"?.033f:.043f);
                    LoomPalette.SetColor(node.glyph.GetComponent<Renderer>(),Face);
                }
                echoes[i].transform.localPosition=n.Position+new Vector3(0,0,-.035f);
                echoes[i].gameObject.SetActive(false);
            }
            for(int i=current.Level.nodes.Length;i<echoes.Length;i++)echoes[i].gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if(session==null||view==null||!view.assembly.gameObject.activeInHierarchy)return;
            var game=GameManager.Instance;if(game==null)return;
            bool animate=game.Settings.particles&&!game.Paused;
            var result=session.Result;
            if(result!=evaluation){evaluation=result;Array.Clear(sounded,0,sounded.Length);completed=false;solvedAge=0;}
            if(session.Solved){if(!completed){completed=true;solvedAge=0;}if(animate)solvedAge+=Time.unscaledDeltaTime;}
            else{completed=false;solvedAge=0;}
            for(int i=0;i<session.Level.nodes.Length;i++)
            {
                var n=session.Level.nodes[i];var node=view.nodes[i];
                float arrival=Arrival(result,n.id);
                bool reached=result!=null&&session.Elapsed>=arrival;
                bool failure=result!=null&&result.FirstFailure==n.id&&(reached||session.TestFinished);
                bool focus=session.HintTarget==n.id||session.Tutorial.Target==n.id||session.Selected>=0&&session.Sockets[session.Selected].id==n.id;
                var accent=failure?LoomPalette.Error:reached||session.Solved&&n.kind=="source"?LoomPalette.Gold:focus?LoomPalette.Gold:Rim;
                LoomPalette.SetColor(node.ring.GetComponent<Renderer>(),accent);
                if(animate&&n.kind=="source")node.ring.Rotate(Vector3.forward,Time.unscaledDeltaTime*9,Space.Self);
                float age=reached?session.Elapsed-arrival:float.PositiveInfinity;
                bool wave=animate&&(age>=0&&age<.75f||session.Solved&&solvedAge<1.25f&&(reached||n.kind=="source"));
                var echo=echoes[i];echo.gameObject.SetActive(wave);
                if(wave)
                {
                    float t=session.Solved?Mathf.Clamp01(solvedAge/1.25f):Mathf.Clamp01(age/.75f);
                    echo.transform.localScale=Vector3.one*Mathf.Lerp(.058f,.090f,t);
                    // Opaque, quiet colors avoid transparent sorting artefacts in MR.
                    LoomPalette.SetColor(echo,Color.Lerp(accent,Case,t));
                }
            }
            for(int i=0;i<tails.Length;i++)
            {
                var trace=result!=null&&i<result.traces.Count?result.traces[i]:null;
                bool active=animate&&trace!=null&&session.Elapsed>=trace.start&&session.Elapsed<trace.end;
                tails[i].gameObject.SetActive(active);
                if(active)
                {
                    var a=Node(trace.from).Position;var b=Node(trace.to).Position;
                    float t=Mathf.Clamp01((session.Elapsed-trace.start)/(trace.end-trace.start)-.055f);
                    tails[i].transform.localPosition=Vector3.Lerp(a,b,t);
                    tails[i].transform.localRotation=Quaternion.FromToRotation(Vector3.forward,b-a);
                    tails[i].transform.localScale=new Vector3(.010f,.010f,.043f);
                    LoomPalette.SetColor(tails[i],LoomPalette.Hex("AC844B"));
                }
                if(trace!=null&&i<sounded.Length&&session.Elapsed>=trace.end&&!sounded[i])
                {
                    sounded[i]=true;var target=Node(trace.to);
                    string sound=result.FirstFailure==trace.to?"error":target.swapFrom.Length>0?"splitter":target.gate.Length>0?"mirror":"connect";
                    // Basic positional stereo through the existing audio path.
                    if(!game.Paused&&session.Level.syncWindow<=0&&session.Running&&!session.Paused)
                        OrbitalLoom.Audio.LoomAudio.Instance?.Play(sound,target.Position);
                }
            }
            for(int i=0;i<winShards.Length;i++)
            {
                bool show=animate&&session.Solved&&solvedAge<1.6f;
                var shard=winShards[i];shard.gameObject.SetActive(show);if(!show)continue;
                float angle=(i*30+solvedAge*40)*Mathf.Deg2Rad;
                float radius=Mathf.Lerp(.04f,.36f,Mathf.SmoothStep(0,1,solvedAge/1.6f));
                shard.transform.localPosition=new Vector3(Mathf.Cos(angle)*radius,.06f+Mathf.Sin(angle)*radius*.65f,-.18f+Mathf.Sin(angle*2)*.07f);
                shard.transform.localRotation=Quaternion.Euler(0,i*30+solvedAge*65,45);
                shard.transform.localScale=Vector3.one*(.012f*(1-Mathf.Clamp01(solvedAge/1.6f))+.002f);
            }
        }
        private BoxNode Node(string id)
        {foreach(var n in session.Level.nodes)if(n.id==id)return n;throw new InvalidOperationException("Missing art node "+id);}
        public static float Arrival(BoxEvaluation result,string id)
        {float arrival=float.PositiveInfinity;if(result!=null)foreach(var t in result.traces)if(t.to==id)arrival=Mathf.Min(arrival,t.end);return arrival;}
    }
}
