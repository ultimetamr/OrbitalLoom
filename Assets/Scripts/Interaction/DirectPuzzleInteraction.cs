using System;
using OrbitalLoom.Core;
using OrbitalLoom.Gameplay;
using OrbitalLoom.Rendering;
using UnityEngine;

namespace OrbitalLoom.Interaction
{
    /// <summary>Uses official SpatialInputState positions and device rotations for both
    /// pinch and controller grabs. Exactly one release commits each manipulation.</summary>
    public sealed class DirectPuzzleInteraction : MonoBehaviour
    {
        public EnergyBoxView view;
        public Transform inventoryRoot, turnCrystal, splitCrystal, viewGrip, snapMarker;
        public Transform[] spatialHandles;
        public Transform relicGrip;
        public float snapRadius = .13f;
        public bool Grabbing => captured != null;
        public int Pointer => pointer;
        private LoomTarget captured;
        private EnergyBoxSession session;
        private int pointer, socket, original, preview, dropSocket = -1;
        private Vector3 startPoint, initialPosition, positionOffset, rotationAxis, lastRadial;
        private Quaternion initialRotation, lastDeviceRotation;
        private float pointerDegrees, deviceDegrees, initialYaw, lastFeedback;

        public bool Handles(LoomTarget target) => target != null && (target.kind == TargetKind.BoxPiece ||
            target.kind == TargetKind.BoxSocket || target.kind == TargetKind.BoxViewGrip ||
            target.kind == TargetKind.BoxSpatialHandle || target.kind == TargetKind.RelicGrip);

        public bool Begin(int id, LoomTarget target, Vector3 point, Quaternion rotation)
        {
            var game = GameManager.Instance;
            if (Grabbing || game == null || game.Paused || !Handles(target) || !DirectPuzzleActions.Finite(point) || !ValidRotation(rotation)) return false;
            session = game.BoxSession;
            if (session == null || game.State != GameState.LevelPlaying) return false;
            bool exhibit = target.kind == TargetKind.RelicGrip || target.kind == TargetKind.BoxSpatialHandle;
            if (game.UiOverlayOpen != exhibit || (!exhibit && !DirectPuzzleActions.Editable(session))) return false;
            if (target.kind == TargetKind.BoxSocket)
            {
                string focus = session.Tutorial.Target;
                if (focus != null && target.id != focus) return false;
                session.Select(target.controlIndex);
                if (session.Installed(target.controlIndex) == null) return true;
            }
            if (target.kind == TargetKind.BoxSpatialHandle)
            {
                if (session.Running || session.Solved) return false;
                session.SelectSpatial(target.controlIndex);
            }
            captured = target; pointer = id; startPoint = point; initialPosition = target.transform.position;
            initialRotation = target.transform.rotation; lastDeviceRotation = rotation;
            positionOffset = Quaternion.Inverse(rotation) * (initialPosition - point);
            initialYaw = target.kind == TargetKind.RelicGrip ? session.Observation.Angle : view.Yaw;
            socket = target.kind == TargetKind.BoxSpatialHandle ? Array.FindIndex(session.Sockets,
                n => n.id == session.Level.spatial.controls[target.controlIndex]) : target.controlIndex;
            original = socket >= 0 && socket < session.Sockets.Length ? session.Choice(socket) : 0;
            preview = original; dropSocket = -1; pointerDegrees = deviceDegrees = 0;
            if (target.kind == TargetKind.RelicGrip) preview = Mathf.RoundToInt(initialYaw / 15);
            rotationAxis = target.kind == TargetKind.BoxSocket ? target.transform.forward :
                target.kind == TargetKind.BoxSpatialHandle ? view.relic.mechanism.rotor.up : view.transform.up;
            lastRadial = Vector3.ProjectOnPlane(point - initialPosition, rotationAxis);
            target.Hover(true); game.Activity(); Feedback("select", initialPosition);
            Debug.Log("[Orbital Loom] Direct grab: " + target.kind + " / " + target.id);
            return true;
        }

