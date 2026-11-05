using System.Collections.Generic;
using ByteDance.PICO.SpatialAdapter;
using OrbitalLoom.Core;
using OrbitalLoom.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR;
using Phase=UnityEngine.InputSystem.TouchPhase;
namespace OrbitalLoom.Interaction
{
    public sealed class SpatialInputRouter:MonoBehaviour
    {
        public Camera editorCamera;
        public EnergyBoxView boxView;
        private GameManager game;
        private LoomTarget hovered;
        private InputAction primarySpatial,secondarySpatial;
        private readonly HashSet<int> pressedTaps=new HashSet<int>();
        private readonly Dictionary<int,LoomTarget> captures=new Dictionary<int,LoomTarget>();
        private bool aHeld;
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            // Consume each official SpatialInputDevice event while its phase
            // and position are still together. Polling GetInputState in Update
            // can see only the final snapshot of a short trigger press.
            primarySpatial = new InputAction("Loom primary", InputActionType.PassThrough, "<SpatialInputDevice>/primary");
            secondarySpatial = new InputAction("Loom secondary", InputActionType.PassThrough, "<SpatialInputDevice>/secondary");
            primarySpatial.performed += OnSpatialInput; secondarySpatial.performed += OnSpatialInput;
            primarySpatial.Enable(); secondarySpatial.Enable();
        }
        private void OnDisable()
        {
            primarySpatial?.Dispose(); secondarySpatial?.Dispose();
            primarySpatial = secondarySpatial = null;
            CancelAll(); pressedTaps.Clear(); EnhancedTouchSupport.Disable();
        }
        private void Start() { game = GameManager.Instance; game.Changed += CancelAll; }
        private void OnDestroy() { if (game != null) game.Changed -= CancelAll; }
        private void OnSpatialInput(InputAction.CallbackContext context)
        {
            if (game == null || game.Paused) return;
            var state = context.ReadValue<SpatialInputState>();
            if (state.interactionId < 0 || state.interactionId > 1 || state.phase == Phase.None) return;
            if (!Finite(state.currentPosition)) { Release(state.interactionId, true, Vector3.zero); pressedTaps.Remove(state.interactionId); return; }
            var target = state.targetObject == null ? null : state.targetObject.GetComponentInParent<LoomTarget>();
            if (hovered != target) { hovered?.Hover(false); hovered = target; hovered?.Hover(true); }
            Dispatch(state.interactionId, state.phase, target, state.currentPosition);
        }
        private void Update()
        {
            if (game == null || game.Paused) return;
            ReadControllerButtons();
#if UNITY_EDITOR
            EditorMouse();
#endif
        }

        private void ReadControllerButtons()
        {
            var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool a=right.isValid&&right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out bool down)&&down;
            if(a&&!aHeld&&!game.UiOverlayOpen&&game.State==GameState.LevelPlaying){CancelAll();game.Undo();}aHeld=a;
        }
        private void Dispatch(int id,Phase phase,LoomTarget target,Vector3 point)
        {
            if(phase==Phase.Began)
            {
                if(pressedTaps.Contains(id)||captures.ContainsKey(id)||target==null||!target.isActiveAndEnabled)return;
                if(game.UiOverlayOpen&&target.kind!=TargetKind.Button&&target.kind!=TargetKind.Slider)return;
                if(target.kind==TargetKind.Slider){captures[id]=target;target.Drag(point);return;}
                if(target.kind!=TargetKind.Button&&game.State!=GameState.LevelPlaying)return;
                if(target.kind==TargetKind.BoxSocket&&(game.BoxSession==null||game.BoxSession.Running||game.BoxSession.Solved))return;
                pressedTaps.Add(id);game.Activity();
                Debug.Log($"[Orbital Loom] Input press: kind={target.kind}, target={target.name}, id={target.id}");
                if(target.kind==TargetKind.Button){CancelAll();target.Click();}
                else if(target.kind==TargetKind.BoxSocket)boxView.Select(target.controlIndex);
                else if(target.kind==TargetKind.BoxClue)game.BoxSession?.Examine(target.id);
            }
            else if(phase==Phase.Moved&&captures.TryGetValue(id,out var slider))slider?.Drag(point);
            else if(phase==Phase.Ended||phase==Phase.Canceled){if(phase==Phase.Ended&&captures.TryGetValue(id,out var last))last?.Drag(point);pressedTaps.Remove(id);captures.Remove(id);}
        }
        private void Release(int id,bool cancel,Vector3 point){captures.Remove(id);pressedTaps.Remove(id);}
        public void CancelAll(){captures.Clear();hovered?.Hover(false);hovered=null;}
        private static bool Finite(Vector3 p)=>Data.Point3.IsFinite(p.x)&&Data.Point3.IsFinite(p.y)&&Data.Point3.IsFinite(p.z);
#if UNITY_EDITOR
        private void EditorMouse()
        {
            if(editorCamera==null||Mouse.current==null)return;
            var ray=editorCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            bool hit=Physics.Raycast(ray,out var found,20,~(1<<2));var target=hit?found.collider.GetComponentInParent<LoomTarget>():null;
            Vector3 point=hit?found.point:ray.GetPoint(2);
            if(captures.Count>0){var plane=new Plane(editorCamera.transform.forward,boxView.transform.position);if(plane.Raycast(ray,out float distance))point=ray.GetPoint(distance);}
            if(hovered!=target){hovered?.Hover(false);hovered=target;hovered?.Hover(true);}
            if(Mouse.current.leftButton.wasPressedThisFrame)Dispatch(-100,Phase.Began,target,point);
            else if(Mouse.current.leftButton.wasReleasedThisFrame)Dispatch(-100,Phase.Ended,target,point);
            else if(Mouse.current.leftButton.isPressed)Dispatch(-100,Phase.Moved,target,point);
        }
#endif
    }
}
