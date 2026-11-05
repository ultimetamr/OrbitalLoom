using System;
using ByteDance.PICO.SpatialAdapter;
using ByteDance.PICO.SpatialAdapter.Component;
using OrbitalLoom.Interaction;
using OrbitalLoom.Rendering;
using UnityEngine;

namespace OrbitalLoom.UI
{
    public enum LoomIcon { None = -1, Undo, Reset, Hint, Orbit, Book, Focus, Settings, Trash, Back, Play, Next, Check }
    public sealed class SpatialButton : MonoBehaviour
    {
        public LoomTarget target;
        public MeshRenderer background;
        public SpatialAdapterNativeText label;
        public Transform surface;
        public MeshRenderer outline;
        public MeshFilter icon;
        public Mesh[] iconMeshes;
        public MeshRenderer toggleRail, toggleThumb;
        private Action action;
        private bool enabledAction;
        private Color ink;
        private bool iconDirty, hovered, primaryAction, selected;
        private string tooltip;
        public string Caption => tooltip;
        private float pressedUntil;
        private bool guided;
        private bool tutorialFocus;
        public bool GuidanceVisible=>guided&&enabledAction;
        private bool visualSet;
        private Color lastFill,lastInk,lastEdge;
        private float lastBorder;
        public void SetGuidance(bool value){guided=value;RefreshVisual();}
        /// <summary>During teaching, selected/hover/primary styles cannot
        /// compete with the one current action. Normal styles return on exit.</summary>
        public void SetTutorialFocus(bool active,bool current)
        {tutorialFocus=active;guided=active&&current;RefreshVisual();}
        public void SetSelected(bool value){selected=value;RefreshVisual();}
        private void Awake()
        {
            target.Clicked += () =>
            {
                if (!enabledAction) return;
                pressedUntil = Time.unscaledTime + .14f;RefreshVisual();
                OrbitalLoom.Audio.LoomAudio.Instance?.Play("select", transform.localPosition);
                action?.Invoke();
            };
            target.Hovered += active =>
            {
                hovered = active && enabledAction;
                RefreshVisual();
            };
        }
        public void Bind(string text, Vector3 position, Vector2 size, Action onClick, bool primary = false, bool enabled = true,
            LoomIcon symbol = LoomIcon.None)
        {
            transform.localPosition = position;
            tutorialFocus=guided=selected=hovered=false;pressedUntil=0;visualSet=false;
            if (toggleRail != null) toggleRail.gameObject.SetActive(false);
            if (toggleThumb != null) toggleThumb.gameObject.SetActive(false);
            if (outline != null) outline.gameObject.SetActive(true);
            surface.localScale = new Vector3(size.x, size.y, .009f);
            if (outline != null) outline.transform.localScale = new Vector3(size.x + .003f, size.y + .003f, .009f);
            bool hasIcon = symbol != LoomIcon.None && icon != null && iconMeshes != null && (int)symbol < iconMeshes.Length;
            label.transform.localPosition = new Vector3(0, hasIcon ? -size.y * .29f : 0, -.014f);
            label.BackgroundSize = new Vector2(size.x-.014f, hasIcon ? .038f : size.y-.014f);
            label.BackgroundCornerRadius=12;
            label.Text = text; label.TextSize = hasIcon ? 31 : primary ? 51 : 44;
            label.Justification = TextJustification.Center;
            ink = SpatialUITheme.Foreground(enabled);
            label.TextColor = ink;
            action = onClick; enabledAction = enabled; primaryAction = primary;
            tooltip = text; hovered = false;
            LoomPalette.SetColor(background, SpatialUITheme.Fill(enabled,primary,false,false,false));
            if (icon != null)
            {
                icon.gameObject.SetActive(hasIcon);
                if (hasIcon)
                {
                    icon.sharedMesh = iconMeshes[(int)symbol]; iconDirty = true;
                    icon.transform.localPosition = new Vector3(0, size.y * .12f, -.014f);
                    float diameter = Mathf.Min(size.x, size.y) * .43f;
                    icon.transform.localScale = new Vector3(diameter, diameter, 1);
                    LoomPalette.SetColor(icon.GetComponent<Renderer>(), ink);
                }
            }
            RefreshVisual();
        }
        public void SetAvailable(bool available)
        {
            if (available == enabledAction) return;
            enabledAction = available;
            if(!available){hovered=false;pressedUntil=0;}
            RefreshVisual();
        }
        public void SetToggle(bool value)
        {
            if (toggleRail == null || toggleThumb == null) return;
            selected=value;
            float right = surface.localScale.x * .5f - .065f;
            toggleRail.gameObject.SetActive(true); toggleThumb.gameObject.SetActive(true);
            toggleRail.transform.localPosition = new Vector3(right, 0, -.013f);
            toggleRail.transform.localScale = new Vector3(.078f, .032f, .006f);
            toggleThumb.transform.localPosition = new Vector3(right + (value ? .021f : -.021f), 0, -.02f);
            toggleThumb.transform.localScale = Vector3.one * .025f;
            LoomPalette.SetColor(toggleRail, value ? LoomPalette.Repulsion * .65f : LoomPalette.Hex("465257"));
            LoomPalette.SetColor(toggleThumb, LoomPalette.Text);
            label.transform.localPosition = new Vector3(-.055f, 0, -.014f);
            label.BackgroundSize = new Vector2(surface.localScale.x - .15f, surface.localScale.y - .018f);
            outline.gameObject.SetActive(true);RefreshVisual();
        }
        private void Update(){RefreshVisual();}
        private void RefreshVisual()
        {
            if(label==null||background==null)return;
            ink=SpatialUITheme.Foreground(enabledAction);
            bool emphasize=guided||!tutorialFocus&&(selected||primaryAction);
            bool showHover=hovered&&!tutorialFocus;
            var wanted=SpatialUITheme.Fill(enabledAction,false,emphasize,showHover,!tutorialFocus&&Time.unscaledTime<pressedUntil);
            var edge=enabledAction&&emphasize?SpatialUITheme.ButtonAccent:
                enabledAction&&showHover?SpatialUITheme.Secondary:SpatialUITheme.Edge;
            float border=enabledAction&&emphasize ? .014f : .008f;
            if(visualSet&&wanted==lastFill&&ink==lastInk&&edge==lastEdge&&border==lastBorder)return;
            visualSet=true;lastFill=wanted;lastInk=ink;lastEdge=edge;lastBorder=border;
            // Both values live in the SDK NativeTextData payload. No dark ink,
            // fading text, exposure flash, or old press state survives reuse.
            label.TextColor=ink;label.BackgroundColor=wanted;
            LoomPalette.SetColor(background,wanted);
            if(icon!=null&&icon.gameObject.activeSelf)LoomPalette.SetColor(icon.GetComponent<Renderer>(),ink);
            if(outline!=null)
            {
                outline.transform.localScale=new Vector3(surface.localScale.x+border,surface.localScale.y+border,.009f);
                LoomPalette.SetColor(outline,edge);
            }
        }
        private void LateUpdate()
        {
            if (!iconDirty || SpatialAdapterRuntime.Instance == null || SpatialAdapterRuntime.IsSessionInitialized() != 0) return;
            var tracker = icon.GetComponent<SpatialAdapterGameObjectTracker>();
            if (tracker == null || !tracker.isValid || tracker.uid < 0) return;
            SpatialAdapterRuntime.Instance.SyncMesh(icon, VertexAttributeFlags.POSITION | VertexAttributeFlags.NORMAL | VertexAttributeFlags.UV0);
            iconDirty = false;
        }
    }

}
