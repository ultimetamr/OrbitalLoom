using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace OrbitalLoom.Gameplay
{
    [Serializable] public sealed class BoxChoice
    {
        public string kind, name;
        public int[] parts = Array.Empty<int>();
        public float delay;
        public float angle;
        public int loops;
        public string[] outputs = Array.Empty<string>();
    }
    [Serializable] public sealed class BoxNode
    {
        public string id, kind, title;
        // Runes are visible on physical checkpoints; receivers require this
        // exact visitation order. They never change energy or hide a password.
        public string rune = "", code = "";
        // Gate compares the full arriving sequence; converter replaces only the
        // last rune. Earlier history and branch energy are preserved.
        public string gate = "", swapFrom = "", swapTo = "";
        public string operation = "";
        public float x, y, z;
        public int units;
        public string[] outputs = Array.Empty<string>();
        public BoxChoice[] choices = Array.Empty<BoxChoice>();
        public Vector3 Position => new Vector3(x,y,z);
    }
    [Serializable] public sealed class BoxLink
    {
        public string a,b;
        // Bidirectional permutation of socket states, including empty state 0.
        public int[] map;
    }
    [Serializable] public sealed class EnergyBoxLevel
    {
        public string id, title, lesson;
        public int turns, splitters;
        public float syncWindow;
        public BoxNode[] nodes;
        public string[] hints;
        public int[] reference;
        public BoxLink[] links=Array.Empty<BoxLink>();
        public string tutorial="", tutorialNode="", tutorialSocket="";
        public RelicClue relic;
        public SpatialMechanism spatial;
        public string objective="", question="", curriculum="";
        public void Validate()
        {
            if(relic?.IsEmpty==true)relic=null;
            relic?.Validate();
            if(spatial?.IsEmpty==true)spatial=null;
            if(string.IsNullOrWhiteSpace(id) || nodes == null || nodes.Length<3 || nodes.Length>18 ||
                nodes.Any(n=>n==null || string.IsNullOrWhiteSpace(n.id)) || nodes.Select(n=>n.id).Distinct().Count()!=nodes.Length)
                throw new FormatException("机关节点格式无效");
            var ids=new HashSet<string>(nodes.Select(n=>n.id));
            if(nodes.Count(n=>n.kind=="source")!=1 || !nodes.Any(n=>n.kind=="goal") || turns<0 || splitters<0)
                throw new FormatException("机关能源、目标或库存无效");
            if(hints == null || hints.Length != 3 || hints.Any(string.IsNullOrWhiteSpace) ||
                reference == null || reference.Length != nodes.Count(n=>n.kind=="socket")) throw new FormatException("关卡提示或参考解无效");
            foreach(var n in nodes)
            {
                if(!new[]{"source","goal","socket","relay","blocked"}.Contains(n.kind) ||
                    !Data.Point3.IsFinite(n.x)||!Data.Point3.IsFinite(n.y)||!Data.Point3.IsFinite(n.z) ||
                    Mathf.Abs(n.x)>.9f || Mathf.Abs(n.y)>.5f || Mathf.Abs(n.z)>.5f || n.outputs==null || n.choices==null)
                    throw new FormatException("机关节点类型或坐标无效");
                if((n.kind=="source" || n.kind=="goal") && (n.units<1||n.units>4)) throw new FormatException("能量档位无效");
                if(n.rune==null||n.code==null||n.rune.Length>1||n.code.Length>3||
                    (n.rune+n.code).Any(c=>c!='月'&&c!='星'&&c!='日')||
                    n.rune.Length>0&&n.kind!="relay"||n.code.Length>0&&n.kind!="goal")throw new FormatException("符文检查点格式无效");
                if(n.gate==null||n.swapFrom==null||n.swapTo==null||n.gate.Length>3||
                    (n.gate+n.swapFrom+n.swapTo).Any(c=>c!='月'&&c!='星'&&c!='日')||
                    (n.gate.Length>0||n.swapFrom.Length>0||n.swapTo.Length>0)&&n.kind!="relay"||
                    (n.swapFrom.Length>0||n.swapTo.Length>0)&&(n.swapFrom.Length!=1||n.swapTo.Length!=1||n.swapFrom==n.swapTo)||
                    n.gate.Length>0&&(n.swapFrom.Length>0||n.rune.Length>0)||n.swapFrom.Length>0&&n.rune.Length>0)
                    throw new FormatException("条件门或换印器格式无效");
                if(n.operation==null||!new[]{"","reverse","erase"}.Contains(n.operation)||n.operation.Length>0&&(n.kind!="relay"||n.rune.Length>0||n.gate.Length>0||n.swapFrom.Length>0))throw new FormatException("符文加工机关无效");
                if(n.kind=="socket" && (n.choices.Length==0 || n.choices.Length>8)) throw new FormatException("插槽缺少方向");
                if((n.kind=="source" || n.kind=="relay") && n.outputs.Length!=1 ||
                    (n.kind=="goal" || n.kind=="blocked" || n.kind=="socket") && n.outputs.Length!=0)
                    throw new FormatException("固定导轨出口数量无效");
                if(n.outputs.Any(t=>!ids.Contains(t)||t==n.id) || n.outputs.Distinct().Count()!=n.outputs.Length) throw new FormatException("固定导轨无效");
                foreach(var c in n.choices)
                    if(c==null || c.outputs==null || (c.kind!="turn" && c.kind!="split") ||
                        c.parts==null||!Data.Point3.IsFinite(c.delay)||c.delay<0||c.delay>4||
                        c.parts.Length>0&&(c.kind!="split"||c.parts.Length!=2||c.parts.Any(v=>v<1||v>3))||
                        c.outputs.Length!=(c.kind=="split"?2:1) || c.outputs.Distinct().Count()!=c.outputs.Length || c.outputs.Any(t=>!ids.Contains(t)||t==n.id))
                        throw new FormatException("元件方向无效");
            }
            if(nodes.Where(n=>n.kind=="source").Sum(n=>n.units)!=nodes.Where(n=>n.kind=="goal").Sum(n=>n.units)) throw new FormatException("源与目标能量不守恒");
            var sockets=nodes.Where(n=>n.kind=="socket").ToArray();
            for(int i=0;i<reference.Length;i++)if(reference[i]<0||reference[i]>sockets[i].choices.Length)throw new FormatException("参考方向越界");
            if(links==null)throw new FormatException("联动组无效");
            var linked=new HashSet<string>();
            foreach(var link in links)
            {
                var a=sockets.FirstOrDefault(n=>n.id==link?.a);var b=sockets.FirstOrDefault(n=>n.id==link?.b);
                if(a==null||b==null||a==b||!linked.Add(a.id)||!linked.Add(b.id)||link.map==null||
                    a.choices.Length!=b.choices.Length||link.map.Length!=a.choices.Length+1||link.map[0]!=0||
                    link.map.Any(v=>v<0||v>=link.map.Length)||link.map.Distinct().Count()!=link.map.Length||
                    Enumerable.Range(1,a.choices.Length).Any(c=>a.choices[c-1].kind!=b.choices[link.map[c]-1].kind))
                    throw new FormatException("联动必须为两个独立插槽之间的可逆同类型映射");
                if(link.map[reference[Array.IndexOf(sockets,a)]]!=reference[Array.IndexOf(sockets,b)])throw new FormatException("参考解违反联动");
            }
            spatial?.Validate(this);
            if(!Data.Point3.IsFinite(syncWindow)||syncWindow<0||syncWindow>.5f)throw new FormatException("同步窗口无效");
            if(tutorial!=""&&!new[]{"gate","swap","link","prism","reverse","erase","sync"}.Contains(tutorial)||tutorial==null||
                tutorial.Length>0&&(!ids.Contains(tutorialNode)||!sockets.Any(n=>n.id==tutorialSocket)))throw new FormatException("教学目标无效");
        }
    }
    [Serializable] public sealed class EnergyBoxCampaign
    {
        public int version;
        public EnergyBoxLevel[] levels;
        public static EnergyBoxCampaign Parse(string json)
        {
            EnergyBoxCampaign data;
            try {data=JsonConvert.DeserializeObject<EnergyBoxCampaign>(json,new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=16});}
            catch(JsonException e){throw new FormatException("机关盒关卡损坏",e);}
            if(data==null || (data.version!=1&&data.version!=2&&data.version!=3) || data.levels==null || data.levels.Length!=(data.version==1?6:data.version==2?12:24) || data.levels.Any(l=>l==null)) throw new FormatException("机关盒关卡数量或版本无效");
            foreach(var l in data.levels)l.Validate();
            if(data.levels.Select(l=>l.id).Distinct().Count()!=data.levels.Length)throw new FormatException("机关关卡 ID 重复");
            return data;
        }
    }
    public sealed class BoxTrace
    {
        public string from,to;
        public int units;
        public float start,end;
        public string Key => from+">"+to;
    }
    public sealed class BoxEvaluation
    {
        public readonly List<BoxTrace> traces=new List<BoxTrace>();
        public readonly Dictionary<string,int> received=new Dictionary<string,int>();
        public readonly Dictionary<string,string> signatures=new Dictionary<string,string>();
        public readonly Dictionary<string,string> failures=new Dictionary<string,string>();
        public readonly Dictionary<string,string> incoming=new Dictionary<string,string>();
        public readonly Dictionary<string,float> arrivals=new Dictionary<string,float>();
        // Contention occurs on the second arrival, not the first successful visit.
        public readonly Dictionary<string,float> failureTimes=new Dictionary<string,float>();
        public BoxDiagnosis Diagnosis { get; internal set; }
        public string FirstFailure => Diagnosis?.Node??failures.Keys.FirstOrDefault();
        public bool Complete => failures.Count==0;
        public float Duration => traces.Count==0?.5f:traces.Max(t=>t.end)+.5f;
        public string Message => Complete?"全部接收器精确供能 · 机关已解开":Diagnosis?.Evidence??failures.Values.First();
    }
    /// <summary>Deterministic energy, rune and metric-geometry evaluation. Rendering and audio never decide the answer.</summary>
    public static class EnergyBoxSolver
    {
        private sealed class Token {public string id,signature; public int units; public float time; public HashSet<string> path;}
        public static BoxEvaluation Evaluate(EnergyBoxLevel level,int[] state)
        {
            var sockets=level.nodes.Where(n=>n.kind=="socket").ToArray();
            if(state==null || state.Length!=sockets.Length)throw new ArgumentException("插槽状态数量无效");
            var r=new BoxEvaluation();var map=level.nodes.ToDictionary(n=>n.id);var selected=new Dictionary<string,BoxChoice>();
            for(int i=0;i<sockets.Length;i++)
            {
                if(state[i]<0 || state[i]>sockets[i].choices.Length)throw new ArgumentException("插槽方向越界");
                if(state[i]>0)selected[sockets[i].id]=sockets[i].choices[state[i]-1];
            }
            foreach(var link in level.links)
                if(link.map[state[Array.FindIndex(sockets,n=>n.id==link.a)]]!=state[Array.FindIndex(sockets,n=>n.id==link.b)])
                {r.failures[link.a]="联动状态不一致，请重新调整联动插槽";return r;}
            if(selected.Values.Count(c=>c.kind=="turn")>level.turns || selected.Values.Count(c=>c.kind=="split")>level.splitters)
            {r.failures["inventory"]="元件超过库存";return r;}
            var visits=new HashSet<string>();var edges=new HashSet<string>();var queue=new List<Token>();
            var source=level.nodes.Single(n=>n.kind=="source");
            queue.Add(new Token{id=source.id,signature="",units=source.units,time=0,path=new HashSet<string>()});
            int guard=0;
            while(queue.Count>0 && guard++<128)
            {
                var token=queue.OrderBy(t=>t.time).First();queue.Remove(token);var n=map[token.id];
                if(token.path.Contains(n.id)){r.failures[n.id]=n.title+"：能量进入循环";continue;}
                if(!visits.Add(n.id)){r.failures[n.id]=n.title+"：两路争用，不能合流";r.failureTimes[n.id]=token.time;continue;}
                r.received[n.id]=token.units;r.arrivals[n.id]=token.time;
                string signature=token.signature+n.rune;r.signatures[n.id]=signature;
                r.incoming[n.id]=token.signature;
                if(n.gate.Length>0&&signature!=n.gate)
                {r.failures[n.id]=n.title+"：条件需 "+RuneText(n.gate)+"，实收 "+RuneText(signature);continue;}
                if(n.swapFrom.Length>0)
                {
                    if(!signature.EndsWith(n.swapFrom,StringComparison.Ordinal))
                    {r.failures[n.id]=n.title+"：末印需 "+n.swapFrom+"，实收 "+RuneText(signature);continue;}
                    signature=signature.Substring(0,signature.Length-1)+n.swapTo;r.signatures[n.id]=signature;
                }
                if(n.operation=="reverse")signature=new string(signature.Reverse().ToArray());
                if(n.operation=="erase")
                {
                    if(signature.Length==0){r.failures[n.id]=n.title+"：空白印记不能再退印";continue;}
                    signature=signature.Substring(0,signature.Length-1);
                }
                r.signatures[n.id]=signature;
                if(n.kind=="goal")
                {
                    if(token.units!=n.units)r.failures[n.id]=n.title+"：需要 "+n.units+" 格，收到 "+token.units+" 格";
                    else if(n.code.Length>0&&signature!=n.code)r.failures[n.id]=n.title+"：锁印需 "+RuneText(n.code)+"，实收 "+RuneText(signature);
                    continue;
                }
                if(n.kind=="blocked"){r.failures[n.id]=n.title+"：封闭断口，能量泄漏";continue;}
                string[] outputs=n.outputs;bool split=false;BoxChoice chosen=null;
                if(n.kind=="socket")
                {
                    if(!selected.TryGetValue(n.id,out var choice)){r.failures[n.id]=n.title+"：空插槽，先安装元件";continue;}
                    chosen=choice;outputs=choice.outputs;split=choice.kind=="split";
                }
                if(outputs.Length==0){r.failures[n.id]=n.title+"：没有出口";continue;}
                if(split && (token.units<2||chosen.parts.Length==0&&token.units%2!=0)){r.failures[n.id]=n.title+"：当前能量不能均分";continue;}
                if(split&&chosen.parts.Length>0&&chosen.parts.Sum()!=token.units){r.failures[n.id]=n.title+"：棱镜比例与输入格数不匹配";continue;}
                if(level.spatial!=null&&level.spatial.kind!="path"&&n.id==level.spatial.controls[0]&&
                    !level.spatial.Passes(level,state,out var spatialFailure))
                {r.failures[n.id]=n.title+"："+spatialFailure;continue;}
                float start=token.time+(chosen?.delay??0);
                var path=new HashSet<string>(token.path){n.id};
                for(int output=0;output<outputs.Length;output++)
                {
                    string target=outputs[output];int amount=split?(chosen.parts.Length>0?chosen.parts[output]:token.units/2):token.units;
                    if(path.Contains(target)){r.failures[n.id]=n.title+"：出口返回已走过的接点，形成循环";r.failureTimes[n.id]=start;continue;}
                    string segment=string.CompareOrdinal(n.id,target)<0?n.id+"|"+target:target+"|"+n.id;
                    if(!edges.Add(segment)){r.failures[n.id]=n.title+"：导轨已被另一路占用";r.failureTimes[n.id]=start;continue;}
                    float end=start+(level.spatial?.kind=="path"&&level.spatial.Controls(n.id)?level.spatial.Travel(level,state,n.id):level.syncWindow>0?1:Mathf.Max(.55f,Vector3.Distance(n.Position,map[target].Position)/.38f));
                    r.traces.Add(new BoxTrace{from=n.id,to=target,units=amount,start=start,end=end});
                    queue.Add(new Token{id=target,signature=signature,units=amount,time=end,path=path});
                }
            }
            foreach(var goal in level.nodes.Where(n=>n.kind=="goal"))
                if(!r.received.ContainsKey(goal.id))r.failures[goal.id]=goal.title+"：尚未供能，需要 "+goal.units+" 格";
            if(level.syncWindow>0&&r.failures.Count==0)
            {
                var goals=level.nodes.Where(n=>n.kind=="goal").ToArray();float last=goals.Max(n=>r.arrivals[n.id]);
                foreach(var goal in goals)if(last-r.arrivals[goal.id]>level.syncWindow)r.failures[goal.id]=goal.title+"：提前 "+(last-r.arrivals[goal.id]).ToString("0")+" 拍到达，调整延时使终点同拍";
            }
            if(guard>=128)r.failures[source.id]="路径过长，请检查循环";
            r.Diagnosis=BoxDiagnosis.From(level,r);
            return r;
        }
        public static string RuneText(string code)=>string.IsNullOrEmpty(code)?"空白":string.Join("→",code.ToCharArray());
        /// <summary>Bounded exhaustive certificate; returns all accepted layouts, not player difficulty.</summary>
        public static List<int[]> Solutions(EnergyBoxLevel level)
        {
            var sockets=level.nodes.Where(n=>n.kind=="socket").ToArray();var found=new List<int[]>();var state=new int[sockets.Length];
            long combinations=1;foreach(var n in sockets)combinations*=n.choices.Length+1;
            if(combinations>200000)throw new InvalidOperationException("机关状态空间超出验证上限");
            Search(0);return found;
            void Search(int i)
            {
                if(i==state.Length){if(Evaluate(level,state).Complete)found.Add((int[])state.Clone());return;}
                for(int c=0;c<=sockets[i].choices.Length;c++){state[i]=c;Search(i+1);}
            }
        }
    }
    /// <summary>Discrete placement, reversible edits and an explicit test timeline.</summary>
    public sealed class EnergyBoxSession
    {
        public readonly EnergyBoxLevel Level;
        public readonly BoxNode[] Sockets;
        private int[] state;
        private readonly List<int[]> history=new List<int[]>();
        public int Selected{get;private set;}=-1;
        public int Moves{get;private set;}
        public int Attempts{get;private set;}
        public int Hints{get;private set;}
        public bool Running{get;private set;}
        public bool Paused{get;private set;}
        public bool Solved{get;private set;}
        public float Elapsed{get;private set;}
        public BoxEvaluation Result{get;private set;}
        public bool ReviewingFailure { get; private set; }
        public bool TestFinished=>Result!=null&&(Elapsed>=Result.Duration||ReviewingFailure);
        public string Message{get;private set;}
        public int UndoAvailable=>history.Count;
        public BoxTutorial Tutorial { get; }
        public RelicObservation Observation { get; }
        public string Inspected { get; private set; }
        public event Action Changed;
        public string HintTarget { get; private set; }
        private List<int[]> solutions;
        public BoxDiagnosis Diagnosis=>TestFinished?Result?.Diagnosis:null;
        public string LearningQuestion=>string.IsNullOrEmpty(Level.question)?Level.lesson:Level.question;
        public int SpatialSelected { get; private set; }
        public bool SpatialControlChosen { get; private set; }
        public int SpatialEdits { get; private set; }
        public bool SpatialLessonDismissed { get; private set; }
        public bool SpatialLessonCompleted=>SpatialEdits>0&&Level.spatial?.LessonReady(Level,state)==true;
        public string SpatialReport=>Level.spatial?.Report(Level,state)??"";
        public void SelectSpatial(int index)
        {if(Level.spatial==null||index<0||index>=Level.spatial.controls.Length)return;SpatialSelected=index;SpatialControlChosen=true;Publish();}
        public bool RotateSpatial(int delta)
        {
            if(Level.spatial==null)return false;
            int index=Array.FindIndex(Sockets,n=>n.id==Level.spatial.controls[SpatialSelected]);
            int next=state[index]==0?1:(state[index]-1+Math.Sign(delta)+Sockets[index].choices.Length)%Sockets[index].choices.Length+1;
            return Set(index,next);
        }
        public void DismissSpatialLesson(){SpatialLessonDismissed=true;Publish();}
        public EnergyBoxSession(EnergyBoxLevel level,bool tutorial=false){Level=level;level.Validate();Sockets=level.nodes.Where(n=>n.kind=="socket").ToArray();state=new int[Sockets.Length];Message=level.lesson;Tutorial=new BoxTutorial(tutorial,level.id=="box_02",level.id=="box_03",level);Observation=level.relic==null?null:new RelicObservation(level.relic);}
        public void RotateRelic(int direction){if(Observation==null)return;Observation.Rotate(direction);Publish();}
        public void RecordRelic(bool textAccess=false)
        {
            if(Observation==null||!Observation.Record(textAccess))return;
            Message="遗迹线索："+Observation.Clue.discovery;Publish();
        }
        public void BeginTutorial(){Tutorial.Begin(this);Publish();}
        public void SkipTutorial(){Tutorial.Skip();Publish();}
        public void Examine(string id)
        {
            var node=Level.nodes.FirstOrDefault(n=>n.id==id);if(node==null)return;Inspected=id;
            Message=Level.spatial?.Controls(id)==true?node.title+"：打开右侧空间机关，调整真实方向或光路长度。"+SpatialReport:node.operation=="reverse"?node.title+"：整串符文倒序，月→星变为星→月；能量不变":
                node.operation=="erase"?node.title+"：移除最后一个符文，空白印记会阻断；能量不变":
                node.kind=="socket"&&node.choices.Any(c=>c.parts.Length>0)?node.title+"：棱镜按出口标记分配能量，3＋1与2＋2不同；输出总数必须等于输入":
                node.kind=="socket"&&node.choices.Any(c=>c.delay>0)?node.title+"：每段导轨耗时1拍，可在出口等待0—4拍，让全部终点同拍到达":
                node.gate.Length>0?node.title+"：完整印记必须是 "+EnergyBoxSolver.RuneText(node.gate)+"；多印、少印都不放行":
                node.swapFrom.Length>0?node.title+"：只把末尾的 "+node.swapFrom+" 换为 "+node.swapTo+"，前面的印记保留；末印不符会阻断":
                node.kind=="goal"?node.title+"：需要 "+node.units+" 格"+(node.code.Length>0?"，锁印顺序 "+EnergyBoxSolver.RuneText(node.code):"，没有符文锁"):
                node.rune.Length>0?node.title+"：经过后在光上追加“"+node.rune+"”印记":
                node.kind=="source"?"能源输出 "+node.units+" 格；亮线是已选路线，暗色虚线是候选":node.title+"：固定通道，不在这里分流";
            if(TestFinished&&Result.failures.TryGetValue(id,out var failure))Message=failure;
            Publish();
        }
        private void Publish(){Tutorial.Observe(this);Changed?.Invoke();}
        public int Choice(int index)=>state[index];
        public int[] Snapshot()=>(int[])state.Clone();
        public BoxChoice Installed(int index)=>state[index]==0?null:Sockets[index].choices[state[index]-1];
        public int Used(string kind)=>Enumerable.Range(0,Sockets.Length).Count(i=>Installed(i)?.kind==kind);
        public string LinkDescription(int index)
        {
            var link=Level.links.FirstOrDefault(l=>l.a==Sockets[index].id||l.b==Sockets[index].id);if(link==null)return "";
            string partner=link.a==Sockets[index].id?link.b:link.a;
            var a=Level.nodes.First(n=>n.id==link.a);var b=Level.nodes.First(n=>n.id==link.b);
            if(a.choices.All(c=>c.kind=="turn"&&c.outputs.SequenceEqual(a.choices[0].outputs))&&
               b.choices.All(c=>c.kind=="turn"&&c.outputs.SequenceEqual(b.choices[0].outputs)))
            {
                var sums=Enumerable.Range(1,a.choices.Length).Select(i=>a.choices[i-1].delay+b.choices[link.map[i]-1].delay).Distinct().ToArray();
                if(sums.Length==1)return Level.spatial?.kind=="path"?"联动月日两路 · 展开段数相加固定 4；每段增加1拍，完整映射见目标→联动表":"联动 "+b.title+" / "+a.title+" · 两阀等待相加固定 "+sums[0].ToString("0")+" 拍；完整映射见目标→联动表";
            }
            return "联动 "+Level.nodes.First(n=>n.id==partner).title+" · 装件、转向、取回一起变化";
        }
        public void Select(int index){if(index<0||index>=Sockets.Length||Running||Solved||Tutorial.Active&&!Tutorial.Started)return;Selected=index;Inspected=null;Message=string.IsNullOrEmpty(LinkDescription(index))?Sockets[index].title+" · 选择元件，再调整出口":LinkDescription(index);Publish();}
        public bool Set(int index,int choice)
        {
            if(Running||Solved||index<0||index>=state.Length||choice<0||choice>Sockets[index].choices.Length||state[index]==choice)return false;
            var wanted=choice==0?null:Sockets[index].choices[choice-1];
            var next=(int[])state.Clone();next[index]=choice;
            // A linked edit is one atomic transaction. Inventory is checked
            // after both sockets move, so failure cannot leave a half edit.
            foreach(var link in Level.links)
            {
                int a=Array.FindIndex(Sockets,n=>n.id==link.a),b=Array.FindIndex(Sockets,n=>n.id==link.b);
                if(index==a)next[b]=link.map[choice];else if(index==b)next[a]=Array.IndexOf(link.map,choice);
            }
            int turns=0,splits=0;
            for(int i=0;i<next.Length;i++)if(next[i]>0){if(Sockets[i].choices[next[i]-1].kind=="turn")turns++;else splits++;}
            if(turns>Level.turns||splits>Level.splitters)
            {Message="库存不足"+(LinkDescription(index).Length>0?" · 联动两处需要同时占用两件":" · 先从其他插槽取回");Publish();return false;}
            history.Add((int[])state.Clone());if(history.Count>20)history.RemoveAt(0);
            state=next;Selected=index;Moves++;if(Level.spatial?.Controls(Sockets[index].id)==true)SpatialEdits++;Clear();Message=wanted==null?"元件已取回库存":Sockets[index].title+" · "+wanted.name;
            if(LinkDescription(index).Length>0)Message+=" · 联动另一处已同步";Publish();return true;
        }
        public void Install(string kind)
        {if(Selected<0)return;int c=Array.FindIndex(Sockets[Selected].choices,o=>o.kind==kind);if(c<0){Message="这个插槽没有该元件的合法出口";Publish();return;}Set(Selected,c+1);}
        public void Rotate(int delta)
        {
            if(Selected<0||Installed(Selected)==null)return;string kind=Installed(Selected).kind;
            var choices=Enumerable.Range(1,Sockets[Selected].choices.Length).Where(c=>Sockets[Selected].choices[c-1].kind==kind).ToArray();
            int current=Array.IndexOf(choices,state[Selected]);Set(Selected,choices[(current+delta%choices.Length+choices.Length)%choices.Length]);
        }
        public void Remove(){if(Selected>=0)Set(Selected,0);}
        public void Undo(){if(Running||history.Count==0)return;state=history[history.Count-1];history.RemoveAt(history.Count-1);Moves=Math.Max(0,Moves-1);Clear();Message="已撤销 · 布局恢复";Publish();}
        public void Reset(){state=new int[Sockets.Length];history.Clear();Selected=-1;Moves=Attempts=Hints=SpatialEdits=0;SpatialLessonDismissed=false;SpatialControlChosen=false;Clear();Message=Level.lesson;Publish();}
        public void Test(){if(Running||Solved)return;ReviewingFailure=false;Result=EnergyBoxSolver.Evaluate(Level,state);Elapsed=0;Running=true;Paused=false;Attempts++;Message="观察能量流向 · 可暂停并逐段查看";Publish();}
        public void Tick(float seconds){if(Running&&!Paused)Advance(seconds);}
        private void Advance(float seconds)
        {
            Elapsed=Mathf.Min(Result.Duration,Elapsed+Mathf.Max(0,seconds));
            if(Elapsed<Result.Duration)return;Running=false;Paused=false;Solved=Result.Complete;Message=Result.Message+(Result.Diagnosis!=null?"\n"+Result.Diagnosis.Question:"");
            if(!Solved){Inspected=Result.FirstFailure;HintTarget=Inspected;}
            Publish();
        }
        public void TogglePause(){if(!Running)return;Paused=!Paused;Publish();}
        public void Step(){if(!Running)return;Paused=true;float at=Result.traces.Where(t=>t.end>Elapsed+.01f).Select(t=>t.end).DefaultIfEmpty(Result.Duration).Min();Advance(at-Elapsed+.015f);Publish();}
        public void Stop(){if(!Running)return;Running=false;Paused=false;Message="已停止 · 可以继续调整，或重新开始";Publish();}
        public void Hint()
        {
            if(Running||Solved||Hints>=3)return;
            solutions=solutions??EnergyBoxSolver.Solutions(Level);
            var best=solutions.OrderBy(s=>Enumerable.Range(0,state.Length).Count(i=>state[i]!=s[i])).FirstOrDefault();
            if(best==null){Message="当前关卡缺少可行解，请返回选关";Publish();return;}
            int next=Enumerable.Range(0,state.Length).FirstOrDefault(i=>state[i]!=best[i]);
            bool ready=best.SequenceEqual(state);HintTarget=ready?Level.nodes.First(n=>n.kind=="goal").id:Sockets[next].id;
            int tier=++Hints;
            if(tier==1)HintTarget=null;
            if(Level.spatial!=null&&tier<3)
            {
                Message=tier==1?"提示 1 / 观察："+Level.relic.instruction:"提示 2 / 关系："+SpatialReport+"\n"+Level.hints[0];
                Publish();return;
            }
            if(Observation!=null&&tier<3)
            {
                HintTarget=tier==1?null:HintTarget;
                Message=tier==1?"提示 1 / 观察："+Observation.Clue.instruction:
                    "提示 2 / 关系："+Observation.Clue.discovery+"\n"+Level.hints[0];
                Publish();return;
            }
            Message=ready?"布局满足条件 · 点“开始”验证完整路线":tier==1?"提示 1 / 规则："+Level.hints[0]:
                tier==2?"提示 2 / 位置：留意发光的 "+Sockets[next].title+"。"+Level.hints[1]:
                "提示 3 / 下一步："+Sockets[next].title+(best[next]==0?"取回元件": "选择“"+Sockets[next].choices[best[next]-1].name+"”")+
                (LinkDescription(next).Length>0?"，另一处会一起改变":"")+"。";
            Publish();
        }
        public string GoalSummary => string.Join("    ·    ",Level.nodes.Where(n=>n.kind=="goal").Select(n=>n.title.Replace("终点 ","")+"  "+n.units+"格"+(n.code.Length>0?" / "+EnergyBoxSolver.RuneText(n.code):"")));
        public string GoalReport(BoxNode node)
        {
            string expected="需要 "+node.units+" 格"+(node.code.Length>0?" / "+EnergyBoxSolver.RuneText(node.code):" / 无锁印要求");
            if(Result==null)return expected+"\n尚未开始";
            if(!TestFinished)return expected+(Running?"\n星光仍在传递":"\n已停止，请重新点“开始”");
            if(!Result.received.TryGetValue(node.id,out int amount))return expected+"\n实际：未抵达";
            string actual="实际 "+amount+" 格 / "+EnergyBoxSolver.RuneText(Result.signatures.GetValueOrDefault(node.id));
            return expected+"\n"+actual+(Level.syncWindow>0?" / 第 "+Result.arrivals[node.id].ToString("0")+" 拍":"")+(Result.failures.ContainsKey(node.id)?"  ×":"  ✓");
        }
        public void FocusFailure()
        {
            if(!TestFinished||Result.Complete)return;
            Inspected=Result.FirstFailure;HintTarget=Inspected;Message=Result.Message;Publish();
        }
        /// <summary>Reuses the completed experiment, without counting an attempt or changing layout.</summary>
        public void ReviewFailure()
        {
            if(!TestFinished||Result.Complete||Result.Diagnosis==null)return;
            ReviewingFailure=true;Elapsed=Mathf.Clamp(Result.Diagnosis.Time,0,Result.Duration);
            Running=true;Paused=true;Inspected=HintTarget=Result.FirstFailure;
            Message="首错定格 · "+Result.Diagnosis.Stamp(Level.syncWindow>0)+" · "+Result.Diagnosis.Evidence;
            Publish();
        }
        public string FailureDetail
        {
            get
            {
                if(!TestFinished||Result.Complete)return "星光完整运行后显示首个阻断与实际结果";
                var node=Level.nodes.FirstOrDefault(n=>n.id==Result.FirstFailure);
                if(node==null)return Result.Message;
                if(node.kind=="goal")return node.title+"："+GoalReport(node).Replace("\n","；");
                string actual=Result.incoming.TryGetValue(node.id,out var code)?EnergyBoxSolver.RuneText(code):"未抵达";
                if(node.gate.Length>0)return node.title+"：要求 "+EnergyBoxSolver.RuneText(node.gate)+"；实际 "+actual;
                if(node.swapFrom.Length>0)return node.title+"：要求末印 "+node.swapFrom+"；实际 "+actual;
                return Result.Message;
            }
        }
        private void Clear(){Result=null;HintTarget=null;Elapsed=0;Running=Paused=Solved=ReviewingFailure=false;}
    }
    [Serializable] public sealed class EnergyBoxProgress {public int version=1;public List<string> cleared=new List<string>();}
}

