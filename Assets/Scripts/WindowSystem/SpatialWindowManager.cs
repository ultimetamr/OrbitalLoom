using System;
using ByteDance.PICO.SpatialAdapter;
using OrbitalLoom.Core;
using OrbitalLoom.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace OrbitalLoom.WindowSystem
{
    public sealed class SpatialWindowManager : MonoBehaviour
    {
        public SpatialCamera spatialCamera;
        [FormerlySerializedAs("previewConfiguration")]
        public SpatialCameraConfiguration fullSpaceConfiguration;
        public Transform presentationRoot, puzzleRoot;
        public bool IsFullSpace => spatialCamera.OutputConfiguration != null &&
            spatialCamera.OutputConfiguration.mode == SpatialCamera.CameraMode.Unconstrained;
        private GameManager game;
        private float resetSeconds, stableSeconds, poseWaitSeconds;
        private bool initialPosePending = true, recenterPending;
        private bool fallbackReported;
        private Pose previousHead;
        private const float ViewingDistance = 2.4f;
        private const float StablePoseSeconds = .35f;
        private static readonly Vector3 SafeStart = new Vector3(0, 1.4f, ViewingDistance);

        private void Awake()
        {
            // Stage origin is the floor. Keep a safe startup height while the
            // native runtime is still returning its default (zero) head pose.
            presentationRoot.SetPositionAndRotation(SafeStart, Quaternion.identity);
            // The saved camera and project default must already use Stage Space:
            // OpenWindowOnLoad runs in SpatialCamera.Awake, before our Start.
            if (!IsFullSpace)
            {
                spatialCamera.CloseWindow();
                spatialCamera.OutputConfiguration = fullSpaceConfiguration;
                spatialCamera.OpenWindow();
            }
        }

        private void Start()
        {
            game = GameManager.Instance;
            game.SettingsChanged += ApplyPreferences;
            ApplyPreferences();
            // SDK-generated SpatialAdapterRuntime owns initialization/resource export.
            // Never initialize it a second time or instantiate another runtime here.
        }
        private void OnDestroy() { if (game != null) game.SettingsChanged -= ApplyPreferences; }
        public void Recenter() { recenterPending = true; stableSeconds = 0; }
        private void OnApplicationPause(bool paused) { if (!paused) RequestTrackingPlacement(); }
        private void OnApplicationFocus(bool focused) { if (focused) RequestTrackingPlacement(); }
        private void RequestTrackingPlacement()
        {
            initialPosePending = true; stableSeconds = poseWaitSeconds = 0;
            fallbackReported = false;
        }
        public void ResetContent()
        {
            puzzleRoot.localPosition = new Vector3(.035f, .035f, 0);
            puzzleRoot.localRotation = Quaternion.identity; ApplyPreferences(); Recenter();
        }
        private void ApplyPreferences()
        {
            if (game == null) return;
            puzzleRoot.localScale = Vector3.one * (.88f * game.Settings.contentScale);
        }
        private void LateUpdate()
        {
            if (!Finite(presentationRoot.localPosition) || !Finite(presentationRoot.localRotation))
            {
                presentationRoot.SetPositionAndRotation(SafeStart, Quaternion.identity);
                RequestTrackingPlacement();
            }
            if (!Finite(puzzleRoot.localPosition) || !Finite(puzzleRoot.localRotation)) ResetContent();
#if !UNITY_EDITOR
            if (!IsFullSpace || game == null || game.Paused) return;
            if (SpatialAdapterRuntime.Instance == null || SpatialAdapterRuntime.IsSessionInitialized() != 0) return;
            var pose = new Pose();
            int result = SpatialAdapterRuntime.GetHMDPose(ref pose);
            if (result != 0 || !TryGetComfortPlacement(pose, out var placement))
            {
                // Lost/default tracking never overwrites the last safe placement.
                stableSeconds = 0; poseWaitSeconds += Time.unscaledDeltaTime;
                if (!fallbackReported && poseWaitSeconds > 2)
                {
                    fallbackReported = true;
                    Debug.LogWarning($"[Orbital Loom] Placement waiting for stage head pose: result={result}, head={pose.position:F3}; keeping safe content={presentationRoot.position:F3}");
                }
                return;
            }
            bool stable = Vector3.Distance(previousHead.position, pose.position) < .08f &&
                Quaternion.Angle(previousHead.rotation, pose.rotation) < 8;
            previousHead = pose;
            stableSeconds = stable ? stableSeconds + Time.unscaledDeltaTime : 0;
            if ((initialPosePending || recenterPending) && stableSeconds < StablePoseSeconds) return;
            if (initialPosePending || recenterPending)
            {
                resetSeconds = 1; initialPosePending = recenterPending = false;
                Debug.Log($"[Orbital Loom] Eye-level placement: head={pose.position:F3}, content={placement.position:F3}, distance={ViewingDistance:F2}m");
            }
            float distance = Vector3.Distance(presentationRoot.position, pose.position);
            float angle = Vector3.Angle(placement.rotation * Vector3.forward, presentationRoot.position - pose.position);
            if (!game.Settings.comfortMode && (angle > 30 || distance < 1.4f || distance > 3.4f)) resetSeconds = 1;
            if (resetSeconds <= 0) return;
            resetSeconds -= Time.unscaledDeltaTime;
            // Equivalent to Lerp(.12) at 72 Hz, independent of actual frame rate.
            float alpha = 1 - Mathf.Exp(-9.203f * Time.unscaledDeltaTime);
            presentationRoot.position = Vector3.Lerp(presentationRoot.position, placement.position, alpha);
            presentationRoot.rotation = Quaternion.Slerp(presentationRoot.rotation, placement.rotation, alpha);
#endif
        }
        /// <summary>
        /// Turn a floor-relative head pose into an upright play area. Ignore
        /// pitch when positioning so looking down cannot place it on the floor.
        /// Reject default startup poses; cover ordinary seated/standing heights.
        /// </summary>
        public static bool TryGetComfortPlacement(Pose head, out Pose placement)
        {
            placement = default;
            if (!Finite(head.position) || !Finite(head.rotation) || head.position.y < .65f || head.position.y > 2.7f) return false;
            Vector3 forward = Vector3.ProjectOnPlane(head.rotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < .1f) return false;
            forward.Normalize();
            Vector3 center = head.position + forward * ViewingDistance;
            center.y = Mathf.Clamp(head.position.y - .12f, .95f, 2.3f);
            placement = new Pose(center, Quaternion.LookRotation(forward));
            return true;
        }
        private static bool Finite(Vector3 v) => Point3.IsFinite(v.x) && Point3.IsFinite(v.y) && Point3.IsFinite(v.z);
        private static bool Finite(Quaternion q) => Point3.IsFinite(q.x) && Point3.IsFinite(q.y) && Point3.IsFinite(q.z) && Point3.IsFinite(q.w) && Quaternion.Dot(q, q) > .01f;
    }
}
