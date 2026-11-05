using System.Collections.Generic;
using System.Linq;
using OrbitalLoom.Rendering;
using OrbitalLoom.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace OrbitalLoom.Editor
{
    /// <summary>Persistent ruin geometry is authored here, never spawned by
    /// device gameplay. All ornaments use the excluded raycast layer.</summary>
    public static class OrbitalLoomObservatorySetup
    {
        private const string Folder="Assets/OrbitalLoomGenerated/StarInstrument/";
        public static void Apply(OrbitalLoomUI ui,EnergyBoxView view)
        {
            var hero=ui.menuSculpture;
            foreach(Transform child in hero.Cast<Transform>().ToArray())Undo.DestroyObjectImmediate(child.gameObject);
            Undo.RecordObject(hero,"Place crystal ruin");hero.localPosition=new Vector3(-.56f,.06f,-.22f);hero.localRotation=Quaternion.identity;hero.localScale=Vector3.one;
            var existing=hero.GetComponent<ObservatoryMotion>();var motion=existing!=null?existing:Undo.AddComponent<ObservatoryMotion>(hero.gameObject);
            var stone=Mat("Weathered blue stone","3C565B");var pale=Mat("Ancient limestone","9BAFA9");
            var bronze=Mat("Timeworn bronze","BFA875");var aqua=Mat("Luminous aquamarine","8FDFD3");var violet=Mat("Amethyst facet","B3A6E1");
            var arc=StoneArc();
            // Missing wedges and uneven depth read as recovered architecture,
            // rather than a complete machine ring or a flat graphic backdrop.
            for(int i=0;i<10;i++)
            {
                if(i==2||i==7)continue;
                var piece=Shape(hero,"Broken stone arch "+i,arc,Vector3.one,i%3==0?pale:stone);
                float angle=i*36;piece.localRotation=Quaternion.Euler(0,0,angle);piece.localPosition=new Vector3(0,0,.09f+(i%3)*.012f);
                float a=(angle+14)*Mathf.Deg2Rad;
                var mark=Shape(hero,"Ancient rune "+i,i%2==0?view.art.moonMark:view.art.starMark,Vector3.one*.023f,aqua);
                mark.localPosition=new Vector3(Mathf.Cos(a)*.343f,Mathf.Sin(a)*.343f,.027f);mark.localRotation=Quaternion.Euler(0,0,angle+14);
            }
            var core=Shape(hero,"Suspended heart crystal",view.art.sourceBody,new Vector3(.26f,.39f,.24f),aqua);core.localRotation=Quaternion.Euler(8,27,10);
            var rings=new Transform[2];
            for(int i=0;i<2;i++)
            {
                rings[i]=Shape(hero,"Rune orbit "+i,view.art.orbit,Vector3.one*(.225f-i*.035f),i==0?bronze:violet);
                rings[i].localRotation=Quaternion.Euler(67+i*34,20+i*60,12);
                for(int j=0;j<3;j++)
                {
                    var shard=Shape(rings[i],"Orbit crystal "+j,view.art.sourceBody,new Vector3(.10f,.19f,.10f),i==0?aqua:violet);
                    float a=j*2*Mathf.PI/3;shard.localPosition=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);shard.localRotation=Quaternion.Euler(0,j*35,20);
                }
            }
            var lower=Shape(hero,"Fractured altar",view.art.casing,new Vector3(.53f,.22f,.20f),stone);lower.localPosition=new Vector3(0,-.43f,.06f);lower.localRotation=Quaternion.Euler(72,0,0);
            var cap=Shape(hero,"Altar crown",view.art.casing,new Vector3(.39f,.12f,.16f),pale);cap.localPosition=new Vector3(0,-.35f,.05f);cap.localRotation=Quaternion.Euler(70,0,0);
            for(int i=0;i<4;i++)
            {
                var shard=Shape(hero,"Recovered crystal "+i,view.art.sourceBody,new Vector3(.045f,.11f+i*.015f,.05f),i%2==0?aqua:violet);
                shard.localPosition=new Vector3((i-1.5f)*.105f,-.34f,.015f);shard.localRotation=Quaternion.Euler(0,20*i,(i-1.5f)*12);
            }
            Undo.RecordObject(motion,"Crystal orbit motion");motion.rings=rings;EditorUtility.SetDirty(motion);
            foreach(var node in view.nodes)
            {
                var child=node.transform.Find("Ceramic collar")??node.transform.Find("Relic collar");
                if(child==null)child=Shape(node.transform,"Relic collar",view.art.orbit,Vector3.one*.070f,bronze);
                Undo.RecordObject(child.gameObject,"Relic collar name");child.name="Relic collar";
                Undo.RecordObject(child,"Relic mounting");child.localPosition=new Vector3(0,0,.006f);child.localScale=new Vector3(.070f,.070f,.035f);
                Undo.RecordObject(child.GetComponent<MeshRenderer>(),"Relic collar finish");child.GetComponent<MeshRenderer>().sharedMaterial=bronze;
            }
            var baseRoot=view.transform.Find("Instrument balance");
            if(baseRoot==null){var item=new GameObject("Instrument balance");Undo.RegisterCreatedObjectUndo(item,"Create relic stand");baseRoot=item.transform;baseRoot.SetParent(view.transform,false);}
            else foreach(Transform child in baseRoot.Cast<Transform>().ToArray())Undo.DestroyObjectImmediate(child.gameObject);
            // A shallow interrupted arc beneath the puzzle leaves all routes,
            // node captions and the reading panel unobstructed.
            for(int i=0;i<5;i++)
            {
                var block=Shape(baseRoot,"Relic foundation "+i,view.art.casing,new Vector3(.15f,.055f,.075f),stone);
                block.localPosition=new Vector3((i-2)*.23f,-.43f+Mathf.Abs(i-2)*.008f,.23f);block.localRotation=Quaternion.Euler(0,0,(i-2)*-4);
                var shard=Shape(baseRoot,"Foundation crystal "+i,view.art.sourceBody,new Vector3(.025f,.045f,.025f),i%2==0?aqua:violet);
                shard.localPosition=block.localPosition+new Vector3(0,.027f,-.01f);
            }
            Undo.RecordObject(view,"Bind relic stand");view.stand=baseRoot;EditorUtility.SetDirty(view);EditorUtility.SetDirty(ui);
        }
        private static Mesh StoneArc()
        {
            string path=Folder+"Broken arch wedge.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            var v=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            const int steps=5;
            for(int s=0;s<=steps;s++)
            {
                float angle=(s*28f/steps)*Mathf.Deg2Rad;
                for(int layer=0;layer<2;layer++)for(int ring=0;ring<2;ring++)
                {float radius=ring==0?.30f:.385f;v.Add(new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,layer==0?-.04f:.04f));uv.Add(new Vector2(layer==0?.9f:.25f,.5f));}
            }
            for(int s=0;s<steps;s++)
            {
                int a=s*4,b=(s+1)*4;
                triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b,a+2,b+2,a+3,a+3,b+2,b+3,a,b,a+2,a+2,b,b+2,a+1,a+3,b+1,a+3,b+3,b+1});
            }
            int end=steps*4;triangles.AddRange(new[]{0,2,1,1,2,3,end,end+1,end+2,end+1,end+3,end+2});
            mesh=new Mesh{name="Broken arch wedge"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        private static Material Mat(string name,string color)
        {
            string path=Folder+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name,enableInstancing=true};AssetDatabase.CreateAsset(material,path);}
            Undo.RecordObject(material,"Ruin finish");material.SetColor("_BaseColor",LoomPalette.Hex(color));material.SetFloat("_Cull",0);
            var ramp=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Facet light ramp.png");material.SetTexture("_MainTex",ramp);material.SetTexture("_BaseMap",ramp);EditorUtility.SetDirty(material);return material;
        }
        private static Transform Shape(Transform parent,string name,Mesh mesh,Vector3 scale,Material material)
        {
            var item=new GameObject(name);Undo.RegisterCreatedObjectUndo(item,"Author crystal ruin");item.layer=2;item.transform.SetParent(parent,false);item.transform.localScale=scale;
            Undo.AddComponent<MeshFilter>(item).sharedMesh=mesh;var renderer=Undo.AddComponent<MeshRenderer>(item);renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return item.transform;
        }
    }
}
