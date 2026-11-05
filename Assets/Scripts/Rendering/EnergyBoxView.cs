using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalLoom.Core;
using OrbitalLoom.Gameplay;
using OrbitalLoom.Interaction;
using UnityEngine;

namespace OrbitalLoom.Rendering
{
    /// <summary>All geometry is exported by the Editor. Gameplay only reuses these native pools.</summary>
    public sealed class EnergyBoxView : MonoBehaviour
    {
        public Transform assembly,stand;
        public NodeView[] nodes;
        public TrackView[] rails;
        public Transform[] arrows, pulses;
        public MeshRenderer[] energyCells;
        public Mesh gateGlyph,swapGlyph;
        public EnergyBoxArt art;
        public RelicView relic;
        private GameManager game;
        private EnergyBoxSession bound;
        private readonly List<(string from,string to)> edges=new List<(string,string)>();
        private Dictionary<string,BoxNode> map;
        private readonly Dictionary<string,string> waitCaptions=new Dictionary<string,string>();
        private float yaw;
        private string timelineKey;
        public float Yaw => yaw;
        private void Start()
        {
            game=GameManager.Instance;game.Changed+=Refresh;game.SessionChanged+=Refresh;
            Refresh();
        }
        private void OnDestroy()
        {if(game!=null){game.Changed-=Refresh;game.SessionChanged-=Refresh;}}
        public void Select(int index)
        {game.BoxSession?.Select(index);OrbitalLoom.Audio.LoomAudio.Instance?.Play("select",Vector3.zero);}
        public void RotateView(int direction)
        {yaw=Mathf.Clamp(yaw+direction*20,-60,60);assembly.localRotation=Quaternion.Euler(0,yaw,0);}
        public void Front(){yaw=0;assembly.localRotation=Quaternion.identity;}
        public void Refresh()
        {
            if(game==null)return;
            var session=game.BoxSession;
            bool visible=session!=null&&game.State==GameState.LevelPlaying&&!game.UiOverlayOpen;
            assembly.gameObject.SetActive(visible);if(stand!=null)stand.gameObject.SetActive(visible);if(!visible)return;
            if(bound!=session)
            {
                bound=session;Front();map=session.Level.nodes.ToDictionary(n=>n.id);edges.Clear();
                foreach(var n in session.Level.nodes)
                    foreach(string target in n.outputs.Concat(n.choices.SelectMany(c=>c.outputs)).Distinct())edges.Add((n.id,target));
            }
            for(int i=0;i<nodes.Length;i++)
            {
                bool use=i<session.Level.nodes.Length;nodes[i].gameObject.SetActive(use);if(!use)continue;
                var n=session.Level.nodes[i];int slot=Array.IndexOf(session.Sockets,n);
                var choice=slot<0?null:session.Installed(slot);
                var link=session.Level.links.FirstOrDefault(l=>l.a==n.id||l.b==n.id);
                string text=n.operation=="reverse"?"倒序镜":n.operation=="erase"?"退印台":n.kind=="socket"&&n.choices.Any(c=>c.parts.Length>0)?n.title+" · "+(choice==null?"未装":choice.name):n.kind=="socket"&&n.choices.Any(c=>c.delay>0)?n.title+" · "+(choice==null?"未装":choice.name):n.gate.Length>0?"门 / "+EnergyBoxSolver.RuneText(n.gate):n.swapFrom.Length>0?"换 "+n.swapFrom+"→"+n.swapTo:
                    n.rune.Length>0?n.rune+"印":n.kind=="socket"?n.title.Replace("插槽 ","")+(link!=null?" ↔"+(link.a==n.id?link.b:link.a).ToUpperInvariant():"")+(choice==null?" · 空":" · "+(choice.kind=="split"?"等分":"转向")):n.title+(n.kind=="source"?" · "+n.units+"格":n.kind=="goal"?" · "+n.units+"格"+(n.code.Length>0?"\n锁 "+EnergyBoxSolver.RuneText(n.code):""):"");
                if(session.Level.spatial?.Controls(n.id)==true)text=n.title+" · "+(choice==null?"空":session.Level.spatial.kind=="path"?choice.loops+"段":choice.angle.ToString("0")+"°");
                bool clue=n.kind!="socket"&&n.kind!="blocked";
                nodes[i].Bind(n.id,n.Position,text,ColorFor(n),choice?.kind=="split"?"splitter":null,n.kind=="socket"?TargetKind.BoxSocket:TargetKind.BoxClue);
                nodes[i].glyph.localPosition=Vector3.zero;nodes[i].glyph.localScale=Vector3.one*.035f;
                if(n.gate.Length>0)nodes[i].SetSymbol(gateGlyph);else if(n.swapFrom.Length>0)nodes[i].SetSymbol(swapGlyph);
                if(n.gate.Length>0||n.swapFrom.Length>0)
                {
                    nodes[i].glyph.localPosition=new Vector3(0,0,-.052f);nodes[i].glyph.localScale=Vector3.one*.043f;
                    LoomPalette.SetColor(nodes[i].glyph.GetComponent<Renderer>(),LoomPalette.Hex("102432"));
                }
                nodes[i].target.controlIndex=slot;
                foreach(var collider in nodes[i].GetComponentsInChildren<Collider>(true))
                    collider.enabled=(slot>=0||clue)&&(collider.gameObject==nodes[i].gameObject||collider.gameObject==nodes[i].core.gameObject);
                nodes[i].gameObject.layer=nodes[i].core.gameObject.layer=slot>=0||clue?0:2;
                nodes[i].label.TextColor=LoomPalette.Hex("F1F5F1");nodes[i].label.TextSize=n.code.Length>0?38:40;nodes[i].label.BackgroundSize=new Vector2(n.rune.Length>0?.20f:.43f,n.code.Length>0?.12f:.065f);
                nodes[i].label.transform.localPosition=CaptionOffset(n);
                if(n.id=="h"&&session.Level.spatial!=null)nodes[i].label.transform.localPosition=new Vector3(-.035f,-.11f,-.055f);
                if(n.id=="i"&&session.Level.spatial!=null)nodes[i].label.transform.localPosition=new Vector3(.06f,.10f,-.055f);
                nodes[i].SetSelected(slot>=0&&slot==session.Selected||session.Inspected==n.id);
                nodes[i].SetHint(session.Tutorial.Target==n.id||session.HintTarget==n.id);
                nodes[i].SetTutorialFocus(session.Tutorial.Active,session.Tutorial.Target==n.id);
            }
            if(energyCells!=null)for(int i=0;i<energyCells.Length;i++)
            {
                int node=i/4,cell=i%4;var n=node<session.Level.nodes.Length?session.Level.nodes[node]:null;
                bool cellVisible=n!=null&&(n.kind=="source"||n.kind=="goal")&&cell<n.units;
                energyCells[i].gameObject.SetActive(cellVisible);if(!cellVisible)continue;
                energyCells[i].transform.localPosition=n.Position+new Vector3((cell-(n.units-1)*.5f)*.022f,.066f,-.035f);
                energyCells[i].transform.localScale=new Vector3(.016f,.012f,.012f);
                LoomPalette.SetColor(energyCells[i],n.kind=="source"?LoomPalette.Gold:LoomPalette.Hex("4C687A"));
            }
            for(int i=0;i<rails.Length;i++)
            {
                bool use=i<edges.Count;rails[i].gameObject.SetActive(use);arrows[i].gameObject.SetActive(use);if(!use)continue;
                var edge=edges[i];var a=map[edge.from].Position;var b=map[edge.to].Position;
                var n=map[edge.from];int slot=Array.IndexOf(bound.Sockets,n);
                bool selected=slot<0?n.outputs.Contains(edge.to):bound.Installed(slot)?.outputs.Contains(edge.to)==true;
                // Rails dock into the housings rather than cutting through the
                // front inlays. Pulses still follow the exact logical endpoints.
                var direction=(b-a).normalized;float trim=Mathf.Min(.046f,Vector3.Distance(a,b)*.25f);
                var path=Enumerable.Range(0,TrackGeometry.Samples+1).Select(k=>Vector3.Lerp(a+direction*trim,b-direction*trim,k/(float)TrackGeometry.Samples)+Vector3.forward*.009f).ToArray();
                rails[i].Bind("boxrail"+i,path,false,false,0,!selected,selected?.0035f:.0015f);
                arrows[i].localScale=Vector3.one*(selected?.82f:.55f);
                arrows[i].localPosition=Vector3.Lerp(a,b,.68f);
                arrows[i].localRotation=Quaternion.FromToRotation(Vector3.right,b-a);
            }
            waitCaptions.Clear();art?.Bind(this,session);timelineKey=null;UpdateTimeline();
        }
        // Authored front/back layers need separate caption anchors so a rear
        // checkpoint label never sits on a nearer sphere or the HUD.
        private Vector3 CaptionOffset(BoxNode n)
        {
            if(bound.Level.id=="box_20"&&n.rune.Length>0)return new Vector3(0,.095f,-.055f);
            if(int.TryParse(bound.Level.id.Replace("box_",""),out int number)&&number>=7&&n.rune.Length>0)
                return new Vector3(.12f,.035f,-.055f);
            if(bound.Level.id=="box_05")
            {
                if(n.id=="h")return new Vector3(-.17f,0,-.055f);
                if(n.id=="hy")return new Vector3(-.16f,-.035f,-.055f);
                if(n.id=="hx")return new Vector3(0,.06f,-.055f);
            }
            if(bound.Level.id=="box_04"&&n.id=="c")return new Vector3(.07f,.085f,-.055f);
            if(n.id=="dx")return new Vector3(-.17f,-.02f,-.055f);
            return new Vector3(0,n.kind=="socket"?-.083f:n.code.Length>0?.125f:.104f,-.055f);
        }
        private Color ColorFor(BoxNode n)
        {return n.gate.Length>0?LoomPalette.Hex("7EDCC3"):n.swapFrom.Length>0?LoomPalette.Hex("EDB6DB"):n.rune.Length>0?LoomPalette.Splitter:n.kind=="source"?LoomPalette.Gold:n.kind=="goal"?LoomPalette.Repulsion:n.kind=="blocked"?LoomPalette.Error:n.operation=="reverse"?LoomPalette.Hex("A1C9E2"):n.operation=="erase"?LoomPalette.Hex("E9B49B"):n.kind=="relay"?LoomPalette.Mirror:LoomPalette.Hex("DBE6E6");}
        private void Update()
        {
            if(bound==null||!assembly.gameObject.activeInHierarchy)return;
            // Captions remain facing the player while the actual mechanism turns in depth.
            foreach(var node in nodes)if(node.gameObject.activeSelf)node.label.transform.rotation=transform.rotation;
            UpdateTimeline();
            // Waiting is a real part of the timing puzzle. Show its remaining
            // beats on the valve so a stationary photon never looks frozen.
            if(bound.Level.syncWindow>0&&bound.Result!=null)
                for(int i=0;i<bound.Level.nodes.Length;i++)
                {
                    var n=bound.Level.nodes[i];if(n.kind!="socket"||!n.choices.Any(c=>c.delay>0))continue;
                    if(bound.Result.failures.ContainsKey(n.id))continue;
                    var choice=bound.Installed(Array.IndexOf(bound.Sockets,n));if(choice==null)continue;
                    float arrival=bound.Result.traces.Where(t=>t.to==n.id).Select(t=>t.end).DefaultIfEmpty(float.PositiveInfinity).Min();
                    float departure=bound.Result.traces.Where(t=>t.from==n.id).Select(t=>t.start).DefaultIfEmpty(float.NegativeInfinity).Min();
                    string caption=n.title+" · "+(bound.Elapsed>=arrival&&bound.Elapsed<departure?"等待 "+(departure-bound.Elapsed).ToString("0.0")+"拍":choice.name);
                    if(!waitCaptions.TryGetValue(n.id,out var previous)||previous!=caption){nodes[i].label.Text=caption;waitCaptions[n.id]=caption;}
                }
            for(int i=0;i<pulses.Length;i++)
            {
                var trace=bound.Result!=null&&i<bound.Result.traces.Count?bound.Result.traces[i]:null;
                bool visible=trace!=null&&bound.Elapsed>=trace.start&&bound.Elapsed<trace.end;
                pulses[i].gameObject.SetActive(visible);if(!visible)continue;
                pulses[i].localPosition=Vector3.Lerp(map[trace.from].Position,map[trace.to].Position,Mathf.InverseLerp(trace.start,trace.end,bound.Elapsed));
                pulses[i].localScale=Vector3.one*(.022f+.007f*trace.units);
                LoomPalette.SetColor(pulses[i].GetComponent<Renderer>(),trace.units==1?LoomPalette.Mirror:trace.units==2?LoomPalette.Repulsion:LoomPalette.Gold);
            }
        }
        private void UpdateTimeline()
        {
            var result=bound.Result;
            bool faultVisible=result?.Diagnosis!=null&&bound.Elapsed>=result.Diagnosis.Time;
            string key=result==null?"none":bound.TestFinished+":"+faultVisible+":"+string.Join(",",result.traces.Where(t=>bound.Elapsed>=t.end).Select(t=>t.Key));
            if(key==timelineKey)return;timelineKey=key;
            for(int i=0;i<edges.Count;i++)
            {
                var edge=edges[i];var n=map[edge.from];int slot=Array.IndexOf(bound.Sockets,n);
                bool selected=slot<0?n.outputs.Contains(edge.to):bound.Installed(slot)?.outputs.Contains(edge.to)==true;
                bool arrived=result!=null&&result.traces.Any(t=>t.from==edge.from&&t.to==edge.to&&bound.Elapsed>=t.end);
                var color=arrived?LoomPalette.Gold:selected?LoomPalette.Hex("A4C5D3"):LoomPalette.Hex("4C6170");
                if(result!=null&&result.FirstFailure==edge.to&&arrived&&faultVisible)color=LoomPalette.Error;
                color.a=selected?1:.52f;rails[i].SetTint(color);LoomPalette.SetColor(arrows[i].GetComponent<Renderer>(),color);
            }
            for(int i=0;i<bound.Level.nodes.Length;i++)
            {
                var n=bound.Level.nodes[i];float arrival=result==null?float.PositiveInfinity:result.traces.Where(t=>t.to==n.id).Select(t=>t.end).DefaultIfEmpty(float.PositiveInfinity).Min();
                bool reached=bound.Elapsed>=arrival;
                bool error=result!=null&&result.failures.ContainsKey(n.id)&&
                    (result.FirstFailure==n.id?faultVisible:reached||bound.TestFinished);
                bool cause=error&&result.FirstFailure==n.id&&faultVisible;
                nodes[i].SetFailure(cause);
                nodes[i].SetAccent(cause?LoomPalette.Error:reached?LoomPalette.Gold:ColorFor(n));
                if(n.kind=="goal"&&reached)nodes[i].label.Text=n.title+" · "+result.received.GetValueOrDefault(n.id)+"/"+n.units+(error?" ×":" ✓")+(n.code.Length>0?"\n实收 "+EnergyBoxSolver.RuneText(result.signatures.GetValueOrDefault(n.id)):"");
                else if(error)nodes[i].label.Text=n.title+(cause?" · 首错":" · 未抵达");
                if(n.kind=="goal"&&energyCells!=null)
                    for(int cell=0;cell<n.units;cell++)
                        LoomPalette.SetColor(energyCells[i*4+cell],reached?error?LoomPalette.Error:LoomPalette.Gold:LoomPalette.Hex("4C687A"));
            }
        }
    }
}
