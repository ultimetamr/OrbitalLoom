using System;
using System.Collections.Generic;
using System.Linq;
using OrbitalLoom.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrbitalLoom.Editor
{
    /// <summary>Author and save physical evidence before Spatial Adapter exports it.</summary>
    public static class OrbitalLoomRelicSetup
    {
        private const string Folder="Assets/OrbitalLoomGenerated/Relics/";
        public static void Apply(EnergyBoxView view)
        {
            if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/OrbitalLoomGenerated","Relics");
            var relic=view.GetComponent<RelicView>()??Undo.AddComponent<RelicView>(view.gameObject);
            Undo.RecordObject(view,"Bind observation exhibits");view.relic=relic;
            Undo.RecordObject(relic,"Author spatial evidence and restoration");
            var root=Root(view.transform,"Spatial evidence");
            foreach(Transform child in root.Cast<Transform>().ToArray())Undo.DestroyObjectImmediate(child.gameObject);
            var rotor=Root(root,"Inspection rotor");relic.rotor=rotor;
            var gold=Mat("Bronze inscription","BFA875");var stone=Mat("Relic stone","263F46");
            var aqua=Mat("Crystal evidence","8FDFD3");var violet=Mat("Mirror evidence","B3A6E1");
            var groups=new[]{Root(rotor,"Parallax fragments"),Root(rotor,"Mirror testimony"),Root(rotor,"Three voice measure")};relic.exhibits=groups;
            var machineRoot=Root(root,"Playable spatial mechanism");
            var machine=Undo.AddComponent<SpatialMechanismView>(machineRoot.gameObject);relic.mechanism=machine;
            machine.rotor=Root(machineRoot,"Committed spatial geometry");
            // A metric rail must have unit box bounds. A flattened decorative
            // socket jacket tapers into wedges and misrepresents its endpoints.
            var unitBox=Save("Metric unit box",()=>SolidStrokes(new List<(Vector3,Vector3)>{(Vector3.left*.5f,Vector3.right*.5f)},1));
            machine.plinth=Shape(machine.rotor,"Carved mechanism plinth",unitBox,Vector3.zero,Vector3.one,stone);
            machine.bars=Enumerable.Range(0,16).Select(i=>Shape(machine.rotor,"Physical rail "+i,unitBox,Vector3.zero,Vector3.one,aqua)).ToArray();
            machine.crystals=Enumerable.Range(0,8).Select(i=>Shape(machine.rotor,"Dock crystal "+i,view.art.sourceBody,Vector3.zero,Vector3.one,aqua)).ToArray();
            machine.flashes=Enumerable.Range(0,3).Select(i=>Shape(machine.rotor,"Metric preview photon "+i,view.art.sourceBody,Vector3.zero,Vector3.one,gold)).ToArray();
            machine.obstacle=Shape(machine.rotor,"Front depth obstruction",unitBox,Vector3.zero,Vector3.one,stone);
            machine.mirror=Shape(machine.rotor,"Reflecting crystal plane",unitBox,Vector3.zero,Vector3.one,violet);
            // Static engraving is combined into one saved mesh per exhibit.
            // The readable marks communicate pivot axes, mirror bounds and length.
            var pivotLines=new List<(Vector3,Vector3)>();
            for(int pivot=0;pivot<2;pivot++)for(int mark=0;mark<16;mark++)
            {
                Vector3 At(float angle)=>Gameplay.SpatialMechanism.Pivot(pivot)+new Vector3(Mathf.Cos(angle)*.09f,-.025f,Mathf.Sin(angle)*.09f);
                pivotLines.Add((At(mark*Mathf.PI/8),At((mark+.65f)*Mathf.PI/8)));
            }
            machine.bridgeMarks=Shape(machine.rotor,"Engraved pivot bearings",Save("Pivot bearing engraving",()=>SolidStrokes(pivotLines,.006f)),Vector3.zero,Vector3.one,gold);
            var frameLines=new List<(Vector3,Vector3)>();
            var corners=new[]{new Vector3(-.12f,-.18f,0),new Vector3(.12f,-.18f,0),new Vector3(.12f,.18f,0),new Vector3(-.12f,.18f,0)};
            for(int i=0;i<4;i++)frameLines.Add((corners[i],corners[(i+1)%4]));
            machine.mirrorFrame=Shape(machine.rotor,"Bronze mirror rim",Save("Mirror rim",()=>SolidStrokes(frameLines,.009f)),Vector3.zero,Vector3.one,gold);
            var measures=new List<(Vector3,Vector3)>();
            for(int i=0;i<=10;i++){float z=i*.075f;measures.Add((new Vector3(-.21f,.23f,z),new Vector3(i%2==0?-.26f:-.24f,.23f,z)));}
            measures.Add((new Vector3(-.21f,.23f,0),new Vector3(-.21f,.23f,.75f)));
            machine.pathMeasure=Shape(machine.rotor,"Carved length ruler",Save("Folded path measure",()=>SolidStrokes(measures,.004f)),Vector3.zero,Vector3.one,gold);
            machineRoot.gameObject.SetActive(false);
            // Alternating strokes are displaced in depth. They form the star
            // only when the rotor is viewed at +45 degrees from the front.
            var lines=new List<(Vector3,Vector3)>();
            var star=Enumerable.Range(0,10).Select(i=>
            {float a=(90+i*36)*Mathf.Deg2Rad,r=i%2==0?.29f:.125f;return new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);}).ToArray();
            for(int i=0;i<10;i++)
            {
                var a=star[i];var b=star[(i+1)%10];var mid=(a+b)*.5f;
                foreach(var half in new[]{(a,mid),(mid,b)})
                {
                    float z=lines.Count%2==0?-.16f:.16f;
                    Vector3 Project(Vector3 p)=>Quaternion.Euler(0,-45,0)*new Vector3(p.x*(1+z/2.4f),p.y*(1+z/2.4f),z);
                    lines.Add((Project(half.Item1),Project(half.Item2)));
                }
            }
            Shape(groups[0],"Split star strokes",Save("Parallax star",()=>Strokes(lines,.010f)),Vector3.zero,Vector3.one,aqua);
            for(int i=0;i<2;i++)
            {
                var frame=Root(groups[0],"Broken evidence frame "+i);frame.localRotation=Quaternion.Euler(0,-45,0);
                frame.localPosition=Quaternion.Euler(0,-45,0)*new Vector3(0,0,i==0?-.18f:.18f);
                var p=Shape(frame,"Fractured edge",view.art.casing,new Vector3(i==0?-.37f:.37f,0,0),new Vector3(.055f,.75f,.035f),stone);
                p.localRotation=Quaternion.Euler(0,0,i==0?-8:8);
            }
            // An opaque tablet physically separates the readable front and back.
            Shape(groups[1],"Opaque mirror tablet",view.art.casing,Vector3.zero,new Vector3(.75f,.55f,.10f),stone);
            var front=Root(groups[1],"Before reflection");front.localPosition=new Vector3(0,0,-.065f);
            Shape(front,"Moon before",view.art.moonMark,new Vector3(-.15f,0,0),Vector3.one*.14f,gold);
            Shape(front,"Star before",view.art.starMark,new Vector3(.15f,0,0),Vector3.one*.14f,gold);
            var back=Root(groups[1],"Reverse inscription");back.localPosition=new Vector3(0,0,.065f);back.localRotation=Quaternion.Euler(0,180,0);
            Shape(back,"Star after",view.art.starMark,new Vector3(-.15f,0,0),Vector3.one*.14f,violet);
            Shape(back,"Moon after",view.art.moonMark,new Vector3(.15f,0,0),Vector3.one*.14f,violet);
            foreach(var face in new[]{front,back})Shape(face,"Reading direction",view.art.turnMark,new Vector3(0,-.15f,-.002f),Vector3.one*.05f,aqua);
            // Three independent depth columns carry 4, 5 and 6 timing marks.
            // Rotating to -45 presents their inscriptions on the same plane.
            for(int column=0;column<3;column++)
            {
                var axis=Root(groups[2],"Timing column "+column);
                axis.localRotation=Quaternion.Euler(0,45,0);axis.localPosition=Quaternion.Euler(0,45,0)*new Vector3((column-1)*.29f,0,0);
                Shape(axis,"Stone measure",view.art.casing,new Vector3(0,0,.035f),new Vector3(.20f,.76f,.065f),stone);
                Shape(axis,"Route rune",column==0?view.art.moonMark:column==1?view.art.starMark:view.art.sunMark,new Vector3(0,.24f,-.04f),Vector3.one*.065f,column==1?violet:aqua);
                for(int beat=0;beat<4+column;beat++)Shape(axis,"Time notch "+beat,view.art.casing,new Vector3(0,.12f-beat*.065f,-.045f),new Vector3(.105f,.020f,.018f),gold);
            }
            var restoration=Root(root,"Restored altar");relic.restorationRoot=restoration;
            var repairs=new List<MeshRenderer>();
            for(int i=0;i<6;i++)
            {
                var p=Shape(restoration,"Restored engraving "+i,view.art.casing,new Vector3((i-2.5f)*.15f,-.43f,.27f),new Vector3(.13f,.018f,.028f),gold);
                repairs.Add(p.GetComponent<MeshRenderer>());
                var c=Shape(restoration,"Restored crystal "+i,view.art.sourceBody,new Vector3((i-2.5f)*.15f,-.39f,.29f),new Vector3(.035f,.07f,.035f),aqua);
                repairs.Add(c.GetComponent<MeshRenderer>());
            }
            relic.restoration=repairs.ToArray();relic.heart=Shape(restoration,"Awakened heart",view.art.sourceBody,new Vector3(0,-.35f,.34f),Vector3.one*.065f,aqua);
            relic.heart.gameObject.SetActive(false);rotor.gameObject.SetActive(false);restoration.gameObject.SetActive(false);
            EditorUtility.SetDirty(relic);EditorUtility.SetDirty(view);
        }
        private static Transform Root(Transform parent,string name)
        {
            var existing=parent.Find(name);if(existing!=null)return existing;
            var item=new GameObject(name);Undo.RegisterCreatedObjectUndo(item,"Create native relic");item.transform.SetParent(parent,false);return item.transform;
        }
        private static Transform Shape(Transform parent,string name,Mesh mesh,Vector3 position,Vector3 scale,Material material)
        {
            var t=Root(parent,name);t.gameObject.layer=2;t.localPosition=position;t.localScale=scale;
            Undo.AddComponent<MeshFilter>(t.gameObject).sharedMesh=mesh;
            var r=Undo.AddComponent<MeshRenderer>(t.gameObject);r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return t;
        }
        private static Material Mat(string name,string color)
        {
            string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name,enableInstancing=true};AssetDatabase.CreateAsset(m,path);}
            Undo.RecordObject(m,"Relic finish");m.SetColor("_BaseColor",LoomPalette.Hex(color));m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);return m;
        }
        private static Mesh Save(string name,Func<Mesh> create)
        {
            string path=Folder+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);var authored=create();authored.name=name;
            if(mesh==null){AssetDatabase.CreateAsset(authored,path);return authored;}
            Undo.RecordObject(mesh,"Author depth fragments");EditorUtility.CopySerialized(authored,mesh);UnityEngine.Object.DestroyImmediate(authored);EditorUtility.SetDirty(mesh);return mesh;
        }
        private static Mesh Strokes(List<(Vector3 a,Vector3 b)> strokes,float width)
        {
            var v=new List<Vector3>();var t=new List<int>();
            foreach(var line in strokes)
            {
                var side=Vector3.Cross(line.b-line.a,Vector3.forward).normalized*width;int k=v.Count;
                v.AddRange(new[]{line.a-side,line.a+side,line.b-side,line.b+side});t.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});
            }
            var m=new Mesh();m.SetVertices(v);m.SetUVs(0,v.Select(_=>Vector2.zero).ToList());m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        private static Mesh SolidStrokes(List<(Vector3 a,Vector3 b)> strokes,float width)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh=cube.GetComponent<MeshFilter>().sharedMesh;
            var pieces=strokes.Select(line=>new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS((line.a+line.b)*.5f,
                Quaternion.FromToRotation(Vector3.right,line.b-line.a),new Vector3(Vector3.Distance(line.a,line.b),width,width))}).ToArray();
            var combined=new Mesh();combined.CombineMeshes(pieces,true,true);UnityEngine.Object.DestroyImmediate(cube);
            return combined;
        }
    }
}
