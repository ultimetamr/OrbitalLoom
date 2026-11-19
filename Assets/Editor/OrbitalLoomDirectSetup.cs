using System;
using System.IO;
using System.Linq;
using System.Linq;
using ByteDance.PICO.SpatialAdapter;
using OrbitalLoom.Interaction;
using OrbitalLoom.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrbitalLoom.Editor
{
    /// <summary>All grab targets, colliders and decorative pools are authored before
    /// export. Device code only changes saved transforms and material colors.</summary>
    public static class OrbitalLoomDirectSetup
    {
        private const string Folder = "Assets/OrbitalLoomGenerated/Direct/";
        public static void Apply()
        {
            Directory.CreateDirectory("Builds");
            EditorSceneManager.OpenScene(OrbitalLoomProjectTools.ScenePath);
            var view = UnityEngine.Object.FindObjectOfType<EnergyBoxView>(true);
            var input = UnityEngine.Object.FindObjectOfType<SpatialInputRouter>(true);
            if (view == null || input == null) throw new InvalidOperationException("Missing gameplay/input scene");
            if (!AssetDatabase.IsValidFolder(Folder.TrimEnd('/'))) AssetDatabase.CreateFolder("Assets/OrbitalLoomGenerated", "Direct");
            var direct = GetOrAdd<DirectPuzzleInteraction>(view.gameObject);
            direct.view = view; input.direct = direct;
            if (view.stand != null) { UnityEngine.Object.DestroyImmediate(view.stand.gameObject); view.stand = null; }
            foreach(var child in view.assembly.Cast<Transform>().Where(t=>t.name.StartsWith("Instrument frame ")).ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            Material aqua = Mat("Crystal cyan", "83E1E0"), violet = Mat("Prism violet", "B6A1EB"), gold = Mat("Engraved gold", "E8C88D");
            Material stone = Mat("Carved blue stone", "26374E"), ivory = Mat("Moonstone ceramic", "BAC9D2");
            direct.inventoryRoot = Root(view.transform, "Crystal inventory");
            direct.inventoryRoot.localPosition = new Vector3(-1.28f, -.08f, -.08f);
            direct.turnCrystal = Piece(direct.inventoryRoot, "Turn crystal", "turn", 0, view.art, aqua, gold, stone);
            direct.splitCrystal = Piece(direct.inventoryRoot, "Split prism", "split", 0, view.art, violet, gold, stone);
            direct.turnCrystal.localPosition = new Vector3(0,.16f,0);
            direct.splitCrystal.localPosition = new Vector3(0,-.16f,0);
            direct.inventoryRoot.Find("Turn crystal pedestal").localPosition=new Vector3(0,.06f,.015f);
            direct.inventoryRoot.Find("Split prism pedestal").localPosition=new Vector3(0,-.26f,.015f);
            direct.viewGrip = Target(view.transform, "Inspection grip", TargetKind.BoxViewGrip, "view", 0, new Vector3(.82f, .05f, 0), .065f);
            Visual(direct.viewGrip, "Grip visual", view.art.orbit, gold, Vector3.one * .066f);
            direct.snapMarker = Visual(view.assembly, "Magnetic docking ring", view.art.orbit, gold, Vector3.one * .082f);
            direct.snapMarker.gameObject.SetActive(false);
            direct.relicGrip = Target(view.relic.rotor, "Relic grab handle", TargetKind.RelicGrip, "relic", 0, new Vector3(0, -.34f, 0), .085f);
            Visual(direct.relicGrip, "Grip visual", view.art.orbit, gold, Vector3.one * .080f);
            direct.spatialHandles = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                direct.spatialHandles[i] = Target(view.relic.mechanism.rotor, "Mechanism grip " + i, TargetKind.BoxSpatialHandle, "spatial", i, Vector3.zero, .075f);
                Visual(direct.spatialHandles[i], "Grip visual", view.art.orbit, gold, Vector3.one * .07f).localRotation = Quaternion.Euler(90, 0, 0);
                direct.spatialHandles[i].gameObject.SetActive(false);
            }
            foreach (var node in view.nodes)
            {
                // A layered mounting gives the mechanisms recognizable silhouettes.
                Visual(node.transform, "Outer engraved collar", view.art.orbit, gold, Vector3.one * .079f).localPosition = new Vector3(0, 0, .014f);
                Visual(node.transform, "Moonstone bearing", view.art.casing, ivory, new Vector3(.10f, .10f, .035f)).localPosition = new Vector3(0, 0, .036f);
                var handle = Visual(node.transform, "Grip visual", view.art.orbit, aqua, Vector3.one * .073f);
                handle.localPosition = new Vector3(0, 0, -.025f);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3;
                    var notch = Visual(handle, "Index inlay " + i, view.art.sourceBody, gold, new Vector3(.05f, .10f, .05f));
                    notch.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                    notch.localRotation = Quaternion.Euler(0, 0, i * 60 - 90);
                }
            }
            var config = ByteDance.PICO.XR.PXR_ProjectSetting.GetProjectConfig();
            config.isSpatialAdapter = true; config.portalInited = true;
            ByteDance.PICO.XR.PXR_ProjectSetting.SaveAssets();
            PlayerSettings.bundleVersion = "1.0.0"; PlayerSettings.Android.bundleVersionCode = 28;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.SplashScreen.show = false;
            // Explicit Android legacy, round and adaptive slots avoid Unity defaults.
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Brand/OrbitalLoomIcon.png");
            foreach (IconKind kind in new[] { IconKind.Any }) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { icon }, kind);
            EditorUtility.SetDirty(input); EditorUtility.SetDirty(direct); EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(view.gameObject.scene);
            OrbitalLoomProjectTools.Validate();
            Debug.Log("[Orbital Loom] DIRECT_SETUP PASS; version=1.0.0/code28");
        }
        private static Transform Piece(Transform parent, string name, string kind, float x, EnergyBoxArt art, Material color, Material gold, Material stone)
        {
            var piece = Target(parent, name, TargetKind.BoxPiece, kind, -1, new Vector3(x, 0, 0), .075f);
            Visual(piece, "Faceted crystal", art.sourceBody, color, new Vector3(.072f, .12f, .072f));
            Visual(piece, "Mechanism engraving", kind == "turn" ? art.turnMark : art.splitMark, gold, Vector3.one * .045f).localPosition = new Vector3(0, 0, -.046f);
            Visual(piece, "Focus ring", art.orbit, gold, Vector3.one * .082f).gameObject.SetActive(false);
            Visual(parent, name + " pedestal", art.casing, stone, new Vector3(.15f, .055f, .10f)).localPosition = new Vector3(x, -.10f, .015f);
            return piece;
        }
        private static Transform Target(Transform parent, string name, TargetKind kind, string id, int index, Vector3 position, float radius)
        {
            var root = Root(parent, name); root.localPosition = position; root.gameObject.layer = 0;
            var target = GetOrAdd<LoomTarget>(root.gameObject);
            target.kind = kind; target.id = id; target.controlIndex = index;
            var collider = GetOrAdd<SphereCollider>(root.gameObject); collider.radius = radius;
            return root;
        }
        private static Transform Root(Transform parent, string name)
        { var t = parent.Find(name); if (t != null) return t; var item = new GameObject(name); item.transform.SetParent(parent, false); return item.transform; }
        private static Transform Visual(Transform parent, string name, Mesh mesh, Material material, Vector3 scale)
        {
            var t = Root(parent, name); t.gameObject.layer = 2; t.localScale = scale;
            GetOrAdd<MeshFilter>(t.gameObject).sharedMesh = mesh;
            var renderer = GetOrAdd<MeshRenderer>(t.gameObject); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return t;
        }
        private static Material Mat(string name, string hex)
        {
            string path = Folder + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name, enableInstancing = true }; AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", LoomPalette.Hex(hex)); material.SetFloat("_Cull", 0); EditorUtility.SetDirty(material); return material;
        }
        private static T GetOrAdd<T>(GameObject item) where T:Component
        {var component=item.GetComponent<T>();if(component==null)component=item.AddComponent<T>();return component;}
    }
}
