using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalLoom.Gameplay;
namespace OrbitalLoom.Audio
{
    public sealed class LoomCue
    {
        public float beat,pan,gain;
        public string clip;
    }
    /// <summary>Pure score compiled from the actual test trace. Logical beats
    /// remain independent of audio availability and of node coordinates.</summary>
    public static class LoomScore
    {
        public const int Bpm=84;
        public const double SecondsPerBeat=60.0/Bpm;
        public static List<LoomCue> Build(EnergyBoxLevel level,BoxEvaluation result)
        {
            var cues=new List<LoomCue>();if(level.syncWindow<=0||result==null)return cues;
            void Add(float beat,string clip,float pan,float gain)=>cues.Add(new LoomCue{beat=beat,clip=clip,pan=pan,gain=gain});
            float last=result.traces.Count==0?0:result.traces.Max(t=>t.end);
            for(int beat=0;beat<=Math.Ceiling(last);beat++)Add(beat,beat%4==0?"downbeat":"tick",0,.38f);
            var goals=level.nodes.Where(n=>n.kind=="goal").ToArray();
            foreach(var goal in goals)
            {
                if(!result.arrivals.TryGetValue(goal.id,out var at))continue;
                // A timing mismatch is heard as separated notes, not an early
                // alarm or a premature completion chord. Wrong locks stay dull.
                bool valid=result.received[goal.id]==goal.units&&(goal.code.Length==0||result.signatures[goal.id]==goal.code);
                int index=Array.IndexOf(goals,goal);
                Add(at,valid?new[]{"goal-low","goal-mid","goal-high"}[index%3]:"error",goal.x*.35f,valid?.60f:.28f);
            }
            foreach(var n in level.nodes.Where(n=>n.kind=="socket"))
            {
                if(!result.arrivals.TryGetValue(n.id,out var arrival))continue;
                var outgoing=result.traces.Where(t=>t.from==n.id).ToArray();if(outgoing.Length==0)continue;
                float departure=outgoing.Min(t=>t.start);
                for(float beat=arrival;beat<departure-.01f;beat+=1)Add(beat,"wait",n.x*.35f,.24f);
            }
            // One low note per active beat prevents many parallel rail sounds
            // accumulating into an unpleasant burst of identical attacks.
            foreach(var group in result.traces.Where(t=>!goals.Any(g=>g.id==t.to)).GroupBy(t=>t.end))
                Add(group.Key,"flow",group.Average(t=>level.nodes.First(n=>n.id==t.to).x)*.3f,.32f);
            return cues.OrderBy(c=>c.beat).ToList();
        }
    }
}
