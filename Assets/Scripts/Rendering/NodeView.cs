using ByteDance.PICO.SpatialAdapter.Component;
using ByteDance.PICO.SpatialAdapter;
using OrbitalLoom.Core;
using OrbitalLoom.Interaction;
using UnityEngine;

namespace OrbitalLoom.Rendering
{
    public sealed class NodeView : MonoBehaviour
    {
        public MeshRenderer core, shell;
        public Transform ring, glyph;
        public Mesh[] mechanicGlyphs;
        public SpatialAdapterNativeText label;
        public LoomTarget target;
        public string Id { get; private set; }
        private Color color, current;
        private Vector3 baseScale;
        private bool selected, hinted, active, hovered, failed;
        private bool tutorialFocus;
        public bool GuidanceVisible=>tutorialFocus&&hinted;
        private float arrival;
        private string mechanic;
        private Mesh defaultGlyph;
        private bool glyphDirty, bodyDirty, shellDirty, ringDirty;
        private void Awake()
        { defaultGlyph = glyph.GetComponent<MeshFilter>().sharedMesh; target.Hovered += value => hovered = value; }

        public void Bind(string id, Vector3 position, string text, Color accent, string mechanicType, TargetKind kind)
        {
            Id = id; transform.localPosition = position; baseScale = Vector3.one;
            transform.localScale = baseScale; current = color = accent; mechanic = mechanicType;
            target.kind = kind; target.id = id;
            label.Text = text; label.TextSize = 36;
            tutorialFocus = selected = hinted = active = failed = false; arrival = float.PositiveInfinity;
            LoomPalette.SetColor(core, color); var glass = color; glass.a = .16f; LoomPalette.SetColor(shell, glass);
            glyph.localRotation = mechanic == "splitter" ? Quaternion.Euler(0, 0, 45) : Quaternion.identity;
            int glyphIndex = -1;
            glyph.GetComponent<MeshFilter>().sharedMesh = glyphIndex >= 0 && mechanicGlyphs != null && glyphIndex < mechanicGlyphs.Length
                ? mechanicGlyphs[glyphIndex] : defaultGlyph;
            glyphDirty = true;
            glyph.localRotation = Quaternion.identity;
            LoomPalette.SetColor(glyph.GetComponent<Renderer>(), accent);
        }
        public void SetEnergy(bool reachable, float time) { active = reachable; arrival = time; }
        public void SetAccent(Color value) { color = value; }
        public void SetSelected(bool value) { selected = value; }
        public void SetHint(bool value) { hinted = value; }
        public void SetTutorialFocus(bool enabled,bool current)
        {tutorialFocus=enabled;if(enabled){selected=false;hinted=current;}}
        public void SetFailure(bool value){failed=value;}
        public void SetSymbol(Mesh mesh){if(mesh==null)return;glyph.GetComponent<MeshFilter>().sharedMesh=mesh;glyphDirty=true;}
        public void SetBody(Mesh body,Mesh casing,Mesh orbit)
        {
            Assign(core.GetComponent<MeshFilter>(),body,ref bodyDirty);
            Assign(shell.GetComponent<MeshFilter>(),casing,ref shellDirty);
            Assign(ring.GetComponent<MeshFilter>(),orbit,ref ringDirty);
        }
        private static void Assign(MeshFilter filter,Mesh value,ref bool dirty)
        {if(value!=null&&filter.sharedMesh!=value){filter.sharedMesh=value;dirty=true;}}
        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null) return;
            bool powered = active && game.EnergyElapsed >= arrival;
            bool showHover=hovered&&!tutorialFocus;
            var wanted = failed ? LoomPalette.Error : selected || hinted || powered ? LoomPalette.SkinEnergy[game.Settings.skin] : showHover ? Color.Lerp(color,Color.white,.35f) : color;
            current = Color.Lerp(current, wanted, 1 - Mathf.Exp(-9 * Time.unscaledDeltaTime));
            LoomPalette.SetColor(core, current);
            float scale = selected ? 1.16f : hinted ? 1.07f + .04f * Mathf.Sin(Time.unscaledTime * 4) : showHover ? 1.08f : 1;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * scale, 1 - Mathf.Exp(-10 * Time.unscaledDeltaTime));
            // Enlarge the selected sphere, not its caption. Type keeps a fixed
            // angular size and does not jump into neighbouring rune labels.
            if(target.kind==TargetKind.BoxSocket||target.kind==TargetKind.BoxClue)
                label.transform.localScale=new Vector3(1/transform.localScale.x,1/transform.localScale.y,1/transform.localScale.z);
            if (ring != null && game.Settings.particles&&target.kind!=TargetKind.BoxSocket&&target.kind!=TargetKind.BoxClue)
            {
                float speed = mechanic == "gravity" ? -34 : mechanic == "repulsion" ? 26 : 12;
                ring.Rotate(Vector3.forward, speed * Time.unscaledDeltaTime, Space.Self);
            }
        }
        private void LateUpdate()
        {
            if (SpatialAdapterRuntime.Instance == null || SpatialAdapterRuntime.IsSessionInitialized() != 0) return;
            Flush(glyph.GetComponent<MeshFilter>(),ref glyphDirty);
            Flush(core.GetComponent<MeshFilter>(),ref bodyDirty);
            Flush(shell.GetComponent<MeshFilter>(),ref shellDirty);
            Flush(ring.GetComponent<MeshFilter>(),ref ringDirty);
        }
        private static void Flush(MeshFilter filter,ref bool dirty)
        {
            if(!dirty)return;
            var tracker = filter.GetComponent<SpatialAdapterGameObjectTracker>();
            if (tracker == null || !tracker.isValid || tracker.uid < 0) return;
            SpatialAdapterRuntime.Instance.SyncMesh(filter, VertexAttributeFlags.POSITION | VertexAttributeFlags.NORMAL | VertexAttributeFlags.UV0);
            dirty = false;
        }
    }
}
