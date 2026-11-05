using System;
using System.Collections.Generic;
using System.IO;
using OrbitalLoom.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrbitalLoom.Editor
{
    /// <summary>All headset art is persistent geometry exported by Spatial Adapter.
    /// Bevels, apertures and layered inlays provide depth without screen effects.</summary>
    public static class OrbitalLoomBoxArtSetup
    {
        private const string Folder="Assets/OrbitalLoomGenerated/StarInstrument";
        public static void Apply(EnergyBoxView view)
        {
            Directory.CreateDirectory(Folder);
            var art=view.GetComponent<EnergyBoxArt>();if(art==null)art=Undo.AddComponent<EnergyBoxArt>(view.gameObject);
            Undo.RecordObject(view,"Bind star instrument art");Undo.RecordObject(art,"Author exported instrument meshes");view.art=art;
            art.socketBody=Save("Hex socket",()=>Bevel(6));
            art.sourceBody=Save("Solar crystal",Crystal);
            art.goalBody=Save("Receiver aperture",()=>Torus(24,.37f,.12f));
            art.runeBody=Save("Rune crystal",Crystal);
            art.gateBody=Save("Condition arch",Gate);
            art.converterBody=Save("Converter wheel",()=>Torus(8,.36f,.13f));
            art.blockedBody=Save("Closed plug",()=>Bevel(4));
            art.casing=Save("Bevelled jacket",()=>Bevel(8));
            art.orbit=Save("Instrument orbit",()=>Torus(40,1,.032f));
            art.turnMark=Save("Turn inlay",()=>Mark("turn"));art.splitMark=Save("Split inlay",()=>Mark("split"));
            art.moonMark=Save("Moon inlay",()=>Mark("moon"));art.starMark=Save("Star inlay",()=>Mark("star"));
            art.sunMark=Save("Sun inlay",()=>Mark("sun"));art.goalMark=Save("Receiver inlay",()=>Mark("goal"));
            art.reverseMark=Save("Reverse inlay",()=>Mark("reverse"));art.eraseMark=Save("Erase inlay",()=>Mark("erase"));art.clockMark=Save("Clock inlay",()=>Mark("clock"));
            var ramp=FacetRamp();var opaque=Material("Instrument opaque",LoomPalette.Hex("26394C"));
            Undo.RecordObject(opaque,"Baked facet lighting");
            // Populate both aliases: URP's material upgrader can copy the legacy
            // main texture over BaseMap when a persistent material is imported.
            opaque.SetTexture("_MainTex",ramp);opaque.SetTexture("_BaseMap",ramp);EditorUtility.SetDirty(opaque);
            if(ramp==null)throw new InvalidOperationException("Facet texture import failed");
            AssetDatabase.SaveAssetIfDirty(opaque);
            Debug.Log("[Orbital Loom] Facet material authored: "+AssetDatabase.GetAssetPath(opaque)+" / "+AssetDatabase.GetAssetPath(opaque.GetTexture("_BaseMap")));
            var inlay=Material("Engraved inlay",LoomPalette.Hex("101F30"));
            foreach(var node in view.nodes)
            {
                // Keep the proven collider hierarchy and radii. Decoration is
                // collider-free and ignored by the official input router.
                foreach(var renderer in new[]{node.core,node.shell,node.ring.GetComponent<MeshRenderer>()})
                {Undo.RecordObject(renderer,"Opaque instrument finish");renderer.sharedMaterial=opaque;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
                Undo.RecordObject(node.glyph.GetComponent<MeshRenderer>(),"Readable face engraving");node.glyph.GetComponent<MeshRenderer>().sharedMaterial=inlay;
            }
            art.echoes=Pool(view.assembly,"Arrival resonance",18,art.orbit,Material("Warm resonance",LoomPalette.Gold));
            art.tails=Pool(view.assembly,"Photon tail",24,art.sourceBody,Material("Photon amber",LoomPalette.Hex("AC844B")));
            art.winShards=Pool(view.assembly,"Constellation shard",12,art.sourceBody,Material("Constellation gold",LoomPalette.Gold));
            foreach(var pulse in view.pulses)
            {
                Undo.RecordObject(pulse.GetComponent<MeshFilter>(),"Faceted energy packet");pulse.GetComponent<MeshFilter>().sharedMesh=art.sourceBody;
            }
            EditorUtility.SetDirty(art);EditorUtility.SetDirty(view);
        }
        private static Material Material(string name,Color color)
        {
            string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name=name,enableInstancing=true};
            material.SetColor("_BaseColor",color);material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,path);return material;
        }
        private static MeshRenderer[] Pool(Transform parent,string name,int count,Mesh mesh,Material material)
        {
            var pool=new MeshRenderer[count];
            for(int i=0;i<count;i++)
            {
                var existing=parent.Find(name+" "+i);GameObject item;
                if(existing==null){item=new GameObject(name+" "+i);Undo.RegisterCreatedObjectUndo(item,"Create native art pool");item.transform.SetParent(parent,false);Undo.AddComponent<MeshFilter>(item);Undo.AddComponent<MeshRenderer>(item);}
                else item=existing.gameObject;
                item.layer=2;Undo.RecordObject(item.GetComponent<MeshFilter>(),"Art pool mesh");item.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=item.GetComponent<MeshRenderer>();Undo.RecordObject(renderer,"Art pool material");renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;item.SetActive(false);pool[i]=renderer;
            }
            return pool;
        }
        private static Mesh Save(string name,Func<Mesh> create)
        {
            string path=Folder+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);var created=create();created.name=name;
            if(mesh==null){AssetDatabase.CreateAsset(created,path);return created;}
            Undo.RecordObject(mesh,"Refresh persistent instrument geometry");EditorUtility.CopySerialized(created,mesh);UnityEngine.Object.DestroyImmediate(created);EditorUtility.SetDirty(mesh);return mesh;
        }
        private static Texture2D FacetRamp()
        {
            string path=Folder+"/Facet light ramp.png";
            var texture=new Texture2D(64,1,TextureFormat.RGBA32,false){name="Facet light ramp",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            // Small value changes reveal facets while preserving readable role
            // colors against the passthrough environment, including unlit faces.
            var colors=new Color[64];for(int i=0;i<64;i++){float shade=Mathf.Lerp(.65f,1,i/63f);colors[i]=new Color(shade,shade,shade,1);}
            texture.SetPixels(colors);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Mesh Finish(List<Vector3> v,List<int> t)
        {
            // Bake a restrained studio light into UVs. The native unlit path
            // stays predictable without depending on Unity scene lights or bloom.
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();var normals=new List<Vector3>();
            var light=new Vector3(-.35f,.55f,-.75f).normalized;
            for(int i=0;i<t.Count;i+=3)
            {
                var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];var normal=Vector3.Cross(b-a,c-a).normalized;
                float value=.25f+.75f*Mathf.Max(0,Vector3.Dot(normal,light));
                foreach(var point in new[]{a,b,c}){indices.Add(vertices.Count);vertices.Add(point);normals.Add(normal);uv.Add(new Vector2(value,.5f));}
            }
            var m=new Mesh();m.SetVertices(vertices);m.SetNormals(normals);m.SetUVs(0,uv);m.SetTriangles(indices,0);m.RecalculateBounds();return m;
        }
        private static Mesh Bevel(int sides)
        {
            var v=new List<Vector3>();var t=new List<int>();float[] radii={.38f,.50f,.50f,.38f};float[] depths={-.30f,-.16f,.16f,.30f};
            for(int ring=0;ring<4;ring++)for(int i=0;i<sides;i++){float a=(i+.5f)*2*Mathf.PI/sides;v.Add(new Vector3(Mathf.Cos(a)*radii[ring],Mathf.Sin(a)*radii[ring],depths[ring]));}
            for(int ring=0;ring<3;ring++)for(int i=0;i<sides;i++){int a=ring*sides+i,b=ring*sides+(i+1)%sides;t.AddRange(new[]{a,b,a+sides,b,b+sides,a+sides});}
            for(int i=1;i<sides-1;i++)t.AddRange(new[]{0,i+1,i,3*sides,3*sides+i,3*sides+i+1});
            return Finish(v,t);
        }
        private static Mesh Crystal()
        {
            var v=new List<Vector3>{new Vector3(0,.6f,0),new Vector3(0,-.6f,0)};var t=new List<int>();
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;v.Add(new Vector3(Mathf.Cos(a)*.44f,0,Mathf.Sin(a)*.44f));}
            for(int i=0;i<6;i++){int a=2+i,b=2+(i+1)%6;t.AddRange(new[]{0,b,a,1,a,b});}return Finish(v,t);
        }
        private static Mesh Torus(int segments,float radius,float tube)
        {
            const int sides=6;var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
            {float a=i*2*Mathf.PI/segments,b=j*2*Mathf.PI/sides,r=radius+Mathf.Cos(b)*tube;v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*tube));}
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=(i+1)%segments*sides+j,d=(i+1)%segments*sides+(j+1)%sides;t.AddRange(new[]{a,c,b,b,c,d});}
            return Finish(v,t);
        }
        private static Mesh Gate()
        {
            var v=new List<Vector3>();var t=new List<int>();
            Box(v,t,new Vector3(-.34f,0,0),new Vector3(.18f,.9f,.32f));Box(v,t,new Vector3(.34f,0,0),new Vector3(.18f,.9f,.32f));
            Box(v,t,new Vector3(0,.35f,0),new Vector3(.56f,.20f,.32f));return Finish(v,t);
        }
        private static void Box(List<Vector3> v,List<int> t,Vector3 center,Vector3 size)
        {
            int k=v.Count;for(int i=0;i<8;i++)v.Add(center+Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),size));
            foreach(int i in new[]{0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5})t.Add(k+i);
        }
        private static Mesh Mark(string kind)
        {
            var v=new List<Vector3>();var t=new List<int>();
            void Line(float ax,float ay,float bx,float by,float width=.12f)
            {var a=new Vector3(ax,ay,0);var b=new Vector3(bx,by,0);var d=b-a;float angle=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;int k=v.Count;Box(v,t,(a+b)*.5f,new Vector3(d.magnitude,width,.06f));for(int i=k;i<v.Count;i++)v[i]=(a+b)*.5f+Quaternion.Euler(0,0,angle)*(v[i]-(a+b)*.5f);}
            if(kind=="turn"){Line(-.40f,-.30f,-.40f,.27f);Line(-.40f,.27f,.35f,.27f);Line(.14f,.47f,.36f,.27f);Line(.14f,.07f,.36f,.27f);}
            else if(kind=="split"){Line(0,-.42f,0,0);Line(0,0,-.38f,.37f);Line(0,0,.38f,.37f);}
            else if(kind=="reverse"){Line(-.4f,.22f,.4f,.22f);Line(-.4f,.22f,-.17f,.43f);Line(-.4f,.22f,-.17f,.01f);Line(.4f,-.22f,-.4f,-.22f);Line(.4f,-.22f,.17f,-.43f);Line(.4f,-.22f,.17f,-.01f);}
            else if(kind=="erase"){Line(-.36f,0,.36f,0,.16f);Line(-.36f,.25f,.36f,.25f,.08f);}
            else if(kind=="clock"){for(int i=0;i<16;i++){float a=i*2*Mathf.PI/16,b=(i+1)*2*Mathf.PI/16;Line(Mathf.Cos(a)*.4f,Mathf.Sin(a)*.4f,Mathf.Cos(b)*.4f,Mathf.Sin(b)*.4f,.07f);}Line(0,0,0,.29f,.09f);Line(0,0,.22f,-.10f,.09f);}
            else if(kind=="moon")
            {for(int i=0;i<14;i++){float a=(55+i*250f/14)*Mathf.Deg2Rad,b=(55+(i+1)*250f/14)*Mathf.Deg2Rad;Line(Mathf.Cos(a)*.35f,Mathf.Sin(a)*.40f,Mathf.Cos(b)*.35f,Mathf.Sin(b)*.40f,.15f);}}
            else if(kind=="goal"){Line(0,.42f,.32f,0);Line(.32f,0,0,-.42f);Line(0,-.42f,-.32f,0);Line(-.32f,0,0,.42f);}
            else if(kind=="star")
            {
                for(int i=0;i<10;i++)
                {
                    float a=(90+i*36)*Mathf.Deg2Rad,b=(90+(i+1)*36)*Mathf.Deg2Rad;
                    float ra=i%2==0?.43f:.185f,rb=(i+1)%2==0?.43f:.185f;
                    Line(Mathf.Cos(a)*ra,Mathf.Sin(a)*ra,Mathf.Cos(b)*rb,Mathf.Sin(b)*rb,.075f);
                }
            }
            else
            {int count=kind=="sun"?8:4;for(int i=0;i<count;i++){float a=i*2*Mathf.PI/count;Line(Mathf.Cos(a)*.13f,Mathf.Sin(a)*.13f,Mathf.Cos(a)*.43f,Mathf.Sin(a)*.43f,.11f);}Box(v,t,Vector3.zero,new Vector3(.20f,.20f,.06f));}
            return Finish(v,t);
        }
    }
}
