using ByteDance.PICO.SpatialAdapter;
using ByteDance.PICO.SpatialAdapter.Component;
using UnityEngine;

namespace OrbitalLoom.WindowSystem
{
    /// <summary>
    /// Native text requires a state-tracked entity in adapter 6.1. The Editor
    /// repair gives each label a disabled collider on the excluded raycast layer;
    /// the SDK registers its transform without adding an input target. All native
    /// writes remain in the SDK's own frame and entity lifecycle.
    /// </summary>
    public sealed class SpatialPresentationSync : MonoBehaviour
    {
        private SpatialAdapterNativeText[] labels;
        private int settledFrames;
        private bool reported;
        private void Awake() { labels = GetComponentsInChildren<SpatialAdapterNativeText>(true); }
        private void OnApplicationPause(bool paused) { if (!paused) { settledFrames = 0; reported = false; } }
        private void LateUpdate()
        {
#if UNITY_EDITOR
            // SDK 6.1's mock uses an uninitialized justification snapshot. All
            // app labels are centered; correct only the Editor mock, not SDK data
            // or the device's native text lifecycle.
            foreach (var label in labels)
                foreach (var mock in label.GetComponentsInChildren<TextMesh>())
                {
                    mock.anchor = TextAnchor.MiddleCenter; mock.alignment = TextAlignment.Center;
                    var rect = mock.GetComponent<RectTransform>();
                    if (rect == null) continue;
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                    rect.anchoredPosition = Vector2.zero;
                }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            if (SpatialAdapterRuntime.Instance == null || SpatialAdapterRuntime.IsSessionInitialized() != 0) return;
            if (settledFrames++ < 5)
            {
                // Reassert placement after registration. The SDK owns the write.
                foreach (var label in labels) label.transform.hasChanged = true;
                return;
            }
            if (reported) return;
            reported = true;
            int shown = 0;
            foreach (var label in labels)
            {
                if (!label.gameObject.activeInHierarchy) continue;
                var tracker = label.GetComponent<SpatialAdapterGameObjectTracker>();
                Debug.Log($"[Orbital Loom] Native text tracked: {label.name} uid={tracker?.uid} local={label.transform.localPosition:F3} world={label.transform.position:F3}");
                if (++shown >= 8) break;
            }
            Debug.Log($"[Orbital Loom] Native presentation ready: labels={labels.Length}; SDK-owned state synchronization");
#endif
        }
    }
}