        public void Move(int id, Vector3 point, Quaternion rotation)
        {
            if (!Grabbing || id != pointer) return;
            if (!DirectPuzzleActions.Finite(point) || !ValidRotation(rotation)) { Cancel(); return; }
            if (captured.kind == TargetKind.BoxPiece)
            {
                var position = point + rotation * positionOffset;
                captured.transform.SetPositionAndRotation(position, rotation * Quaternion.Inverse(lastDeviceRotation) * captured.transform.rotation);
                lastDeviceRotation = rotation;
                dropSocket = DirectPuzzleActions.NearestSocket(session, view.assembly.InverseTransformPoint(position), snapRadius);
                snapMarker.gameObject.SetActive(dropSocket >= 0);
                if (dropSocket >= 0)
                {
                    snapMarker.position = view.assembly.TransformPoint(session.Sockets[dropSocket].Position);
                    snapMarker.rotation = view.assembly.rotation;
                }
                return;
            }
            Vector3 radial = Vector3.ProjectOnPlane(point - initialPosition, rotationAxis);
            if (lastRadial.magnitude > .025f && radial.magnitude > .025f)
                pointerDegrees += Vector3.SignedAngle(lastRadial, radial, rotationAxis);
            lastRadial = radial;
            Quaternion delta = rotation * Quaternion.Inverse(lastDeviceRotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180) angle -= 360;
            if (DirectPuzzleActions.Finite(axis)) deviceDegrees += angle * Vector3.Dot(axis, rotationAxis);
            lastDeviceRotation = rotation;
            float degrees = Mathf.Abs(pointerDegrees) > Mathf.Abs(deviceDegrees) ? pointerDegrees : deviceDegrees;
            if (captured.kind == TargetKind.BoxViewGrip || captured.kind == TargetKind.RelicGrip)
            {
                float drag = Vector3.Dot(point - startPoint, view.transform.right) * 160;
                float yaw = initialYaw + (Mathf.Abs(drag) > Mathf.Abs(degrees) ? drag : degrees);
                if (captured.kind == TargetKind.BoxViewGrip) view.SetYaw(yaw);
                else { view.relic.rotor.localRotation = Quaternion.Euler(0, yaw, 0); preview = Mathf.RoundToInt(yaw / 15); }
                return;
            }
            bool spatial = captured.kind == TargetKind.BoxSpatialHandle;
            bool path = spatial && session.Level.spatial.kind == "path";
            int steps = path ? Mathf.RoundToInt(Vector3.Dot(point - startPoint, view.relic.mechanism.rotor.forward) / .09f) : Mathf.RoundToInt(degrees / 30);
            int basis = original == 0 ? 1 : original;
            int next = DirectPuzzleActions.RotateChoice(session, socket, basis, steps);
            if (next != preview)
            {
                preview = next;
                if (Time.unscaledTime - lastFeedback > .09f) { lastFeedback = Time.unscaledTime; Feedback("select", initialPosition); }
            }
            // The collider stays in place; only the saved visual handle rotates.
            var visual = captured.transform.Find("Grip visual");
            if (visual != null) visual.localRotation = Quaternion.AngleAxis(steps * 30, spatial ? Vector3.up : Vector3.forward);
            if (spatial) view.relic.mechanism.Preview(socket, preview);
        }

        public void End(int id, bool cancel, Vector3 point, Quaternion rotation)
        {
            if (!Grabbing || id != pointer) return;
            if (!cancel) Move(id, point, rotation);
            if (!Grabbing) return;
            var target = captured; var current = session; int index = socket, choice = preview, destination = dropSocket;
            bool piece = target.kind == TargetKind.BoxPiece;
            var kind = target.id;
            // Release capture before publishing: a session update rebuilds the UI.
            Restore(cancel); captured = null; snapMarker.gameObject.SetActive(false);
            bool changed = false;
            if (!cancel && current == GameManager.Instance?.BoxSession)
            {
                if (piece && destination >= 0) changed = DirectPuzzleActions.Install(current, destination, kind);
                else if (target.kind == TargetKind.BoxSocket && choice != original) changed = DirectPuzzleActions.Rotate(current, index, choice);
                else if (target.kind == TargetKind.BoxSpatialHandle) changed = DirectPuzzleActions.Editable(current) && current.Set(index, choice);
                else if (target.kind == TargetKind.RelicGrip) current.SetRelicAngle(choice * 15);
            }
            view.Refresh();
            if (target.kind == TargetKind.BoxSpatialHandle || target.kind == TargetKind.RelicGrip) view.relic.Bind(current, true, false);
            if (!cancel) Feedback(changed ? "connect" : piece ? "error" : "select", initialPosition);
            Refresh();
        }

