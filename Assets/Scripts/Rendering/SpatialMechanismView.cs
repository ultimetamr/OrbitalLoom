using System;
using System.Linq;
using OrbitalLoom.Gameplay;
using UnityEngine;

namespace OrbitalLoom.Rendering
{
    /// <summary>Saved mesh pool: backend-native transforms only, no runtime meshes
    /// and no colliders in the manipulation exhibit. Geometry comes from the solver.</summary>
    public sealed class SpatialMechanismView : MonoBehaviour
    {
        public Transform[] bars, crystals, flashes;
        public Transform mirror,plinth,obstacle;
        public Transform rotor;
        public Transform bridgeMarks,mirrorFrame,pathMeasure;
        private EnergyBoxSession session;
        private int usedBars,usedCrystals;
        private readonly Vector3[][] previewPaths=new Vector3[3][];
        public void Bind(EnergyBoxSession current,bool visible)
        {
            session=current;gameObject.SetActive(visible&&current?.Level.spatial!=null);
            if(!gameObject.activeSelf)return;
            usedBars=usedCrystals=0;var rig=current.Level.spatial;var state=current.Snapshot();
            // Tilt the tabletop toward the viewer: an eye-level XZ plane
            // otherwise collapses to a line in projection even after yawing.
            rotor.localRotation=Quaternion.Euler(rig.kind=="path"?45:30,(current.Observation?.Angle??0)+(rig.kind=="path"?35:20),0);
            rotor.localPosition=new Vector3(0,rig.kind=="path"?.18f:.06f,0);
            rotor.localScale=Vector3.one*(rig.kind=="path"?1.15f:1.6f);
            plinth.localPosition=new Vector3(0,rig.kind=="path"?-.33f:-.17f,.12f);
            plinth.localScale=new Vector3(.78f,.025f,.68f);
            // Keep the playable rail silhouettes unobstructed from seated and
            // rotated viewpoints. Engraved bearings supply the visual anchors.
            plinth.gameObject.SetActive(false);
            bool complete=rig.Passes(current.Level,state,out _);
            if(bridgeMarks!=null)bridgeMarks.gameObject.SetActive(rig.kind=="bridge");
            if(mirrorFrame!=null)mirrorFrame.gameObject.SetActive(rig.kind=="mirror");
            if(pathMeasure!=null)pathMeasure.gameObject.SetActive(rig.kind=="path");
            var lit=complete?LoomPalette.Gold:LoomPalette.Hex("91D9DB");
            mirror.gameObject.SetActive(rig.kind=="mirror");
            obstacle.gameObject.SetActive(rig.kind=="bridge");
            obstacle.localPosition=SpatialMechanism.BridgeObstacle.center;obstacle.localScale=SpatialMechanism.BridgeObstacle.size;
            foreach(var f in flashes)f.gameObject.SetActive(false);
            if(rig.kind=="bridge")
            {
                for(int i=0;i<2;i++)
                {
                    var c=rig.Choice(current.Level,state,i);float angle=c?.angle??0;
                    var start=SpatialMechanism.Pivot(i);var end=SpatialMechanism.Tip(i,angle);
                    Crystal(start,LoomPalette.Hex("BFA875"),.065f);Crystal(end,c==null?LoomPalette.Hex("526D76"):lit,.038f);
                    Bar(start,end,c==null?LoomPalette.Hex("526D76"):lit,.020f);
                    // A thin fixed plinth makes depth and the two pivot axes legible.
                    Bar(start+Vector3.down*.10f,start,LoomPalette.Hex("526D76"),.033f);
                }
            }
            else if(rig.kind=="mirror")
            {
                var c=rig.Choice(current.Level,state,0);float angle=c?.angle??0;
                mirror.localPosition=SpatialMechanism.MirrorCenter;
                mirror.localRotation=Quaternion.Euler(0,angle,0);mirror.localScale=new Vector3(.22f,.34f,.018f);
                if(mirrorFrame!=null){mirrorFrame.localPosition=mirror.localPosition;mirrorFrame.localRotation=mirror.localRotation;}
                Crystal(SpatialMechanism.LightSource,LoomPalette.Gold,.065f);
                Crystal(SpatialMechanism.Receiver,complete?LoomPalette.Gold:LoomPalette.Repulsion,.075f);
                Bar(SpatialMechanism.LightSource,SpatialMechanism.MirrorCenter,LoomPalette.Gold,.009f);
                if(c!=null)Bar(SpatialMechanism.MirrorCenter,SpatialMechanism.MirrorCenter+SpatialMechanism.Reflected(angle)*.55f,complete?LoomPalette.Gold:LoomPalette.Error,.009f);
            }
            else
            {
                for(int row=0;row<3;row++)
                {
                    var c=rig.Choice(current.Level,state,row);var p=SpatialMechanism.Path(row,c?.loops??0);previewPaths[row]=c==null?null:p;
                    Color color=new[]{LoomPalette.Repulsion,LoomPalette.Splitter,LoomPalette.Gold}[row];
                    for(int i=1;i<p.Length;i++)Bar(p[i-1],p[i],c==null?LoomPalette.Hex("526D76"):color,.011f);
                    Crystal(p[0],color,.035f);Crystal(p[p.Length-1],color,.035f);
                }
            }
            for(int i=usedBars;i<bars.Length;i++)bars[i].gameObject.SetActive(false);
            for(int i=usedCrystals;i<crystals.Length;i++)crystals[i].gameObject.SetActive(false);
        }
        private void LateUpdate()
        {
            if(session?.Level.spatial==null||session.Level.spatial.kind!="path")return;
            // Preview packets use the exact arc-length parametrization and speed
            // used by the solver. This is inspection, never a completion signal.
            var rig=session.Level.spatial;
            for(int row=0;row<3;row++)
            {
                var path=previewPaths[row];bool animate=Core.GameManager.Instance?.Settings.particles==true&&!Core.GameManager.Instance.Paused;
                flashes[row].gameObject.SetActive(path!=null&&animate);
                if(path==null)continue;
                flashes[row].localPosition=SpatialMechanism.Along(path,Mathf.Repeat(Time.unscaledTime*rig.speed,SpatialMechanism.Length(path))/SpatialMechanism.Length(path));
                flashes[row].localScale=Vector3.one*.025f;
            }
        }
        private void Bar(Vector3 a,Vector3 b,Color color,float width)
        {
            if(usedBars>=bars.Length)throw new InvalidOperationException("空间导轨池容量不足");
            var t=bars[usedBars++];t.gameObject.SetActive(true);t.localPosition=(a+b)*.5f;
            t.localRotation=Quaternion.FromToRotation(Vector3.right,b-a);t.localScale=new Vector3(Mathf.Max(.001f,Vector3.Distance(a,b)),width,width);
            LoomPalette.SetColor(t.GetComponent<Renderer>(),color);
        }
        private void Crystal(Vector3 at,Color color,float size)
        {
            var t=crystals[usedCrystals++];t.gameObject.SetActive(true);t.localPosition=at;t.localRotation=Quaternion.Euler(0,25,0);t.localScale=Vector3.one*size;
            LoomPalette.SetColor(t.GetComponent<Renderer>(),color);
        }
    }
}
