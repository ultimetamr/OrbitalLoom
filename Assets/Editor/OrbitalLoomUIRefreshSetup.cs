using System;
using System.Linq;
using ByteDance.PICO.SpatialAdapter;
using ByteDance.PICO.SpatialAdapter.Component;
using OrbitalLoom.Rendering;
using OrbitalLoom.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrbitalLoom.Editor
{
    /// <summary>Persistent, exported geometry. Never create device UI objects at runtime.</summary>
    public static class OrbitalLoomUIRefreshSetup
    {
        private const string Folder="Assets/OrbitalLoomGenerated/UI/";
        public static void Apply()
        {
            var ui=UnityEngine.Object.FindObjectOfType<OrbitalLoomUI>();
            if(ui==null)throw new InvalidOperationException("UI scene is not open");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Midnight opaque.mat");
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Midnight opaque",enableInstancing=true};
                material.SetColor("_BaseColor",SpatialUITheme.Panel);material.SetFloat("_Cull",0);
                AssetDatabase.CreateAsset(material,Folder+"Midnight opaque.mat");
            }
            // Native automatic hover can brighten a surface independently of ink.
            // SpatialButton owns all visual states; SDK input events stay unchanged.
            var runtime=UnityEngine.Object.FindObjectOfType<SpatialAdapterRuntime>();
            if(runtime!=null){Undo.RecordObject(runtime,"Use coordinated UI states");runtime.autoAddUIHoverEffect=false;EditorUtility.SetDirty(runtime);}
            var mesh=Rounded();
            foreach(var button in ui.buttons)
            {
                Undo.RecordObject(button.background,"Opaque legible controls");button.background.sharedMaterial=material;
                Undo.RecordObject(button.background.GetComponent<MeshFilter>(),"Refine control corners");button.background.GetComponent<MeshFilter>().sharedMesh=mesh;
                if(button.outline!=null)
                {
                    Undo.RecordObject(button.outline,"Solid control borders");button.outline.sharedMaterial=material;
                    Undo.RecordObject(button.outline.GetComponent<MeshFilter>(),"Refine border corners");button.outline.GetComponent<MeshFilter>().sharedMesh=mesh;
                }
                foreach(var hover in button.GetComponentsInChildren<SpatialAdapterHoverEffect>(true))Undo.DestroyObjectImmediate(hover);
            }
            Undo.RecordObject(ui,"Create organized reading panels");
            ui.panels=Enumerable.Range(0,4).Select(i=>
            {
                var child=ui.buttons[0].transform.parent.Find("Reading panel "+i);
                if(child==null)
                {
                    var item=new GameObject("Reading panel "+i);Undo.RegisterCreatedObjectUndo(item,"Create native UI backing");
                    item.transform.SetParent(ui.buttons[0].transform.parent,false);child=item.transform;
                    Undo.AddComponent<MeshFilter>(item);Undo.AddComponent<MeshRenderer>(item);
                }
                Undo.RecordObject(child.GetComponent<MeshFilter>(),"Panel shape");child.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=child.GetComponent<MeshRenderer>();Undo.RecordObject(renderer,"Panel finish");renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                child.gameObject.layer=2;child.gameObject.SetActive(true);return renderer;
            }).ToArray();
            var view=UnityEngine.Object.FindObjectOfType<EnergyBoxView>();
            // The old cage added six unrelated outlines around already busy graphs.
            foreach(Transform child in view.assembly)
                if(child.name.StartsWith("Instrument frame")){Undo.RecordObject(child.gameObject,"Remove decorative graph cage");child.gameObject.SetActive(false);}
            Undo.RecordObject(ui.toast.transform,"Separate transient messages");ui.toast.transform.localPosition=new Vector3(0,.88f,-.57f);
            Undo.RecordObject(ui.toast,"Readable transient messages");ui.toast.BackgroundSize=new Vector2(1.55f,.11f);ui.toast.TextSize=38;
            EditorUtility.SetDirty(ui);PlayerSettings.bundleVersion="0.6.0";PlayerSettings.Android.bundleVersionCode=14;
        }
        private static Mesh Rounded()
        {
            string path=Folder+"Quiet rounded surface.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null)return mesh;
            var vertices=new System.Collections.Generic.List<Vector3>{Vector3.zero};
            const float radius=.035f;
            for(int corner=0;corner<4;corner++)
            {
                float angle=corner*90;var center=new Vector2(corner==0||corner==3?.5f-radius:-.5f+radius,corner<2?.5f-radius:-.5f+radius);
                for(int step=0;step<=6;step++)
                {float a=(angle+step*15)*Mathf.Deg2Rad;vertices.Add(new Vector3(center.x+Mathf.Cos(a)*radius,center.y+Mathf.Sin(a)*radius,0));}
            }
            var triangles=new System.Collections.Generic.List<int>();
            for(int i=1;i<vertices.Count;i++){triangles.Add(0);triangles.Add(i==vertices.Count-1?1:i+1);triangles.Add(i);}
            mesh=new Mesh{name="Quiet rounded surface"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }
}
