using System;
using System.Linq;
using UnityEngine;

namespace OrbitalLoom.Gameplay
{
    /// <summary>A causal observation of a completed experiment. Never reads the reference solution.</summary>
    public sealed class BoxDiagnosis
    {
        public string Node { get; private set; }
        public string Category { get; private set; }
        public string Evidence { get; private set; }
        public string Question { get; private set; }
        public float Time { get; private set; }
        public bool Unreached { get; private set; }

        public static BoxDiagnosis From(EnergyBoxLevel level, BoxEvaluation result)
        {
            if(result==null||result.Complete)return null;
            // Missing receivers are consequences. Prefer a reached, failing device,
            // ordered by actual arrival time, before reporting an unpowered target.
            float At(string id)=>result.failureTimes.TryGetValue(id,out var fault)?fault:result.arrivals.TryGetValue(id,out var at)?
                (result.failures[id].Contains("提前")?result.arrivals.Values.Max():at):float.PositiveInfinity;
            string id=result.failures.Keys.OrderBy(At).First();
            var n=level.nodes.FirstOrDefault(x=>x.id==id);
            string text=result.failures[id];
            bool reached=result.received.ContainsKey(id);
            string category=!reached?"路径未抵达":text.Contains("提前")?"抵达不同拍":
                text.Contains("争用")||text.Contains("占用")?"通道争用":text.Contains("循环")?"路径循环":
                level.spatial?.Controls(id)==true?"空间机关未接通":
                text.Contains("印")||text.Contains("条件需")?"符文条件不符":
                text.Contains("格")||text.Contains("均分")||text.Contains("比例")?"能量分配不符":"路线中断";
            string question=category=="抵达不同拍"?"哪一路最早？比较实际路长和等待，先推算差几拍。":
                category=="通道争用"?"哪条支路没有替代通道？先为它留路。":
                category=="空间机关未接通"?"换个角度检查真实接合、阻挡或反射方向。":
                category=="符文条件不符"?"从这个位置往回读：哪一步改变了顺序或删掉了末印？":
                category=="能量分配不符"?"先比较输入总量与各目标需求，再决定在哪里分流。":
                "沿最后一段亮线回看，出口是否真正通向下一接点？";
            return new BoxDiagnosis{Node=id,Category=category,Evidence=text,Question=question,
                Time=reached?At(id):Mathf.Max(0,result.Duration-.5f),Unreached=!reached};
        }
        public string Stamp(bool beats)=>"第 "+Time.ToString("0.##")+(beats?" 拍":" 秒");
    }
}