        public void Cancel()
        {
            if (!Grabbing) return;
            bool exhibit=captured.kind==TargetKind.BoxSpatialHandle||captured.kind==TargetKind.RelicGrip;
            Restore(); captured = null; if (snapMarker != null) snapMarker.gameObject.SetActive(false);
            if(exhibit&&session==GameManager.Instance?.BoxSession)view.relic.Bind(session,true,false);
        }
        private void Restore(bool cancel = true)
        {
            captured.Hover(false);
            if (captured.kind == TargetKind.BoxPiece) captured.transform.SetPositionAndRotation(initialPosition, initialRotation);
            var visual = captured.transform.Find("Grip visual"); if (visual != null) visual.localRotation = Quaternion.identity;
            if (captured.kind == TargetKind.BoxViewGrip && cancel) view.SetYaw(initialYaw);
        }
        private void Update()
        {
            var game = GameManager.Instance;
            if (Grabbing && (game == null || game.Paused || session != game.BoxSession || !captured.gameObject.activeInHierarchy)) Cancel();
            Refresh();
        }
        public void Refresh()
        {
            var game = GameManager.Instance; var current = game?.BoxSession;
            bool workspace = game != null && game.State == GameState.LevelPlaying && !game.UiOverlayOpen && current != null;
            if (inventoryRoot != null) inventoryRoot.gameObject.SetActive(workspace);
            if (viewGrip != null) viewGrip.gameObject.SetActive(workspace);
            if (workspace)
            {
                turnCrystal.gameObject.SetActive(current.Level.turns > 0 && !current.Running && !current.Solved);
                splitCrystal.gameObject.SetActive(current.Level.splitters > 0 && !current.Running && !current.Solved);
                foreach (var crystal in new[] { turnCrystal, splitCrystal })
                {
                    var ring = crystal.Find("Focus ring");
                    if (ring != null) ring.gameObject.SetActive(!Grabbing && current.Tutorial.Active &&
                        (current.Tutorial.Stage == 2 || current.Tutorial.Stage == 13) && current.Tutorial.Allows(crystal.GetComponent<LoomTarget>().id));
                }
            }
            bool exhibit = view?.relic != null && view.relic.Displaying && game.UiOverlayOpen;
            if (relicGrip != null) relicGrip.gameObject.SetActive(exhibit && current.Level.spatial == null);
            if (spatialHandles != null) for (int i = 0; i < spatialHandles.Length; i++)
            {
                bool show = exhibit && current.Level.spatial != null && i < current.Level.spatial.controls.Length;
                spatialHandles[i].gameObject.SetActive(show);
                if (!show || Grabbing && captured.transform == spatialHandles[i]) continue;
                var rig = current.Level.spatial;
                var at = rig.kind == "bridge" ? SpatialMechanism.Pivot(i) : rig.kind == "mirror" ? SpatialMechanism.MirrorCenter : SpatialMechanism.Path(i, 0)[0];
                spatialHandles[i].localPosition = at;
            }
        }
        private void OnDisable() { Cancel(); }
        private static bool ValidRotation(Quaternion value) => Data.Point3.IsFinite(value.x) && Data.Point3.IsFinite(value.y) && Data.Point3.IsFinite(value.z) && Data.Point3.IsFinite(value.w);
        private static void Feedback(string sound, Vector3 position) => OrbitalLoom.Audio.LoomAudio.Instance?.Play(sound, position);
    }
}
