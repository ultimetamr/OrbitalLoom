using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OrbitalLoom.Gameplay
{
    /// <summary>Metric geometry shared by solver, rendering and accessibility reports.
    /// Inspection yaw is cosmetic; only committed socket choices alter the apparatus.</summary>
    [Serializable] public sealed class SpatialMechanism
    {
        public string kind = "";
        public string[] controls = Array.Empty<string>();
        public float speed = .30f;
        public bool IsEmpty => string.IsNullOrEmpty(kind);
        public void Validate(EnergyBoxLevel level)
        {
            int count=kind=="bridge"?2:kind=="mirror"?1:kind=="path"?3:0;
            if(count==0||controls==null||controls.Length!=count||controls.Distinct().Count()!=count||
                !Data.Point3.IsFinite(speed)||speed<.1f||speed>1)throw new FormatException("空间机关配置无效");
            foreach(var id in controls)
            {
                var node=level.nodes.FirstOrDefault(n=>n.id==id);
                if(node==null||node.kind!="socket"||node.choices.Any(c=>c.kind!="turn"||c.outputs.Length!=1||
                    c.outputs[0]!=node.choices[0].outputs[0]||!Data.Point3.IsFinite(c.angle)||Mathf.Abs(c.angle)>180||
                    c.loops<0||c.loops>4||c.delay!=0))throw new FormatException("空间机关控制插槽无效");
            }
        }
        public bool Controls(string id)=>controls.Contains(id);
        public static Vector3 Pivot(int side)=>new Vector3(side==0?-.24f:.24f,0,0);
        public const float ArmLength=.33941125f;
        public static readonly Bounds BridgeObstacle=new Bounds(new Vector3(0,0,-.24f),new Vector3(.18f,.14f,.16f));
        public static Vector3 Tip(int side,float angle)=>Pivot(side)+Quaternion.Euler(0,angle,0)*Vector3.forward*ArmLength;
        public static bool Blocked(Vector3 a,Vector3 b)
        {
            if(BridgeObstacle.Contains(a)||BridgeObstacle.Contains(b))return true;
            return BridgeObstacle.IntersectRay(new Ray(a,(b-a).normalized),out float at)&&at<=Vector3.Distance(a,b);
        }
        public static readonly Vector3 LightSource=new Vector3(-.42f,0,-.27f);
        public static readonly Vector3 MirrorCenter=new Vector3(0,0,-.27f);
        public static readonly Vector3 Receiver=new Vector3(0,0,.28f);
        public static Vector3 Reflected(float angle)=>Vector3.Reflect(Vector3.right,Quaternion.Euler(0,angle,0)*Vector3.forward).normalized;
        public static float MirrorMiss(float angle)
        {
            var ray=Reflected(angle);float along=Vector3.Dot(Receiver-MirrorCenter,ray);
            return along<=0?Vector3.Distance(Receiver,MirrorCenter):Vector3.Distance(MirrorCenter+ray*along,Receiver);
        }
        // One extra fold adds exactly 0.30m, hence one beat at the authored
        // 0.30m/beat speed. Durations are derived from vertices, never loop labels.
        public static Vector3[] Path(int row,int loops)
        {
            float y=(1-row)*.24f;
            return new[]{new Vector3(-.15f,y,0),new Vector3(-.15f,y,.15f*loops),
                new Vector3(.15f,y,.15f*loops),new Vector3(.15f,y,0)};
        }
        public static float Length(Vector3[] path)
        {float length=0;for(int i=1;i<path.Length;i++)length+=Vector3.Distance(path[i-1],path[i]);return length;}
        public static Vector3 Along(Vector3[] path,float fraction)
        {
            float remaining=Mathf.Clamp01(fraction)*Length(path);
            for(int i=1;i<path.Length;i++){float span=Vector3.Distance(path[i-1],path[i]);if(remaining<=span&&span>.0001f)return Vector3.Lerp(path[i-1],path[i],remaining/span);remaining-=span;}
            return path[path.Length-1];
        }
        public BoxChoice Choice(EnergyBoxLevel level,int[] state,int control)
        {
            var sockets=level.nodes.Where(n=>n.kind=="socket").ToArray();int i=Array.FindIndex(sockets,n=>n.id==controls[control]);
            return state[i]==0?null:sockets[i].choices[state[i]-1];
        }
        public bool Passes(EnergyBoxLevel level,int[] state,out string reason)
        {
            var choices=Enumerable.Range(0,controls.Length).Select(i=>Choice(level,state,i)).ToArray();
            if(choices.Any(c=>c==null)){reason="先安装空间机关的全部转向件";return false;}
            if(kind=="bridge")
            {
                float gap=Vector3.Distance(Tip(0,choices[0].angle),Tip(1,choices[1].angle));
                bool blocked=Blocked(Pivot(0),Tip(0,choices[0].angle))||Blocked(Pivot(1),Tip(1,choices[1].angle));
                reason=gap>.012f?"桥口相距 "+(gap*100).ToString("0.0")+" cm；两端须在空间中重合":
                    blocked?"桥口已对齐，但前层暗石阻断；转到另一侧绕行":"双桥臂已接通 · 光路绕开暗石";
                return gap<=.012f&&!blocked;
            }
            if(kind=="mirror")
            {
                float miss=MirrorMiss(choices[0].angle);reason=miss<=.018f?"反射光已命中水晶":"反射光偏离接收器 "+(miss*100).ToString("0.0")+" cm；调整镜面方向";return miss<=.018f;
            }
            reason="折返光路已展开；同拍结果由全路线长度共同决定";return true;
        }
        public float Travel(EnergyBoxLevel level,int[] state,string from)
        {
            int i=Array.IndexOf(controls,from);if(kind!="path"||i<0)return 1;
            var c=Choice(level,state,i);return c==null?1:Length(Path(i,c.loops))/speed;
        }
        public bool LessonReady(EnergyBoxLevel level,int[] state)
        {
            if(!Passes(level,state,out _))return false;
            if(kind!="path")return true;
            float[] total=Enumerable.Range(0,3).Select(i=>3+i+Travel(level,state,controls[i])).ToArray();
            return total.Max()-total.Min()<=level.syncWindow;
        }
        public string Report(EnergyBoxLevel level,int[] state)
        {
            Passes(level,state,out string reason);
            if(kind!="path")return string.Join(" / ",Enumerable.Range(0,controls.Length).Select(i=>
            {var c=Choice(level,state,i);return controls[i].ToUpperInvariant()+" "+(c==null?"未装":c.angle.ToString("0")+"°");}))+"\n"+reason;
            return string.Join(" / ",Enumerable.Range(0,controls.Length).Select(i=>
            {var c=Choice(level,state,i);return new[]{"月","星","日"}[i]+(c==null?"未装":Length(Path(i,c.loops)).ToString("0.00")+"m · "+Travel(level,state,controls[i]).ToString("0")+"拍");}));
        }
    }
}
